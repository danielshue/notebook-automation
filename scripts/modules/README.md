# PowerShell Modules

This directory contains reusable PowerShell modules used across the Notebook Automation scripts.

## Overview

The modules provide a comprehensive, well-tested library of functionality for building, releasing, and managing the Notebook Automation project. Each module is designed to be:

- **🔄 Reusable** - Use across all scripts without duplication
- **🧪 Testable** - Each module can be tested independently
- **📚 Documented** - Comprehensive help and examples for every function
- **🛠️ Maintainable** - Bug fixes and improvements in one central location
- **✅ Consistent** - Same patterns for error handling, logging, and operations
- **🌐 Cross-Platform** - Works seamlessly on Windows, Linux, and macOS

### Module Categories

| Category    | Purpose                                                            | Modules |
| ----------- | ------------------------------------------------------------------ | ------- |
| **Core**    | Fundamental utilities (logging, platform detection, prerequisites) | 3       |
| **Build**   | .NET and plugin build operations                                   | 2       |
| **GitHub**  | GitHub CLI wrappers, artifacts, and release maintenance            | 3       |
| **Version** | Version management and Git tagging                                 | 1       |
| **Safety**  | Rollback tracking and error recovery                               | 1       |
| **Quality** | Release notes, checksums, and dependency validation                | 3       |

**Total: 4,122 lines of reusable functionality across 13 modules**

## Complete Module Catalog

The modules are organized into functional categories, each providing specialized capabilities:

### Core Modules (629 lines, 15.3%)

#### Core/Logging.psm1 (176 lines)

Unified logging and colored console output:

- `Write-Success` - Green success messages with ✅
- `Write-Error` - Red error messages with ❌
- `Write-Warning` - Yellow warning messages with ⚠️
- `Write-Step` - Cyan section headers with === borders
- `Write-ColoredOutput` - Generic colored output
- `Write-ConditionalHost` - Output respecting script-level `-Quiet` flag
- `Write-VerboseHost` - Diagnostic output respecting `-Diagnostic` flag

#### Core/Platform.psm1 (183 lines)

Cross-platform detection and utilities:

- Platform detection variables: `$IsWindows`, `$IsLinux`, `$IsMacOS`
- `Join-CrossPlatformPath` - Platform-agnostic path joining
- `Set-ExecutablePermission` - Unix chmod +x wrapper (no-op on Windows)
- `Get-PlatformName` - Returns platform as string ("windows", "linux", "macos")

#### Core/Prerequisites.psm1 (270 lines)

External dependency validation:

- `Test-GitRepository` - Git validation with branch detection
- `Test-GitHubCLI` - GitHub CLI installation and authentication check
- `Test-DotNetSDK` - .NET SDK validation with optional minimum version
- `Test-NodeJS` - Node.js validation with optional minimum version

### Build Modules (810 lines, 19.6%)

#### Build/DotNetBuild.psm1 (432 lines)

.NET build, restore, clean, and publish operations:

- `Invoke-DotNetRestore` - Restores NuGet packages
- `Invoke-DotNetClean` - Cleans build outputs
- `Invoke-DotNetBuild` - Builds solutions/projects with configuration and verbosity control
- `Invoke-DotNetPublishWithRetry` - Publishes with automatic retry logic (up to 3 attempts, exponential backoff)
- `Invoke-DotNetFormat` - Formats code using dotnet format

**Key Feature**: Resilient publishing with automatic cleanup, exponential backoff, and support for single-file/self-contained builds.

#### Build/PluginBuild.psm1 (378 lines)

Obsidian plugin npm operations and vault deployment:

- `Invoke-PluginNpmInstall` - Installs npm dependencies
- `Invoke-PluginBuild` - Builds plugin using npm scripts
- `Invoke-PluginInstallAndBuild` - Combined install and build operation
- `Update-PluginVersion` - Updates version in package.json and manifest.json
- `Deploy-PluginToVault` - Deploys built plugin to Obsidian vault for testing

**Key Feature**: Automated vault deployment for rapid development iteration.

### GitHub Modules (973 lines, 23.6%)

#### GitHub/CLI.psm1 (400 lines)

GitHub CLI wrapper functions for release and workflow management:

- `Invoke-GhRunList` - Lists workflow runs with robust JSON parsing
- `Get-WorkflowRunsForCommit` - Filters workflows for specific commit SHA
- `Wait-GitHubActionsComplete` - Monitors workflows with configurable timeout and polling
- `Invoke-GhReleaseCreate` - Creates releases with asset uploads
- `Invoke-GhReleaseDelete` - Deletes releases programmatically

