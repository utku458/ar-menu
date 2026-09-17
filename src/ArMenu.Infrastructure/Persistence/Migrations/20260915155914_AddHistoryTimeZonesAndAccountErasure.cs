using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArMenu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoryTimeZonesAndAccountErasure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "erased_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "time_zone",
                table: "tenants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                // Existing businesses counted their statistics in UTC days until now; they choose their zone in settings.
                defaultValue: "UTC");

            migrationBuilder.CreateTable(
                name: "audit_log_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    subject_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_name = table.Column<string>(type: "jsonb", nullable: true),
                    changes = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log_entries", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_audit_log_entries_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audit_log_entries_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entries_actor_user_id",
                table: "audit_log_entries",
                column: "actor_user_id");

            // A business's history belongs to it like its menu.
            migrationBuilder.EnableTenantRowLevelSecurity("audit_log_entries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DisableTenantRowLevelSecurity("audit_log_entries");

            migrationBuilder.DropTable(
                name: "audit_log_entries");

            migrationBuilder.DropColumn(
                name: "erased_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "closed_at",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "time_zone",
                table: "tenants");
        }
    }
}
