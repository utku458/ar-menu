namespace ArMenu.Infrastructure.Persistence;

/// <summary>Persistence-only properties that are deliberately kept out of the domain model.</summary>
internal static class ShadowProperties
{
    /// <summary>Optimistic concurrency token mapped to PostgreSQL's <c>xmin</c> system column.</summary>
    public const string RowVersion = nameof(RowVersion);
}
