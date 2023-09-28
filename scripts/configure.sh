#!/usr/bin/env bash
# Configure user secrets, appsettings.Development.json, and webapp/.env for Chat Copilot.

set -e

# Get defaults and constants
SCRIPT_DIRECTORY="$(dirname $0)"
. $SCRIPT_DIRECTORY/.env

# Argument parsing
POSITIONAL_ARGS=()

while [[ $# -gt 0 ]]; do
  case $1 in
  --aiservice) # Required argument
    AI_SERVICE="$2"
    shift
    shift
    ;;
  -a | --apikey) # Required argument
    API_KEY="$2"
    shift
    shift
    ;;
  -e | --endpoint) # Required argument for Azure OpenAI
    ENDPOINT="$2"
    shift
    shift
    ;;
  --completionmodel)
    COMPLETION_MODEL="$2"
    shift
    shift
    ;;
  --embeddingmodel)
    EMBEDDING_MODEL="$2"
    shift
    shift
    ;;
  --plannermodel)
    PLANNER_MODEL="$2"
    shift
    shift
    ;;
  -fc | --frontend-clientid)
    FRONTEND_CLIENT_ID="$2"
    shift
    shift
    ;;
  -bc | --backend-clientid)
    BACKEND_CLIENT_ID="$2"
    shift
    shift
    ;;
  -t | --tenantid)
    TENANT_ID="$2"
    shift
    shift
    ;;
  -i | --instance)
    INSTANCE="$2"
    shift
    shift
    ;;
  -* | --*)
    echo "Unknown option $1"
    exit 1
    ;;
  *)
    POSITIONAL_ARGS+=("$1") # save positional arg
    shift                   # past argument
    ;;
  esac
done

set -- "${POSITIONAL_ARGS[@]}" # restore positional parameters

# Validate arguments
if [ -z "$AI_SERVICE" -o \( "$AI_SERVICE" != "$ENV_OPEN_AI" -a "$AI_SERVICE" != "$ENV_AZURE_OPEN_AI" \) ]; then
  echo "Please specify an AI service (AzureOpenAI or OpenAI) for --aiservice. "
  exit 1
fi
if [ -z "$API_KEY" ]; then
  echo "Please specify an API key with -a or --apikey."
  exit 1
fi
if [ "$AI_SERVICE" = "$ENV_AZURE_OPEN_AI" ] && [ -z "$ENDPOINT" ]; then
  echo "When using $(--aiservice AzureOpenAI), please specify an endpoint with -e or --endpoint."
  exit 1
fi

if [ "$FRONTEND_CLIENT_ID" ] && [ "$BACKEND_CLIENT_ID" ] && [ "$TENANT_ID" ]; then
  # Set auth type to AzureAd
  AUTH_TYPE="$ENV_AZURE_AD"
  # If instance empty, use default
  if [ -z "$INSTANCE" ]; then
    INSTANCE="$ENV_INSTANCE"
  fi
else
  if [ -z "$FRONTEND_CLIENT_ID" ] && [ -z "$BACKEND_CLIENT_ID" ] && [ -z "$TENANT_ID" ]; then
    # Set auth type to None
    AUTH_TYPE="$ENV_NONE"
  else
    echo "To use Azure AD authentication, please set --frontend-clientid, --backend-clientid, and --tenantid."
    exit 1
  fi
fi

# Set remaining values from .env if not passed as argument
if [ "$AI_SERVICE" = "$ENV_OPEN_AI" ]; then
  if [ -z "$COMPLETION_MODEL" ]; then
    COMPLETION_MODEL="$ENV_COMPLETION_MODEL_OPEN_AI"
  fi
  if [ -z "$PLANNER_MODEL" ]; then
    PLANNER_MODEL="$ENV_PLANNER_MODEL_OPEN_AI"
  fi
  # TO DO: Validate model values if set by command line.
else # elif [ "$AI_SERVICE" = "$ENV_AZURE_OPEN_AI" ]; then
  if [ -z "$COMPLETION_MODEL" ]; then
    COMPLETION_MODEL="$ENV_COMPLETION_MODEL_AZURE_OPEN_AI"
  fi
  if [ -z "$PLANNER_MODEL" ]; then
    PLANNER_MODEL="$ENV_PLANNER_MODEL_AZURE_OPEN_AI"
  fi
  # TO DO: Validate model values if set by command line.
fi

if [ -z "$EMBEDDING_MODEL" ]; then
  EMBEDDING_MODEL="$ENV_EMBEDDING_MODEL"
  # TO DO: Validate model values if set by command line.
