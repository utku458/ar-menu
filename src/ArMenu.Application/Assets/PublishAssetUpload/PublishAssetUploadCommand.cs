using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Assets.PublishAssetUpload;

/// <summary>
/// Accepts a completed upload: inspects the file, then publishes it under an immutable public key that can be attached
/// to menu items. Rejected files are removed from staging.
/// </summary>
public sealed record PublishAssetUploadCommand(Guid UploadId, AssetKind Kind) : ICommand<Result<PublishedAsset>>;

// Path: the storage key menu items reference (e.g. in an AR model).
public sealed record PublishedAsset(string Path, Uri Url, string ContentType, long Size);
