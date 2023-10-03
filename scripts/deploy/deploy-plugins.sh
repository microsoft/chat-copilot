#!/bin/bash

# Package CiopilotChat's MemoryPipeline for deployment to Azure

set -e

SCRIPT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIRECTORY="$SCRIPT_ROOT"
#!/bin/bash

# Deploy CopilotChat's MemoryPipeline to Azure.

set -e

usage() {
    echo "Usage: $0 -d DEPLOYMENT_NAME -s SUBSCRIPTION -rg RESOURCE_GROUP [OPTIONS]"
    echo ""
    echo "Arguments:"
    echo "  -d, --deployment-name DEPLOYMENT_NAME   Name of the deployment from a 'deploy-azure.sh' deployment (mandatory)"
    echo "  -s, --subscription SUBSCRIPTION         Subscription to which to make the deployment (mandatory)"
    echo "  -rg, --resource-group RESOURCE_GROUP    Resource group name from a 'deploy-azure.sh' deployment (mandatory)"
    echo "  -p, --packages PACKAGES_PATH            Path that contains the plugin packages to deploy"
}

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
    -p | --packages)
        PACKAGES_PATH="$2"
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
: "${PACKAGES_PATH:="$(dirname "$0")/out/plugins"}"

# Ensure $PACKAGES_PATH exists
if [[ ! -d "$PACKAGES_PATH" ]]; then
    echo "Package file '$PACKAGES_PATH' does not exist. Have you run 'package-plugins.sh' yet?"
    exit 1
fi

az account show --output none
if [ $? -ne 0 ]; then
    echo "Log into your Azure account"
    az login --use-device-code
fi

az account set -s "$SUBSCRIPTION"

echo "Getting Azure Function resource names"
PLUGIN_DEPLOYMENT_NAMES=$(
    az deployment group show \
        --name $DEPLOYMENT_NAME \
        --resource-group $RESOURCE_GROUP \
        --output json | jq -r '.properties.outputs.pluginNames.value[]'
)
echo "-----Found the following Azure Function names-----"
for PLUGIN_DEPLOYMENT_NAME in $PLUGIN_DEPLOYMENT_NAMES; do
    echo $PLUGIN_DEPLOYMENT_NAME
done
echo ""

# Find the Azure Function resource name for each plugin package
# before we deploy the plugins. This can minimize the risk of
# deploying to the wrong Azure Function resource.
echo "---Matching plugins to Azure Function resources---"
PLUGIN_DEPLOYMENT_MATCHES=()
PLUGIN_PACKAGE_MATCHES=()
for PLUGIN_PACKAGE in $PACKAGES_PATH/*; do
    PLUGIN_PACKAGE_NAME=$(basename $PLUGIN_PACKAGE)
    PLUGIN_NAME=$(echo $PLUGIN_PACKAGE_NAME | sed 's/\.zip//g')

    echo "Looking for the resource for '$PLUGIN_NAME'..."

    MATCHED_NUMBER=0
    MATCHED_DEPLOYMENT=""
    for PLUGIN_DEPLOYMENT_NAME in $PLUGIN_DEPLOYMENT_NAMES; do
        if [[ "$PLUGIN_DEPLOYMENT_NAME" =~ ^function-.*$PLUGIN_NAME-plugin$ ]]; then
            MATCHED_NUMBER=$((MATCHED_NUMBER + 1))
            MATCHED_DEPLOYMENT=$PLUGIN_DEPLOYMENT_NAME
        fi
    done

    if [[ $MATCHED_NUMBER -eq 0 ]]; then
        echo "Could not find Azure Function resource name for plugin '$PLUGIN_NAME'."
        echo "Make sure the deployed Azure Function resource name matches the plugin zip package name."
        exit 1
    elif [[ $MATCHED_NUMBER -gt 1 ]]; then
        echo "Found multiple Azure Function resource names for plugin '$PLUGIN_NAME'."
        echo "Make sure the deployed Azure Function resource name matches the plugin zip package name."
        exit 1
    fi

    echo "Matched Azure Function resource name '$MATCHED_DEPLOYMENT' for '$PLUGIN_NAME'"
    PLUGIN_DEPLOYMENT_MATCHES+=($MATCHED_DEPLOYMENT)
    PLUGIN_PACKAGE_MATCHES+=($PLUGIN_PACKAGE)
done
echo ""

echo "-------Deploying plugins to Azure Functions-------"
for i in "${!PLUGIN_DEPLOYMENT_MATCHES[@]}"; do
    PLUGIN_DEPLOYMENT_NAME=${PLUGIN_DEPLOYMENT_MATCHES[$i]}
    PLUGIN_PACKAGE=${PLUGIN_PACKAGE_MATCHES[$i]}

    echo "Deploying '$PLUGIN_PACKAGE' to Azure Function '$PLUGIN_DEPLOYMENT_NAME'..."
    az functionapp deployment source config-zip \
        --resource-group $RESOURCE_GROUP \
        --name $PLUGIN_DEPLOYMENT_NAME \
        --src $PLUGIN_PACKAGE

    if [ $? -ne 0 ]; then
        echo "Could not deploy '$PLUGIN_PACKAGE' to Azure Function '$PLUGIN_DEPLOYMENT_NAME'."
        exit 1
    fi
done
