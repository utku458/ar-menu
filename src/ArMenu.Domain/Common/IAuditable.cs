namespace ArMenu.Domain.Common;

/// <summary>
/// Marks an entity whose creation and modification timestamps are maintained by the persistence layer.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }

    DateTimeOffset? UpdatedAt { get; }
}
