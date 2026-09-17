using ArMenu.Application.Abstractions.Assets;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Assets.PublishAssetUpload;

public sealed class PublishAssetUploadCommandHandler(ITenantContext tenantContext, IAssetStorage storage, IAssetUrlResolver assetUrls)
    : ICommandHandler<PublishAssetUploadCommand, Result<PublishedAsset>>
{
    public async ValueTask<Result<PublishedAsset>> Handle(PublishAssetUploadCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenantId = tenantContext.TenantId;
        // Derived from the tenant of the request: an upload id of another business simply does not exist here.
        var stagingKey = TenantAssetKeys.Staging(tenantId, command.UploadId);

        var staged = await storage.FindStagedAsync(stagingKey, cancellationToken);
        if (staged is null)
        {
            return AssetErrors.UploadNotFound;
        }

        var inspected = staged.Size > AssetLimits.MaxBytes(command.Kind)
            ? AssetErrors.TooLarge(command.Kind)
            : await AssetInspector.InspectAsync(
                command.Kind,
                staged.Size,
                (offset, count, token) => storage.ReadStagedAsync(stagingKey, offset, count, token),
                cancellationToken);

        if (inspected.IsFailure)
        {
            await storage.DeleteStagedAsync(stagingKey, cancellationToken);
            return inspected.Error;
        }

        var path = TenantAssetKeys.Published(tenantId, command.UploadId, inspected.Value.Extension);
        await storage.PublishAsync(stagingKey, path, inspected.Value.ContentType, cancellationToken);

        return new PublishedAsset(path.Value, assetUrls.Resolve(path), inspected.Value.ContentType, staged.Size);
    }
}
