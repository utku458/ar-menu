using System.Diagnostics;
using ArMenu.Application.ArModels.RunArModelProcessing;
using ArMenu.Application.Common.Diagnostics;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.ArModels;
using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.Persistence;
using ArMenu.Infrastructure.Persistence.Configurations;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ArMenu.Infrastructure.ArModels;

/// <summary>
/// Claims the next due processing and runs it in a scope bound to its tenant. Claims use <c>FOR UPDATE SKIP LOCKED</c>
/// and a lease, so any number of workers across instances share the queue without running a job twice, and a job whose
/// worker died becomes available again when its lease expires.
/// </summary>
internal sealed partial class ArModelProcessingDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<AssetProcessorOptions> options,
    TimeProvider timeProvider,
    ILogger<ArModelProcessingDispatcher> logger)
{
    private static readonly string ClaimSql = $"""
        UPDATE {ArModelProcessingQueueEntryConfiguration.TableName} AS entry
        SET lease_expires_at = @lease_expires_at
        FROM (
            SELECT processing_id
            FROM {ArModelProcessingQueueEntryConfiguration.TableName}
            WHERE available_at <= @now AND (lease_expires_at IS NULL OR lease_expires_at <= @now)
            ORDER BY available_at
            LIMIT 1
            FOR UPDATE SKIP LOCKED
        ) AS due
        WHERE entry.processing_id = due.processing_id
        RETURNING entry.processing_id, entry.tenant_id
        """;

    /// <returns>Whether a job was found; <see langword="false"/> means the queue has nothing due.</returns>
    public async Task<bool> RunNextAsync(CancellationToken cancellationToken)
    {
        if (await ClaimAsync(cancellationToken) is not { } claim)
        {
            return false;
        }

        var (processingId, tenantId) = claim;

        // A worker's run has no incoming request: it starts its own trace, which the processor call joins.
        using var activity = ArMenuTelemetry.ActivitySource.StartActivity("ArModelProcessing", ActivityKind.Internal, parentContext: default);
        activity?.SetTag("armenu.ar_model.processing_id", processingId.Value);
        activity?.SetTag("armenu.tenant_id", tenantId.Value);

        await using var scope = scopeFactory.CreateAsyncScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<ITenantLookup>().FindByIdAsync(tenantId, cancellationToken);
        if (tenant is not { IsActive: true })
        {
            LogTenantInactive(logger, processingId.Value);
            await RemoveAsync(scope, processingId, cancellationToken);
            return true;
        }

        scope.ServiceProvider.GetRequiredService<ITenantContextSetter>().SetTenant(tenant);

        try
        {
            await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new RunArModelProcessingCommand(processingId), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Bugs, database outages, a newer upload saved concurrently: give the job back instead of waiting for the
            // lease. Its attempts are counted by the processing itself, so a job that always crashes still ends.
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            LogCrashed(logger, exception, processingId.Value);
            await using var retryScope = scopeFactory.CreateAsyncScope();
            await ReleaseAsync(retryScope, processingId, timeProvider.GetUtcNow() + options.Value.CrashRetryDelay, CancellationToken.None);
        }

        return true;
    }

    private async Task<(ArModelProcessingId, TenantId)?> ClaimAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ArMenuDbContext>();
        var now = timeProvider.GetUtcNow();

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        await using var command = (NpgsqlCommand)dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = ClaimSql;
        command.Parameters.AddWithValue("now", now);
        command.Parameters.AddWithValue("lease_expires_at", now + options.Value.Lease);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? (ArModelProcessingId.From(reader.GetGuid(0)), TenantId.From(reader.GetGuid(1)))
            : null;
    }

    private static Task<int> RemoveAsync(AsyncServiceScope scope, ArModelProcessingId processingId, CancellationToken cancellationToken) =>
        scope.ServiceProvider.GetRequiredService<ArMenuDbContext>().Set<ArModelProcessingQueueEntry>()
            .Where(entry => entry.ProcessingId == processingId)
            .ExecuteDeleteAsync(cancellationToken);

    private static Task<int> ReleaseAsync(AsyncServiceScope scope, ArModelProcessingId processingId, DateTimeOffset availableAt, CancellationToken cancellationToken) =>
        scope.ServiceProvider.GetRequiredService<ArMenuDbContext>().Set<ArModelProcessingQueueEntry>()
            .Where(entry => entry.ProcessingId == processingId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(entry => entry.AvailableAt, availableAt)
                    .SetProperty(entry => entry.LeaseExpiresAt, (DateTimeOffset?)null),
                cancellationToken);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dropped model processing {ProcessingId}: its business is not active")]
    private static partial void LogTenantInactive(ILogger logger, Guid processingId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Model processing {ProcessingId} crashed; it will be retried")]
    private static partial void LogCrashed(ILogger logger, Exception exception, Guid processingId);
}
