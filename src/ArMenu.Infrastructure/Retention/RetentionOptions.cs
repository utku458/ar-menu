namespace ArMenu.Infrastructure.Retention;

public sealed class RetentionOptions
{
    public const string SectionName = "Retention";

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// How long a closed business's data is kept before it is deleted: time to notice a deletion someone did not intend,
    /// short enough that nothing is kept without a reason.
    /// </summary>
    public TimeSpan ClosedBusinessRetention { get; set; } = TimeSpan.FromDays(30);
}
