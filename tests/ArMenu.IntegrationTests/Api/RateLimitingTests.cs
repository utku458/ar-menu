using System.Net;
using System.Net.Http.Json;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ArMenu.IntegrationTests.Api;

public sealed class RateLimitingTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private const int AuthenticationPermitsPerMinute = 3;

    private readonly WebApplicationFactory<Program> _factory = new ArMenuApiFactory(database)
        .WithWebHostBuilder(builder => builder.UseSetting(
            "RateLimiting:AuthenticationPermitsPerMinute",
            AuthenticationPermitsPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Authentication_endpoints_throttle_rapid_attempts()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var signInPath = $"/api/v1/tenants/{restaurant.Tenant.Slug}/auth/sign-in";
        var credentials = new { email = "attacker@armenu.test", password = "guess-guess-guess" };

        for (var attempt = 0; attempt < AuthenticationPermitsPerMinute; attempt++)
        {
            using var allowed = await client.PostAsJsonAsync(signInPath, credentials, Ct);
            allowed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        using var throttled = await client.PostAsJsonAsync(signInPath, credentials, Ct);

        throttled.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        throttled.Headers.RetryAfter.ShouldNotBeNull();
        (await throttled.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("rate_limited");
    }

    [Fact]
    public async Task Public_menus_are_not_affected_by_authentication_throttling()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        for (var request = 0; request < AuthenticationPermitsPerMinute * 3; request++)
        {
            using var menu = await client.GetAsync($"/api/v1/menus/{restaurant.Tenant.Slug}", Ct);
            menu.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
