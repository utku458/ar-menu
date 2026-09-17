using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.ArModels;

/// <summary>The asset processor service and the workers that hand it jobs.</summary>
public sealed class AssetProcessorOptions
{
    public const string SectionName = "AssetProcessor";

    /// <summary>Base URL of the processor (web/services/asset-processor).</summary>
    public Uri? Url { get; set; }

    /// <summary>Shared secret sent as a bearer token; the processor refuses jobs without it.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Longest a job may take, from download to the last upload.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Run processing workers in this process. Disable on instances that should only serve requests.</summary>
    public bool WorkersEnabled { get; set; } = true;

    /// <summary>Jobs this process runs at once; the processor's own concurrency is the real limit.</summary>
    public int Workers { get; set; } = 1;

    /// <summary>How often idle workers look for work scheduled by other instances.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>How long a claimed job belongs to its worker. Longer than <see cref="Timeout"/>, so only dead workers lose jobs.</summary>
    public TimeSpan Lease { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Wait before a job whose attempt crashed unexpectedly becomes available again.</summary>
    public TimeSpan CrashRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}

internal sealed class AssetProcessorOptionsValidator : IValidateOptions<AssetProcessorOptions>
{
    private const int MinimumTokenLength = 32;

    public ValidateOptionsResult Validate(string? name, AssetProcessorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.WorkersEnabled)
        {
            if (options.Url is not { IsAbsoluteUri: true })
            {
                failures.Add("AssetProcessor:Url must be an absolute URL when workers are enabled.");
            }

            if (options.Token.Length < MinimumTokenLength)
            {
                failures.Add($"AssetProcessor:Token must be at least {MinimumTokenLength} characters when workers are enabled.");
            }
        }

        if (options.Workers < 1 || options.PollInterval <= TimeSpan.Zero || options.CrashRetryDelay < TimeSpan.Zero)
        {
            failures.Add("AssetProcessor:Workers and PollInterval must be positive, and CrashRetryDelay cannot be negative.");
        }

        if (options.Timeout <= TimeSpan.Zero || options.Lease <= options.Timeout)
        {
            failures.Add("AssetProcessor:Timeout must be positive and shorter than AssetProcessor:Lease.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
