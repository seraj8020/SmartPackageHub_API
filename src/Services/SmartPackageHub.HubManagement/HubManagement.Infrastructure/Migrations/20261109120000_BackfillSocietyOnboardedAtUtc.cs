using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillSocietyOnboardedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Societies"
                SET "OnboardedAtUtc" = (now() AT TIME ZONE 'utc')
                WHERE "OnboardedAtUtc" IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "OnboardedAtUtc",
                table: "Societies",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "(now() AT TIME ZONE 'utc')",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "OnboardedAtUtc",
                table: "Societies",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "(now() AT TIME ZONE 'utc')");
        }
    }
}
