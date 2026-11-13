#!/bin/sh
set -e
# Named volume mounts often arrive as root-owned; app user must write photos here.
mkdir -p /app/App_Data
chown -R app:app /app/App_Data
exec runuser -u app -- dotnet HubManagement.Api.dll "$@"
