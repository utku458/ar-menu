using ArMenu.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ArMenu.Infrastructure.Persistence.Migrations;

internal static class RowLevelSecurityMigrationExtensions
{
    public const string PolicyName = "tenant_isolation";

    /// <summary>The one exception to tenant isolation: people may read their own memberships in every tenant.</summary>
    public const string OwnMembershipsPolicyName = "own_memberships";

    /// <summary>Transaction-local setting naming the signed-in user, set only by the query that lists their workspaces.</summary>
    public const string CurrentUserSettingName = "app.current_user";

    /// <summary>
    /// Enables PostgreSQL row-level security on a tenant-scoped table: rows are visible and writable only when their
    /// <c>tenant_id</c> matches the session's <c>app.current_tenant</c> (see <see cref="TenantSessionConnectionInterceptor"/>).
    /// </summary>
    /// <remarks>
    /// <c>FORCE</c> applies the policy to the table owner as well; only superusers and BYPASSRLS roles are exempt.
    /// <c>NULLIF(..., '')</c> maps an unset or empty setting to NULL, and <c>tenant_id = NULL</c> matches nothing.
    /// </remarks>
    public static void EnableTenantRowLevelSecurity(this MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.Sql($"""
            ALTER TABLE "{table}" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "{table}" FORCE ROW LEVEL SECURITY;
            CREATE POLICY {PolicyName} ON "{table}"
                USING (tenant_id = NULLIF(current_setting('{TenantSessionConnectionInterceptor.SettingName}', true), '')::uuid)
                WITH CHECK (tenant_id = NULLIF(current_setting('{TenantSessionConnectionInterceptor.SettingName}', true), '')::uuid);
            """);
    }

    /// <summary>
    /// Adds a read-only policy to <c>tenant_memberships</c>: rows of the user named by <c>app.current_user</c> are
    /// visible whatever the tenant. Policies are permissive, so it widens reads by exactly those rows; writes still
    /// require the tenant, because it applies to <c>SELECT</c> only.
    /// </summary>
    public static void EnableOwnMembershipsReadPolicy(this MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql($"""
            CREATE POLICY {OwnMembershipsPolicyName} ON "tenant_memberships" FOR SELECT
                USING (user_id = NULLIF(current_setting('{CurrentUserSettingName}', true), '')::uuid);
            """);

    public static void DisableOwnMembershipsReadPolicy(this MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql($"""DROP POLICY IF EXISTS {OwnMembershipsPolicyName} ON "tenant_memberships";""");

    public static void DisableTenantRowLevelSecurity(this MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.Sql($"""
            DROP POLICY IF EXISTS {PolicyName} ON "{table}";
            ALTER TABLE "{table}" NO FORCE ROW LEVEL SECURITY;
            ALTER TABLE "{table}" DISABLE ROW LEVEL SECURITY;
            """);
    }
}
