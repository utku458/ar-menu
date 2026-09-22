using System.Net;
using System.Net.Http.Json;
using ArMenu.Api.Endpoints.Authentication;
using ArMenu.Api.Endpoints.Team;
using ArMenu.Application.Team;
using ArMenu.Application.Team.Queries.GetTeam;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.Api.Team;

/// <summary>
/// Staff accounts an owner opens directly, with a user name and a password instead of an invitation nobody could
/// receive.
/// </summary>
public sealed class UserNameMemberTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private const string MemberPassword = "member-password-1234";
    private const string ReplacementPassword = "replacement-password-1234";

    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task An_owner_opens_a_staff_account_that_signs_in_with_its_name()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var client = _factory.CreateBrowserClient();
        var owner = await SignInAsOwnerAsync(client, restaurant);
        var memberName = $"kasa-{Guid.NewGuid():N}"[..16];

        using var added = await client.Authenticate(owner).PostAsJsonAsync(
            "/api/v1/manage/team/members",
            new { FullName = "Front of House", UserName = memberName, Password = MemberPassword, Role = TeamRole.Staff },
            Ct);

        added.StatusCode.ShouldBe(HttpStatusCode.Created);

        // The team lists the name to sign in with, not the placeholder address the account carries.
        var team = await client.Authenticate(owner).GetFromJsonAsync<TeamResponse>("/api/v1/manage/team", Ct);
        var member = team.ShouldNotBeNull().Members.Single(candidate => candidate.UserName == memberName);
        member.Role.ShouldBe(TeamRole.Staff);

        using var memberClient = _factory.CreateBrowserClient();
        using var signIn = await SignInAsync(memberClient, memberName, MemberPassword);

        signIn.StatusCode.ShouldBe(HttpStatusCode.OK);
        var session = (await signIn.Content.ReadFromJsonAsync<AuthenticationEndpoints.WorkspaceAccessTokenResponse>(Ct)).ShouldNotBeNull();
        session.Workspace.ShouldBe(restaurant.Tenant.Slug);

        var me = await memberClient.Authenticate(session.AccessToken)
            .GetFromJsonAsync<AuthenticationEndpoints.CurrentUserResponse>("/api/v1/me", Ct);
        me.ShouldNotBeNull().Role.ShouldBe(nameof(TenantRole.Staff));

        // Staff may mark dishes sold out, but editing the menu is not theirs.
        using var edit = await memberClient.Authenticate(session.AccessToken)
            .PostAsJsonAsync("/api/v1/manage/menu/categories", new { Name = new Dictionary<string, string> { ["tr"] = "Tatlılar" } }, Ct);
        edit.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Resetting_a_members_password_ends_the_sessions_it_had()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var client = _factory.CreateBrowserClient();
        var owner = await SignInAsOwnerAsync(client, restaurant);
        var memberName = $"kasa-{Guid.NewGuid():N}"[..16];

        using var added = await client.Authenticate(owner).PostAsJsonAsync(
            "/api/v1/manage/team/members",
            new { FullName = "Front of House", UserName = memberName, Password = MemberPassword, Role = TeamRole.Staff },
            Ct);
        var membership = (await added.Content.ReadFromJsonAsync<TeamEndpoints.MemberAddedResponse>(Ct)).ShouldNotBeNull();

        using var memberClient = _factory.CreateBrowserClient();
        using var before = await SignInAsync(memberClient, memberName, MemberPassword);
        before.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var reset = await client.Authenticate(owner).PutAsJsonAsync(
            $"/api/v1/manage/team/members/{membership.MembershipId}/password",
            new { Password = ReplacementPassword },
            Ct);

        reset.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // The session that was open cannot be refreshed any more...
        using var refreshed = await memberClient.PostAsync(
            new Uri($"/api/v1/tenants/{restaurant.Tenant.Slug}/auth/refresh", UriKind.Relative), content: null, Ct);
        refreshed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // ...the old password is gone, and the new one works.
        using var withOldPassword = await SignInAsync(memberClient, memberName, MemberPassword);
        using var withNewPassword = await SignInAsync(memberClient, memberName, ReplacementPassword);
        withOldPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        withNewPassword.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task An_owner_cannot_reach_another_businesss_member()
    {
        var mine = await database.Services.SeedTenantAsync(Ct);
        var theirs = await database.Services.SeedTenantAsync(Ct);
        using var client = _factory.CreateBrowserClient();
        var myOwner = await SignInAsOwnerAsync(client, mine);

        using var otherClient = _factory.CreateBrowserClient();
        var theirOwner = await SignInAsOwnerAsync(otherClient, theirs);
        using var theirMember = await otherClient.Authenticate(theirOwner).PostAsJsonAsync(
            "/api/v1/manage/team/members",
            new { FullName = "Their Staff", UserName = $"kasa-{Guid.NewGuid():N}"[..16], Password = MemberPassword, Role = TeamRole.Staff },
            Ct);
        var theirMembership = (await theirMember.Content.ReadFromJsonAsync<TeamEndpoints.MemberAddedResponse>(Ct)).ShouldNotBeNull();

        // Row-level security scopes the lookup to my own business, so their membership does not exist for me.
        using var reset = await client.Authenticate(myOwner).PutAsJsonAsync(
            $"/api/v1/manage/team/members/{theirMembership.MembershipId}/password",
            new { Password = ReplacementPassword },
            Ct);

        reset.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_e_mail_account_keeps_its_password_to_itself()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        var manager = await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Manager, Ct);
        using var client = _factory.CreateBrowserClient();
        var owner = await SignInAsOwnerAsync(client, restaurant);

        var team = await client.Authenticate(owner).GetFromJsonAsync<TeamResponse>("/api/v1/manage/team", Ct);
        var managerMembership = team.ShouldNotBeNull().Members.Single(member => member.UserId == manager.UserId.Value);
        managerMembership.UserName.ShouldBeNull();

        using var reset = await client.Authenticate(owner).PutAsJsonAsync(
            $"/api/v1/manage/team/members/{managerMembership.Id}/password",
            new { Password = ReplacementPassword },
            Ct);

        reset.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await reset.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("team.password_managed_by_mailbox");
    }

    private static async Task<HttpResponseMessage> SignInAsync(HttpClient client, string userName, string password) =>
        await client.PostAsJsonAsync("/api/v1/auth/sign-in", new { UserName = userName, Password = password }, Ct);

    private async Task<string> SignInAsOwnerAsync(HttpClient client, SeededTenant restaurant)
    {
        var owner = await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Owner, Ct);

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{restaurant.Tenant.Slug}/auth/sign-in",
            new { owner.Email, Password = TestConfiguration.Password },
            Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<AuthenticationEndpoints.AccessTokenResponse>(Ct)).ShouldNotBeNull().AccessToken;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
