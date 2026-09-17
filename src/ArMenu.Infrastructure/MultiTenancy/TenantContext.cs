using ArMenu.Application.MultiTenancy;

namespace ArMenu.Infrastructure.MultiTenancy;

/// <summary>Scoped holder of the tenant bound to the current request or job.</summary>
internal sealed class TenantContext : ITenantContext, ITenantContextSetter
{
    public TenantInfo? Tenant { get; private set; }

    public void SetTenant(TenantInfo tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (Tenant is not null && Tenant.Id != tenant.Id)
        {
            throw new TenantIsolationViolationException(
                "The current scope is already bound to another tenant. A scope can never switch tenants.");
        }

        Tenant = tenant;
    }
}
