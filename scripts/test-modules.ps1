#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test script for PowerShell modules.

.DESCRIPTION
    This script tests the newly created PowerShell modules to ensure they
    work correctly and can be imported without errors.
#>

$ErrorActionPreference = "Stop"

Write-Host "Testing PowerShell Modules" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ModulesDir = Join-Path $ScriptDir "modules"

# Test 1: Import Logging module
Write-Host "Test 1: Importing Core/Logging.psm1..." -ForegroundColor Yellow
try {
    Import-Module (Join-Path $ModulesDir "Core\Logging.psm1") -Force
    Write-Host "✓ Logging module imported successfully" -ForegroundColor Green
    
    # Test logging functions
    Write-Success "This is a success message"
    Write-Warning "This is a warning message"
    Write-Error "This is an error message"
    Write-Step "This is a step header"
    Write-ColoredOutput "This is colored output" -Color Magenta
    
    Write-Host "✓ All logging functions work correctly" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to import or test Logging module: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 2: Import Platform module
Write-Host "Test 2: Importing Core/Platform.psm1..." -ForegroundColor Yellow
try {
    Import-Module (Join-Path $ModulesDir "Core\Platform.psm1") -Force
    Write-Host "✓ Platform module imported successfully" -ForegroundColor Green
    
    # Test platform detection
    $platform = Get-PlatformName
    Write-Host "  Current platform: $platform" -ForegroundColor Cyan
    Write-Host "  IsWindows: $IsWindows" -ForegroundColor Gray
    Write-Host "  IsLinux: $IsLinux" -ForegroundColor Gray
    Write-Host "  IsMacOS: $IsMacOS" -ForegroundColor Gray
    
    # Test path joining
    $testPath = Join-CrossPlatformPath @("src", "c-sharp", "test.txt")
    Write-Host "  Test path: $testPath" -ForegroundColor Gray
    
    Write-Host "✓ All platform functions work correctly" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to import or test Platform module: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 3: Import Prerequisites module
Write-Host "Test 3: Importing Core/Prerequisites.psm1..." -ForegroundColor Yellow
try {
    Import-Module (Join-Path $ModulesDir "Core\Prerequisites.psm1") -Force
    Write-Host "✓ Prerequisites module imported successfully" -ForegroundColor Green
    
    # Test prerequisite checks (non-throwing)
    Write-Host "  Checking Git repository..." -ForegroundColor Cyan
    $isGitRepo = Test-GitRepository
    
    Write-Host "  Checking .NET SDK..." -ForegroundColor Cyan
    $hasDotNet = Test-DotNetSDK
    
    Write-Host "  Checking Node.js..." -ForegroundColor Cyan
    $hasNode = Test-NodeJS
    
    Write-Host "  Checking GitHub CLI..." -ForegroundColor Cyan
    $hasGH = Test-GitHubCLI
    
    Write-Host "✓ All prerequisite checks completed" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to import or test Prerequisites module: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 4: Import Build/DotNetBuild module
Write-Host "Test 4: Importing Build/DotNetBuild.psm1..." -ForegroundColor Yellow
try {
    Import-Module (Join-Path $ModulesDir "Build\DotNetBuild.psm1") -Force
    Write-Host "✓ DotNetBuild module imported successfully" -ForegroundColor Green
    
    # Test function availability
    Get-Command Invoke-DotNetRestore -ErrorAction Stop | Out-Null
    Get-Command Invoke-DotNetBuild -ErrorAction Stop | Out-Null
    Get-Command Invoke-DotNetPublishWithRetry -ErrorAction Stop | Out-Null
    
    Write-Host "✓ All DotNetBuild functions available" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to import or test DotNetBuild module: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 5: Import Build/PluginBuild module
Write-Host "Test 5: Importing Build/PluginBuild.psm1..." -ForegroundColor Yellow
try {
    Import-Module (Join-Path $ModulesDir "Build\PluginBuild.psm1") -Force
    Write-Host "✓ PluginBuild module imported successfully" -ForegroundColor Green
    
    # Test function availability
    Get-Command Invoke-PluginNpmInstall -ErrorAction Stop | Out-Null
    Get-Command Invoke-PluginBuild -ErrorAction Stop | Out-Null
    Get-Command Invoke-PluginInstallAndBuild -ErrorAction Stop | Out-Null
    
    Write-Host "✓ All PluginBuild functions available" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to import or test PluginBuild module: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 6: Import GitHub/CLI module
Write-Host "Test 6: Importing GitHub/CLI.psm1..." -ForegroundColor Yellow
try {
    Import-Module (Join-Path $ModulesDir "GitHub\CLI.psm1") -Force
    Write-Host "✓ GitHub CLI module imported successfully" -ForegroundColor Green
    
    # Test function availability
    Get-Command Invoke-GhRunList -ErrorAction Stop | Out-Null
    Get-Command Invoke-GhReleaseCreate -ErrorAction Stop | Out-Null
    Get-Command Wait-GitHubActionsComplete -ErrorAction Stop | Out-Null
    
    Write-Host "✓ All GitHub CLI functions available" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to import or test GitHub CLI module: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 7: Import Version/Management module
Write-Host "Test 7: Importing Version/Management.psm1..." -ForegroundColor Yellow
try {
    Import-Module (Join-Path $ModulesDir "Version\Management.psm1") -Force
    Write-Host "✓ Version Management module imported successfully" -ForegroundColor Green
    
    # Test function availability
    Get-Command Get-VersionData -ErrorAction Stop | Out-Null
    Get-Command Sync-PluginVersion -ErrorAction Stop | Out-Null
    Get-Command New-GitVersionTag -ErrorAction Stop | Out-Null
    Get-Command Test-VersionFormat -ErrorAction Stop | Out-Null
    
    # Test version format validation
    $validStable = Test-VersionFormat -Version "1.0.0"
    $validBeta = Test-VersionFormat -Version "1.0.0-beta.1" -AllowPreRelease
    $invalid = Test-VersionFormat -Version "invalid"
    
    if ($validStable -and $validBeta -and -not $invalid) {
        Write-Host "✓ Version format validation works correctly" -ForegroundColor Green
    }
    else {
        Write-Host "✗ Version format validation failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✓ All Version Management functions available" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to import or test Version Management module: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

Write-Host "================================" -ForegroundColor Green
Write-Host "All module tests passed! ✓" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
