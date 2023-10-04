<#
.SYNOPSIS
Deploy Chat Copilot Azure resources
#>

param(
    [Parameter(Mandatory)]
    [string]
    # Name for the deployment
    $DeploymentName,

    [Parameter(Mandatory)]
    [string]
    # Subscription to which to make the deployment
    $Subscription,

    [Parameter(Mandatory)]
    [string]
    # Azure AD client ID for the Web API backend app registration
    $BackendClientId,

    [Parameter(Mandatory)]
    [string]
    # Azure AD tenant ID for authenticating users
    $TenantId,

    [Parameter(Mandatory)]
    [ValidateSet("AzureOpenAI", "OpenAI")]
    [string]
    # AI service to use
    $AIService,

    [string]
    # API key for existing Azure OpenAI resource or OpenAI account
    $AIApiKey,

    # Endpoint for existing Azure OpenAI resource
    [string]
    $AIEndpoint,

    [string]
    # Resource group to which to make the deployment
    $ResourceGroup,

    [string]
    # Region to which to make the deployment (ignored when deploying to an existing resource group)
    $Region = "southcentralus",

    [string]
    # Region to deploy to the static web app into. This must be a region that supports static web apps.
    $WebAppRegion = "westus2",

    [string]
    # SKU for the Azure App Service plan
    $WebAppServiceSku = "B1",

    [string]
    # Azure AD cloud instance for authenticating users
    $AzureAdInstance = "https://login.microsoftonline.com",

    [ValidateSet("Volatile", "AzureCognitiveSearch", "Qdrant", "Postgres")]
    [string]
    # What method to use to persist embeddings
    $MemoryStore = "AzureCognitiveSearch",

    [SecureString]
    # Password for the Postgres database
    $SqlAdminPassword,

    [switch]
    # Don't deploy Cosmos DB for chat storage - Use volatile memory instead
    $NoCosmosDb,

    [switch]
    # Don't deploy Speech Services to enable speech as chat input
    $NoSpeechServices,

    [switch]
    # Switches on verbose template deployment output
    $DebugDeployment,

    [switch]
    # Switches on whether to deploy release packages
    $NoDeployPackage
)

# if AIService is AzureOpenAI
if ($AIService -eq "AzureOpenAI") {
    # Both $AIEndpoint and $AIApiKey must be set
    if ((!$AIEndpoint -and $AIApiKey) -or ($AIEndpoint -and !$AIApiKey)) {
        Write-Error "When AIService is AzureOpenAI, when either AIEndpoint and AIApiKey are set then both must be set."
        exit 1
    }

    # If both $AIEndpoint and $AIApiKey are not set, set $DeployAzureOpenAI to true and inform the user. Otherwise set $DeployAzureOpenAI to false and inform the user.
    if (!$AIEndpoint -and !$AIApiKey) {
        $DeployAzureOpenAI = $true
        Write-Host "When AIService is AzureOpenAI and both AIEndpoint and AIApiKey are not set then a new Azure OpenAI resource will be created."
    }
    else {
        $DeployAzureOpenAI = $false
        Write-Host "When AIService is AzureOpenAI and both AIEndpoint and AIApiKey are set, use the existing Azure OpenAI resource."
    }
}

# if AIService is OpenAI then $AIApiKey is mandatory.
if ($AIService -eq "OpenAI" -and !$AIApiKey) {
    Write-Error "When AIService is OpenAI, AIApiKey must be set."
    exit 1
}

if ($MemoryStore -eq "Postgres" -and !$SqlAdminPassword) {
    Write-Host "When MemoryStore is Postgres, SqlAdminPassword must be set"
    exit 1
}

$jsonConfig = "
{
    `\`"webAppServiceSku`\`": { `\`"value`\`": `\`"$WebAppServiceSku`\`" },
    `\`"webappLocation`\`": { `\`"value`\`": `\`"$WebAppRegion`\`" },
    `\`"aiService`\`": { `\`"value`\`": `\`"$AIService`\`" },
    `\`"aiApiKey`\`": { `\`"value`\`": `\`"$AIApiKey`\`" },
    `\`"aiEndpoint`\`": { `\`"value`\`": `\`"$AIEndpoint`\`" },
    `\`"deployWebApiPackage`\`": { `\`"value`\`": $(If (!($NoDeployPackage)) {"true"} Else {"false"}) },
    `\`"deployMemoryPipelinePackage`\`": { `\`"value`\`": $(If (!($NoDeployPackage)) {"true"} Else {"false"}) },
    `\`"deployWebSearcherPackage`\`": { `\`"value`\`": $(If (!($NoDeployPackage)) {"true"} Else {"false"}) },
    `\`"azureAdInstance`\`": { `\`"value`\`": `\`"$AzureAdInstance`\`" },
    `\`"azureAdTenantId`\`": { `\`"value`\`": `\`"$TenantId`\`" },
    `\`"webApiClientId`\`": { `\`"value`\`": `\`"$BackendClientId`\`"},
    `\`"deployNewAzureOpenAI`\`": { `\`"value`\`": $(If ($DeployAzureOpenAI) {"true"} Else {"false"}) },
    `\`"memoryStore`\`": { `\`"value`\`": `\`"$MemoryStore`\`" },
    `\`"deployCosmosDB`\`": { `\`"value`\`": $(If (!($NoCosmosDb)) {"true"} Else {"false"}) },
    `\`"deploySpeechServices`\`": { `\`"value`\`": $(If (!($NoSpeechServices)) {"true"} Else {"false"}) },
    `\`"sqlAdminPassword`\`": { `\`"value`\`": `\`"$(If ($SqlAdminPassword) {ConvertFrom-SecureString $SqlAdminPassword -AsPlainText} Else {$null})`\`" }
}
"

$jsonConfig = $jsonConfig -replace '\s', ''

$ErrorActionPreference = "Stop"

$templateFile = "$($PSScriptRoot)/main.bicep"

if (!$ResourceGroup) {
    $ResourceGroup = "rg-" + $DeploymentName
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

Write-Host "Ensuring resource group '$ResourceGroup' exists..."
az group create --location $Region --name $ResourceGroup --tags Creator=$env:UserName
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Validating template file..."
az deployment group validate --name $DeploymentName --resource-group $ResourceGroup --template-file $templateFile --parameters $jsonConfig
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Deploying Azure resources ($DeploymentName)..."
if ($DebugDeployment) {
    az deployment group create --name $DeploymentName --resource-group $ResourceGroup --template-file $templateFile --debug --parameters $jsonConfig
}
else {
    az deployment group create --name $DeploymentName --resource-group $ResourceGroup --template-file $templateFile --parameters $jsonConfig
}
