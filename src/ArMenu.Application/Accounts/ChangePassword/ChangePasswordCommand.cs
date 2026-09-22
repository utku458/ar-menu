using ArMenu.Domain.Common;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Accounts.ChangePassword;

/// <summary>
/// The signed-in person replaces their password, proving they know the current one. Every session ends, the one in
/// use included, so the new password is needed from the next sign-in on — and an old one someone learned is useless.
/// </summary>
public sealed record ChangePasswordCommand(UserId UserId, string CurrentPassword, string NewPassword) : ICommand<Result>
{
    public override string ToString() => $"ChangePasswordCommand {{ UserId = {UserId}, *** }}";
}
