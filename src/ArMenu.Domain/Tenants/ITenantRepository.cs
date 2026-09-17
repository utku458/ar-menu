namespace ArMenu.Domain.Tenants;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(TenantSlug slug, CancellationToken cancellationToken = default);

    void Add(Tenant tenant);
}
