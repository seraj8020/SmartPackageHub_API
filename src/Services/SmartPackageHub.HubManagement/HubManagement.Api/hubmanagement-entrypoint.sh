#!/bin/sh
# Add your custom logic here (e.g., waiting for database)
echo "Running EF Core migrations..."
set -e
CONNECTION_STRING="HOST=postgres; User ID=majid;Password=Alm1ghty1;Port=5432;Database=CBSAccount;Pooling=true;"
echo "$CONNECTION_STRING"
./efbundle --connection "$CONNECTION_STRING"
echo "Starting application..."
exec dotnet "BankingApp.AccountManagement.dll"