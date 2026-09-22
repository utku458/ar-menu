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

    /// <summary>The sign-in name of a user-name account (the OpenID Connect claim for it). Absent for e-mail accounts.</summary>
    public const string UserName = "preferred_username";

    /// <summary>
    /// Present, and <c>true</c>, only on the platform administrator's tokens. It unlocks the platform endpoints; it
    /// does not widen access inside a business, where the administrator holds the owner's role like anyone else.
    /// </summary>
    public const string PlatformAdmin = "platform_admin";
}
