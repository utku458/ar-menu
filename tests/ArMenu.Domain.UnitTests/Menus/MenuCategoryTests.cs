using ArMenu.Domain.Menus;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.UnitTests.TestSupport;

namespace ArMenu.Domain.UnitTests.Menus;

public sealed class MenuCategoryTests
{
    [Fact]
    public void Create_produces_a_visible_category_of_the_given_tenant()
    {
        var tenantId = TenantId.New();

        var category = MenuCategory.Create(tenantId, Make.Text(("tr", "Mezeler"), ("en", "Starters")), displayOrder: 0)
            .ShouldSucceed();

        category.TenantId.ShouldBe(tenantId);
        category.IsVisible.ShouldBeTrue();
        category.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Create_rejects_a_name_that_is_too_long_in_any_language()
    {
        var name = Make.Text(("tr", new string('x', MenuCategory.NameMaxLength + 1)));

        MenuCategory.Create(TenantId.New(), name, 0).ShouldFailWith(MenuCategoryErrors.NameTooLong);
    }

    [Fact]
    public void ChangeDisplayOrder_rejects_negative_positions()
    {
        var category = MenuCategory.Create(TenantId.New(), Make.Text(("tr", "Mezeler")), 2).ShouldSucceed();

        category.ChangeDisplayOrder(-1).ShouldFailWith(MenuCategoryErrors.DisplayOrderNegative);
        category.DisplayOrder.ShouldBe(2);
    }

    [Fact]
    public void Hidden_categories_can_be_shown_again()
    {
        var category = MenuCategory.Create(TenantId.New(), Make.Text(("tr", "Mezeler")), 0).ShouldSucceed();

        category.Hide();
        category.IsVisible.ShouldBeFalse();

        category.Show();
        category.IsVisible.ShouldBeTrue();
    }
}
