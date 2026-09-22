using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Memberships;
using Microsoft.AspNetCore.Authorization;

namespace ArMenu.Api.Authentication;

internal static class AuthorizationPolicies
{
    /// <summary>Any member of the tenant: read the menu, toggle availability.</summary>
    public const string MenuStaff = "menu:staff";

    /// <summary>Members allowed to change menu content.</summary>
    public const string MenuEditor = "menu:editor";

    /// <summary>Members allowed to invite, change and remove members: the owner.</summary>
    public const string TeamAdmin = "team:admin";

    /// <summary>
    /// The platform administrator: opens businesses and enters them. Checked by the token's claim here, and again
    /// against the account by every command that changes something (see <c>PlatformAdministrator</c>).
    /// </summary>
    public const string PlatformAdmin = "platform:admin";

    public static IServiceCollection AddArMenuAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            // Secure by default: an endpoint someone forgets to protect still requires a signed-in user.
            // Public endpoints must opt out explicitly with AllowAnonymous().
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(MenuStaff, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(nameof(TenantRole.Owner), nameof(TenantRole.Manager), nameof(TenantRole.Staff)))
            .AddPolicy(MenuEditor, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(nameof(TenantRole.Owner), nameof(TenantRole.Manager)))
            .AddPolicy(TeamAdmin, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(nameof(TenantRole.Owner)))
            .AddPolicy(PlatformAdmin, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(ArMenuClaimTypes.PlatformAdmin, "true"));

        return services;
    }
}
