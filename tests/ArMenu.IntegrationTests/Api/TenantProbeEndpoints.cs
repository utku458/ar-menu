using ArMenu.Api.Endpoints;
using ArMenu.Api.MultiTenancy;
using ArMenu.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.IntegrationTests.Api;

/// <summary>
/// Minimal tenant-bound endpoint plugged into the real pipeline, so tests observe what an actual feature endpoint
/// would see after tenant resolution: nothing but the requested tenant's data.
/// </summary>
internal sealed class TenantProbeEndpoints : IEndpointModule
{
    public const string CategoriesRoute = "/test/tenants/{tenant}/categories";

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints
            .MapGet(CategoriesRoute, async (ArMenuDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var categoryIds = await dbContext.MenuCategories.Select(category => category.Id).ToListAsync(cancellationToken);
                return categoryIds.Select(id => id.Value).ToArray();
            })
            .RequireTenantFromRoute()
            .AllowAnonymous()
            .ExcludeFromDescription();
}
