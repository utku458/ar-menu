namespace ArMenu.Domain.Sessions;

public enum SessionRevocationReason
{
    /// <summary>The user signed out.</summary>
    SignedOut = 0,

    /// <summary>An already rotated refresh token was presented again: the token was most likely stolen.</summary>
    RefreshTokenReused = 1,

    /// <summary>The user lost access to the tenant (membership removed, account locked).</summary>
    AccessRevoked = 2,

    /// <summary>The password was reset after the session started.</summary>
    PasswordReset = 3,
}
