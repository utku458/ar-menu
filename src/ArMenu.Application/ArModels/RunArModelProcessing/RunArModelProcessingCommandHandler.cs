using System.Diagnostics;
using ArMenu.Application.Abstractions.Assets;
using ArMenu.Application.Assets;
using ArMenu.Application.Common.Diagnostics;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.ArModels;
using ArMenu.Domain.Common;
using ArMenu.Domain.Media;
using ArMenu.Domain.Menus;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArMenu.Application.ArModels.RunArModelProcessing;

public sealed partial class RunArModelProcessingCommandHandler(
    ITenantContext tenantContext,
    IArModelProcessingRepository processings,
    IMenuItemRepository items,
    IArModelProcessingScheduler scheduler,
    IAssetStorage storage,
    IModelProcessor processor,
    IUnitOfWork unitOfWork,
    IOptions<ArModelProcessingOptions> options,
    TimeProvider timeProvider,
    ArMenuMetrics metrics,
    ILogger<RunArModelProcessingCommandHandler> logger)
    : ICommandHandler<RunArModelProcessingCommand, Result>
{
    private readonly ArModelProcessingOptions _options = options.Value;

    public async ValueTask<Result> Handle(RunArModelProcessingCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var processing = await processings.GetByIdAsync(command.ProcessingId, cancellationToken);
        if (processing is null)
        {
            await scheduler.CompleteAsync(command.ProcessingId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ArModelProcessingErrors.NotFound;
        }

        // Superseded while queued, or out of attempts after crashed workers: nothing left to run.
        if (processing.Start(timeProvider.GetUtcNow()).IsFailure)
        {
            await scheduler.CompleteAsync(processing.Id, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        // Record the attempt before the long call: a crash during processing must still count it.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var paths = TenantAssetKeys.Processed(tenantContext.TenantId, processing.Id);
        var job = await CreateJobAsync(processing.Id, paths, cancellationToken);
        var startedAt = Stopwatch.GetTimestamp();
        var outcome = await processor.ProcessAsync(job, cancellationToken);
        var duration = Stopwatch.GetElapsedTime(startedAt);

        var now = timeProvider.GetUtcNow();
        switch (outcome)
        {
            case ModelProcessingOutcome.Processed processed:
                await PublishAsync(processing, paths, processed.Report, now, cancellationToken);
                if (processing.Status == ArModelProcessingStatus.Succeeded)
                {
                    metrics.ProcessingAttempted(ArMenuMetrics.ProcessingOutcome.Succeeded, duration);
                    metrics.ModelPublished(now - processing.CreatedAt);
                }
                else
                {
                    metrics.ProcessingAttempted(ArMenuMetrics.ProcessingOutcome.Rejected, duration, processing.FailureCode);
                }

                break;

            case ModelProcessingOutcome.Rejected rejected:
                LogRejected(logger, processing.Id.Value, rejected.Code, rejected.Detail);
                processing.Reject(ModelRejections.Known.Contains(rejected.Code) ? rejected.Code : AssetErrors.ProcessingFailed.Code, now);
                metrics.ProcessingAttempted(ArMenuMetrics.ProcessingOutcome.Rejected, duration, processing.FailureCode);
                await scheduler.CompleteAsync(processing.Id, cancellationToken);
                break;

            case ModelProcessingOutcome.Unavailable unavailable:
                var willRetry = processing.RetryOrGiveUp(now).Value;
                LogUnavailable(logger, processing.Id.Value, processing.Attempts, willRetry, unavailable.Reason);
                metrics.ProcessingAttempted(
                    willRetry ? ArMenuMetrics.ProcessingOutcome.Retrying : ArMenuMetrics.ProcessingOutcome.Failed,
                    duration,
                    willRetry ? null : processing.FailureCode);
                if (willRetry)
                {
                    await scheduler.RescheduleAsync(processing.Id, now + _options.RetryDelayAfter(processing.Attempts), cancellationToken);
                }
                else
                {
                    await scheduler.CompleteAsync(processing.Id, cancellationToken);
                }

                break;

            default:
                throw new InvalidOperationException($"Unknown processing outcome '{outcome.GetType().Name}'.");
        }

        // A newer upload that superseded this processing meanwhile makes this save fail on the row version, and nothing
        // of this attempt is attached. The worker then finds the processing finished and drops it from the queue.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<ModelProcessingJob> CreateJobAsync(ArModelProcessingId id, ProcessedModelPaths paths, CancellationToken cancellationToken)
    {
        var lifetime = _options.TransferUrlLifetime;
        var source = await storage.CreateSourceDownloadAsync(TenantAssetKeys.Source(tenantContext.TenantId, id), lifetime, cancellationToken);

        return new ModelProcessingJob(
            id.Value,
            source,
            new ModelProcessingUploads(
                Model: await storage.CreateProcessedUploadAsync(paths.Model, AssetLimits.ModelContentType, lifetime, cancellationToken),
                SceneViewerModel: await storage.CreateProcessedUploadAsync(paths.SceneViewerModel, AssetLimits.ModelContentType, lifetime, cancellationToken),
                AppleModel: await storage.CreateProcessedUploadAsync(paths.AppleModel, AssetLimits.AppleModelContentType, lifetime, cancellationToken),
                Poster: await storage.CreateProcessedUploadAsync(paths.Poster, "image/webp", lifetime, cancellationToken)));
    }

    private async Task PublishAsync(
        ArModelProcessing processing,
        ProcessedModelPaths paths,
        ArModelReport report,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await scheduler.CompleteAsync(processing.Id, cancellationToken);

        // The processor is trusted to do its job, not to be right: its files are inspected like any upload.
        if (await FindInvalidOutputAsync(paths, cancellationToken) is { } invalid)
        {
            LogOutputInvalid(logger, processing.Id.Value, invalid.Value);
            processing.Reject(AssetErrors.ProcessingOutputInvalid.Code, now);
            return;
        }

        var item = await items.GetByIdAsync(processing.MenuItemId, cancellationToken);
        if (item is null)
        {
            processing.Reject(MenuItemErrors.NotFound.Code, now);
            return;
        }

        item.AttachArModel(ArModel.Create(
            glbPath: paths.Model,
            sceneViewerGlbPath: paths.SceneViewerModel,
            usdzPath: paths.AppleModel,
            posterPath: paths.Poster).Value);
        processing.Succeed(report, now);
    }

    private async Task<AssetPath?> FindInvalidOutputAsync(ProcessedModelPaths paths, CancellationToken cancellationToken)
    {
        (AssetPath Path, Func<long, ReadRange, Task<Result<InspectedAsset>>> Inspect, long MaxBytes)[] outputs =
        [
            (paths.Model, (size, read) => AssetInspector.InspectModelAsync(GlbProfile.Web, size, read, cancellationToken), AssetLimits.MaxModelBytes),
            (paths.SceneViewerModel, (size, read) => AssetInspector.InspectModelAsync(GlbProfile.SceneViewer, size, read, cancellationToken), AssetLimits.MaxModelBytes),
            (paths.AppleModel, (size, read) => AssetInspector.InspectAsync(AssetKind.AppleModel, size, read, cancellationToken), AssetLimits.MaxModelBytes),
            (paths.Poster, (size, read) => AssetInspector.InspectAsync(AssetKind.Poster, size, read, cancellationToken), AssetLimits.MaxPosterBytes),
        ];

        foreach (var (path, inspect, maxBytes) in outputs)
        {
            var stored = await storage.FindPublishedAsync(path, cancellationToken);
            if (stored is null || stored.Size > maxBytes ||
                (await inspect(stored.Size, (offset, count, token) => storage.ReadPublishedAsync(path, offset, count, token))).IsFailure)
            {
                return path;
            }
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Model processing {ProcessingId} was rejected with {Code}: {Detail}")]
    private static partial void LogRejected(ILogger logger, Guid processingId, string code, string detail);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Model processing {ProcessingId} attempt {Attempt} failed (retrying: {WillRetry}): {Reason}")]
    private static partial void LogUnavailable(ILogger logger, Guid processingId, int attempt, bool willRetry, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Model processing {ProcessingId} produced an invalid file at {Path}")]
    private static partial void LogOutputInvalid(ILogger logger, Guid processingId, string path);
}
