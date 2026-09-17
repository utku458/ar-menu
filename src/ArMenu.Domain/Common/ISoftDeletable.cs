namespace ArMenu.Domain.Common;

/// <summary>
/// Marks an entity whose deletion is recorded instead of physically removing the row,
/// so deleted menu content can be restored and historical references stay intact.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }

    DateTimeOffset? DeletedAt { get; }
}
