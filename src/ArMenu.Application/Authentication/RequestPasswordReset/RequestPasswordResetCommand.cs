using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Authentication.RequestPasswordReset;

/// <summary>
/// E-mails a password reset link if the address has an account. The answer is the same either way, and the e-mail is
/// sent in the background, so neither the response nor its timing tells whether an account exists.
/// </summary>
public sealed record RequestPasswordResetCommand(string Email, string? Language) : ICommand<Result>
{
    public override string ToString() => "RequestPasswordResetCommand { *** }";
}
