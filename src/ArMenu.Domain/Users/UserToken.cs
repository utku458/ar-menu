using ArMenu.Domain.Common;

namespace ArMenu.Domain.Users;

/// <summary>
/// A single-use link e-mailed to a user: to reset the password or to verify the address. Users are global, so their
/// tokens are too; a token names its user, never a tenant.
/// </summary>
public sealed class UserToken : AggregateRoot<UserTokenId>, IAuditable
{
    public static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromHours(1);
    public static readonly TimeSpan EmailVerificationLifetime = TimeSpan.FromDays(3);

    private UserToken(UserTokenId id, UserId userId, UserTokenPurpose purpose, UserTokenHash hash, DateTimeOffset expiresAt)
        : base(id)
    {
        UserId = userId;
        Purpose = purpose;
        Hash = hash;
        ExpiresAt = expiresAt;
    }

#pragma warning disable CS8618 // Used by EF Core; every property is populated during materialization.
    private UserToken()
    {
    }
#pragma warning restore CS8618

    public UserId UserId { get; private init; }

    public UserTokenPurpose Purpose { get; private init; }

    public UserTokenHash Hash { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    /// <summary>When the link was used, or replaced by a newer link for the same purpose.</summary>
    public DateTimeOffset? ConsumedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static UserToken Issue(UserId userId, UserTokenPurpose purpose, UserTokenHash hash, DateTimeOffset now)
    {
        Guard.NotDefault(userId);
        ArgumentNullException.ThrowIfNull(hash);

        var lifetime = purpose switch
        {
            UserTokenPurpose.PasswordReset => PasswordResetLifetime,
            UserTokenPurpose.EmailVerification => EmailVerificationLifetime,
            _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null),
        };

        return new UserToken(UserTokenId.New(), userId, purpose, hash, now + lifetime);
    }

    /// <summary>Uses the link: it must be for <paramref name="purpose"/>, carry the right secret, and be unused and current.</summary>
    public Result Consume(UserTokenPurpose purpose, UserTokenHash presented, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(presented);

        // A verification link must not reset a password, whatever its secret.
        if (purpose != Purpose || !Hash.Matches(presented))
        {
            return UserTokenErrors.InvalidLink;
        }

        if (ConsumedAt is not null)
        {
            return UserTokenErrors.AlreadyUsed;
        }

        if (now >= ExpiresAt)
        {
            return UserTokenErrors.Expired;
        }

        ConsumedAt = now;
        return Result.Success();
    }

    /// <summary>Retires an unused link because a newer one was sent.</summary>
    public void Supersede(DateTimeOffset now) => ConsumedAt ??= now;
}
