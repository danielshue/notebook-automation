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
    .\build-ci-local.ps1 -TestAllArch
#>

param(
    [Parameter(HelpMessage = "Skip running tests to speed up the build")]
    [switch]$SkipTests,

    [Parameter(HelpMessage = "Build configuration (Debug/Release)")]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter(HelpMessage = "Skip cleaning before build")]
    [switch]$SkipClean,

    [Parameter(HelpMessage = "Skip code formatting to speed up the build")]
    [switch]$SkipFormat,

    [Parameter(HelpMessage = "Test all architectures (x64, ARM64)")]
    [switch]$TestAllArch,

    [Parameter(HelpMessage = "Show verbose output")]
    [switch]$VerboseOutput
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory and solution path
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepositoryRoot = Split-Path -Parent $ScriptDir
$SolutionPath = Join-Path $RepositoryRoot "src\c-sharp\NotebookAutomation.sln"
$TestProjectPath = Join-Path $RepositoryRoot "src\c-sharp\NotebookAutomation.Core.Tests"

# Change to repository root to ensure relative paths work
Set-Location $RepositoryRoot

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

try {
    Write-Host "🚀 Starting Local CI Build Pipeline" -ForegroundColor $Cyan
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
    }
    else {
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
        }
        else {
            dotnet format $SolutionPath
        }
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Code formatting encountered issues but continuing..."
            Write-Host "You may want to review the changes and commit them." -ForegroundColor $Yellow
        }
        else {
            Write-Success "Code formatting completed successfully"
        }
    }
    else {
        Write-Warning "Skipping code formatting"
    }    # Step 4: Generate Version Information
    Write-Step "Step 4: Generate Version Information"
    $buildDate = Get-Date

    # Microsoft recommended format: major.minor.build.revision
    # where build is typically days since a base date and revision is seconds since midnight / 2

    # Calculate build number (days since Jan 1, 2024)
    $baseDate = Get-Date -Year 2024 -Month 1 -Day 1
    $daysSinceBase = [math]::Floor(($buildDate - $baseDate).TotalDays)

    # Calculate revision (seconds since midnight / 2)
    $midnight = $buildDate.Date
    $secondsSinceMidnight = [math]::Floor(($buildDate - $midnight).TotalSeconds)
    $revision = [math]::Floor($secondsSinceMidnight / 2)

    # Format version strings
    $major = 1
    $minor = 0

    # For NuGet package version (must be valid SemVer)
    $packageVersion = "$major.$minor.0"

    # For assembly version - must be a specific version for NuGet restore
    $assemblyVersion = "$major.$minor.0.0"

    # For file version - use MS recommended format
    $fileVersion = "$major.$minor.$daysSinceBase.$revision"

    # For logging purposes, we also keep a timestamp-based version for easier human reading
    $timestampVersion = $buildDate.ToString("yyMMdd.HHmm")

    Write-Host "Package Version: $packageVersion" -ForegroundColor $Yellow
    Write-Host "Assembly Version: $assemblyVersion" -ForegroundColor $Yellow
    Write-Host "File Version: $fileVersion" -ForegroundColor $Yellow
    Write-Host "Timestamp: $timestampVersion" -ForegroundColor $Yellow

    # Step 5: Build Solution (mirrors CI)
    Write-Step "Step 5: Build Solution"
    Write-Host "Build command: dotnet build `"$SolutionPath`" --configuration $Configuration --no-restore" -ForegroundColor $Yellow
    $buildParams = @(
        "build",
        "$SolutionPath",
        "--configuration", "$Configuration",
        "--no-restore",
        "/p:Version=$packageVersion",
        "/p:FileVersion=$fileVersion",
        "/p:AssemblyVersion=$assemblyVersion"
    )

    if ($VerboseOutput) {
        $buildParams += "--verbosity"
        $buildParams += "normal"
    }

    & dotnet $buildParams

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Success "Build completed successfully with version $buildVersion"# Step 5: Run Tests (mirrors CI)
    if (-not $SkipTests) {
        Write-Step "Step 5: Run Tests with Coverage"
        $env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
        if ($VerboseOutput) {
            dotnet test "$SolutionPath" --configuration $Configuration --no-build --logger "trx;LogFileName=test-results.trx" --collect:"XPlat Code Coverage" --settings "$RepositoryRoot\coverlet.runsettings" --verbosity normal
        }
        else {
            dotnet test "$SolutionPath" --configuration $Configuration --no-build --logger "trx;LogFileName=test-results.trx" --collect:"XPlat Code Coverage" --settings "$RepositoryRoot\coverlet.runsettings"
        }
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed with exit code $LASTEXITCODE"
        }
        Write-Success "All tests passed"
    }
    else {
        Write-Warning "Skipping tests"
    }    # Step 6: Test Publish Operations (mirrors CI publish steps)
    Write-Step "Step 6: Test Publish Operations"
    $cliProjectPath = Join-Path $RepositoryRoot "src\c-sharp\NotebookAutomation.Cli\NotebookAutomation.Cli.csproj"
    $tempPublishDir = Join-Path $ScriptDir "temp_publish_test"

    try {
        # Test win-x64 publish (mirrors CI)
        Write-Host "Testing win-x64 publish with version info:" -ForegroundColor $Yellow
        Write-Host "  - Package Version: $packageVersion" -ForegroundColor $Yellow
        Write-Host "  - File Version: $fileVersion" -ForegroundColor $Yellow

        dotnet publish $cliProjectPath -c $Configuration -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true /p:Version=$packageVersion /p:FileVersion=$fileVersion /p:AssemblyVersion=$assemblyVersion --output "$tempPublishDir\win-x64"
        if ($LASTEXITCODE -ne 0) {
            throw "win-x64 publish failed with exit code $LASTEXITCODE"
        }        # Verify win-x64 binary
        $winX64Binary = Join-Path "$tempPublishDir\win-x64" "na.exe"
        if (-not (Test-Path $winX64Binary)) {
            throw "win-x64 binary not found at $winX64Binary"
        }        # Test the win-x64 binary with version command
        # Only test if not in CI environment and either TestAllArch is true or platform is x64
        if ((-not $env:CI) -and ($TestAllArch -or [Environment]::Is64BitOperatingSystem)) {
            Write-Host "Testing win-x64 binary with --version option..." -ForegroundColor $Yellow
            & $winX64Binary --version
            if ($LASTEXITCODE -ne 0) {
                throw "win-x64 binary test failed with exit code $LASTEXITCODE"
            }
            Write-Success "win-x64 binary test passed"
        }

        # Test win-arm64 publish (mirrors CI)
        Write-Host "`nTesting win-arm64 publish..." -ForegroundColor $Yellow
        dotnet publish $cliProjectPath -c $Configuration -r win-arm64 /p:PublishSingleFile=true /p:SelfContained=true /p:Version=$packageVersion /p:FileVersion=$fileVersion /p:AssemblyVersion=$assemblyVersion --output "$tempPublishDir\win-arm64"
        if ($LASTEXITCODE -ne 0) {
            throw "win-arm64 publish failed with exit code $LASTEXITCODE"
        }        # Verify win-arm64 binary
        $winArm64Binary = Join-Path "$tempPublishDir\win-arm64" "na.exe"
        if (-not (Test-Path $winArm64Binary)) {
            throw "win-arm64 binary not found at $winArm64Binary"
        }        # Test the win-arm64 binary with version command if possible
        # Only attempt this on ARM64 hardware or if explicitly testing all architectures
        $isArm64Hardware = (Get-CimInstance -Class Win32_ComputerSystem).SystemType -like "*ARM64*"
        if ((-not $env:CI) -and ($TestAllArch -or $isArm64Hardware)) {
            Write-Host "Testing win-arm64 binary with --version option..." -ForegroundColor $Yellow
            try {
                & $winArm64Binary --version
                if ($LASTEXITCODE -ne 0) {
                    throw "win-arm64 binary test failed with exit code $LASTEXITCODE"
                }
                Write-Success "win-arm64 binary test passed"
            }
            catch {
                if ($isArm64Hardware) {
                    # If we're on ARM64 hardware and it still failed, that's a real error
                    throw
                }
                else {
                    # On non-ARM64 hardware, just warn that we couldn't test
                    Write-Warning "Could not test ARM64 binary on non-ARM64 hardware. This is expected."
                    Write-Warning "To fully test ARM64 binary, run this script on ARM64 hardware or use emulation."
                }
            }
        }

        # Check file sizes to verify they are reasonable
        $x64Size = (Get-Item $winX64Binary).Length / 1MB
        $arm64Size = (Get-Item $winArm64Binary).Length / 1MB

        Write-Host "`nBinary sizes:" -ForegroundColor $Yellow
        Write-Host "  - win-x64: $([math]::Round($x64Size, 2)) MB" -ForegroundColor $Yellow
        Write-Host "  - win-arm64: $([math]::Round($arm64Size, 2)) MB" -ForegroundColor $Yellow

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

}
catch {
    Write-Host "`n💥 LOCAL CI BUILD PIPELINE FAILED! 💥" -ForegroundColor $Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor $Red
    Write-Host "`nPlease fix the issues above before pushing to the repository." -ForegroundColor $Yellow
    # Display helpful commands
    Write-Host "`nHelpful Commands:" -ForegroundColor $Cyan
    Write-Host "  Fix formatting: dotnet format $SolutionPath" -ForegroundColor $Yellow
    Write-Host "  Run tests only: dotnet test $SolutionPath --configuration $Configuration" -ForegroundColor $Yellow
    Write-Host "  Build only: dotnet build $SolutionPath --configuration $Configuration" -ForegroundColor $Yellow

    exit 1
}
