using ArMenu.Domain.ArModels;
using ArMenu.Domain.Media;
using ArMenu.Domain.Tenants;

namespace ArMenu.Application.Assets;

/// <summary>
/// Where a tenant's files live in storage. Keys are derived from the tenant bound to the request, never taken from the
/// client, so one business can neither publish nor reference another business's files.
/// </summary>
public static class TenantAssetKeys
{
    /// <summary>Prefix of browser uploads in the private uploads bucket; nothing stays there for long.</summary>
    public const string StagingRoot = "staging/";

    /// <summary>Prefix of processed models' sources in the private uploads bucket, kept for reprocessing.</summary>
    public const string SourcesRoot = "sources/";

    public static string Staging(TenantId tenantId, Guid uploadId) => $"{StagingPrefix(tenantId)}{uploadId:N}";

    public static string StagingPrefix(TenantId tenantId) => $"{StagingRoot}{tenantId.Value:N}/";

    public static string Source(TenantId tenantId, ArModelProcessingId processingId) =>
        $"{SourcesPrefix(tenantId)}{processingId.Value:N}.glb";

    public static string SourcesPrefix(TenantId tenantId) => $"{SourcesRoot}{tenantId.Value:N}/";

    /// <summary>
    /// Where a processing publishes its files. Derived from the processing's id: a retry overwrites its own files, and
    /// no two uploads can ever share a key.
    /// </summary>
    public static ProcessedModelPaths Processed(TenantId tenantId, ArModelProcessingId processingId)
    {
        var stem = $"{PublishedPrefix(tenantId)}{processingId.Value:N}";
        return new ProcessedModelPaths(
            Model: AssetPath.Create($"{stem}.glb").Value,
            SceneViewerModel: AssetPath.Create($"{stem}.scene-viewer.glb").Value,
            AppleModel: AssetPath.Create($"{stem}.usdz").Value,
            Poster: AssetPath.Create($"{stem}.webp").Value);
    }

    /// <summary>Public, immutable key: a new upload always gets a new key, so it can be cached forever.</summary>
    public static AssetPath Published(TenantId tenantId, Guid uploadId, string extension) =>
        AssetPath.Create($"{PublishedPrefix(tenantId)}{uploadId:N}{extension}").Value;

    public static bool IsPublishedBy(AssetPath path, TenantId tenantId)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.Value.StartsWith(PublishedPrefix(tenantId), StringComparison.Ordinal);
    }

    public static string PublishedPrefix(TenantId tenantId) => $"tenants/{tenantId.Value:N}/assets/";
}

public sealed record ProcessedModelPaths(AssetPath Model, AssetPath SceneViewerModel, AssetPath AppleModel, AssetPath Poster)
{
    public IEnumerable<AssetPath> All => [Model, SceneViewerModel, AppleModel, Poster];
}
