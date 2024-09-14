#!/usr/bin/env bash

##########################################################################
#                                 Formatter
#                               WEBAPP / WBAPI
#
# How to use: ./format.sh
#
# Format (write): ./format.sh
# Check (no write): ./format.sh --check
#
# Note: By default, this script will format and write changes for webapp and webapi.
#
# CSharpier: https://csharpier.com/docs/About
# CSharpier Options: https://csharpier.com/docs/CLI
#
##########################################################################

parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
cd "$parent_path"

if [ "$1" == "--check" ]; then
  echo "###################################################"
  echo "Format Check WebAPI: Checking for formatting issues"
  echo "###################################################"

  cd ../webapi
  dotnet tool restore --tool-manifest ../webapi/.config/dotnet-tools.json
  dotnet csharpier --check .

  echo "###################################################"
  echo "Format Check WebAPP: Checking for formatting issues"
  echo "###################################################"

  cd ../webapp
  npm run format

else
  echo "###################################################"
  echo "Format Fix WebAPI: Checking for formatting issues"
  echo "###################################################"

  cd ../webapi
  dotnet tool restore
  dotnet csharpier .

  echo "###################################################"
  echo "Format Fix WebAPP: Checking for formatting issues"
  echo "###################################################"

  cd ../webapp
  npm run format:fix

fi

