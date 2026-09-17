using ArMenu.Domain.Localization;
using ArMenu.Domain.UnitTests.TestSupport;

namespace ArMenu.Domain.UnitTests.Localization;

public sealed class CultureCodeTests
{
    [Theory]
    [InlineData("tr", "tr")]
    [InlineData("EN", "en")]
    [InlineData(" pt-BR ", "pt-br")]
    [InlineData("zh-Hant", "zh-hant")]
    [InlineData("zh-Hant-TW", "zh-hant-tw")]
    [InlineData("es-419", "es-419")]
    public void Create_accepts_language_script_and_region_tags_and_normalizes_them(string input, string expected)
    {
        CultureCode.Create(input).ShouldSucceed().Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("e")]
    [InlineData("english")]
    [InlineData("en_US")]
    [InlineData("en-")]
    [InlineData("en-u")]
    [InlineData("123")]
    [InlineData("tr-TR-x-private")]
    public void Create_rejects_malformed_tags(string input)
    {
        CultureCode.Create(input).ShouldFailWith(LocalizationErrors.CultureCodeInvalid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_requires_a_value(string? input)
    {
        CultureCode.Create(input).ShouldFailWith(LocalizationErrors.CultureCodeRequired);
    }

    [Fact]
    public void Parent_of_a_regional_culture_is_its_neutral_language()
    {
        Make.Culture("de-AT").Parent.ShouldBe(Make.Culture("de"));
        Make.Culture("de").Parent.ShouldBeNull();
    }
}
