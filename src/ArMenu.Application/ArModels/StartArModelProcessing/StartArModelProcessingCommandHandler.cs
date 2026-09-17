using ArMenu.Application.Abstractions.Assets;
using ArMenu.Application.Assets;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.ArModels;
using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.ArModels.StartArModelProcessing;

public sealed class StartArModelProcessingCommandHandler(
    ITenantContext tenantContext,
    IMenuItemRepository items,
    IArModelProcessingRepository processings,
    IArModelProcessingScheduler scheduler,
    IAssetStorage storage,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<StartArModelProcessingCommand, Result<ArModelProcessingId>>
{
    public async ValueTask<Result<ArModelProcessingId>> Handle(StartArModelProcessingCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var item = await items.GetByIdAsync(command.ItemId, cancellationToken);
        if (item is null)
        {
            return MenuItemErrors.NotFound;
        }

        var tenantId = tenantContext.TenantId;
        // Derived from the tenant of the request: an upload id of another business simply does not exist here.
        var stagingKey = TenantAssetKeys.Staging(tenantId, command.UploadId);

        var staged = await storage.FindStagedAsync(stagingKey, cancellationToken);
        if (staged is null)
        {
            return AssetErrors.UploadNotFound;
        }

        // Cheap checks before a worker spends a minute on the file: the size, and a GLB the pipeline can read.
        var inspected = staged.Size > AssetLimits.MaxModelBytes
            ? AssetErrors.TooLarge(AssetKind.Model)
            : await AssetInspector.InspectModelAsync(
                GlbProfile.Source,
                staged.Size,
                (offset, count, token) => storage.ReadStagedAsync(stagingKey, offset, count, token),
                cancellationToken);

        if (inspected.IsFailure)
        {
            await storage.DeleteStagedAsync(stagingKey, cancellationToken);
            return inspected.Error;
        }

        var now = timeProvider.GetUtcNow();

        // The newest upload wins: whatever older, unfinished uploads for the item still produce is discarded.
        foreach (var unfinished in await processings.ListUnfinishedForItemAsync(item.Id, cancellationToken))
        {
            unfinished.Supersede(now);
        }

        var processing = ArModelProcessing.Queue(tenantId, item.Id, staged.Size);
        await storage.MoveToSourcesAsync(stagingKey, TenantAssetKeys.Source(tenantId, processing.Id), cancellationToken);

        processings.Add(processing);
        scheduler.Schedule(processing, availableAt: now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        scheduler.Wake();
        return processing.Id;
    }
}
