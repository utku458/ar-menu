using Amazon.S3;
using Amazon.S3.Model;
using ArMenu.Application.Assets;
using ArMenu.Application.Common.Diagnostics;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.ArModels;
using ArMenu.Infrastructure.Assets;
using ArMenu.Infrastructure.Auditing;
using ArMenu.Infrastructure.Maintenance;
using ArMenu.Infrastructure.Persistence;
using ArMenu.Infrastructure.Statistics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.Retention;

/// <summary>
/// Deletes everything a closed business owned once its retention period is over: files first, then its rows, then the
/// mark that it was purged. A run that fails halfway leaves the business unmarked, so the next run finishes the job;
/// deleting the rows first would leave files nobody could find anymore.
/// </summary>
/// <remarks>
/// The tenant record stays: its slug must never be claimed again, because printed QR codes point at it. Each business is
/// purged in a scope bound to it, so row-level security checks every delete.
/// </remarks>
internal sealed partial class TenantPurge(
    IServiceScopeFactory scopeFactory,
    IAmazonS3 s3,
    IOptions<StorageOptions> storageOptions,
    IOptions<RetentionOptions> retentionOptions,
    TimeProvider timeProvider,
    ArMenuMetrics metrics,
    ILogger<TenantPurge> logger)
{
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        using var activity = ArMenuTelemetry.ActivitySource.StartActivity("TenantPurge");
        var purged = 0;
        foreach (var tenantId in await ListDueAsync(cancellationToken))
        {
            await PurgeAsync(tenantId, cancellationToken);
            purged++;
        }

        return purged;
    }

    public async Task PurgeAsync(TenantId tenantId, CancellationToken cancellationToken)
    {
        var storage = storageOptions.Value;
        await DeletePrefixAsync(storage.UploadsBucket, TenantAssetKeys.StagingPrefix(tenantId), cancellationToken);
        await DeletePrefixAsync(storage.UploadsBucket, TenantAssetKeys.SourcesPrefix(tenantId), cancellationToken);
        await DeletePrefixAsync(storage.AssetsBucket, TenantAssetKeys.PublishedPrefix(tenantId), cancellationToken);

        await using var scope = scopeFactory.CreateAsyncScope();
        var tenantInfo = await scope.ServiceProvider.GetRequiredService<ITenantLookup>().FindByIdAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Business {tenantId} to purge does not exist.");
        scope.ServiceProvider.GetRequiredService<ITenantContextSetter>().SetTenant(tenantInfo);
        var dbContext = scope.ServiceProvider.GetRequiredService<ArMenuDbContext>();

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async token =>
        {
            dbContext.ChangeTracker.Clear();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(token);

            // Children before parents; the tenant filter is on, row-level security checks each statement.
            await dbContext.Set<ArModelProcessingQueueEntry>().Where(entry => entry.TenantId == tenantId).ExecuteDeleteAsync(token);
            await dbContext.ArModelProcessings.ExecuteDeleteAsync(token);
            await dbContext.Set<MenuDailyStatistic>().ExecuteDeleteAsync(token);
            await dbContext.Set<AuditLogEntry>().ExecuteDeleteAsync(token);
            await dbContext.MenuItems.IgnoreQueryFilters([QueryFilters.SoftDelete]).ExecuteDeleteAsync(token);
            await dbContext.MenuCategories.IgnoreQueryFilters([QueryFilters.SoftDelete]).ExecuteDeleteAsync(token);
            await dbContext.TenantInvitations.ExecuteDeleteAsync(token);
            await dbContext.TenantMemberships.ExecuteDeleteAsync(token);

            var tenant = await dbContext.Tenants.SingleAsync(row => row.Id == tenantId, token);
            tenant.MarkPurged(timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(token);

            await transaction.CommitAsync(token);
        }, cancellationToken);

        metrics.TenantPurged();
        LogPurged(logger, tenantId.Value);
    }

    private async Task<List<TenantId>> ListDueAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var cutoff = timeProvider.GetUtcNow() - retentionOptions.Value.ClosedBusinessRetention;

        // The tenants table has no row-level security: it is how requests find their business in the first place.
        return await scope.ServiceProvider.GetRequiredService<ArMenuDbContext>().Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Status == TenantStatus.Closed && tenant.PurgedAt == null && tenant.ClosedAt <= cutoff)
            .Select(tenant => tenant.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task DeletePrefixAsync(string bucket, string prefix, CancellationToken cancellationToken)
    {
        var request = new ListObjectsV2Request { BucketName = bucket, Prefix = prefix };
        do
        {
            var response = await s3.ListObjectsV2Async(request, cancellationToken);
            var keys = response.S3Objects ?? [];
            if (keys.Count > 0)
            {
                await s3.DeleteObjectsAsync(
                    new DeleteObjectsRequest { BucketName = bucket, Objects = [.. keys.Select(entry => new KeyVersion { Key = entry.Key })] },
                    cancellationToken);
                metrics.FilesCleanedUp("purged", keys.Count);
            }

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (!string.IsNullOrEmpty(request.ContinuationToken));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Purged the data and files of closed business {TenantId}")]
    private static partial void LogPurged(ILogger logger, Guid tenantId);
}

/// <summary>Runs <see cref="TenantPurge"/> periodically, on one instance at a time.</summary>
internal sealed class TenantPurgeWorker(
    TenantPurge purge,
    IServiceScopeFactory scopeFactory,
    IOptions<RetentionOptions> options,
    ILogger<TenantPurgeWorker> logger) : ExclusiveMaintenanceWorker(scopeFactory, logger)
{
    protected override long LockKey => 0x41_52_4D_45_4E_55_50_47; // "ARMENUPG"

    protected override string JobName => "Closed business purge";

    protected override bool Enabled => options.Value.Enabled;

    protected override TimeSpan Interval => options.Value.Interval;

    protected override Task RunJobAsync(CancellationToken stoppingToken) => purge.RunAsync(stoppingToken);
}
