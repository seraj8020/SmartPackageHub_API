using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportRequestTicketFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceCode",
                table: "SupportRequests",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "SupportRequests",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Urgency",
                table: "SupportRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Backfill existing rows so unique ReferenceCode and required fields succeed.
            migrationBuilder.Sql(
                """
                UPDATE "SupportRequests"
                SET
                    "ReferenceCode" = UPPER(SUBSTR(REPLACE("Id"::text, '-', ''), 1, 7)),
                    "Subject" = CASE
                        WHEN COALESCE(TRIM("Topic"), '') = '' THEN 'Support request'
                        ELSE LEFT(TRIM("Topic"), 120)
                    END,
                    "Urgency" = 'Normal'
                WHERE "ReferenceCode" IS NULL OR "Subject" IS NULL OR "Urgency" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceCode",
                table: "SupportRequests",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "SupportRequests",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Urgency",
                table: "SupportRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_ReferenceCode",
                table: "SupportRequests",
                column: "ReferenceCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupportRequests_ReferenceCode",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "ReferenceCode",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "Urgency",
                table: "SupportRequests");
        }
    }
}
