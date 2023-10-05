<#
.SYNOPSIS
Configure user secrets, appsettings.Development.json, and webapp/.env for Chat Copilot.

.PARAMETER AIService
The service type used: OpenAI or Azure OpenAI.

.PARAMETER APIKey
The API key for the AI service.

.PARAMETER Endpoint
Set when using Azure OpenAI.

.PARAMETER CompletionModel
The chat completion model to use (e.g., gpt-3.5-turbo or gpt-4).

.PARAMETER EmbeddingModel
The embedding model to use (e.g., text-embedding-ada-002).

.PARAMETER PlannerModel
The chat completion model to use for planning (e.g., gpt-3.5-turbo or gpt-4).

.PARAMETER FrontendClientId
The client (application) ID associated with your frontend's AAD app registration.

.PARAMETER BackendClientId
The client (application) ID associated with your backend's AAD app registration.

.PARAMETER TenantId
The tenant (directory) associated with your AAD app registrations.
See https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-client-application-configuration#authority.

.PARAMETER Instance
The Azure cloud instance used for authenticating users. Defaults to https://login.microsoftonline.com.
See https://learn.microsoft.com/en-us/azure/active-directory/develop/authentication-national-cloud#azure-ad-authentication-endpoints.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$AIService,
    
    [Parameter(Mandatory = $true)]
    [string]$APIKey,

    [Parameter(Mandatory = $false)]
    [string]$Endpoint,
    
    [Parameter(Mandatory = $false)]
    [string]$CompletionModel,

    [Parameter(Mandatory = $false)]
    [string]$EmbeddingModel, 

    [Parameter(Mandatory = $false)]
    [string]$PlannerModel,

    [Parameter(Mandatory = $false)]
    [string] $FrontendClientId,

    [Parameter(Mandatory = $false)]
    [string] $BackendClientId,

    [Parameter(Mandatory = $false)]
    [string] $TenantId,

    [Parameter(Mandatory = $false)]
    [string] $Instance
)

# Get defaults and constants
$varScriptFilePath = Join-Path "$PSScriptRoot" 'Variables.ps1'
. $varScriptFilePath

# Set remaining values from Variables.ps1
if ($AIService -eq $varOpenAI) {
    if (!$CompletionModel) {
        $CompletionModel = $varCompletionModelOpenAI
    }
    if (!$PlannerModel) {
        $PlannerModel = $varPlannerModelOpenAI
    }

    # TO DO: Validate model values if set by command line.
}
elseif ($AIService -eq $varAzureOpenAI) {
    if (!$CompletionModel) {
        $CompletionModel = $varCompletionModelAzureOpenAI
    }
    if (!$PlannerModel) {
        $PlannerModel = $varPlannerModelAzureOpenAI
    }
   
    # TO DO: Validate model values if set by command line.

    if (!$Endpoint) {
        Write-Error "Please specify an endpoint for -Endpoint when using AzureOpenAI."
        exit(1)
    }
}
else {
    Write-Error "Please specify an AI service (AzureOpenAI or OpenAI) for -AIService."
    exit(1)
}

if (!$EmbeddingModel) {
    $EmbeddingModel = $varEmbeddingModel
    # TO DO: Validate model values if set by command line.
}

# Determine authentication type based on arguments
if ($FrontendClientId -and $BackendClientId -and $TenantId) {
    $authType = $varAzureAd
    if (!$Instance) {
        $Instance = $varInstance
    }
}
elseif (!$FrontendClientId -and !$BackendClientId -and !$TenantId) {
    $authType = $varNone
}
else {
    Write-Error "To use Azure AD authentication, please set -FrontendClientId, -BackendClientId, and -TenantId."
    exit(1)
}

Write-Host "#########################"
Write-Host "# Backend configuration #"
Write-Host "#########################"

# Install dev certificate
if ($IsLinux) {
    dotnet dev-certs https
    if ($LASTEXITCODE -ne 0) { exit(1) }
}
else {
    # Windows/MacOS
    dotnet dev-certs https --trust
    if ($LASTEXITCODE -ne 0) { exit(1) }
}

$webapiProjectPath = Join-Path "$PSScriptRoot" '../webapi'

Write-Host "Setting 'APIKey' user secret for $AIService..."
if ($AIService -eq $varOpenAI) {
    dotnet user-secrets set --project $webapiProjectPath SemanticMemory:Services:OpenAI:APIKey $ApiKey
    if ($LASTEXITCODE -ne 0) { exit(1) }
    $AIServiceOverrides = @{
        OpenAI = @{
            TextModel      = $CompletionModel;
            EmbeddingModel = $EmbeddingModel;
        }
    };
}
else {
    dotnet user-secrets set --project $webapiProjectPath SemanticMemory:Services:AzureOpenAIText:APIKey $ApiKey
    if ($LASTEXITCODE -ne 0) { exit(1) }
    dotnet user-secrets set --project $webapiProjectPath SemanticMemory:Services:AzureOpenAIEmbedding:APIKey $ApiKey
    if ($LASTEXITCODE -ne 0) { exit(1) }
    $AIServiceOverrides = @{
        AzureOpenAIText      = @{
            Endpoint   = $Endpoint;
            Deployment = $CompletionModel;
        };
        AzureOpenAIEmbedding = @{
            Endpoint   = $Endpoint;
            Deployment = $EmbeddingModel;
        }
    };
}

$appsettingsOverrides = @{
    Authentication = @{
        Type    = $authType;
        AzureAd = @{
            Instance = $Instance;
            TenantId = $TenantId;
            ClientId = $BackendClientId;
            Scopes   = $varScopes
        }
    };
    Planner = @{
        Model = $PlannerModel
    };
    SemanticMemory = @{
        TextGeneratorType = $AIService;
        DataIngestion     = @{
            EmbeddingGeneratorTypes = @($AIService)
        };
        Retrieval         = @{
            EmbeddingGeneratorType = $AIService
        };
        Services          = $AIServiceOverrides;
    };
    Frontend = @{
        AadClientId = $FrontendClientId
    };
}
$appSettingsJson = -join ("appsettings.", $varASPNetCore, ".json");
$appsettingsOverridesFilePath = Join-Path $webapiProjectPath $appSettingsJson

Write-Host "Setting up '$appSettingsJson' for $AIService..."
# Setting depth to 100 to avoid truncating the JSON
ConvertTo-Json $appsettingsOverrides -Depth 100 | Out-File -Encoding utf8 $appsettingsOverridesFilePath

Write-Host "($appsettingsOverridesFilePath)"
Write-Host "========"
Get-Content $appsettingsOverridesFilePath | Write-Host
Write-Host "========"

Write-Host ""
Write-Host "##########################"
Write-Host "# Frontend configuration #"
Write-Host "##########################"

$webappProjectPath = Join-Path "$PSScriptRoot" '../webapp'
$webappEnvFilePath = Join-Path "$webappProjectPath" '/.env'

Write-Host "Setting up '.env'..."
Set-Content -Path $webappEnvFilePath -Value "REACT_APP_BACKEND_URI=https://localhost:40443/"

Write-Host "($webappEnvFilePath)"
Write-Host "========"
Get-Content $webappEnvFilePath | Write-Host
Write-Host "========"

Write-Host "Done!"
