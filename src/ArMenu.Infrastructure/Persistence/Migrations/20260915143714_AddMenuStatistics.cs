using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArMenu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "menu_daily_statistics",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    @event = table.Column<string>(name: "event", type: "character varying(32)", maxLength: 32, nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    count = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_daily_statistics", x => new { x.tenant_id, x.day, x.@event, x.menu_item_id });
                    table.ForeignKey(
                        name: "fk_menu_daily_statistics_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Guest activity belongs to its business like the menu it counts.
            migrationBuilder.EnableTenantRowLevelSecurity("menu_daily_statistics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DisableTenantRowLevelSecurity("menu_daily_statistics");

            migrationBuilder.DropTable(
                name: "menu_daily_statistics");
        }
    }
}
