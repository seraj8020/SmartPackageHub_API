using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignByteandUriChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_OTPLogs_OtpLogId",
                table: "Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_OTPLogs_Packages_PackageId",
                table: "OTPLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_OTPLogs_Residents_ResidentId",
                table: "OTPLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_OTPLogs_Societies_SocietyId",
                table: "OTPLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OTPLogs",
                table: "OTPLogs");

            migrationBuilder.RenameTable(
                name: "OTPLogs",
                newName: "OtpLogs");

            migrationBuilder.RenameIndex(
                name: "IX_OTPLogs_SocietyId",
                table: "OtpLogs",
                newName: "IX_OtpLogs_SocietyId");

            migrationBuilder.RenameIndex(
                name: "IX_OTPLogs_ResidentId",
                table: "OtpLogs",
                newName: "IX_OtpLogs_ResidentId");

            migrationBuilder.RenameIndex(
                name: "IX_OTPLogs_PackageId",
                table: "OtpLogs",
                newName: "IX_OtpLogs_PackageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OtpLogs",
                table: "OtpLogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_OtpLogs_OtpLogId",
                table: "Deliveries",
                column: "OtpLogId",
                principalTable: "OtpLogs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OtpLogs_Packages_PackageId",
                table: "OtpLogs",
                column: "PackageId",
                principalTable: "Packages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OtpLogs_Residents_ResidentId",
                table: "OtpLogs",
                column: "ResidentId",
                principalTable: "Residents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OtpLogs_Societies_SocietyId",
                table: "OtpLogs",
                column: "SocietyId",
                principalTable: "Societies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_OtpLogs_OtpLogId",
                table: "Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_OtpLogs_Packages_PackageId",
                table: "OtpLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_OtpLogs_Residents_ResidentId",
                table: "OtpLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_OtpLogs_Societies_SocietyId",
                table: "OtpLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OtpLogs",
                table: "OtpLogs");

            migrationBuilder.RenameTable(
                name: "OtpLogs",
                newName: "OTPLogs");

            migrationBuilder.RenameIndex(
                name: "IX_OtpLogs_SocietyId",
                table: "OTPLogs",
                newName: "IX_OTPLogs_SocietyId");

            migrationBuilder.RenameIndex(
                name: "IX_OtpLogs_ResidentId",
                table: "OTPLogs",
                newName: "IX_OTPLogs_ResidentId");

            migrationBuilder.RenameIndex(
                name: "IX_OtpLogs_PackageId",
                table: "OTPLogs",
                newName: "IX_OTPLogs_PackageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OTPLogs",
                table: "OTPLogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_OTPLogs_OtpLogId",
                table: "Deliveries",
                column: "OtpLogId",
                principalTable: "OTPLogs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OTPLogs_Packages_PackageId",
                table: "OTPLogs",
                column: "PackageId",
                principalTable: "Packages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OTPLogs_Residents_ResidentId",
                table: "OTPLogs",
                column: "ResidentId",
                principalTable: "Residents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OTPLogs_Societies_SocietyId",
                table: "OTPLogs",
                column: "SocietyId",
                principalTable: "Societies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
