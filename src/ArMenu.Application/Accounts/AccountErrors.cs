using ArMenu.Application.Common.Validation;
using ArMenu.Domain.Common;

namespace ArMenu.Application.Accounts;

public static class AccountErrors
{
    /// <summary>
    /// A field error, not 401: the person is signed in, and a 401 would end their session over a typo.
    /// </summary>
    public static readonly Error PasswordIncorrect = new ValidationError(
        [new FieldError("Password", "account.password_incorrect", "The password is incorrect.")]);

    public static readonly Error TooManyAttempts = Error.Conflict(
        "account.too_many_attempts", "Too many incorrect passwords. Try again in a few minutes.");

    public static readonly Error OwnershipTransferRequired = Error.Conflict(
        "account.ownership_transfer_required",
        "You own a business that has other members. Hand it over to one of them before deleting your account.");
}
