using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Tenants;
using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.MultiTenancy;

public sealed class TenantLookupTests(PostgresDatabaseFixture database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Finds_tenants_by_slug_and_by_id()
    {
        var seeded = await database.Services.SeedTenantAsync(Ct);
        await using var scope = database.Services.BeginScope();
        var lookup = scope.Get<ITenantLookup>();

        var bySlug = await lookup.FindBySlugAsync(TenantSlug.Create(seeded.Tenant.Slug).Value, Ct);
        var byId = await lookup.FindByIdAsync(seeded.Tenant.Id, Ct);

        bySlug.ShouldNotBeNull().Id.ShouldBe(seeded.Tenant.Id);
        byId.ShouldNotBeNull().Slug.ShouldBe(seeded.Tenant.Slug);
    }

    [Fact]
    public async Task Returns_null_for_unknown_tenants()
    {
        await using var scope = database.Services.BeginScope();
        var lookup = scope.Get<ITenantLookup>();

        (await lookup.FindBySlugAsync(TenantSlug.Create("no-such-restaurant").Value, Ct)).ShouldBeNull();
        (await lookup.FindByIdAsync(TenantId.New(), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Serves_repeated_lookups_from_cache()
    {
        var seeded = await database.Services.SeedTenantAsync(Ct);
        var slug = TenantSlug.Create(seeded.Tenant.Slug).Value;
        await using var scope = database.Services.BeginScope();
        var lookup = scope.Get<ITenantLookup>();

        var first = await lookup.FindBySlugAsync(slug, Ct);
        await database.ExecuteAsOwnerAsync($"UPDATE tenants SET name = 'Renamed Behind The Cache' WHERE id = '{seeded.Tenant.Id.Value}'", Ct);
        var second = await lookup.FindBySlugAsync(slug, Ct);

        second.ShouldNotBeNull().Name.ShouldBe(first.ShouldNotBeNull().Name);
    }
}
