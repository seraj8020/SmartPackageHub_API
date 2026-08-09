using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPackageHubAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddedPackageNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PkgNumber",
                table: "Packages",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PkgNumber",
                table: "Packages");
        }
    }
}
