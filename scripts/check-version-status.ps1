#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Shows the current version status across all components.

.DESCRIPTION
    This script displays the current version information for CLI and plugin
    components, helping identify version alignment issues.

.PARAMETER Detailed
    Show detailed version information including build metadata

.EXAMPLE
    .\scripts\check-version-status.ps1
    
.EXAMPLE
    .\scripts\check-version-status.ps1 -Detailed
#>

param(
    [switch]$Detailed
)

$ErrorActionPreference = "Stop"

# Define paths
$RepoRoot = Get-Location
$PluginRoot = Join-Path $RepoRoot "src\obsidian-plugin"
$CliRoot = Join-Path $RepoRoot "src\c-sharp"
$ManifestPath = Join-Path $PluginRoot "manifest.json"
$PackageJsonPath = Join-Path $PluginRoot "package.json"
$GitVersionPath = Join-Path $RepoRoot "GitVersion.yml"

Write-Host "📊 Version Status Report" -ForegroundColor Green
Write-Host "========================" -ForegroundColor Green
Write-Host ""

# Plugin versions
Write-Host "🔌 Plugin Component:" -ForegroundColor Blue
if (Test-Path $ManifestPath) {
    $manifest = Get-Content $ManifestPath | ConvertFrom-Json
    Write-Host "  manifest.json: $($manifest.version)" -ForegroundColor White
    
    if ($Detailed) {
        Write-Host "    minAppVersion: $($manifest.minAppVersion)" -ForegroundColor Gray
        Write-Host "    id: $($manifest.id)" -ForegroundColor Gray
    }
} else {
    Write-Host "  manifest.json: ❌ NOT FOUND" -ForegroundColor Red
}

if (Test-Path $PackageJsonPath) {
    $packageJson = Get-Content $PackageJsonPath | ConvertFrom-Json
    Write-Host "  package.json: $($packageJson.version)" -ForegroundColor White
    
    if ($Detailed) {
        Write-Host "    name: $($packageJson.name)" -ForegroundColor Gray
        Write-Host "    description: $($packageJson.description)" -ForegroundColor Gray
    }
} else {
    Write-Host "  package.json: ❌ NOT FOUND" -ForegroundColor Red
}

Write-Host ""

# CLI versions
Write-Host "💻 CLI Component:" -ForegroundColor Blue

# GitVersion configuration
if (Test-Path $GitVersionPath) {
    $gitVersionContent = Get-Content $GitVersionPath
    $nextVersion = $gitVersionContent | Select-String 'next-version' | ForEach-Object { $_.ToString().Split(':')[1].Trim() }
    Write-Host "  GitVersion.yml: $nextVersion" -ForegroundColor White
    
    if ($Detailed) {
        $mode = $gitVersionContent | Select-String 'mode' | ForEach-Object { $_.ToString().Split(':')[1].Trim() }
        Write-Host "    mode: $mode" -ForegroundColor Gray
    }
} else {
    Write-Host "  GitVersion.yml: ❌ NOT FOUND" -ForegroundColor Red
}

# Try to get actual CLI version by running it
try {
    Push-Location $CliRoot
    $cliVersionOutput = dotnet run --project NotebookAutomation.Cli/NotebookAutomation.Cli.csproj -- --version 2>&1
    $cliVersion = ($cliVersionOutput | Select-String "version").ToString()
    Write-Host "  CLI Runtime: $cliVersion" -ForegroundColor White
    
    if ($Detailed) {
        Write-Host "    Full output: $cliVersionOutput" -ForegroundColor Gray
    }
} catch {
    Write-Host "  CLI Runtime: ❌ CANNOT DETERMINE (not built?)" -ForegroundColor Red
    if ($Detailed) {
        Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Gray
    }
} finally {
    Pop-Location
}

Write-Host ""

# Version alignment check
Write-Host "🔍 Version Alignment Check:" -ForegroundColor Yellow

$versions = @()
if (Test-Path $ManifestPath) {
    $manifest = Get-Content $ManifestPath | ConvertFrom-Json
    $versions += $manifest.version
}
if (Test-Path $PackageJsonPath) {
    $packageJson = Get-Content $PackageJsonPath | ConvertFrom-Json
    $versions += $packageJson.version
}
if (Test-Path $GitVersionPath) {
    $gitVersionContent = Get-Content $GitVersionPath
    $nextVersion = $gitVersionContent | Select-String 'next-version' | ForEach-Object { $_.ToString().Split(':')[1].Trim() }
    $versions += $nextVersion
}

$uniqueVersions = $versions | Select-Object -Unique
if ($uniqueVersions.Count -eq 1) {
    Write-Host "  Status: ✅ ALL VERSIONS ALIGNED" -ForegroundColor Green
    Write-Host "  Common Version: $($uniqueVersions[0])" -ForegroundColor Green
} else {
    Write-Host "  Status: ❌ VERSION MISMATCH DETECTED" -ForegroundColor Red
    Write-Host "  Found versions: $($uniqueVersions -join ', ')" -ForegroundColor Red
    Write-Host ""
    Write-Host "  🔧 To fix alignment, run:" -ForegroundColor Yellow
    Write-Host "     .\scripts\sync-versions.ps1 -PluginVersion ""$($uniqueVersions[0])""" -ForegroundColor White
}

Write-Host ""

# Git information
Write-Host "📝 Git Status:" -ForegroundColor Blue
try {
    $currentBranch = git rev-parse --abbrev-ref HEAD
    $lastCommit = git log -1 --oneline
    $tags = git tag --points-at HEAD
    
    Write-Host "  Current Branch: $currentBranch" -ForegroundColor White
    Write-Host "  Last Commit: $lastCommit" -ForegroundColor White
    
    if ($tags) {
        Write-Host "  Tags at HEAD: $($tags -join ', ')" -ForegroundColor White
    } else {
        Write-Host "  Tags at HEAD: None" -ForegroundColor Gray
    }
    
    if ($Detailed) {
        $status = git status --porcelain
        if ($status) {
            Write-Host "  Working Directory: Modified files detected" -ForegroundColor Yellow
            $status | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
        } else {
            Write-Host "  Working Directory: Clean" -ForegroundColor Green
        }
    }
} catch {
    Write-Host "  Git Status: ❌ ERROR" -ForegroundColor Red
}

Write-Host ""
Write-Host "📋 Available Commands:" -ForegroundColor Yellow
Write-Host "  Sync versions:     .\scripts\sync-versions.ps1"
Write-Host "  Manage versions:   .\scripts\manage-versions.ps1"
Write-Host "  Check status:      .\scripts\check-version-status.ps1"
