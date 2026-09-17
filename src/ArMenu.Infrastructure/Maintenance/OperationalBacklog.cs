using System.Diagnostics.Metrics;
using ArMenu.Application.Common.Diagnostics;
using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.ArModels;
using ArMenu.Infrastructure.Mail;
using ArMenu.Infrastructure.Persistence;
using ArMenu.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.Maintenance;

/// <summary>
/// Work that is waiting, at one moment. Counters say what was done; only a backlog says that something stopped doing it:
/// a mail server that refuses everything, a processor that is down, a purge worker that never runs.
/// </summary>
/// <param name="EmailsPending">Messages in the outbox, whether due now or waiting for a retry.</param>
/// <param name="OldestEmailAge">How long the oldest message has been waiting; zero when there is none.</param>
/// <param name="ProcessingsQueued">Model processings waiting in the queue, claimed or not.</param>
/// <param name="ProcessingOverdue">How long the most overdue processing has been due without being finished.</param>
/// <param name="TenantsOverdueForPurge">Closed businesses kept a day beyond their retention period.</param>
public sealed record BacklogSnapshot(
    long EmailsPending,
    TimeSpan OldestEmailAge,
    long ProcessingsQueued,
    TimeSpan ProcessingOverdue,
    long TenantsOverdueForPurge);

/// <summary>Reads the backlog from the database. Only cross-tenant tables are read, so no tenant is bound.</summary>
internal sealed class OperationalBacklog(IServiceScopeFactory scopeFactory, IOptions<RetentionOptions> retention, TimeProvider timeProvider)
{
    /// <summary>A purge runs every few hours; a business still waiting a day after it was due means the purge is not running.</summary>
    public static readonly TimeSpan PurgeGrace = TimeSpan.FromDays(1);

    public async Task<BacklogSnapshot> SampleAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ArMenuDbContext>();
        var now = timeProvider.GetUtcNow();

        var emails = await dbContext.Set<OutboxEmail>()
            .GroupBy(_ => 1)
            .Select(group => new { Count = group.LongCount(), Oldest = group.Min(email => (DateTimeOffset?)email.CreatedAt) })
            .SingleOrDefaultAsync(cancellationToken);

        var queue = await dbContext.Set<ArModelProcessingQueueEntry>()
            .GroupBy(_ => 1)
            .Select(group => new { Count = group.LongCount(), EarliestDue = group.Min(entry => (DateTimeOffset?)entry.AvailableAt) })
            .SingleOrDefaultAsync(cancellationToken);

        var purgeDeadline = now - retention.Value.ClosedBusinessRetention - PurgeGrace;
        var overdueTenants = await dbContext.Tenants
            .LongCountAsync(tenant => tenant.Status == TenantStatus.Closed && tenant.PurgedAt == null && tenant.ClosedAt <= purgeDeadline, cancellationToken);

        return new BacklogSnapshot(
            emails?.Count ?? 0,
            Age(emails?.Oldest, now),
            queue?.Count ?? 0,
            Age(queue?.EarliestDue, now),
            overdueTenants);
    }

    private static TimeSpan Age(DateTimeOffset? since, DateTimeOffset now) =>
        since is { } start && start < now ? now - start : TimeSpan.Zero;
}

/// <summary>
/// Publishes the latest <see cref="BacklogSnapshot"/> as gauges. The database is sampled on a timer rather than on each
/// metrics export, so an exporter's schedule never turns into query load, and a slow database never stalls an export.
/// </summary>
internal sealed class BacklogGauges
{
    private BacklogSnapshot? _latest;

    public BacklogGauges(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        var meter = meterFactory.Create(ArMenuTelemetry.Name);

        meter.CreateObservableGauge(
            "armenu.email.outbox.pending",
            () => Observe(snapshot => snapshot.EmailsPending),
            unit: "{message}",
            description: "E-mails waiting in the outbox, due now or waiting for a retry.");
        meter.CreateObservableGauge(
            "armenu.email.outbox.oldest_age",
            () => Observe(snapshot => snapshot.OldestEmailAge.TotalSeconds),
            unit: "s",
            description: "How long the oldest e-mail in the outbox has been waiting.");
        meter.CreateObservableGauge(
            "armenu.ar_model.processing.queue.pending",
            () => Observe(snapshot => snapshot.ProcessingsQueued),
            unit: "{processing}",
            description: "Model processings waiting in the queue.");
        meter.CreateObservableGauge(
            "armenu.ar_model.processing.queue.overdue",
            () => Observe(snapshot => snapshot.ProcessingOverdue.TotalSeconds),
            unit: "s",
            description: "How long the most overdue model processing has been due.");
        meter.CreateObservableGauge(
            "armenu.tenants.purge.overdue",
            () => Observe(snapshot => snapshot.TenantsOverdueForPurge),
            unit: "{tenant}",
            description: "Closed businesses kept more than a day beyond their retention period.");
    }

    public BacklogSnapshot? Latest => Volatile.Read(ref _latest);

    public void Publish(BacklogSnapshot snapshot) => Volatile.Write(ref _latest, snapshot);

    // Nothing is reported before the first sample: a zero would claim an empty backlog nobody measured.
    private IEnumerable<Measurement<T>> Observe<T>(Func<BacklogSnapshot, T> read)
        where T : struct =>
        Latest is { } snapshot ? [new Measurement<T>(read(snapshot))] : [];
}

public sealed class BacklogMetricsOptions
{
    public const string SectionName = "BacklogMetrics";

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>Samples the backlog periodically. Every instance samples; alert rules take the maximum.</summary>
internal sealed partial class BacklogMetricsWorker(
    OperationalBacklog backlog,
    BacklogGauges gauges,
    IOptions<BacklogMetricsOptions> options,
    TimeProvider timeProvider,
    ILogger<BacklogMetricsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(options.Value.Interval, timeProvider);
        do
        {
            try
            {
                gauges.Publish(await backlog.SampleAsync(stoppingToken));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A database that cannot be read is reported by the readiness probe; the gauges keep their last values.
                LogSampleFailed(logger, exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sampling the operational backlog failed")]
    private static partial void LogSampleFailed(ILogger logger, Exception exception);
}
