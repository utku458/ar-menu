using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArMenu.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Database-level tenant isolation (defense in depth behind EF Core query filters).
    /// Every new tenant-scoped table must be added here or in a later migration; an integration test enforces it.
    /// </summary>
    public partial class EnableTenantRowLevelSecurity : Migration
    {
        private static readonly string[] TenantScopedTables = ["menu_categories", "menu_items"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in TenantScopedTables)
            {
                migrationBuilder.EnableTenantRowLevelSecurity(table);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in TenantScopedTables)
            {
                migrationBuilder.DisableTenantRowLevelSecurity(table);
            }
        }
    }
}
