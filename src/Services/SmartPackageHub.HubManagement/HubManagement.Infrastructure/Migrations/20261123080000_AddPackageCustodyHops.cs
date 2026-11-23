using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPackageHub.HubManagement.Infrastructure;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    [DbContext(typeof(HubDbContext))]
    [Migration("20261123080000_AddPackageCustodyHops")]
    public partial class AddPackageCustodyHops : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackageCustodyHops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FromZone = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ToZone = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LocationLabel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    StaffUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    StaffUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageCustodyHops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageCustodyHops_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageCustodyHops_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageCustodyHops_Users_StaffUserId",
                        column: x => x.StaffUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageCustodyHops_PackageId_OccurredAtUtc",
                table: "PackageCustodyHops",
                columns: new[] { "PackageId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageCustodyHops_SocietyId_OccurredAtUtc",
                table: "PackageCustodyHops",
                columns: new[] { "SocietyId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageCustodyHops_StaffUserId",
                table: "PackageCustodyHops",
                column: "StaffUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PackageCustodyHops");
        }
    }
}
