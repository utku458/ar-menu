using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Authentication.SignIn;

/// <summary>Signs a user in to the tenant bound to the current scope (the workspace in the URL).</summary>
public sealed record SignInCommand(string Email, string Password) : ICommand<Result<AuthenticationResult>>
{
    public override string ToString() => "SignInCommand { *** }";
}
