#!/usr/bin/env bash

# Deploy Chat Copilot application to Azure.

set -e

usage() {
    echo "Usage: $0 -d DEPLOYMENT_NAME -s SUBSCRIPTION -rg RESOURCE_GROUP [OPTIONS]"
    echo ""
    echo "Arguments:"
    echo "  -d, --deployment-name DEPLOYMENT_NAME   Name of the deployment from a 'deploy-azure.sh' deployment (mandatory)"
    echo "  -s, --subscription SUBSCRIPTION         Subscription to which to make the deployment (mandatory)"
    echo "  -rg, --resource-group RESOURCE_GROUP    Resource group name from a 'deploy-azure.sh' deployment (mandatory)"
    echo "  -p, --package PACKAGE_FILE_PATH         Path to the package file from a 'package-webapi.sh' run (default: \"./out/webapi.zip\")"
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
        -rg|--resource-group)
        RESOURCE_GROUP="$2"
        shift
        shift
        ;;
        -p|--package)
        PACKAGE_FILE_PATH="$2"
        shift
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
if [[ -z "$DEPLOYMENT_NAME" ]] || [[ -z "$SUBSCRIPTION" ]] || [[ -z "$RESOURCE_GROUP" ]]; then
    usage
    exit 1
fi

# Set defaults
: "${PACKAGE_FILE_PATH:="$(dirname "$0")/out/webapi.zip"}"

# Ensure $PACKAGE_FILE_PATH exists
if [[ ! -f "$PACKAGE_FILE_PATH" ]]; then
    echo "Package file '$PACKAGE_FILE_PATH' does not exist. Have you run 'package-webapi.sh' yet?"
    exit 1
fi

az account show --output none
if [ $? -ne 0 ]; then
    echo "Log into your Azure account"
    az login --use-device-code
fi

az account set -s "$SUBSCRIPTION"

echo "Getting Azure WebApp resource name..."
DEPLOYMENT_JSON=$(az deployment group show --name $DEPLOYMENT_NAME --resource-group $RESOURCE_GROUP --output json)
eval WEB_API_URL=$(echo $DEPLOYMENT_JSON | jq -r '.properties.outputs.webapiUrl.value')
echo "WEB_API_URL: $WEB_API_URL"
eval WEB_API_NAME=$(echo $DEPLOYMENT_JSON | jq -r '.properties.outputs.webapiName.value')
echo "WEB_API_NAME: $WEB_API_NAME"
# Ensure $WEB_API_NAME is set
if [[ -z "$WEB_API_NAME" ]]; then
    echo "Could not get Azure WebApp resource name from deployment output."
    exit 1
fi

echo "Azure WebApp name: $WEB_API_NAME"

echo "Configuring Azure WebApp to run from package..."
az webapp config appsettings set --resource-group $RESOURCE_GROUP --name $WEB_API_NAME --settings WEBSITE_RUN_FROM_PACKAGE="1"
if [ $? -ne 0 ]; then
    echo "Could not configure Azure WebApp to run from package."
    exit 1
fi

echo "Deploying '$PACKAGE_FILE_PATH' to Azure WebApp '$WEB_API_NAME'..."
az webapp deployment source config-zip --resource-group $RESOURCE_GROUP --name $WEB_API_NAME --src $PACKAGE_FILE_PATH --debug
if [ $? -ne 0 ]; then
    echo "Could not deploy '$PACKAGE_FILE_PATH' to Azure WebApp '$WEB_API_NAME'."
    exit 1
fi

ORIGIN="https://$WEB_API_URL"
echo "Ensuring '$ORIGIN' is included in AAD app registration's redirect URIs..."
eval OBJECT_ID=$(az ad app show --id $FRONTEND_CLIENT_ID | jq -r '.id')

if [ "$NO_REDIRECT" != true ]; then
    REDIRECT_URIS=$(az rest --method GET --uri "https://graph.microsoft.com/v1.0/applications/$OBJECT_ID" --headers 'Content-Type=application/json' | jq -r '.spa.redirectUris')
    if [[ ! "$REDIRECT_URIS" =~ "$ORIGIN" ]]; then
        BODY="{spa:{redirectUris:['"
        eval BODY+=$(echo $REDIRECT_URIS | jq $'join("\',\'")')
        BODY+="','$ORIGIN']}}"

        az rest \
        --method PATCH \
        --uri "https://graph.microsoft.com/v1.0/applications/$OBJECT_ID" \
        --headers 'Content-Type=application/json' \
        --body $BODY
    fi
    if [ $? -ne 0 ]; then
        echo "Failed to update app registration"
        exit 1
    fi
fi

echo "To verify your deployment, go to 'https://$WEB_APP_URL/' in your browser."
