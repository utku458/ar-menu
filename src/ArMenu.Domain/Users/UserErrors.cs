using ArMenu.Domain.Common;

namespace ArMenu.Domain.Users;

public static class UserErrors
{
    public static readonly Error EmailRequired = Error.Validation(
        "user.email_required", "An e-mail address is required.");

    public static readonly Error EmailInvalid = Error.Validation(
        "user.email_invalid", "The e-mail address is not valid.");

    public static readonly Error EmailTaken = Error.Conflict(
        "user.email_taken", "An account with this e-mail address already exists.");

    public static readonly Error FullNameRequired = Error.Validation(
        "user.full_name_required", "A full name is required.");

    public static readonly Error FullNameTooLong = Error.Validation(
        "user.full_name_too_long", FormattableString.Invariant($"Full names cannot exceed {User.FullNameMaxLength} characters."));
}
