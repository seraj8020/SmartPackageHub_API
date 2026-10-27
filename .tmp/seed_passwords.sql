UPDATE "Users" SET "PasswordHash" = '100000.uNLOR/xkMTf3Ql3jolDKjQ==.LVEbyPydals4T2p4ZjQAbSIAvJP0anyDPlX30FUyyAQ='
WHERE "Username" IN ('hub.staff', 'society.admin');
SELECT "Username", length("PasswordHash") AS len, left("PasswordHash", 15) AS prefix
FROM "Users" WHERE "Username" IN ('hub.staff', 'society.admin');
