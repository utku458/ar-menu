using System.Globalization;
using ArMenu.Application.MultiTenancy;

namespace ArMenu.Application.Menus.Transfer;

/// <summary>
/// The columns of a menu spreadsheet. Headers are stable, English and lower case so a file keeps working whatever the
/// interface language; texts get one column per language (<c>name:tr</c>, <c>description:en</c>).
/// </summary>
internal static class MenuCsvColumns
{
    public const string Id = "id";
    public const string Category = "category";
    public const string Price = "price";
    public const string Visible = "visible";
    public const string Available = "available";
    public const string Allergens = "allergens";
    public const string Dietary = "dietary";

    /// <summary>Written in the allergens column for a dish declared to contain none; an empty cell means "not declared".</summary>
    public const string NoAllergens = "none";
    public const string NamePrefix = "name:";
    public const string DescriptionPrefix = "description:";

    public static IReadOnlyList<string> For(TenantInfo tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        var cultures = Cultures(tenant);
        return [Id, Category, .. cultures.Select(culture => NamePrefix + culture), .. cultures.Select(culture => DescriptionPrefix + culture), Price, Visible, Available, Allergens, Dietary];
    }

    /// <summary>The default language first, then the others as the business lists them.</summary>
    public static IReadOnlyList<string> Cultures(TenantInfo tenant) =>
        [tenant.DefaultCulture, .. tenant.SupportedCultures.Where(culture => culture != tenant.DefaultCulture)];

    public static string FormatPrice(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>
    /// A price as people type it: <c>185</c>, <c>185.50</c> or <c>185,50</c>. Thousands separators are refused rather than
    /// guessed, since <c>1.250</c> means a thousand in Turkish and one and a quarter in English: prices have at most two
    /// decimals, so three digits after the separator are never read as a price.
    /// </summary>
    public static decimal? ParsePrice(string value)
    {
        var text = value.Trim().Replace(',', '.');
        var separator = text.IndexOf('.', StringComparison.Ordinal);
        return text.Count(character => character == '.') <= 1 &&
               (separator < 0 || text.Length - separator - 1 is > 0 and <= 2) &&
               text.Length > 0 &&
               text.All(character => char.IsAsciiDigit(character) || character == '.') &&
               decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
    }

    /// <summary>Codes in one cell, as <c>gluten, milk</c>.</summary>
    public static string FormatCodes(IEnumerable<string> codes) => string.Join(", ", codes);

    /// <summary>
    /// Codes typed into one cell. Commas, semicolons, slashes and spaces all separate them, so a cell reads the same
    /// whichever separator the spreadsheet itself uses.
    /// </summary>
    public static IReadOnlyList<string> ParseCodes(string value) =>
        value.Split([',', ';', '/', '|', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static string FormatFlag(bool value) => value ? "yes" : "no";

    /// <summary>yes/no, true/false, 1/0 and the Turkish evet/hayır.</summary>
    public static bool? ParseFlag(string value) => value.Trim().ToLowerInvariant() switch
    {
        "yes" or "true" or "1" or "evet" => true,
        "no" or "false" or "0" or "hayır" or "hayir" => false,
        _ => null,
    };
}
