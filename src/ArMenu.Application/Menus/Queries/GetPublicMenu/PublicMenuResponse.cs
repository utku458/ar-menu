using System.ComponentModel;

namespace ArMenu.Application.Menus.Queries.GetPublicMenu;

// Immutable read models: HybridCache can hand the same instance to every guest instead of copying it per request.

[ImmutableObject(true)]
public sealed record PublicMenuResponse(
    PublicTenantResponse Tenant,
    string Culture,
    IReadOnlyList<PublicMenuCategoryResponse> Categories);

/// <summary>
/// The business the menu belongs to. <c>LogoUrl</c> and <c>AccentColor</c> (<c>#rrggbb</c>) are <see langword="null"/>
/// when the business has not styled its menu; <c>OnAccentColor</c> is the text colour to draw on <c>AccentColor</c>,
/// decided here so every client reads the same, legible pairing.
/// </summary>
[ImmutableObject(true)]
public sealed record PublicTenantResponse(
    string Name,
    string Slug,
    string Currency,
    string DefaultCulture,
    IReadOnlyList<string> SupportedCultures,
    Uri? LogoUrl,
    string? AccentColor,
    string? OnAccentColor);

[ImmutableObject(true)]
public sealed record PublicMenuCategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<PublicMenuItemResponse> Items);

/// <summary>
/// A dish as guests see it. <c>Allergens</c> is <see langword="null"/> when the business has not declared them, which a
/// guest must never read as "contains none"; an empty list does say that.
/// </summary>
[ImmutableObject(true)]
public sealed record PublicMenuItemResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    bool IsAvailable,
    ArModelResponse? ArModel,
    IReadOnlyList<string>? Allergens,
    IReadOnlyList<string> DietaryLabels);

/// <summary>
/// Everything &lt;model-viewer&gt; needs: <c>src</c>, <c>ios-src</c> and <c>poster</c>, plus the plain GLB that devices
/// opening Android Scene Viewer should load instead of <see cref="GlbUrl"/>.
/// </summary>
[ImmutableObject(true)]
public sealed record ArModelResponse(Uri GlbUrl, Uri? SceneViewerGlbUrl, Uri? UsdzUrl, Uri? PosterUrl);
