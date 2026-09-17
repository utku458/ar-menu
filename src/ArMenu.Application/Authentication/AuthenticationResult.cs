using ArMenu.Application.Abstractions.Authentication;

namespace ArMenu.Application.Authentication;

/// <param name="AccessToken">Short-lived bearer token for API calls.</param>
/// <param name="RefreshToken">Opaque token to obtain the next access token. Must never be exposed to scripts.</param>
/// <param name="RefreshTokenExpiresAt">When the session expires if it is not refreshed.</param>
public sealed record AuthenticationResult(AccessToken AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt)
{
    public override string ToString() => $"AuthenticationResult {{ AccessTokenExpiresAt = {AccessToken.ExpiresAt:O} }}";
}
