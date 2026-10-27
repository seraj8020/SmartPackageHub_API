using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SegregateDeliverySlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliverySlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WindowEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliverySlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliverySlots_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliverySlots_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliverySlots_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "DeliverySlots" ("Id", "SocietyId", "ResidentId", "PackageId", "WindowStart", "WindowEnd", "Status", "Notes")
                SELECT gen_random_uuid(),
                       d."SocietyId",
                       d."ResidentId",
                       d."PackageId",
                       d."SlotStartTime",
                       d."SlotEndTime",
                       CASE
                         WHEN d."Slot_Status" IS NULL OR btrim(d."Slot_Status") = '' THEN 'booked'
                         ELSE left(d."Slot_Status", 50)
                       END,
                       ''
                FROM "Deliveries" d
                WHERE d."SlotStartTime" IS NOT NULL
                  AND d."SlotEndTime" IS NOT NULL
                  AND d."SlotStartTime" < d."SlotEndTime";
                """);

            migrationBuilder.DropColumn(
                name: "SlotEndTime",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "SlotStartTime",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "Slot_Status",
                table: "Deliveries");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySlots_PackageId",
                table: "DeliverySlots",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySlots_ResidentId",
                table: "DeliverySlots",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySlots_SocietyId_WindowStart_WindowEnd",
                table: "DeliverySlots",
                columns: new[] { "SocietyId", "WindowStart", "WindowEnd" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SlotEndTime",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SlotStartTime",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Slot_Status",
                table: "Deliveries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "Deliveries" d
                SET "SlotStartTime" = s."WindowStart",
                    "SlotEndTime" = s."WindowEnd",
                    "Slot_Status" = s."Status"
                FROM (
                    SELECT DISTINCT ON ("PackageId")
                           "PackageId", "WindowStart", "WindowEnd", "Status"
                    FROM "DeliverySlots"
                    ORDER BY "PackageId", "WindowStart" DESC
                ) s
                WHERE d."PackageId" = s."PackageId";
                """);

            migrationBuilder.DropTable(
                name: "DeliverySlots");
        }
    }
}
