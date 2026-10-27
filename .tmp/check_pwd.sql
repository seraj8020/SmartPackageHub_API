SELECT "Username", left("PasswordHash", 40) AS hash_prefix, length("PasswordHash") AS len
FROM "Users"
WHERE "Username" IN ('hub.staff', 'society.admin');
