using ArMenu.Domain.Common;

namespace ArMenu.Domain.Pricing;

public static class PricingErrors
{
    public static readonly Error CurrencyRequired = Error.Validation(
        "currency.required", "A currency is required.");

    public static readonly Error CurrencyInvalid = Error.Validation(
        "currency.invalid", "Currencies must be three-letter ISO 4217 codes such as 'TRY' or 'EUR'.");

    public static readonly Error AmountNegative = Error.Validation(
        "money.amount_negative", "Amounts cannot be negative.");

    public static readonly Error AmountTooLarge = Error.Validation(
        "money.amount_too_large", FormattableString.Invariant($"Amounts cannot exceed {Money.MaxAmount}."));

    public static readonly Error AmountPrecisionExceeded = Error.Validation(
        "money.amount_precision_exceeded", FormattableString.Invariant($"Amounts can have at most {Money.Scale} decimal places."));
}
