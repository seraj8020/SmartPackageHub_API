using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageReturnFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "CollectionReminderLocalTime",
                table: "Societies",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(10, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "OverdueAfterHours",
                table: "Societies",
                type: "integer",
                nullable: false,
                defaultValue: 24);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Societies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Asia/Kolkata");

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Deliveries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PackageReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Note = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedByStaffUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShelfId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShelfAssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CourierPickupNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CourierPickupAwb = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageReturns_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageReturns_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageReturns_Shelves_ShelfId",
                        column: x => x.ShelfId,
                        principalTable: "Shelves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PackageReturns_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageReturns_Users_ReceivedByStaffUserId",
                        column: x => x.ReceivedByStaffUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SocietyCarrierReturnPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReturnWindowDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 7)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocietyCarrierReturnPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocietyCarrierReturnPolicies_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageReturnImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SHA256Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageReturnImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageReturnImages_PackageReturns_ReturnId",
                        column: x => x.ReturnId,
                        principalTable: "PackageReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageReturnImages_ReturnId",
                table: "PackageReturnImages",
                column: "ReturnId");

            migrationBuilder.CreateIndex(
                name: "UX_PackageReturnImages_ReturnId_SortOrder",
                table: "PackageReturnImages",
                columns: new[] { "ReturnId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackageReturns_ReceivedByStaffUserId",
                table: "PackageReturns",
                column: "ReceivedByStaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageReturns_ResidentId_Status",
                table: "PackageReturns",
                columns: new[] { "ResidentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageReturns_ShelfId",
                table: "PackageReturns",
                column: "ShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageReturns_SocietyId_Status_RequestedAtUtc",
                table: "PackageReturns",
                columns: new[] { "SocietyId", "Status", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageReturns_PackageId",
                table: "PackageReturns",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "UX_PackageReturns_PackageId_Open",
                table: "PackageReturns",
                column: "PackageId",
                unique: true,
                filter: "\"Status\" IN ('ReturnRequested', 'ReturnReceived', 'ReturnShelved')");

            migrationBuilder.CreateIndex(
                name: "UX_SocietyCarrierReturnPolicies_SocietyId_CarrierName",
                table: "SocietyCarrierReturnPolicies",
                columns: new[] { "SocietyId", "CarrierName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageReturnImages");

            migrationBuilder.DropTable(
                name: "SocietyCarrierReturnPolicies");

            migrationBuilder.DropTable(
                name: "PackageReturns");

            migrationBuilder.DropColumn(
                name: "CollectionReminderLocalTime",
                table: "Societies");

            migrationBuilder.DropColumn(
                name: "OverdueAfterHours",
                table: "Societies");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Societies");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "AuditLogs");
        }
    }
}
