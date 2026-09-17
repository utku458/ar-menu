using System.Net;
using System.Net.Http.Json;
using Amazon.S3;
using ArMenu.Application.Assets;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;
using static ArMenu.IntegrationTests.Api.Assets.AssetFlows;

namespace ArMenu.IntegrationTests.Api.Assets;

public sealed class AssetUploadTests(PostgresDatabaseFixture database, StorageFixture storage) : IAsyncLifetime
{
    private readonly ArMenuApiFactory _factory = new(database, storage);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Posters_and_apple_models_are_published_under_immutable_urls()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);

        var appleModel = await UploadAndPublishAsync(owner, "appleModel", "model/vnd.usdz+zip", await DemoFileAsync("models/smash-burger.usdz"));
        var poster = await UploadAndPublishAsync(owner, "poster", "image/webp", await DemoFileAsync("posters/smash-burger.webp"));

        appleModel.Path.ShouldStartWith($"tenants/{restaurant.Tenant.Id.Value:N}/assets/");
        appleModel.Path.ShouldEndWith(".usdz");
        poster.ContentType.ShouldBe("image/webp");
        using var published = await Browser.GetAsync(appleModel.Url, Ct);
        published.StatusCode.ShouldBe(HttpStatusCode.OK);
        published.Content.Headers.ContentType?.MediaType.ShouldBe("model/vnd.usdz+zip");
        published.Headers.CacheControl?.ToString().ShouldBe("public, max-age=31536000, immutable");
    }

    [Fact]
    public async Task Models_cannot_skip_processing()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        var upload = await UploadAsync(owner, "model", "model/gltf-binary", await DemoFileAsync("models/sea-bass.glb"));

        using var publish = await PublishAsync(owner, upload.UploadId, "model");

        publish.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await publish.ReadJsonAsync()).GetProperty("errorCodes").GetProperty("kind")[0].GetString().ShouldBe("asset.model_requires_processing");
    }

    [Fact]
    public async Task Storage_accepts_only_the_exact_file_that_was_declared()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        var upload = await CreateUploadAsync(owner, "poster", "image/png", size: 100);

        (await PutAsync(upload, new byte[101])).ShouldBe(HttpStatusCode.Forbidden);
        (await PutAsync(upload, new byte[100], contentType: "image/webp")).ShouldBe(HttpStatusCode.Forbidden);
        (await PutAsync(upload, new byte[100])).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_file_that_is_not_what_it_claims_is_rejected_and_discarded()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        var upload = await UploadAsync(owner, "poster", "image/png", "definitely not a PNG image"u8.ToArray());

        using var publish = await PublishAsync(owner, upload.UploadId, "poster");

        publish.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await publish.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("asset.poster_invalid");
        var staged = await Should.ThrowAsync<AmazonS3Exception>(() => storage.Client.GetObjectMetadataAsync(
            StorageFixture.UploadsBucket,
            TenantAssetKeys.Staging(restaurant.Tenant.Id, upload.UploadId),
            Ct));
        staged.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task One_business_can_neither_publish_nor_attach_the_files_of_another()
    {
        var first = await database.Services.SeedTenantAsync(Ct);
        var second = await database.Services.SeedTenantAsync(Ct);
        using var firstOwner = await _factory.SignedInClientAsync(database, first, TenantRole.Owner);
        using var secondOwner = await _factory.SignedInClientAsync(database, second, TenantRole.Owner);
        var upload = await UploadAsync(firstOwner, "poster", "image/webp", await DemoFileAsync("posters/sea-bass.webp"));

        using (var stolenPublish = await PublishAsync(secondOwner, upload.UploadId, "poster"))
        {
            stolenPublish.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        using var publish = await PublishAsync(firstOwner, upload.UploadId, "poster");
        var published = (await publish.Content.ReadFromJsonAsync<PublishedAssetBody>(Ct))!;
        using var stolenAttach = await secondOwner.PutAsJsonAsync(
            $"/api/v1/manage/menu/items/{second.ItemId}/ar-model",
            new { glbPath = $"tenants/{first.Tenant.Id.Value:N}/assets/{Guid.NewGuid():N}.glb", posterPath = published.Path },
            Ct);

        stolenAttach.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = (await stolenAttach.ReadJsonAsync()).GetProperty("errorCodes");
        errors.GetProperty("glbPath")[0].GetString().ShouldBe("asset.not_owned");
        errors.GetProperty("posterPath")[0].GetString().ShouldBe("asset.not_owned");
    }

    [Fact]
    public async Task Staff_cannot_upload_assets()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var staff = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Staff);

        using var response = await staff.PostAsJsonAsync(UploadsPath, new { kind = "poster", contentType = "image/png", size = 10 }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
