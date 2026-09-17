using System.Security.Claims;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Tenants;

namespace ArMenu.Api.MultiTenancy;

/// <summary>
/// Resolves the tenant from the authenticated user's <see cref="ArMenuClaimTypes.TenantId"/> claim.
/// Meant for staff (management) endpoints: the tenant comes from the signed token, never from anything the client can
/// edit (route, query string, header), which rules out tenant spoofing by construction.
/// </summary>
internal sealed class ClaimsTenantResolutionStrategy : ITenantResolutionStrategy
{
    private ClaimsTenantResolutionStrategy()
    {
    }

    public static ClaimsTenantResolutionStrategy Instance { get; } = new();

    public ValueTask<TenantInfo?> ResolveAsync(HttpContext httpContext, ITenantLookup tenantLookup)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(tenantLookup);

        var user = httpContext.User;

        if (user.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(user.FindFirstValue(ArMenuClaimTypes.TenantId), out var tenantId))
        {
            return ValueTask.FromResult<TenantInfo?>(null);
        }

        return tenantLookup.FindByIdAsync(TenantId.From(tenantId), httpContext.RequestAborted);
    }
}
