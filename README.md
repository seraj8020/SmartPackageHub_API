# SmartPackageHub API

ASP.NET Core 8 Web API for Smart Package Hub (Hub Management).

## Prerequisites

Install the following before setting up a local development environment:

| Tool | Purpose | Notes |
| --- | --- | --- |
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | Build and run the API | `dotnet --list-sdks` should show 8.x |
| [Git](https://git-scm.com/downloads) | Clone the repository | |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Run PostgreSQL, pgAdmin, Redis, Seq | Windows: enable WSL 2 backend if prompted |
| Visual Studio 2022 **or** VS Code | Optional IDE | Visual Studio: ASP.NET and web development workload |

## 1. Clone the repository

```bat
git clone https://github.com/seraj8020/SmartPackageHub_API.git
cd SmartPackageHub_API
git checkout Development
```

All paths below assume your current directory is the repository root (`SmartPackageHub_API`), unless a step says otherwise.

## 2. Install and start Docker Desktop

1. Download and install **Docker Desktop** for Windows.
2. Launch Docker Desktop and wait until the engine status shows **Running**.
3. Confirm Docker is available from Command Prompt or PowerShell:

```bat
docker --version
docker compose version
```

## 3. Pull and run dependency containers

`docker-compose.yml` at the repository root starts these services:

| Service | Image | Host ports | Purpose |
| --- | --- | --- | --- |
| **postgres** | `postgres:16.4` | `5434` → container `5432` | Application database |
| **pgadmin** | `dpage/pgadmin4` | `5050` → `80` | Database UI |
| **redis** | `redis:latest` | `6379` | Cache / outbound message queue |
| **seq** | `datalust/seq:2024.3` | UI `8081`, ingest `5341` | Structured logs |
| **seq-input-gelf** | `datalust/seq-input-gelf:latest` | UDP `12201` | Optional GELF log input |

### Pull images

From the repository root:

```bat
cd D:\SmartPackageHub21\SmartPackageHub_API
docker compose pull
```

### Start containers

```bat
docker compose up -d
```

Check status:

```bat
docker compose ps
```

Useful URLs after containers are healthy:

- **pgAdmin:** http://localhost:5050  
  - Email: `admin@admin.com`  
  - Password: `admin`
- **Seq UI:** http://localhost:8081
- **PostgreSQL (from your machine):** host `localhost`, port **`5434`**, database `PackageHubRevamp`, user `SpHubAdmin`, password `Admin@123`
- **Redis:** `localhost:6379`

Stop containers when finished:

```bat
docker compose down
```

> **Note:** Postgres is published on host port **5434** (not 5432). Use `5434` in local connection strings when the API runs on the host against Docker Postgres.

## 4. Point the API at local Docker Postgres

Update the connection string in both of these files so EF tools and the API use local Docker:

- `src\Services\SmartPackageHub.HubManagement\HubManagement.Api\appsettings.json`
- `src\Services\SmartPackageHub.HubManagement\HubManagement.Infrastructure\appsettings.json`

Example local connection string:

```text
Port=5434;Host=localhost;Database=PackageHubRevamp;User ID=SpHubAdmin;Password=Admin@123;Pooling=true;
```

Optional local Seq URL (when the API runs on the host, not inside the Docker network):

```json
"WriteTo": [
  { "Name": "Console" },
  {
    "Name": "Seq",
    "Args": {
      "serverUrl": "http://localhost:5341"
    }
  }
]
```

Redis is already set to `localhost:6379` for local development.

Do **not** commit real SMS/API secrets. Keep placeholders such as `YOUR_FAST2SMS_TEST_API_KEY` in source control.

## 5. Install Entity Framework Core tools

The solution uses **EF Core 9** (`Microsoft.EntityFrameworkCore.Tools` / `Design` 9.0.0) with **Npgsql**.

Open **Command Prompt** or **PowerShell** and install (or update) the global `dotnet-ef` tool:

```bat
dotnet tool install --global dotnet-ef --version 9.0.0
```

If it is already installed:

```bat
dotnet tool update --global dotnet-ef --version 9.0.0
```

Verify:

```bat
dotnet ef --version
```

## 6. Restore and build the solution

From the repository root:

```bat
cd D:\SmartPackageHub21\SmartPackageHub_API
dotnet restore SmartPackageHub.sln
dotnet build SmartPackageHub.sln
```

## 7. Entity Framework migrations

Migrations live under:

`src\Services\SmartPackageHub.HubManagement\HubManagement.Infrastructure\Migrations`

Design-time DbContext factory:

`HubManagement.Infrastructure\ApplicationDesignTimeDbContextFactory.cs`

It reads `appsettings.json` from the **current working directory**, so run EF commands from the Infrastructure project folder.

### Change directory before running EF commands

```bat
cd D:\SmartPackageHub21\SmartPackageHub_API\src\Services\SmartPackageHub.HubManagement\HubManagement.Infrastructure
```

### List existing migrations

```bat
dotnet ef migrations list --startup-project ..\HubManagement.Api\HubManagement.Api.csproj --project HubManagement.Infrastructure.csproj
```

### Create a new migration

Replace `YourMigrationName` with a meaningful name (for example `AddShelfCapacity`):

```bat
dotnet ef migrations add YourMigrationName --startup-project ..\HubManagement.Api\HubManagement.Api.csproj --project HubManagement.Infrastructure.csproj --output-dir Migrations
```

### Apply migrations (update database)

```bat
dotnet ef database update --startup-project ..\HubManagement.Api\HubManagement.Api.csproj --project HubManagement.Infrastructure.csproj
```

### Apply up to a specific migration

```bat
dotnet ef database update YourMigrationName --startup-project ..\HubManagement.Api\HubManagement.Api.csproj --project HubManagement.Infrastructure.csproj
```

### Remove the last unapplied migration (local only)

```bat
dotnet ef migrations remove --startup-project ..\HubManagement.Api\HubManagement.Api.csproj --project HubManagement.Infrastructure.csproj
```

> **Startup note:** `DataSeeder` also calls `context.Database.Migrate()` when the API starts, so existing migrations are applied automatically on run if the database is reachable. Prefer running `dotnet ef database update` yourself when creating or debugging new migrations.

## 8. Run the Web API

From the repository root (or the Api project folder):

```bat
cd D:\SmartPackageHub21\SmartPackageHub_API\src\Services\SmartPackageHub.HubManagement\HubManagement.Api
dotnet run --launch-profile http
```

Or open `SmartPackageHub.sln` in Visual Studio, set **HubManagement.Api** as the startup project, and press F5 (profile **http**).

Default local URL:

- API / Swagger: http://localhost:5210/swagger

On first successful start against an empty database, seed data under `HubManagement.Api\SeedData\` is loaded after migrations.

## 9. Quick verification checklist

1. Docker Desktop is running.
2. `docker compose ps` shows `postgres`, `pgadmin`, `redis`, `seq` (and `seq-input-gelf`) as healthy/running.
3. You can open pgAdmin at http://localhost:5050 and connect to Postgres on host `postgres` (from inside Docker) or `host.docker.internal` / `localhost` port `5434` from the host.
4. Redis responds: `docker exec -it redis redis-cli ping` → `PONG`.
5. Seq UI loads at http://localhost:8081.
6. `dotnet ef database update` succeeds from the Infrastructure directory.
7. API starts and Swagger opens at http://localhost:5210/swagger.

## Common troubleshooting

| Problem | What to try |
| --- | --- |
| `dotnet ef` not found | Close and reopen the terminal after installing the tool; confirm `%USERPROFILE%\.dotnet\tools` is on `PATH` |
| Cannot connect to Postgres | Confirm Docker is up; use port **5434** from the host; wait for the healthcheck to pass |
| Port already in use | Stop the conflicting process or change the host port mapping in `docker-compose.yml` |
| EF design-time cannot find connection string | Run commands from `HubManagement.Infrastructure` so `appsettings.json` is found |
| Seq sink errors when API runs locally | Point Serilog `serverUrl` to `http://localhost:5341` (host), not `http://seq:5341` (Docker network DNS) |
| Docker pull fails | Sign in to Docker Hub if rate-limited; retry `docker compose pull` |

## Project layout (high level)

```text
SmartPackageHub_API/
├── docker-compose.yml
├── SmartPackageHub.sln
└── src/
    ├── Core/                          # Shared libraries
    └── Services/SmartPackageHub.HubManagement/
        ├── HubManagement.Api/         # Web API host, Swagger, seed data
        ├── HubManagement.Application/ # Commands, queries, handlers
        ├── HubManagement.Core/        # Domain entities
        ├── HubManagement.Infrastructure/  # EF Core, migrations
        └── HubManagement.Workers/     # Background workers (e.g. outbound SMS)
```
