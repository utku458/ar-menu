using ArMenu.Domain.Menus;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.UnitTests.TestSupport;

namespace ArMenu.Domain.UnitTests.Menus;

public sealed class MenuItemTests
{
    [Fact]
    public void Create_produces_a_visible_available_item_without_ar_model()
    {
        var tenantId = TenantId.New();
        var categoryId = MenuCategoryId.New();

        var item = MenuItem.Create(tenantId, categoryId, Make.Text(("tr", "Lahmacun")), Make.Price(120m), displayOrder: 3)
            .ShouldSucceed();

        item.TenantId.ShouldBe(tenantId);
        item.CategoryId.ShouldBe(categoryId);
        item.DisplayOrder.ShouldBe(3);
        item.IsVisible.ShouldBeTrue();
        item.IsAvailable.ShouldBeTrue();
        item.ArModel.ShouldBeNull();
    }

    [Fact]
    public void Create_without_a_tenant_is_a_programming_error()
    {
        Should.Throw<ArgumentException>(() =>
            MenuItem.Create(default, MenuCategoryId.New(), Make.Text(("tr", "Lahmacun")), Make.Price(120m), 0));
    }

    [Fact]
    public void Create_rejects_a_name_that_is_too_long_in_any_language()
    {
        var name = Make.Text(("tr", "Lahmacun"), ("en", new string('x', MenuItem.NameMaxLength + 1)));

        MenuItem.Create(TenantId.New(), MenuCategoryId.New(), name, Make.Price(120m), 0)
            .ShouldFailWith(MenuItemErrors.NameTooLong);
    }

    [Fact]
    public void Create_rejects_a_description_that_is_too_long_in_any_language()
    {
        var description = Make.Text(("tr", new string('x', MenuItem.DescriptionMaxLength + 1)));

        MenuItem.Create(TenantId.New(), MenuCategoryId.New(), Make.Text(("tr", "Lahmacun")), Make.Price(120m), 0, description)
            .ShouldFailWith(MenuItemErrors.DescriptionTooLong);
    }

    [Fact]
    public void Create_rejects_negative_display_order()
    {
        MenuItem.Create(TenantId.New(), MenuCategoryId.New(), Make.Text(("tr", "Lahmacun")), Make.Price(120m), -1)
            .ShouldFailWith(MenuItemErrors.DisplayOrderNegative);
    }

    [Fact]
    public void Failed_rename_leaves_the_item_unchanged()
    {
        var item = Make.NewMenuItem();
        var originalName = item.Name;

        item.Rename(Make.Text(("tr", new string('x', MenuItem.NameMaxLength + 1)))).ShouldFailWith(MenuItemErrors.NameTooLong);

        item.Name.ShouldBe(originalName);
    }

    [Fact]
    public void Sold_out_items_stay_on_the_menu()
    {
        var item = Make.NewMenuItem();

        item.MarkAsSoldOut();

        item.IsAvailable.ShouldBeFalse();
        item.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public void Ar_model_can_be_attached_and_detached()
    {
        var item = Make.NewMenuItem();
        var model = ArModel.Create(Make.Asset("models/adana.glb")).ShouldSucceed();

        item.AttachArModel(model);
        item.ArModel.ShouldBe(model);

        item.DetachArModel();
        item.ArModel.ShouldBeNull();
    }
}
