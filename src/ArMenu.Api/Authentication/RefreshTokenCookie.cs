using ArMenu.Application.MultiTenancy;

namespace ArMenu.Api.Authentication;

/// <summary>
/// The refresh token travels only in an HttpOnly, Secure, SameSite=Strict cookie: scripts (and therefore XSS) can never
/// read it, cross-site requests never carry it, and its path limits it to the tenant's own authentication endpoints.
/// </summary>
internal static class RefreshTokenCookie
{
    public const string Name = "armenu_refresh";

    public static void Write(HttpContext httpContext, string refreshToken, DateTimeOffset expiresAt) =>
        httpContext.Response.Cookies.Append(Name, refreshToken, CreateOptions(httpContext, expiresAt));

    public static string? Read(HttpContext httpContext) => httpContext.Request.Cookies[Name];

    public static void Delete(HttpContext httpContext) =>
        httpContext.Response.Cookies.Delete(Name, CreateOptions(httpContext, expiresAt: null));

    private static CookieOptions CreateOptions(HttpContext httpContext, DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        IsEssential = true,
        Expires = expiresAt,
        Path = PathFor(httpContext),
    };

    // Per-tenant path: sessions of different workspaces in the same browser never overwrite each other.
    private static string PathFor(HttpContext httpContext)
    {
        var tenant = httpContext.RequestServices.GetRequiredService<ITenantContext>().RequireTenant();
        return $"{httpContext.Request.PathBase}/api/v1/tenants/{tenant.Slug}/auth";
    }
}
