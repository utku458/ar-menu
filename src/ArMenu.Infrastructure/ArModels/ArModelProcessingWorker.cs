using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.ArModels;

/// <summary>Runs processing jobs in the background: at once when they are scheduled here, otherwise on the next poll.</summary>
internal sealed partial class ArModelProcessingWorker(
    ArModelProcessingDispatcher dispatcher,
    ArModelProcessingSignal signal,
    IOptions<AssetProcessorOptions> options,
    ILogger<ArModelProcessingWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        options.Value.WorkersEnabled
            ? Task.WhenAll(Enumerable.Range(0, options.Value.Workers).Select(_ => RunAsync(stoppingToken)))
            : Task.CompletedTask;

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await dispatcher.RunNextAsync(stoppingToken))
                {
                    await signal.WaitAsync(options.Value.PollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // The database or the queue itself is unavailable: back off instead of spinning.
                LogPollFailed(logger, exception);
                if (!await DelayAsync(options.Value.PollInterval, stoppingToken))
                {
                    return;
                }
            }
        }
    }

    /// <returns><see langword="false"/> when the host is stopping.</returns>
    private static async Task<bool> DelayAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, stoppingToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Looking for model processing jobs failed")]
    private static partial void LogPollFailed(ILogger logger, Exception exception);
}
