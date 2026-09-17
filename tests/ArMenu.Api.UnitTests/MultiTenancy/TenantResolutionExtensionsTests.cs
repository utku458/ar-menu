using ArMenu.Api.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ArMenu.Api.UnitTests.MultiTenancy;

public sealed class TenantResolutionExtensionsTests
{
    [Fact]
    public async Task Route_resolution_fails_fast_when_the_route_template_lacks_the_tenant_parameter()
    {
        await using var app = WebApplication.CreateSlimBuilder().Build();
        app.MapGet("/menus/{restaurant}", () => Results.Ok()).RequireTenantFromRoute("tenant");

        var dataSource = ((IEndpointRouteBuilder)app).DataSources.Single();

        Should.Throw<InvalidOperationException>(() => dataSource.Endpoints).Message.ShouldContain("'tenant'");
    }

    [Fact]
    public async Task Route_resolution_attaches_its_strategy_to_the_endpoint()
    {
        await using var app = WebApplication.CreateSlimBuilder().Build();
        app.MapGet("/menus/{tenant}", () => Results.Ok()).RequireTenantFromRoute();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources.Single().Endpoints.Single();

        endpoint.Metadata.GetMetadata<ITenantResolutionStrategy>()
            .ShouldBeOfType<RouteTenantResolutionStrategy>()
            .RouteParameterName.ShouldBe("tenant");
    }
}
