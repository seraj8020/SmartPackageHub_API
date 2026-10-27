ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PasswordHash" character varying(500) NULL;
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20261027120000_AddUserPasswordHash', '9.0.0'
WHERE NOT EXISTS (
  SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261027120000_AddUserPasswordHash'
);
SELECT column_name FROM information_schema.columns WHERE table_name = 'Users' ORDER BY 1;
