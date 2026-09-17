namespace ArMenu.Infrastructure.Assets;

public sealed class AssetOptions
{
    public const string SectionName = "Assets";

    /// <summary>
    /// Public base URL of the asset storage or CDN, ending with a slash (e.g. <c>https://cdn.armenu.app/</c>).
    /// Required: validated at startup.
    /// </summary>
    public Uri? PublicBaseUrl { get; set; }
}
