using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPackageHub.HubManagement.Infrastructure;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(HubDbContext))]
    [Migration("20261101120000_AddPackagePreferredDeliveryMethod")]
    public partial class AddPackagePreferredDeliveryMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredDeliveryMethod",
                table: "Packages",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredDeliveryMethod",
                table: "Packages");
        }
    }
}
