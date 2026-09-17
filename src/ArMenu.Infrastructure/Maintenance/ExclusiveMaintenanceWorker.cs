using ArMenu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArMenu.Infrastructure.Maintenance;

/// <summary>
/// Runs a maintenance job periodically on one instance at a time: every instance tries, a PostgreSQL advisory lock lets
/// one through. A failed run is logged and tried again at the next interval.
/// </summary>
internal abstract partial class ExclusiveMaintenanceWorker(IServiceScopeFactory scopeFactory, ILogger logger) : BackgroundService
{
    /// <summary>Any fixed number shared by all instances; PostgreSQL advisory locks are keyed by 64-bit integers.</summary>
    protected abstract long LockKey { get; }

    protected abstract string JobName { get; }

    protected abstract bool Enabled { get; }

    protected abstract TimeSpan Interval { get; }

    protected abstract Task RunJobAsync(CancellationToken stoppingToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await RunExclusivelyAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                LogFailed(logger, exception, JobName);
            }
        }
        while (await WaitAsync(timer, stoppingToken));
    }

    private async Task RunExclusivelyAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ArMenuDbContext>().Database;

        // The session-level lock lives as long as this connection stays open.
        await database.OpenConnectionAsync(stoppingToken);
        var acquired = await database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({LockKey}) AS \"Value\"").SingleAsync(stoppingToken);
        if (!acquired)
        {
            return;
        }

        try
        {
            await RunJobAsync(stoppingToken);
        }
        finally
        {
            await database.SqlQuery<bool>($"SELECT pg_advisory_unlock({LockKey}) AS \"Value\"").SingleAsync(CancellationToken.None);
        }
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "{Job} failed; it will run again at the next interval")]
    private static partial void LogFailed(ILogger logger, Exception exception, string job);
}
