namespace ArMenu.Domain.Common;

/// <summary>
/// An object defined by a stable identity rather than by its attributes.
/// Two entity instances are equal when they are of the same type and share the same identifier.
/// </summary>
/// <typeparam name="TId">Strongly-typed identifier of the entity.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IEquatable<TId>
{
    protected Entity(TId id)
    {
        if (id.Equals(default))
        {
            throw new ArgumentException("An entity identifier cannot be the default value.", nameof(id));
        }

        Id = id;
    }

    /// <summary>Used by EF Core when materializing entities from the database.</summary>
    protected Entity()
    {
    }

    public TId Id { get; private init; }

    public bool Equals(Entity<TId>? other) =>
        other is not null && GetType() == other.GetType() && Id.Equals(other.Id);

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
