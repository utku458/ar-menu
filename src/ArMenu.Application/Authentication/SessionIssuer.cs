using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Sessions;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using Microsoft.Extensions.Options;

namespace ArMenu.Application.Authentication;

/// <summary>Starts sessions and produces the token pair for a user acting in one tenant.</summary>
public sealed class SessionIssuer(
    IUserSessionRepository sessions,
    IRefreshTokenCodec refreshTokenCodec,
    IAccessTokenIssuer accessTokenIssuer,
    TimeProvider timeProvider,
    IOptions<UserSessionOptions> sessionOptions)
{
    public AuthenticationResult StartSession(User user, TenantMembership membership)
    {
        ArgumentNullException.ThrowIfNull(membership);

        return StartSession(user, membership.TenantId, membership.Role);
    }

    /// <summary>Starts a session in <paramref name="tenantId"/> with <paramref name="role"/>.</summary>
    /// <remarks>
    /// A membership for the pair must exist: <c>user_sessions</c> has a foreign key to <c>tenant_memberships</c>, so
    /// the database refuses a session for someone who is not a member.
    /// </remarks>
    public AuthenticationResult StartSession(User user, TenantId tenantId, TenantRole role)
    {
        ArgumentNullException.ThrowIfNull(user);

        var secret = refreshTokenCodec.GenerateSecret();
        var session = UserSession.Start(
            tenantId,
            user.Id,
            secret.Hash,
            timeProvider.GetUtcNow(),
            sessionOptions.Value.ToLifetime());

        sessions.Add(session);

        return CreateResult(user, tenantId, role, session, secret);
    }

    public AuthenticationResult CreateResult(User user, TenantMembership membership, UserSession session, RefreshTokenSecret secret)
    {
        ArgumentNullException.ThrowIfNull(membership);

        return CreateResult(user, membership.TenantId, membership.Role, session, secret);
    }

    /// <summary>
    /// An access token for a tenant the caller has no session in, carrying the session they do have. Used where a
    /// refreshable session would be wrong: the platform administrator working inside a business it is not a member
    /// of, whose access is a quarter of an hour at a time rather than a fortnight (see EnterBusinessCommandHandler).
    /// </summary>
    public AccessToken IssueAccessToken(User user, TenantId tenantId, TenantRole role, UserSessionId sessionId)
    {
        ArgumentNullException.ThrowIfNull(user);

        return accessTokenIssuer.Issue(new AccessTokenSubject(
            user.Id,
            user.Email.Value,
            user.IsEmailVerified,
            user.FullName,
            tenantId,
            role,
            sessionId,
            user.UserName?.Value,
            user.IsPlatformAdmin));
    }

    public AuthenticationResult CreateResult(
        User user,
        TenantId tenantId,
        TenantRole role,
        UserSession session,
        RefreshTokenSecret secret)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(secret);

        var accessToken = accessTokenIssuer.Issue(new AccessTokenSubject(
            user.Id,
            user.Email.Value,
            user.IsEmailVerified,
            user.FullName,
            tenantId,
            role,
            session.Id,
            user.UserName?.Value,
            user.IsPlatformAdmin));

        return new AuthenticationResult(accessToken, refreshTokenCodec.Encode(session.Id, secret), session.ExpiresAt);
    }
}
