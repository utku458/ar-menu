using ArMenu.Domain.Common;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Accounts.DeleteAccount;

/// <summary>
/// Deletes the signed-in person's account for good: their personal data is erased, they leave every team, and the
/// businesses only they could run are closed. Refused while they own a business that has other members.
/// </summary>
public sealed record DeleteAccountCommand(UserId UserId, string Password, string? Language) : ICommand<Result>
{
    public override string ToString() => $"DeleteAccountCommand {{ UserId = {UserId}, *** }}";
}
