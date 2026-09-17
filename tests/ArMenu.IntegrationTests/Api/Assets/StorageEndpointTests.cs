using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;
using static ArMenu.IntegrationTests.Api.Assets.AssetFlows;

namespace ArMenu.IntegrationTests.Api.Assets;

/// <summary>Where the API reaches storage and where browsers do can be different hosts, as behind a private network.</summary>
public sealed class StorageEndpointTests(PostgresDatabaseFixture database, StorageFixture storage) : IAsyncLifetime
{
    private readonly ArMenuApiFactory _factory = new(database, storage, settings: new Dictionary<string, string?>
    {
        ["Storage:PublicServiceUrl"] = "https://uploads.armenu.test/",
    });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Browsers_get_upload_urls_signed_for_the_public_endpoint()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);

        var upload = await CreateUploadAsync(owner, "poster", "image/png", 10);

        upload.Url.GetLeftPart(UriPartial.Authority).ShouldBe("https://uploads.armenu.test");
        upload.Url.AbsolutePath.ShouldStartWith($"/{StorageFixture.UploadsBucket}/");
        upload.Url.Query.ShouldContain("X-Amz-Signature=");
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
