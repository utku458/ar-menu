using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArMenu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArModelProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ar_model_scene_viewer_glb_path",
                table: "menu_items",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ar_model_processings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_size = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    report = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ar_model_processings", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_ar_model_processings_menu_items_tenant_id_menu_item_id",
                        columns: x => new { x.tenant_id, x.menu_item_id },
                        principalTable: "menu_items",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ar_model_processings_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ar_model_processing_queue",
                columns: table => new
                {
                    processing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    available_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lease_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ar_model_processing_queue", x => x.processing_id);
                    table.ForeignKey(
                        name: "fk_ar_model_processing_queue_ar_model_processings_tenant_id_pr",
                        columns: x => new { x.tenant_id, x.processing_id },
                        principalTable: "ar_model_processings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ar_model_processing_queue_available_at",
                table: "ar_model_processing_queue",
                column: "available_at");

            migrationBuilder.CreateIndex(
                name: "ix_ar_model_processing_queue_tenant_id_processing_id",
                table: "ar_model_processing_queue",
                columns: new[] { "tenant_id", "processing_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ar_model_processings_tenant_id_menu_item_id_created_at",
                table: "ar_model_processings",
                columns: new[] { "tenant_id", "menu_item_id", "created_at" });

            migrationBuilder.EnableTenantRowLevelSecurity("ar_model_processings");

            // ar_model_processing_queue stays without row-level security on purpose: workers claim jobs across tenants,
            // and it holds nothing but ids and schedule. SchemaGuardrailTests pins its columns so no data drifts in.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DisableTenantRowLevelSecurity("ar_model_processings");

            migrationBuilder.DropTable(
                name: "ar_model_processing_queue");

            migrationBuilder.DropTable(
                name: "ar_model_processings");

            migrationBuilder.DropColumn(
                name: "ar_model_scene_viewer_glb_path",
                table: "menu_items");
        }
    }
}
