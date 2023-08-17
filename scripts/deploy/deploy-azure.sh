#!/bin/bash

# Deploy Chat Copilot Azure resources.

set -e

usage() {
    echo "Usage: $0 -d DEPLOYMENT_NAME -s SUBSCRIPTION -c BACKEND_CLIENT_ID -t AZURE_AD_TENANT_ID -ai AI_SERVICE_TYPE [OPTIONS]"
    echo ""
    echo "Arguments:"
    echo "  -d, --deployment-name DEPLOYMENT_NAME      Name for the deployment (mandatory)"
    echo "  -s, --subscription SUBSCRIPTION            Subscription to which to make the deployment (mandatory)"
    echo "  -c, --client-id BACKEND_CLIENT_ID          Azure AD client ID for the Web API backend app registration (mandatory)"
    echo "  -t, --tenant-id AZURE_AD_TENANT_ID         Azure AD tenant ID for authenticating users (mandatory)"
    echo "  -ai, --ai-service AI_SERVICE_TYPE          Type of AI service to use (i.e., OpenAI or AzureOpenAI) (mandatory)"
    echo "  -aiend, --ai-endpoint AI_ENDPOINT          Endpoint for existing Azure OpenAI resource"
    echo "  -aikey, --ai-service-key AI_SERVICE_KEY    API key for existing Azure OpenAI resource or OpenAI account"
    echo "  -rg, --resource-group RESOURCE_GROUP       Resource group to which to make the deployment (default: \"rg-\$DEPLOYMENT_NAME\")"
    echo "  -r, --region REGION                        Region to which to make the deployment (default: \"South Central US\")"
    echo "  -wr, --web-app-region WEB_APP_REGION       Region to deploy to the static web app into. This must be a region that supports static web apps. (default: \"West US 2\")"
    echo "  -a, --app-service-sku WEB_APP_SVC_SKU      SKU for the Azure App Service plan (default: \"B1\")"
    echo "  -i, --instance AZURE_AD_INSTANCE           Azure AD cloud instance for authenticating users"
    echo "                                             (default: \"https://login.microsoftonline.com\")"
    echo "  -ms, --memory-store                        Method to use to persist embeddings. Valid values are"
    echo "                                             \"AzureCognitiveSearch\" (default), \"Qdrant\", \"Postgres\" and \"Volatile\""
    echo "  -sap, --sql-admin-password                 Password for the PostgreSQL Server admin user"
    echo "  -nc, --no-cosmos-db                        Don't deploy Cosmos DB for chat storage - Use volatile memory instead"
    echo "  -ns, --no-speech-services                  Don't deploy Speech Services to enable speech as chat input"
    echo "  -dd, --debug-deployment                    Switches on verbose template deployment output"
    echo "  -ndp, --no-deploy-package                  Skips deploying the Web API package when set."
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    key="$1"
    case $key in
        -d|--deployment-name)
        DEPLOYMENT_NAME="$2"
        shift
        shift
        ;;
        -s|--subscription)
        SUBSCRIPTION="$2"
        shift
        shift
        ;;
        -c|--client-id)
        BACKEND_CLIENT_ID="$2"
        shift
        shift
        ;;
        -t|--tenant-id)
        AZURE_AD_TENANT_ID="$2"
        shift
        shift
        ;;
        -ai|--ai-service)
        AI_SERVICE_TYPE="$2"
        shift
        shift
        ;;
        -aikey|--ai-service-key)
        AI_SERVICE_KEY="$2"
        shift
        shift
        ;;
        -aiend|--ai-endpoint)
        AI_ENDPOINT="$2"
        shift
        shift
        ;;
        -rg|--resource-group)
        RESOURCE_GROUP="$2"
        shift
        shift
        ;;
        -r|--region)
        REGION="$2"
        shift
        shift
        ;;
        -wr|--web-app-region)
        WEB_APP_REGION="$2"
        shift
        shift
        ;;
        -a|--app-service-sku)
        WEB_APP_SVC_SKU="$2"
        shift
        shift
        ;;
        -i|--instance)
        AZURE_AD_INSTANCE="$2"
        shift
        shift
        ;;
        -ms|--memory-store)
        MEMORY_STORE=="$2"
        shift
        ;;
        -sap|--sql-admin-password)
        SQL_ADMIN_PASSWORD="$2"
        shift
        shift
        ;;
        -nc|--no-cosmos-db)
        NO_COSMOS_DB=true
        shift
        ;;
        -ns|--no-speech-services)
        NO_SPEECH_SERVICES=true
        shift
        ;;
        -dd|--debug-deployment)
        DEBUG_DEPLOYMENT=true
        shift
        ;;
        -ndp|--no-deploy-package)
        NO_DEPLOY_PACKAGE=true
        shift
        ;;
        *)
        echo "Unknown option $1"
        usage
        exit 1
        ;;
    esac
done

# Check mandatory arguments
if [[ -z "$DEPLOYMENT_NAME" ]] || [[ -z "$SUBSCRIPTION" ]] || [[ -z "$BACKEND_CLIENT_ID" ]] || [[ -z "$AZURE_AD_TENANT_ID" ]] || [[ -z "$AI_SERVICE_TYPE" ]]; then
    usage
    exit 1
fi

# Check if AI_SERVICE_TYPE is either OpenAI or AzureOpenAI
if [[ "${AI_SERVICE_TYPE,,}" != "openai" ]] && [[ "${AI_SERVICE_TYPE,,}" != "azureopenai" ]]; then
    echo "--ai-service must be either OpenAI or AzureOpenAI"
    usage
    exit 1
