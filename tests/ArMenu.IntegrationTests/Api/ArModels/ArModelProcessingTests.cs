using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ArMenu.Application.Abstractions.Assets;
using ArMenu.Application.Common.Diagnostics;
using ArMenu.Application.Menus.Queries.GetPublicMenu;
using ArMenu.Domain.ArModels;
using ArMenu.Domain.Memberships;
using ArMenu.Infrastructure.ArModels;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using static ArMenu.IntegrationTests.Api.Assets.AssetFlows;

namespace ArMenu.IntegrationTests.Api.ArModels;

/// <remarks>The processing queue spans every tenant of the shared database, so its users run one at a time.</remarks>
[Collection(ProcessingQueueTestGroup.Name)]
public sealed class ArModelProcessingTests : IAsyncLifetime
{
    private readonly PostgresDatabaseFixture _database;
    private readonly FakeModelProcessor _processor = new();
    private readonly ArMenuApiFactory _factory;

    public ArModelProcessingTests(PostgresDatabaseFixture database, StorageFixture storage)
    {
        _database = database;
        _factory = new ArMenuApiFactory(database, storage, services => services.AddSingleton<IModelProcessor>(_processor));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ArModelProcessingDispatcher Dispatcher => _factory.Services.GetRequiredService<ArModelProcessingDispatcher>();

    [Fact]
    public async Task One_uploaded_glb_becomes_every_file_guests_devices_need()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);
        var source = await DemoFileAsync("models/sea-bass.scene-viewer.glb");

        using var started = await StartAsync(owner, restaurant, source);

        started.StatusCode.ShouldBe(HttpStatusCode.Accepted, await started.Content.ReadAsStringAsync(Ct));
        started.Headers.Location?.ToString().ShouldEndWith($"/api/v1/manage/menu/items/{restaurant.ItemId}/ar-model/processing");
        (await started.ReadJsonAsync()).GetProperty("status").GetString().ShouldBe("queued");

        (await Dispatcher.RunNextAsync(Ct)).ShouldBeTrue();
        (await Dispatcher.RunNextAsync(Ct)).ShouldBeFalse("the finished job left the queue");

        _processor.Sources.ShouldHaveSingleItem().ShouldBe(source);
        var status = await StatusAsync(owner, restaurant);
        status.GetProperty("status").GetString().ShouldBe("succeeded");
        status.GetProperty("attempts").GetInt32().ShouldBe(1);
        status.GetProperty("report").GetProperty("optimized").GetProperty("triangles").GetInt32().ShouldBe(99_620);
        status.GetProperty("report").GetProperty("warnings")[0].GetString().ShouldBe("model.simplified");

        using var guest = _factory.CreateBrowserClient();
        var menu = await guest.GetFromJsonAsync<PublicMenuResponse>($"/api/v1/menus/{restaurant.Tenant.Slug}", Ct);
        var model = menu!.Categories[0].Items[0].ArModel.ShouldNotBeNull();
        var stem = $"tenants/{restaurant.Tenant.Id.Value:N}/assets/{status.GetProperty("id").GetGuid():N}";
        model.GlbUrl.AbsolutePath.ShouldEndWith($"{stem}.glb");
        model.SceneViewerGlbUrl!.AbsolutePath.ShouldEndWith($"{stem}.scene-viewer.glb");
        model.UsdzUrl!.AbsolutePath.ShouldEndWith($"{stem}.usdz");
        model.PosterUrl!.AbsolutePath.ShouldEndWith($"{stem}.webp");

