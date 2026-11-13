# Smart Package Hub API — multi-stage image for Oracle Cloud / Docker hosts.
# Build: docker build -f src/Services/SmartPackageHub.HubManagement/HubManagement.Api/Dockerfile -t sph-api:qa .
# Run:   docker run --rm -e ASPNETCORE_ENVIRONMENT=QA -p 5000:8080 sph-api:qa

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Services/SmartPackageHub.HubManagement/HubManagement.Api/HubManagement.Api.csproj", "src/Services/SmartPackageHub.HubManagement/HubManagement.Api/"]
COPY ["src/Services/SmartPackageHub.HubManagement/HubManagement.Application/HubManagement.Application.csproj", "src/Services/SmartPackageHub.HubManagement/HubManagement.Application/"]
COPY ["src/Services/SmartPackageHub.HubManagement/HubManagement.Core/HubManagement.Core.csproj", "src/Services/SmartPackageHub.HubManagement/HubManagement.Core/"]
COPY ["src/Services/SmartPackageHub.HubManagement/HubManagement.Infrastructure/HubManagement.Infrastructure.csproj", "src/Services/SmartPackageHub.HubManagement/HubManagement.Infrastructure/"]
COPY ["src/Services/SmartPackageHub.HubManagement/HubManagement.Workers/HubManagement.Workers.csproj", "src/Services/SmartPackageHub.HubManagement/HubManagement.Workers/"]
COPY ["src/Core/SmartPackageHub.CommonLibrary/Common/Common.csproj", "src/Core/SmartPackageHub.CommonLibrary/Common/"]
COPY ["src/Core/SmartPackageHub.Application/Applications/Applications.csproj", "src/Core/SmartPackageHub.Application/Applications/"]
COPY ["src/Core/SmartPackageHub.Domain/Domains/Domains.csproj", "src/Core/SmartPackageHub.Domain/Domains/"]
COPY ["src/Core/SmartPackageHub.Infrastructure/Infrastructures/Infrastructures.csproj", "src/Core/SmartPackageHub.Infrastructure/Infrastructures/"]

RUN dotnet restore "./src/Services/SmartPackageHub.HubManagement/HubManagement.Api/HubManagement.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/SmartPackageHub.HubManagement/HubManagement.Api"
RUN dotnet build "./HubManagement.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HubManagement.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY src/Services/SmartPackageHub.HubManagement/HubManagement.Api/docker-entrypoint.sh /docker-entrypoint.sh

# Parcel/damage photos write under App_Data; entrypoint chowns volume for non-root `app`.
USER root
RUN mkdir -p /app/App_Data \
  && chown -R app:app /app/App_Data \
  && chmod +x /docker-entrypoint.sh

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -fsS http://127.0.0.1:8080/healthz || exit 1

ENTRYPOINT ["/docker-entrypoint.sh"]
