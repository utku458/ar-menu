using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Sessions;
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
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(membership);

        var secret = refreshTokenCodec.GenerateSecret();
        var session = UserSession.Start(
            membership.TenantId,
            user.Id,
            secret.Hash,
            timeProvider.GetUtcNow(),
            sessionOptions.Value.ToLifetime());

        sessions.Add(session);

        return CreateResult(user, membership, session, secret);
    }

    public AuthenticationResult CreateResult(User user, TenantMembership membership, UserSession session, RefreshTokenSecret secret)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(membership);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(secret);

        var accessToken = accessTokenIssuer.Issue(new AccessTokenSubject(
            user.Id,
            user.Email.Value,
            user.IsEmailVerified,
            user.FullName,
            membership.TenantId,
            membership.Role,
            session.Id));

        return new AuthenticationResult(accessToken, refreshTokenCodec.Encode(session.Id, secret), session.ExpiresAt);
    }
}
