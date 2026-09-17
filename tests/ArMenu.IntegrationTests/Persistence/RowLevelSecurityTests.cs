using ArMenu.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ArMenu.IntegrationTests.Persistence;

/// <summary>
/// Defense in depth: PostgreSQL row-level security keeps tenants apart even when EF Core's protections are bypassed.
/// These tests deliberately use the escape hatches (IgnoreQueryFilters, raw SQL) that application code should not.
/// </summary>
public sealed class RowLevelSecurityTests(PostgresDatabaseFixture database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Ignoring_every_ef_query_filter_still_does_not_expose_other_tenants()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        await database.Services.SeedTenantAsync(Ct);

        await using var scope = database.Services.BeginScope(burgerLab.Tenant);

        var visibleTenants = await scope.Db.MenuItems
            .IgnoreQueryFilters()
            .Select(item => item.TenantId)
            .Distinct()
            .ToListAsync(Ct);

        visibleTenants.ShouldBe([burgerLab.Tenant.Id]);
    }

    [Fact]
    public async Task Raw_sql_only_sees_rows_of_the_bound_tenant()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        await database.Services.SeedTenantAsync(Ct);

        await using var scope = database.Services.BeginScope(burgerLab.Tenant);

        var visibleTenants = await scope.Db.Database
            .SqlQueryRaw<Guid>("SELECT DISTINCT tenant_id AS \"Value\" FROM menu_items")
            .ToListAsync(Ct);

        visibleTenants.ShouldBe([burgerLab.Tenant.Id.Value]);
    }

    [Fact]
    public async Task A_scope_without_tenant_sees_no_tenant_data_at_all_rather_than_all_of_it()
    {
        await database.Services.SeedTenantAsync(Ct);

        await using var scope = database.Services.BeginScope(tenant: null);

        var visibleToRuntimeRole = await scope.Db.Database.SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM menu_items").SingleAsync(Ct);
        var existingRows = await database.ScalarAsOwnerAsync<long>("SELECT count(*) FROM menu_items", Ct);

        existingRows.ShouldBeGreaterThan(0);
        visibleToRuntimeRole.ShouldBe(0);
    }

    [Fact]
    public async Task Naming_a_user_reveals_only_their_own_memberships_and_nothing_to_change()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);
        var owner = await database.Services.SeedMemberAsync(burgerLab.Tenant, ArMenu.Domain.Memberships.TenantRole.Owner, Ct);
        await database.Services.AddMembershipAsync(fishRestaurant.Tenant, owner.UserId, ArMenu.Domain.Memberships.TenantRole.Staff, Ct);
        await database.Services.SeedMemberAsync(fishRestaurant.Tenant, ArMenu.Domain.Memberships.TenantRole.Manager, Ct);

        // The runtime role, no tenant bound, the user named for this transaction only.
        await using var connection = new NpgsqlConnection(database.RuntimeConnectionString);
        await connection.OpenAsync(Ct);
        await using var transaction = await connection.BeginTransactionAsync(Ct);
        async Task<T> ScalarAsync<T>(string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("user_id", owner.UserId.Value);
            return (T)(await command.ExecuteScalarAsync(Ct))!;
        }

        await ScalarAsync<string>("SELECT set_config('app.current_user', @user_id::text, true)");

        var visibleMemberships = await ScalarAsync<long>("SELECT count(*) FROM tenant_memberships");
        var othersVisible = await ScalarAsync<long>("SELECT count(*) FROM tenant_memberships WHERE user_id <> @user_id");
        await using (var update = new NpgsqlCommand("UPDATE tenant_memberships SET role = 'Owner' WHERE user_id = @user_id", connection, transaction))
        {
            update.Parameters.AddWithValue("user_id", owner.UserId.Value);
            (await update.ExecuteNonQueryAsync(Ct)).ShouldBe(0, "the policy lets the user read, never write");
        }

        var sessionsVisible = await ScalarAsync<long>("SELECT count(*) FROM user_sessions");

        visibleMemberships.ShouldBe(2);
        othersVisible.ShouldBe(0);
        sessionsVisible.ShouldBe(0, "only memberships are readable across tenants");
    }

    [Fact]
    public async Task Raw_sql_cannot_update_another_tenants_rows()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);

        await using var scope = database.Services.BeginScope(burgerLab.Tenant);

        var affectedRows = await scope.Db.Database.ExecuteSqlAsync(
            $"UPDATE menu_items SET is_available = false WHERE id = {fishRestaurant.ItemId.Value}",
            Ct);

        affectedRows.ShouldBe(0);
    }

    [Fact]
    public async Task Raw_sql_cannot_insert_rows_for_another_tenant()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);

        await using var scope = database.Services.BeginScope(burgerLab.Tenant);

        var exception = await Should.ThrowAsync<PostgresException>(() => scope.Db.Database.ExecuteSqlAsync(
            $$"""
            INSERT INTO menu_categories (id, tenant_id, name, display_order, is_visible, is_deleted, created_at)
            VALUES ({{Guid.CreateVersion7()}}, {{fishRestaurant.Tenant.Id.Value}}, '{"tr": "Sızma"}'::jsonb, 0, true, false, now())
            """,
            Ct));

        exception.SqlState.ShouldBe(PostgresErrorCodes.InsufficientPrivilege);
        exception.MessageText.ShouldContain("row-level security");
    }
}
