using ArMenu.Domain.Localization;
using ArMenu.Domain.UnitTests.TestSupport;

namespace ArMenu.Domain.UnitTests.Localization;

public sealed class LocalizedTextTests
{
    [Fact]
    public void Create_requires_at_least_one_translation()
    {
        LocalizedText.Create([]).ShouldFailWith(LocalizationErrors.TextRequired);
    }

    [Fact]
    public void Create_rejects_blank_translations()
    {
        LocalizedText.Create([KeyValuePair.Create("tr", "  ")]).ShouldFailWith(LocalizationErrors.TranslationEmpty);
    }

    [Fact]
    public void Create_rejects_cultures_that_collide_after_normalization()
    {
        LocalizedText.Create([KeyValuePair.Create("EN", "Kebab"), KeyValuePair.Create("en", "Kebap")])
            .ShouldFailWith(LocalizationErrors.TranslationDuplicated);
    }

    [Fact]
    public void Create_normalizes_cultures_and_trims_translations()
    {
        var text = Make.Text(("TR", "  Adana Kebap "));

        text.Translations.ShouldBe(new Dictionary<string, string> { ["tr"] = "Adana Kebap" });
    }

    [Theory]
    [InlineData("de", "Gegrillter Wolfsbarsch")] // exact match
    [InlineData("de-AT", "Gegrillter Wolfsbarsch")] // neutral parent of a regional culture
    [InlineData("fr", "Izgara Levrek")] // tenant default culture
    public void Resolve_picks_the_best_available_translation(string requested, string expected)
    {
        var text = Make.Text(("tr", "Izgara Levrek"), ("en", "Grilled Sea Bass"), ("de", "Gegrillter Wolfsbarsch"));

        text.Resolve(Make.Culture(requested), fallback: Make.Culture("tr")).ShouldBe(expected);
    }

    [Fact]
    public void Resolve_falls_back_to_any_translation_rather_than_returning_nothing()
    {
        var text = Make.Text(("en", "Grilled Sea Bass"));

        text.Resolve(Make.Culture("ar"), fallback: Make.Culture("tr")).ShouldBe("Grilled Sea Bass");
    }

    [Fact]
    public void Equality_is_structural_and_ignores_insertion_order()
    {
        var first = Make.Text(("tr", "Humus"), ("en", "Hummus"));
        var second = Make.Text(("en", "Hummus"), ("tr", "Humus"));

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
        first.ShouldNotBe(Make.Text(("tr", "Humus")));
    }

    [Fact]
    public void ExceedsLength_checks_every_translation()
    {
        var text = Make.Text(("tr", "Çay"), ("en", "Turkish black tea"));

        text.ExceedsLength(10).ShouldBeTrue();
        text.ExceedsLength(17).ShouldBeFalse();
    }
}
