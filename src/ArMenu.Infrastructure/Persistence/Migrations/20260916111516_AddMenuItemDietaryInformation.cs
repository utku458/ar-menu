using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArMenu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemDietaryInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "allergens",
                table: "menu_items",
                type: "character varying(16)[]",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "dietary_labels",
                table: "menu_items",
                type: "character varying(16)[]",
                nullable: false,
                defaultValue: new string[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allergens",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "dietary_labels",
                table: "menu_items");
        }
    }
}
