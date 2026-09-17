using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Sessions;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArMenu.Application.Authentication.RefreshSession;

public sealed partial class RefreshSessionCommandHandler(
    IUserSessionRepository sessions,
    IUserRepository users,
    ITenantMembershipRepository memberships,
    IRefreshTokenCodec refreshTokenCodec,
    SessionIssuer sessionIssuer,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<UserSessionOptions> sessionOptions,
    ILogger<RefreshSessionCommandHandler> logger)
    : ICommandHandler<RefreshSessionCommand, Result<AuthenticationResult>>
{
    public async ValueTask<Result<AuthenticationResult>> Handle(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!refreshTokenCodec.TryDecode(command.RefreshToken, out var sessionId, out var presentedHash))
        {
            return AuthenticationErrors.InvalidRefreshToken;
        }

        // Scoped to the tenant in the URL: a token of another tenant simply does not exist here.
        var session = await sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return AuthenticationErrors.InvalidRefreshToken;
        }

        var now = timeProvider.GetUtcNow();
        var replacement = refreshTokenCodec.GenerateSecret();
        var rotation = session.Rotate(presentedHash, replacement.Hash, now, sessionOptions.Value.ToLifetime());

        if (rotation.IsFailure)
        {
            if (rotation.Error == SessionErrors.RefreshTokenReused)
            {
                LogRefreshTokenReused(logger, session.TenantId, session.UserId, session.Id);
            }

            // Persists the revocation performed by reuse detection.
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AuthenticationErrors.InvalidRefreshToken;
        }

        // Access is re-evaluated on every refresh: removed members lose it, role changes take effect.
        var user = await users.GetByIdAsync(session.UserId, cancellationToken);
        var membership = await memberships.GetByUserIdAsync(session.UserId, cancellationToken);
        if (user is null || membership is null)
        {
            session.Revoke(now, SessionRevocationReason.AccessRevoked);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AuthenticationErrors.InvalidRefreshToken;
        }

        if (user.IsSessionOutdated(session.CreatedAt))
        {
            session.Revoke(now, SessionRevocationReason.PasswordReset);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AuthenticationErrors.InvalidRefreshToken;
        }

        var authentication = sessionIssuer.CreateResult(user, membership, session, replacement);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return authentication;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Refresh token reuse detected for user {UserId} in tenant {TenantId}; session {SessionId} revoked")]
    private static partial void LogRefreshTokenReused(ILogger logger, TenantId tenantId, UserId userId, UserSessionId sessionId);
}
