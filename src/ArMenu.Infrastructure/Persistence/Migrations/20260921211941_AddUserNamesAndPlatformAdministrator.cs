using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArMenu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNamesAndPlatformAdministrator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_platform_admin",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "user_name",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_user_name",
                table: "users",
                column: "user_name",
                unique: true,
                filter: "user_name IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_user_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_platform_admin",
                table: "users");

            migrationBuilder.DropColumn(
                name: "user_name",
                table: "users");
        }
    }
}
