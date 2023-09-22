<#
.SYNOPSIS
Build Copilot Chat's frontend (aka webapp)
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

    [Parameter(Mandatory)]
    [string]
    # Client application id for the frontend web app
    $FrontendClientId,

    [string]
    # Version to display in UI.
    $Version = "0.0.0",

    [string]
    # Additional information given in version info. (Ex: commit SHA)
    $VersionInfo = ""
)

Write-Host "Setting up Azure credentials..."
az account show --output none
if ($LASTEXITCODE -ne 0) {
    Write-Host "Log into your Azure account"
    az login --output none
}

Write-Host "Setting subscription to '$Subscription'..."
az account set -s $Subscription
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Getting deployment outputs..."
$deployment = $(az deployment group show --name $DeploymentName --resource-group $ResourceGroupName --output json | ConvertFrom-Json)
$webapiUrl = $deployment.properties.outputs.webapiUrl.value
$webapiName = $deployment.properties.outputs.webapiName.value
Write-Host "webapiName: $webapiName"
Write-Host "webapiUrl: $webapiUrl"

$webapiSettings = $(az webapp config appsettings list --name $webapiName --resource-group $ResourceGroupName | ConvertFrom-JSON)
$webapiClientId = ($webapiSettings | Where-Object -Property name -EQ -Value Authentication:AzureAd:ClientId).value
$webapiTenantId = ($webapiSettings | Where-Object -Property name -EQ -Value Authentication:AzureAd:TenantId).value
$webapiInstance = ($webapiSettings | Where-Object -Property name -EQ -Value Authentication:AzureAd:Instance).value
$webapiScope = ($webapiSettings | Where-Object -Property name -EQ -Value Authentication:AzureAd:Scopes).value

# Set ASCII as default encoding for Out-File
$PSDefaultParameterValues['Out-File:Encoding'] = 'ascii'

$envFilePath = "$PSScriptRoot/../../webapp/.env"
Write-Host "Writing environment variables to '$envFilePath'..."
"REACT_APP_BACKEND_URI=https://$webapiUrl/" | Out-File -FilePath $envFilePath
"REACT_APP_AUTH_TYPE=AzureAd" | Out-File -FilePath $envFilePath -Append
"REACT_APP_AAD_AUTHORITY=$($webapiInstance.Trim("/"))/$webapiTenantId" | Out-File -FilePath $envFilePath -Append
"REACT_APP_AAD_CLIENT_ID=$FrontendClientId" | Out-File -FilePath $envFilePath -Append
"REACT_APP_AAD_API_SCOPE=api://$webapiClientId/$webapiScope" | Out-File -FilePath $envFilePath -Append
"REACT_APP_SK_VERSION=$Version" | Out-File -FilePath $envFilePath -Append
"REACT_APP_SK_BUILD_INFO=$VersionInfo" | Out-File -FilePath $envFilePath -Append

Push-Location -Path "$PSScriptRoot/../../webapp"

Write-Host "Installing yarn dependencies..."
yarn install
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Building webapp..."
yarn build
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Pop-Location

$origin = "https://$webapiUrl"
Write-Host "Ensuring '$origin' is included in AAD app registration's redirect URIs..."
$objectId = (az ad app show --id $FrontendClientId | ConvertFrom-Json).id
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

Write-Host "To verify your deployment, go to 'https://$webapiUrl' in your browser."
