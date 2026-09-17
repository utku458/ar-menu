using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Authentication.SignOut;

/// <summary>Ends the session behind a refresh token. Always succeeds, so it reveals nothing about the token.</summary>
public sealed record SignOutCommand(string? RefreshToken) : ICommand<Result>
{
    public override string ToString() => "SignOutCommand { *** }";
}