**Key Feature**: Intelligent workflow monitoring with detailed status reporting and early failure detection.

#### GitHub/Artifacts.psm1 (312 lines)

GitHub Actions artifact download and management:

- `Invoke-CIArtifactDownload` - Downloads CI-built executables from GitHub Actions
- `Find-DownloadedExecutables` - Locates executables in artifact directories
- `Copy-DownloadedExecutables` - Copies executables with permission setting
- `Set-ExecutableFilePermission` - Sets executable permissions on Unix systems

**Key Feature**: Cross-platform artifact handling with automatic permission management.

#### GitHub/ReleaseManagement.psm1 (261 lines)

GitHub release management and maintenance operations:

- `Remove-OldBetaReleases` - Prunes old beta releases while keeping milestones and recent versions
- `Set-ReleasePrerelease` - Updates release prerelease flag for API sync
- `Get-BetaReleaseStats` - Analyzes beta release statistics and health

**Key Feature**: Automated release hygiene with configurable retention policies (keep recent + milestones) and API sync capabilities to fix incorrectly marked releases.

### Version Modules (378 lines, 9.2%)

#### Version/Management.psm1 (378 lines)

Version synchronization and Git tag management:

- `Get-VersionData` - Retrieves versions from package.json, manifest.json, and Git tags
- `Sync-PluginVersion` - Synchronizes version across multiple files
- `New-GitVersionTag` - Creates Git tags (lightweight or annotated)
- `Push-GitVersionTag` - Pushes tags to remote repository
- `Test-VersionFormat` - Validates semantic versioning format
- `Get-GitCommitSha` - Gets current commit SHA (full or short)

**Key Feature**: Ensures version consistency across npm, Obsidian, and Git with support for stable and pre-release versions.

### Safety Modules (444 lines, 10.8%)

#### Safety/Rollback.psm1 (444 lines)

Rollback tracking and error recovery for version management operations:

- `Initialize-RollbackTracking` - Sets up rollback state with workspace validation
- `Register-FileModification` - Tracks file modifications for rollback
- `Register-CommitCreation` - Tracks commit and tag creation
- `Register-ReleaseCreation` - Tracks GitHub release creation
- `Clear-RollbackRequirement` - Marks operation as successful
- `Invoke-PreCommitRollback` - Reverts uncommitted file changes
- `Invoke-PostCommitRollback` - Reverts commits, tags, and releases
- `Invoke-RollbackStrategy` - Executes appropriate rollback based on phase

**Key Feature**: Phase-based rollback system (PreCommit vs PostCommit) with automatic workspace state tracking and commit/tag/release reversal.

### Quality Modules (888 lines, 21.5%)

#### Quality/ReleaseNotes.psm1 (293 lines)

AI-powered release notes generation using GitHub Copilot CLI:

- `New-AIGeneratedReleaseNotes` - Generates release notes with commit analysis
- `Get-CommitRangeSinceLastRelease` - Determines commit range (beta vs stable)
- `Clean-CopilotOutput` - Removes ANSI codes and CLI artifacts

**Key Feature**: Smart commit range detection matching release types with configurable timeout and automatic output cleaning.

#### Quality/Checksums.psm1 (285 lines)

Checksum generation and validation for executables:

- `New-OrValidateChecksumsFile` - Generates or validates checksums.json
- `Test-ChecksumsFile` - Validates files against recorded checksums
- `Get-FileChecksum` - Generates SHA256 checksum for single file

**Key Feature**: Intelligent validation that creates checksums.json if missing or validates existing checksums with mismatch detection.

#### Quality/Dependencies.psm1 (310 lines)

Dependency validation and repository structure verification:

- `Test-CommandDependency` - Tests command availability with user prompts
- `Test-RepositoryStructure` - Validates repository directory structure
- `Test-AllDependencies` - Tests all required dependencies (Git, GitHub CLI, .NET, Node)

**Key Feature**: User-friendly dependency checking with installation instructions and optional vs required handling.

## Module Structure

