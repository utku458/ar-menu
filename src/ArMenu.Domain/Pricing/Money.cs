using System.Globalization;
using ArMenu.Domain.Common;

namespace ArMenu.Domain.Pricing;

/// <summary>A non-negative monetary amount in a specific currency.</summary>
public sealed record Money
{
    public const int Precision = 12;
    public const int Scale = 2;

    /// <summary>Largest amount representable by the <c>numeric(12, 2)</c> column the value is stored in.</summary>
    public const decimal MaxAmount = 9_999_999_999.99m;

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

#pragma warning disable CS8618 // Used by EF Core; every property is populated during materialization.
    private Money()
    {
    }
#pragma warning restore CS8618

    public decimal Amount { get; private init; }

    public Currency Currency { get; private init; }

    public static Result<Money> Create(decimal amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        if (amount < 0)
        {
            return PricingErrors.AmountNegative;
        }

        if (amount > MaxAmount)
        {
            return PricingErrors.AmountTooLarge;
        }

        // Reject instead of rounding: silently changing a price is worse than refusing it.
        return decimal.Round(amount, Scale) == amount
            ? new Money(amount, currency)
            : PricingErrors.AmountPrecisionExceeded;
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency}");
}
