using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Authentication.VerifyEmail;

/// <summary>Confirms that the account's address receives mail, with the link e-mailed to it.</summary>
public sealed record VerifyEmailCommand(string Token) : ICommand<Result>
{
    public override string ToString() => "VerifyEmailCommand { *** }";
}
