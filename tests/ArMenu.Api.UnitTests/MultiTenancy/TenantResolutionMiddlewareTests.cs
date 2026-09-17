using System.Text.Json;
using ArMenu.Api.MultiTenancy;
using ArMenu.Api.UnitTests.TestSupport;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArMenu.Api.UnitTests.MultiTenancy;

public sealed class TenantResolutionMiddlewareTests
{
    private static readonly TenantInfo BurgerLab = TestTenants.Create("kadikoy-burger-lab");
    private static readonly TenantInfo SuspendedCafe = TestTenants.Create("suspended-cafe", TenantStatus.Suspended);

    private readonly FakeTenantLookup _lookup = new(BurgerLab, SuspendedCafe);
    private readonly RecordingTenantContextSetter _tenantContextSetter = new();
    private bool _nextCalled;

    [Fact]
    public async Task Endpoints_without_a_resolution_strategy_are_not_tenant_bound()
    {
        var httpContext = CreateHttpContext(strategy: null, routeSlug: "kadikoy-burger-lab");

        await InvokeMiddlewareAsync(httpContext);

        _nextCalled.ShouldBeTrue();
        _tenantContextSetter.Tenant.ShouldBeNull();
        _lookup.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Active_tenant_is_bound_to_the_request_before_the_endpoint_runs()
    {
        var httpContext = CreateHttpContext(new RouteTenantResolutionStrategy("tenant"), routeSlug: "kadikoy-burger-lab");

        await InvokeMiddlewareAsync(httpContext);

        _tenantContextSetter.Tenant.ShouldBe(BurgerLab);
        _nextCalled.ShouldBeTrue();
    }

    [Theory]
    [InlineData("unknown-restaurant")]
    [InlineData("suspended-cafe")]
    public async Task Unknown_and_suspended_tenants_are_indistinguishable_404s(string slug)
    {
        var httpContext = CreateHttpContext(new RouteTenantResolutionStrategy("tenant"), routeSlug: slug);

        await InvokeMiddlewareAsync(httpContext);

        _nextCalled.ShouldBeFalse();
        _tenantContextSetter.Tenant.ShouldBeNull();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);

        using var problem = await ReadJsonAsync(httpContext.Response);
        problem.RootElement.GetProperty("code").GetString().ShouldBe("tenant.not_found");
        problem.RootElement.GetProperty("detail").GetString().ShouldBe("No active tenant matches this request.");
    }

    private async Task InvokeMiddlewareAsync(HttpContext httpContext)
    {
        var middleware = new TenantResolutionMiddleware(
            _ =>
            {
                _nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<TenantResolutionMiddleware>.Instance);

        await middleware.InvokeAsync(
            httpContext,
            _lookup,
            _tenantContextSetter,
            httpContext.RequestServices.GetRequiredService<IProblemDetailsService>());
    }

    private static DefaultHttpContext CreateHttpContext(ITenantResolutionStrategy? strategy, string routeSlug)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.RouteValues["tenant"] = routeSlug;

        var metadata = strategy is null ? new EndpointMetadataCollection() : new EndpointMetadataCollection(strategy);
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, metadata, "test endpoint"));

        return httpContext;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponse response)
    {
        response.Body.Position = 0;
        return await JsonDocument.ParseAsync(response.Body, cancellationToken: TestContext.Current.CancellationToken);
    }

    private sealed class RecordingTenantContextSetter : ITenantContextSetter
    {
        public TenantInfo? Tenant { get; private set; }

        public void SetTenant(TenantInfo tenant) => Tenant = tenant;
    }
}
