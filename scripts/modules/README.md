# PowerShell Modules

This directory contains reusable PowerShell modules used across the Notebook Automation scripts.

## Overview

The modules provide common functionality that was previously duplicated across multiple scripts. The refactoring achieved:

### Code Reduction
- **Scripts reduced by 969 lines (-22%)** - Duplicate code eliminated
- **3,861 lines of reusable module code** - Now available to all scripts
- **Net result**: Added 2,892 lines total, but these are now reusable across all current and future scripts

### Scripts Before & After
| Script | Before | After | Reduction |
|--------|--------|-------|-----------|
| manage-version.ps1 | 2,531 lines | 1,603 lines | -928 lines (-37%) |
| build-ci-local.ps1 | 1,066 lines | ~900 lines | ~166 lines (-16%) |
| download-latest-artifact.ps1 | 420 lines | 401 lines | -19 lines (-5%) |
| format-csharp-advanced.ps1 | 256 lines | 250 lines | -6 lines (-2%) |
| check-csharp-test-documentation.ps1 | 125 lines | 125 lines | 0 lines |
| **TOTAL** | **4,398 lines** | **~3,279 lines** | **~1,119 lines (-25%)** |

### Value Proposition
**Did the refactoring save lines of code?**
- **From a script maintenance perspective: YES** - 969+ lines removed from scripts (-22%)
- **From a total codebase perspective**: Net +2,892 lines, BUT those 3,861 module lines are:
  - **Reusable** across all scripts (not duplicated anymore)
  - **Testable** independently
  - **Documented** comprehensively  
  - **Maintainable** in one location (bug fixes only needed once)
  - **Consistent** - same error handling, logging, and patterns everywhere

**The modules made the codebase more modular, not necessarily smaller**, which was the primary goal.

## Complete Module Catalog

### Core Modules (629 lines, 16.3%)

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

### Build Modules (810 lines, 21.0%)

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

### GitHub Modules (712 lines, 18.4%)

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

### Version Modules (378 lines, 9.8%)

#### Version/Management.psm1 (378 lines)
Version synchronization and Git tag management:
- `Get-VersionData` - Retrieves versions from package.json, manifest.json, and Git tags
- `Sync-PluginVersion` - Synchronizes version across multiple files
- `New-GitVersionTag` - Creates Git tags (lightweight or annotated)
- `Push-GitVersionTag` - Pushes tags to remote repository
- `Test-VersionFormat` - Validates semantic versioning format
- `Get-GitCommitSha` - Gets current commit SHA (full or short)

**Key Feature**: Ensures version consistency across npm, Obsidian, and Git with support for stable and pre-release versions.

### Safety Modules (444 lines, 11.5%)

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

### Quality Modules (888 lines, 23.0%)

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
│   └── Artifacts.psm1      # Artifact download and management
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

### build-ci-local.ps1
**Modules Used:**
- `Core/Logging` - Unified console output
- `Core/Prerequisites` - Node.js/npm validation
- `Build/DotNetBuild` - Clean, restore, build, format operations
- `Build/PluginBuild` - Plugin install, build, deployment

**Refactoring Impact**: ~166 lines removed (-16%), now uses module functions for all build operations.

### manage-version.ps1
**Modules Used:**
- `Core/Logging` - Console output
- `Core/Platform` - Cross-platform utilities
- `GitHub/CLI` - Workflow monitoring, release management
- `GitHub/Artifacts` - CI artifact downloads
- `Version/Management` - Version synchronization, Git tags
- `Safety/Rollback` - Complete rollback system
- `Quality/ReleaseNotes` - AI release notes generation
- `Quality/Checksums` - Checksum validation
- `Quality/Dependencies` - Dependency validation

**Refactoring Impact**: 928 lines removed (-37%), transformed from 2,531 to 1,603 lines.

### download-latest-artifact.ps1
**Modules Used:**
- `Core/Prerequisites` - Git and GitHub CLI validation

**Refactoring Impact**: 19 lines removed (-5%).

### format-csharp-advanced.ps1
**Modules Used:**
- `Core/Logging` - Colored output functions

**Refactoring Impact**: 6 lines removed (-2%).

### check-csharp-test-documentation.ps1
**Modules Used:**
- `Core/Logging` - Colored output functions

**Refactoring Impact**: Minimal (already lean).

## Design Principles

### 1. Minimal Changes
The modules extract only the most duplicated functionality, maintaining backward compatibility with existing scripts.

### 2. Self-Contained
Each module is independent and can be imported separately. Dependencies between modules are minimized.

### 3. Comprehensive Documentation
All functions include PowerShell help comments with synopsis, description, parameters, and examples.

### 4. Error Handling
Functions provide both throwing (`-ThrowOnFailure`) and non-throwing modes for flexibility.

### 5. Cross-Platform
All modules work consistently across Windows, Linux, and macOS.

## Benefits

### Code Reuse
Common functionality is defined once and used across all scripts, reducing duplication.

### Consistency
All scripts use the same logging patterns, platform detection, and validation logic.

### Maintainability
Bug fixes and improvements to common functionality only need to be made in one place.

### Testability
Modules can be independently tested and validated (see `scripts/test-modules.ps1`).

### Documentation
Centralized documentation makes it easier to understand available functionality.

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

Potential future modules (as needed):

- **Build/DotNetBuild.psm1** - .NET build operations
- **Build/PluginBuild.psm1** - Obsidian plugin build
- **GitHub/CLI.psm1** - GitHub CLI wrapper functions
- **GitHub/Actions.psm1** - CI/workflow monitoring
- **GitHub/Artifacts.psm1** - Artifact download & management
- **Version/Management.psm1** - Version sync & validation

These will be added incrementally as clear patterns of duplication emerge.

## Contributing

When adding new modules or functions:

1. **Follow existing patterns** - Use similar structure and documentation style
2. **Keep modules focused** - Each module should have a single responsibility
3. **Document thoroughly** - Include help comments with examples
4. **Test cross-platform** - Verify functionality on Windows, Linux, and macOS
5. **Minimize dependencies** - Keep modules as independent as possible
6. **Handle errors gracefully** - Provide both throwing and non-throwing modes

## Module Statistics Summary

**Total: 3,861 lines of reusable functionality across 12 modules**

| Category | Modules | Lines | % of Total |
|----------|---------|-------|------------|
| Core | 3 | 629 | 16.3% |
| Build | 2 | 810 | 21.0% |
| GitHub | 2 | 712 | 18.4% |
| Version | 1 | 378 | 9.8% |
| Safety | 1 | 444 | 11.5% |
| Quality | 3 | 888 | 23.0% |

All modules include:
- ✅ Comprehensive PowerShell help documentation (.SYNOPSIS, .DESCRIPTION, .PARAMETER, .EXAMPLE)
- ✅ Error handling with both throwing (-ThrowOnFailure) and non-throwing modes
- ✅ Cross-platform compatibility (Windows, Linux, macOS)
- ✅ Consistent parameter naming and structure
- ✅ Proper error messages and status output

