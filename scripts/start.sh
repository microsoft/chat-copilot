#!/usr/bin/env bash

# Initializes and runs both the backend and frontend for Copilot Chat.

set -e

ScriptDir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ScriptDir"

# Start backend (in background)
./start-backend.sh &

# Start frontend
./start-frontend.sh
