ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PhoneNumber" character varying(50) NULL;
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20261027123000_AddUserPhoneNumber', '9.0.0'
WHERE NOT EXISTS (
  SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261027123000_AddUserPhoneNumber'
);
UPDATE "Users" SET "PhoneNumber" = '9900783610' WHERE "Username" = 'hub.staff' AND ("PhoneNumber" IS NULL OR "PhoneNumber" = '');
UPDATE "Users" SET "PhoneNumber" = '9900783611' WHERE "Username" = 'society.admin' AND ("PhoneNumber" IS NULL OR "PhoneNumber" = '');
SELECT "Username", "PhoneNumber" FROM "Users" WHERE "Username" IN ('hub.staff', 'society.admin');
