#!/usr/bin/env bash

# Deploy CopilotChat's WebAPI to Azure.

set -e

usage() {
    echo "Usage: $0 -d DEPLOYMENT_NAME -s SUBSCRIPTION -rg RESOURCE_GROUP [OPTIONS]"
    echo ""
    echo "Arguments:"
    echo "  -d, --deployment-name DEPLOYMENT_NAME   Name of the deployment from a 'deploy-azure.sh' deployment (mandatory)"
    echo "  -s, --subscription SUBSCRIPTION         Subscription to which to make the deployment (mandatory)"
    echo "  -rg, --resource-group RESOURCE_GROUP    Resource group name from a 'deploy-azure.sh' deployment (mandatory)"
    echo "  -p, --package PACKAGE_FILE_PATH         Path to the WebAPI package file from a 'package-webapi.sh' run (default: \"./out/webapi.zip\")"
    echo "  -o, --slot DEPLOYMENT_SLOT              Name of the target web app deployment slot"
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
        -o|--slot)
        DEPLOYMENT_SLOT="$2"
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
eval WEB_APP_NAME=$(az deployment group show --name $DEPLOYMENT_NAME --resource-group $RESOURCE_GROUP --output json | jq -r '.properties.outputs.webapiName.value')
# Ensure $WEB_APP_NAME is set
if [[ -z "$WEB_APP_NAME" ]]; then
    echo "Could not get Azure WebApp resource name from deployment output."
    exit 1
fi

echo "Azure WebApp name: $WEB_APP_NAME"

echo "Configuring Azure WebApp to run from package..."
az webapp config appsettings set --resource-group $RESOURCE_GROUP --name $WEB_APP_NAME --settings WEBSITE_RUN_FROM_PACKAGE="1" > /dev/null
if [ $? -ne 0 ]; then
    echo "Could not configure Azure WebApp to run from package."
    exit 1
fi

if [ -n "$DEPLOYMENT_SLOT" ]; then

    echo "Checking if slot $DEPLOYMENT_SLOT exists for $WEB_APP_NAME..."

    # Getting the list of slots
    AVAILABLE_SLOTS=$(az webapp deployment slot list --resource-group $RESOURCE_GROUP --name $WEB_APP_NAME | jq -r '.[].name')

    SLOT_EXISTS=false

    # Checking if the slot exists
    for SLOT in $AVAILABLE_SLOTS; do
        if [ "$SLOT" == "$DEPLOYMENT_SLOT" ]; then
            SLOT_EXISTS=true
            break
        fi
    done

    # If slot does not exist, create it
    if [ "$SLOT_EXISTS" == "false" ]; then
        echo "Slot $DEPLOYMENT_SLOT does not exist, creating..."
        az webapp deployment slot create --slot $DEPLOYMENT_SLOT --resource-group $RESOURCE_GROUP --name $WEB_APP_NAME > /dev/null
    fi
fi


echo "Deploying '$PACKAGE_FILE_PATH' to Azure WebApp '$WEB_APP_NAME'..."

azWebAppCommand="az webapp deployment source config-zip --resource-group $RESOURCE_GROUP --name $WEB_APP_NAME --src $PACKAGE_FILE_PATH"

if [ ! -z "$DEPLOYMENT_SLOT" ]; then
    azWebAppCommand+=" --slot $DEPLOYMENT_SLOT"
fi

eval $azWebAppCommand


if [ $? -ne 0 ]; then
    echo "Could not deploy '$PACKAGE_FILE_PATH' to Azure WebApp '$WEB_APP_NAME'."
    exit 1
fi

eval WEB_APP_URL=$(az deployment group show --name $DEPLOYMENT_NAME --resource-group $RESOURCE_GROUP --output json | jq -r '.properties.outputs.webapiUrl.value')
echo "To verify your deployment, go to 'https://$WEB_APP_URL/healthz' in your browser."
