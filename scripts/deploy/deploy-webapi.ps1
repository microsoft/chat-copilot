<#
.SYNOPSIS
Deploy CopilotChat's WebAPI to Azure
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
    # Name of the web app deployment slot
    $DeploymentSlot,

    [string]
    # CopilotChat WebApi package to deploy
    $PackageFilePath = "$PSScriptRoot/out/webapi.zip"
)

# Ensure $PackageFilePath exists
if (!(Test-Path $PackageFilePath)) {
    Write-Error "Package file '$PackageFilePath' does not exist. Have you run 'package-webapi.ps1' yet?"
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
$webappName = $(az deployment group show --name $DeploymentName --resource-group $ResourceGroupName --output json | ConvertFrom-Json).properties.outputs.webapiName.value
if ($null -eq $webAppName) {
    Write-Error "Could not get Azure WebApp resource name from deployment output."
    exit 1
}

Write-Host "Azure WebApp name: $webappName"

Write-Host "Configuring Azure WebApp to run from package..."
az webapp config appsettings set --resource-group $ResourceGroupName --name $webappName --settings WEBSITE_RUN_FROM_PACKAGE="1" | out-null
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# Check if DeploymentSlot parameter was passed
if ($DeploymentSlot) {

    Write-Host "Checking if slot $DeploymentSlot exists for '$webappName'..."
    $availableSlots = az webapp deployment slot list --resource-group $ResourceGroupName --name $webappName | ConvertFrom-JSON | Select-Object -Property Name
    $slotExists = false

    foreach ($slot in $availableSlots) { 
        if ($slot.name -eq $DeploymentSlot) { 
            # Deployment slot was found we dont need to create it
            $slotExists = true
        } 
    }

    # Web App DeploymentSlot does not exist, create it
    if (!$slotExists) {
        Write-Host "DeploymentSlot $DeploymentSlot does not exist, creating..."
        az webapp deployment slot create --slot $DeploymentSlot --resource-group $ResourceGroupName --name $webappName | Out-Null
    }
}

Write-Host "Deploying '$PackageFilePath' to Azure WebApp '$webappName'..."
# Setup the command as a string
$azWebAppCommand = "az webapp deployment source config-zip --resource-group $ResourceGroupName --name $webappName --src $PackageFilePath"

# Check if DeploymentSlot parameter was passed and append the argument to azwebappcommand
if ($DeploymentSlot) {
    $azWebAppCommand += " --slot $DeploymentSlot"
}

# Invoke the command string
Invoke-Expression $azWebAppCommand

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
