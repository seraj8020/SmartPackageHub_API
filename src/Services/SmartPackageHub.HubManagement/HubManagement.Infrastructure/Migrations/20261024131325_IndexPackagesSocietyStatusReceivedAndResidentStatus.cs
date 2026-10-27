using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IndexPackagesSocietyStatusReceivedAndResidentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Packages_ResidentId",
                table: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_Packages_SocietyId",
                table: "Packages");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_ResidentId_Status",
                table: "Packages",
                columns: new[] { "ResidentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Packages_SocietyId_Status_RecievedAt",
                table: "Packages",
                columns: new[] { "SocietyId", "Status", "RecievedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Packages_ResidentId_Status",
                table: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_Packages_SocietyId_Status_RecievedAt",
                table: "Packages");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_ResidentId",
                table: "Packages",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_SocietyId",
                table: "Packages",
                column: "SocietyId");
        }
    }
}
