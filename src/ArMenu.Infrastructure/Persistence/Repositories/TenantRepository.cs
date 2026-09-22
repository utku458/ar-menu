using ArMenu.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Persistence.Repositories;

internal sealed class TenantRepository(ArMenuDbContext dbContext) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken = default) =>
        dbContext.Tenants.SingleOrDefaultAsync(tenant => tenant.Id == id, cancellationToken);

    public Task<bool> SlugExistsAsync(TenantSlug slug, CancellationToken cancellationToken = default) =>
        dbContext.Tenants.AnyAsync(tenant => tenant.Slug == slug, cancellationToken);

    public Task<Tenant?> FindBySlugAsync(TenantSlug slug, CancellationToken cancellationToken = default) =>
        dbContext.Tenants.SingleOrDefaultAsync(tenant => tenant.Slug == slug, cancellationToken);

    public void Add(Tenant tenant) => dbContext.Tenants.Add(tenant);
}
