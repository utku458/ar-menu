using ArMenu.Domain.Common;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;

namespace ArMenu.Domain.Sessions;

/// <summary>Security-relevant: a refresh token was replayed, which usually means it was stolen.</summary>
public sealed record RefreshTokenReuseDetectedDomainEvent(TenantId TenantId, UserId UserId, UserSessionId SessionId) : IDomainEvent;