fi

# if AI_SERVICE_TYPE is AzureOpenAI
if [[ "${AI_SERVICE_TYPE,,}" = "azureopenai" ]]; then
    # Both AI_ENDPOINT and AI_SERVICE_KEY must be set or neither of them.
    if [[ (-z "$AI_ENDPOINT" && -n "$AI_SERVICE_KEY") || (-n "$AI_ENDPOINT" && -z "$AI_SERVICE_KEY") ]]; then
        echo "When --ai is 'AzureOpenAI', if either --ai-endpoint or --ai-service-key is set, then both must be set."
        usage
        exit 1
    fi

    # if AI_ENDPOINT and AI_SERVICE_KEY are not set, set NO_NEW_AZURE_OPENAI to false and tell the user, else set NO_NEW_AZURE_OPENAI to true
    if [[ -z "$AI_ENDPOINT" ]] && [[ -z "$AI_SERVICE_KEY" ]]; then
        NO_NEW_AZURE_OPENAI=false
        echo "When --ai is 'AzureOpenAI', if neither --ai-endpoint nor --ai-service-key are set, then a new Azure OpenAI resource will be created."
    else
        NO_NEW_AZURE_OPENAI=true
        echo "When --ai is 'AzureOpenAI', if both --ai-endpoint and --ai-service-key are set, then an existing Azure OpenAI resource will be used."
    fi
fi

# if AI_SERVICE_TYPE is OpenAI then AI_SERVICE_KEY is mandatory
if [[ "${AI_SERVICE_TYPE,,}" = "openai" ]] && [[ -z "$AI_SERVICE_KEY" ]]; then
    echo "When --ai is 'OpenAI', --ai-service-key must be set."
    usage
    exit 1
fi

# If MEMORY_STORE is Postges, then SQL_ADMIN_PASSWORD is mandatory
if [[ "${MEMORY_STORE,,}" = "postgres" ]] && [[ -z "$SQL_ADMIN_PASSWORD" ]]; then
    echo "When --memory-store is 'Postgres', --sql-admin-password must be set."
    usage
    exit 1
fi

# If resource group is not set, then set it to rg-DEPLOYMENT_NAME
if [ -z "$RESOURCE_GROUP" ]; then
    RESOURCE_GROUP="rg-${DEPLOYMENT_NAME}"
fi

TEMPLATE_FILE="$(dirname "$0")/main.bicep"

az account show --output none
if [ $? -ne 0 ]; then
    echo "Log into your Azure account"
    az login --use-device-code
fi

az account set -s "$SUBSCRIPTION"

# Set defaults
: "${REGION:="southcentralus"}"
: "${WEB_APP_SVC_SKU:="B1"}"
: "${WEB_APP_REGION:="westus2"}"
: "${AZURE_AD_INSTANCE:="https://login.microsoftonline.com"}"
: "${MEMORY_STORE:="AzureCognitiveSearch"}"
: "${NO_COSMOS_DB:=false}"
: "${NO_SPEECH_SERVICES:=false}"

# Create JSON config
JSON_CONFIG=$(cat << EOF
{
    "webAppServiceSku": { "value": "$WEB_APP_SVC_SKU" },
    "webappLocation": { "value": "$WEB_APP_REGION" },
    "aiService": { "value": "$AI_SERVICE_TYPE" },
    "aiApiKey": { "value": "$AI_SERVICE_KEY" },
    "deployWebApiPackage": { "value": $([ "$NO_DEPLOY_PACKAGE" = true ] && echo "false" || echo "true") },
    "aiEndpoint": { "value": "$([ ! -z "$AI_ENDPOINT" ] && echo "$AI_ENDPOINT")" },
    "azureAdInstance": { "value": "$AZURE_AD_INSTANCE" },
    "azureAdTenantId": { "value": "$AZURE_AD_TENANT_ID" },
    "webApiClientId": { "value": "$BACKEND_CLIENT_ID" },
    "deployNewAzureOpenAI": { "value": $([ "$NO_NEW_AZURE_OPENAI" = true ] && echo "false" || echo "true") },
    "memoryStore": { "value": "$MEMORY_STORE" },
    "sqlAdminPassword": { "value": "$SQL_ADMIN_PASSWORD" },
    "deployCosmosDB": { "value": $([ "$NO_COSMOS_DB" = true ] && echo "false" || echo "true") },
    "deploySpeechServices": { "value": $([ "$NO_SPEECH_SERVICES" = true ] && echo "false" || echo "true") }
}
EOF
)

echo "Ensuring resource group $RESOURCE_GROUP..."
az group create --location "$REGION" --name "$RESOURCE_GROUP" --tags Creator="$USER"

echo "Validating template file..."
az deployment group validate --name "$DEPLOYMENT_NAME" --resource-group "$RESOURCE_GROUP" --template-file "$TEMPLATE_FILE" --parameters "$JSON_CONFIG"

echo "Deploying Azure resources ($DEPLOYMENT_NAME)..."
if [ "$DEBUG_DEPLOYMENT" = true ]; then
    az deployment group create --name "$DEPLOYMENT_NAME" --resource-group "$RESOURCE_GROUP" --template-file "$TEMPLATE_FILE" --debug --parameters "$JSON_CONFIG"
else
    az deployment group create --name "$DEPLOYMENT_NAME" --resource-group "$RESOURCE_GROUP" --template-file "$TEMPLATE_FILE" --parameters "$JSON_CONFIG"
fi
