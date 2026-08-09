using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPackageHubAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedPackageModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Packages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Weight",
                table: "Packages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Packages");
        }
    }
}
