#!/usr/bin/env bash

# Package CopilotChat's plugins for deployment to Azure

set -e

SCRIPT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIRECTORY="$SCRIPT_ROOT"

usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Arguments:"
    echo "  -c, --configuration CONFIGURATION      Build configuration (default: Release)"
    echo "  -d, --dotnet DOTNET_FRAMEWORK_VERSION  Target dotnet framework (default: net6.0)"
    echo "  -o, --output OUTPUT_DIRECTORY          Output directory (default: $SCRIPT_ROOT)"
    echo "  -v  --version VERSION                  Version to set files to (default: 1.0.0)"
    echo "  -i  --info INFO                        Additional info to put in version details"
    echo "  -nz, --no-zip                          Do not zip package (default: false)"
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    key="$1"
    case $key in
    -c | --configuration)
        CONFIGURATION="$2"
        shift
        shift
        ;;
    -d | --dotnet)
        DOTNET="$2"
        shift
        shift
        ;;
    -o | --output)
        OUTPUT_DIRECTORY="$2"
        shift
        shift
        ;;
    -v | --version)
        VERSION="$2"
        shift
        shift
        ;;
    -i | --info)
        INFO="$2"
        shift
        shift
        ;;
    -nz | --no-zip)
        NO_ZIP=true
        shift
        ;;
    *)
        echo "Unknown option $1"
        usage
        exit 1
        ;;
    esac
done

# Set defaults
: "${CONFIGURATION:="Release"}"
: "${DOTNET:="net6.0"}"
: "${VERSION:="1.0.0"}"
: "${INFO:=""}"
: "${OUTPUT_DIRECTORY:="$SCRIPT_ROOT"}"

PUBLISH_OUTPUT_DIRECTORY="$OUTPUT_DIRECTORY/publish"
PUBLISH_ZIP_DIRECTORY="$OUTPUT_DIRECTORY/out/plugins"

if [[ ! -d "$PUBLISH_OUTPUT_DIRECTORY" ]]; then
    mkdir -p "$PUBLISH_OUTPUT_DIRECTORY"
fi
if [[ ! -d "$PUBLISH_ZIP_DIRECTORY" ]]; then
    mkdir -p "$PUBLISH_ZIP_DIRECTORY"
fi

PLUGIN_PROJECT_FILES=()
mapfile -d $'\0' PLUGIN_PROJECT_FILES < <(find "${SCRIPT_ROOT}/../../plugins" -name "*.csproj")

for PLUGIN_PROJECT_FILE in $PLUGIN_PROJECT_FILES; do
    PLUGIN_PROJECT_FILE=$(realpath "$PLUGIN_PROJECT_FILE")
    PLUGIN_NAME=$(basename "$PLUGIN_PROJECT_FILE" .csproj)
    PLUGIN_NAME="${PLUGIN_NAME,,}" # Lowercase

    if [[ "$PLUGIN_NAME" = "pluginshared" ]]; then
        continue
    fi

    echo "Packaging $PLUGIN_NAME from $PLUGIN_PROJECT_FILE"

    dotnet publish "$PLUGIN_PROJECT_FILE" \
        --configuration $CONFIGURATION \
        --framework $DOTNET \
        --output "$PUBLISH_OUTPUT_DIRECTORY" \
        //p:AssemblyVersion=$VERSION \
        //p:FileVersion=$VERSION \
        //p:InformationalVersion=$INFO

    if [ $? -ne 0 ]; then
        exit 1
    fi

    # if not NO_ZIP then zip the package
    PACKAGE_FILE_PATH="$PUBLISH_ZIP_DIRECTORY/$PLUGIN_NAME.zip"
    if [[ -z "$NO_ZIP" ]]; then
        pushd "$PUBLISH_OUTPUT_DIRECTORY"
        echo "Compressing to $PACKAGE_FILE_PATH"
        zip -r $PACKAGE_FILE_PATH .
        popd
    fi
done
