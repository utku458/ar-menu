using System.Net;
using System.Net.Http.Json;
using ArMenu.Api.Endpoints.Authentication;
using ArMenu.Api.Endpoints.Platform;
using ArMenu.Application.Platform.Queries.ListBusinesses;
using ArMenu.Application.Team.Queries.GetTeam;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Tenants;
using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.Api.Platform;

/// <summary>
/// The platform administrator: the account the API creates for itself on first start, which opens businesses and runs
/// them without being a member of any.
/// </summary>
public sealed class PlatformAdministrationTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private const string AdministratorName = "admin";
    private const string AdministratorPassword = "admin1234";
    private const string OwnerPassword = "owner-password-1234";

    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_administrator_is_created_on_start_and_signs_in_without_naming_a_workspace()
    {
        using var client = _factory.CreateBrowserClient();

        using var response = await SignInAsync(client, AdministratorName, AdministratorPassword);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var signIn = (await response.Content.ReadFromJsonAsync<AuthenticationEndpoints.WorkspaceAccessTokenResponse>(Ct)).ShouldNotBeNull();
        signIn.IsPlatformAdmin.ShouldBeTrue();
        signIn.Workspace.ShouldBe(TenantSlug.PlatformValue);

        // The session belongs to the platform workspace, so its cookie is confined to that workspace's endpoints.
        response.RefreshCookieHeader().ShouldNotBeNull().ShouldContain($"path=/api/v1/tenants/{TenantSlug.PlatformValue}/auth");

        var me = await client.Authenticate(signIn.AccessToken)
            .GetFromJsonAsync<AuthenticationEndpoints.CurrentUserResponse>("/api/v1/me", Ct);
        me.ShouldNotBeNull();
        me.UserName.ShouldBe(AdministratorName);
        me.IsPlatformAdmin.ShouldBeTrue();
    }

    [Fact]
    public async Task It_opens_a_business_whose_owner_then_signs_in_with_a_user_name()
    {
        using var client = _factory.CreateBrowserClient();
        var administrator = await SignInAsAdministratorAsync(client);
        var ownerName = $"owner-{Guid.NewGuid():N}"[..20];
        var slug = $"business-{Guid.NewGuid():N}"[..20];

        using var opened = await client.Authenticate(administrator.AccessToken).PostAsJsonAsync(
            "/api/v1/platform/businesses",
            NewBusiness(slug, ownerName),
            Ct);

        opened.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await opened.Content.ReadFromJsonAsync<PlatformEndpoints.OpenedBusinessResponse>(Ct)).ShouldNotBeNull().Slug.ShouldBe(slug);

        // The owner was given a name and a password, and nothing else: no invitation, no address to confirm.
        using var ownerClient = _factory.CreateBrowserClient();
        using var ownerSignIn = await SignInAsync(ownerClient, ownerName, OwnerPassword);

        ownerSignIn.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ownerSession = (await ownerSignIn.Content.ReadFromJsonAsync<AuthenticationEndpoints.WorkspaceAccessTokenResponse>(Ct)).ShouldNotBeNull();
        ownerSession.Workspace.ShouldBe(slug);
        ownerSession.IsPlatformAdmin.ShouldBeFalse();

        var me = await ownerClient.Authenticate(ownerSession.AccessToken)
            .GetFromJsonAsync<AuthenticationEndpoints.CurrentUserResponse>("/api/v1/me", Ct);
        me.ShouldNotBeNull();
        me.Role.ShouldBe(nameof(TenantRole.Owner));
        me.Tenant.Slug.ShouldBe(slug);
    }

    /// <summary>
    /// The heart of it: the administrator holds no membership in the business, so it gets in on an access token alone
    /// — the owner's screens open, and nothing refreshable is left behind in someone else's business.
    /// </summary>
    [Fact]
    public async Task It_runs_a_business_it_is_not_a_member_of_without_holding_a_session_there()
    {
        using var client = _factory.CreateBrowserClient();
        var administrator = await SignInAsAdministratorAsync(client);
        var slug = $"entered-{Guid.NewGuid():N}"[..20];

        using var opened = await client.Authenticate(administrator.AccessToken).PostAsJsonAsync(
            "/api/v1/platform/businesses",
            NewBusiness(slug, $"owner-{Guid.NewGuid():N}"[..20]),
            Ct);
        var business = (await opened.Content.ReadFromJsonAsync<PlatformEndpoints.OpenedBusinessResponse>(Ct)).ShouldNotBeNull();

        using var entered = await client.Authenticate(administrator.AccessToken)
            .PostAsync(new Uri($"/api/v1/platform/businesses/{business.Id}/enter", UriKind.Relative), content: null, Ct);

        entered.StatusCode.ShouldBe(HttpStatusCode.OK);
        var inside = (await entered.Content.ReadFromJsonAsync<PlatformEndpoints.EnteredBusinessResponse>(Ct)).ShouldNotBeNull();
        inside.Workspace.ShouldBe(slug);

        // Nothing refreshable was handed out for a business the administrator is not part of.
        entered.RefreshCookieHeader().ShouldBeNull();

        var me = await client.Authenticate(inside.AccessToken)
            .GetFromJsonAsync<AuthenticationEndpoints.CurrentUserResponse>("/api/v1/me", Ct);
        me.ShouldNotBeNull();
        me.Tenant.Slug.ShouldBe(slug);
        me.Role.ShouldBe(nameof(TenantRole.Owner));

        // The owner's screens open: the team is an owner-only one.
        using var team = await client.Authenticate(inside.AccessToken).GetAsync(new Uri("/api/v1/manage/team", UriKind.Relative), Ct);
        team.StatusCode.ShouldBe(HttpStatusCode.OK);

        // The business's own team does not gain a member from being administered.
        var members = await client.Authenticate(inside.AccessToken).GetFromJsonAsync<TeamResponse>("/api/v1/manage/team", Ct);
        members.ShouldNotBeNull().Members.ShouldAllBe(member => member.UserName != AdministratorName);
    }

    [Fact]
    public async Task A_business_owner_cannot_reach_the_platform()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        var owner = await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Owner, Ct);
        using var client = _factory.CreateBrowserClient();

        using var signIn = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{restaurant.Tenant.Slug}/auth/sign-in",
            new { owner.Email, Password = TestConfiguration.Password },
            Ct);
        var token = (await signIn.Content.ReadFromJsonAsync<AuthenticationEndpoints.AccessTokenResponse>(Ct)).ShouldNotBeNull();

        using var businesses = await client.Authenticate(token.AccessToken).GetAsync(new Uri("/api/v1/platform/businesses", UriKind.Relative), Ct);

        businesses.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_platform_workspace_is_not_a_business()
    {
        using var client = _factory.CreateBrowserClient();
        var administrator = await SignInAsAdministratorAsync(client);

        // Guests get the same answer for it as for a slug nobody ever claimed.
        using var menu = await client.GetAsync(new Uri($"/api/v1/menus/{TenantSlug.PlatformValue}", UriKind.Relative), Ct);
        menu.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // And it is not offered as somewhere to work either.
        var businesses = await client.Authenticate(administrator.AccessToken)
            .GetFromJsonAsync<IReadOnlyList<BusinessSummary>>("/api/v1/platform/businesses", Ct);
        businesses.ShouldNotBeNull().ShouldAllBe(business => business.Slug != TenantSlug.PlatformValue);
    }

    private static async Task<HttpResponseMessage> SignInAsync(HttpClient client, string userName, string password) =>
        await client.PostAsJsonAsync("/api/v1/auth/sign-in", new { UserName = userName, Password = password }, Ct);

    private static async Task<AuthenticationEndpoints.WorkspaceAccessTokenResponse> SignInAsAdministratorAsync(HttpClient client)
    {
        using var response = await SignInAsync(client, AdministratorName, AdministratorPassword);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<AuthenticationEndpoints.WorkspaceAccessTokenResponse>(Ct)).ShouldNotBeNull();
    }

    private static PlatformEndpoints.OpenBusinessRequest NewBusiness(string slug, string ownerUserName) =>
        new("Test Business", slug, "tr", "TRY", "Test Owner", ownerUserName, OwnerPassword, TimeZone: null);

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
