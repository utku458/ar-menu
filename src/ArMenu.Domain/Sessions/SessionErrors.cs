using ArMenu.Domain.Common;

namespace ArMenu.Domain.Sessions;

public static class SessionErrors
{
    public static readonly Error Revoked = Error.Unauthorized(
        "session.revoked", "The session has been revoked.");

    public static readonly Error Expired = Error.Unauthorized(
        "session.expired", "The session has expired.");

    public static readonly Error RefreshTokenReused = Error.Unauthorized(
        "session.refresh_token_reused", "A refresh token was used more than once; the session has been revoked.");

    public static readonly Error RefreshTokenSuperseded = Error.Unauthorized(
        "session.refresh_token_superseded", "The refresh token was just replaced by a concurrent request.");

    public static readonly Error RefreshTokenHashInvalid = Error.Validation(
        "session.refresh_token_hash_invalid", "A refresh token hash must be a base64url-encoded SHA-256 digest.");
}
