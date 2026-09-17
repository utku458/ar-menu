using ArMenu.Application.Menus.Items.CreateMenuItem;
using ArMenu.Application.UnitTests.TestSupport;
using ArMenu.Domain.Menus;

namespace ArMenu.Application.UnitTests.Menus;

public sealed class MenuTextValidationTests
{
    private readonly CreateMenuItemCommandValidator _validator = new(TestTenantContext.TurkishRestaurant());

    [Fact]
    public async Task Translations_in_supported_languages_including_the_default_are_valid()
    {
        var command = Command(name: new() { ["tr"] = "Izgara Levrek", ["de"] = "Gegrillter Wolfsbarsch" });

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task The_default_language_translation_is_required()
    {
        var command = Command(name: new() { ["en"] = "Grilled Sea Bass" });

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("menu.default_culture_translation_missing");
    }

    [Fact]
    public async Task Languages_the_tenant_does_not_offer_are_rejected()
    {
        var command = Command(name: new() { ["tr"] = "Izgara Levrek", ["fr"] = "Bar grillé" });

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("menu.culture_not_supported");
    }

    [Fact]
    public async Task Tenant_language_rules_and_field_rules_are_reported_together()
    {
        var command = Command(name: new() { ["en"] = "Grilled Sea Bass" }, price: -5.555m);

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.Errors.Select(error => error.ErrorCode).ShouldBe(
            ["menu.default_culture_translation_missing", "money.amount_negative"],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Names_longer_than_the_limit_in_any_language_are_rejected()
    {
        var command = Command(name: new() { ["tr"] = "Levrek", ["en"] = new string('x', MenuItem.NameMaxLength + 1) });

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe(MenuItemErrors.NameTooLong.Code);
    }

    [Fact]
    public async Task An_empty_description_means_no_description()
    {
        var command = Command(name: new() { ["tr"] = "Levrek" }) with { Description = new Dictionary<string, string>() };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
    }

    private static CreateMenuItemCommand Command(Dictionary<string, string> name, decimal price = 750m) =>
        new(MenuCategoryId.New(), name, Description: null, price);
}
