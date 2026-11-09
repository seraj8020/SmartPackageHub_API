using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShelfSocietyCodeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: only replace the non-unique SocietyId index when present.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = ANY (current_schemas(false))
                          AND indexname = 'IX_Shelves_SocietyId'
                    ) THEN
                        DROP INDEX "IX_Shelves_SocietyId";
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = ANY (current_schemas(false))
                          AND indexname = 'IX_Shelves_SocietyId_Code'
                    ) THEN
                        CREATE UNIQUE INDEX "IX_Shelves_SocietyId_Code"
                            ON "Shelves" ("SocietyId", "Code");
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shelves_SocietyId_Code",
                table: "Shelves");

            migrationBuilder.CreateIndex(
                name: "IX_Shelves_SocietyId",
                table: "Shelves",
                column: "SocietyId");
        }
    }
}
