using System.Net;
using System.Net.Http.Json;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ArMenu.IntegrationTests.Api.Http;

public sealed class ReverseProxyTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private const string TrustedProxy = "10.0.0.4";
    private const int PermitsPerMinute = 2;

    private readonly ArMenuApiFactory _factory = new(
        database,
        configureServices: services => services.AddSingleton<IStartupFilter, PeerAddressFromTestHeader>(),
        settings: new Dictionary<string, string?>
        {
            ["ReverseProxy:KnownProxies:0"] = TrustedProxy,
            ["RateLimiting:AuthenticationPermitsPerMinute"] = PermitsPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture),
        });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Guests_behind_a_trusted_proxy_are_limited_by_their_own_address()
    {
        var signIn = await SignInPathAsync();
        using var client = _factory.CreateClient();

        for (var attempt = 0; attempt < PermitsPerMinute; attempt++)
        {
            (await AttemptAsync(client, signIn, peer: TrustedProxy, forwardedFor: "203.0.113.10")).ShouldBe(HttpStatusCode.Unauthorized);
        }

        (await AttemptAsync(client, signIn, peer: TrustedProxy, forwardedFor: "203.0.113.10")).ShouldBe(HttpStatusCode.TooManyRequests);

        // Another guest on the same proxy is not punished for the first one.
        (await AttemptAsync(client, signIn, peer: TrustedProxy, forwardedFor: "203.0.113.11")).ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Forwarded_addresses_from_anyone_else_are_ignored()
    {
        var signIn = await SignInPathAsync();
        using var client = _factory.CreateClient();

        for (var attempt = 0; attempt < PermitsPerMinute; attempt++)
        {
            (await AttemptAsync(client, signIn, peer: "198.51.100.7", forwardedFor: $"203.0.113.{attempt + 20}")).ShouldBe(HttpStatusCode.Unauthorized);
        }

        // Making up a new address per attempt does not reset the limit.
        (await AttemptAsync(client, signIn, peer: "198.51.100.7", forwardedFor: "203.0.113.99")).ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Api_responses_can_never_be_rendered_or_framed()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/menus/does-not-exist", Ct);

        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        response.Headers.GetValues("Referrer-Policy").ShouldBe(["no-referrer"]);
        response.Headers.GetValues("Content-Security-Policy").ShouldBe(["default-src 'none'; frame-ancestors 'none'; base-uri 'none'"]);
    }

    private async Task<string> SignInPathAsync()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        return $"/api/v1/tenants/{restaurant.Tenant.Slug}/auth/sign-in";
    }

    private static async Task<HttpStatusCode> AttemptAsync(HttpClient client, string path, string peer, string forwardedFor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new { email = "guess@armenu.test", password = "not-the-password" }),
        };
        request.Headers.Add(PeerAddressFromTestHeader.HeaderName, peer);
        request.Headers.Add("X-Forwarded-For", forwardedFor);

        using var response = await client.SendAsync(request, Ct);
        return response.StatusCode;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    /// <summary>The in-memory test server has no network peer; this plays the connecting machine.</summary>
    private sealed class PeerAddressFromTestHeader : IStartupFilter
    {
        public const string HeaderName = "X-Test-Peer-Address";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers[HeaderName] is [{ } peer])
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
                }

                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
