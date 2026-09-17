namespace ArMenu.Infrastructure.Assets;

public sealed class AssetCleanupOptions
{
    public const string SectionName = "AssetCleanup";

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Files younger than this are never deleted: an upload waiting to be attached, or a model being processed, must not
    /// disappear under the business's hands.
    /// </summary>
    public TimeSpan GracePeriod { get; set; } = TimeSpan.FromDays(1);
}
