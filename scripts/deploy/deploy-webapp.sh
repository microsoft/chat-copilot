#!/usr/bin/env bash

# Deploy CopilotChat's WebApp to Azure

set -e

SCRIPT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

usage() {
    echo "Usage: $0 -d DEPLOYMENT_NAME -s SUBSCRIPTION -rg RESOURCE_GROUP -c FRONTEND_CLIENT_ID [OPTIONS]"
    echo ""
    echo "Arguments:"
    echo "  -d, --deployment-name DEPLOYMENT_NAME  Name of the deployment from a 'deploy-azure.sh' deployment (mandatory)"
    echo "  -s, --subscription SUBSCRIPTION        Subscription to which to make the deployment (mandatory)"
    echo "  -rg, --resource-group RESOURCE_GROUP   Resource group name from a 'deploy-azure.sh' deployment (mandatory)"
    echo "  -c, --client-id FRONTEND_CLIENT_ID     Client application ID for the frontend web app (mandatory)"
    echo "  -v  --version VERSION                  Version to display in UI (default: 1.0.0)"
    echo "  -i  --version-info INFO                Additional info to put in version details"
    echo "  -nr, --no-redirect                     Do not attempt to register redirect URIs with the client application"
    echo "  -env --environment                     Specify a SWA environment"
}

# Default the environment variable to Production
ENVIRONMENT="Production"

# Parse arguments
while [[ $# -gt 0 ]]; do
    key="$1"
    case $key in
    -d | --deployment-name)
        DEPLOYMENT_NAME="$2"
        shift
        shift
        ;;
    -s | --subscription)
        SUBSCRIPTION="$2"
        shift
        shift
        ;;
    -rg | --resource-group)
        RESOURCE_GROUP="$2"
        shift
        shift
        ;;
    -c | --client-id)
        FRONTEND_CLIENT_ID="$2"
        shift
        shift
        ;;
    -v | --version)
        VERSION="$2"
        shift
        shift
        ;;
    -i | --version-info)
        VERSION_INFO="$2"
        shift
        shift
        ;;
    -nr | --no-redirect)
        NO_REDIRECT=true
        shift
        ;;
        -env|--environment)
        ENVIRONMENT="$2"                # Overwrite the default value if the option is provided
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
if [[ -z "$DEPLOYMENT_NAME" ]] || [[ -z "$SUBSCRIPTION" ]] || [[ -z "$RESOURCE_GROUP" ]] || [[ -z "$FRONTEND_CLIENT_ID" ]]; then
    usage
    exit 1
fi

az account show --output none
if [ $? -ne 0 ]; then
    echo "Log into your Azure account"
    az login --use-device-code
fi

az account set -s "$SUBSCRIPTION"

echo "Getting deployment outputs..."
DEPLOYMENT_JSON=$(az deployment group show --name $DEPLOYMENT_NAME --resource-group $RESOURCE_GROUP --output json)
# get the webapiUrl from the deployment outputs
eval WEB_APP_URL=$(echo $DEPLOYMENT_JSON | jq -r '.properties.outputs.webappUrl.value')
echo "WEB_APP_URL: $WEB_APP_URL"
eval WEB_APP_NAME=$(echo $DEPLOYMENT_JSON | jq -r '.properties.outputs.webappName.value')
echo "WEB_APP_NAME: $WEB_APP_NAME"
eval WEB_API_URL=$(echo $DEPLOYMENT_JSON | jq -r '.properties.outputs.webapiUrl.value')
echo "WEB_API_URL: $WEB_API_URL"
eval WEB_API_NAME=$(echo $DEPLOYMENT_JSON | jq -r '.properties.outputs.webapiName.value')
echo "WEB_API_NAME: $WEB_API_NAME"
eval PLUGIN_NAMES=$(echo $DEPLOYMENT_JSON | jq -r '.properties.outputs.pluginNames.value[]')
echo "PLUGIN_NAMES: $PLUGIN_NAMES"

WEB_API_SETTINGS=$(az webapp config appsettings list --name $WEB_API_NAME --resource-group $RESOURCE_GROUP --output json)
eval WEB_API_CLIENT_ID=$(echo $WEB_API_SETTINGS | jq '.[] | select(.name=="Authentication:AzureAd:ClientId").value')
eval WEB_API_TENANT_ID=$(echo $WEB_API_SETTINGS | jq '.[] | select(.name=="Authentication:AzureAd:TenantId").value')
eval WEB_API_INSTANCE=$(echo $WEB_API_SETTINGS | jq '.[] | select(.name=="Authentication:AzureAd:Instance").value')
eval WEB_API_SCOPE=$(echo $WEB_API_SETTINGS | jq '.[] | select(.name=="Authentication:AzureAd:Scopes").value')