```
modules/
├── Core/
│   ├── Logging.psm1        # Console output and logging functions
│   ├── Platform.psm1       # Platform detection and path utilities
│   └── Prerequisites.psm1  # Dependency validation (Git, .NET, Node, GitHub CLI)
│
├── Build/
│   ├── DotNetBuild.psm1    # .NET build, restore, clean, and publish operations
│   └── PluginBuild.psm1    # Obsidian plugin npm operations and deployment
│
├── GitHub/
│   ├── CLI.psm1            # GitHub CLI wrappers (releases, workflows, artifacts)
│   ├── Artifacts.psm1      # Artifact download and management
│   └── ReleaseManagement.psm1  # Release maintenance and pruning
│
├── Version/
│   └── Management.psm1     # Version synchronization and Git tag management
│
├── Safety/
│   └── Rollback.psm1       # Rollback tracking and error recovery
│
├── Quality/
│   ├── ReleaseNotes.psm1   # AI-powered release notes generation
│   ├── Checksums.psm1      # Checksum generation and validation
│   └── Dependencies.psm1   # Dependency testing and repository validation
│
└── README.md               # This file
```

## Usage

### Importing Modules

To use a module in your PowerShell script:

```powershell
# Import a single module
Import-Module (Join-Path $PSScriptRoot "modules\Core\Logging.psm1") -Force

# Import multiple modules
$ModulesDir = Join-Path $PSScriptRoot "modules"
Import-Module (Join-Path $ModulesDir "Core\Logging.psm1") -Force
Import-Module (Join-Path $ModulesDir "Core\Platform.psm1") -Force
Import-Module (Join-Path $ModulesDir "Core\Prerequisites.psm1") -Force
```

### Core/Logging Module

Provides consistent colored console output:

```powershell
Import-Module (Join-Path $PSScriptRoot "modules\Core\Logging.psm1") -Force

Write-Success "Operation completed successfully"
Write-Error "Something went wrong"
Write-Warning "This is a warning"
Write-Step "Starting new phase"
Write-ColoredOutput "Custom message" -Color Magenta
Write-ConditionalHost "Respects -Quiet flag" -ForegroundColor Cyan
Write-VerboseHost "Diagnostic info (requires -Diagnostic flag)"
```

**Available Functions:**

- `Write-Success` - Green success messages with ✅
- `Write-Error` - Red error messages with ❌
- `Write-Warning` - Yellow warning messages with ⚠️
- `Write-Step` - Cyan section headers with === borders
- `Write-ColoredOutput` - Generic colored output
- `Write-ConditionalHost` - Output that respects script-level `$Quiet` variable
- `Write-VerboseHost` - Diagnostic output that respects script-level `$Diagnostic` variable

### Core/Platform Module

Provides cross-platform detection and utilities:

```powershell
Import-Module (Join-Path $PSScriptRoot "modules\Core\Platform.psm1") -Force

# Platform detection variables are automatically initialized
if ($IsWindows) { Write-Host "Running on Windows" }
if ($IsLinux) { Write-Host "Running on Linux" }
if ($IsMacOS) { Write-Host "Running on macOS" }

# Get platform name as string
$platform = Get-PlatformName  # Returns "windows", "linux", "macos", or "unknown"

# Cross-platform path construction
$path = Join-CrossPlatformPath @("src", "c-sharp", "NotebookAutomation.sln")

# Set executable permission (Unix only)
Set-ExecutablePermission -FilePath "./my-script.sh"
```

**Available Functions:**

- `Initialize-PlatformDetection` - Sets up platform detection variables
- `Join-CrossPlatformPath` - Joins path segments using platform-appropriate separators
- `Set-ExecutablePermission` - Makes files executable on Unix systems (no-op on Windows)
- `Get-PlatformName` - Returns the current platform as a string

**Available Variables:**

- `$IsWindows` - True if running on Windows
- `$IsLinux` - True if running on Linux
- `$IsMacOS` - True if running on macOS

### Build/DotNetBuild Module

Provides .NET build, restore, clean, and publish operations:

```powershell
Import-Module (Join-Path $PSScriptRoot "modules\Build\DotNetBuild.psm1") -Force

# Restore dependencies
Invoke-DotNetRestore -Path "MyApp.sln" -ThrowOnFailure

# Clean build outputs
Invoke-DotNetClean -Path "MyApp.sln" -Configuration "Release"

# Build solution
Invoke-DotNetBuild -Path "MyApp.sln" -Configuration "Release" -NoRestore -ThrowOnFailure

# Publish with retry logic (resilient to transient errors)
Invoke-DotNetPublishWithRetry `
    -ProjectPath "MyApp.csproj" `
    -RuntimeId "win-x64" `
    -OutputDir "./dist" `
    -PackageVersion "1.0.0" `
    -PublishSingleFile `
    -SelfContained `
    -ThrowOnFailure

