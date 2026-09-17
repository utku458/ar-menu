using ArMenu.Api.MultiTenancy;
using ArMenu.Api.UnitTests.TestSupport;
using Microsoft.AspNetCore.Http;

namespace ArMenu.Api.UnitTests.MultiTenancy;

public sealed class RouteTenantResolutionStrategyTests
{
    private readonly RouteTenantResolutionStrategy _strategy = new("tenant");

    [Fact]
    public async Task Resolves_the_tenant_whose_slug_is_in_the_route_regardless_of_case()
    {
        var tenant = TestTenants.Create("kadikoy-burger-lab");
        var lookup = new FakeTenantLookup(tenant);

        var resolved = await _strategy.ResolveAsync(RequestWithRouteValue("Kadikoy-Burger-Lab"), lookup);

        resolved.ShouldBe(tenant);
    }

    [Fact]
    public async Task Returns_null_for_an_unknown_slug()
    {
        var lookup = new FakeTenantLookup(TestTenants.Create("kadikoy-burger-lab"));

        var resolved = await _strategy.ResolveAsync(RequestWithRouteValue("bogazici-balikcisi"), lookup);

        resolved.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("x")]
    [InlineData("'; DROP TABLE tenants; --")]
    public async Task Missing_or_malformed_slugs_never_reach_the_cache_or_database(string? routeValue)
    {
        var lookup = new FakeTenantLookup(TestTenants.Create("kadikoy-burger-lab"));

        var resolved = await _strategy.ResolveAsync(RequestWithRouteValue(routeValue), lookup);

        resolved.ShouldBeNull();
        lookup.CallCount.ShouldBe(0);
    }

    private static DefaultHttpContext RequestWithRouteValue(string? slug)
    {
        var httpContext = new DefaultHttpContext();
        if (slug is not null)
        {
            httpContext.Request.RouteValues["tenant"] = slug;
        }

        return httpContext;
    }
}
