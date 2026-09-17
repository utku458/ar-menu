using System.Net;
using System.Net.Http.Json;
using ArMenu.Api.Endpoints.Authentication;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.Api.Authentication;

public sealed class SignInTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Members_get_an_access_token_and_a_locked_down_refresh_cookie()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        var manager = await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Manager, Ct);
        using var client = _factory.CreateBrowserClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{restaurant.Tenant.Slug}/auth/sign-in",
            new { manager.Email, Password = TestConfiguration.Password },
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();

        var cookie = response.RefreshCookieHeader().ShouldNotBeNull().ToLowerInvariant();
        cookie.ShouldContain("httponly");
        cookie.ShouldContain("secure");
        cookie.ShouldContain("samesite=strict");
        cookie.ShouldContain($"path=/api/v1/tenants/{restaurant.Tenant.Slug}/auth");

        var token = await response.Content.ReadFromJsonAsync<AuthenticationEndpoints.AccessTokenResponse>(Ct);
        var me = await client.Authenticate(token!.AccessToken).GetFromJsonAsync<AuthenticationEndpoints.CurrentUserResponse>("/api/v1/me", Ct);
        me!.Email.ShouldBe(manager.Email);
        me.Role.ShouldBe(nameof(TenantRole.Manager));
    }

    [Fact]
    public async Task Wrong_passwords_and_unknown_accounts_get_the_same_answer()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        var owner = await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Owner, Ct);
        using var client = _factory.CreateBrowserClient();

        using var wrongPassword = await SignInAsync(client, restaurant.Tenant.Slug, owner.Email, "not-the-password");
        using var unknownAccount = await SignInAsync(client, restaurant.Tenant.Slug, "nobody@armenu.test", TestConfiguration.Password);

        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknownAccount.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var wrongPasswordProblem = await wrongPassword.ReadJsonAsync();
        var unknownAccountProblem = await unknownAccount.ReadJsonAsync();
        wrongPasswordProblem.GetProperty("code").GetString().ShouldBe("auth.invalid_credentials");
        unknownAccountProblem.GetProperty("code").GetString().ShouldBe("auth.invalid_credentials");
        wrongPasswordProblem.GetProperty("detail").GetString().ShouldBe(unknownAccountProblem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Valid_credentials_only_open_workspaces_the_account_belongs_to()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);
        var burgerLabOwner = await database.Services.SeedMemberAsync(burgerLab.Tenant, TenantRole.Owner, Ct);
        using var client = _factory.CreateBrowserClient();

        using var response = await SignInAsync(client, fishRestaurant.Tenant.Slug, burgerLabOwner.Email, TestConfiguration.Password);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Repeated_failures_lock_the_account_even_against_the_correct_password()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        var owner = await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Owner, Ct);
        using var client = _factory.CreateBrowserClient();

        for (var attempt = 0; attempt < User.MaxFailedSignInAttempts; attempt++)
        {
            using var failed = await SignInAsync(client, restaurant.Tenant.Slug, owner.Email, "guessed-password");
            failed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        using var correctPassword = await SignInAsync(client, restaurant.Tenant.Slug, owner.Email, TestConfiguration.Password);

        correctPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Suspended_workspaces_do_not_exist_for_sign_in()
    {
        var suspended = await database.Services.SeedTenantAsync(Ct, tenant => tenant.Suspend());
        var owner = await database.Services.SeedMemberAsync(suspended.Tenant, TenantRole.Owner, Ct);
        using var client = _factory.CreateBrowserClient();

        using var response = await SignInAsync(client, suspended.Tenant.Slug, owner.Email, TestConfiguration.Password);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static Task<HttpResponseMessage> SignInAsync(HttpClient client, string slug, string email, string password) =>
        client.PostAsJsonAsync($"/api/v1/tenants/{slug}/auth/sign-in", new { email, password }, Ct);
}
