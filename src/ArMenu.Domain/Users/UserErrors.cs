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

    public static readonly Error UserNameRequired = Error.Validation(
        "user.user_name_required", "A user name is required.");

    public static readonly Error UserNameInvalid = Error.Validation(
        "user.user_name_invalid",
        FormattableString.Invariant(
            $"User names are {UserName.MinLength}-{UserName.MaxLength} lower-case letters, digits, dots, hyphens or underscores, starting and ending with a letter or digit."));

    public static readonly Error UserNameTaken = Error.Conflict(
        "user.user_name_taken", "An account with this user name already exists.");
}
