#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Synchronizes versions between CLI and Obsidian plugin components.

.DESCRIPTION
    This script ensures version consistency across the CLI and plugin by:
    1. Reading the current plugin version from manifest.json
    2. Updating the CLI build configuration to use the same version
    3. Optionally building both components with the synchronized version

.PARAMETER BuildAfterSync
    Whether to build both components after synchronizing versions

.PARAMETER PluginVersion
    Specify a specific version instead of reading from manifest.json

.EXAMPLE
    .\scripts\sync-versions.ps1
    
.EXAMPLE
    .\scripts\sync-versions.ps1 -BuildAfterSync
    
.EXAMPLE
    .\scripts\sync-versions.ps1 -PluginVersion "0.1.0-beta.5" -BuildAfterSync
#>

param(
    [switch]$BuildAfterSync,
    [string]$PluginVersion = ""
)

$ErrorActionPreference = "Stop"

Write-Host "🔄 Synchronizing CLI and Plugin Versions" -ForegroundColor Green

# Define paths
$RepoRoot = Get-Location
$PluginManifestPath = Join-Path $RepoRoot "src\obsidian-plugin\manifest.json"
$DirectoryBuildPropsPath = Join-Path $RepoRoot "src\c-sharp\Directory.Build.props"
$GitVersionPath = Join-Path $RepoRoot "GitVersion.yml"

# Step 1: Get the target version
if ($PluginVersion) {
    $targetVersion = $PluginVersion
    Write-Host "📝 Using specified version: $targetVersion"
}
else {
    if (-not (Test-Path $PluginManifestPath)) {
        throw "Plugin manifest not found: $PluginManifestPath"
    }
    
    $manifest = Get-Content $PluginManifestPath | ConvertFrom-Json
    $targetVersion = $manifest.version
    Write-Host "📝 Using plugin version: $targetVersion"
}

# Step 2: Update GitVersion.yml to use the target version
Write-Host "🔧 Updating GitVersion.yml"
$gitVersionContent = @"
next-version: $targetVersion
mode: ContinuousDelivery
"@

Set-Content -Path $GitVersionPath -Value $gitVersionContent
Write-Host "✅ GitVersion.yml updated to: $targetVersion"

# Step 3: Update Directory.Build.props to use manual versioning
Write-Host "🔧 Updating Directory.Build.props for manual versioning"
$directoryBuildContent = Get-Content $DirectoryBuildPropsPath -Raw

# Enable manual versioning and set the version
$updatedContent = $directoryBuildContent -replace 'GitVersion_UseGitVersion>false', 'GitVersion_UseGitVersion>false'
$updatedContent = $updatedContent -replace 'GitVersion_SemVer>.*?</GitVersion_SemVer>', "GitVersion_SemVer>$targetVersion</GitVersion_SemVer>"

# If the GitVersion_SemVer doesn't exist, add it
if ($updatedContent -notmatch 'GitVersion_SemVer>') {
    $updatedContent = $updatedContent -replace '(GitVersion_SemVer>0\.1\.0-fallback)', "$targetVersion"
}

Set-Content -Path $DirectoryBuildPropsPath -Value $updatedContent
Write-Host "✅ Directory.Build.props updated"

# Step 4: Show current versions
Write-Host ""
Write-Host "📋 Version Status:" -ForegroundColor Yellow
Write-Host "  Target Version: $targetVersion" -ForegroundColor White
Write-Host "  CLI GitVersion: $targetVersion" -ForegroundColor White
Write-Host "  Plugin Version: $targetVersion" -ForegroundColor White

# Step 5: Build if requested
if ($BuildAfterSync) {
    Write-Host ""
    Write-Host "🔨 Building components with synchronized version..." -ForegroundColor Green
    
    # Build CLI
    Write-Host "Building CLI..."
    dotnet build src/c-sharp/NotebookAutomation.sln --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "CLI build failed"
    }
    
    # Build Plugin
    Write-Host "Building Plugin..."
    Push-Location "src/obsidian-plugin"
    try {
        npm run build
        if ($LASTEXITCODE -ne 0) {
            throw "Plugin build failed"
        }
    }
    finally {
        Pop-Location
    }
    
    # Verify versions
    Write-Host ""
    Write-Host "✅ Build completed. Verifying versions:" -ForegroundColor Green
    
    # Check CLI version
    $cliVersion = (dotnet run --project src/c-sharp/NotebookAutomation.Cli/NotebookAutomation.Cli.csproj -- --version 2>&1 | Select-String "version").ToString()
    Write-Host "  CLI: $cliVersion" -ForegroundColor White
    
    # Check plugin version
    $pluginManifest = Get-Content $PluginManifestPath | ConvertFrom-Json
    Write-Host "  Plugin: $($pluginManifest.version)" -ForegroundColor White
}

Write-Host ""
Write-Host "🎉 Version synchronization completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Test both CLI and plugin with the synchronized version"
Write-Host "  2. Commit the version changes when ready"
Write-Host "  3. Use manage-plugin-version.ps1 for releases"
