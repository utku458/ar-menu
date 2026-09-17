using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Authentication.RefreshSession;

/// <summary>Exchanges a refresh token for a new access token and a new (rotated) refresh token.</summary>
public sealed record RefreshSessionCommand(string RefreshToken) : ICommand<Result<AuthenticationResult>>
{
    public override string ToString() => "RefreshSessionCommand { *** }";
}
