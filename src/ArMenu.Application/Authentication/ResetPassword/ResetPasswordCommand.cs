using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Authentication.ResetPassword;

/// <summary>
/// Sets a new password with a reset link. Every existing session of the account, in every business, stops being
/// refreshable; the person signs in again with the new password.
/// </summary>
public sealed record ResetPasswordCommand(string Token, string Password) : ICommand<Result>
{
    public override string ToString() => "ResetPasswordCommand { *** }";
}
