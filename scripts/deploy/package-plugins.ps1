<#
.SYNOPSIS
Package CopilotChat's plugins for deployment to Azure
#>

param(
    [string]
    # Build configuration to publish.
    $BuildConfiguration = "Release",
    
    [string]
    # .NET framework to publish.
    $DotNetFramework = "net6.0",
    
    [string]
    # Output directory for published assets.
    $OutputDirectory = "$PSScriptRoot",

    [string]
    # Version to give to assemblies and files.
    $Version = "1.0.0",

    [string]
    # Additional information given in version info.
    $InformationalVersion = ""
)

Write-Host "BuildConfiguration: $BuildConfiguration"
Write-Host "DotNetFramework: $DotNetFramework"
Write-Host "TargetRuntime: $TargetRuntime"
Write-Host "OutputDirectory: $OutputDirectory"

$publishOutputDirectory = "$OutputDirectory/publish"
$publishedZipDirectory = "$OutputDirectory/out/plugins"
if (!(Test-Path $publishedZipDirectory)) {
    New-Item -ItemType Directory -Force -Path $publishedZipDirectory | Out-Null
}
if (!(Test-Path $publishOutputDirectory)) {
    New-Item -ItemType Directory -Force -Path $publishOutputDirectory | Out-Null
}

$pluginProjectFiles = Get-ChildItem -Path "$PSScriptRoot/../../plugins" -Recurse -Filter "*.csproj" -Exclude "PluginShared.csproj"
foreach ($pluginProjectFile in $pluginProjectFiles) {
    $pluginName = $pluginProjectFile.Name.Replace(".csproj", "").ToLowerInvariant()
    $publishedZipFilePath = "$publishedZipDirectory/$pluginName.zip"

    Write-Host "Packaging $pluginName from $pluginProjectFile"

    dotnet publish $pluginProjectFile `
        --configuration $BuildConfiguration `
        --framework $DotNetFramework `
        --output "$publishOutputDirectory" `
        /p:AssemblyVersion=$Version `
        /p:FileVersion=$Version `
        /p:InformationalVersion=$InformationalVersion

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    Write-Host "Compressing to $publishedZipFilePath"
    Compress-Archive -Path $publishOutputDirectory\* -DestinationPath $publishedZipFilePath -Force

    Write-Host "Published $pluginName package to '$publishedZipFilePath'"
}