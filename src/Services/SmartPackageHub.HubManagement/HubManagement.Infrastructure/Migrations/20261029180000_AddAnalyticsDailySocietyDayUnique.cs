using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPackageHub.HubManagement.Infrastructure;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <summary>
    /// Ensures one AnalyticsDaily row per society per calendar day (rollup upsert safety).
    /// </summary>
    [DbContext(typeof(HubDbContext))]
    [Migration("20261029180000_AddAnalyticsDailySocietyDayUnique")]
    public partial class AddAnalyticsDailySocietyDayUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsDailies_SocietyId_Day",
                table: "AnalyticsDailies",
                columns: new[] { "SocietyId", "Day" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnalyticsDailies_SocietyId_Day",
                table: "AnalyticsDailies");
        }
    }
}
