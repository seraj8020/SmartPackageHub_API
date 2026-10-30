using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPackageHub.HubManagement.Infrastructure;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <summary>
    /// Moves platform-admin identity from Users.IsPlatformAdmin into Role = PlatformAdmin, then drops the flag.
    /// </summary>
    [DbContext(typeof(HubDbContext))]
    [Migration("20261028210000_ReplaceIsPlatformAdminWithRole")]
    public partial class ReplaceIsPlatformAdminWithRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Users"
                SET "Role" = 'PlatformAdmin'
                WHERE "IsPlatformAdmin" = TRUE;
                """);

            migrationBuilder.DropColumn(
                name: "IsPlatformAdmin",
                table: "Users");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPlatformAdmin",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE "Users"
                SET "IsPlatformAdmin" = TRUE
                WHERE "Role" = 'PlatformAdmin';

                UPDATE "Users"
                SET "Role" = 'Admin'
                WHERE "Role" = 'PlatformAdmin';
                """);
        }
    }
}
