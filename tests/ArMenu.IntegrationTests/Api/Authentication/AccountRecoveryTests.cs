using System.Net;
using System.Net.Http.Json;
using ArMenu.Api.Endpoints.Authentication;
using ArMenu.Application.Abstractions.Email;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace ArMenu.IntegrationTests.Api.Authentication;

public sealed class AccountRecoveryTests : IAsyncLifetime
{
    private const string NewPassword = "a-brand-new-long-password";

    private readonly PostgresDatabaseFixture _database;
    private readonly FakeEmailSender _email;
    private readonly FakeTimeProvider _time = new(DateTimeOffset.UtcNow);
    private readonly ArMenuApiFactory _factory;

    public AccountRecoveryTests(PostgresDatabaseFixture database)
    {
        _database = database;
        _email = new FakeEmailSender(database);
        _factory = new ArMenuApiFactory(database, configureServices: services => services
            .AddSingleton<IEmailSender>(_email)
            .AddSingleton<TimeProvider>(_time));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_reset_link_sets_a_new_password_and_ends_every_earlier_session()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        var manager = await _database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Manager, Ct);
        using var oldDevice = _factory.CreateBrowserClient();
        await oldDevice.SignInAsync(restaurant.Tenant.Slug, manager.Email);
        _time.Advance(TimeSpan.FromSeconds(1));

        using var requested = await Anonymous().PostAsJsonAsync("/api/v1/auth/password-reset", new { email = manager.Email.ToUpperInvariant(), language = "en" }, Ct);
        requested.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var message = await _email.WaitForMessageToAsync(manager.Email);
        message.Language.ShouldBe("en");
        var token = FakeEmailSender.TokenIn(message, "reset-password");

        using var reset = await Anonymous().PostAsJsonAsync("/api/v1/auth/password-reset/confirm", new { token, password = NewPassword }, Ct);
        reset.StatusCode.ShouldBe(HttpStatusCode.NoContent, await reset.Content.ReadAsStringAsync(Ct));

        (await SignInAsync(restaurant.Tenant.Slug, manager.Email, TestConfiguration.Password)).ShouldBe(HttpStatusCode.Unauthorized);
        (await SignInAsync(restaurant.Tenant.Slug, manager.Email, NewPassword)).ShouldBe(HttpStatusCode.OK);

        using var refresh = await oldDevice.PostAsync($"/api/v1/tenants/{restaurant.Tenant.Slug}/auth/refresh", null, Ct);
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "a session from before the reset");

        using var reused = await Anonymous().PostAsJsonAsync("/api/v1/auth/password-reset/confirm", new { token, password = "yet-another-password" }, Ct);
        reused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await reused.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("user_token.already_used");
    }

    [Fact]
    public async Task Unknown_addresses_get_the_same_answer_and_no_email()
    {
        using var requested = await Anonymous().PostAsJsonAsync("/api/v1/auth/password-reset", new { email = $"nobody-{Guid.NewGuid():N}@armenu.test" }, Ct);

        requested.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await requested.Content.ReadAsStringAsync(Ct)).ShouldBeEmpty();
        await Task.Delay(300, Ct);
        _email.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Reset_links_expire_within_the_hour_and_a_newer_link_retires_the_older()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        var staff = await _database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Staff, Ct);

        await RequestResetAsync(staff.Email);
        var first = FakeEmailSender.TokenIn(await _email.WaitForMessageToAsync(staff.Email), "reset-password");
        await RequestResetAsync(staff.Email);
        var second = FakeEmailSender.TokenIn(await _email.WaitForMessageToAsync(staff.Email, count: 2), "reset-password");

        (await ConfirmResetAsync(first)).ShouldBe("user_token.already_used");

        _time.Advance(UserToken.PasswordResetLifetime);
        (await ConfirmResetAsync(second)).ShouldBe("user_token.expired");
    }

    [Fact]
    public async Task A_new_owner_verifies_the_address_before_inviting_anyone()
    {
        var slug = $"verify-{Guid.NewGuid():N}"[..30];
        var ownerEmail = $"owner-{Guid.NewGuid():N}@armenu.test";
        using var browser = _factory.CreateBrowserClient();
        using var signedUp = await browser.PostAsJsonAsync("/api/v1/tenants", new
        {
            businessName = "Doğrulama Lokantası",
            slug,
            defaultCulture = "tr",
            currency = "TRY",
            ownerFullName = "Selin Kaya",
            ownerEmail,
            password = NewPassword,
        }, Ct);
        signedUp.StatusCode.ShouldBe(HttpStatusCode.Created, await signedUp.Content.ReadAsStringAsync(Ct));
        browser.Authenticate((await signedUp.ReadJsonAsync()).GetProperty("accessToken").GetString()!);

        (await MeAsync(browser)).EmailVerified.ShouldBeFalse();
        using var tooEarly = await browser.PostAsJsonAsync("/api/v1/manage/team/invitations", new { email = "chef@armenu.test", role = "Staff" }, Ct);
        tooEarly.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await tooEarly.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("team.email_not_verified");

        var welcome = await _email.WaitForMessageToAsync(ownerEmail);
        welcome.Language.ShouldBe("tr");
        var firstLink = FakeEmailSender.TokenIn(welcome, "verify-email");

        // Asking again replaces the link.
        (await browser.PostAsJsonAsync("/api/v1/me/email-verification", new { language = "en" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var secondLink = FakeEmailSender.TokenIn(await _email.WaitForMessageToAsync(ownerEmail, count: 2), "verify-email");
        (await ConfirmVerificationAsync(firstLink)).ShouldBe(HttpStatusCode.Conflict);

        // A verification link is no password reset link.
        (await ConfirmResetAsync(secondLink)).ShouldBe("user_token.invalid_link");

        (await ConfirmVerificationAsync(secondLink)).ShouldBe(HttpStatusCode.NoContent);

        using var refreshed = await browser.PostAsync($"/api/v1/tenants/{slug}/auth/refresh", null, Ct);
        var token = await refreshed.Content.ReadFromJsonAsync<AuthenticationEndpoints.AccessTokenResponse>(Ct);
        browser.Authenticate(token!.AccessToken);
        (await MeAsync(browser)).EmailVerified.ShouldBeTrue();
        (await browser.PostAsJsonAsync("/api/v1/manage/team/invitations", new { email = "chef@armenu.test", role = "Staff" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private HttpClient Anonymous() => _factory.CreateClient();

    private async Task<HttpStatusCode> SignInAsync(string slug, string email, string password)
    {
        using var response = await Anonymous().PostAsJsonAsync($"/api/v1/tenants/{slug}/auth/sign-in", new { email, password }, Ct);
        return response.StatusCode;
    }

    private async Task RequestResetAsync(string email)
    {
        using var response = await Anonymous().PostAsJsonAsync("/api/v1/auth/password-reset", new { email }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    private async Task<string?> ConfirmResetAsync(string token)
    {
        using var response = await Anonymous().PostAsJsonAsync("/api/v1/auth/password-reset/confirm", new { token, password = NewPassword }, Ct);
        return response.IsSuccessStatusCode ? null : (await response.ReadJsonAsync()).GetProperty("code").GetString();
    }

    private async Task<HttpStatusCode> ConfirmVerificationAsync(string token)
    {
        using var response = await Anonymous().PostAsJsonAsync("/api/v1/auth/email-verification/confirm", new { token }, Ct);
        return response.StatusCode;
    }

    private static async Task<AuthenticationEndpoints.CurrentUserResponse> MeAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<AuthenticationEndpoints.CurrentUserResponse>("/api/v1/me", Ct))!;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
