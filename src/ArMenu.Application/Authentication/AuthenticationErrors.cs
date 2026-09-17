using ArMenu.Domain.Common;

namespace ArMenu.Application.Authentication;

public static class AuthenticationErrors
{
    /// <summary>
    /// Deliberately the same for an unknown e-mail, a wrong password, a locked account and a user who is not a member of
    /// the tenant, so sign-in responses cannot be used to enumerate accounts.
    /// </summary>
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "auth.invalid_credentials", "The e-mail address or password is incorrect.");

    public static readonly Error InvalidRefreshToken = Error.Unauthorized(
        "auth.invalid_refresh_token", "The session is no longer valid. Sign in again.");
}
