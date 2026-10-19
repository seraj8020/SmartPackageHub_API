using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignResidentLoginAndCourierIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Residents_SocietyId",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Residents");

            migrationBuilder.AddColumn<Guid>(
                name: "DigitalSignOffReceiptId",
                table: "Packages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SecureBagId",
                table: "Packages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CourierAgentId",
                table: "PackageIntakes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CourierAgents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AgentName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VehicleNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EmployeeOrBadgeNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierAgents_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecureBags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BagNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QrCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SealNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CapacityOrSize = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecureBags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecureBags_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DigitalSignOffReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CourierPartner = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CourierAgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CourierAgentName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CourierAgentPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StaffUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalPackagesCount = table.Column<int>(type: "integer", nullable: false),
                    DigitalSignature = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalSignOffReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalSignOffReceipts_CourierAgents_CourierAgentId",
                        column: x => x.CourierAgentId,
                        principalTable: "CourierAgents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DigitalSignOffReceipts_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DigitalSignOffReceipts_Users_StaffUserId",
                        column: x => x.StaffUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Residents_SocietyId_PhoneNumber",
                table: "Residents",
                columns: new[] { "SocietyId", "PhoneNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Residents_SocietyId_TowerWing",
                table: "Residents",
                columns: new[] { "SocietyId", "TowerWing" });

            migrationBuilder.CreateIndex(
                name: "IX_Packages_DigitalSignOffReceiptId",
                table: "Packages",
                column: "DigitalSignOffReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_SecureBagId",
                table: "Packages",
                column: "SecureBagId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageIntakes_CourierAgentId",
                table: "PackageIntakes",
                column: "CourierAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierAgents_SocietyId_Phone",
                table: "CourierAgents",
                columns: new[] { "SocietyId", "Phone" });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignOffReceipts_CourierAgentId",
                table: "DigitalSignOffReceipts",
                column: "CourierAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignOffReceipts_ReceiptNumber",
                table: "DigitalSignOffReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignOffReceipts_SocietyId_SignedAt",
                table: "DigitalSignOffReceipts",
                columns: new[] { "SocietyId", "SignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignOffReceipts_StaffUserId",
                table: "DigitalSignOffReceipts",
                column: "StaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecureBags_SocietyId_BagNumber",
                table: "SecureBags",
                columns: new[] { "SocietyId", "BagNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageIntakes_CourierAgents_CourierAgentId",
                table: "PackageIntakes",
                column: "CourierAgentId",
                principalTable: "CourierAgents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Packages_DigitalSignOffReceipts_DigitalSignOffReceiptId",
                table: "Packages",
                column: "DigitalSignOffReceiptId",
                principalTable: "DigitalSignOffReceipts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Packages_SecureBags_SecureBagId",
                table: "Packages",
                column: "SecureBagId",
                principalTable: "SecureBags",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackageIntakes_CourierAgents_CourierAgentId",
                table: "PackageIntakes");

            migrationBuilder.DropForeignKey(
                name: "FK_Packages_DigitalSignOffReceipts_DigitalSignOffReceiptId",
                table: "Packages");

            migrationBuilder.DropForeignKey(
                name: "FK_Packages_SecureBags_SecureBagId",
                table: "Packages");

            migrationBuilder.DropTable(
                name: "DigitalSignOffReceipts");

            migrationBuilder.DropTable(
                name: "SecureBags");

            migrationBuilder.DropTable(
                name: "CourierAgents");

            migrationBuilder.DropIndex(
                name: "IX_Residents_SocietyId_PhoneNumber",
                table: "Residents");

            migrationBuilder.DropIndex(
                name: "IX_Residents_SocietyId_TowerWing",
                table: "Residents");

            migrationBuilder.DropIndex(
                name: "IX_Packages_DigitalSignOffReceiptId",
                table: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_Packages_SecureBagId",
                table: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_PackageIntakes_CourierAgentId",
                table: "PackageIntakes");

            migrationBuilder.DropColumn(
                name: "DigitalSignOffReceiptId",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "SecureBagId",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "CourierAgentId",
                table: "PackageIntakes");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Residents",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Residents_SocietyId",
                table: "Residents",
                column: "SocietyId");
        }
    }
}
