namespace ArMenu.Infrastructure.ArModels;

/// <summary>
/// Wakes the workers of this process when work was scheduled here, so a model starts processing at once instead of on
/// the next poll. Workers of other instances still find it by polling.
/// </summary>
internal sealed class ArModelProcessingSignal : IDisposable
{
    private readonly SemaphoreSlim _signal = new(0, 1);

    public void Notify()
    {
        // At most one pending wake-up: several notifications before a worker looks are one piece of news.
        if (_signal.CurrentCount == 0)
        {
            try
            {
                _signal.Release();
            }
            catch (SemaphoreFullException)
            {
                // Another notification got there first.
            }
        }
    }

    /// <returns>Whether a notification arrived before <paramref name="timeout"/> elapsed.</returns>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) => _signal.WaitAsync(timeout, cancellationToken);

    public void Dispose() => _signal.Dispose();
}
