using ArMenu.Application.MultiTenancy;
using ArMenu.Infrastructure.Persistence;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.IntegrationTests.Persistence;

/// <summary>Read isolation enforced by EF Core's named global query filters.</summary>
public sealed class TenantQueryFilterTests(PostgresDatabaseFixture database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Queries_only_return_rows_of_the_bound_tenant()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);

        await using var scope = database.Services.BeginScope(burgerLab.Tenant);

        var categoryTenants = await scope.Db.MenuCategories.Select(category => category.TenantId).Distinct().ToListAsync(Ct);
        var itemIds = await scope.Db.MenuItems.Select(item => item.Id).ToListAsync(Ct);

        categoryTenants.ShouldBe([burgerLab.Tenant.Id]);
        itemIds.ShouldBe([burgerLab.ItemId]);
        itemIds.ShouldNotContain(fishRestaurant.ItemId);
    }

    [Fact]
    public async Task The_query_filter_isolates_tenants_on_its_own_without_row_level_security()
    {
        // The schema owner is a superuser and bypasses row-level security, leaving the EF Core filter as the only guard.
        await using var ownerServices = new TestServices(database.OwnerConnectionString);
        var burgerLab = await ownerServices.SeedTenantAsync(Ct);
        var fishRestaurant = await ownerServices.SeedTenantAsync(Ct);

        await using var scope = ownerServices.BeginScope(burgerLab.Tenant);

        var filteredItemIds = await scope.Db.MenuItems.Select(item => item.Id).ToListAsync(Ct);
        var unfilteredItemIds = await scope.Db.MenuItems.IgnoreQueryFilters().Select(item => item.Id).ToListAsync(Ct);

        filteredItemIds.ShouldBe([burgerLab.ItemId]);
        unfilteredItemIds.ShouldContain(fishRestaurant.ItemId, "the connection must really bypass row-level security");
    }

    [Fact]
    public async Task Another_tenants_row_cannot_be_found_even_by_its_exact_id()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);

        await using var scope = database.Services.BeginScope(burgerLab.Tenant);

        var item = await scope.Db.MenuItems.SingleOrDefaultAsync(menuItem => menuItem.Id == fishRestaurant.ItemId, Ct);

        item.ShouldBeNull();
    }

    [Fact]
    public async Task Tenant_scoped_queries_fail_loudly_when_no_tenant_is_bound()
    {
        await database.Services.SeedTenantAsync(Ct);

        await using var scope = database.Services.BeginScope(tenant: null);

        await Should.ThrowAsync<TenantNotResolvedException>(() => scope.Db.MenuItems.ToListAsync(Ct));
    }

    [Fact]
    public async Task Disabling_the_soft_delete_filter_keeps_the_tenant_filter_in_force()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);
        await SoftDeleteItemAsync(burgerLab);
        await SoftDeleteItemAsync(fishRestaurant);

        await using var scope = database.Services.BeginScope(burgerLab.Tenant);

        (await scope.Db.MenuItems.CountAsync(Ct)).ShouldBe(0);

        var includingDeleted = await scope.Db.MenuItems
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .Select(item => item.Id)
            .ToListAsync(Ct);

        includingDeleted.ShouldBe([burgerLab.ItemId]);
    }

    private async Task SoftDeleteItemAsync(SeededTenant seeded)
    {
        await using var scope = database.Services.BeginScope(seeded.Tenant);
        var item = await scope.Db.MenuItems.SingleAsync(menuItem => menuItem.Id == seeded.ItemId, Ct);
        scope.Db.MenuItems.Remove(item);
        await scope.Db.SaveChangesAsync(Ct);
    }
}
