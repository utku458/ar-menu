using System.Net;
using System.Net.Http.Json;
using ArMenu.Api.Endpoints.Authentication;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ArMenu.IntegrationTests.Api.Authentication;

public sealed class RefreshSessionTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_browser_refreshes_with_its_cookie_and_receives_a_rotated_one()
    {
        var (restaurant, owner) = await SeedOwnerAsync();
        using var browser = _factory.CreateBrowserClient();

        using var signIn = await browser.PostAsJsonAsync(AuthPath(restaurant, "sign-in"), new { owner.Email, Password = TestConfiguration.Password }, Ct);
        var original = signIn.RefreshToken();

        using var refresh = await browser.PostAsync(AuthPath(restaurant, "refresh"), content: null, Ct);

        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
        refresh.RefreshToken().ShouldNotBeNull().ShouldNotBe(original);

        var token = await refresh.Content.ReadFromJsonAsync<AuthenticationEndpoints.AccessTokenResponse>(Ct);
        using var me = await browser.Authenticate(token!.AccessToken).GetAsync("/api/v1/me", Ct);
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Replaying_a_stolen_refresh_token_revokes_the_session_for_everyone()
    {
        var (restaurant, owner) = await SeedOwnerAsync();
        using var client = CreateClientWithoutCookieJar();
        var first = await SignInAndGetRefreshTokenAsync(client, restaurant, owner);

        var second = await RefreshAsync(client, restaurant, first);
        var third = await RefreshAsync(client, restaurant, second);

        // The first token is two rotations old: presenting it can only mean it was copied.
        using var replay = await client.PostWithRefreshTokenAsync(AuthPath(restaurant, "refresh"), first);
        using var legitimateHolder = await client.PostWithRefreshTokenAsync(AuthPath(restaurant, "refresh"), third);

        replay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        legitimateHolder.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await database.ScalarAsOwnerAsync<string>(
            $"SELECT revocation_reason FROM user_sessions WHERE user_id = '{owner.UserId.Value}'", Ct)).ShouldBe("RefreshTokenReused");
    }

    [Fact]
    public async Task Signing_out_ends_the_session_and_clears_the_cookie()
    {
        var (restaurant, owner) = await SeedOwnerAsync();
        using var client = CreateClientWithoutCookieJar();
        var refreshToken = await SignInAndGetRefreshTokenAsync(client, restaurant, owner);

        using var signOut = await client.PostWithRefreshTokenAsync(AuthPath(restaurant, "sign-out"), refreshToken);
        using var refreshAfterSignOut = await client.PostWithRefreshTokenAsync(AuthPath(restaurant, "refresh"), refreshToken);

        signOut.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        signOut.RefreshCookieHeader().ShouldNotBeNull().ShouldContain("expires=Thu, 01 Jan 1970");
        refreshAfterSignOut.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_tokens_only_work_in_the_workspace_that_issued_them()
    {
        var (restaurant, owner) = await SeedOwnerAsync();
        var otherRestaurant = await database.Services.SeedTenantAsync(Ct);
        using var client = CreateClientWithoutCookieJar();
        var refreshToken = await SignInAndGetRefreshTokenAsync(client, restaurant, owner);

        using var elsewhere = await client.PostWithRefreshTokenAsync(AuthPath(otherRestaurant, "refresh"), refreshToken);
        using var home = await client.PostWithRefreshTokenAsync(AuthPath(restaurant, "refresh"), refreshToken);

        elsewhere.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        home.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Removing_a_membership_ends_its_sessions()
    {
        var (restaurant, owner) = await SeedOwnerAsync();
        using var client = CreateClientWithoutCookieJar();
        var refreshToken = await SignInAndGetRefreshTokenAsync(client, restaurant, owner);

        await database.ExecuteAsOwnerAsync($"DELETE FROM tenant_memberships WHERE user_id = '{owner.UserId.Value}'", Ct);
        using var refresh = await client.PostWithRefreshTokenAsync(AuthPath(restaurant, "refresh"), refreshToken);

        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await database.ScalarAsOwnerAsync<long>($"SELECT count(*) FROM user_sessions WHERE user_id = '{owner.UserId.Value}'", Ct)).ShouldBe(0);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static string AuthPath(SeededTenant tenant, string action) => $"/api/v1/tenants/{tenant.Tenant.Slug}/auth/{action}";

    private async Task<(SeededTenant Restaurant, SeededMember Owner)> SeedOwnerAsync()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        return (restaurant, await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Owner, Ct));
    }

    private HttpClient CreateClientWithoutCookieJar() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false,
    });

    private static async Task<string> SignInAndGetRefreshTokenAsync(HttpClient client, SeededTenant restaurant, SeededMember member)
    {
        using var response = await client.PostAsJsonAsync(AuthPath(restaurant, "sign-in"), new { member.Email, Password = TestConfiguration.Password }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return response.RefreshToken().ShouldNotBeNull();
    }

    private static async Task<string> RefreshAsync(HttpClient client, SeededTenant restaurant, string refreshToken)
    {
        using var response = await client.PostWithRefreshTokenAsync(AuthPath(restaurant, "refresh"), refreshToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return response.RefreshToken().ShouldNotBeNull();
    }
}
