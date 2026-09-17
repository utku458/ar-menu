using ArMenu.Application.Abstractions.Assets;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Assets.CreateAssetUpload;

public sealed class CreateAssetUploadCommandHandler(ITenantContext tenantContext, IAssetStorage storage, TimeProvider timeProvider)
    : ICommandHandler<CreateAssetUploadCommand, Result<AssetUpload>>
{
    public async ValueTask<Result<AssetUpload>> Handle(CreateAssetUploadCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var uploadId = Guid.CreateVersion7(timeProvider.GetUtcNow());
        var stagingKey = TenantAssetKeys.Staging(tenantContext.TenantId, uploadId);

        var upload = await storage.CreateUploadAsync(stagingKey, command.ContentType.ToLowerInvariant(), command.Size, cancellationToken);

        return new AssetUpload(uploadId, upload.Url, "PUT", upload.Headers, upload.ExpiresAt);
    }
}
