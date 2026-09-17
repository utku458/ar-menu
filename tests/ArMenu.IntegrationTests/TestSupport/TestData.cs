using ArMenu.Domain.Localization;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Pricing;
using ArMenu.Domain.Tenants;

namespace ArMenu.IntegrationTests.TestSupport;

internal static class TestData
{
    /// <summary>Every test creates its own tenants (unique slugs), so tests stay independent and can run in parallel.</summary>
    public static Tenant NewTenant() =>
        Tenant.Create(
            "Test Restaurant",
            TenantSlug.Create($"test-{Guid.NewGuid():N}").Value,
            CultureCode.Create("tr").Value,
            Currency.Create("TRY").Value).Value;

    public static MenuCategory NewCategory(TenantId tenantId) =>
        MenuCategory.Create(tenantId, Text("Mezeler"), displayOrder: 0).Value;

    public static MenuItem NewItem(TenantId tenantId, MenuCategoryId categoryId) =>
        MenuItem.Create(tenantId, categoryId, Text("Humus"), Money.Create(180m, Currency.Create("TRY").Value).Value, displayOrder: 0).Value;

    public static LocalizedText Text(string turkish) => LocalizedText.Create(CultureCode.Create("tr").Value, turkish).Value;
}
