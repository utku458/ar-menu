using ArMenu.Domain.Common;

namespace ArMenu.Domain.Users;

/// <summary>
/// A person who can sign in to manage one or more tenants. Users are global (one identity across tenants);
/// what they may do inside a tenant is defined by a tenant-scoped membership.
/// </summary>
public sealed class User : AggregateRoot<UserId>, IAuditable
{
    public const int FullNameMaxLength = 100;
    public const int MaxFailedSignInAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>Not in any hash format, so no password verifies against it.</summary>
    private const string ErasedPasswordHash = "erased";

    private User(UserId id, Email email, string fullName, string passwordHash)
        : base(id)
    {
        Email = email;
        FullName = fullName;
        PasswordHash = passwordHash;
    }

#pragma warning disable CS8618 // Used by EF Core; every property is populated during materialization.
    private User()
    {
    }
#pragma warning restore CS8618

    public Email Email { get; private set; }

    public string FullName { get; private set; }

    /// <summary>Opaque, versioned password hash. The domain never sees or stores plain-text passwords.</summary>
    public string PasswordHash { get; private set; }

    public int FailedSignInAttempts { get; private set; }

    public DateTimeOffset? LockedOutUntil { get; private set; }

    public DateTimeOffset? LastSignedInAt { get; private set; }

    /// <summary>When the person proved they receive mail at <see cref="Email"/>; <see langword="null"/> until then.</summary>
    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    /// <summary>When the password was last reset. Sessions started before it are no longer honored.</summary>
    public DateTimeOffset? PasswordChangedAt { get; private set; }

    /// <summary>When the person deleted their account; the row remains only as an anonymous reference.</summary>
    public DateTimeOffset? ErasedAt { get; private set; }

    public bool IsEmailVerified => EmailVerifiedAt is not null;

    public bool IsErased => ErasedAt is not null;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Result<User> Register(Email email, string fullName, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return UserErrors.FullNameRequired;
        }

        var trimmedName = fullName.Trim();
        return trimmedName.Length <= FullNameMaxLength
            ? new User(UserId.New(), email, trimmedName, passwordHash)
            : UserErrors.FullNameTooLong;
    }

    public bool IsLockedOut(DateTimeOffset now) => LockedOutUntil > now;

    /// <summary>
    /// Counts a failed sign-in. After <see cref="MaxFailedSignInAttempts"/> consecutive failures the account is locked for
    /// <see cref="LockoutDuration"/>, which turns online password guessing into a very slow process.
    /// </summary>
    public void RecordFailedSignIn(DateTimeOffset now)
    {
        if (IsLockedOut(now))
        {
            return;
        }

        FailedSignInAttempts++;

        if (FailedSignInAttempts >= MaxFailedSignInAttempts)
        {
            LockedOutUntil = now + LockoutDuration;
            FailedSignInAttempts = 0;
        }
    }

    public void RecordSuccessfulSignIn(DateTimeOffset now)
    {
        FailedSignInAttempts = 0;
        LockedOutUntil = null;
        LastSignedInAt = now;
    }

    /// <summary>Records that a link sent to the address was used. Keeps the first verification time.</summary>
    public void MarkEmailVerified(DateTimeOffset now) => EmailVerifiedAt ??= now;

    /// <summary>
    /// Sets a new password after the person proved control of the address. Lockout ends, and every session started
    /// before <paramref name="now"/> stops being refreshable (see <see cref="Sessions.UserSession"/>).
    /// </summary>
    public void ResetPassword(string passwordHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        PasswordHash = passwordHash;
        PasswordChangedAt = now;
        FailedSignInAttempts = 0;
        LockedOutUntil = null;
        MarkEmailVerified(now);
    }

    /// <summary>Whether a session started at <paramref name="sessionStartedAt"/> predates the latest password reset.</summary>
    public bool IsSessionOutdated(DateTimeOffset sessionStartedAt) => PasswordChangedAt > sessionStartedAt;

    /// <summary>
    /// Removes everything that identifies the person, when they delete their account. The row stays, anonymous, so that
    /// what they did in a business's history still points at "a deleted account" instead of breaking or naming them.
    /// The address becomes free for a new account; no password can match; sessions stop being refreshable.
    /// </summary>
    public void Erase(DateTimeOffset now)
    {
        if (IsErased)
        {
            return;
        }

        Email = Email.Create(FormattableString.Invariant($"erased-{Id.Value:N}@erased.invalid")).Value;
        FullName = string.Empty;
        PasswordHash = ErasedPasswordHash;
        EmailVerifiedAt = null;
        LastSignedInAt = null;
        FailedSignInAttempts = 0;
        LockedOutUntil = null;
        PasswordChangedAt = now;
        ErasedAt = now;
    }

    /// <summary>Replaces the stored hash, e.g. to upgrade it to stronger hashing parameters after a successful sign-in.</summary>
    public void ChangePasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }
}
