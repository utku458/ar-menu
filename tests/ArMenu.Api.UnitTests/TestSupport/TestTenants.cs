using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Tenants;

namespace ArMenu.Api.UnitTests.TestSupport;

internal static class TestTenants
{
    public static TenantInfo Create(string slug, TenantStatus status = TenantStatus.Active) =>
        new(TenantId.New(), slug, $"Tenant {slug}", status, "tr", ["tr"], "TRY", "Europe/Istanbul");
}
