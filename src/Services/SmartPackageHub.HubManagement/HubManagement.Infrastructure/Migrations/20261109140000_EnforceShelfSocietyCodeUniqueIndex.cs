using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <summary>
    /// Applies SPH-45 shelf uniqueness: one Code per SocietyId.
    /// Dedupes existing rows first so seed/demo databases can migrate cleanly.
    /// </summary>
    public partial class EnforceShelfSocietyCodeUniqueIndex : Migration
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
    }
}
