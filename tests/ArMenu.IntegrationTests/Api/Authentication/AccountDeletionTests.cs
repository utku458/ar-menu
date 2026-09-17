using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ArMenu.Application.Abstractions.Email;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace ArMenu.IntegrationTests.Api.Authentication;

public sealed class AccountDeletionTests : IAsyncLifetime
{
    private const string DeletionPath = "/api/v1/me/deletion";

    private readonly PostgresDatabaseFixture _database;
    private readonly FakeEmailSender _email;
    private readonly ArMenuApiFactory _factory;

    public AccountDeletionTests(PostgresDatabaseFixture database)
    {
        _database = database;
        _email = new FakeEmailSender(database);
        _factory = new ArMenuApiFactory(database, configureServices: services => services.AddSingleton<IEmailSender>(_email));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Deleting_an_account_closes_the_businesses_only_it_ran_and_leaves_the_others()
    {
        var ownBusiness = await _database.Services.SeedTenantAsync(Ct, tenant => tenant.Rename("Ayşe'nin Kafesi"));
        var workplace = await _database.Services.SeedTenantAsync(Ct, tenant => tenant.Rename("Balıkçı"));
        var person = await _database.Services.SeedMemberAsync(ownBusiness.Tenant, TenantRole.Owner, Ct);
        await _database.Services.AddMembershipAsync(workplace.Tenant, person.UserId, TenantRole.Staff, Ct);
        var boss = await _database.Services.SeedMemberAsync(workplace.Tenant, TenantRole.Owner, Ct);

        using var client = _factory.CreateBrowserClient();
        client.Authenticate(await client.SignInAsync(workplace.Tenant.Slug, person.Email));

        var preview = await client.GetFromJsonAsync<JsonElement>(DeletionPath, Ct);
        preview.GetProperty("closingWorkspaces").EnumerateArray().ShouldHaveSingleItem().GetProperty("slug").GetString().ShouldBe(ownBusiness.Tenant.Slug);
        preview.GetProperty("workspacesToHandOver").GetArrayLength().ShouldBe(0);
        preview.GetProperty("workspacesToLeave").EnumerateArray().ShouldHaveSingleItem().GetProperty("otherMembers").GetInt32().ShouldBe(1);

        using var deleted = await client.PostAsJsonAsync(DeletionPath, new { password = TestConfiguration.Password, language = "tr" }, Ct);
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent, await deleted.Content.ReadAsStringAsync(Ct));

        using var refresh = await client.PostAsync($"/api/v1/tenants/{workplace.Tenant.Slug}/auth/refresh", null, Ct);
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await SignInStatusAsync(workplace.Tenant.Slug, person.Email)).ShouldBe(HttpStatusCode.Unauthorized);

        using var closedMenu = await _factory.CreateClient().GetAsync($"/api/v1/menus/{ownBusiness.Tenant.Slug}", Ct);
        closedMenu.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var openMenu = await _factory.CreateClient().GetAsync($"/api/v1/menus/{workplace.Tenant.Slug}", Ct);
        openMenu.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var bossClient = _factory.CreateBrowserClient();
        bossClient.Authenticate(await bossClient.SignInAsync(workplace.Tenant.Slug, boss.Email));
        var team = await bossClient.GetFromJsonAsync<JsonElement>("/api/v1/manage/team", Ct);
        team.GetProperty("members").EnumerateArray().ShouldHaveSingleItem().GetProperty("userId").GetGuid().ShouldBe(boss.UserId.Value);

        // The history keeps that someone left, without a name.
        var history = await bossClient.GetFromJsonAsync<JsonElement>("/api/v1/manage/history", Ct);
        var departure = history.GetProperty("entries").EnumerateArray().First();
        departure.GetProperty("action").GetString().ShouldBe("deleted");
        departure.GetProperty("subject").GetProperty("type").GetString().ShouldBe("member");
        departure.GetProperty("subject").GetProperty("person").GetProperty("fullName").ValueKind.ShouldBe(JsonValueKind.Null);

        var goodbye = await _email.WaitForMessageToAsync(person.Email);
        goodbye.Language.ShouldBe("tr");
        goodbye.TextBody.ShouldContain("Ayşe'nin Kafesi");

        // The address is free again.
        using var signUp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/tenants", new
        {
            businessName = "Yeni Başlangıç",
            slug = $"test-{Guid.NewGuid():N}",
            defaultCulture = "tr",
            currency = "TRY",
            ownerFullName = "Ayşe",
            ownerEmail = person.Email,
            password = TestConfiguration.Password,
        }, Ct);
        signUp.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task An_owner_with_a_team_hands_the_business_over_first()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        var owner = await _database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Owner, Ct);
        await _database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Staff, Ct);
        using var client = _factory.CreateBrowserClient();
        client.Authenticate(await client.SignInAsync(restaurant.Tenant.Slug, owner.Email));

        var preview = await client.GetFromJsonAsync<JsonElement>(DeletionPath, Ct);
        preview.GetProperty("workspacesToHandOver").EnumerateArray().ShouldHaveSingleItem().GetProperty("slug").GetString().ShouldBe(restaurant.Tenant.Slug);

        using var refused = await client.PostAsJsonAsync(DeletionPath, new { password = TestConfiguration.Password }, Ct);
        refused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await refused.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("account.ownership_transfer_required");

        (await SignInStatusAsync(restaurant.Tenant.Slug, owner.Email)).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_password_confirms_the_deletion()
    {
        var restaurant = await _database.Services.SeedTenantAsync(Ct);
        var manager = await _database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Manager, Ct);
        using var client = _factory.CreateBrowserClient();
        client.Authenticate(await client.SignInAsync(restaurant.Tenant.Slug, manager.Email));

        using var wrong = await client.PostAsJsonAsync(DeletionPath, new { password = "not-my-password" }, Ct);

        wrong.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.GetAsync("/api/v1/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await SignInStatusAsync(restaurant.Tenant.Slug, manager.Email)).ShouldBe(HttpStatusCode.OK);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private async Task<HttpStatusCode> SignInStatusAsync(string slug, string email)
    {
        using var response = await _factory.CreateBrowserClient().PostAsJsonAsync($"/api/v1/tenants/{slug}/auth/sign-in", new { email, password = TestConfiguration.Password }, Ct);
        return response.StatusCode;
    }
}
