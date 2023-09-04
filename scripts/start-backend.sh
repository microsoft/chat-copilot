#!/usr/bin/env bash

# Builds and runs the Chat Copilot backend.

set -e

# Stop any existing backend API process
while pid=$(pgrep CopilotChatWebA); do
    echo $pid | sed 's/\([0-9]\{4\}\) .*/\1/' | xargs kill
done

# Get defaults and constants
SCRIPT_DIRECTORY="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
. $SCRIPT_DIRECTORY/.env
cd "$SCRIPT_DIRECTORY/../webapi"

# Environment variable `ASPNETCORE_ENVIRONMENT` required to override appsettings.json with
# appsettings.$ENV_ASPNETCORE.json. See `webapi/ConfigurationExtensions.cs`
export ASPNETCORE_ENVIRONMENT=$ENV_ASPNETCORE

# Build and run the backend API server
dotnet build && dotnet run
