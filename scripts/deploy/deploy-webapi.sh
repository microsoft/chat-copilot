#!/bin/bash

# Deploy Chat Copilot application to Azure

usage() {
    echo "Usage: $0 -d DEPLOYMENT_NAME -s SUBSCRIPTION -rg RESOURCE_GROUP [OPTIONS]"
    echo ""
    echo "Arguments:"
    echo "  -d, --deployment-name DEPLOYMENT_NAME   Name of the deployment from a 'deploy-azure.sh' deployment (mandatory)"
    echo "  -s, --subscription SUBSCRIPTION         Subscription to which to make the deployment (mandatory)"
    echo "  -rg, --resource-group RESOURCE_GROUP    Resource group name from a 'deploy-azure.sh' deployment (mandatory)"
    echo "  -p, --package PACKAGE_FILE_PATH         Path to the package file from a 'package-webapi.sh' run (default: \"./out/webapi.zip\")"
    echo "  -o, --slot DEPLOYMENT_SLOT              Name of the target web app deployment slot"
    echo "  -r, --register-app                      Switch to add our URI in app registration's redirect URIs if missing"
    echo "  -c, --register-cors                     Register service with the plugins as allowed CORS origin"
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
    -r|--register-app)
        REGISTER_APP=true
        shift
        ;;
    -o|--slot)
        DEPLOYMENT_SLOT="$2"
        shift
        shift
        ;;
    -c|--register-cors)
        REGISTER_CORS=true
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
WEB_API_URL=$(echo $DEPLOYMENT_JSON | jq -r '.properties.outputs.webapiUrl.value')
echo "WEB_API_URL: $WEB_API_URL"
WEB_API_NAME=$(echo $DEPLOYMENT_JSON | jq -r '.properties.outputs.webapiName.value')
echo "WEB_API_NAME: $WEB_API_NAME"
PLUGIN_NAMES=$(echo $DEPLOYMENT_JSON | jq -r '.properties.outputs.pluginNames.value[]')
# Remove double quotes
PLUGIN_NAMES=${PLUGIN_NAMES//\"/}
echo "PLUGIN_NAMES: $PLUGIN_NAMES"
# Ensure $WEB_API_NAME is set
if [[ -z "$WEB_API_NAME" ]]; then
    echo "Could not get Azure WebApp resource name from deployment output."
    exit 1
fi

echo "Configuring Azure WebApp to run from package..."
az webapp config appsettings set --resource-group $RESOURCE_GROUP --name $WEB_API_NAME --settings WEBSITE_RUN_FROM_PACKAGE="1" --output none
if [ $? -ne 0 ]; then
    echo "Could not configure Azure WebApp to run from package."
    exit 1
fi

# Set up deployment command as a string
AZ_WEB_APP_CMD="az webapp deployment source config-zip --resource-group $RESOURCE_GROUP --name $WEB_API_NAME --src $PACKAGE_FILE_PATH"

ORIGINS="$WEB_API_URL"
if [ -n "$DEPLOYMENT_SLOT" ]; then
    AZ_WEB_APP_CMD+=" --slot ${DEPLOYMENT_SLOT}"
    echo "Checking whether slot $DEPLOYMENT_SLOT exists for $WEB_APP_NAME..."
    SLOT_INFO=$(az webapp deployment slot list --resource-group $RESOURCE_GROUP --name $WEB_API_NAME)

    AVAILABLE_SLOTS=$(echo $SLOT_INFO | jq '.[].name')
    ORIGINS=$(echo $slotInfo | jq '.[].defaultHostName')
    SLOT_EXISTS=false

    # Checking if the slot exists
    for SLOT in $(echo "$AVAILABLE_SLOTS" | tr '\n' ' '); do
        if [[ "$SLOT" == "$DEPLOYMENT_SLOT" ]]; then
            SLOT_EXISTS=true
            break
        fi
    done

    if [[ "$SLOT_EXISTS" = false ]]; then 
        echo "Deployment slot ${DEPLOYMENT_SLOT} does not exist, creating..."
        
        az webapp deployment slot create --slot=$DEPLOYMENT_SLOT --resource-group=$RESOURCE_GROUP --name $WEB_API_NAME

        ORIGINS=$(az webapp deployment slot list --resource-group=$RESOURCE_GROUP --name $WEB_API_NAME | jq '.[].defaultHostName')
    fi
fi

echo "Deploying '$PACKAGE_FILE_PATH' to Azure WebApp '$WEB_API_NAME'..."
eval "$AZ_WEB_APP_CMD"
if [ $? -ne 0 ]; then
    echo "Could not deploy '$PACKAGE_FILE_PATH' to Azure WebApp '$WEB_API_NAME'."
    exit 1
fi

if [[ -n $REGISTER_APP ]]; then
    WEBAPI_SETTINGS=$(az webapp config appsettings list --name $WEB_API_NAME --resource-group $RESOURCE_GROUP --output json)
    FRONTEND_CLIENT_ID=$(echo $WEBAPI_SETTINGS | jq -r '.[] | select(.name == "Frontend:AadClientId") | .value')
    OBJECT_ID=$(az ad app show --id $FRONTEND_CLIENT_ID | jq -r '.id')
    REDIRECT_URIS=$(az rest --method GET --uri "https://graph.microsoft.com/v1.0/applications/$OBJECT_ID" --headers 'Content-Type=application/json' | jq -r '.spa.redirectUris[]')
    NEED_TO_UPDATE_REG=false

    for ADDRESS in $(echo "$ORIGINS"); do
        ORIGIN="https://$ADDRESS"
        echo "Ensuring '$ORIGIN' is included in AAD app registration's redirect URIs..."

        if [[ ! "$REDIRECT_URIS" =~ "$ORIGIN" ]]; then
            if [[ -n $REDIRECT_URIS ]]; then
                REDIRECT_URIS=$(echo "$REDIRECT_URIS,$ORIGIN")
            else
                REDIRECT_URIS=$(echo "$ORIGIN")
            fi
            NEED_TO_UPDATE_REG=true
        fi 
    done 

    if [ $NEED_TO_UPDATE_REG = true ]; then
        BODY="{spa:{redirectUris:['$(echo "$REDIRECT_URIS")']}}"
        BODY="${BODY//\,/\',\'}"

        echo "Updating redirects with $BODY"

        az rest \
            --method PATCH \
            --uri "https://graph.microsoft.com/v1.0/applications/$OBJECT_ID" \
            --headers 'Content-Type=application/json' \
            --body $BODY

        if [ $? -ne 0 ]; then
            echo "Failed to update app registration $OBJECT_ID with redirect URIs"
            exit 1
        fi
    fi
fi

if [[ -n $REGISTER_CORS ]]; then
    for PLUGIN_NAME in $PLUGIN_NAMES; do
        ALLOWED_ORIGINS=$(az webapp cors show --name $PLUGIN_NAME --resource-group $RESOURCE_GROUP --subscription $SUBSCRIPTION | jq -r '.allowedOrigins[]')
        for ADDRESS in $(echo "$ORIGINS"); do
            ORIGIN="https://$ADDRESS"
            echo "Ensuring '$ORIGIN' is included in CORS origins for plugin '$PLUGIN_NAME'..."
            if [[ ! "$ALLOWED_ORIGINS" =~ "$ORIGIN" ]]; then
                az webapp cors add --name $PLUGIN_NAME --resource-group $RESOURCE_GROUP --subscription $SUBSCRIPTION --allowed-origins "$ORIGIN"
                if [ $? -ne 0 ]; then
                    echo "Failed to update CORS origins with $ORIGIN"
                    exit 1
                fi
            fi 
        done 
    done 
fi

echo "To verify your deployment, go to 'https://$WEB_API_URL/' in your browser."
