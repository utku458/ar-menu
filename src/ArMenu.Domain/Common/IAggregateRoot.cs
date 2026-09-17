namespace ArMenu.Domain.Common;

/// <summary>
/// Non-generic view of an aggregate root, used by infrastructure to collect domain events regardless of identifier type.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
