SmartPackageHub API (ASP.NET Core)

This is a simple API demo for package pickup and history with OTP verification, using EF Core and PostgreSQL.

Endpoints:
- POST /api/members -> create a resident/member
- GET /api/residents/{id}/packages -> list packages at hub for resident
- POST /api/residents/{id}/otp -> request OTP (returns OTP in response for demo)
- POST /api/residents/{id}/verify-otp -> verify OTP and mark packages picked up
- GET /api/residents/{id}/history -> delivery history for resident's packages

To run:
1. Update appsettings.json ConnectionStrings:DefaultConnection to point to your PostgreSQL instance.
2. dotnet restore
3. dotnet run

Notes:
- For production, run EF Core migrations instead of EnsureCreated, and use a real SMS/Email provider to deliver OTPs.
