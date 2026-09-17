using ArMenu.Domain.Common;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;

namespace ArMenu.Domain.Sessions;

/// <summary>
/// A signed-in session of a user in one tenant, backed by a rotating refresh token.
/// </summary>
/// <remarks>
/// Every refresh replaces the token (rotation). Presenting a token that was already replaced means two parties hold the
/// same token, so the whole session is revoked (reuse detection, OAuth 2.0 Security BCP). One row per session keeps
/// storage bounded while still detecting reuse of <em>any</em> older token.
/// </remarks>
public sealed class UserSession : AggregateRoot<UserSessionId>, ITenantScoped, IAuditable
{
    /// <summary>
    /// Window in which the token replaced by the latest rotation is rejected without revoking the session. Two browser
    /// tabs refreshing at the same moment is normal behavior, not theft.
    /// </summary>
    public static readonly TimeSpan ConcurrentRefreshGracePeriod = TimeSpan.FromSeconds(30);

    private UserSession(
        UserSessionId id,
        TenantId tenantId,
        UserId userId,
        RefreshTokenHash refreshTokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset absoluteExpiresAt)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        RefreshTokenHash = refreshTokenHash;
        ExpiresAt = expiresAt;
        AbsoluteExpiresAt = absoluteExpiresAt;
    }

#pragma warning disable CS8618 // Used by EF Core; every property is populated during materialization.
    private UserSession()
    {
    }
#pragma warning restore CS8618

    public TenantId TenantId { get; private init; }

    public UserId UserId { get; private init; }

    public RefreshTokenHash RefreshTokenHash { get; private set; }

    public RefreshTokenHash? PreviousRefreshTokenHash { get; private set; }

    public DateTimeOffset? RotatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset AbsoluteExpiresAt { get; private init; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public SessionRevocationReason? RevocationReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static UserSession Start(
        TenantId tenantId,
        UserId userId,
        RefreshTokenHash refreshTokenHash,
        DateTimeOffset now,
        SessionLifetime lifetime)
    {
        Guard.NotDefault(tenantId);
        Guard.NotDefault(userId);
        ArgumentNullException.ThrowIfNull(refreshTokenHash);
        ArgumentNullException.ThrowIfNull(lifetime);

        return new UserSession(
            UserSessionId.New(),
            tenantId,
            userId,
            refreshTokenHash,
            expiresAt: now + lifetime.IdleTimeout,
            absoluteExpiresAt: now + lifetime.AbsoluteLifetime);
    }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    /// <summary>
    /// Exchanges the presented refresh token for <paramref name="replacement"/>. A failure caused by token reuse revokes
    /// the session: callers must persist the aggregate even when this method fails.
    /// </summary>
    public Result Rotate(RefreshTokenHash presented, RefreshTokenHash replacement, DateTimeOffset now, SessionLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(presented);
        ArgumentNullException.ThrowIfNull(replacement);
        ArgumentNullException.ThrowIfNull(lifetime);

        if (RevokedAt is not null)
        {
            return SessionErrors.Revoked;
        }

        if (now >= ExpiresAt)
        {
            return SessionErrors.Expired;
        }

        if (!presented.Matches(RefreshTokenHash))
        {
            if (IsWithinConcurrentRefreshGracePeriod(presented, now))
            {
                return SessionErrors.RefreshTokenSuperseded;
            }

            Revoke(now, SessionRevocationReason.RefreshTokenReused);
            RaiseDomainEvent(new RefreshTokenReuseDetectedDomainEvent(TenantId, UserId, Id));
            return SessionErrors.RefreshTokenReused;
        }

        PreviousRefreshTokenHash = RefreshTokenHash;
        RefreshTokenHash = replacement;
        RotatedAt = now;

        var idleDeadline = now + lifetime.IdleTimeout;
        ExpiresAt = idleDeadline < AbsoluteExpiresAt ? idleDeadline : AbsoluteExpiresAt;

        return Result.Success();
    }

    /// <summary>Whether <paramref name="presented"/> is the current refresh token. Used before sign-out.</summary>
    public bool IsCurrentRefreshToken(RefreshTokenHash presented)
    {
        ArgumentNullException.ThrowIfNull(presented);
        return presented.Matches(RefreshTokenHash);
    }

    public void Revoke(DateTimeOffset now, SessionRevocationReason reason)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
        RevocationReason = reason;
    }

    private bool IsWithinConcurrentRefreshGracePeriod(RefreshTokenHash presented, DateTimeOffset now) =>
        PreviousRefreshTokenHash is not null &&
        RotatedAt is not null &&
        now - RotatedAt.Value < ConcurrentRefreshGracePeriod &&
        presented.Matches(PreviousRefreshTokenHash);
}
