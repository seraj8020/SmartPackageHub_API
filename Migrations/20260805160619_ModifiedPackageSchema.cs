using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPackageHubAPI.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedPackageSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Courier",
                table: "Packages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Courier",
                table: "Packages");
        }
    }
}
