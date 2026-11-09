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
            // Deduplicate shelves that share the same SocietyId + Code (keep one row),
            // then enforce uniqueness. Required because seed/manual data may already conflict.
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

                    IF EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = ANY (current_schemas(false))
                          AND indexname = 'IX_Shelves_SocietyId_Code'
                    ) THEN
                        RETURN;
                    END IF;

                    -- Clear FK references from packages pointing at duplicate shelves we will delete.
                    WITH ranked AS (
                        SELECT
                            "Id",
                            ROW_NUMBER() OVER (
                                PARTITION BY "SocietyId", "Code"
                                ORDER BY "Id"
                            ) AS rn
                        FROM "Shelves"
                    ),
                    losers AS (
                        SELECT "Id" FROM ranked WHERE rn > 1
                    ),
                    winners AS (
                        SELECT DISTINCT ON ("SocietyId", "Code")
                            s."Id" AS keep_id,
                            s."SocietyId",
                            s."Code"
                        FROM "Shelves" s
                        ORDER BY s."SocietyId", s."Code", s."Id"
                    )
                    UPDATE "Packages" p
                    SET "ShelfId" = w.keep_id
                    FROM losers l
                    JOIN "Shelves" dup ON dup."Id" = l."Id"
                    JOIN winners w
                      ON w."SocietyId" = dup."SocietyId"
                     AND w."Code" = dup."Code"
                    WHERE p."ShelfId" = l."Id";

                    DELETE FROM "Shelves" s
                    USING (
                        SELECT "Id"
                        FROM (
                            SELECT
                                "Id",
                                ROW_NUMBER() OVER (
                                    PARTITION BY "SocietyId", "Code"
                                    ORDER BY "Id"
                                ) AS rn
                            FROM "Shelves"
                        ) ranked
                        WHERE rn > 1
                    ) d
                    WHERE s."Id" = d."Id";

                    CREATE UNIQUE INDEX "IX_Shelves_SocietyId_Code"
                        ON "Shelves" ("SocietyId", "Code");
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
