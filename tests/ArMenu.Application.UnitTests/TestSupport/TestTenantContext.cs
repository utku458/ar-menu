using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Tenants;

namespace ArMenu.Application.UnitTests.TestSupport;

internal sealed class TestTenantContext(TenantInfo? tenant) : ITenantContext
{
    public TenantInfo? Tenant { get; } = tenant;

    /// <summary>A Turkish-default tenant that also offers English and German.</summary>
    public static TestTenantContext TurkishRestaurant() =>
        new(new TenantInfo(TenantId.New(), "bogazici-balikcisi", "Boğaziçi Balıkçısı", TenantStatus.Active, "tr", ["tr", "en", "de"], "TRY", "Europe/Istanbul"));
}
