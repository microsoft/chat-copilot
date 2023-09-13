#!/usr/bin/env bash

# Initializes and runs both the backend and frontend for Copilot Chat.

set -e

ScriptDir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ScriptDir"

# get the port from the REACT_APP_BACKEND_URI env variable
envContent=$(grep -v '^#' ../webapp/.env | xargs)
backendPort=$(echo $envContent | sed -n 's/.*:\([0-9]*\).*/\1/p')

# Start backend (in background)
./start-backend.sh &

maxRetries=5
retryCount=0
retryWait=5  # set the number of seconds to wait before retrying

# while the backend is not running wait.
while [ $retryCount -lt $maxRetries ]
do
  if nc -z localhost $backendPort
  then
    # if the backend is running, start the frontend and break out of the loop
    ./start-frontend.sh
    break
  else
    # if the backend is not running, sleep, then increment the retry count
    sleep $retryWait
    retryCount=$((retryCount+1))
  fi
done

# if we have exceeded the number of retries
if [ $retryCount -eq $maxRetries ]
then
# write to the console that the backend is not running and we have exceeded the number of retries and we are exiting
  echo "*************************************************"
  echo "Backend is not running and we have exceeded "
  echo "the maximum number of retries."
  echo ""
  echo "Therefore, we are exiting."
  echo "*************************************************"
fi
