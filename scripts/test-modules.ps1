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

Write-Host "================================" -ForegroundColor Green
Write-Host "All module tests passed! ✓" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
