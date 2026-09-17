namespace ArMenu.Domain.Users;

/// <summary>What a link e-mailed to a user lets its holder do.</summary>
public enum UserTokenPurpose
{
    /// <summary>Set a new password. Short-lived: it is as powerful as the password itself.</summary>
    PasswordReset = 0,

    /// <summary>Confirm the address receives mail.</summary>
    EmailVerification = 1,
}
