using ArMenu.Domain.Tenants;

namespace ArMenu.Application.MultiTenancy;

/// <summary>
/// Read-only view of the tenant the current scope (an HTTP request, a background job) is bound to.
/// Application code depends on this interface only; binding a scope is reserved for <see cref="ITenantContextSetter"/>.
/// </summary>
public interface ITenantContext
{
    /// <summary>The tenant the scope is bound to, or <see langword="null"/> when the scope is not tenant-bound.</summary>
    TenantInfo? Tenant { get; }

    /// <summary>Identifier of the tenant the scope is bound to.</summary>
    /// <exception cref="TenantNotResolvedException">The scope is not bound to a tenant.</exception>
    TenantId TenantId => RequireTenant().Id;

    /// <summary>The tenant the scope is bound to.</summary>
    /// <exception cref="TenantNotResolvedException">The scope is not bound to a tenant.</exception>
    TenantInfo RequireTenant() => Tenant ?? throw new TenantNotResolvedException();
}
