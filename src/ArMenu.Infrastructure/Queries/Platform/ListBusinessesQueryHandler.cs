using ArMenu.Application.Platform.Queries.ListBusinesses;
using ArMenu.Domain.Common;
using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Queries.Platform;

/// <remarks>
/// Reads only the tenants table, which has no row-level security because it holds no business's data — just the
/// directory of businesses. Nothing tenant-scoped is touched, so no isolation is being stepped around here.
/// </remarks>
public sealed class ListBusinessesQueryHandler(ArMenuDbContext dbContext)
    : IQueryHandler<ListBusinessesQuery, Result<IReadOnlyList<BusinessSummary>>>
{
    public async ValueTask<Result<IReadOnlyList<BusinessSummary>>> Handle(ListBusinessesQuery query, CancellationToken cancellationToken)
    {
        var platform = TenantSlug.Platform;

        var businesses = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Slug != platform)
            .OrderBy(tenant => tenant.Name)
            .Select(tenant => new { tenant.Id, tenant.Name, tenant.Slug, tenant.Status, tenant.CreatedAt })
            .ToListAsync(cancellationToken);

        return businesses
            .Select(tenant => new BusinessSummary(tenant.Id.Value, tenant.Name, tenant.Slug.Value, tenant.Status.ToString(), tenant.CreatedAt))
            .ToList();
    }
}
