using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <summary>
    /// Historical no-op. An earlier revision of this migration was unsafe.
    /// The real unique index is applied in 20261109140000_EnforceShelfSocietyCodeUniqueIndex.
    /// </summary>
    public partial class AddShelfSocietyCodeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: superseded by EnforceShelfSocietyCodeUniqueIndex after a bad full-schema revision.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: nothing was applied in Up for this migration id.
        }
    }
}
