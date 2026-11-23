using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPackageHub.HubManagement.Infrastructure;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    [DbContext(typeof(HubDbContext))]
    [Migration("20261123143000_AddPackageDeliveryEvidence")]
    public partial class AddPackageDeliveryEvidence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackageDeliveryEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SHA256Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SignedByName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageDeliveryEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageDeliveryEvidence_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageDeliveryEvidence_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageDeliveryEvidence_DeliveryId",
                table: "PackageDeliveryEvidence",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageDeliveryEvidence_PackageId",
                table: "PackageDeliveryEvidence",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "UX_PackageDeliveryEvidence_DeliveryId_Kind",
                table: "PackageDeliveryEvidence",
                columns: new[] { "DeliveryId", "Kind" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PackageDeliveryEvidence");
        }
    }
}
