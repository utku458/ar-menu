using ArMenu.Domain.Memberships;
using ArMenu.Domain.Sessions;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;

namespace ArMenu.Application.Abstractions.Authentication;

/// <summary>Issues short-lived access tokens, always scoped to exactly one tenant.</summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(AccessTokenSubject subject);
}

public sealed record AccessTokenSubject(
    UserId UserId,
    string Email,
    bool EmailVerified,
    string FullName,
    TenantId TenantId,
    TenantRole Role,
    UserSessionId SessionId,
    string? UserName = null,
    bool IsPlatformAdmin = false);

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt)
{
    public override string ToString() => $"AccessToken {{ ExpiresAt = {ExpiresAt:O} }}";
}
