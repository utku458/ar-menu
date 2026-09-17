namespace ArMenu.Application.ArModels;

public sealed class ArModelProcessingOptions
{
    public const string SectionName = "ArModelProcessing";

    /// <summary>
    /// Wait before the first retry after a transient failure; every further retry waits four times longer. The number
    /// of attempts is fixed by the domain (<c>ArModelProcessing.MaxAttempts</c>).
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Lifetime of the presigned URLs handed to the processor; longer than it may take to process a model.</summary>
    public TimeSpan TransferUrlLifetime { get; set; } = TimeSpan.FromMinutes(30);

    /// <param name="attempt">The attempt that just failed, starting at 1.</param>
    public TimeSpan RetryDelayAfter(int attempt) => RetryDelay * Math.Pow(4, Math.Max(attempt - 1, 0));
}
