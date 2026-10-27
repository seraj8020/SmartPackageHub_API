UPDATE "Users" SET "Username" = 'Staff', "PasswordHash" = '100000.XWcPglOoBDQvqxzfi1XD9A==.nndMEqOBcR++zhF63SyrsYYzsUWR6ENzVqek3/q5tAs=', "Status" = 'Active'
WHERE "Username" IN ('hub.staff', 'Staff') OR "Id" = '11111111-1111-4111-8111-111111111111';
SELECT "Username", "Role", "Status", length("PasswordHash") AS pwd_len FROM "Users"
WHERE "Id" = '11111111-1111-4111-8111-111111111111' OR "Username" = 'Staff';
