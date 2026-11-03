using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPackageHub.HubManagement.Infrastructure;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    [DbContext(typeof(HubDbContext))]
    [Migration("20261031120000_DashboardActivityTimestamps")]
    public partial class DashboardActivityTimestamps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(name: "ShelfAssignedAtUtc", table: "Packages", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "OnboardedAtUtc", table: "Societies", type: "timestamp with time zone", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ShelfAssignedAtUtc", table: "Packages");
            migrationBuilder.DropColumn(name: "OnboardedAtUtc", table: "Societies");
        }
    }
}
