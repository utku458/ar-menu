using System.Net;
using System.Net.Http.Json;
using Amazon.S3;
using Amazon.S3.Model;
using ArMenu.Application.Abstractions.Assets;
using ArMenu.Application.Assets;
using ArMenu.Domain.Memberships;
using ArMenu.Infrastructure.ArModels;
using ArMenu.Infrastructure.Assets;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using static ArMenu.IntegrationTests.Api.Assets.AssetFlows;

namespace ArMenu.IntegrationTests.Api.ArModels;

[Collection(ProcessingQueueTestGroup.Name)]
public sealed class AssetCleanupTests(PostgresDatabaseFixture database, StorageFixture storage) : IAsyncDisposable
{
    private readonly FakeModelProcessor _processor = new();
    private readonly List<ArMenuApiFactory> _factories = [];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Files_nothing_refers_to_are_deleted_once_the_grace_period_is_over()
    {
        var api = Factory(gracePeriod: "1.00:00:00");
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await api.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        var tenant = restaurant.Tenant.Id;

        // A processed model on the menu, and the files a business leaves behind along the way.
        var upload = await UploadAsync(owner, "model", "model/gltf-binary", await DemoFileAsync("models/sea-bass.glb"));
        using (var started = await owner.PostAsJsonAsync(
            $"/api/v1/manage/menu/items/{restaurant.ItemId}/ar-model/processing", new { uploadId = upload.UploadId }, Ct))
        {
            started.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        }

        var dispatcher = api.Services.GetRequiredService<ArModelProcessingDispatcher>();
        while (await dispatcher.RunNextAsync(Ct))
        {
        }

        var inUse = (await ListAsync(StorageFixture.AssetsBucket, TenantAssetKeys.PublishedPrefix(tenant)))
            .Concat(await ListAsync(StorageFixture.UploadsBucket, TenantAssetKeys.SourcesPrefix(tenant)))
            .ToList();
        inUse.Count.ShouldBe(5, "four published files and the source");

        var unattachedPoster = await UploadAndPublishAsync(owner, "poster", "image/webp", await DemoFileAsync("posters/sea-bass.webp"));
        var abandonedUpload = await CreateUploadAsync(owner, "poster", "image/png", 3);
        (await PutAsync(abandonedUpload, [1, 2, 3])).ShouldBe(HttpStatusCode.OK);
        var failedSource = $"{TenantAssetKeys.SourcesPrefix(tenant)}{Guid.NewGuid():N}.glb";
        await PutObjectAsync(StorageFixture.UploadsBucket, failedSource);
        var demoFile = $"demo/cleanup-tests/{Guid.NewGuid():N}.glb";
        await PutObjectAsync(StorageFixture.AssetsBucket, demoFile);

        (await api.Services.GetRequiredService<AssetCleanup>().CleanTenantAsync(tenant, Ct)).ShouldBe(0, "every file is younger than a day");

        var expired = Factory(gracePeriod: "00:00:00");
        (await expired.Services.GetRequiredService<AssetCleanup>().CleanTenantAsync(tenant, Ct)).ShouldBe(3);

        foreach (var key in inUse)
        {
            (await ExistsAsync(key.StartsWith("sources/", StringComparison.Ordinal) ? StorageFixture.UploadsBucket : StorageFixture.AssetsBucket, key))
                .ShouldBeTrue(key);
        }

        (await ExistsAsync(StorageFixture.AssetsBucket, demoFile)).ShouldBeTrue("demo files are not a tenant's");
        (await ExistsAsync(StorageFixture.AssetsBucket, unattachedPoster.Path)).ShouldBeFalse();
        (await ExistsAsync(StorageFixture.UploadsBucket, TenantAssetKeys.Staging(tenant, abandonedUpload.UploadId))).ShouldBeFalse();
        (await ExistsAsync(StorageFixture.UploadsBucket, failedSource)).ShouldBeFalse();
    }

    [Fact]
    public async Task The_logo_on_the_menu_survives_a_cleanup()
    {
        var api = Factory(gracePeriod: "00:00:00");
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await api.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        var logo = await UploadAndPublishAsync(owner, "logo", "image/webp", await DemoFileAsync("posters/sea-bass.webp"));

        // No menu item points at a logo, so only the business's own record keeps it from being taken for an orphan.
        using (var saved = await owner.PutAsJsonAsync(
            "/api/v1/manage/settings/branding", new { name = restaurant.Tenant.Name, logoPath = logo.Path, accentColor = (string?)null }, Ct))
        {
            saved.StatusCode.ShouldBe(HttpStatusCode.NoContent, await saved.Content.ReadAsStringAsync(Ct));
        }

        await api.Services.GetRequiredService<AssetCleanup>().CleanTenantAsync(restaurant.Tenant.Id, Ct);

        (await ExistsAsync(StorageFixture.AssetsBucket, logo.Path)).ShouldBeTrue();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var factory in _factories)
        {
            await factory.DisposeAsync();
        }
    }

    private ArMenuApiFactory Factory(string gracePeriod)
    {
        var factory = new ArMenuApiFactory(
            database,
            storage,
            services => services.AddSingleton<IModelProcessor>(_processor),
            new Dictionary<string, string?> { ["AssetCleanup:GracePeriod"] = gracePeriod });
        _factories.Add(factory);
        return factory;
    }

    private async Task<List<string>> ListAsync(string bucket, string prefix) =>
        [.. ((await storage.Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, Prefix = prefix }, Ct)).S3Objects ?? [])
            .Select(entry => entry.Key)];

    private Task<PutObjectResponse> PutObjectAsync(string bucket, string key) =>
        storage.Client.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = key, ContentBody = "file" }, Ct);

    private async Task<bool> ExistsAsync(string bucket, string key)
    {
        try
        {
            await storage.Client.GetObjectMetadataAsync(bucket, key, Ct);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
