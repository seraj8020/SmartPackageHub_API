# See https://aka.ms/customizecontainer to learn how to customize your debug container.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER app

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
ENTRYPOINT ["dotnet", "HubManagement.Api.dll"]
