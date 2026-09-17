using ArMenu.Domain.Tenants;

namespace ArMenu.Domain.Common;

/// <summary>
/// Marks an entity that belongs to exactly one tenant.
/// Persistence guarantees such rows are never readable or writable from the scope of another tenant.
/// </summary>
public interface ITenantScoped
{
    TenantId TenantId { get; }
}
