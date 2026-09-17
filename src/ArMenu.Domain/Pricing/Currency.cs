using System.Text.RegularExpressions;
using ArMenu.Domain.Common;

namespace ArMenu.Domain.Pricing;

/// <summary>An ISO 4217 alphabetic currency code, normalized to upper case (e.g. <c>TRY</c>, <c>EUR</c>).</summary>
public sealed partial record Currency
{
    public const int Length = 3;

    private Currency(string code) => Code = code;

    public string Code { get; }

    public static Result<Currency> Create(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return PricingErrors.CurrencyRequired;
        }

        var normalized = code.Trim().ToUpperInvariant();

        return Pattern().IsMatch(normalized)
            ? new Currency(normalized)
            : PricingErrors.CurrencyInvalid;
    }

    public override string ToString() => Code;

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
