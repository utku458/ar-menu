using ArMenu.Application.Menus;
using ArMenu.Application.UnitTests.TestSupport;

namespace ArMenu.Application.UnitTests.Menus;

public sealed class MenuCultureSelectorTests
{
    private readonly TestTenantContext _tenant = TestTenantContext.TurkishRestaurant();

    [Theory]
    [InlineData(new[] { "de" }, "de")]
    [InlineData(new[] { "de-AT", "en" }, "de")]
    [InlineData(new[] { "fr", "en-GB" }, "en")]
    [InlineData(new[] { "fr", "ja" }, "tr")]
    [InlineData(new[] { "not a culture", "en" }, "en")]
    [InlineData(new string[0], "tr")]
    public void Selects_the_first_supported_preference_or_the_default(string[] preferences, string expected)
    {
        MenuCultureSelector.Select(preferences, _tenant.Tenant!).Value.ShouldBe(expected);
    }
}
