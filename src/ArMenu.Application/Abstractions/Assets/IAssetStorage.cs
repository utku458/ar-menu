using ArMenu.Domain.Media;

namespace ArMenu.Application.Abstractions.Assets;

/// <summary>
/// Object storage for uploaded menu assets. Browsers upload straight to a private staging area with short-lived
/// presigned requests; the API inspects what arrived and publishes accepted files under immutable public keys.
/// </summary>
public interface IAssetStorage
{
    /// <summary>
    /// A presigned request that can only upload exactly <paramref name="size"/> bytes of
    /// <paramref name="contentType"/> to <paramref name="stagingKey"/>. The storage service enforces both.
    /// </summary>
    Task<PresignedUpload> CreateUploadAsync(string stagingKey, string contentType, long size, CancellationToken cancellationToken);

    /// <returns>The staged object, or <see langword="null"/> when nothing was uploaded to the key.</returns>
    Task<StagedObject?> FindStagedAsync(string stagingKey, CancellationToken cancellationToken);

    /// <summary>Reads <paramref name="count"/> bytes of a staged object, starting at <paramref name="offset"/>.</summary>
    Task<byte[]> ReadStagedAsync(string stagingKey, long offset, int count, CancellationToken cancellationToken);

    /// <summary>Copies a staged object to its public, immutable location and removes it from staging.</summary>
    Task PublishAsync(string stagingKey, AssetPath path, string contentType, CancellationToken cancellationToken);

    Task DeleteStagedAsync(string stagingKey, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(AssetPath path, CancellationToken cancellationToken);

    /// <summary>Moves a staged upload to the private sources area, where it waits for processing.</summary>
    Task MoveToSourcesAsync(string stagingKey, string sourceKey, CancellationToken cancellationToken);

    /// <summary>A presigned download of a source, for the processor. The URL is valid for <paramref name="lifetime"/>.</summary>
    Task<Uri> CreateSourceDownloadAsync(string sourceKey, TimeSpan lifetime, CancellationToken cancellationToken);

    /// <summary>
    /// A presigned upload of a processed file to its public, immutable location, for the processor. The content type
    /// and the immutable caching headers are part of the signature.
    /// </summary>
    Task<PresignedUpload> CreateProcessedUploadAsync(AssetPath path, string contentType, TimeSpan lifetime, CancellationToken cancellationToken);

    /// <returns>The published object, or <see langword="null"/> when there is none.</returns>
    Task<StagedObject?> FindPublishedAsync(AssetPath path, CancellationToken cancellationToken);

    Task<byte[]> ReadPublishedAsync(AssetPath path, long offset, int count, CancellationToken cancellationToken);

    Task DeletePublishedAsync(AssetPath path, CancellationToken cancellationToken);
}

// Headers: sent exactly as given by the upload request, because they are part of the signature.
public sealed record PresignedUpload(Uri Url, IReadOnlyDictionary<string, string> Headers, DateTimeOffset ExpiresAt);

public sealed record StagedObject(long Size, string? ContentType);
