SELECT "Username", "Role", "Status", length("PasswordHash") AS pwd_len, "PhoneNumber"
FROM "Users"
WHERE "Role" IN ('Staff', 'Admin', 'Courier')
ORDER BY "Username";
