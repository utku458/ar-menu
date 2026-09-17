using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArMenu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnMembershipsPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Lets the workspace switcher list a user's businesses without reading across tenants in any other way.
            migrationBuilder.EnableOwnMembershipsReadPolicy();
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DisableOwnMembershipsReadPolicy();
        }
    }
}
