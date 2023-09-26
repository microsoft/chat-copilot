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
    # Copilot Chat application package to deploy
    $PackageFilePath = "$PSScriptRoot/out/webapi.zip",

    [switch]
    # Switch to add our URI in app registration's redirect URIs if missing
    $EnsureUriInAppRegistration
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

Write-Host "Deploying '$PackageFilePath' to Azure WebApp '$webApiName'..."
az webapp deployment source config-zip --resource-group $ResourceGroupName --name $webApiName --src $PackageFilePath
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if ($EnsureUriInAppRegistration) {
    $origin = "https://$webApiUrl"
    Write-Host "Ensuring '$origin' is included in AAD app registration's redirect URIs..."

    $webapiSettings = $(az webapp config appsettings list --name $webapiName --resource-group $ResourceGroupName | ConvertFrom-JSON)
    $frontendClientId = ($webapiSettings | Where-Object -Property name -EQ -Value Frontend:AadClientId).value

    $objectId = (az ad app show --id $frontendClientId | ConvertFrom-Json).id
    $redirectUris = (az rest --method GET --uri "https://graph.microsoft.com/v1.0/applications/$objectId" --headers 'Content-Type=application/json' | ConvertFrom-Json).spa.redirectUris
    if ($redirectUris -notcontains "$origin") {
        $redirectUris += "$origin"

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
    }
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Write-Host "To verify your deployment, go to 'https://$webApiUrl/' in your browser."