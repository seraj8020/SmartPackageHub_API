using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPackageHub.HubManagement.Infrastructure;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(HubDbContext))]
    [Migration("20261025100000_AddPackageCodApprovalStatus")]
    public partial class AddPackageCodApprovalStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodApprovalStatus",
                table: "Packages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "None");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodApprovalStatus",
                table: "Packages");
        }
    }
}
