using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPackageHubAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddedDeliveryHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "DeliveryHistories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Courier",
                table: "DeliveryHistories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PkgNumber",
                table: "DeliveryHistories",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "DeliveryHistories");

            migrationBuilder.DropColumn(
                name: "Courier",
                table: "DeliveryHistories");

            migrationBuilder.DropColumn(
                name: "PkgNumber",
                table: "DeliveryHistories");
        }
    }
}
