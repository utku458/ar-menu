using System.Net;
using System.Net.Http.Json;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.Api.Authentication;

public sealed class WorkspaceListTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task People_see_every_business_they_belong_to_and_no_other()
    {
        var home = await database.Services.SeedTenantAsync(Ct, tenant => tenant.Rename("A Burger Evi"));
        var sideJob = await database.Services.SeedTenantAsync(Ct, tenant => tenant.Rename("B Balıkçı"));
        var elsewhere = await database.Services.SeedTenantAsync(Ct);
        var person = await database.Services.SeedMemberAsync(home.Tenant, TenantRole.Owner, Ct);
        await database.Services.AddMembershipAsync(sideJob.Tenant, person.UserId, TenantRole.Staff, Ct);
        await database.Services.SeedMemberAsync(elsewhere.Tenant, TenantRole.Owner, Ct);

        using var client = _factory.CreateBrowserClient();
        client.Authenticate(await client.SignInAsync(sideJob.Tenant.Slug, person.Email));

        var workspaces = await client.GetFromJsonAsync<List<WorkspaceBody>>("/api/v1/me/workspaces", Ct);

        workspaces.ShouldBe(
        [
            new WorkspaceBody(home.Tenant.Slug, "A Burger Evi", "Owner"),
            new WorkspaceBody(sideJob.Tenant.Slug, "B Balıkçı", "Staff"),
        ]);
    }

    [Fact]
    public async Task Listing_workspaces_requires_a_signed_in_user()
    {
        using var response = await _factory.CreateClient().GetAsync("/api/v1/me/workspaces", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private sealed record WorkspaceBody(string Slug, string Name, string Role);
}