# Format code
Invoke-DotNetFormat -Path "MyApp.sln" -VerifyOnly
```

**Available Functions:**

- `Invoke-DotNetRestore` - Restores NuGet packages
- `Invoke-DotNetClean` - Cleans build outputs
- `Invoke-DotNetBuild` - Builds solution or project
- `Invoke-DotNetPublishWithRetry` - Publishes with automatic retry logic
- `Invoke-DotNetFormat` - Formats code using dotnet format

### Build/PluginBuild Module

Provides Obsidian plugin build and deployment operations:

```powershell
Import-Module (Join-Path $PSScriptRoot "modules\Build\PluginBuild.psm1") -Force

# Install npm dependencies
Invoke-PluginNpmInstall -PluginPath "./src/obsidian-plugin" -ThrowOnFailure

# Build plugin
Invoke-PluginBuild -PluginPath "./src/obsidian-plugin" -BuildCommand "build" -ThrowOnFailure

# Install and build in one operation
Invoke-PluginInstallAndBuild -PluginPath "./src/obsidian-plugin" -ThrowOnFailure

# Update plugin version
Update-PluginVersion `
    -PluginPath "./src/obsidian-plugin" `
    -Version "1.0.0" `
    -UpdatePackageJson `
    -UpdateManifestJson `
    -ThrowOnFailure

# Deploy to test vault
Deploy-PluginToVault `
    -PluginPath "./src/obsidian-plugin" `
    -VaultPath "C:/MyVault/.obsidian/plugins" `
    -PluginName "my-plugin" `
    -ThrowOnFailure
```

**Available Functions:**

- `Invoke-PluginNpmInstall` - Installs npm dependencies
- `Invoke-PluginBuild` - Builds plugin using npm
- `Invoke-PluginInstallAndBuild` - Combined install and build
- `Update-PluginVersion` - Updates version in plugin files
- `Deploy-PluginToVault` - Deploys plugin to Obsidian vault

### GitHub/CLI Module

Provides GitHub CLI wrapper functions:

```powershell
Import-Module (Join-Path $PSScriptRoot "modules\GitHub\CLI.psm1") -Force

# Get workflow runs
$runs = Invoke-GhRunList -Limit 20 -Fields @('status', 'conclusion', 'name')

# Get workflow runs for specific commit
$workflows = Get-WorkflowRunsForCommit -CommitSha "abc123" -Limit 50

# Wait for workflows to complete
Wait-GitHubActionsComplete `
    -CommitSha "abc123" `
    -TimeoutMinutes 30 `
    -ThrowOnFailure

# Create a release
Invoke-GhReleaseCreate `
    -Tag "v1.0.0" `
    -Title "Release 1.0.0" `
    -Notes "Release notes here" `
    -PreRelease `
    -Assets @("./dist/app.exe", "./dist/app-linux") `
    -ThrowOnFailure

# Delete a release
Invoke-GhReleaseDelete -Tag "v1.0.0" -Confirm:$false
```

**Available Functions:**

- `Invoke-GhRunList` - Lists GitHub workflow runs with JSON parsing
- `Get-WorkflowRunsForCommit` - Gets workflows for a specific commit
- `Wait-GitHubActionsComplete` - Waits for workflows to complete with timeout
- `Invoke-GhReleaseCreate` - Creates a GitHub release with assets
- `Invoke-GhReleaseDelete` - Deletes a GitHub release

### GitHub/ReleaseManagement Module

Provides release maintenance and pruning operations:

```powershell
Import-Module (Join-Path $PSScriptRoot "modules\GitHub\ReleaseManagement.psm1") -Force

# Get statistics about beta releases
Get-BetaReleaseStats

# Fix beta releases incorrectly marked as stable (for API sync)
$releases = gh release list --limit 100 --json tagName,isPrerelease | ConvertFrom-Json
$betasMarkedStable = $releases | Where-Object {
    $_.tagName -match '^v\d+\.\d+\.\d+-beta\.\d+$' -and -not $_.isPrerelease
}
foreach ($release in $betasMarkedStable) {
    Set-ReleasePrerelease -Tag $release.tagName -Prerelease $true
}

# Prune old beta releases (preview mode)
Remove-OldBetaReleases -WhatIf

# Prune old beta releases keeping last 5 + milestones
Remove-OldBetaReleases `
    -KeepCount 5 `
    -MilestoneReleases @("v0.1.0-beta.18", "v0.1.0-beta.30")

