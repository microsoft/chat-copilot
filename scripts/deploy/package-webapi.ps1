<#
.SYNOPSIS
Package Chat Copilot application for deployment to Azure
#>

param(
    [string]
    # Build configuration to publish.
    $BuildConfiguration = "Release",
    
    [string]
    # .NET framework to publish.
    $DotNetFramework = "net6.0",
    
    [string]
    # Target runtime to publish.
    $TargetRuntime = "win-x64",
    
    [string]
    # Output directory for published assets.
    $OutputDirectory = "$PSScriptRoot",

    [string]
    # Version to give to assemblies and files.
    $Version = "0.0.0",

    [string]
    # Additional information given in version info.
    $InformationalVersion = "",
    
    [bool]
    # Whether to build frontend files (true by default)
    $BuildFrontendFiles = $true
)

Write-Host "Building backend executables..."

Write-Host "BuildConfiguration: $BuildConfiguration"
Write-Host "DotNetFramework: $DotNetFramework"
Write-Host "TargetRuntime: $TargetRuntime"
Write-Host "OutputDirectory: $OutputDirectory"

$publishOutputDirectory = "$OutputDirectory/publish"
$publishedZipDirectory = "$OutputDirectory/out"
$publishedZipFilePath = "$publishedZipDirectory/webapi.zip"
if (!(Test-Path $publishedZipDirectory)) {
    New-Item -ItemType Directory -Force -Path $publishedZipDirectory | Out-Null
}
if (!(Test-Path $publishOutputDirectory)) {
    New-Item -ItemType Directory -Force -Path $publishOutputDirectory | Out-Null
}

Write-Host "Build configuration: $BuildConfiguration"
dotnet publish "$PSScriptRoot/../../webapi/CopilotChatWebApi.csproj" --configuration $BuildConfiguration --framework $DotNetFramework --runtime $TargetRuntime --self-contained --output "$publishOutputDirectory" /p:AssemblyVersion=$Version /p:FileVersion=$Version /p:InformationalVersion=$InformationalVersion
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if ($BuildFrontendFiles) {
    Write-Host "Building static frontend files..."

    # Set ASCII as default encoding for Out-File
    $PSDefaultParameterValues['Out-File:Encoding'] = 'ascii'

    $envFilePath = "$PSScriptRoot/../../webapp/.env"

    Write-Host "Writing environment variables to '$envFilePath'..."
    "REACT_APP_BACKEND_URI=<-=TOKEN=->Frontend:BackendUri</-=TOKEN=->" | Out-File -FilePath $envFilePath
    "REACT_APP_AUTH_TYPE=AzureAd" | Out-File -FilePath $envFilePath -Append
    "REACT_APP_AAD_AUTHORITY=<-=TOKEN=->Authentication:AzureAd:Instance</-=TOKEN=->/<-=TOKEN=->Authentication:AzureAd:TenantId</-=TOKEN=->" | Out-File -FilePath $envFilePath -Append
    "REACT_APP_AAD_CLIENT_ID=<-=TOKEN=->Frontend:AadClientId</-=TOKEN=->" | Out-File -FilePath $envFilePath -Append
    "REACT_APP_AAD_API_SCOPE=api://<-=TOKEN=->Authentication:AzureAd:ClientId</-=TOKEN=->/<-=TOKEN=->Authentication:AzureAd:Scopes</-=TOKEN=->" | Out-File -FilePath $envFilePath -Append
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

    Write-Host "Copying frontend files to package"
    Copy-Item -Path "$PSScriptRoot/../../webapp/build" -Destination "$publishOutputDirectory\wwwroot" -Recurse -Force
}

Write-Host "Compressing package to $publishedZipFilePath"
Compress-Archive -Path $publishOutputDirectory\* -DestinationPath $publishedZipFilePath -Force

Write-Host "Published package to '$publishedZipFilePath'"