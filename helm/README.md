# Introduction

This is the helm chart used for deploying Quartech Co-Pilot into a K8s environment.

# Getting Started

TODO: Guide users through getting your code up and running on their own system. In this section you can talk about:

1. Installation process
2. Software dependencies
3. Latest releases
4. API references

# Build and Test

`helm lint`
Run helm lint to validate your chart's structure and identify any obvious issues.

`helm template -s templates/webapp/deployment.yaml .`
Run helm template to test the generation of the K8s files

`helm upgrade -i -f values.yaml -f values-dev.yaml -n copilot-dev dev .`

# Contribute

Reach out to David (david.camden@quartech.com) if you want to contribute.
