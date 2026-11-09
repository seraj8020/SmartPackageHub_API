using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <summary>
    /// Intermediate rollback of a bad unique-index attempt. Uniqueness is re-applied
    /// cleanly in 20261109140000_EnforceShelfSocietyCodeUniqueIndex.
    /// </summary>
    public partial class RevertShelfSocietyCodeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = ANY (current_schemas(false))
                          AND indexname = 'IX_Shelves_SocietyId_Code'
                    ) THEN
                        DROP INDEX "IX_Shelves_SocietyId_Code";
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = ANY (current_schemas(false))
                          AND indexname = 'IX_Shelves_SocietyId'
                    ) THEN
                        CREATE INDEX "IX_Shelves_SocietyId"
                            ON "Shelves" ("SocietyId");
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: forward path is superseded by EnforceShelfSocietyCodeUniqueIndex.
        }
    }
}
