using ArMenu.Domain.Media;

namespace ArMenu.Domain.Tenants;

/// <summary>
/// How a business's guest menu looks: its logo and the colour the menu is painted with. Both are optional; a menu
/// without either is shown in the platform's own neutral style.
/// </summary>
/// <remarks>
/// The logo is a published asset of this business, referenced by storage key rather than by URL, so the CDN host can
/// change without touching the data.
/// </remarks>
public sealed record TenantBranding(AssetPath? LogoPath, BrandColor? AccentColor)
{
    /// <summary>A business that has not styled its menu.</summary>
    public static TenantBranding None { get; } = new(null, null);

    public bool IsEmpty => LogoPath is null && AccentColor is null;
}
