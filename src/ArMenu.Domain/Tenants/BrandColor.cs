using System.Globalization;
using ArMenu.Domain.Common;

namespace ArMenu.Domain.Tenants;

/// <summary>
/// The one colour a business paints its guest menu with, as <c>#rrggbb</c>.
/// </summary>
/// <remarks>
/// Only an opaque sRGB colour is accepted: no transparency, no CSS colour function and no named colour. The value is
/// interpolated into a page, so anything that is not six hexadecimal digits could carry markup or a CSS expression.
/// </remarks>
public sealed record BrandColor
{
    public const int Length = 7;

    /// <summary>Text drawn on a colour dark enough to carry it.</summary>
    public const string LightForeground = "#ffffff";

    /// <summary>
    /// Text drawn on a light colour. Pure black rather than the softer near-black the rest of the menu writes in: only
    /// the two extremes are far enough apart that <em>every</em> colour a business could pick keeps its text legible.
    /// A mid grey, the hardest case, reaches 4.5:1 with neither white nor a near-black.
    /// </summary>
    public const string DarkForeground = "#000000";

    private BrandColor(string value) => Value = value;

    /// <summary>Lower-case <c>#rrggbb</c>.</summary>
    public string Value { get; }

    /// <summary>
    /// The text colour to draw on this one: whichever of <see cref="LightForeground"/> and <see cref="DarkForeground"/>
    /// contrasts more with it. Decided here so the dashboard's preview and the guest menu can never disagree.
    /// </summary>
    public string Foreground =>
        ContrastWith(LightForeground) >= ContrastWith(DarkForeground) ? LightForeground : DarkForeground;

    public static Result<BrandColor> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TenantErrors.BrandColorRequired;
        }

        var trimmed = value.Trim();
        if (trimmed.Length != Length || trimmed[0] != '#')
        {
            return TenantErrors.BrandColorInvalid;
        }

        var digits = trimmed.AsSpan(1);
        foreach (var digit in digits)
        {
            if (!char.IsAsciiHexDigit(digit))
            {
                return TenantErrors.BrandColorInvalid;
            }
        }

        return new BrandColor(string.Concat("#", digits.ToString().ToLowerInvariant()));
    }

    /// <summary>The WCAG contrast ratio between this colour and <paramref name="other"/>, from 1 to 21.</summary>
    public double ContrastWith(string other)
    {
        var mine = RelativeLuminance(Value);
        var theirs = RelativeLuminance(other);
        var (lighter, darker) = mine >= theirs ? (mine, theirs) : (theirs, mine);
        return (lighter + 0.05) / (darker + 0.05);
    }

    public override string ToString() => Value;

    // WCAG 2.2, relative luminance of an sRGB colour.
    private static double RelativeLuminance(string hex)
    {
        var red = Channel(hex, 1);
        var green = Channel(hex, 3);
        var blue = Channel(hex, 5);
        return (0.2126 * red) + (0.7152 * green) + (0.0722 * blue);
    }

    private static double Channel(string hex, int offset)
    {
        var value = byte.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d;
        return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
