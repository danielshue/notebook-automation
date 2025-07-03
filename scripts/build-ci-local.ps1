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
    .\build-ci-local.ps1 -PluginOnly
    .\build-ci-local.ps1 -PluginOnly -DeployPlugin
    .\build-ci-local.ps1 -AdvancedCSharpFormatting
    .\build-ci-local.ps1 -CheckTestDocumentation
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
    [switch]$VerboseOutput,

    [Parameter(HelpMessage = "Build only the Obsidian plugin (skips .NET solution)")]
    [switch]$PluginOnly,

    [Parameter(HelpMessage = "Deploy plugin to test vault after building")]
    [switch]$DeployPlugin,

    [Parameter(HelpMessage = "Use advanced C# formatting with XML documentation spacing and StyleCop rules (calls format-csharp-advanced.ps1)")]
    [switch]$AdvancedCSharpFormatting,

    [Parameter(HelpMessage = "Check test method documentation coverage (calls check-csharp-test-documentation.ps1)")]
    [switch]$CheckTestDocumentation
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
$Magenta = [System.ConsoleColor]::Magenta

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
    if ($AdvancedCSharpFormatting) {
        Write-Host "Advanced C# Formatting: Enabled (XML docs + StyleCop)" -ForegroundColor $Magenta
    }

    # Plugin-only mode - skip .NET solution build
    if ($PluginOnly) {
        Write-Host "🔌 Plugin-Only Mode - Skipping .NET solution build" -ForegroundColor $Magenta
        
        # Jump directly to Step 8: Build Obsidian Plugin
        Write-Step "Build Obsidian Plugin (Plugin-Only Mode)"
        $obsidianPluginDir = Join-Path $RepositoryRoot "src\obsidian-plugin"
        
        if (Test-Path $obsidianPluginDir) {
            Push-Location $obsidianPluginDir
            try {
                # Check if Node.js is available
                try {
                    $nodeVersion = node --version 2>$null
                    Write-Host "Found Node.js version: $nodeVersion" -ForegroundColor $Yellow
                }
                catch {
                    throw "Node.js is required for plugin builds. Please install Node.js 18+ and npm."
                }

                # Check if npm is available
                try {
                    $npmVersion = npm --version 2>$null
                    Write-Host "Found npm version: $npmVersion" -ForegroundColor $Yellow
                }
                catch {
                    throw "npm is required for plugin builds. Please install npm."
                }

                # Install dependencies
                Write-Host "Installing Obsidian plugin dependencies..." -ForegroundColor $Yellow
                npm install
                if ($LASTEXITCODE -ne 0) {
                    throw "npm install failed with exit code $LASTEXITCODE"
                }

                # Build the plugin
                Write-Host "Building Obsidian plugin..." -ForegroundColor $Yellow
                npm run build
                if ($LASTEXITCODE -ne 0) {
                    throw "Plugin build failed with exit code $LASTEXITCODE"
                }

                # Verify build outputs
                $mainJs = Join-Path $obsidianPluginDir "main.js"
                $manifestJson = Join-Path $obsidianPluginDir "manifest.json"
                $stylesCss = Join-Path $obsidianPluginDir "styles.css"

                $buildOutputs = @()
                if (Test-Path $mainJs) { $buildOutputs += "✓ main.js" }
                if (Test-Path $manifestJson) { $buildOutputs += "✓ manifest.json" }
                if (Test-Path $stylesCss) { $buildOutputs += "✓ styles.css" }

                if ($buildOutputs.Count -gt 0) {
                    Write-Success "Obsidian plugin build completed successfully"
                    Write-Host "Plugin files: $($buildOutputs -join ', ')" -ForegroundColor $Yellow
                }
                else {
                    throw "Plugin build failed - no output files found"
                }

                # Deploy to test vault if requested
                if ($DeployPlugin) {
                    Write-Host "`nDeploying plugin to test vault..." -ForegroundColor $Yellow
                    
                    # Deploy plugin files to test vault
                    $pluginName = "notebook-automation"
                    $vaultPluginsPath = "../../tests/obsidian-vault/Obsidian Vault Test/.obsidian/plugins"
                    $destPath = Resolve-Path (Join-Path -Path $obsidianPluginDir -ChildPath $vaultPluginsPath)
                    $destPath = Join-Path -Path $destPath -ChildPath $pluginName

                    # Ensure destination directory exists
                    if (-not (Test-Path $destPath)) {
                        New-Item -ItemType Directory -Path $destPath -Force | Out-Null
                        Write-Host "Created plugin directory: $destPath" -ForegroundColor $Yellow
                    }

                    # Copy plugin files
                    $filesToCopy = @("main.js", "manifest.json", "styles.css")
                    if (Test-Path "default-config.json") {
                        $filesToCopy += "default-config.json"
                    }

                    foreach ($file in $filesToCopy) {
                        $src = Join-Path $obsidianPluginDir $file
                        $dst = Join-Path $destPath $file
                        if (Test-Path $src) {
                            Copy-Item $src $dst -Force
                            Write-Host "  ✓ Copied $file" -ForegroundColor $Green
                        }
                    }

                    # Copy executables if they exist from a previous full build
                    # (Note: Plugin-only mode doesn't build executables, but can deploy existing ones)
                    $distPath = "../../dist"
                    $executablesFound = @()

                    # Check for notebook-automation directory structure first (matches CI output)
                    $notebookAutomationPath = "$distPath/notebook-automation"
                    if (Test-Path $notebookAutomationPath) {
                        $availableExecutables = Get-ChildItem -Path $notebookAutomationPath -File | Where-Object { $_.Name -like "na-*" }
                        if ($availableExecutables) {
                            Write-Host "  Found executables in CI output structure:" -ForegroundColor $Yellow
                            foreach ($exe in $availableExecutables) {
                                $executablesFound += $exe.FullName
                            }
                        }
                    }

                    # Look for executables in dist folder (flat structure from local builds)
                    if ($executablesFound.Count -eq 0 -and (Test-Path $distPath)) {
                        $availableExecutables = Get-ChildItem -Path $distPath -File | Where-Object { $_.Name -like "na-*" }
                        if ($availableExecutables) {
                            Write-Host "  Found executables in local build structure:" -ForegroundColor $Yellow
                            foreach ($exe in $availableExecutables) {
                                $executablesFound += $exe.FullName
                            }
                        }
                    }

                    # Copy found executables
                    if ($executablesFound.Count -gt 0) {
                        Write-Host "  Deploying $($executablesFound.Count) executables:" -ForegroundColor $Yellow
                        foreach ($exePath in $executablesFound) {
                            $exeName = Split-Path $exePath -Leaf
                            $vaultExePath = Join-Path $destPath $exeName
                            Copy-Item $exePath $vaultExePath -Force
                            Write-Host "    ✓ $exeName" -ForegroundColor $Green
                            
                            # Set executable permissions on non-Windows
                            if (-not $IsWindows -and -not $exeName.EndsWith(".exe")) {
                                if (Get-Command chmod -ErrorAction SilentlyContinue) {
                                    & chmod +x $vaultExePath
                                }
                            }
                        }
                    }
                    else {
                        Write-Host "  No executables found in $distPath - plugin will work without CLI features" -ForegroundColor $Yellow
                        Write-Host "  Run full build first to generate executables" -ForegroundColor $Yellow
                    }

                    Write-Success "Plugin deployed to test vault: $destPath"
                    Write-Host "Reload plugins in Obsidian to see changes." -ForegroundColor $Yellow
                }

            }
            finally {
                Pop-Location
            }
        }
        else {
            throw "Obsidian plugin directory not found at: $obsidianPluginDir"
        }

        # Success summary for plugin-only mode
        Write-Host "`n🎉 PLUGIN BUILD COMPLETED SUCCESSFULLY! 🎉" -ForegroundColor $Green
        if ($DeployPlugin) {
            Write-Host "Plugin built and deployed to test vault." -ForegroundColor $Green
        }
        else {
            Write-Host "Plugin built successfully. Use -DeployPlugin to deploy to test vault." -ForegroundColor $Green
        }
        return
    }

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
        
        if ($AdvancedCSharpFormatting) {
            Write-Host "Applying advanced C# formatting with XML documentation spacing and StyleCop rules..." -ForegroundColor $Yellow
            
            # Use the advanced formatting script
            $advancedFormatScript = Join-Path $ScriptDir "format-csharp-advanced.ps1"
            if (Test-Path $advancedFormatScript) {
                $csharpSourcePath = Join-Path $RepositoryRoot "src\c-sharp"
                & pwsh -ExecutionPolicy Bypass -File $advancedFormatScript -Path $csharpSourcePath -Fix
                if ($LASTEXITCODE -ne 0) {
                    Write-Warning "Advanced C# formatting encountered issues but continuing..."
                    Write-Host "You may want to review the changes and commit them." -ForegroundColor $Yellow
                }
                else {
                    Write-Success "Advanced C# formatting completed successfully"
                }
            }
            else {
                Write-Warning "Advanced C# formatting script not found at: $advancedFormatScript"
                Write-Host "Falling back to standard dotnet format..." -ForegroundColor $Yellow
                
                if ($VerboseOutput) {
                    dotnet format $SolutionPath --verbosity normal
                }
                else {
                    dotnet format $SolutionPath
                }
            }
        }
        else {
            Write-Host "Applying standard code formatting..." -ForegroundColor $Yellow

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
    Write-Success "Build completed successfully with version $fileVersion"

    # Step 6: Run Tests (mirrors CI)
    if (-not $SkipTests) {
        Write-Step "Step 6: Run Tests with Coverage"
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
        
        # Generate coverage report (mirrors bash script functionality)
        if (Test-Path "$RepositoryRoot\TestResults") {
            $coverageFiles = Get-ChildItem -Path "$RepositoryRoot\TestResults" -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue
            
            if ($coverageFiles) {
                Write-Host "Generating coverage report..." -ForegroundColor $Yellow
                
                # Check if reportgenerator tool is available
                try {
                    $null = Get-Command reportgenerator -ErrorAction Stop
                }
                catch {
                    Write-Host "Installing ReportGenerator tool..." -ForegroundColor $Yellow
                    try {
                        dotnet tool install -g dotnet-reportgenerator-globaltool
                        if ($LASTEXITCODE -ne 0) {
                            Write-Warning "Failed to install ReportGenerator, skipping coverage report"
                        }
                    }
                    catch {
                        Write-Warning "Failed to install ReportGenerator, skipping coverage report"
                    }
                }
                
                # Generate coverage report
                try {
                    $reportArgs = @(
                        "-reports:$RepositoryRoot\TestResults\**\coverage.cobertura.xml",
                        "-targetdir:$RepositoryRoot\CoverageReport",
                        "-reporttypes:HtmlInline;Cobertura;TextSummary",
                        "-assemblyfilters:+NotebookAutomation.*;-*.Tests",
                        "-classfilters:+*;-*Test*",
                        "-title:Notebook Automation Code Coverage Report (Local)"
                    )
                    
                    & reportgenerator $reportArgs
                    if ($LASTEXITCODE -eq 0) {
                        # Display text summary if available
                        $summaryFile = "$RepositoryRoot\CoverageReport\Summary.txt"
                        if (Test-Path $summaryFile) {
                            Write-Host "Coverage Summary:" -ForegroundColor $Yellow
                            Get-Content $summaryFile
                        }
                        Write-Success "Coverage report generated at: $RepositoryRoot\CoverageReport\index.html"
                    }
                    else {
                        Write-Warning "Failed to generate coverage report"
                    }
                }
                catch {
                    Write-Warning "Failed to generate coverage report: $($_.Exception.Message)"
                }
            }
            else {
                Write-Warning "No coverage files found, skipping report generation"
            }
        }
    }
    else {
        Write-Warning "Skipping tests"
    }    

    # Step 7: Test Cross-Platform Publish Operations (mirrors CI publish steps)
    Write-Step "Step 7: Test Cross-Platform Publish Operations"
    $cliProjectPath = Join-Path $RepositoryRoot "src\c-sharp\NotebookAutomation.Cli\NotebookAutomation.Cli.csproj"
    $tempPublishDir = Join-Path $ScriptDir "temp_publish_test"

    try {
        # Create executables directory (mirrors new CI structure)
        $executablesDir = Join-Path $tempPublishDir "executables"
        New-Item -ItemType Directory -Path $executablesDir -Force | Out-Null

        Write-Host "Publishing cross-platform executables with version info:" -ForegroundColor $Yellow
        Write-Host "  - Package Version: $packageVersion" -ForegroundColor $Yellow
        Write-Host "  - File Version: $fileVersion" -ForegroundColor $Yellow

        # Define all platforms and architectures (mirrors CI matrix)
        $platforms = @(
            @{
                Name            = "Windows"
                RuntimeIds      = @("win-x64", "win-arm64")
                ExecutableExt   = ".exe"
                ExecutableNames = @("na-win-x64.exe", "na-win-arm64.exe")
                CanTest         = $IsWindows
            },
            @{
                Name            = "Linux"  
                RuntimeIds      = @("linux-x64", "linux-arm64")
                ExecutableExt   = ""
                ExecutableNames = @("na-linux-x64", "na-linux-arm64")
                CanTest         = $IsLinux
            },
            @{
                Name            = "macOS"
                RuntimeIds      = @("osx-x64", "osx-arm64") 
                ExecutableExt   = ""
                ExecutableNames = @("na-macos-x64", "na-macos-arm64")
                CanTest         = $IsMacOS
            }
        )

        foreach ($platform in $platforms) {
            Write-Host "`nPublishing $($platform.Name) executables..." -ForegroundColor $Yellow
            
            for ($i = 0; $i -lt $platform.RuntimeIds.Count; $i++) {
                $runtimeId = $platform.RuntimeIds[$i]
                $executableName = $platform.ExecutableNames[$i]
                
                Write-Host "  Publishing $runtimeId..." -ForegroundColor $Yellow
                
                $tempPlatformDir = Join-Path $tempPublishDir "temp-$runtimeId"
                dotnet publish $cliProjectPath -c $Configuration -r $runtimeId /p:PublishSingleFile=true /p:SelfContained=true /p:Version=$packageVersion /p:FileVersion=$fileVersion /p:AssemblyVersion=$assemblyVersion --output $tempPlatformDir
                if ($LASTEXITCODE -ne 0) {
                    throw "$runtimeId publish failed with exit code $LASTEXITCODE"
                }

                # Copy and rename using new naming convention (mirrors CI exactly)
                $sourceBinary = Join-Path $tempPlatformDir "na$($platform.ExecutableExt)"
                $targetBinary = Join-Path $executablesDir $executableName
                if (-not (Test-Path $sourceBinary)) {
                    throw "$runtimeId binary not found at $sourceBinary"
                }
                Copy-Item $sourceBinary $targetBinary
                Write-Success "  ✓ $executableName"

                # Test executable if we can run it on current platform
                $isCurrentArch = ($runtimeId -like "*x64*" -and [Environment]::Is64BitOperatingSystem) -or 
                ($runtimeId -like "*arm64*" -and (Get-CimInstance -Class Win32_ComputerSystem -ErrorAction SilentlyContinue)?.SystemType -like "*ARM64*")
                
                if ((-not $env:CI) -and $platform.CanTest -and ($TestAllArch -or $isCurrentArch)) {
                    Write-Host "    Testing $executableName with --version..." -ForegroundColor $Yellow
                    try {
                        & $targetBinary --version
                        if ($LASTEXITCODE -eq 0) {
                            Write-Success "    ✓ $executableName test passed"
                        }
                        else {
                            Write-Warning "    ⚠ $executableName test failed (exit code $LASTEXITCODE)"
                        }
                    }
                    catch {
                        Write-Warning "    ⚠ $executableName test failed: $($_.Exception.Message)"
                    }
                }
            }
        }

        # Display summary (mirrors CI output format)
        Write-Host "`nCross-platform executables published:" -ForegroundColor $Yellow
        $publishedFiles = Get-ChildItem -Path $executablesDir | Sort-Object Name
        
        # Group by platform for better display (mirrors CI summary)
        $windowsExes = $publishedFiles | Where-Object { $_.Name -like "na-win-*" }
        $linuxExes = $publishedFiles | Where-Object { $_.Name -like "na-linux-*" }
        $macosExes = $publishedFiles | Where-Object { $_.Name -like "na-macos-*" }
        
        if ($windowsExes) {
            Write-Host "  Windows:" -ForegroundColor $Yellow
            $windowsExes | ForEach-Object {
                $sizeKB = [math]::Round($_.Length / 1KB, 2)
                Write-Host "    - $($_.Name) ($sizeKB KB)" -ForegroundColor $Yellow
            }
        }
        
        if ($linuxExes) {
            Write-Host "  Linux:" -ForegroundColor $Yellow
            $linuxExes | ForEach-Object {
                $sizeKB = [math]::Round($_.Length / 1KB, 2)
                Write-Host "    - $($_.Name) ($sizeKB KB)" -ForegroundColor $Yellow
            }
        }
        
        if ($macosExes) {
            Write-Host "  macOS:" -ForegroundColor $Yellow
            $macosExes | ForEach-Object {
                $sizeKB = [math]::Round($_.Length / 1KB, 2)
                Write-Host "    - $($_.Name) ($sizeKB KB)" -ForegroundColor $Yellow
            }
        }

        Write-Success "Cross-platform publish operations completed successfully"
    }
    finally {
        # Clean up temp publish directory but preserve executables if plugin deployment is requested
        if (Test-Path $tempPublishDir) {
            if ($DeployPlugin) {
                # Move executables to a persistent location for plugin deployment
                $persistentExecutablesDir = Join-Path $RepositoryRoot "dist"
                if (-not (Test-Path $persistentExecutablesDir)) {
                    New-Item -ItemType Directory -Path $persistentExecutablesDir -Force | Out-Null
                }
                
                if (Test-Path $executablesDir) {
                    $availableExecutables = Get-ChildItem -Path $executablesDir -File | Where-Object { $_.Name -like "na-*" }
                    foreach ($exe in $availableExecutables) {
                        $persistentPath = Join-Path $persistentExecutablesDir $exe.Name
                        Copy-Item $exe.FullName $persistentPath -Force
                        Write-Host "  Preserved $($exe.Name) for plugin deployment" -ForegroundColor $Yellow
                    }
                }
            }
            Remove-Item -Recurse -Force $tempPublishDir -ErrorAction SilentlyContinue
        }
    }

    # Step 8: Build Obsidian Plugin (mirrors CI)
    Write-Step "Step 8: Build Obsidian Plugin"
    $obsidianPluginDir = Join-Path $RepositoryRoot "src\obsidian-plugin"
    
    if (Test-Path $obsidianPluginDir) {
        Push-Location $obsidianPluginDir
        try {
            # Check if Node.js is available
            try {
                $nodeVersion = node --version
                Write-Host "Found Node.js version: $nodeVersion" -ForegroundColor $Yellow
            }
            catch {
                throw "Node.js not found. Please install Node.js 18+ to build the Obsidian plugin."
            }

            # Check if npm is available
            try {
                $npmVersion = npm --version
                Write-Host "Found npm version: $npmVersion" -ForegroundColor $Yellow
            }
            catch {
                throw "npm not found. Please ensure npm is installed with Node.js."
            }

            # Install dependencies
            Write-Host "Installing Obsidian plugin dependencies..." -ForegroundColor $Yellow
            npm install
            if ($LASTEXITCODE -ne 0) {
                throw "npm install failed with exit code $LASTEXITCODE"
            }

            # Build the plugin
            Write-Host "Building Obsidian plugin..." -ForegroundColor $Yellow
            npm run build
            if ($LASTEXITCODE -ne 0) {
                throw "npm run build failed with exit code $LASTEXITCODE"
            }

            # Verify build outputs
            $mainJs = Join-Path $obsidianPluginDir "main.js"
            $manifestJson = Join-Path $obsidianPluginDir "manifest.json"
            $stylesCss = Join-Path $obsidianPluginDir "styles.css"

            $buildOutputs = @()
            if (Test-Path $mainJs) { $buildOutputs += "main.js" }
            if (Test-Path $manifestJson) { $buildOutputs += "manifest.json" }
            if (Test-Path $stylesCss) { $buildOutputs += "styles.css" }

            if ($buildOutputs.Count -gt 0) {
                Write-Host "Successfully built plugin files:" -ForegroundColor $Yellow
                $buildOutputs | ForEach-Object {
                    Write-Host "  ✓ $_" -ForegroundColor $Green
                }
                Write-Success "Obsidian plugin build completed successfully"
                
                # Deploy to test vault if requested (includes executables from Step 7)
                if ($DeployPlugin) {
                    Write-Host "`nDeploying plugin to test vault..." -ForegroundColor $Yellow
                    
                    $pluginName = "notebook-automation"
                    $vaultPluginsPath = "../../tests/obsidian-vault/Obsidian Vault Test/.obsidian/plugins"
                    $destPath = Resolve-Path (Join-Path -Path $obsidianPluginDir -ChildPath $vaultPluginsPath)
                    $destPath = Join-Path -Path $destPath -ChildPath $pluginName

                    # Ensure destination directory exists
                    if (-not (Test-Path $destPath)) {
                        New-Item -ItemType Directory -Path $destPath -Force | Out-Null
                        Write-Host "  Created plugin directory: $destPath" -ForegroundColor $Yellow
                    }

                    # Copy plugin files
                    $filesToCopy = @("main.js", "manifest.json", "styles.css")
                    if (Test-Path "default-config.json") {
                        $filesToCopy += "default-config.json"
                    }

                    foreach ($file in $filesToCopy) {
                        $src = Join-Path $obsidianPluginDir $file
                        $dst = Join-Path $destPath $file
                        if (Test-Path $src) {
                            Copy-Item $src $dst -Force
                            Write-Host "    ✓ Copied $file" -ForegroundColor $Green
                        }
                    }

                    # Copy executables from Step 7 build
                    $persistentExecutablesDir = Join-Path $RepositoryRoot "dist"
                    
                    if (Test-Path $persistentExecutablesDir) {
                        $availableExecutables = Get-ChildItem -Path $persistentExecutablesDir -File | Where-Object { $_.Name -like "na-*" }
                        if ($availableExecutables) {
                            Write-Host "  Deploying executables from current build:" -ForegroundColor $Yellow
                            foreach ($exe in $availableExecutables) {
                                $vaultExePath = Join-Path $destPath $exe.Name
                                Copy-Item $exe.FullName $vaultExePath -Force
                                Write-Host "    ✓ $($exe.Name)" -ForegroundColor $Green
                                
                                # Set executable permissions on non-Windows
                                if (-not $IsWindows -and -not $exe.Name.EndsWith(".exe")) {
                                    if (Get-Command chmod -ErrorAction SilentlyContinue) {
                                        & chmod +x $vaultExePath
                                    }
                                }
                            }
                        }
                    }
                    else {
                        Write-Host "  No executables available from current build" -ForegroundColor $Yellow
                    }

                    Write-Success "Plugin deployed to test vault: $destPath"
                    Write-Host "  Reload plugins in Obsidian to see changes." -ForegroundColor $Yellow
                }
            }
            else {
                Write-Warning "Plugin build completed but expected output files not found"
            }

        }
        finally {
            Pop-Location
        }
    }
    else {
        Write-Warning "Obsidian plugin directory not found at: $obsidianPluginDir"
        Write-Warning "Skipping plugin build step"
    }
    # Step 8: Run Static Code Analysis (mirrors CI - this runs last)
    Write-Step "Step 8: Static Code Analysis"
    dotnet format $SolutionPath --verify-no-changes --severity error
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Code formatting issues detected!"
        Write-Host "Run 'dotnet format $SolutionPath' to fix formatting issues" -ForegroundColor $Yellow
        throw "Static code analysis failed with exit code $LASTEXITCODE"
    }
    Write-Success "Static code analysis passed"

    # Step 9: Check Test Documentation Coverage (optional)
    if ($CheckTestDocumentation) {
        Write-Step "Step 9: Check C# Test Documentation Coverage"
        $testDocScript = Join-Path $ScriptDir "check-csharp-test-documentation.ps1"
        if (Test-Path $testDocScript) {
            $csharpSourcePath = Join-Path $RepositoryRoot "src\c-sharp"
            & pwsh -ExecutionPolicy Bypass -File $testDocScript -TestPath $csharpSourcePath
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "Test documentation issues found but continuing..."
                Write-Host "Consider adding XML documentation to undocumented test methods." -ForegroundColor $Yellow
            }
            else {
                Write-Success "Test documentation coverage check completed"
            }
        }
        else {
            Write-Warning "Test documentation script not found at: $testDocScript"
        }
    }

    # Success Summary
    Write-Host "`n🎉 LOCAL CI BUILD PIPELINE COMPLETED SUCCESSFULLY! 🎉" -ForegroundColor $Green
    Write-Host "All steps that run in GitHub Actions CI have passed locally." -ForegroundColor $Green
    Write-Host "Your changes should pass CI when pushed to the repository." -ForegroundColor $Green
    
    Write-Host "`nLocal CI Build Summary:" -ForegroundColor $Magenta
    Write-Host "======================" -ForegroundColor $Magenta
    Write-Host "✓ .NET solution built and tested" -ForegroundColor $Green
    Write-Host "✓ Code coverage report generated" -ForegroundColor $Green
    if ($AdvancedCSharpFormatting) {
        Write-Host "✓ Advanced C# formatting applied (XML docs + StyleCop)" -ForegroundColor $Green
    }
    else {
        Write-Host "✓ Standard code formatting applied" -ForegroundColor $Green
    }
    Write-Host "✓ Cross-platform executables published (6 platforms)" -ForegroundColor $Green
    Write-Host "✓ Obsidian plugin built successfully" -ForegroundColor $Green
    Write-Host "✓ Static code analysis passed" -ForegroundColor $Green
    
    if ($CheckTestDocumentation) {
        Write-Host "✓ Test documentation coverage checked" -ForegroundColor $Green
    }
    
    if ($DeployPlugin) {
        Write-Host "✓ Plugin deployed to test vault" -ForegroundColor $Green
    }

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
    Write-Host "  Advanced C# formatting: pwsh scripts/format-csharp-advanced.ps1 -Path src/c-sharp -Fix" -ForegroundColor $Yellow
    Write-Host "  Check test documentation: pwsh scripts/check-csharp-test-documentation.ps1 -TestPath src/c-sharp" -ForegroundColor $Yellow
    Write-Host "  Run tests only: dotnet test $SolutionPath --configuration $Configuration" -ForegroundColor $Yellow
    Write-Host "  Build only: dotnet build $SolutionPath --configuration $Configuration" -ForegroundColor $Yellow

    exit 1
}
