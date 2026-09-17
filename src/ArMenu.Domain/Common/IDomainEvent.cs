namespace ArMenu.Domain.Common;

/// <summary>
/// Something business-relevant that happened inside an aggregate (e.g. a tenant was suspended).
/// Events are raised by aggregates and published after the aggregate has been persisted.
/// </summary>
public interface IDomainEvent;
