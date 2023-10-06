<#
.SYNOPSIS
Deploy CopilotChat's MemoryPipeline to Azure
#>

param(
    [Parameter(Mandatory)]
    [string]
    # Subscription to which to make the deployment
    $Subscription,

    [Parameter(Mandatory)]
    [string]
    # Resource group to which to make the deployment
    $ResourceGroupName,
    
    [Parameter(Mandatory)]
    [string]
    # Name of the previously deployed Azure deployment 
    $DeploymentName,

    [string]
    # CopilotChat memorypipeline package to deploy
    $PackageFilePath = "$PSScriptRoot/out/memorypipeline.zip"
)

# Ensure $PackageFilePath exists
if (!(Test-Path $PackageFilePath)) {
    Write-Error "Package file '$PackageFilePath' does not exist. Have you run 'package-memorypipeline.ps1' yet?"
    exit 1
}

az account show --output none
if ($LASTEXITCODE -ne 0) {
    Write-Host "Log into your Azure account"
    az login --output none
}

az account set -s $Subscription
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

Write-Host "Getting Azure WebApp resource name..."
$memoryPipelineName=$(az deployment group show --name $DeploymentName --resource-group $ResourceGroupName --output json | ConvertFrom-Json).properties.outputs.memoryPipelineName.value
if ($null -eq $memoryPipelineName) {
    Write-Error "Could not get Azure WebApp resource name from deployment output."
    exit 1
}

Write-Host "Azure WebApp name: $memoryPipelineName"

Write-Host "Configuring Azure WebApp to run from package..."
az webapp config appsettings set --resource-group $ResourceGroupName --name $memoryPipelineName --settings WEBSITE_RUN_FROM_PACKAGE="1" --output none
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Deploying '$PackageFilePath' to Azure WebApp '$memoryPipelineName'..."
az webapp deployment source config-zip --resource-group $ResourceGroupName --name $memoryPipelineName --src $PackageFilePath
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}