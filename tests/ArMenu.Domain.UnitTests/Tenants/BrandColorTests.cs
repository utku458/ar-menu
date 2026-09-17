using ArMenu.Domain.Tenants;

namespace ArMenu.Domain.UnitTests.Tenants;

public sealed class BrandColorTests
{
    [Theory]
    [InlineData("#B8442F", "#b8442f")]
    [InlineData("  #1f6f5c  ", "#1f6f5c")]
    public void A_colour_is_stored_in_one_spelling(string written, string stored) =>
        BrandColor.Create(written).Value.Value.ShouldBe(stored);

    [Theory]
    [InlineData("b8442f")]           // Without the hash it could be read as a name.
    [InlineData("#b84")]             // Three digits are a CSS shorthand this does not accept.
    [InlineData("#b8442f80")]        // Transparency would let a colour disappear on the menu.
    [InlineData("rebeccapurple")]
    [InlineData("var(--x)")]
    [InlineData("#b8442g")]
    [InlineData("")]
    public void Anything_that_is_not_six_hexadecimal_digits_is_refused(string written) =>
        BrandColor.Create(written).IsFailure.ShouldBeTrue();

    [Theory]
    [InlineData("#000000", BrandColor.LightForeground)]
    [InlineData("#b8442f", BrandColor.LightForeground)]
    [InlineData("#ffffff", BrandColor.DarkForeground)]
    [InlineData("#f5c518", BrandColor.DarkForeground)] // A bright yellow carries dark text, not white.
    public void Text_on_a_colour_is_whichever_of_the_two_contrasts_more(string color, string foreground) =>
        BrandColor.Create(color).Value.Foreground.ShouldBe(foreground);

    [Theory]
    [InlineData("#000000")]
    [InlineData("#ffffff")]
    [InlineData("#f5c518")]
    [InlineData("#1f6f5c")]
    [InlineData("#808080")] // The hardest case: mid grey is the furthest from both black and white.
    public void Text_on_a_colour_is_always_legible(string color)
    {
        var brand = BrandColor.Create(color).Value;

        // WCAG 2.2 asks 4.5:1 of body text; the pairing is chosen for the business, so it must hold for any colour.
        brand.ContrastWith(brand.Foreground).ShouldBeGreaterThanOrEqualTo(4.5);
    }

    [Fact]
    public void The_contrast_of_a_colour_with_itself_is_one() =>
        BrandColor.Create("#1f6f5c").Value.ContrastWith("#1f6f5c").ShouldBe(1, 0.0001);

    [Fact]
    public void Black_and_white_are_as_far_apart_as_colours_get() =>
        BrandColor.Create("#000000").Value.ContrastWith("#ffffff").ShouldBe(21, 0.0001);
}
