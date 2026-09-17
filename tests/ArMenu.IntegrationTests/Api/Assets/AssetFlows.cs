using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.Api.Assets;

/// <summary>What the dashboard does with files: ask for an upload, send the bytes straight to storage, use the upload.</summary>
internal static class AssetFlows
{
    public const string UploadsPath = "/api/v1/manage/assets/uploads";

    // Talks to storage directly, without credentials, like a browser.
    public static readonly HttpClient Browser = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static Task<byte[]> DemoFileAsync(string relativePath) => File.ReadAllBytesAsync(RepositoryPaths.DemoAsset(relativePath), Ct);

    public static async Task<HttpClient> SignedInClientAsync(
        this ArMenuApiFactory factory,
        PostgresDatabaseFixture database,
        SeededTenant restaurant,
        TenantRole role)
    {
        var member = await database.Services.SeedMemberAsync(restaurant.Tenant, role, Ct);
        var client = factory.CreateBrowserClient();
        return client.Authenticate(await client.SignInAsync(restaurant.Tenant.Slug, member.Email));
    }

    public static async Task<UploadBody> CreateUploadAsync(HttpClient client, string kind, string contentType, long size)
    {
        using var response = await client.PostAsJsonAsync(UploadsPath, new { kind, contentType, size }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<UploadBody>(Ct))!;
    }

    public static async Task<HttpStatusCode> PutAsync(UploadBody upload, byte[] body, string? contentType = null)
    {
        using var content = new ByteArrayContent(body);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType ?? upload.Headers["Content-Type"]);
        using var response = await Browser.PutAsync(upload.Url, content, Ct);
        return response.StatusCode;
    }

    public static async Task<UploadBody> UploadAsync(HttpClient client, string kind, string contentType, byte[] file)
    {
        var upload = await CreateUploadAsync(client, kind, contentType, file.Length);
        (await PutAsync(upload, file)).ShouldBe(HttpStatusCode.OK);
        return upload;
    }

    public static Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid uploadId, string kind) =>
        client.PostAsJsonAsync($"{UploadsPath}/{uploadId}/publish", new { kind }, Ct);

    public static async Task<PublishedAssetBody> UploadAndPublishAsync(HttpClient client, string kind, string contentType, byte[] file)
    {
        var upload = await UploadAsync(client, kind, contentType, file);
        using var publish = await PublishAsync(client, upload.UploadId, kind);
        publish.StatusCode.ShouldBe(HttpStatusCode.OK, await publish.Content.ReadAsStringAsync(Ct));
        return (await publish.Content.ReadFromJsonAsync<PublishedAssetBody>(Ct))!;
    }
}

internal sealed record UploadBody(Guid UploadId, Uri Url, string Method, Dictionary<string, string> Headers);

internal sealed record PublishedAssetBody(string Path, Uri Url, string ContentType, long Size);
