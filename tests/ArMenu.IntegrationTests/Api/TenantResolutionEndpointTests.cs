using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.Api;

/// <summary>End to end through HTTP: routing, tenant resolution middleware, caching, EF Core and row-level security.</summary>
public sealed class TenantResolutionEndpointTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Each_tenant_slug_sees_only_its_own_menu()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);
        using var client = _factory.CreateClient();

        var burgerLabCategories = await client.GetFromJsonAsync<Guid[]>(CategoriesUrl(burgerLab), Ct);
        var fishRestaurantCategories = await client.GetFromJsonAsync<Guid[]>(CategoriesUrl(fishRestaurant), Ct);

        burgerLabCategories.ShouldBe([burgerLab.CategoryId.Value]);
        fishRestaurantCategories.ShouldBe([fishRestaurant.CategoryId.Value]);
    }

    [Fact]
    public async Task Unknown_tenant_gets_a_problem_details_404()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(TenantProbeEndpoints.CategoriesRoute.Replace("{tenant}", "no-such-restaurant", StringComparison.Ordinal), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        problem.RootElement.GetProperty("code").GetString().ShouldBe("tenant.not_found");
        problem.RootElement.TryGetProperty("traceId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Suspended_tenant_is_not_served()
    {
        var suspended = await database.Services.SeedTenantAsync(Ct, tenant => tenant.Suspend());
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(CategoriesUrl(suspended), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Readiness_probe_checks_the_database()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/ready", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldBe("Healthy");
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static string CategoriesUrl(SeededTenant seeded) =>
        TenantProbeEndpoints.CategoriesRoute.Replace("{tenant}", seeded.Tenant.Slug, StringComparison.Ordinal);
}
