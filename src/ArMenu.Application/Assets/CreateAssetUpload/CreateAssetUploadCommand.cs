using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Assets.CreateAssetUpload;

/// <summary>
/// Grants a browser a short-lived, single-file upload straight to storage. <c>ContentType</c> and <c>Size</c> become
/// part of the upload's signature, so storage rejects a file of any other type or size.
/// </summary>
public sealed record CreateAssetUploadCommand(AssetKind Kind, string ContentType, long Size) : ICommand<Result<AssetUpload>>;

// Headers: the upload must send them unchanged.
public sealed record AssetUpload(Guid UploadId, Uri Url, string Method, IReadOnlyDictionary<string, string> Headers, DateTimeOffset ExpiresAt);
