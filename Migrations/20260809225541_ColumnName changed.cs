using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPackageHubAPI.Migrations
{
    /// <inheritdoc />
    public partial class ColumnNamechanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "method",
                table: "DeliveryHistories",
                newName: "MethodOfDelivery");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MethodOfDelivery",
                table: "DeliveryHistories",
                newName: "method");
        }
    }
}
