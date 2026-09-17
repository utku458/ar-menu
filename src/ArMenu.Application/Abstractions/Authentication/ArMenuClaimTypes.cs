namespace ArMenu.Application.Abstractions.Authentication;

/// <summary>Claim names written into access tokens by the issuer and read by the API.</summary>
public static class ArMenuClaimTypes
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string EmailVerified = "email_verified";
    public const string Name = "name";
    public const string Role = "role";
    public const string SessionId = "sid";

    /// <summary>Identifier of the tenant the token was issued for. Tokens are always scoped to a single tenant.</summary>
    public const string TenantId = "tenant_id";
}
