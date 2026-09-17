using ArMenu.Application.MultiTenancy;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ArMenu.IntegrationTests.Persistence;

/// <summary>Write isolation: the SaveChanges guard and the tenant-safe composite foreign key.</summary>
public sealed class TenantWriteIsolationTests(PostgresDatabaseFixture database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Creating_data_for_another_tenant_is_rejected_before_reaching_the_database()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);

        await using var scope = database.Services.BeginScope(burgerLab.Tenant);
        scope.Db.MenuCategories.Add(TestData.NewCategory(fishRestaurant.Tenant.Id));

        await Should.ThrowAsync<TenantIsolationViolationException>(() => scope.Db.SaveChangesAsync(Ct));
        (await CountCategoriesAsOwnerAsync(fishRestaurant)).ShouldBe(1);
    }

    [Fact]
    public async Task Updating_a_detached_entity_of_another_tenant_is_rejected()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);

        // An entity obtained outside the attacker's scope (e.g. rebuilt from a request body), then attached.
        await using var victimScope = database.Services.BeginScope(fishRestaurant.Tenant);
        var victimItem = await victimScope.Db.MenuItems.AsNoTracking().SingleAsync(item => item.Id == fishRestaurant.ItemId, Ct);

        await using var attackerScope = database.Services.BeginScope(burgerLab.Tenant);
        victimItem.MarkAsSoldOut();
        attackerScope.Db.MenuItems.Update(victimItem);

        await Should.ThrowAsync<TenantIsolationViolationException>(() => attackerScope.Db.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task Deleting_another_tenants_entity_is_rejected()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);

        await using var victimScope = database.Services.BeginScope(fishRestaurant.Tenant);
        var victimCategory = await victimScope.Db.MenuCategories.AsNoTracking().SingleAsync(Ct);

        await using var attackerScope = database.Services.BeginScope(burgerLab.Tenant);
        attackerScope.Db.MenuCategories.Remove(victimCategory);

        await Should.ThrowAsync<TenantIsolationViolationException>(() => attackerScope.Db.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task A_tenant_bound_scope_cannot_modify_another_tenant_record()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);

        await using var scope = database.Services.BeginScope(burgerLab.Tenant);
        var otherTenant = await scope.Db.Tenants.SingleAsync(tenant => tenant.Id == fishRestaurant.Tenant.Id, Ct);
        otherTenant.Suspend();

        await Should.ThrowAsync<TenantIsolationViolationException>(() => scope.Db.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task Saving_tenant_scoped_data_without_a_bound_tenant_fails_loudly()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);

        await using var scope = database.Services.BeginScope(tenant: null);
        scope.Db.MenuCategories.Add(TestData.NewCategory(burgerLab.Tenant.Id));

        await Should.ThrowAsync<TenantNotResolvedException>(() => scope.Db.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task A_scope_can_never_switch_to_another_tenant()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);

        await using var scope = database.Services.BeginScope(burgerLab.Tenant);

        Should.Throw<TenantIsolationViolationException>(() => scope.Get<ITenantContextSetter>().SetTenant(fishRestaurant.Tenant));
    }

    [Fact]
    public async Task The_database_rejects_an_item_pointing_at_another_tenants_category()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);

        // Passes the application-level guard (the item itself belongs to the bound tenant) but references a foreign
        // category: the composite (tenant_id, category_id) foreign key is the net under that bug.
        await using var scope = database.Services.BeginScope(burgerLab.Tenant);
        scope.Db.MenuItems.Add(TestData.NewItem(burgerLab.Tenant.Id, fishRestaurant.CategoryId));

        var exception = await Should.ThrowAsync<DbUpdateException>(() => scope.Db.SaveChangesAsync(Ct));
        exception.InnerException.ShouldBeOfType<PostgresException>().SqlState.ShouldBe(PostgresErrorCodes.ForeignKeyViolation);
    }

    private Task<long> CountCategoriesAsOwnerAsync(SeededTenant seeded) =>
        database.ScalarAsOwnerAsync<long>($"SELECT count(*) FROM menu_categories WHERE tenant_id = '{seeded.Tenant.Id.Value}'", Ct);
}
