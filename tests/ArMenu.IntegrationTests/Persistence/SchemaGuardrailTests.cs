using ArMenu.Domain.Common;
using ArMenu.Infrastructure.Persistence;
using ArMenu.IntegrationTests.TestSupport;
using Npgsql;

namespace ArMenu.IntegrationTests.Persistence;

/// <summary>
/// Guardrails that fail the build when someone adds a tenant-scoped table or entity and forgets its protection.
/// </summary>
public sealed class SchemaGuardrailTests(PostgresDatabaseFixture database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Every_table_with_a_tenant_id_column_enforces_the_tenant_isolation_policy()
    {
        const string sql = """
            SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity,
                   EXISTS (SELECT 1 FROM pg_policies p
                           WHERE p.schemaname = 'public' AND p.tablename = c.relname AND p.policyname = 'tenant_isolation')
            FROM information_schema.columns col
            JOIN pg_namespace n ON n.nspname = col.table_schema
            JOIN pg_class c ON c.relnamespace = n.oid AND c.relname = col.table_name
            WHERE col.table_schema = 'public' AND col.column_name = 'tenant_id' AND c.relkind = 'r'
              -- The one deliberate exception, pinned by The_processing_queue_is_the_only_cross_tenant_table_and_holds_no_tenant_data.
              AND c.relname <> 'ar_model_processing_queue'
            ORDER BY c.relname
            """;

        await using var connection = new NpgsqlConnection(database.OwnerConnectionString);
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(Ct);

        var tables = new List<string>();
        while (await reader.ReadAsync(Ct))
        {
            var table = reader.GetString(0);
            tables.Add(table);

            reader.GetBoolean(1).ShouldBeTrue($"Row-level security is not enabled on '{table}'.");
            reader.GetBoolean(2).ShouldBeTrue($"Row-level security is not forced on '{table}'.");
            reader.GetBoolean(3).ShouldBeTrue($"Table '{table}' has no tenant_isolation policy.");
        }

        tables.ShouldBe(["ar_model_processings", "audit_log_entries", "menu_categories", "menu_daily_statistics", "menu_items", "tenant_invitations", "tenant_memberships", "user_sessions"], ignoreOrder: true);
    }

    [Fact]
    public async Task Tenant_isolation_has_exactly_one_exception_reading_a_users_own_memberships()
    {
        // Any new policy widens what some query can see: it must be added here, deliberately, with a reason.
        const string sql = """
            SELECT tablename || ':' || policyname || ':' || cmd
            FROM pg_policies
            WHERE schemaname = 'public' AND policyname <> 'tenant_isolation'
            ORDER BY 1
            """;

        await using var connection = new NpgsqlConnection(database.OwnerConnectionString);
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(Ct);

        var policies = new List<string>();
        while (await reader.ReadAsync(Ct))
        {
            policies.Add(reader.GetString(0));
        }

        policies.ShouldBe(["tenant_memberships:own_memberships:SELECT"]);
    }

    [Fact]
    public async Task The_processing_queue_is_the_only_cross_tenant_table_and_holds_no_tenant_data()
    {
        // Workers claim jobs across tenants, so the queue cannot have row-level security. It must stay a list of pointers:
        // anything more belongs in ar_model_processings, behind the tenant policy.
        const string sql = """
            SELECT column_name FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'ar_model_processing_queue'
            ORDER BY column_name
            """;

        await using var connection = new NpgsqlConnection(database.OwnerConnectionString);
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(Ct);

        var columns = new List<string>();
        while (await reader.ReadAsync(Ct))
        {
            columns.Add(reader.GetString(0));
        }

        columns.ShouldBe(["available_at", "lease_expires_at", "processing_id", "tenant_id"]);
    }

    [Fact]
    public async Task Every_tenant_scoped_entity_has_the_tenant_query_filter()
    {
        await using var scope = database.Services.BeginScope(tenant: null);

        var tenantScopedEntities = scope.Db.Model.GetEntityTypes()
            .Where(entityType => typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            .ToList();

        tenantScopedEntities.ShouldNotBeEmpty();
        foreach (var entityType in tenantScopedEntities)
        {
            entityType.GetDeclaredQueryFilters()
                .Select(filter => filter.Key)
                .ShouldContain(QueryFilters.Tenant, $"'{entityType.DisplayName()}' is missing the tenant query filter.");
        }
    }

    [Fact]
    public async Task The_runtime_role_is_actually_subject_to_row_level_security()
    {
        await using var connection = new NpgsqlConnection(database.RuntimeConnectionString);
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(
            "SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname = current_user", connection);

        var bypassesRowLevelSecurity = (bool)(await command.ExecuteScalarAsync(Ct))!;

        bypassesRowLevelSecurity.ShouldBeFalse();
    }
}
