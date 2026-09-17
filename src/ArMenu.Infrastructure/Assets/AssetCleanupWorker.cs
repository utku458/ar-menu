using ArMenu.Infrastructure.Maintenance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.Assets;

/// <summary>Runs <see cref="AssetCleanup"/> periodically, on one instance at a time.</summary>
internal sealed class AssetCleanupWorker(
    AssetCleanup cleanup,
    IServiceScopeFactory scopeFactory,
    IOptions<AssetCleanupOptions> options,
    ILogger<AssetCleanupWorker> logger) : ExclusiveMaintenanceWorker(scopeFactory, logger)
{
    protected override long LockKey => 0x41_52_4D_45_4E_55_43_4C; // "ARMENUCL"

    protected override string JobName => "Asset cleanup";

    protected override bool Enabled => options.Value.Enabled;

    protected override TimeSpan Interval => options.Value.Interval;

    protected override Task RunJobAsync(CancellationToken stoppingToken) => cleanup.RunAsync(stoppingToken);
}
