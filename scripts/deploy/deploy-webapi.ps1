<#
.SYNOPSIS
Deploy Chat Copilot application to Azure
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
    $PackageFilePath = "$PSScriptRoot/out/webapi.zip",

    [switch]
    # Switch to add our URIs in app registration's redirect URIs if missing
    $EnsureUriInAppRegistration,
    
    [switch]
    # Switch to add our URIs in CORS origins for our plugins
    $RegisterPluginCors
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
$deployment=$(az deployment group show --name $DeploymentName --resource-group $ResourceGroupName --output json | ConvertFrom-Json)
$webApiUrl = $deployment.properties.outputs.webapiUrl.value
$webApiName = $deployment.properties.outputs.webapiName.value
$pluginNames = $deployment.properties.outputs.pluginNames.value

if ($null -eq $webApiName) {
    Write-Error "Could not get Azure WebApp resource name from deployment output."
    exit 1
}

Write-Host "Azure WebApp name: $webApiName"

Write-Host "Configuring Azure WebApp to run from package..."
az webapp config appsettings set --resource-group $ResourceGroupName --name $webApiName --settings WEBSITE_RUN_FROM_PACKAGE="1" --output none
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# Set up deployment command as a string
$azWebAppCommand = "az webapp deployment source config-zip --resource-group $ResourceGroupName --name $webApiName --src $PackageFilePath"

# Check if DeploymentSlot parameter was passed
$origins = @("$webApiUrl")
if ($DeploymentSlot) {
    Write-Host "Checking if slot $DeploymentSlot exists for '$webApiName'..."
    $azWebAppCommand += " --slot $DeploymentSlot"
    $slotInfo = az webapp deployment slot list --resource-group $ResourceGroupName --name $webApiName | ConvertFrom-JSON
    $availableSlots = $slotInfo | Select-Object -Property Name
    $origins = $slotInfo | Select-Object -Property defaultHostName
    $slotExists = false

    foreach ($slot in $availableSlots) { 
        if ($slot.name -eq $DeploymentSlot) { 
            # Deployment slot was found we dont need to create it
            $slotExists = true
        } 
    }

    # Web App deployment slot does not exist, create it
    if (!$slotExists) {
        Write-Host "Deployment slot $DeploymentSlot does not exist, creating..."
        az webapp deployment slot create --slot $DeploymentSlot --resource-group $ResourceGroupName --name $webApiName --output none
        $origins = az webapp deployment slot list --resource-group $ResourceGroupName --name $webApiName | ConvertFrom-JSON | Select-Object -Property defaultHostName
    }
}

Write-Host "Deploying '$PackageFilePath' to Azure WebApp '$webApiName'..."

# Invoke the command string
Invoke-Expression $azWebAppCommand
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if ($EnsureUriInAppRegistration) {
    $webapiSettings = $(az webapp config appsettings list --name $webapiName --resource-group $ResourceGroupName | ConvertFrom-JSON)
    $frontendClientId = ($webapiSettings | Where-Object -Property name -EQ -Value Frontend:AadClientId).value
    $objectId = (az ad app show --id $frontendClientId | ConvertFrom-Json).id
    $redirectUris = (az rest --method GET --uri "https://graph.microsoft.com/v1.0/applications/$objectId" --headers 'Content-Type=application/json' | ConvertFrom-Json).spa.redirectUris
    $needToUpdateRegistration = $false

    foreach ($address in $origins) {
        $origin = "https://$address"
        Write-Host "Ensuring '$origin' is included in AAD app registration's redirect URIs..."

        if ($redirectUris -notcontains "$origin") {
            $redirectUris += "$origin"
            $needToUpdateRegistration = $true
        }
    }

    if ($needToUpdateRegistration) {
        $body = "{spa:{redirectUris:["
        foreach ($uri in $redirectUris) {
            $body += "'$uri',"
        }
        $body += "]}}"

        az rest `
            --method PATCH `
            --uri "https://graph.microsoft.com/v1.0/applications/$objectId" `
            --headers 'Content-Type=application/json' `
            --body $body
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
}

if ($RegisterPluginCors) {
    foreach ($pluginName in $pluginNames) {
        $allowedOrigins = $((az webapp cors show --name $pluginName --resource-group $ResourceGroupName --subscription $Subscription | ConvertFrom-Json).allowedOrigins)
        foreach ($address in $origins) {
            $origin = "https://$address"
            Write-Host "Ensuring '$origin' is included in CORS origins for plugin '$pluginName'..."
            if (-not $allowedOrigins -contains $origin) {
                az webapp cors add --name $pluginName --resource-group $ResourceGroupName --subscription $Subscription --allowed-origins $origin
            }
        }
    }
}

Write-Host "To verify your deployment, go to 'https://$webApiUrl/' in your browser."