using System.Security.Claims;
using ArMenu.Api.MultiTenancy;
using ArMenu.Api.UnitTests.TestSupport;
using ArMenu.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Http;

namespace ArMenu.Api.UnitTests.MultiTenancy;

public sealed class ClaimsTenantResolutionStrategyTests
{
    private readonly ClaimsTenantResolutionStrategy _strategy = ClaimsTenantResolutionStrategy.Instance;

    [Fact]
    public async Task Resolves_the_tenant_of_the_authenticated_user()
    {
        var tenant = TestTenants.Create("kadikoy-burger-lab");
        var lookup = new FakeTenantLookup(tenant);

        var resolved = await _strategy.ResolveAsync(Request(authenticated: true, tenant.Id.Value.ToString()), lookup);

        resolved.ShouldBe(tenant);
    }

    [Fact]
    public async Task Ignores_tenant_claims_of_unauthenticated_identities()
    {
        var tenant = TestTenants.Create("kadikoy-burger-lab");
        var lookup = new FakeTenantLookup(tenant);

        var resolved = await _strategy.ResolveAsync(Request(authenticated: false, tenant.Id.Value.ToString()), lookup);

        resolved.ShouldBeNull();
        lookup.CallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("kadikoy-burger-lab")]
    public async Task Returns_null_when_the_claim_is_missing_or_not_a_tenant_id(string? claimValue)
    {
        var lookup = new FakeTenantLookup(TestTenants.Create("kadikoy-burger-lab"));

        var resolved = await _strategy.ResolveAsync(Request(authenticated: true, claimValue), lookup);

        resolved.ShouldBeNull();
        lookup.CallCount.ShouldBe(0);
    }

    private static DefaultHttpContext Request(bool authenticated, string? tenantIdClaim)
    {
        Claim[] claims = tenantIdClaim is null ? [] : [new Claim(ArMenuClaimTypes.TenantId, tenantIdClaim)];
        var identity = new ClaimsIdentity(claims, authenticationType: authenticated ? "Bearer" : null);

        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }
}
