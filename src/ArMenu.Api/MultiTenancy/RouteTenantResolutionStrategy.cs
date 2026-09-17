using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Tenants;

namespace ArMenu.Api.MultiTenancy;

/// <summary>
/// Resolves the tenant from a route value holding its slug, e.g. <c>/api/v1/menus/{tenant}</c>.
/// Meant for anonymous, customer-facing endpoints reached by scanning a QR code.
/// </summary>
internal sealed class RouteTenantResolutionStrategy(string routeParameterName) : ITenantResolutionStrategy
{
    public string RouteParameterName { get; } = routeParameterName;

    public ValueTask<TenantInfo?> ResolveAsync(HttpContext httpContext, ITenantLookup tenantLookup)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(tenantLookup);

        // A malformed slug cannot belong to any tenant. Answering without touching the cache or the database keeps
        // random probing from filling the cache with junk keys.
        if (httpContext.GetRouteValue(RouteParameterName) is not string value ||
            TenantSlug.Create(value) is not { IsSuccess: true } slug)
        {
            return ValueTask.FromResult<TenantInfo?>(null);
        }

        return tenantLookup.FindBySlugAsync(slug.Value, httpContext.RequestAborted);
    }
}
