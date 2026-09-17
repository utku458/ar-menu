using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ArMenu.Api.Authentication;
using ArMenu.Api.Endpoints.Authentication;
using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.Api;

internal static class ApiTestExtensions
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static async Task<string> SignInAsync(this HttpClient client, string tenantSlug, string email, string password = TestConfiguration.Password)
    {
        using var response = await client.PostAsJsonAsync($"/api/v1/tenants/{tenantSlug}/auth/sign-in", new { email, password }, Ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthenticationEndpoints.AccessTokenResponse>(Ct);
        return body!.AccessToken;
    }

    public static HttpClient Authenticate(this HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return document.RootElement.Clone();
    }

    /// <summary>The raw <c>Set-Cookie</c> header of the refresh token cookie, if the response set one.</summary>
    public static string? RefreshCookieHeader(this HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value => value.StartsWith($"{RefreshTokenCookie.Name}=", StringComparison.Ordinal))
            : null;

    /// <summary>The refresh token value from the response's <c>Set-Cookie</c> header.</summary>
    public static string? RefreshToken(this HttpResponseMessage response) =>
        response.RefreshCookieHeader() is { } header
            ? header[(RefreshTokenCookie.Name.Length + 1)..header.IndexOf(';', StringComparison.Ordinal)]
            : null;

    /// <summary>Sends a request carrying an explicit refresh token cookie, for tests that need to replay old tokens.</summary>
    public static async Task<HttpResponseMessage> PostWithRefreshTokenAsync(this HttpClient client, string path, string refreshToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("Cookie", $"{RefreshTokenCookie.Name}={refreshToken}");
        return await client.SendAsync(request, Ct);
    }
}
