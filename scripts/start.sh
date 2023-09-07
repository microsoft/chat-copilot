#!/usr/bin/env bash

# Initializes and runs both the backend and frontend for Copilot Chat.

set -e

ScriptDir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ScriptDir"

# get REACT_APP_BACKEND_URI from ../webapp/.env
envContent=$(grep -v '^#' ../webapp/.env | xargs)
# get the '40443" from REACT_APP_BACKEND_URI=https://localhost:40443/
backendPort=$(echo $envContent | sed -n 's/.*:\([0-9]*\).*/\1/p')
# echo "backendPort: $backendPort"

# Start backend (in background)
./start-backend.sh &

# check that the backend is running and keep checking until it is
while ! nc -z localhost $backendPort; do
  sleep 5 # wait for 5 seconds before check again
  echo "Waiting for backend to start..."
done

# Start frontend
./start-frontend.sh
