using ArMenu.Domain.Common;

namespace ArMenu.Domain.Media;

public static class MediaErrors
{
    public static readonly Error AssetPathRequired = Error.Validation(
        "asset_path.required", "An asset path is required.");

    public static readonly Error AssetPathInvalid = Error.Validation(
        "asset_path.invalid",
        "Asset paths must be relative storage keys made of letters, digits, '.', '_', '-' and '/' separators.");
}
