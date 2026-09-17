using ArMenu.Domain.Common;

namespace ArMenu.Domain.Tenants;

public sealed record TenantCreatedDomainEvent(TenantId TenantId) : IDomainEvent;

public sealed record TenantSuspendedDomainEvent(TenantId TenantId) : IDomainEvent;

public sealed record TenantReactivatedDomainEvent(TenantId TenantId) : IDomainEvent;

public sealed record TenantClosedDomainEvent(TenantId TenantId) : IDomainEvent;
