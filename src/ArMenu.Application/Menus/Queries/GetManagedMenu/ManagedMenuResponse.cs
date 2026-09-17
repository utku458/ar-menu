namespace ArMenu.Application.Menus.Queries.GetManagedMenu;

public sealed record ManagedMenuResponse(IReadOnlyList<ManagedMenuCategoryResponse> Categories);

public sealed record ManagedMenuCategoryResponse(
    Guid Id,
    IReadOnlyDictionary<string, string> Name,
    IReadOnlyDictionary<string, string>? Description,
    int DisplayOrder,
    bool IsVisible,
    IReadOnlyList<ManagedMenuItemResponse> Items);

public sealed record ManagedMenuItemResponse(
    Guid Id,
    IReadOnlyDictionary<string, string> Name,
    IReadOnlyDictionary<string, string>? Description,
    decimal Price,
    int DisplayOrder,
    bool IsVisible,
    bool IsAvailable,
    ManagedArModelResponse? ArModel,
    IReadOnlyList<string>? Allergens,
    IReadOnlyList<string> DietaryLabels);

public sealed record ManagedArModelResponse(
    string GlbPath,
    string? SceneViewerGlbPath,
    string? UsdzPath,
    string? PosterPath,
    Uri GlbUrl,
    Uri? SceneViewerGlbUrl,
    Uri? UsdzUrl,
    Uri? PosterUrl);
