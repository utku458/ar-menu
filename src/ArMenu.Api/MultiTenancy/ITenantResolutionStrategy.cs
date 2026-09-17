using ArMenu.Application.MultiTenancy;

namespace ArMenu.Api.MultiTenancy;

/// <summary>
/// Determines a request's tenant from one specific source. Strategies are attached to endpoints as metadata, so every
/// endpoint states explicitly where its tenant comes from, and an endpoint without a strategy is never tenant-bound.
/// </summary>
/// <remarks>
/// A global fallback chain ("try the claim, else the route, else a header") was rejected on purpose: it lets a request
/// pick the source that suits it, which is exactly how tenant-spoofing bugs are born.
/// </remarks>
internal interface ITenantResolutionStrategy
{
    ValueTask<TenantInfo?> ResolveAsync(HttpContext httpContext, ITenantLookup tenantLookup);
}
