using ArMenu.Domain.Common;

namespace ArMenu.Domain.Users;

public static class UserTokenErrors
{
    /// <summary>The same answer for a malformed link, an unknown token and a wrong secret.</summary>
    public static readonly Error InvalidLink = Error.NotFound(
        "user_token.invalid_link", "This link is not valid. Request a new one.");

    public static readonly Error Expired = Error.Conflict(
        "user_token.expired", "This link has expired. Request a new one.");

    public static readonly Error AlreadyUsed = Error.Conflict(
        "user_token.already_used", "This link was already used or replaced by a newer one.");

    public static readonly Error HashInvalid = Error.Validation(
        "user_token.hash_invalid", "The token hash must be 43 base64url characters.");
}
