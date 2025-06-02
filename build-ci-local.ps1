#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Local CI Build Script - Mirrors GitHub Actions CI Pipeline
.DESCRIPTION
    This script runs the same steps as the GitHub Actions CI pipeline locally
    to catch issues before pushing to the repository.
.EXAMPLE
    .\build-ci-local.ps1
    .\build-ci-local.ps1 -SkipTests
    .\build-ci-local.ps1 -SkipFormat
    .\build-ci-local.ps1 -Configuration Debug
#>

param(
    [Parameter(HelpMessage="Skip running tests to speed up the build")]
    [switch]$SkipTests,
    
    [Parameter(HelpMessage="Build configuration (Debug/Release)")]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
      [Parameter(HelpMessage="Skip cleaning before build")]
    [switch]$SkipClean,
    
    [Parameter(HelpMessage="Skip code formatting to speed up the build")]
    [switch]$SkipFormat,
    
    [Parameter(HelpMessage="Show verbose output")]
    [switch]$VerboseOutput
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory and solution path
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionPath = Join-Path $ScriptDir "src\c-sharp\NotebookAutomation.sln"
$TestProjectPath = Join-Path $ScriptDir "src\c-sharp\NotebookAutomation.Core.Tests"

# Change to script directory to ensure relative paths work
Set-Location $ScriptDir

# Colors for output
$Green = [System.ConsoleColor]::Green
$Red = [System.ConsoleColor]::Red
$Yellow = [System.ConsoleColor]::Yellow
$Cyan = [System.ConsoleColor]::Cyan

function Write-Step {
    param([string]$Message)
    Write-Host "`n=== $Message ===" -ForegroundColor $Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor $Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $Red
}

try {    Write-Host "🚀 Starting Local CI Build Pipeline" -ForegroundColor $Cyan
    Write-Host "Configuration: $Configuration" -ForegroundColor $Yellow
    Write-Host "Solution: $SolutionPath" -ForegroundColor $Yellow
    Write-Host "Working Directory: $(Get-Location)" -ForegroundColor $Yellow
    Write-Host "Solution Exists: $(Test-Path $SolutionPath)" -ForegroundColor $Yellow
    
    # Step 1: Clean (if not skipped)
    if (-not $SkipClean) {
        Write-Step "Step 1: Clean Solution"
        dotnet clean $SolutionPath --configuration $Configuration
        if ($LASTEXITCODE -ne 0) {
            throw "Clean failed with exit code $LASTEXITCODE"
        }
        Write-Success "Clean completed successfully"
    } else {
        Write-Warning "Skipping clean step"
    }
    
    # Step 2: Restore Dependencies (mirrors CI)
    Write-Step "Step 2: Restore Dependencies"
    dotnet restore $SolutionPath
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed with exit code $LASTEXITCODE"
    }    Write-Success "Dependencies restored successfully"
    
    # Step 3: Code Formatting (mirrors CI preparation)
    if (-not $SkipFormat) {
        Write-Step "Step 3: Code Formatting"
        Write-Host "Applying code formatting standards..." -ForegroundColor $Yellow
        
        if ($VerboseOutput) {
            dotnet format $SolutionPath --verbosity normal
        } else {
            dotnet format $SolutionPath
        }
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Code formatting encountered issues but continuing..."
            Write-Host "You may want to review the changes and commit them." -ForegroundColor $Yellow
        } else {
            Write-Success "Code formatting completed successfully"
        }
    } else {
        Write-Warning "Skipping code formatting"
    }
      # Step 4: Build Solution (mirrors CI)
    Write-Step "Step 4: Build Solution"
    Write-Host "Build command: dotnet build `"$SolutionPath`" --configuration $Configuration --no-restore" -ForegroundColor $Yellow
    
    if ($VerboseOutput) {
        dotnet build "$SolutionPath" --configuration $Configuration --no-restore --verbosity normal
    } else {
        dotnet build "$SolutionPath" --configuration $Configuration --no-restore
    }
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Success "Build completed successfully"    # Step 5: Run Tests (mirrors CI)
    if (-not $SkipTests) {
        Write-Step "Step 5: Run Tests with Coverage"
        $env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
        
        if ($VerboseOutput) {
            dotnet test "$TestProjectPath" --configuration $Configuration --no-build --logger "trx;LogFileName=test-results.trx" --collect:"XPlat Code Coverage" --verbosity normal
        } else {
            dotnet test "$TestProjectPath" --configuration $Configuration --no-build --logger "trx;LogFileName=test-results.trx" --collect:"XPlat Code Coverage"
        }
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed with exit code $LASTEXITCODE"
        }
        Write-Success "All tests passed"
    } else {
        Write-Warning "Skipping tests"
    }    # Step 6: Test Publish Operations (mirrors CI publish steps)
    Write-Step "Step 6: Test Publish Operations"
    $cliProjectPath = Join-Path $ScriptDir "src\c-sharp\NotebookAutomation.Cli\NotebookAutomation.Cli.csproj"
    $tempPublishDir = Join-Path $ScriptDir "temp_publish_test"
    
    try {
        # Test win-x64 publish (mirrors CI)
        Write-Host "Testing win-x64 publish..." -ForegroundColor $Yellow
        dotnet publish $cliProjectPath -c $Configuration -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true --output "$tempPublishDir\win-x64"
        if ($LASTEXITCODE -ne 0) {
            throw "win-x64 publish failed with exit code $LASTEXITCODE"
        }
        
        # Test win-arm64 publish (mirrors CI)
        Write-Host "Testing win-arm64 publish..." -ForegroundColor $Yellow
        dotnet publish $cliProjectPath -c $Configuration -r win-arm64 /p:PublishSingleFile=true /p:SelfContained=true --output "$tempPublishDir\win-arm64"
        if ($LASTEXITCODE -ne 0) {
            throw "win-arm64 publish failed with exit code $LASTEXITCODE"
        }
          Write-Success "Publish operations completed successfully"
    }
    finally {
        # Clean up temp publish directory
        if (Test-Path $tempPublishDir) {
            Remove-Item -Recurse -Force $tempPublishDir -ErrorAction SilentlyContinue
        }
    }
      # Step 7: Run Static Code Analysis (mirrors CI - this runs last)
    Write-Step "Step 7: Static Code Analysis"
    dotnet format $SolutionPath --verify-no-changes --severity error
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Code formatting issues detected!"
        Write-Host "Run 'dotnet format $SolutionPath' to fix formatting issues" -ForegroundColor $Yellow
        throw "Static code analysis failed with exit code $LASTEXITCODE"
    }
    Write-Success "Static code analysis passed"
    
    # Success Summary
    Write-Host "`n🎉 LOCAL CI BUILD PIPELINE COMPLETED SUCCESSFULLY! 🎉" -ForegroundColor $Green
    Write-Host "All steps that run in GitHub Actions CI have passed locally." -ForegroundColor $Green
    Write-Host "Your changes should pass CI when pushed to the repository." -ForegroundColor $Green
    
    # Display timing info
    $endTime = Get-Date
    Write-Host "`nBuild completed at: $($endTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor $Cyan
    
} catch {
    Write-Host "`n💥 LOCAL CI BUILD PIPELINE FAILED! 💥" -ForegroundColor $Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor $Red
    Write-Host "`nPlease fix the issues above before pushing to the repository." -ForegroundColor $Yellow
      # Display helpful commands
    Write-Host "`nHelpful Commands:" -ForegroundColor $Cyan
    Write-Host "  Fix formatting: dotnet format $SolutionPath" -ForegroundColor $Yellow
    Write-Host "  Run tests only: dotnet test $TestProjectPath --configuration $Configuration" -ForegroundColor $Yellow
    Write-Host "  Build only: dotnet build $SolutionPath --configuration $Configuration" -ForegroundColor $Yellow
    
    exit 1
}
