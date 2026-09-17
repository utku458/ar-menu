using ArMenu.Domain.Menus;

namespace ArMenu.Domain.UnitTests.Menus;

public sealed class DietaryInformationTests
{
    [Fact]
    public void Not_declaring_allergens_is_not_the_same_as_declaring_none()
    {
        var unknown = DietaryInformation.FromCodes(null, []).Value;
        var none = DietaryInformation.FromCodes([], []).Value;

        unknown.Allergens.ShouldBeNull();
        none.Allergens.ShouldBeEmpty();
        unknown.ShouldNotBe(none);
    }

    [Fact]
    public void A_vegan_dish_is_vegetarian_too() =>
        DietaryInformation.FromCodes([], ["vegan"]).Value.LabelCodesOf().ShouldBe(["vegetarian", "vegan"]);

    [Fact]
    public void Codes_are_kept_once_in_a_fixed_order_whatever_order_they_were_given_in() =>
        DietaryInformation.FromCodes(["milk", "gluten", "MILK"], []).Value.AllergenCodesOf().ShouldBe(["gluten", "milk"]);

    [Theory]
    [InlineData("vegan", "milk")]
    [InlineData("vegan", "eggs")]
    [InlineData("vegetarian", "fish")]
    [InlineData("vegetarian", "molluscs")]
    [InlineData("glutenFree", "gluten")]
    public void A_label_cannot_promise_what_the_allergens_break(string label, string allergen)
    {
        var result = DietaryInformation.FromCodes([allergen], [label]);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("menu_item.dietary_label_contradicts_allergen");
    }

    [Fact]
    public void Labels_are_accepted_when_allergens_are_not_declared() =>
        DietaryInformation.FromCodes(null, ["glutenFree"]).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData("peanut")]
    [InlineData("lactose")]
    [InlineData("")]
    public void Only_the_fourteen_allergens_are_known(string code) =>
        DietaryInformation.FromCodes([code], []).Error.Code.ShouldBe("menu_item.allergen_unknown");

    [Fact]
    public void Every_allergen_and_label_has_a_code_that_reads_back()
    {
        foreach (var allergen in Enum.GetValues<Allergen>())
        {
            DietaryInformation.ParseAllergen(DietaryInformation.CodeOf(allergen)).Value.ShouldBe(allergen);
        }

        foreach (var label in Enum.GetValues<DietaryLabel>())
        {
            DietaryInformation.ParseLabel(DietaryInformation.CodeOf(label)).Value.ShouldBe(label);
        }

        DietaryInformation.AllAllergenCodes.Count.ShouldBe(14);
    }
}
