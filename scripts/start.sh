#!/usr/bin/env bash

# Initializes and runs both the backend and frontend for Copilot Chat.

set -e

ScriptDir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ScriptDir"

# Start backend (in background)
./start-backend.sh &

# check that the backend is running and keep checking until it is
while ! nc -z localhost 40443; do
  sleep 5 # wait for 5 seconds before check again
done

# Start frontend
./start-frontend.sh