ENV_FILE_PATH="$SCRIPT_ROOT/../../webapp/.env"
echo "Writing environment variables to '$ENV_FILE_PATH'..."
echo "REACT_APP_BACKEND_URI=https://$WEB_API_URL/" >$ENV_FILE_PATH
echo "REACT_APP_AUTH_TYPE=AzureAd" >>$ENV_FILE_PATH
# Trim any trailing slash from instance before generating authority
WEB_API_INSTANCE=${WEB_API_INSTANCE%/}
echo "REACT_APP_AAD_AUTHORITY=$WEB_API_INSTANCE/$WEB_API_TENANT_ID" >>$ENV_FILE_PATH
echo "REACT_APP_AAD_CLIENT_ID=$FRONTEND_CLIENT_ID" >>$ENV_FILE_PATH
echo "REACT_APP_AAD_API_SCOPE=api://$WEB_API_CLIENT_ID/$WEB_API_SCOPE" >>$ENV_FILE_PATH
echo "REACT_APP_SK_VERSION=$VERSION" >>$ENV_FILE_PATH
echo "REACT_APP_SK_BUILD_INFO=$VERSION_INFO" >>$ENV_FILE_PATH

echo "Writing swa-cli.config.json..."
SWA_CONFIG_FILE_PATH="$SCRIPT_ROOT/../../webapp/swa-cli.config.json"
SWA_CONFIG_TEMPLATE_FILE_PATH="$SCRIPT_ROOT/../../webapp/template.swa-cli.config.json"
swaConfig=$(cat $SWA_CONFIG_TEMPLATE_FILE_PATH)
swaConfig=$(echo $swaConfig | sed "s/{{appDevserverUrl}}/https:\/\/${WEB_APP_URL}/")
swaConfig=$(echo $swaConfig | sed "s/{{appName}}/$WEB_API_NAME/")
swaConfig=$(echo $swaConfig | sed "s/{{resourceGroup}}/$RESOURCE_GROUP/")
swaConfig=$(echo $swaConfig | sed "s/{{subscription-id}}/$SUBSCRIPTION/")
echo $swaConfig >$SWA_CONFIG_FILE_PATH

pushd "$SCRIPT_ROOT/../../webapp"

echo "Installing yarn dependencies..."
yarn install
if [ $? -ne 0 ]; then
    echo "Failed to install yarn dependencies"
    exit 1
fi

echo "Building webapp..."
swa build
if [ $? -ne 0 ]; then
    echo "Failed to build webapp"
    exit 1
fi

echo "Deploying webapp..."
swa deploy --subscription-id $SUBSCRIPTION --app-name $WEB_APP_NAME --env $ENVIRONMENT
if [ $? -ne 0 ]; then
    echo "Failed to deploy webapp"
    exit 1
fi

popd

ENVIRONMENTS=$(az staticwebapp environment list --name "$WEB_APP_NAME")

for env in $(echo "${ENVIRONMENTS}" | jq -r '.[] | @base64'); do
    HOSTNAME=$(echo "$env" | base64 --decode | jq -r '.hostname')
    ORIGIN="https://$HOSTNAME"
    
    echo "Ensuring origin '$ORIGIN' is included in CORS origins for webapi '$WEB_API_NAME'..."
    CORS_RESULT=$(az webapp cors show --name $WEB_API_NAME --resource-group $RESOURCE_GROUP --subscription $SUBSCRIPTION | jq '.allowedOrigins | index("$ORIGIN")')
    if [[ "$CORS_RESULT" == "null" ]]; then
        echo "Adding CORS origin '$ORIGIN' to webapi '$WEB_API_NAME'..."
        az webapp cors add --name $WEB_API_NAME --resource-group $RESOURCE_GROUP --subscription $SUBSCRIPTION --allowed-origins $ORIGIN
    fi

    for PLUGIN_NAME in $PLUGIN_NAMES; do
        echo "Ensuring origin '$ORIGIN' is included in CORS origins for plugin '$PLUGIN_NAME'..."
        CORS_RESULT=$(az webapp cors show --name $PLUGIN_NAME --resource-group $RESOURCE_GROUP --subscription $SUBSCRIPTION | jq '.allowedOrigins | index("$ORIGIN")')
        if [[ "$CORS_RESULT" == "null" ]]; then
            echo "Adding CORS origin '$ORIGIN' to plugin '$PLUGIN_NAME'..."
            az webapp cors add --name $PLUGIN_NAME --resource-group $RESOURCE_GROUP --subscription $SUBSCRIPTION --allowed-origins $ORIGIN
        fi
    done

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

    echo "To verify your deployment, go to 'https://$WEB_APP_URL' in your browser."
done