        foreach (var url in new[] { model.GlbUrl, model.SceneViewerGlbUrl, model.UsdzUrl, model.PosterUrl })
        {
            using var file = await Browser.GetAsync(url, Ct);
            file.StatusCode.ShouldBe(HttpStatusCode.OK, url.ToString());
            file.Headers.CacheControl?.ToString().ShouldBe("public, max-age=31536000, immutable");
        }
    }

    [Theory]
    [InlineData("model.texture_unsupported", "model.texture_unsupported")]
    [InlineData("model.from_a_newer_processor", "asset.processing_failed")]
    public async Task A_rejected_model_fails_with_its_reason_and_leaves_the_menu_as_it_was(string rejection, string reported)
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);
        _processor.ThenAnswer(new ModelProcessingOutcome.Rejected(rejection, "Texture 'crust' is image/ktx2."));
        using var started = await StartAsync(owner, restaurant, await DemoFileAsync("models/sea-bass.glb"));

        await Dispatcher.RunNextAsync(Ct);

        var status = await StatusAsync(owner, restaurant);
        status.GetProperty("status").GetString().ShouldBe("failed");
        status.GetProperty("failureCode").GetString().ShouldBe(reported);
        (await ItemModelAsync(owner, restaurant)).ValueKind.ShouldBe(JsonValueKind.Null);
        (await Dispatcher.RunNextAsync(Ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task A_processor_outage_is_retried_and_the_model_still_arrives()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);
        _processor.ThenAnswer(new ModelProcessingOutcome.Unavailable("HTTP 503 (processor.busy)"));
        using var attempts = new MetricCollector<long>(
            _factory.Services.GetRequiredService<IMeterFactory>(), ArMenuTelemetry.Name, "armenu.ar_model.processing.attempts");
        using var timeToPublish = new MetricCollector<double>(
            _factory.Services.GetRequiredService<IMeterFactory>(), ArMenuTelemetry.Name, "armenu.ar_model.processing.time_to_publish");
        using var started = await StartAsync(owner, restaurant, await DemoFileAsync("models/sea-bass.glb"));

        await Dispatcher.RunNextAsync(Ct);
        var retrying = await StatusAsync(owner, restaurant);
        retrying.GetProperty("status").GetString().ShouldBe("queued");
        retrying.GetProperty("attempts").GetInt32().ShouldBe(1);

        await Dispatcher.RunNextAsync(Ct);
        var status = await StatusAsync(owner, restaurant);
        status.GetProperty("status").GetString().ShouldBe("succeeded");
        status.GetProperty("attempts").GetInt32().ShouldBe(2);

        // What operators see: one retry, then a published model.
        attempts.GetMeasurementSnapshot().Select(measurement => measurement.Tags[ArMenuTelemetry.Tags.Outcome]).ShouldBe(["retrying", "succeeded"]);
        timeToPublish.GetMeasurementSnapshot().ShouldHaveSingleItem().Value.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Processing_gives_up_after_every_attempt_failed()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);
        for (var attempt = 0; attempt < ArModelProcessing.MaxAttempts; attempt++)
        {
            _processor.ThenAnswer(new ModelProcessingOutcome.Unavailable("connection refused"));
        }

        using var started = await StartAsync(owner, restaurant, await DemoFileAsync("models/sea-bass.glb"));
        while (await Dispatcher.RunNextAsync(Ct))
        {
        }

        var status = await StatusAsync(owner, restaurant);
        status.GetProperty("status").GetString().ShouldBe("failed");
        status.GetProperty("failureCode").GetString().ShouldBe("asset.processing_unavailable");
        _processor.Jobs.Count.ShouldBe(ArModelProcessing.MaxAttempts);
    }

    [Fact]
    public async Task Files_the_processor_got_wrong_are_never_published()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);
        _processor.Then(async job =>
        {
            var outcome = await _processor.ProcessDemoAsync(job, Ct);
            // Scene Viewer cannot open a Meshopt compressed model: the web model in its place must be caught.
            await FakeModelProcessor.UploadAsync(job.Outputs.SceneViewerModel, await DemoFileAsync("models/smash-burger.glb"), Ct);
            return outcome;
        });
        using var started = await StartAsync(owner, restaurant, await DemoFileAsync("models/sea-bass.glb"));

        await Dispatcher.RunNextAsync(Ct);

        var status = await StatusAsync(owner, restaurant);
        status.GetProperty("status").GetString().ShouldBe("failed");
        status.GetProperty("failureCode").GetString().ShouldBe("asset.processing_output_invalid");
        (await ItemModelAsync(owner, restaurant)).ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task The_newest_upload_wins_even_when_it_arrives_while_an_older_one_is_processing()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);
        var source = await DemoFileAsync("models/sea-bass.glb");
        _processor.Then(async job =>
        {
            // The business uploads a better model while the first one is being processed.
            using var newer = await StartAsync(owner, restaurant, source);
            newer.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            return await _processor.ProcessDemoAsync(job, Ct);
        });
        using var first = await StartAsync(owner, restaurant, source);
        var firstId = (await first.ReadJsonAsync()).GetProperty("id").GetGuid();

        while (await Dispatcher.RunNextAsync(Ct))
        {
        }

        var status = await StatusAsync(owner, restaurant);
        var newestId = status.GetProperty("id").GetGuid();
        newestId.ShouldNotBe(firstId);
        status.GetProperty("status").GetString().ShouldBe("succeeded");
        (await ItemModelAsync(owner, restaurant)).GetProperty("glbPath").GetString().ShouldNotBeNull().ShouldContain(newestId.ToString("N"));
    }

    [Fact]
    public async Task A_file_the_pipeline_cannot_read_is_refused_before_it_is_queued()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);

        using var started = await StartAsync(owner, restaurant, "no glTF header here"u8.ToArray());

        started.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await started.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("asset.model_invalid");
        (await Dispatcher.RunNextAsync(Ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task One_business_can_neither_process_nor_watch_the_uploads_of_another()
    {
        var first = await _database.Services.SeedTenantAsync(Ct);
        var second = await _database.Services.SeedTenantAsync(Ct);
        using var firstOwner = await _factory.SignedInClientAsync(_database, first, TenantRole.Owner);
        using var secondOwner = await _factory.SignedInClientAsync(_database, second, TenantRole.Owner);
        var upload = await UploadAsync(firstOwner, "model", "model/gltf-binary", await DemoFileAsync("models/sea-bass.glb"));

        using var stolen = await secondOwner.PostAsJsonAsync(
            $"/api/v1/manage/menu/items/{second.ItemId}/ar-model/processing", new { uploadId = upload.UploadId }, Ct);
        stolen.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stolen.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("asset.upload_not_found");

        using var mine = await firstOwner.PostAsJsonAsync(
            $"/api/v1/manage/menu/items/{first.ItemId}/ar-model/processing", new { uploadId = upload.UploadId }, Ct);
        mine.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        using var watched = await secondOwner.GetAsync($"/api/v1/manage/menu/items/{first.ItemId}/ar-model/processing", Ct);
        watched.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Staff_cannot_process_models()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        using var staff = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Staff);

        using var response = await staff.PostAsJsonAsync(
            $"/api/v1/manage/menu/items/{restaurant.ItemId}/ar-model/processing", new { uploadId = Guid.NewGuid() }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    public async ValueTask InitializeAsync()
    {
        // Jobs earlier tests left queued would otherwise be claimed first.
        while (await Dispatcher.RunNextAsync(Ct))
        {
        }

        _processor.Jobs.Clear();
        _processor.Sources.Clear();
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static async Task<HttpResponseMessage> StartAsync(HttpClient client, SeededTenant restaurant, byte[] source)
    {
        var upload = await UploadAsync(client, "model", "model/gltf-binary", source);
        return await client.PostAsJsonAsync(
            $"/api/v1/manage/menu/items/{restaurant.ItemId}/ar-model/processing", new { uploadId = upload.UploadId }, Ct);
    }

    private static async Task<JsonElement> StatusAsync(HttpClient client, SeededTenant restaurant)
    {
        using var response = await client.GetAsync($"/api/v1/manage/menu/items/{restaurant.ItemId}/ar-model/processing", Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.ReadJsonAsync();
    }

    private static async Task<JsonElement> ItemModelAsync(HttpClient client, SeededTenant restaurant)
    {
        using var response = await client.GetAsync("/api/v1/manage/menu", Ct);
        var menu = await response.ReadJsonAsync();
        return menu.GetProperty("categories")[0].GetProperty("items")[0].GetProperty("arModel");
    }
}
