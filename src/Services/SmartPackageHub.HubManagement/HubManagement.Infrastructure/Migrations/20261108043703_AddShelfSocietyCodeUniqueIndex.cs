using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <summary>
    /// Intentionally a no-op. An earlier version of this migration incorrectly
    /// recreated the schema / unique shelf index and broke seed data.
    /// See 20261109130000_RevertShelfSocietyCodeUniqueIndex.
    /// </summary>
    public partial class AddShelfSocietyCodeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
