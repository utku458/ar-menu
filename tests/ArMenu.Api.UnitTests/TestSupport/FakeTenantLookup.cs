using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Tenants;

namespace ArMenu.Api.UnitTests.TestSupport;

internal sealed class FakeTenantLookup(params TenantInfo[] tenants) : ITenantLookup
{
    public int CallCount { get; private set; }

    public ValueTask<TenantInfo?> FindByIdAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return ValueTask.FromResult(tenants.SingleOrDefault(tenant => tenant.Id == tenantId));
    }

    public ValueTask<TenantInfo?> FindBySlugAsync(TenantSlug slug, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return ValueTask.FromResult(tenants.SingleOrDefault(tenant => tenant.Slug == slug.Value));
    }

    public ValueTask InvalidateAsync(TenantId tenantId, TenantSlug slug, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}
