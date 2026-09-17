namespace ArMenu.Application.Assets;

/// <summary>What each kind of upload may declare before it is allowed to reach storage.</summary>
public static class AssetLimits
{
    /// <summary>Uploads are raw exports and scans: the asset pipeline makes them small. Matches the processor's limit.</summary>
    public const long MaxModelBytes = 64 * 1024 * 1024;
    public const long MaxPosterBytes = 2 * 1024 * 1024;

    /// <summary>A logo is a small mark shown at the top of a menu; every guest downloads it before anything else.</summary>
    public const long MaxLogoBytes = 512 * 1024;

    public const string ModelContentType = "model/gltf-binary";
    public const string AppleModelContentType = "model/vnd.usdz+zip";

    // SVG is deliberately absent: it is a document that may carry script, and it is served from the assets host.
    private static readonly string[] ImageContentTypes = ["image/webp", "image/avif", "image/png", "image/jpeg"];

    public static long MaxBytes(AssetKind kind) => kind switch
    {
        AssetKind.Poster => MaxPosterBytes,
        AssetKind.Logo => MaxLogoBytes,
        _ => MaxModelBytes,
    };

    public static IReadOnlyList<string> ContentTypes(AssetKind kind) => kind switch
    {
        AssetKind.Model => [ModelContentType],
        AssetKind.AppleModel => [AppleModelContentType],
        AssetKind.Poster or AssetKind.Logo => ImageContentTypes,
        _ => [],
    };
}
