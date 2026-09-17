using ArMenu.Domain.Tenants;

namespace ArMenu.Application.MultiTenancy;

/// <summary>
/// Finds tenants for resolution purposes. It sits on the hot path of every tenant-bound request,
/// so implementations are expected to cache results.
/// </summary>
public interface ITenantLookup
{
    ValueTask<TenantInfo?> FindByIdAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    ValueTask<TenantInfo?> FindBySlugAsync(TenantSlug slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards cached lookups of a tenant after it changed, including a cached "not found" for a slug that was
    /// probed before the tenant signed up.
    /// </summary>
    ValueTask InvalidateAsync(TenantId tenantId, TenantSlug slug, CancellationToken cancellationToken = default);
}
