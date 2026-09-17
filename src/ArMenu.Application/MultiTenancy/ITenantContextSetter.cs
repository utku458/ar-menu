namespace ArMenu.Application.MultiTenancy;

/// <summary>
/// Binds the current scope to a tenant. Reserved for tenant resolution (HTTP middleware), tenant provisioning
/// (sign-up binds the scope to the tenant it creates) and job runners.
/// </summary>
public interface ITenantContextSetter
{
    /// <summary>
    /// Binds the scope to <paramref name="tenant"/>. A scope can never be re-bound to a <em>different</em> tenant.
    /// </summary>
    /// <exception cref="TenantIsolationViolationException">The scope is already bound to another tenant.</exception>
    void SetTenant(TenantInfo tenant);
}
