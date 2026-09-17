using ArMenu.Domain.Pricing;
using ArMenu.Domain.UnitTests.TestSupport;

namespace ArMenu.Domain.UnitTests.Pricing;

public sealed class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(385)]
    [InlineData(12.5)]
    [InlineData(99.99)]
    public void Create_accepts_non_negative_amounts_with_at_most_two_decimals(decimal amount)
    {
        Money.Create(amount, Make.CurrencyCode()).ShouldSucceed().Amount.ShouldBe(amount);
    }

    [Fact]
    public void Create_treats_trailing_zeros_as_the_same_precision()
    {
        Money.Create(12.500m, Make.CurrencyCode()).ShouldSucceed();
    }

    [Fact]
    public void Create_rejects_negative_amounts()
    {
        Money.Create(-0.01m, Make.CurrencyCode()).ShouldFailWith(PricingErrors.AmountNegative);
    }

    [Fact]
    public void Create_rejects_amounts_the_database_column_cannot_hold()
    {
        Money.Create(Money.MaxAmount + 1, Make.CurrencyCode()).ShouldFailWith(PricingErrors.AmountTooLarge);
    }

    [Fact]
    public void Create_refuses_to_silently_round_prices()
    {
        Money.Create(9.999m, Make.CurrencyCode()).ShouldFailWith(PricingErrors.AmountPrecisionExceeded);
    }

    [Fact]
    public void Equality_is_by_amount_and_currency()
    {
        Make.Price(10m).ShouldBe(Make.Price(10.00m));
        Make.Price(10m, "TRY").ShouldNotBe(Make.Price(10m, "EUR"));
    }

    [Theory]
    [InlineData("try", "TRY")]
    [InlineData(" eur ", "EUR")]
    public void Currency_is_normalized_to_upper_case(string input, string expected)
    {
        Currency.Create(input).ShouldSucceed().Code.ShouldBe(expected);
    }

    [Theory]
    [InlineData("TL")]
    [InlineData("EURO")]
    [InlineData("€")]
    [InlineData("T1Y")]
    public void Currency_must_be_a_three_letter_iso_code(string input)
    {
        Currency.Create(input).ShouldFailWith(PricingErrors.CurrencyInvalid);
    }
}
