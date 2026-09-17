using ArMenu.Domain.Common;

namespace ArMenu.Application.Team;

public static class TeamErrors
{
    public static readonly Error LanguageUnsupported = Error.Validation(
        "team.language_unsupported", "Invitation e-mails are written in Turkish (tr) or English (en).");

    public static readonly Error EmailNotVerified = Error.Conflict(
        "team.email_not_verified", "Confirm your e-mail address before inviting people to the team.");

    public static readonly Error PasswordTooShort = Error.Validation(
        "auth.password_too_short", FormattableString.Invariant($"Passwords must be at least {Authentication.PasswordPolicy.MinLength} characters long."));
}