fi

echo "#########################"
echo "# Backend configuration #"
echo "#########################"

# Install dev certificate
case "$OSTYPE" in
darwin*)
  dotnet dev-certs https --trust
  if [ $? -ne 0 ]; then $(exit) 1; fi
  ;;
msys*)
  dotnet dev-certs https --trust
  if [ $? -ne 0 ]; then exit 1; fi
  ;;
cygwin*)
  dotnet dev-certs https --trust
  if [ $? -ne 0 ]; then exit 1; fi
  ;;
linux*)
  dotnet dev-certs https
  if [ $? -ne 0 ]; then exit 1; fi
  ;;
esac

WEBAPI_PROJECT_PATH="${SCRIPT_DIRECTORY}/../webapi"

echo "Setting 'APIKey' user secret for $AI_SERVICE..."
if [ "$AI_SERVICE" = "$ENV_OPEN_AI" ]; then
  dotnet user-secrets set --project $WEBAPI_PROJECT_PATH SemanticMemory:Services:OpenAI:APIKey $API_KEY
  if [ $? -ne 0 ]; then exit 1; fi
  AISERVICE_OVERRIDES="{
    \"OpenAI\":
      {
        \"TextModel\": \"${COMPLETION_MODEL}\",
        \"EmbeddingModel\": \"${EMBEDDING_MODEL}\",
      }
    }"
else
  dotnet user-secrets set --project $WEBAPI_PROJECT_PATH SemanticMemory:Services:AzureOpenAIText:APIKey $API_KEY
  if [ $? -ne 0 ]; then exit 1; fi
  dotnet user-secrets set --project $WEBAPI_PROJECT_PATH SemanticMemory:Services:AzureOpenAIEmbedding:APIKey $API_KEY
  if [ $? -ne 0 ]; then exit 1; fi
  AISERVICE_OVERRIDES="{
    \"AzureOpenAIText\": {
        \"Endpoint\": \"${ENDPOINT}\",
        \"Deployment\": \"${COMPLETION_MODEL}\"
      },
    \"AzureOpenAIEmbedding\": {
        \"Endpoint\": \"${ENDPOINT}\",
        \"Deployment\": \"${EMBEDDING_MODEL}\"
      }
    }"
fi

APPSETTINGS_OVERRIDES="{
  \"Authentication\": {
    \"Type\": \"${AUTH_TYPE}\",
    \"AzureAd\": {
      \"Instance\": \"${INSTANCE}\",
      \"TenantId\": \"${TENANT_ID}\",
      \"ClientId\": \"${BACKEND_CLIENT_ID}\",
      \"Scopes\": \"${ENV_SCOPES}\"
    }
  },
  \"Planner\": {
    \"Model\": \"${PLANNER_MODEL}\"
  },
  \"SemanticMemory\": {
    \"TextGeneratorType\": \"${AI_SERVICE}\",
    \"DataIngestion\": {
      \"EmbeddingGeneratorTypes\": [\"${AI_SERVICE}\"]
    },
    \"Retrieval\": {
      \"EmbeddingGeneratorType\": \"${AI_SERVICE}\"
    },
    \"Services\": ${AISERVICE_OVERRIDES}
  },
  \"Frontend\": {
    \"AadClientId\": \"${FRONTEND_CLIENT_ID}\"
  }
}"
APPSETTINGS_OVERRIDES_FILEPATH="${WEBAPI_PROJECT_PATH}/appsettings.${ENV_ASPNETCORE}.json"

echo "Setting up 'appsettings.${ENV_ASPNETCORE}.json' for $AI_SERVICE..."
echo $APPSETTINGS_OVERRIDES >$APPSETTINGS_OVERRIDES_FILEPATH

echo "($APPSETTINGS_OVERRIDES_FILEPATH)"
echo "========"
cat $APPSETTINGS_OVERRIDES_FILEPATH
echo "========"

echo ""
echo "##########################"
echo "# Frontend configuration #"
echo "##########################"

WEBAPP_PROJECT_PATH="${SCRIPT_DIRECTORY}/../webapp"
WEBAPP_ENV_FILEPATH="${WEBAPP_PROJECT_PATH}/.env"

echo "Setting up '.env' for webapp..."
echo "REACT_APP_BACKEND_URI=https://localhost:40443/" >$WEBAPP_ENV_FILEPATH

echo "($WEBAPP_ENV_FILEPATH)"
echo "========"
cat $WEBAPP_ENV_FILEPATH
echo "========"

echo "Done!"
