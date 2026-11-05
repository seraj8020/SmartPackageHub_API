using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPackageHub.HubManagement.Infrastructure;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    [DbContext(typeof(HubDbContext))]
    [Migration("20261105120000_AddCourierSplitRollup")]
    public partial class AddCourierSplitRollup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CourierSplitJson",
                table: "AnalyticsDailies",
                type: "text",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.Sql("""
                UPDATE "AnalyticsDailies" AS a
                SET "CourierSplitJson" = COALESCE((
                    SELECT jsonb_object_agg(couriers."Courier", couriers."Count")::text
                    FROM (
                        SELECT COALESCE(NULLIF(BTRIM(p."CourierPartner"), ''), 'Unknown') AS "Courier",
                               COUNT(*)::integer AS "Count"
                        FROM "Packages" AS p
                        WHERE p."SocietyId" = a."SocietyId"
                          AND p."RecievedAt" >= (a."Day"::timestamp AT TIME ZONE 'UTC')
                          AND p."RecievedAt" < ((a."Day" + 1)::timestamp AT TIME ZONE 'UTC')
                          AND p."RecievedAt" <= COALESCE(a."LastRolledUpAtUtc", ((a."Day" + 1)::timestamp AT TIME ZONE 'UTC'))
                        GROUP BY COALESCE(NULLIF(BTRIM(p."CourierPartner"), ''), 'Unknown')
                    ) AS couriers
                ), '{}');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CourierSplitJson", table: "AnalyticsDailies");
        }
    }
}
