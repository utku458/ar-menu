using System.Net;
using System.Net.Http.Json;
using ArMenu.Api.Endpoints.Authentication;
using ArMenu.Application.Abstractions.Email;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using static ArMenu.IntegrationTests.Api.Assets.AssetFlows;

namespace ArMenu.IntegrationTests.Api.Team;

public sealed class TeamInvitationTests : IAsyncLifetime
{
    private const string NewPassword = "a-long-enough-new-password";

    private readonly PostgresDatabaseFixture _database;
    private readonly FakeEmailSender _email = new();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.UtcNow);
    private readonly ArMenuApiFactory _factory;

    public TeamInvitationTests(PostgresDatabaseFixture database)
    {
        _database = database;
        _factory = new ArMenuApiFactory(database, configureServices: services => services
            .AddSingleton<IEmailSender>(_email)
            .AddSingleton<TimeProvider>(_time));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task An_invited_person_creates_an_account_joins_with_the_role_and_is_signed_in()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);

        using var invited = await owner.PostAsJsonAsync(InvitationsPath, new { email = "Chef@Armenu.test", role = "Staff", language = "en" }, Ct);

        invited.StatusCode.ShouldBe(HttpStatusCode.Created, await invited.Content.ReadAsStringAsync(Ct));
        (await invited.ReadJsonAsync()).GetProperty("emailSent").GetBoolean().ShouldBeTrue();

        var email = _email.Sent.ShouldHaveSingleItem();
        email.To.ShouldBe("chef@armenu.test");
        email.Language.ShouldBe("en");
        email.Subject.ShouldContain(restaurant.Tenant.Name);
        var link = _email.LinkSentTo("chef@armenu.test");
        link.GetLeftPart(UriPartial.Path).ShouldBe($"{TestConfiguration.DashboardUrl}{restaurant.Tenant.Slug}/join");

        var pending = (await TeamAsync(owner)).GetProperty("invitations").EnumerateArray().ShouldHaveSingleItem();
        pending.GetProperty("email").GetString().ShouldBe("chef@armenu.test");
        pending.GetProperty("isExpired").GetBoolean().ShouldBeFalse();

        using var guest = _factory.CreateBrowserClient();
        using var lookup = await guest.PostAsJsonAsync(LookupPath(restaurant.Tenant.Slug), new { token = TokenOf(link) }, Ct);
        lookup.StatusCode.ShouldBe(HttpStatusCode.OK);
        lookup.Headers.CacheControl!.NoStore.ShouldBeTrue();
        var details = await lookup.ReadJsonAsync();
        details.GetProperty("businessName").GetString().ShouldBe(restaurant.Tenant.Name);
        details.GetProperty("role").GetString().ShouldBe("Staff");
        details.GetProperty("hasAccount").GetBoolean().ShouldBeFalse();

        using var accepted = await guest.PostAsJsonAsync(
            AcceptPath(restaurant.Tenant.Slug),
            new { token = TokenOf(link), fullName = "Ayşe Yılmaz", password = NewPassword },
            Ct);

        accepted.StatusCode.ShouldBe(HttpStatusCode.OK, await accepted.Content.ReadAsStringAsync(Ct));
        accepted.RefreshCookieHeader().ShouldNotBeNull();
        var token = await accepted.Content.ReadFromJsonAsync<AuthenticationEndpoints.AccessTokenResponse>(Ct);
        var me = await guest.Authenticate(token!.AccessToken).GetFromJsonAsync<AuthenticationEndpoints.CurrentUserResponse>("/api/v1/me", Ct);
        me!.FullName.ShouldBe("Ayşe Yılmaz");
        me.Role.ShouldBe(nameof(TenantRole.Staff));

        var team = await TeamAsync(owner);
        team.GetProperty("invitations").GetArrayLength().ShouldBe(0);
        team.GetProperty("members").EnumerateArray().Select(member => member.GetProperty("email").GetString()).ShouldContain("chef@armenu.test");

        using var reused = await _factory.CreateBrowserClient().PostAsJsonAsync(
            AcceptPath(restaurant.Tenant.Slug), new { token = TokenOf(link), fullName = "Someone Else", password = NewPassword }, Ct);
        reused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await reused.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("invitation.no_longer_pending");
    }

    [Fact]
    public async Task Someone_with_an_account_joins_by_confirming_their_password()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        var elsewhere = await _database.Services.SeedTenantAsync(Ct);
        var chef = await _database.Services.SeedMemberAsync(elsewhere.Tenant, TenantRole.Owner, Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);
        await InviteAsync(owner, chef.Email, "Manager");
        var token = TokenOf(_email.LinkSentTo(chef.Email));
        using var guest = _factory.CreateBrowserClient();

        (await (await guest.PostAsJsonAsync(LookupPath(restaurant.Tenant.Slug), new { token }, Ct)).ReadJsonAsync())
            .GetProperty("hasAccount").GetBoolean().ShouldBeTrue();

        using var wrongPassword = await guest.PostAsJsonAsync(AcceptPath(restaurant.Tenant.Slug), new { token, password = "not-my-password" }, Ct);
        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await TeamAsync(owner)).GetProperty("invitations").GetArrayLength().ShouldBe(1, "a failed attempt leaves the invitation usable");

        using var accepted = await guest.PostAsJsonAsync(AcceptPath(restaurant.Tenant.Slug), new { token, password = TestConfiguration.Password }, Ct);

        accepted.StatusCode.ShouldBe(HttpStatusCode.OK, await accepted.Content.ReadAsStringAsync(Ct));
        var member = (await TeamAsync(owner)).GetProperty("members").EnumerateArray().Single(candidate => candidate.GetProperty("email").GetString() == chef.Email);
        member.GetProperty("role").GetString().ShouldBe("Manager");

        // Still the owner of their own business: one identity, a role per tenant.
        using var home = _factory.CreateBrowserClient();
        await home.SignInAsync(elsewhere.Tenant.Slug, chef.Email);
    }

    [Fact]
    public async Task A_link_works_only_under_its_own_business_and_only_intact()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        var other = await _database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);
        await InviteAsync(owner, "waiter@armenu.test");
        var token = TokenOf(_email.LinkSentTo("waiter@armenu.test"));
        using var guest = _factory.CreateBrowserClient();

        // A well-formed secret that differs in one character: the hash comparison, not the parser, must reject it.
        var tampered = string.Concat(token[..40], token[40] == 'A' ? "B" : "A", token[41..]);

        foreach (var (slug, candidate) in new[] { (other.Tenant.Slug, token), (restaurant.Tenant.Slug, tampered), (restaurant.Tenant.Slug, "garbage") })
        {
            using var lookup = await guest.PostAsJsonAsync(LookupPath(slug), new { token = candidate }, Ct);
            lookup.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await lookup.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("invitation.invalid_link");
        }
    }

    [Fact]
    public async Task Sending_again_retires_the_old_link_and_an_expired_invitation_can_be_renewed()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);
        var invitationId = await InviteAsync(owner, "host@armenu.test");
        var firstToken = TokenOf(_email.LinkSentTo("host@armenu.test"));
        using var guest = _factory.CreateBrowserClient();

        _time.Advance(TenantInvitation.Lifetime + TimeSpan.FromMinutes(1));
        using var expired = await guest.PostAsJsonAsync(LookupPath(restaurant.Tenant.Slug), new { token = firstToken }, Ct);
        expired.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await expired.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("invitation.expired");
        (await TeamAsync(owner)).GetProperty("invitations")[0].GetProperty("isExpired").GetBoolean().ShouldBeTrue();

        using var resent = await owner.PostAsJsonAsync($"{InvitationsPath}/{invitationId}/resend", new { language = "tr" }, Ct);
        resent.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondToken = TokenOf(_email.LinkSentTo("host@armenu.test"));
        _email.Sent[^1].Language.ShouldBe("tr");

        (await guest.PostAsJsonAsync(LookupPath(restaurant.Tenant.Slug), new { token = firstToken }, Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await guest.PostAsJsonAsync(LookupPath(restaurant.Tenant.Slug), new { token = secondToken }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_same_address_is_not_invited_twice_nor_a_member_invited_again()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        var manager = await _database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Manager, Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);
        await InviteAsync(owner, "twice@armenu.test");

        using var again = await owner.PostAsJsonAsync(InvitationsPath, new { email = "twice@armenu.test", role = "Staff" }, Ct);
        using var member = await owner.PostAsJsonAsync(InvitationsPath, new { email = manager.Email, role = "Staff" }, Ct);
        using var asOwner = await owner.PostAsJsonAsync(InvitationsPath, new { email = "boss@armenu.test", role = "Owner" }, Ct);

        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await again.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("invitation.already_pending");
        member.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await member.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("membership.already_member");
        asOwner.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await asOwner.ReadJsonAsync()).GetProperty("errorCodes").GetProperty("role")[0].GetString().ShouldBe("invitation.owner_not_invitable");
    }

    [Fact]
    public async Task An_invitation_survives_a_mail_server_outage_and_can_be_sent_again()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);
        _email.Delivers = false;

        using var invited = await owner.PostAsJsonAsync(InvitationsPath, new { email = "later@armenu.test", role = "Staff" }, Ct);

        invited.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await invited.ReadJsonAsync();
        body.GetProperty("emailSent").GetBoolean().ShouldBeFalse();
        (await TeamAsync(owner)).GetProperty("invitations").GetArrayLength().ShouldBe(1);

        _email.Delivers = true;
        using var resent = await owner.PostAsJsonAsync($"{InvitationsPath}/{body.GetProperty("invitationId").GetGuid()}/resend", new { }, Ct);
        (await resent.ReadJsonAsync()).GetProperty("emailSent").GetBoolean().ShouldBeTrue();
        _email.Sent.ShouldHaveSingleItem().To.ShouldBe("later@armenu.test");
    }

    [Fact]
    public async Task A_new_account_needs_a_name_and_a_strong_enough_password()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);
        await InviteAsync(owner, "weak@armenu.test");
        var token = TokenOf(_email.LinkSentTo("weak@armenu.test"));

        using var accepted = await _factory.CreateBrowserClient().PostAsJsonAsync(AcceptPath(restaurant.Tenant.Slug), new { token, password = "short" }, Ct);

        accepted.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var codes = (await accepted.ReadJsonAsync()).GetProperty("errorCodes");
        codes.GetProperty("fullName")[0].GetString().ShouldBe("user.full_name_required");
        codes.GetProperty("password")[0].GetString().ShouldBe("auth.password_too_short");
    }

    [Fact]
    public async Task Only_the_owner_manages_the_team()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        using var manager = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Manager);

        (await manager.GetAsync(TeamPath, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await manager.PostAsJsonAsync(InvitationsPath, new { email = "friend@armenu.test", role = "Manager" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        _email.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Roles_change_and_removed_members_lose_their_sessions_but_the_owner_stays()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        var staff = await _database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Staff, Ct);
        using var owner = await _factory.SignedInClientAsync(_database, restaurant, TenantRole.Owner);
        using var staffBrowser = _factory.CreateBrowserClient();
        await staffBrowser.SignInAsync(restaurant.Tenant.Slug, staff.Email);

        var members = (await TeamAsync(owner)).GetProperty("members").EnumerateArray().ToList();
        var staffId = members.Single(member => member.GetProperty("email").GetString() == staff.Email).GetProperty("id").GetGuid();
        var ownerId = members.Single(member => member.GetProperty("role").GetString() == "Owner").GetProperty("id").GetGuid();

        (await owner.PutAsJsonAsync($"{TeamPath}/members/{staffId}/role", new { role = "Manager" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var refreshed = await staffBrowser.PostAsync($"/api/v1/tenants/{restaurant.Tenant.Slug}/auth/refresh", null, Ct);
        var token = await refreshed.Content.ReadFromJsonAsync<AuthenticationEndpoints.AccessTokenResponse>(Ct);
        (await _factory.CreateClient().Authenticate(token!.AccessToken).GetFromJsonAsync<AuthenticationEndpoints.CurrentUserResponse>("/api/v1/me", Ct))!
            .Role.ShouldBe(nameof(TenantRole.Manager));

        using var ownerDemotion = await owner.PutAsJsonAsync($"{TeamPath}/members/{ownerId}/role", new { role = "Staff" }, Ct);
        ownerDemotion.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        using var ownerRemoval = await owner.DeleteAsync($"{TeamPath}/members/{ownerId}", Ct);
        (await ownerRemoval.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("membership.owner_unchangeable");

        (await owner.DeleteAsync($"{TeamPath}/members/{staffId}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var afterRemoval = await staffBrowser.PostAsync($"/api/v1/tenants/{restaurant.Tenant.Slug}/auth/refresh", null, Ct);
        afterRemoval.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await TeamAsync(owner)).GetProperty("members").GetArrayLength().ShouldBe(1);
    }

    private const string TeamPath = "/api/v1/manage/team";
    private const string InvitationsPath = $"{TeamPath}/invitations";

    private static string LookupPath(string slug) => $"/api/v1/tenants/{slug}/invitations/lookup";

    private static string AcceptPath(string slug) => $"/api/v1/tenants/{slug}/invitations/accept";

    private static string TokenOf(Uri link) => link.Fragment.TrimStart('#');

    private static async Task<Guid> InviteAsync(HttpClient owner, string email, string role = "Staff")
    {
        using var response = await owner.PostAsJsonAsync(InvitationsPath, new { email, role }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return (await response.ReadJsonAsync()).GetProperty("invitationId").GetGuid();
    }

    private static async Task<System.Text.Json.JsonElement> TeamAsync(HttpClient owner)
    {
        using var response = await owner.GetAsync(TeamPath, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.ReadJsonAsync();
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