# Prune without confirmation (use with caution)
Remove-OldBetaReleases -Force -KeepCount 3
```

**Available Functions:**

- `Remove-OldBetaReleases` - Prunes old beta releases while preserving milestones
- `Set-ReleasePrerelease` - Updates release prerelease flag
- `Get-BetaReleaseStats` - Analyzes beta release statistics

### Version/Management Module

Provides version synchronization and Git tag management:

```powershell
Import-Module (Join-Path $PSScriptRoot "modules\Version\Management.psm1") -Force

# Get current version data
$versionData = Get-VersionData -PluginPath "./src/obsidian-plugin"
Write-Host "Manifest version: $($versionData.ManifestVersion)"
Write-Host "Package version: $($versionData.PackageVersion)"

# Synchronize versions
Sync-PluginVersion `
    -PluginPath "./src/obsidian-plugin" `
    -Version "1.0.0" `
    -ThrowOnFailure

# Create Git tag
New-GitVersionTag `
    -Version "1.0.0" `
    -AddVPrefix `
    -Message "Release 1.0.0" `
    -ThrowOnFailure

# Push tag to remote
Push-GitVersionTag -Tag "v1.0.0" -ThrowOnFailure

# Validate version format
$isValid = Test-VersionFormat -Version "1.0.0" -AllowPreRelease
$isValid = Test-VersionFormat -Version "1.0.0-beta.1" -AllowPreRelease

# Get current commit SHA
$sha = Get-GitCommitSha -Short
```

**Available Functions:**

- `Get-VersionData` - Gets version data from package.json, manifest.json, and Git
- `Sync-PluginVersion` - Synchronizes version across plugin files
- `New-GitVersionTag` - Creates a Git tag for a version
- `Push-GitVersionTag` - Pushes Git tags to remote
- `Test-VersionFormat` - Validates semantic version format
- `Get-GitCommitSha` - Gets current Git commit SHA

### Core/Prerequisites Module

Validates external dependencies:

```powershell
Import-Module (Join-Path $PSScriptRoot "modules\Core\Prerequisites.psm1") -Force

# Check prerequisites with helpful error messages
Test-GitRepository -ThrowOnFailure
Test-GitHubCLI -ThrowOnFailure
Test-DotNetSDK -MinimumVersion "8.0" -ThrowOnFailure
Test-NodeJS -MinimumVersion "18.0" -ThrowOnFailure

# Or check without throwing (returns true/false)
if (Test-GitHubCLI) {
    Write-Host "GitHub CLI is ready"
}

# Get current Git branch
$branch = Test-GitRepository -GetBranch
```

**Available Functions:**

- `Test-GitRepository` - Verifies current directory is a Git repository
- `Test-GitHubCLI` - Validates GitHub CLI installation and authentication
- `Test-DotNetSDK` - Checks for .NET SDK with optional version validation
- `Test-NodeJS` - Checks for Node.js with optional version validation

## Scripts Using Modules

The modules are actively used across all major scripts in the project:

### build-ci-local.ps1

Comprehensive local CI build script that mirrors GitHub Actions.

**Modules Used:**

- `Core/Logging` - Unified console output
- `Core/Prerequisites` - Node.js/npm validation
- `Build/DotNetBuild` - Clean, restore, build, format operations
- `Build/PluginBuild` - Plugin install, build, deployment

### manage-version.ps1

Complete version management solution with release automation.

**Modules Used:**

- `Core/Logging` - Console output
- `Core/Platform` - Cross-platform utilities
- `GitHub/CLI` - Workflow monitoring, release management
- `GitHub/Artifacts` - CI artifact downloads
- `GitHub/ReleaseManagement` - Release pruning and maintenance
- `Version/Management` - Version synchronization, Git tags
- `Safety/Rollback` - Complete rollback system
- `Quality/ReleaseNotes` - AI release notes generation
- `Quality/Checksums` - Checksum validation
- `Quality/Dependencies` - Dependency validation

### download-latest-artifact.ps1

Downloads latest plugin artifacts from GitHub Actions.

**Modules Used:**

- `Core/Prerequisites` - Git and GitHub CLI validation

### format-csharp-advanced.ps1

Advanced C# formatting with XML documentation spacing.

**Modules Used:**

- `Core/Logging` - Colored output functions

### check-csharp-test-documentation.ps1

C# test documentation coverage checker.

**Modules Used:**

- `Core/Logging` - Colored output functions

**Refactoring Impact**: 6 lines removed (-2%).

### check-csharp-test-documentation.ps1

C# test documentation coverage checker.

