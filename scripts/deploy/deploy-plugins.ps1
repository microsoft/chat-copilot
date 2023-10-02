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
    # Path that contains the plugin packages to deploy
    $PackagesPath = "$PSScriptRoot/out/plugins"
)

# Ensure $PackageFilePath exists
if (!(Test-Path $PackagesPath)) {
    Write-Error "Package file '$PackagesPath' does not exist. Have you run 'package-plugins.ps1' yet?"
    exit 1
}

# Get all the plugin packages
$pluginPackages = Get-ChildItem -Path $PackagesPath -Filter *.zip
if ($null -eq $pluginPackages) {
    Write-Error "No plugin packages found in '$PackagesPath'. Have you run 'package-plugins.ps1' yet?"
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

Write-Host "Getting Azure Function resource names"
$pluginDeploymentNames = $(
    az deployment group show `
        --name $DeploymentName `
        --resource-group $ResourceGroupName `
        --output json | ConvertFrom-Json
).properties.outputs.pluginNames.value
Write-Host "-----Found the following Azure Function names-----"
foreach ($pluginDeploymentName in $pluginDeploymentNames) {
    Write-Host "$pluginDeploymentName"
}
Write-Host ""

# Find the Azure Function resource name for each plugin package
# before we deploy the plugins. This can minimize the risk of
# deploying to the wrong Azure Function resource.
Write-Host "---Matching plugins to Azure Function resources---"
$pluginDeploymentMatches = @{}
foreach ($pluginPackage in $pluginPackages) {
    $pluginName = $pluginPackage.BaseName
    Write-Host "Looking for the resource for '$pluginName'..."

    # Check if the plugin name matches any of the Azure Function names we got from the deployment output
    $matchedNumber = 0
    $matchedDeployment = ""
    foreach ($pluginDeploymentName in $pluginDeploymentNames) {
        if ($pluginDeploymentName -match "function-.*$pluginName-plugin") {
            $matchedNumber++
            $matchedDeployment = $pluginDeploymentName
        }
    }

    if ($matchedNumber -eq 0) {
        Write-Error "Could not find Azure Function resource name for '$pluginName'."
        Write-Error "Make sure the deployed Azure Function resource name matches the plugin zip package name."
        exit 1
    }
    
    if ($matchedNumber -gt 1) {
        Write-Error "Found multiple Azure Function resource names for '$pluginName'."
        Write-Error "Make sure the deployed Azure Function resource name matches the plugin zip package name."
        exit 1
    }
    
    Write-Host "Matched Azure Function resource name '$matchedDeployment' for '$pluginName'"
    $pluginDeploymentMatches.Add($pluginPackage, $matchedDeployment)
}
Write-Host ""

Write-Host "-------Deploying plugins to Azure Functions-------"
foreach ($pluginDeploymentMatches in $pluginDeploymentMatches.GetEnumerator()) {
    $pluginPackage = $pluginDeploymentMatches.Key
    $pluginDeploymentName = $pluginDeploymentMatches.Value

    Write-Host "Deploying '$pluginPackage' to Azure Function '$pluginDeploymentName'..."
    az functionapp deployment source config-zip `
        --resource-group $ResourceGroupName `
        --name $pluginDeploymentName `
        --src $pluginPackage
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}