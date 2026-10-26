using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IndexOtpLogsPackagePurposeIsUsed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OtpLogs_PackageId",
                table: "OtpLogs");

            migrationBuilder.CreateIndex(
                name: "IX_OtpLogs_PackageId_Purpose_IsUsed",
                table: "OtpLogs",
                columns: new[] { "PackageId", "Purpose", "IsUsed" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OtpLogs_PackageId_Purpose_IsUsed",
                table: "OtpLogs");

            migrationBuilder.CreateIndex(
                name: "IX_OtpLogs_PackageId",
                table: "OtpLogs",
                column: "PackageId");
        }
    }
}