**Modules Used:**

- `Core/Logging` - Colored output functions

## Design Principles

The module system is built on the following core principles:

### 1. Self-Contained Modules

Each module is independent and can be imported separately. Dependencies between modules are minimized to maintain flexibility.

### 2. Comprehensive Documentation

All functions include PowerShell help comments with synopsis, description, parameters, and examples for ease of use.

### 3. Flexible Error Handling

Functions provide both throwing (`-ThrowOnFailure`) and non-throwing modes to suit different use cases.

### 4. Cross-Platform Compatibility

All modules work consistently across Windows, Linux, and macOS with platform-specific handling where needed.

### 5. Consistent Patterns

Modules follow consistent naming conventions, parameter structures, and output formatting throughout.

## Benefits

The module system provides significant advantages for maintaining and extending the codebase:

### Code Reuse

Common functionality is defined once and used across all scripts, eliminating duplication and reducing maintenance burden.

### Consistency

All scripts use the same logging patterns, platform detection, and validation logic, ensuring predictable behavior.

### Maintainability

Bug fixes and improvements to common functionality only need to be made in one central location.

### Testability

Modules can be independently tested and validated, improving code quality and reliability.

### Discoverability

Centralized documentation makes it easier to understand available functionality and find the right tool for the job.

## Testing

Run the test script to verify all modules work correctly:

```powershell
.\scripts\test-modules.ps1
```

This will:

1. Import each module
2. Test basic functionality
3. Verify platform detection
4. Check prerequisite validation

## Migration Guide

To convert an existing script to use these modules:

### Step 1: Import the required modules

```powershell
# Add at the top of your script after parameters
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ModulesDir = Join-Path $ScriptDir "modules"
Import-Module (Join-Path $ModulesDir "Core\Logging.psm1") -Force
```

### Step 2: Remove duplicate functions

Delete any local definitions of:

- `Write-Success`, `Write-Error`, `Write-Warning`, `Write-Step`
- `Write-ColoredOutput`, `Write-ConditionalHost`, `Write-VerboseHost`
- Platform detection code (`$IsWindows`, `$IsLinux`, `$IsMacOS`)
- `Join-CrossPlatformPath`, `Set-ExecutablePermission`
- Git/GitHub CLI validation code

### Step 3: Replace prerequisite checks

Before:

```powershell
try {
    $branch = git rev-parse --abbrev-ref HEAD
    Write-Host "✓ Git repository found, current branch: $branch"
}
catch {
    Write-Host "✗ Error: Not in a git repository" -ForegroundColor Red
    exit 1
}
```

After:

```powershell
$branch = Test-GitRepository -GetBranch -ThrowOnFailure
```

## Future Enhancements

The module library is designed to grow as new patterns and needs emerge:

- Additional GitHub integration modules for advanced workflow management
- Enhanced build system modules for specialized compilation scenarios
- Security and compliance modules for audit and verification
- Performance monitoring and profiling utilities
- Extended cross-platform compatibility features

New modules will be added incrementally as clear patterns of duplication or new requirements emerge.

## Contributing

When adding new modules or functions:

1. **Follow existing patterns** - Use similar structure and documentation style
2. **Keep modules focused** - Each module should have a single responsibility
3. **Document thoroughly** - Include help comments with examples
4. **Test cross-platform** - Verify functionality on Windows, Linux, and macOS
5. **Minimize dependencies** - Keep modules as independent as possible
6. **Handle errors gracefully** - Provide both throwing and non-throwing modes

## Module Statistics Summary

### Statistics

The module library contains **4,122 lines of reusable functionality across 13 modules**, organized into 6 functional categories:

| Category | Modules | Lines | % of Total |
| -------- | ------- | ----- | ---------- |
| Core     | 3       | 629   | 15.3%      |
| Build    | 2       | 810   | 19.7%      |
| GitHub   | 3       | 973   | 23.6%      |
| Version  | 1       | 378   | 9.2%       |
| Safety   | 1       | 444   | 10.8%      |
| Quality  | 3       | 888   | 21.5%      |

### Module Quality Standards

All modules include:

- ✅ Comprehensive PowerShell help documentation (.SYNOPSIS, .DESCRIPTION, .PARAMETER, .EXAMPLE)
- ✅ Error handling with both throwing (-ThrowOnFailure) and non-throwing modes
- ✅ Cross-platform compatibility (Windows, Linux, macOS)
- ✅ Consistent parameter naming and structure
- ✅ Proper error messages and status output
