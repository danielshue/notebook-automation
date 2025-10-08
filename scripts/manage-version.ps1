#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Comprehensive version management for the Notebook Automation project.

.DESCRIPTION
    This script automates the complete version management process for both CLI and
    Obsidian plugin components, ensuring version consistency across package.json, 
    manifest.json, and Git tags for proper BRAT (Beta Reviewer's Auto-update Tool) 
    functionality and release automation.
    
    Features:
    - Unified CLI and plugin version management
    - Cross-platform compatibility (Windows, Linux, macOS)
    - Automatic executable building for all platforms
    - GitHub release creation with asset uploads
    - BRAT compatibility with proper manifest handling
    - Checksum validation and integrity verification
    - Support for beta, stable, and patch releases
    
    In reissue mode (-Reissue), the script performs automatic post-release 
    verification including asset comparison and checksum validation.

.PARAMETER Version
    The version to set (e.g., "0.1.0-beta.1", "0.1.0")

.PARAMETER Type
    The type of version update: "beta", "stable", or "patch"

.PARAMETER CreateRelease
    Whether to create a GitHub release after tagging

.PARAMETER PreRelease
    Whether to mark the GitHub release as a pre-release (for beta versions)

.PARAMETER Reissue
    Recreate a GitHub release for an existing tag using current local dist assets (no version bump).

.PARAMETER ReissueVersion
    The semantic version (without leading 'v') of the existing tag to reissue (e.g. 0.1.0-beta.16)

.PARAMETER SyncOnly
    Only synchronize versions between CLI and plugin components (no releases)

.PARAMETER StatusOnly
    Only show version status across all components

.PARAMETER Detailed
    Show detailed version information (used with -StatusOnly)

.PARAMETER BuildAfterSync
    Build components after synchronization (used with -SyncOnly)

.PARAMETER Help
    Show detailed help and usage examples

.PARAMETER Diagnostic
    Enable extra diagnostic output for troubleshooting

.PARAMETER Quiet
    Minimize output for automation scenarios

.PARAMETER CITimeoutMinutes
    Timeout in minutes when waiting for GitHub Actions CI to complete (default: 45 minutes)

.PARAMETER UseArtifacts
    Download and use CI-built executables from GitHub Actions instead of building locally.
    This ensures proper cross-platform compatibility and native performance.

.PARAMETER ForceLocalBuild
    Force local executable building even when -UseArtifacts is specified

.EXAMPLE
    # Create a new beta release
    .\scripts\manage-version.ps1 -Version "0.1.0-beta.1" -Type "beta" -CreateRelease -PreRelease
    
.EXAMPLE
    # Create a stable release
    .\scripts\manage-version.ps1 -Version "0.1.0" -Type "stable" -CreateRelease

.EXAMPLE
    # Sync versions only (no release)
    .\scripts\manage-version.ps1 -SyncOnly
    
.EXAMPLE
    # Sync versions and build
    .\scripts\manage-version.ps1 -SyncOnly -BuildAfterSync
    
.EXAMPLE
    # Check version status
    .\scripts\manage-version.ps1 -StatusOnly
    
.EXAMPLE
    # Check detailed version status
    .\scripts\manage-version.ps1 -StatusOnly -Detailed

.EXAMPLE
    # Show help and usage examples
    .\scripts\manage-version.ps1 -Help

.NOTES
    DEPENDENCIES:
    - Git (required): Version control operations
    - .NET SDK (required): Building C# CLI components  
    - Node.js (required): Building Obsidian plugin
    - npm (required): Node.js package management
    - GitHub CLI (conditional): Required only for release operations (-CreateRelease or -Reissue)
    
    DIRECTORY:
    - Must be run from the repository root directory
    - Script validates presence of required project files
    
    FEATURES:
    - Cross-platform compatibility (Windows, Linux, macOS)
    - Automatic dependency validation with helpful install prompts
    - Repository directory validation 
    - GitHub CLI authentication checking for release operations
    - Automatically syncs versions between package.json and manifest.json
    - Intelligent path handling for all supported platforms
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$Version,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("beta", "stable", "patch")]
    [string]$Type = 'beta',
    
    [switch]$CreateRelease,
    
    [switch]$PreRelease,

    # If specified, only rebuild CLI executables & validate they embed current version (no version bump / tagging)
    [switch]$RebuildOnly,

    # Recreate an existing release (no tag or version mutation)
    [switch]$Reissue,

    # Version to reissue (semantic part only, tag assumed to be v<version>)
    [string]$ReissueVersion,

    # UTILITY MODES - mutually exclusive with version management
    
    # Only synchronize versions between CLI and plugin (no releases)
    [switch]$SyncOnly,
    
    # Only show version status across all components
    [switch]$StatusOnly,
    
    # Show detailed version information (used with -StatusOnly)
    [switch]$Detailed,
    
    # Build after synchronization (used with -SyncOnly)
    [switch]$BuildAfterSync,
    
    # Show help information
    [switch]$Help,
    
    # Enable extra diagnostic output for troubleshooting  
    [switch]$Diagnostic,
    
    # Minimize output for automation scenarios
    [switch]$Quiet,
    
    # Timeout for waiting for CI completion (minutes)
    [int]$CITimeoutMinutes = 45,
    
    # Use CI-built executables from GitHub Actions instead of building locally
    [switch]$UseArtifacts,
    
    # Force local build even when UseArtifacts is specified
    [switch]$ForceLocalBuild
)

# GLOBAL ERROR HANDLING AND ROLLBACK SYSTEM
# ============================================

# Rollback state tracking
$script:rollbackState = @{
    InitialCommitHash = $null
    Phase             = "Initialization"  # Initialization, PreCommit, PostCommit, Completed
    ModifiedFiles     = @()
    CommitCreated     = $false
    CommitHash        = $null
    TagCreated        = $false
    TagName           = $null
    ReleaseCreated    = $false
    NeedsRollback     = $false
}

# Also initialize global state for Ctrl-C handler access
$global:ManageVersionRollbackState = $script:rollbackState

function Initialize-RollbackSystem {
    """Initialize rollback tracking system"""
    
    Write-ConditionalHost "🔍 Initializing rollback tracking system..." -ForegroundColor Cyan
    
    # Capture current commit hash
    try {
        $script:rollbackState.InitialCommitHash = & git rev-parse HEAD 2>$null
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to get current commit hash"
        }
    }
    catch {
        Write-Host "⚠️  Warning: Could not capture initial commit hash - rollback may be limited" -ForegroundColor Yellow
    }
    
    # Check if workspace is clean
    $currentStatus = & git status --porcelain 2>$null
    if ($currentStatus) {
        Write-Host "⚠️  WARNING: Workspace has uncommitted changes:" -ForegroundColor Yellow
        $currentStatus | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
        Write-Host ""
        
        $response = Read-Host "Continue anyway? These changes may interfere with rollback [y/N]"
        if ($response -ne 'y' -and $response -ne 'Y') {
            Write-Host "❌ Aborted by user" -ForegroundColor Red
            exit 1
        }
    }
    
    $script:rollbackState.Phase = "PreCommit"
    
    # Also set in global scope for Ctrl-C handler access
    $global:ManageVersionRollbackState = $script:rollbackState
    
    Write-ConditionalHost "✅ Rollback system initialized - Phase: PreCommit" -ForegroundColor DarkGreen
}

function Set-RollbackPhase {
    param(
        [ValidateSet("Initialization", "PreCommit", "PostCommit", "Completed")]
        [string]$Phase
    )
    
    $script:rollbackState.Phase = $Phase
    Write-ConditionalHost "📍 Rollback phase: $Phase" -ForegroundColor DarkCyan
}

function Register-ModifiedFile {
    param([string]$FilePath)
    
    if ($FilePath -and $FilePath -notin $script:rollbackState.ModifiedFiles) {
        $script:rollbackState.ModifiedFiles += $FilePath
        $script:rollbackState.NeedsRollback = $true
        Write-ConditionalHost "📝 Registered for rollback: $FilePath" -ForegroundColor DarkGray
    }
}

function Register-CommitCreated {
    param([string]$CommitHash, [string]$TagName = $null)
    
    $script:rollbackState.CommitCreated = $true
    $script:rollbackState.CommitHash = $CommitHash
    $script:rollbackState.NeedsRollback = $true
    
    if ($TagName) {
        $script:rollbackState.TagCreated = $true
        $script:rollbackState.TagName = $TagName
    }
    
    Set-RollbackPhase -Phase "PostCommit"
    Write-ConditionalHost "📍 Commit created: $CommitHash $(if($TagName){"(Tag: $TagName)"})" -ForegroundColor DarkCyan
}

function Register-ReleaseCreated {
    $script:rollbackState.ReleaseCreated = $true
    Write-ConditionalHost "📍 GitHub release created" -ForegroundColor DarkCyan
}

function Clear-RollbackRequirement {
    """Mark that rollback is no longer needed (successful completion)"""
    $script:rollbackState.NeedsRollback = $false
    Set-RollbackPhase -Phase "Completed"
    Write-ConditionalHost "✅ Script completed successfully - rollback not needed" -ForegroundColor DarkGreen
}

function Invoke-PreCommitRollback {
    """Rollback Strategy 1: Uncommitted local changes"""
    
    Write-Host "🔄 STRATEGY 1: Rolling back uncommitted changes..." -ForegroundColor Yellow
    
    try {
        # Check what files are currently modified
        $modifiedFiles = & git status --porcelain 2>$null
        
        if ($modifiedFiles) {
            Write-Host "📋 Uncommitted changes to rollback:" -ForegroundColor Cyan
            $modifiedFiles | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
            
            # Reset all modified tracked files
            Write-Host "🔄 Resetting modified files..." -ForegroundColor Cyan
            & git checkout -- . 2>&1 | Out-Null
            
            # Remove untracked files that were created during script execution
            $untrackedFiles = & git status --porcelain 2>$null | Where-Object { $_.StartsWith("??") }
            if ($untrackedFiles) {
                Write-Host "🗑️  Removing untracked files..." -ForegroundColor Cyan
                $untrackedFiles | ForEach-Object {
                    $file = $_.Substring(3).Trim()
                    if (Test-Path $file) {
                        Remove-Item $file -Force -ErrorAction SilentlyContinue
                        Write-Host "   Removed: $file" -ForegroundColor Gray
                    }
                }
            }
            
            # Verify rollback success
            $remainingChanges = & git status --porcelain 2>$null
            if (-not $remainingChanges) {
                Write-Host "✅ Pre-commit rollback successful - workspace is clean" -ForegroundColor Green
                return $true
            }
            else {
                Write-Host "⚠️  Some changes remain after rollback:" -ForegroundColor Yellow
                $remainingChanges | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
                return $false
            }
        }
        else {
            Write-Host "ℹ️  No uncommitted changes found" -ForegroundColor Gray
            return $true
        }
    }
    catch {
        Write-Host "❌ Error during pre-commit rollback: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Invoke-PostCommitRollback {
    """Rollback Strategy 2: Committed changes and releases"""
    
    Write-Host "🔄 STRATEGY 2: Rolling back committed changes..." -ForegroundColor Yellow
    
    $rollbackSuccess = $true
    
    try {
        # Step 1: Delete GitHub release if created
        if ($script:rollbackState.ReleaseCreated -and $script:rollbackState.TagName) {
            Write-Host "🗑️  Deleting GitHub release..." -ForegroundColor Cyan
            try {
                & gh release delete $script:rollbackState.TagName --yes 2>&1 | Out-Null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "   ✅ GitHub release deleted" -ForegroundColor Green
                }
                else {
                    Write-Host "   ⚠️  Could not delete GitHub release (may not exist)" -ForegroundColor Yellow
                }
            }
            catch {
                Write-Host "   ⚠️  Error deleting GitHub release: $($_.Exception.Message)" -ForegroundColor Yellow
                $rollbackSuccess = $false
            }
        }
        
        # Step 2: Delete Git tag if created
        if ($script:rollbackState.TagCreated -and $script:rollbackState.TagName) {
            Write-Host "🗑️  Deleting Git tag..." -ForegroundColor Cyan
            try {
                # Delete local tag
                & git tag -d $script:rollbackState.TagName 2>&1 | Out-Null
                # Delete remote tag
                & git push origin --delete $script:rollbackState.TagName 2>&1 | Out-Null
                Write-Host "   ✅ Git tag deleted (local and remote)" -ForegroundColor Green
            }
            catch {
                Write-Host "   ⚠️  Error deleting Git tag: $($_.Exception.Message)" -ForegroundColor Yellow
                $rollbackSuccess = $false
            }
        }
        
        # Step 3: Reset to previous commit if we created a version bump commit
        if ($script:rollbackState.CommitCreated -and $script:rollbackState.InitialCommitHash) {
            Write-Host "🔄 Resetting to previous commit..." -ForegroundColor Cyan
            try {
                # Reset to the initial commit (hard reset)
                & git reset --hard $script:rollbackState.InitialCommitHash 2>&1 | Out-Null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "   ✅ Reset to commit: $($script:rollbackState.InitialCommitHash.Substring(0,8))" -ForegroundColor Green
                    
                    # Force push to update remote (if we had pushed)
                    $response = Read-Host "   Force push to remote to update origin? [y/N]"
                    if ($response -eq 'y' -or $response -eq 'Y') {
                        & git push --force-with-lease 2>&1 | Out-Null
                        if ($LASTEXITCODE -eq 0) {
                            Write-Host "   ✅ Remote updated" -ForegroundColor Green
                        }
                        else {
                            Write-Host "   ⚠️  Could not update remote - manual push may be needed" -ForegroundColor Yellow
                            $rollbackSuccess = $false
                        }
                    }
                }
                else {
                    Write-Host "   ❌ Failed to reset commit" -ForegroundColor Red
                    $rollbackSuccess = $false
                }
            }
            catch {
                Write-Host "   ❌ Error resetting commit: $($_.Exception.Message)" -ForegroundColor Red
                $rollbackSuccess = $false
            }
        }
        
        return $rollbackSuccess
    }
    catch {
        Write-Host "❌ Error during post-commit rollback: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Invoke-RollbackStrategy {
    param([string]$Reason = "Script failure")
    
    if (-not $script:rollbackState.NeedsRollback) {
        Write-ConditionalHost "ℹ️  No rollback needed" -ForegroundColor Gray
        return
    }
    
    Write-Host ""
    Write-Host "🚨 EXECUTING ROLLBACK: $Reason" -ForegroundColor Red
    Write-Host "================================================" -ForegroundColor Red
    Write-Host "Phase: $($script:rollbackState.Phase)" -ForegroundColor Yellow
    Write-Host ""
    
    $success = $false
    
    switch ($script:rollbackState.Phase) {
        "PreCommit" {
            $success = Invoke-PreCommitRollback
        }
        "PostCommit" {
            # Try both strategies - first post-commit, then pre-commit for any remaining changes
            $postCommitSuccess = Invoke-PostCommitRollback
            $preCommitSuccess = Invoke-PreCommitRollback
            $success = $postCommitSuccess -and $preCommitSuccess
        }
        default {
            Write-Host "⚠️  Unknown phase: $($script:rollbackState.Phase) - attempting pre-commit rollback" -ForegroundColor Yellow
            $success = Invoke-PreCommitRollback
        }
    }
    
    Write-Host ""
    if ($success) {
        Write-Host "✅ ROLLBACK COMPLETED SUCCESSFULLY" -ForegroundColor Green
        Write-Host "   Workspace has been restored to its previous state" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  ROLLBACK PARTIALLY FAILED" -ForegroundColor Yellow
        Write-Host "   Some manual cleanup may be required" -ForegroundColor Yellow
        Write-Host "   Check git status and remote repository state" -ForegroundColor Yellow
    }
    Write-Host ""
}

# SCRIPT-LEVEL ERROR HANDLER
trap {
    Write-Host ""
    Write-Host "💥 UNHANDLED ERROR OCCURRED" -ForegroundColor Red
    Write-Host "=============================" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Location: Line $($_.InvocationInfo.ScriptLineNumber)" -ForegroundColor Red
    Write-Host ""
    
    Invoke-RollbackStrategy -Reason "Unhandled script error"
    
    Write-Host "❌ Script execution failed and rollback attempted" -ForegroundColor Red
    exit 1
}

# CTRL-C INTERRUPT HANDLER
# Store rollback state in global scope for event handler access
$global:ManageVersionRollbackState = $null

Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action {
    Write-Host ""
    Write-Host "⚠️  SCRIPT INTERRUPTED (Ctrl-C)" -ForegroundColor Yellow
    Write-Host "=============================" -ForegroundColor Yellow
    Write-Host "User cancelled script execution" -ForegroundColor Yellow
    Write-Host ""
    
    # Check both script and global scope for rollback state
    $rollbackNeeded = $false
    if ($global:ManageVersionRollbackState -and $global:ManageVersionRollbackState.NeedsRollback) {
        $rollbackNeeded = $true
    }
    elseif ($script:rollbackState -and $script:rollbackState.NeedsRollback) {
        $rollbackNeeded = $true
    }
    
    if ($rollbackNeeded) {
        Write-Host "🔄 Performing rollback due to user cancellation..." -ForegroundColor Yellow
        try {
            # Try to call rollback function if available
            if (Get-Command Invoke-RollbackStrategy -ErrorAction SilentlyContinue) {
                Invoke-RollbackStrategy -Reason "User cancellation (Ctrl-C)"
                Write-Host "✅ Rollback completed successfully" -ForegroundColor Green
            }
            else {
                Write-Host "⚠️  Rollback function not available - manual cleanup may be required" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "❌ Rollback failed: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "⚠️  Manual cleanup may be required" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "ℹ️  No rollback needed - script was cancelled early" -ForegroundColor Gray
    }
    
    exit 130  # Standard exit code for SIGINT
} | Out-Null

#
# HELP AND USAGE - Show help when no meaningful arguments provided
#

function Show-Help {
    Write-Host ""
    Write-Host "📚 Notebook Automation Version Management" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "This script manages versions for both CLI and Obsidian plugin components," -ForegroundColor White
    Write-Host "ensuring consistency across package.json, manifest.json, and Git tags." -ForegroundColor White
    Write-Host "✨ Cross-platform compatible: Windows, Linux, and macOS" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "🔧 DEPENDENCIES REQUIRED:" -ForegroundColor Blue
    Write-Host "   • Git - Version control operations" -ForegroundColor Gray
    Write-Host "   • .NET SDK - Building C# CLI components" -ForegroundColor Gray  
    Write-Host "   • Node.js & npm - Building Obsidian plugin" -ForegroundColor Gray
    Write-Host "   • GitHub CLI - Required only for release operations" -ForegroundColor Gray
    Write-Host ""
    Write-Host "📖 COMMON USAGE EXAMPLES:" -ForegroundColor Blue
    Write-Host ""
    Write-Host "   # Check current version status" -ForegroundColor Yellow
    Write-Host "   .\scripts\manage-version.ps1 -StatusOnly" -ForegroundColor White
    Write-Host ""
    Write-Host "   # Check detailed version status" -ForegroundColor Yellow  
    Write-Host "   .\scripts\manage-version.ps1 -StatusOnly -Detailed" -ForegroundColor White
    Write-Host ""
    Write-Host "   # Synchronize versions between components" -ForegroundColor Yellow
    Write-Host "   .\scripts\manage-version.ps1 -SyncOnly -Version `"0.1.0-beta.18`"" -ForegroundColor White
    Write-Host ""
    Write-Host "   # Create a new beta version with GitHub release" -ForegroundColor Yellow
    Write-Host "   .\scripts\manage-version.ps1 -Version `"0.1.0-beta.18`" -Type beta -CreateRelease -PreRelease" -ForegroundColor White
    Write-Host ""
    Write-Host "   # Create a stable release" -ForegroundColor Yellow
    Write-Host "   .\scripts\manage-version.ps1 -Version `"0.1.0`" -Type stable -CreateRelease" -ForegroundColor White
    Write-Host ""
    Write-Host "   # Rebuild CLI executables for current version (no version bump)" -ForegroundColor Yellow
    Write-Host "   .\scripts\manage-version.ps1 -RebuildOnly" -ForegroundColor White
    Write-Host ""
    Write-Host "   # Create a release using CI-built executables (recommended)" -ForegroundColor Yellow
    Write-Host "   .\scripts\manage-version.ps1 -Version `"0.1.0`" -Type stable -CreateRelease -UseArtifacts" -ForegroundColor White
    Write-Host ""
    Write-Host "   # Reissue an existing GitHub release with current assets" -ForegroundColor Yellow
    Write-Host "   .\scripts\manage-version.ps1 -Reissue -ReissueVersion `"0.1.0-beta.17`"" -ForegroundColor White
    Write-Host ""
    Write-Host "🏷️  VERSION TYPES:" -ForegroundColor Blue
    Write-Host "   • beta    - Development releases (e.g., 0.1.0-beta.1)" -ForegroundColor Gray
    Write-Host "   • stable  - Production releases (e.g., 0.1.0)" -ForegroundColor Gray
    Write-Host "   • patch   - Bug fix releases (e.g., 0.1.1)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "⚙️  UTILITY MODES:" -ForegroundColor Blue
    Write-Host "   • -StatusOnly    - Show version status across all components" -ForegroundColor Gray
    Write-Host "   • -SyncOnly      - Synchronize versions without creating releases" -ForegroundColor Gray  
    Write-Host "   • -RebuildOnly   - Rebuild executables without version changes" -ForegroundColor Gray
    Write-Host "   • -Reissue       - Recreate an existing GitHub release" -ForegroundColor Gray
    Write-Host ""
    Write-Host "� OUTPUT CONTROL:" -ForegroundColor Blue
    Write-Host "   • -Quiet         - Minimize output for automation scenarios" -ForegroundColor Gray
    Write-Host "   • -Diagnostic    - Show extra diagnostic information" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🏗️ BUILD OPTIONS:" -ForegroundColor Blue
    Write-Host "   • -UseArtifacts      - Use CI-built executables (commits changes, waits for CI, downloads)" -ForegroundColor Gray
    Write-Host "   • -ForceLocalBuild   - Force local build even with -UseArtifacts" -ForegroundColor Gray
    Write-Host "   • -CITimeoutMinutes  - CI wait timeout in minutes (default: 45 for cross-platform builds)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "�� TIP: Use -StatusOnly to check current state before making changes!" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "For detailed parameter information, use: Get-Help .\scripts\manage-version.ps1 -Full" -ForegroundColor DarkYellow
    Write-Host ""
}

# Check if no meaningful arguments were provided or help requested - show help
$noArgumentsProvided = (-not $Version -and -not $StatusOnly -and -not $SyncOnly -and -not $RebuildOnly -and -not $Reissue)
if ($noArgumentsProvided -or $Help) {
    Show-Help
    exit 0
}

# Set error handling
$ErrorActionPreference = "Stop"

#
# CROSS-PLATFORM COMPATIBILITY - Ensure platform detection works across PowerShell versions
#

# Define platform detection variables for compatibility with older PowerShell versions
if (-not (Test-Path variable:IsWindows)) {
    try {
        $script:IsWindows = ([System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT)
    }
    catch {
        # Fallback for very old PowerShell versions
        $script:IsWindows = ($env:OS -eq "Windows_NT")
    }
}
if (-not (Test-Path variable:IsLinux)) {
    try {
        $script:IsLinux = ([System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Unix) -and 
        (-not [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX))
    }
    catch {
        # Fallback detection
        $script:IsLinux = (-not $IsWindows -and -not $IsMacOS -and (Test-Path "/proc/version"))
    }
}
if (-not (Test-Path variable:IsMacOS)) {
    try {
        $script:IsMacOS = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)
    }
    catch {
        # Fallback detection  
        $script:IsMacOS = (-not $IsWindows -and (Test-Path "/System/Library/CoreServices/SystemVersion.plist"))
    }
}# Helper function for cross-platform path construction
function Join-CrossPlatformPath {
    param([string[]]$PathParts)
    
    $result = $PathParts[0]
    for ($i = 1; $i -lt $PathParts.Length; $i++) {
        $result = Join-Path $result $PathParts[$i]
    }
    return $result
}

# Helper function for making files executable on Unix systems
function Set-ExecutablePermission {
    param([string]$FilePath)
    
    if (-not $IsWindows) {
        try {
            Write-VerboseHost "Setting executable permission for $FilePath"
            if (Get-Command "chmod" -ErrorAction SilentlyContinue) {
                chmod +x $FilePath 2>$null
            }
        }
        catch {
            Write-VerboseHost "Warning: Could not set executable permission for $FilePath"
        }
    }
}

# Function to wait for GitHub Actions workflows to complete
function Wait-GitHubActionsComplete {
    param(
        [string]$CommitSha,
        [string]$ExpectedVersion,
        [int]$TimeoutMinutes = 45,  # Increased from 20 to 45 minutes for cross-platform builds
        [int]$PollIntervalSeconds = 15  # Poll every 15 seconds for fast build completion detection
    )
    
    $shortSha = $CommitSha.Substring(0, 8)
    Write-ConditionalHost "⏳ Waiting for GitHub Actions to complete for version $ExpectedVersion (commit $shortSha)..." -ForegroundColor Yellow
    Write-ConditionalHost "   This ensures CI builds executables with the correct version before download" -ForegroundColor Gray
    Write-ConditionalHost "   Timeout: $TimeoutMinutes minutes, checking every $([Math]::Round($PollIntervalSeconds/60.0, 1)) minutes" -ForegroundColor Gray
    
    $timeoutTime = (Get-Date).AddMinutes($TimeoutMinutes)
    $workflowsCompleted = $false
    $initialWaitTime = 15  # Reduced from 60 to 15 seconds - workflows often start quickly
    
    Write-ConditionalHost "⏸️  Initial wait of $initialWaitTime seconds for workflows to start..." -ForegroundColor Gray
    Start-Sleep -Seconds $initialWaitTime
    
    # Do an immediate check first - workflows might already be running or completed
    Write-ConditionalHost "🔍 Performing initial workflow check..." -ForegroundColor Gray

    # Helper to run gh run list and return parsed JSON or throw with stderr
    function Invoke-GhRunListJson {
        param(
            [int]$Limit = 20,
            [string[]]$Fields = @('status', 'conclusion', 'name', 'headSha')
        )

        $fieldsArg = ($Fields -join ',')
        $cmd = @('run', 'list', '--json', $fieldsArg, '--limit', $Limit)
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = (Get-Command gh).Source
        $psi.Arguments = $cmd -join ' '
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true

        $proc = New-Object System.Diagnostics.Process
        $proc.StartInfo = $psi
        $proc.Start() | Out-Null
        $stdout = $proc.StandardOutput.ReadToEnd()
        $stderr = $proc.StandardError.ReadToEnd()
        $proc.WaitForExit()

        if ($proc.ExitCode -ne 0) {
            # Pass along stderr for debugging
            throw "gh run list failed (exit $($proc.ExitCode)): $stderr"
        }

        if ($stdout.Trim() -eq '') { return @() }
        try {
            return $stdout | ConvertFrom-Json
        }
        catch {
            throw "Failed to parse gh output: $($_.Exception.Message). Raw output: $stdout`nStdErr: $stderr"
        }
    }
    
    # Quick check to see if workflows are already running or completed
    try {
        Write-ConditionalHost "   Running: gh run list --json status,conclusion,name,headSha --limit 20" -ForegroundColor DarkGray
        $allQuickWorkflows = Invoke-GhRunListJson -Limit 20 -Fields @('status', 'conclusion', 'name', 'headSha')
        $quickWorkflows = $allQuickWorkflows | Where-Object { $_.headSha -eq $CommitSha }
        Write-ConditionalHost "   ✅ GitHub CLI call successful, found $($quickWorkflows.Count) workflows for commit" -ForegroundColor DarkGray
        if ($quickWorkflows.Count -gt 0) {
            $quickCompleted = $quickWorkflows | Where-Object { $_.status -eq "completed" }
            $quickInProgress = $quickWorkflows | Where-Object { $_.status -eq "in_progress" -or $_.status -eq "queued" }
            Write-ConditionalHost "📋 Quick check: Found $($quickWorkflows.Count) workflow(s) - $($quickCompleted.Count) completed, $($quickInProgress.Count) in progress" -ForegroundColor Cyan
        }
        else {
            Write-ConditionalHost "📋 Quick check: No workflows found yet for commit $shortSha, will start polling..." -ForegroundColor Yellow
        }
    }
    catch {
        Write-ConditionalHost "⚠️  Quick check failed, proceeding with normal polling..." -ForegroundColor Yellow
        Write-ConditionalHost "   Error: $($_.Exception.Message)" -ForegroundColor Gray
    }
    
    while ((Get-Date) -lt $timeoutTime -and -not $workflowsCompleted) {
        $currentTime = Get-Date -Format "HH:mm:ss"
        Write-ConditionalHost "🔍 [$currentTime] Checking workflows for commit $shortSha..." -ForegroundColor Gray
        
        try {
            # Get workflow runs for the commit (use general list and filter, as --commit can be unreliable)
            Write-ConditionalHost "   Running: gh run list --json status,conclusion,name,url,headSha --limit 50" -ForegroundColor DarkGray
            $allWorkflows = Invoke-GhRunListJson -Limit 50 -Fields @('status', 'conclusion', 'name', 'url', 'headSha')
            if ($null -ne $allWorkflows) {
                # ensure $allWorkflows is an array
                if ($allWorkflows -isnot [System.Array]) { $allWorkflows = @($allWorkflows) }
                # Filter to workflows for our specific commit
                $workflows = $allWorkflows | Where-Object { $_.headSha -eq $CommitSha }
                Write-ConditionalHost "   ✅ Found $($allWorkflows.Count) total workflows, $($workflows.Count) for commit $shortSha" -ForegroundColor DarkGray
                
                if ($workflows.Count -eq 0) {
                    Write-ConditionalHost "⏳ No workflows found yet for commit $shortSha, waiting..." -ForegroundColor Yellow
                }
                else {
                    $inProgress = $workflows | Where-Object { $_.status -eq "in_progress" -or $_.status -eq "queued" }
                    $failed = $workflows | Where-Object { $_.conclusion -eq "failure" -or $_.conclusion -eq "cancelled" }
                    $completed = $workflows | Where-Object { $_.status -eq "completed" -and $_.conclusion -eq "success" }
                    $skipped = $workflows | Where-Object { $_.status -eq "completed" -and $_.conclusion -eq "skipped" }
                    $allCompleted = $workflows | Where-Object { $_.status -eq "completed" }
                    
                    $timestamp = Get-Date -Format "HH:mm:ss"
                    Write-ConditionalHost "📊 [$timestamp] Workflow Status - Total: $($workflows.Count), Completed: $($allCompleted.Count), Success: $($completed.Count), Skipped: $($skipped.Count), In Progress: $($inProgress.Count), Failed: $($failed.Count)" -ForegroundColor Cyan
                    
                    # Show workflow details (always show during polling)
                    $workflows | ForEach-Object {
                        $status = if ($_.status -eq "completed") { "✅ $($_.conclusion)" } else { "⏳ $($_.status)" }
                        Write-ConditionalHost "   $($_.name): $status" -ForegroundColor Gray
                    }
                    
                    # Debug: Show detailed workflow categorization
                    Write-ConditionalHost "   📋 Workflow breakdown:" -ForegroundColor DarkGray
                    Write-ConditionalHost "      • In Progress/Queued: $($inProgress.Count)" -ForegroundColor DarkGray
                    Write-ConditionalHost "      • Completed+Success: $($completed.Count)" -ForegroundColor DarkGray  
                    Write-ConditionalHost "      • Completed+Skipped: $($skipped.Count)" -ForegroundColor DarkGray
                    Write-ConditionalHost "      • Failed/Cancelled: $($failed.Count)" -ForegroundColor DarkGray
                    
                    # Additional debug info when not quiet
                    if (-not $Quiet) {
                        Write-VerboseHost "Debug: Total workflows found for commit: $($workflows.Count)"
                        Write-VerboseHost "Debug: Workflows by conclusion: Success=$($completed.Count), Skipped=$($skipped.Count), Failed=$($failed.Count), InProgress=$($inProgress.Count)"
                    }
                    
                    if ($failed.Count -gt 0) {
                        $failedNames = ($failed | ForEach-Object { $_.name }) -join ", "
                        Write-Host "❌ Failed workflows:" -ForegroundColor Red
                        $failed | ForEach-Object {
                            Write-Host "   $($_.name): $($_.url)" -ForegroundColor Red
                        }
                        throw "GitHub Actions workflows failed: $failedNames"
                    }
                    
                    # Check if all workflows are completed (regardless of count)
                    if ($workflows.Count -gt 0 -and $inProgress.Count -eq 0) {
                        # All workflows finished - check if they're successful
                        if ($failed.Count -eq 0) {
                            # Success if we have successful or skipped workflows (no failures)
                            if ($completed.Count -gt 0 -or $skipped.Count -gt 0) {
                                Write-ConditionalHost "✅ All GitHub Actions workflows completed successfully!" -ForegroundColor Green
                                Write-ConditionalHost "   Successful workflows: $($completed.Count), Skipped: $($skipped.Count)" -ForegroundColor Green
                                $workflowsCompleted = $true
                                break
                            }
                            else {
                                Write-Host "⚠️  All workflows completed but none were successful or skipped" -ForegroundColor Yellow
                                Write-Host "   This is unusual - check workflow status manually" -ForegroundColor Yellow
                            }
                        }
                        else {
                            Write-Host "⚠️  Workflows completed but some failed" -ForegroundColor Yellow
                            Write-Host "   Total completed: $($allCompleted.Count), Successful: $($completed.Count), Failed: $($failed.Count)" -ForegroundColor Yellow
                        }
                    }
                    
                    if ($inProgress.Count -gt 0) {
                        $inProgressNames = ($inProgress | ForEach-Object { $_.name }) -join ", "
                        Write-ConditionalHost "⏳ Still waiting for: $inProgressNames" -ForegroundColor Yellow
                    }
                }
            }
            else {
                Write-ConditionalHost "⚠️  GitHub CLI command failed (exit code: $LASTEXITCODE), retrying..." -ForegroundColor Yellow
                Write-ConditionalHost "   Command: gh run list --json status,conclusion,name,url,headSha --limit 50" -ForegroundColor Gray
            }
        }
        catch {
            Write-ConditionalHost "❌ Error checking workflow status: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        if (-not $workflowsCompleted) {
            $elapsed = [Math]::Round(((Get-Date) - (Get-Date).AddMinutes(-$TimeoutMinutes + (($timeoutTime - (Get-Date)).TotalMinutes))).TotalMinutes, 1)
            $remaining = [Math]::Round(($timeoutTime - (Get-Date)).TotalMinutes, 1)
            Write-ConditionalHost "⏳ Sleeping $PollIntervalSeconds seconds before next check... (Elapsed: ${elapsed}m, Remaining: ${remaining}m)" -ForegroundColor Gray
            
            # Interruptible sleep - break into smaller chunks to allow Ctrl-C detection
            $sleepChunks = [Math]::Max(1, [Math]::Floor($PollIntervalSeconds / 3))
            for ($i = 0; $i -lt 3; $i++) {
                Start-Sleep -Seconds $sleepChunks
            }
            # Sleep any remainder
            $remainder = $PollIntervalSeconds - ($sleepChunks * 3)
            if ($remainder -gt 0) {
                Start-Sleep -Seconds $remainder
            }
        }
    }
    
    if (-not $workflowsCompleted) {
        throw "Timeout: GitHub Actions workflows did not complete within $TimeoutMinutes minutes. The builds may still be running - check GitHub Actions manually. Consider using -CITimeoutMinutes to increase the timeout for complex cross-platform builds."
    }
    
    Write-ConditionalHost "✅ GitHub Actions monitoring completed successfully" -ForegroundColor Green
}


# Function to commit and push version changes, then wait for CI
function Invoke-CommitAndWaitForCI {
    param(
        [string]$Version,
        [string]$Type,
        [string]$PackageJsonPath,
        [string]$ManifestJsonPath, 
        [string]$VersionConstantsPath,
        [string]$ScriptPath
    )
    
    Write-ConditionalHost "📝 Committing version changes and waiting for CI..." -ForegroundColor Cyan
    
    # Create commit message
    $commitMessage = switch ($Type) {
        "beta" { "feat: prepare v$Version for BRAT beta testing" }
        "stable" { "release: v$Version stable release" }
        "patch" { "fix: patch release v$Version" }
        default { "chore: version bump to v$Version" }
    }
    
    # Add and commit files
    $packageLockPath = Join-Path $PluginDir "package-lock.json"
    $rootManifestPath = Join-Path $RepoRoot "manifest.json"
    git add -- $PackageJsonPath $ManifestJsonPath $VersionConstantsPath $ScriptPath $packageLockPath $rootManifestPath
    
    # Check if there are changes to commit
    $changes = git diff --cached --name-only
    $needsPush = $true
    
    if (-not $changes) {
        Write-ConditionalHost "ℹ️  No changes to commit - version may already be set. Checking if CI build is needed..." -ForegroundColor Yellow
        
        # Check if there's already a tag for this version
        $tagExists = git tag -l "v$Version"
        if ($tagExists) {
            Write-ConditionalHost "⚠️  Tag v$Version already exists. Skipping CI build." -ForegroundColor Yellow
            $needsPush = $false
        }
        else {
            Write-ConditionalHost "✅ Tag v$Version doesn't exist yet. Creating empty commit to trigger CI..." -ForegroundColor Green
            git commit --allow-empty -m "$commitMessage (trigger CI build)"
        }
    }
    else {
        git commit -m $commitMessage
    }
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to commit version changes"
    }
    
    # Get the commit SHA and register for rollback tracking
    $commitSha = git rev-parse HEAD
    Write-VerboseHost "Committed with SHA: $commitSha"
    Register-CommitCreated -CommitHash $commitSha
    
    # Push to trigger CI (only if needed)
    if ($needsPush) {
        Write-ConditionalHost "📤 Pushing to origin to trigger CI build..." -ForegroundColor Yellow
        git push origin HEAD
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to push changes to origin"
        }
        
        # Wait for CI to complete with correct version
        Wait-GitHubActionsComplete -CommitSha $commitSha -ExpectedVersion $Version -TimeoutMinutes $CITimeoutMinutes
    }
    else {
        Write-ConditionalHost "ℹ️  Skipping CI build - no changes and tag already exists" -ForegroundColor Yellow
    }
    
    return $commitSha
}

# Function to download CI-built executables from GitHub Actions
function Invoke-ArtifactDownload {
    param(
        [string]$RepoRoot,
        [string]$TargetPath
    )
    
    Write-ConditionalHost "📦 Downloading CI-built executables from GitHub Actions..." -ForegroundColor Cyan
    
    $downloadScript = Join-Path $RepoRoot "scripts" "download-latest-artifact.ps1"
    if (-not (Test-Path $downloadScript)) {
        throw "Artifact download script not found: $downloadScript"
    }
    
    # Target directory for CI artifacts should be RepoRoot/dist (cross-platform)
    $artifactDistPath = Join-Path $RepoRoot "dist"
    
    # Clear the dist directory before downloading (ensures clean state)
    if (Test-Path $artifactDistPath) {
        Write-ConditionalHost "🗑️  Clearing existing dist directory for clean CI artifact download..." -ForegroundColor Yellow
        Remove-Item -Path $artifactDistPath -Recurse -Force
    }
    
    # Ensure target directory exists
    New-Item -ItemType Directory -Path $artifactDistPath -Force | Out-Null
    
    # Run the download script
    try {
        Write-VerboseHost "Running artifact download script: $downloadScript"
        $originalLocation = Get-Location
        Set-Location $RepoRoot
        
        & pwsh -ExecutionPolicy Bypass -File $downloadScript
        if ($LASTEXITCODE -ne 0) {
            throw "Artifact download script failed with exit code $LASTEXITCODE"
        }
        
        # Check for downloaded executables in the dist directory
        $pluginArtifactPath = Join-Path $artifactDistPath "notebook-automation"
        
        # Check both possible locations for executables (cross-platform compatible)
        $sourceExecutablePath = $null
        if (Test-Path $pluginArtifactPath) {
            $executables = Get-ChildItem -Path $pluginArtifactPath -File | Where-Object { $_.Name -like "na-*" }
            if ($executables.Count -gt 0) {
                $sourceExecutablePath = $pluginArtifactPath
                Write-VerboseHost "Found executables in plugin artifact path: $pluginArtifactPath"
            }
        }
        
        if (-not $sourceExecutablePath -and (Test-Path $artifactDistPath)) {
            $executables = Get-ChildItem -Path $artifactDistPath -File | Where-Object { $_.Name -like "na-*" }
            if ($executables.Count -gt 0) {
                $sourceExecutablePath = $artifactDistPath
                Write-VerboseHost "Found executables in dist path: $artifactDistPath"
            }
        }
        
        if (-not $sourceExecutablePath) {
            # Use cross-platform compatible path display
            $expectedPath1 = $pluginArtifactPath -replace '\\', '/'
            $expectedPath2 = $artifactDistPath -replace '\\', '/'
            throw "No executables found in downloaded artifacts. Expected location: $expectedPath1 or $expectedPath2"
        }
        
        # Copy executables to target location (only if different from source)
        $executables = Get-ChildItem -Path $sourceExecutablePath -File | Where-Object { $_.Name -like "na-*" }
        Write-ConditionalHost "✅ Found $($executables.Count) executables in CI artifacts" -ForegroundColor Green
        
        # Check if source and target are the same directory
        $normalizedSource = [System.IO.Path]::GetFullPath($sourceExecutablePath)
        $normalizedTarget = [System.IO.Path]::GetFullPath($TargetPath)
        
        if ($normalizedSource -eq $normalizedTarget) {
            Write-ConditionalHost "✅ Executables already in target location - no copy needed" -ForegroundColor Green
            # Still need to set executable permissions for Unix systems
            foreach ($exe in $executables) {
                Set-ExecutablePermission -FilePath $exe.FullName
            }
        }
        else {
            Write-ConditionalHost "📋 Copying executables from $sourceExecutablePath to $TargetPath" -ForegroundColor Cyan
            foreach ($exe in $executables) {
                $targetFile = Join-Path $TargetPath $exe.Name
                Copy-Item $exe.FullName $targetFile -Force
                
                # Set executable permissions for Unix systems
                Set-ExecutablePermission -FilePath $targetFile
                
                Write-VerboseHost "Copied $($exe.Name) to $targetFile"
            }
        }
        
        Write-ConditionalHost "✅ Successfully downloaded and installed CI-built executables" -ForegroundColor Green
        return $true
    }
    catch {
        Write-ConditionalHost "❌ Failed to download CI artifacts: $($_.Exception.Message)" -ForegroundColor Red -Force
        return $false
    }
    finally {
        if ($originalLocation) {
            Set-Location $originalLocation
        }
    }
}

# Define paths
$RepoRoot = Get-Location
$PluginDir = Join-CrossPlatformPath @($RepoRoot, "src", "obsidian-plugin")
$PackageJsonPath = Join-Path $PluginDir "package.json"
$ManifestJsonPath = Join-Path $PluginDir "manifest.json"

#
# HELPER FUNCTIONS - Eliminate redundancy
#

function Get-VersionData {
    param([string]$Type = "all")
    
    $data = @{
        ManifestExists  = Test-Path $ManifestJsonPath
        PackageExists   = Test-Path $PackageJsonPath
        ManifestData    = $null
        PackageData     = $null
        ManifestVersion = $null
        PackageVersion  = $null
        GitVersion      = $null
    }
    
    if ($data.ManifestExists) {
        $data.ManifestData = Get-Content $ManifestJsonPath | ConvertFrom-Json
        $data.ManifestVersion = $data.ManifestData.version
    }
    
    if ($data.PackageExists) {
        $data.PackageData = Get-Content $PackageJsonPath | ConvertFrom-Json  
        $data.PackageVersion = $data.PackageData.version
    }
    
    # Get Git version if needed
    if ($Type -eq "all" -or $Type -eq "git") {
        $GitVersionPath = Join-Path $RepoRoot "GitVersion.yml"
        if (Test-Path $GitVersionPath) {
            $gitVersionContent = Get-Content $GitVersionPath -Raw
            if ($gitVersionContent -match "next-version:\s*([^\r\n]+)") {
                $data.GitVersion = $matches[1].Trim()
            }
        }
    }
    
    return $data
}

function Write-VersionStatus {
    param(
        [hashtable]$VersionData,
        [switch]$Detailed
    )
    
    Write-Host "📊 Version Status Report" -ForegroundColor Green
    Write-Host "========================" -ForegroundColor Green
    Write-Host ""

    # Plugin versions
    Write-Host "🔌 Plugin Component:" -ForegroundColor Blue
    if ($VersionData.ManifestExists) {
        Write-Host "  manifest.json: $($VersionData.ManifestVersion)" -ForegroundColor White
        if ($Detailed) {
            Write-Host "    minAppVersion: $($VersionData.ManifestData.minAppVersion)" -ForegroundColor Gray
            Write-Host "    id: $($VersionData.ManifestData.id)" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "  manifest.json: ❌ NOT FOUND" -ForegroundColor Red
    }

    if ($VersionData.PackageExists) {
        Write-Host "  package.json: $($VersionData.PackageVersion)" -ForegroundColor White
        if ($Detailed) {
            Write-Host "    name: $($VersionData.PackageData.name)" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "  package.json: ❌ NOT FOUND" -ForegroundColor Red
    }

    # CLI versions
    Write-Host ""
    Write-Host "🛠️  CLI Component:" -ForegroundColor Blue
    if ($VersionData.GitVersion) {
        Write-Host "  GitVersion.yml: $($VersionData.GitVersion)" -ForegroundColor White
    }
    else {
        Write-Host "  GitVersion.yml: ❌ NOT FOUND" -ForegroundColor Red
    }

    # Git tags
    Write-Host ""
    Write-Host "🏷️  Git Tags:" -ForegroundColor Blue
    try {
        $latestTag = git describe --tags --abbrev=0 2>$null
        if ($latestTag) {
            Write-Host "  Latest tag: $latestTag" -ForegroundColor White
        }
        else {
            Write-Host "  Latest tag: ❌ NO TAGS FOUND" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "  Latest tag: ❌ ERROR READING TAGS" -ForegroundColor Red
    }

    # Version alignment check
    Write-Host ""
    Write-Host "🔍 Alignment Check:" -ForegroundColor Blue
    $versions = @()
    if ($VersionData.ManifestVersion) { $versions += $VersionData.ManifestVersion }
    if ($VersionData.PackageVersion) { $versions += $VersionData.PackageVersion }

    $uniqueVersions = $versions | Sort-Object -Unique
    if ($uniqueVersions.Count -eq 1) {
        Write-Host "  ✅ All components aligned at version: $($uniqueVersions[0])" -ForegroundColor Green
    }
    else {
        Write-Host "  ⚠️  Version mismatch detected!" -ForegroundColor Yellow
        $versions | ForEach-Object { Write-Host "     - $_" -ForegroundColor Yellow }
    }
}

# Helper function for conditional output
function Write-ConditionalHost {
    param(
        [string]$Message,
        [string]$ForegroundColor = "White",
        [switch]$Force
    )
    
    if (-not $Quiet -or $Force) {
        Write-Host $Message -ForegroundColor $ForegroundColor
    }
}

function Write-VerboseHost {
    param(
        [string]$Message,
        [string]$ForegroundColor = "DarkGray"
    )
    
    if ($Diagnostic -and -not $Quiet) {
        Write-Host "[DIAG] $Message" -ForegroundColor $ForegroundColor
    }
}

#
# DEPENDENCY VALIDATION - Check required tools and directory
#

function Test-Dependency {
    param(
        [string]$CommandName,
        [string]$DisplayName,
        [string]$InstallPrompt,
        [bool]$Required = $true
    )
    
    try {
        $command = Get-Command $CommandName -ErrorAction Stop
        Write-ConditionalHost "✅ $DisplayName found: $($command.Source)" -ForegroundColor Green
        Write-VerboseHost "Dependency $DisplayName validated at $($command.Source)"
        return $true
    }
    catch {
        if ($Required) {
            Write-ConditionalHost "❌ $DisplayName not found in PATH" -ForegroundColor Red
            Write-ConditionalHost "   $InstallPrompt" -ForegroundColor Yellow
            Write-ConditionalHost ""
            
            if (-not $Quiet) {
                $response = Read-Host "Would you like to continue anyway? This may cause the script to fail later (y/N)"
                if ($response -notmatch '^[yY]') {
                    throw "$DisplayName is required but not installed. Install it and try again."
                }
            }
            Write-ConditionalHost "⚠️  Continuing without $DisplayName - expect failures if this tool is needed" -ForegroundColor Yellow
            return $false
        }
        else {
            Write-ConditionalHost "⚠️  $DisplayName not found (optional)" -ForegroundColor Yellow
            return $false
        }
    }
}

function Test-RepositoryDirectory {
    Write-ConditionalHost "📁 Validating repository directory..." -ForegroundColor Cyan
    
    # Check if we're in a git repository
    try {
        git rev-parse --git-dir | Out-Null
        Write-VerboseHost "Git repository validation passed"
    }
    catch {
        throw "❌ Not running in a git repository. Please run this script from the repository root."
    }
    
    # Check for key project files that should exist in the repo root
    $requiredFiles = @(
        "GitVersion.yml",
        (Join-CrossPlatformPath @("src", "obsidian-plugin", "package.json")),
        (Join-CrossPlatformPath @("src", "obsidian-plugin", "manifest.json")),
        (Join-CrossPlatformPath @("src", "c-sharp", "NotebookAutomation.sln"))
    )
    
    $missingFiles = @()
    foreach ($file in $requiredFiles) {
        $fullPath = Join-Path $RepoRoot $file
        if (-not (Test-Path $fullPath)) {
            $missingFiles += $file
            Write-VerboseHost "Missing required file: $file"
        }
        else {
            Write-VerboseHost "Found required file: $file"
        }
    }
    
    if ($missingFiles.Count -gt 0) {
        Write-ConditionalHost "❌ Missing required project files:" -ForegroundColor Red -Force
        $missingFiles | ForEach-Object { Write-ConditionalHost "   - $_" -ForegroundColor Red -Force }
        throw "Please run this script from the repository root directory."
    }
    
    Write-ConditionalHost "✅ Repository directory validation passed" -ForegroundColor Green
}

function Test-AllDependencies {
    Write-Host "🔍 Checking dependencies..." -ForegroundColor Cyan
    Write-Host ""
    
    # Test repository directory first
    Test-RepositoryDirectory
    Write-Host ""
    
    # Core dependencies (always required)
    $gitAvailable = Test-Dependency -CommandName "git" -DisplayName "Git" -InstallPrompt "Install Git from: https://git-scm.com/downloads"
    $dotnetAvailable = Test-Dependency -CommandName "dotnet" -DisplayName ".NET SDK" -InstallPrompt "Install .NET SDK from: https://dotnet.microsoft.com/download"
    $nodeAvailable = Test-Dependency -CommandName "node" -DisplayName "Node.js" -InstallPrompt "Install Node.js from: https://nodejs.org/"
    $npmAvailable = Test-Dependency -CommandName "npm" -DisplayName "npm" -InstallPrompt "npm should be included with Node.js installation"
    
    # Conditional dependencies (only required for certain operations)
    $needsGitHub = $CreateRelease -or $Reissue -or ($UseArtifacts -and -not $ForceLocalBuild)
    $ghAvailable = $true
    if ($needsGitHub) {
        Write-Host ""
        if ($UseArtifacts -and -not $ForceLocalBuild) {
            Write-Host "🤖 CI artifact integration requested - checking GitHub CLI..." -ForegroundColor Cyan
        }
        else {
            Write-Host "🔗 GitHub operations requested - checking GitHub CLI..." -ForegroundColor Cyan
        }
        $ghAvailable = Test-Dependency -CommandName "gh" -DisplayName "GitHub CLI" -InstallPrompt "Install GitHub CLI from: https://cli.github.com/ or run: winget install GitHub.cli"
        
        if ($ghAvailable) {
            # Test GitHub CLI authentication
            try {
                gh auth status 2>&1 | Out-Null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✅ GitHub CLI authenticated" -ForegroundColor Green
                }
                else {
                    Write-Host "⚠️  GitHub CLI not authenticated" -ForegroundColor Yellow
                    Write-Host "   Run: gh auth login" -ForegroundColor Yellow
                    
                    $response = Read-Host "Continue without authentication? Release creation will fail (y/N)"
                    if ($response -notmatch '^[yY]') {
                        throw "GitHub CLI authentication required for release operations"
                    }
                }
            }
            catch {
                Write-Host "⚠️  Could not verify GitHub CLI authentication" -ForegroundColor Yellow
            }
        }
    }
    
    Write-Host ""
    Write-Host "📋 Dependency Summary:" -ForegroundColor Blue
    Write-Host "   Git: $(if($gitAvailable){'✅'}else{'❌'})" -ForegroundColor $(if ($gitAvailable) { 'Green' }else { 'Red' })
    Write-Host "   .NET SDK: $(if($dotnetAvailable){'✅'}else{'❌'})" -ForegroundColor $(if ($dotnetAvailable) { 'Green' }else { 'Red' })
    Write-Host "   Node.js: $(if($nodeAvailable){'✅'}else{'❌'})" -ForegroundColor $(if ($nodeAvailable) { 'Green' }else { 'Red' })
    Write-Host "   npm: $(if($npmAvailable){'✅'}else{'❌'})" -ForegroundColor $(if ($npmAvailable) { 'Green' }else { 'Red' })
    if ($needsGitHub) {
        Write-Host "   GitHub CLI: $(if($ghAvailable){'✅'}else{'❌'})" -ForegroundColor $(if ($ghAvailable) { 'Green' }else { 'Red' })
    }
    
    Write-Host ""
    if ($gitAvailable -and $dotnetAvailable -and $nodeAvailable -and $npmAvailable -and ($ghAvailable -or -not $needsGitHub)) {
        Write-Host "✅ All dependencies satisfied" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  Some dependencies missing - script may fail during execution" -ForegroundColor Yellow
    }
    Write-Host ""
}

# MAIN SCRIPT EXECUTION WITH ROLLBACK PROTECTION
# ===============================================

try {
    # Initialize rollback system
    Initialize-RollbackSystem

    # Run dependency validation before any operations
    Test-AllDependencies

    #
    # VERSION VALIDATION - Validate version format early
    #

    function Test-VersionFormat {
        param([string]$Version)
    
        if ([string]::IsNullOrEmpty($Version)) { return $true } # Allow empty for certain modes
    
        # Semantic version pattern: major.minor.patch[-prerelease][+build]
        $semverPattern = '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$'
    
        if ($Version -notmatch $semverPattern) {
            Write-Host "❌ Invalid version format: $Version" -ForegroundColor Red
            Write-Host "   Expected semantic version format: MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]" -ForegroundColor Yellow
            Write-Host "   Examples:" -ForegroundColor Yellow  
            Write-Host "     • 1.0.0" -ForegroundColor Gray
            Write-Host "     • 1.2.3-beta.1" -ForegroundColor Gray
            Write-Host "     • 2.0.0-alpha.1+build.123" -ForegroundColor Gray
            throw "Invalid version format: $Version"
        }
    
        if (-not $Quiet) {
            Write-Host "✅ Version format validation passed: $Version" -ForegroundColor Green
        }
    }

    # Validate version format if provided
    if ($Version) {
        Test-VersionFormat -Version $Version
    }

    #
    # OUTPUT CONTROL - Set verbosity levels
    #

    # Override PowerShell preference variables based on our parameters
    if ($Diagnostic) {
        $VerbosePreference = "Continue"
        $DebugPreference = "Continue"
        if (-not $Quiet) {
            Write-Host "🔍 Diagnostic mode enabled" -ForegroundColor Cyan
        }
    }

    if ($Quiet) {
        $VerbosePreference = "SilentlyContinue"
        $DebugPreference = "SilentlyContinue" 
        $WarningPreference = "SilentlyContinue"
        # Suppress most output except errors and final results
    }

    # Run dependency validation before any operations
    # Test-AllDependencies  # Remove duplicate call

    #
    # UTILITY MODES - Handle sync and status operations
    #

    if ($StatusOnly) {
        $versionData = Get-VersionData
        Write-VersionStatus -VersionData $versionData -Detailed:$Detailed
        exit 0
    }

    if ($SyncOnly) {
        Write-Host "🔄 Synchronizing CLI and Plugin Versions" -ForegroundColor Green

        # Get target version
        $targetVersion = $Version
        if (-not $targetVersion) {
            $versionData = Get-VersionData
            if ($versionData.ManifestExists) {
                $targetVersion = $versionData.ManifestVersion
                Write-Host "📖 Using version from manifest.json: $targetVersion"
            }
            else {
                throw "No version specified and manifest.json not found. Use -Version parameter."
            }
        }
        else {
            Write-Host "📝 Using specified version: $targetVersion"
        }

        # Update CLI version (GitVersion.yml)
        $GitVersionPath = Join-Path $RepoRoot "GitVersion.yml"
        if (Test-Path $GitVersionPath) {
            $gitVersionContent = Get-Content $GitVersionPath -Raw
            $newGitVersionContent = $gitVersionContent -replace "next-version:\s*[^\r\n]+", "next-version: $targetVersion"
            Set-Content -Path $GitVersionPath -Value $newGitVersionContent -Encoding UTF8
            Write-Host "✅ Updated GitVersion.yml to: $targetVersion"
        }

        # Update plugin versions if not already set
        if (Test-Path $ManifestJsonPath) {
            $manifest = Get-Content $ManifestJsonPath | ConvertFrom-Json
            if ($manifest.version -ne $targetVersion) {
                $manifest.version = $targetVersion
                $manifest | ConvertTo-Json -Depth 5 | Set-Content $ManifestJsonPath -Encoding UTF8
                Write-Host "✅ Updated manifest.json to: $targetVersion"
            }
            else {
                Write-Host "ℹ️  manifest.json already at: $targetVersion"
            }
        }

        if (Test-Path $PackageJsonPath) {
            $packageJson = Get-Content $PackageJsonPath | ConvertFrom-Json
            if ($packageJson.version -ne $targetVersion) {
                $packageJson.version = $targetVersion
                $packageJson | ConvertTo-Json -Depth 5 | Set-Content $PackageJsonPath -Encoding UTF8
                Write-Host "✅ Updated package.json to: $targetVersion"
            }
            else {
                Write-Host "ℹ️  package.json already at: $targetVersion"
            }
        }

        Write-Host "✅ Version synchronization complete!" -ForegroundColor Green

        if ($BuildAfterSync) {
            Write-Host ""
            Write-Host "🔨 Building components after sync..."
        
            # Build CLI
            Write-Host "Building CLI..."
            $solutionPath = Join-CrossPlatformPath @($RepoRoot, "src", "c-sharp", "NotebookAutomation.sln")
            dotnet build $solutionPath --configuration Release
            if ($LASTEXITCODE -ne 0) {
                throw "CLI build failed"
            }

            # Build Plugin
            Write-Host "Building Plugin..."
            Push-Location $PluginDir
            try {
                npm install
                npm run build
                if ($LASTEXITCODE -ne 0) {
                    throw "Plugin build failed"
                }
            }
            finally {
                Pop-Location
            }

            Write-Host "✅ Build complete!" -ForegroundColor Green
        }

        exit 0
    }

    if (-not $Reissue -and $RebuildOnly -and -not $Version) {
        # Infer version from manifest if not provided
        $versionData = Get-VersionData
        if ($versionData.ManifestExists) {
            $Version = $versionData.ManifestVersion
            Write-Host "ℹ️  Inferred current version from manifest: $Version" -ForegroundColor Cyan
        }
        else {
            throw "Cannot infer version (manifest.json missing). Provide -Version explicitly when using -RebuildOnly."
        }
    }

    if (-not $Reissue -and -not $Version) { throw "-Version is required unless -RebuildOnly (inferable) or -Reissue is used." }

    if ($Reissue) {
        if (-not $ReissueVersion) { throw "-ReissueVersion is required when using -Reissue (omit leading 'v')." }
        Write-Host "♻️  Reissuing existing release v$ReissueVersion" -ForegroundColor Green
        Write-Host "🧩 Ensuring completeness (executables, checksums, manifest)" -ForegroundColor Cyan
    }

    if (-not $Reissue) {
        Write-Host ( $RebuildOnly ? "🔧 Rebuilding executables for existing version: $Version" : "🔧 Managing Obsidian Plugin Version: $Version ($Type)" ) -ForegroundColor Green
    }

    # Validation
    if (-not (Test-Path $PluginDir)) {
        throw "Plugin directory not found: $PluginDir"
    }

    if (-not (Test-Path $PackageJsonPath)) {
        throw "package.json not found: $PackageJsonPath"
    }

    if (-not (Test-Path $ManifestJsonPath)) {
        throw "manifest.json not found: $ManifestJsonPath"
    }

    # Repository validation already performed in Test-AllDependencies

    # Check for uncommitted changes (skip prompt for non-interactive reissue to ensure deterministic automation)
    $gitStatus = git status --porcelain
    if ($gitStatus) {
        if ($Reissue) {
            Write-Warning "⚠️  Uncommitted changes present; proceeding with reissue (no version mutation)."
        }
        else {
            Write-Warning "⚠️  Uncommitted changes detected:"
            $gitStatus | ForEach-Object { Write-Warning "   $_" }
            $continue = Read-Host "Continue anyway? (y/N)"
            if ($continue -ne 'y' -and $continue -ne 'Y') { throw "Aborted due to uncommitted changes" }
        }
    }

    # Debugging: Check variable types and values before Join-Path calls
    Write-Host "[DEBUG] RepoRoot: $RepoRoot (Type: $($RepoRoot.GetType().Name))" -ForegroundColor Yellow
    Write-Host "[DEBUG] PluginDir: $PluginDir (Type: $($PluginDir.GetType().Name))" -ForegroundColor Yellow
    Write-Host "[DEBUG] PackageJsonPath: $PackageJsonPath (Type: $($PackageJsonPath.GetType().Name))" -ForegroundColor Yellow
    Write-Host "[DEBUG] ManifestJsonPath: $ManifestJsonPath (Type: $($ManifestJsonPath.GetType().Name))" -ForegroundColor Yellow

    function Invoke-DotnetPublishMatrix {
        param(
            [string]$CliProject,
            [string]$PublishRoot,
            [string]$SemanticVersion
        )

        # Check if we should use CI artifacts instead of local build
        if ($UseArtifacts -and -not $ForceLocalBuild) {
            Write-ConditionalHost "🎯 Using CI-built executables from GitHub Actions (recommended for releases)" -ForegroundColor Green
        
            $success = Invoke-ArtifactDownload -RepoRoot $RepoRoot -TargetPath $PublishRoot
            if ($success) {
                Write-ConditionalHost "✅ CI artifacts successfully integrated" -ForegroundColor Green
                return
            }
            else {
                Write-ConditionalHost "⚠️  CI artifact download failed, falling back to local build" -ForegroundColor Yellow
            }
        }

        # Fall back to local build or if ForceLocalBuild is specified
        Write-ConditionalHost "🧪 Publishing fresh CLI executables for all platforms (local build)" -ForegroundColor $(if ($UseArtifacts) { 'Yellow' } else { 'Green' })
    
        if ($UseArtifacts -and -not $ForceLocalBuild) {
            Write-ConditionalHost "⚠️  WARNING: Using local build instead of CI artifacts may result in platform compatibility issues" -ForegroundColor Yellow
            Write-ConditionalHost "   Consider using -UseArtifacts for production releases to ensure proper cross-platform support" -ForegroundColor Yellow
        }
        if (-not (Test-Path $CliProject)) { throw "CLI project not found at $CliProject" }
        if (-not (Test-Path $PublishRoot)) { New-Item -ItemType Directory -Path $PublishRoot | Out-Null }

        Get-ChildItem -Path $PublishRoot -File -Filter 'na-*' -ErrorAction SilentlyContinue | ForEach-Object { $_ | Remove-Item -Force }

        $targets = @(
            @{ Rid = 'win-x64'; Out = 'na-win-x64.exe'; Ext = '.exe' },
            @{ Rid = 'win-arm64'; Out = 'na-win-arm64.exe'; Ext = '.exe' },
            @{ Rid = 'linux-x64'; Out = 'na-linux-x64'; Ext = '' },
            @{ Rid = 'linux-arm64'; Out = 'na-linux-arm64'; Ext = '' },
            @{ Rid = 'osx-x64'; Out = 'na-macos-x64'; Ext = '' },
            @{ Rid = 'osx-arm64'; Out = 'na-macos-arm64'; Ext = '' }
        )

        foreach ($t in $targets) {
            $rid = $t.Rid; $outName = $t.Out
            $tempOut = Join-Path $PublishRoot "_temp-$rid"
            if (Test-Path $tempOut) { Remove-Item -Recurse -Force $tempOut -ErrorAction SilentlyContinue }
            Write-Host "  • Publishing $rid → $outName" -ForegroundColor Yellow
            $publishArgs = @('publish', $CliProject, '-c', 'Release', '-r', $rid, '/p:PublishSingleFile=true', '/p:SelfContained=true', '--output', $tempOut)
            $pub = & dotnet @publishArgs 2>&1
            if ($LASTEXITCODE -ne 0) { Write-Host $pub -ForegroundColor Red; throw "Publish failed for $rid" }
            $produced = Join-Path $tempOut ("na" + $t.Ext)
            if (-not (Test-Path $produced)) { throw "Expected binary not found: $produced" }
            $finalPath = Join-Path $PublishRoot $outName
            Copy-Item $produced $finalPath -Force
            # Set executable permissions on Unix systems
            Set-ExecutablePermission -FilePath $finalPath
            Write-Host "    ✓ $outName" -ForegroundColor Green
            Remove-Item -Recurse -Force $tempOut -ErrorAction SilentlyContinue
        }

        Write-Host "🔍 Validating semantic version in host executables" -ForegroundColor Green
        $hostExecutables = Get-ChildItem -Path $PublishRoot -File | Where-Object { $_.Name -like 'na-*' -and ( ($IsWindows -and $_.Extension -eq '.exe') -or ($IsLinux -and $_.Name -match 'linux') -or ($IsMacOS -and $_.Name -match 'macos') ) }
        foreach ($exe in $hostExecutables) {
            try {
                $raw = & $exe.FullName --version 2>$null
                if ($LASTEXITCODE -ne 0) { throw "Non-zero exit" }
                $lines = $raw -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -and ($_ -notmatch '^-(version|v)$') }
                $verOutput = ($lines -join ' ')
                if ($verOutput -notmatch [Regex]::Escape($SemanticVersion)) { throw "Semantic version $SemanticVersion not detected in output of $($exe.Name)" }
                Write-Host "    ✓ $($exe.Name) version OK" -ForegroundColor Green
            }
            catch { throw "Version validation failed for $($exe.Name): $($_.Exception.Message)" }
        }
    }

    if ($RebuildOnly) {
        $cliProject = Join-CrossPlatformPath @($RepoRoot, "src", "c-sharp", "NotebookAutomation.Cli", "NotebookAutomation.Cli.csproj")
        $publishRoot = Join-Path $RepoRoot 'dist'
        Invoke-DotnetPublishMatrix -CliProject $cliProject -PublishRoot $publishRoot -SemanticVersion $Version
        Write-Host "✅ Rebuild-only complete." -ForegroundColor Green
        return
    }

    if (-not $Reissue) {
        # Step 1: Update package.json version
        # Check if the specified version is already set in package.json
        $versionData = Get-VersionData
        if ($versionData.PackageVersion -eq $Version) {
            Write-Host "⚠️  Specified version ($Version) is already set in package.json. Skipping version update." -ForegroundColor Yellow
        }
        else {
            Write-Host "📝 Updating package.json version to $Version"
            Push-Location $PluginDir
            try {
                npm version $Version --no-git-tag-version
                if ($LASTEXITCODE -ne 0) { throw "Failed to update package.json version" }
                Register-ModifiedFile -FilePath $PackageJsonPath
            }
            finally { Pop-Location }
        }

        # Step 2: Run version bump script to sync manifest.json
        Write-Host "🔄 Syncing manifest.json with package.json"
        Push-Location $PluginDir
        try {
            npm run version
            if ($LASTEXITCODE -ne 0) { throw "Failed to run version bump script" }
            Register-ModifiedFile -FilePath $ManifestJsonPath
        }
        finally { Pop-Location }

        # Step 3: Verify versions are synchronized
        Write-Host "✅ Verifying version synchronization"
        $versionData = Get-VersionData
        $packageVersion = $versionData.PackageVersion
        $manifestVersion = $versionData.ManifestVersion
        Write-Host "   package.json: $packageVersion"
        Write-Host "   manifest.json: $manifestVersion"
        if ($packageVersion -ne $manifestVersion) { throw "Version mismatch: package.json ($packageVersion) != manifest.json ($manifestVersion)" }
        if ($packageVersion -ne $Version) { throw "Version mismatch: Expected $Version, got $packageVersion" }

        # Step 3b: Update CLI compile-time version constant
        $versionConstantsPath = Join-CrossPlatformPath @($RepoRoot, "src", "c-sharp", "NotebookAutomation.Cli", "VersionConstants.cs")
        if (Test-Path $versionConstantsPath) {
            Write-Host "🧩 Updating VersionConstants.cs (compile-time injection)" -ForegroundColor Green
            $versionConstantsContent = @(
                "// <auto-generated>",
                "//  This file is generated during version bump operations.",
                "//  Do not edit manually; update via version management scripts.",
                "// </auto-generated>",
                "",
                "namespace NotebookAutomation.Cli;",
                "",
                "internal static class VersionConstants",
                "{",
                "    /// <summary>",
                "    /// The current plugin release (semantic) version synchronized with manifest.json.",
                "    /// </summary>",
                "    public const string PluginReleaseVersion = `"$Version`";",
                "}"
            ) -join "`n"
            Set-Content -Path $versionConstantsPath -Value $versionConstantsContent -Encoding UTF8
            git add $versionConstantsPath
            Write-Host "✅ VersionConstants.cs updated" -ForegroundColor Green
        }
        else { Write-Warning "VersionConstants.cs not found at $versionConstantsPath (skipping compile-time constant update)" }

        # Note: CLI executable building happens after commit (Step 6) to ensure CI has correct version
    }

    # Guard: Ensure only expected executable naming (post-publish)
    function Assert-NaExecutableSet {
        param(
            [string]$DistPath,
            [string]$ExpectedVersion
        )

        if (-not (Test-Path $DistPath)) { throw "Dist path not found: $DistPath" }
        $executables = Get-ChildItem -Path $DistPath -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -like 'na-*' }

        $expected = @(
            'na-win-x64.exe', 'na-win-arm64.exe',
            'na-linux-x64', 'na-linux-arm64',
            'na-macos-x64', 'na-macos-arm64'
        )

        $legacy = $executables | Where-Object { $_.Name -like 'na-osx-*' }
        if ($legacy) {
            Write-Host "❌ Legacy osx-named executables detected:" -ForegroundColor Red
            $legacy | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
            throw "Legacy executable names (na-osx-*) present. Aborting."
        }

        # Check for unexpected extras
        $names = $executables.Name
        $unexpected = $names | Where-Object { $_ -notin $expected }
        if ($unexpected) {
            Write-Host "❌ Unexpected executables present:" -ForegroundColor Red
            $unexpected | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
            throw "Unexpected executables found in dist."
        }

        # Ensure all expected exist
        $missing = $expected | Where-Object { $_ -notin $names }
        if ($missing) {
            Write-Host "❌ Missing expected executables:" -ForegroundColor Red
            $missing | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
            throw "One or more expected executables missing."
        }

        # Semantic version validation (best-effort) – only attempt to execute binaries runnable on the current host
        $hostPlatform = if ($IsWindows) { 'windows' } elseif ($IsLinux) { 'linux' } elseif ($IsMacOS) { 'macos' } else { 'unknown' }

        foreach ($exe in $executables) {
            $canRun = switch ($hostPlatform) {
                'windows' { $exe.Extension -eq '.exe' }
                'linux' { $exe.Name -like 'na-linux-*' }
                'macos' { $exe.Name -like 'na-macos-*' }
                default { $false }
            }

            if (-not $canRun) {
                Write-Host "   ↺ Skipping version validation for non-host binary $($exe.Name)" -ForegroundColor DarkYellow
                continue
            }

            try {
                $output = & $exe.FullName --version 2>$null
                if ($LASTEXITCODE -ne 0 -or ([string]::IsNullOrWhiteSpace($output))) { throw "No output" }
                if ($output -notmatch [Regex]::Escape($ExpectedVersion)) { throw "Version string '$ExpectedVersion' not found in output" }
                Write-Host "   ✓ $($exe.Name) version OK" -ForegroundColor Green
            }
            catch {
                Write-Warning "   ⚠️  Version validation warning for $($exe.Name): $($_.Exception.Message)"
            }
        }

        Write-Host "✅ Executable naming & version validation passed" -ForegroundColor Green
    }

    # Note: Executable validation happens after build/download process

    # Step 3c: Generate or validate checksums.json for distributed executables
    function New-OrValidateChecksumsJson {
        param(
            [string]$DistDir,
            [string]$SemanticVersion
        )

        if (-not (Test-Path $DistDir)) { throw "Dist directory not found: $DistDir" }
        $expected = @('na-win-x64.exe', 'na-win-arm64.exe', 'na-linux-x64', 'na-linux-arm64', 'na-macos-x64', 'na-macos-arm64')
        $executables = Get-ChildItem -Path $DistDir -File | Where-Object { $_.Name -in $expected }
        $missing = $expected | Where-Object { $_ -notin $executables.Name }
        if ($missing) { throw "Cannot create checksums.json - missing executables: $($missing -join ', ')" }

        $checksumsPath = Join-Path $DistDir 'checksums.json'
        $algorithm = 'SHA256'
        $hashMap = @{}
        foreach ($exe in $executables) {
            $hash = (Get-FileHash -Algorithm SHA256 -Path $exe.FullName).Hash.ToLowerInvariant()
            $hashMap[$exe.Name] = $hash
        }

        if (Test-Path $checksumsPath) {
            try {
                $existing = Get-Content $checksumsPath -Raw | ConvertFrom-Json
                $existingFiles = $existing.files | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name
                # Validate presence
                foreach ($name in $expected) { if ($name -notin $existingFiles) { throw "checksums.json missing entry for $name" } }
                # Validate hash equality
                foreach ($name in $expected) {
                    $currentHash = $hashMap[$name]
                    $recorded = $existing.files.$name
                    if ($currentHash -ne $recorded) { throw "Checksum mismatch for $name (recorded=$recorded actual=$currentHash)" }
                }
                Write-Host "✅ Existing checksums.json verified" -ForegroundColor Green
                return $checksumsPath
            }
            catch {
                throw "checksums.json validation failed: $($_.Exception.Message)"
            }
        }
        else {
            $payload = [ordered]@{
                version      = $SemanticVersion
                algorithm    = $algorithm
                generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
                files        = $hashMap
            }
            ($payload | ConvertTo-Json -Depth 5) | Set-Content -Path $checksumsPath -Encoding UTF8
            Write-Host "🧾 Generated checksums.json" -ForegroundColor Green
            return $checksumsPath
        }
    }

    if (-not $Reissue) {
        # Checksums and validation happen after executable building
    }

    <#
 Step 4: Build the plugin
 In reissue mode we should NOT rebuild or modify artifacts; we rely on existing dist contents.
 When using UseArtifacts, we skip local plugin build and let CI handle everything.
 This also avoids referencing $checksumsFilePath which is only set in non-reissue flows.
#>
    if (-not $Reissue -and -not ($UseArtifacts -and -not $ForceLocalBuild)) {
        Write-Host "🔨 Building plugin"
        Push-Location $PluginDir
        try {
            npm run build
            if ($LASTEXITCODE -ne 0) { throw "Failed to build plugin" }

            Write-Host "✅ Build completed with executable preservation"
            # Copy manifest.json to repository root for BRAT compatibility
            $repoRootManifest = Join-Path $RepoRoot "manifest.json"
            Copy-Item -Path $ManifestJsonPath -Destination $repoRootManifest -Force
            Register-ModifiedFile -FilePath $repoRootManifest
            Write-Host "✅ Copied manifest.json to repository root for BRAT compatibility"

            # Copy checksums.json into plugin dist & ensure asset-manifest includes it
            if ($checksumsFilePath) {
                $pluginChecksumsTarget = Join-Path $PluginDir 'dist' 'checksums.json'
                if (Test-Path $checksumsFilePath) {
                    Copy-Item $checksumsFilePath $pluginChecksumsTarget -Force
                    Write-Host "✅ Copied checksums.json into plugin dist" -ForegroundColor Green
                }
            }

            $assetManifestPath = Join-Path $PluginDir 'dist' 'asset-manifest.json'
            if ((Test-Path $assetManifestPath) -and $checksumsFilePath) {
                try {
                    $am = Get-Content $assetManifestPath -Raw | ConvertFrom-Json
                    if (-not ($am.files -contains 'checksums.json')) {
                        $am.files += 'checksums.json'
                        ($am | ConvertTo-Json -Depth 5) | Set-Content -Path $assetManifestPath -Encoding UTF8
                        Write-Host "🛠️  Updated asset-manifest.json to include checksums.json" -ForegroundColor Green
                    }
                }
                catch { Write-Warning "Failed to update asset-manifest.json: $($_.Exception.Message)" }
            }
        }
        finally { Pop-Location }
    }
    else {
        Write-Host "↺ Skipping plugin rebuild in reissue mode (using existing dist assets)" -ForegroundColor Yellow
    }

    # Step 5: Verify build artifacts (only for local builds)
    if (-not $Reissue -and -not ($UseArtifacts -and -not $ForceLocalBuild)) {
        $distDir = Join-Path $RepoRoot "dist"
        $distFiles = Get-ChildItem -Path $distDir | Select-Object -ExpandProperty Name
        Write-Host "[DEBUG] Files in dist directory:" -ForegroundColor Yellow
        $distFiles | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
        $buildArtifacts = @(
            Join-Path $distDir "main.js"
            Join-Path $distDir "manifest.json"
            Join-Path $distDir "styles.css"
        )

        foreach ($artifact in $buildArtifacts) {
            if (-not (Test-Path $artifact)) {
                throw "Build artifact missing: $artifact"
            }
        }

        Write-Host "✅ Build artifacts verified"
    }

    # Step 6: Commit changes and handle CI workflow
    if (-not $Reissue) {
        if ($UseArtifacts -and -not $ForceLocalBuild) {
            # Commit changes and wait for CI to build with correct version
            Write-Host "🔄 Using CI artifact workflow: commit → build → download" -ForegroundColor Green
            $commitSha = Invoke-CommitAndWaitForCI -Version $Version -Type $Type -PackageJsonPath $PackageJsonPath -ManifestJsonPath $ManifestJsonPath -VersionConstantsPath $versionConstantsPath -ScriptPath $PSCommandPath
        
            # Download CI-built executables (skip local build entirely)
            Write-Host "📦 Downloading CI-built executables now that build is complete..." -ForegroundColor Yellow
            $distPath = Join-Path $RepoRoot 'dist'
            $success = Invoke-ArtifactDownload -RepoRoot $RepoRoot -TargetPath $distPath
            if (-not $success) {
                throw "Failed to download CI artifacts after waiting for build completion"
            }
        }
        else {
            # Traditional workflow: build locally then commit
            Write-Host "🔨 Building CLI executables locally first..." -ForegroundColor Green
            $cliProjectPath = Join-CrossPlatformPath @($RepoRoot, "src", "c-sharp", "NotebookAutomation.Cli", "NotebookAutomation.Cli.csproj")
            Invoke-DotnetPublishMatrix -CliProject $cliProjectPath -PublishRoot (Join-Path $RepoRoot 'dist') -SemanticVersion $Version
        
            Write-Host "📝 Committing version changes"
            $commitMessage = switch ($Type) {
                "beta" { "feat: prepare v$Version for BRAT beta testing" }
                "stable" { "release: v$Version stable release" }
                "patch" { "fix: patch release v$Version" }
                default { "chore: version bump to v$Version" }
            }
            $packageLockPath = Join-Path $PluginDir "package-lock.json"
            $rootManifestPath = Join-Path $RepoRoot "manifest.json"
            git add -- $PackageJsonPath $ManifestJsonPath $versionConstantsPath scripts/manage-version.ps1 $packageLockPath $rootManifestPath
            git commit -m $commitMessage
            if ($LASTEXITCODE -eq 0) {
                $commitHash = git rev-parse HEAD
                Register-CommitCreated -CommitHash $commitHash
            }
            else {
                throw "Failed to commit version changes"
            }
        }
    
        # Create and push tag
        $tagName = "v$Version"
        Write-Host "🏷️  Creating tag: $tagName"
        git tag $tagName
    
        if ($LASTEXITCODE -eq 0) {
            Register-CommitCreated -CommitHash (git rev-parse HEAD) -TagName $tagName
            git push origin $tagName
        }
        else {
            throw "Failed to create tag"
        }
    
        # Validate executables are now present and correctly built
        Write-Host "✅ Validating built executables..." -ForegroundColor Green
        Assert-NaExecutableSet -DistPath (Join-Path $RepoRoot 'dist') -ExpectedVersion $Version
    
        # Generate checksums now that executables are built
        Write-Host "🧦 Generating checksums for built executables..." -ForegroundColor Green
        $distDirRoot = Join-Path $RepoRoot 'dist'
        $checksumsFilePath = New-OrValidateChecksumsJson -DistDir $distDirRoot -SemanticVersion $Version
    }

    # -------------------- Reissue Mode --------------------
    if ($Reissue) {
        $reTag = "v$ReissueVersion"
        # Validate tag exists
        $tagExists = git show-ref --tags | Select-String -SimpleMatch "$reTag"
        if (-not $tagExists) { throw "Tag $reTag does not exist; cannot reissue." }

        # GitHub CLI dependency already validated in Test-AllDependencies

        $rootDist = Join-Path $RepoRoot 'dist'
        $pluginDist = Join-Path $RepoRoot 'src/obsidian-plugin/dist'
        $manifestCandidates = @(
            Join-Path $rootDist 'asset-manifest.json';
            Join-Path $pluginDist 'asset-manifest.json'
        ) | Where-Object { Test-Path $_ }
        if (-not $manifestCandidates) { throw "No asset-manifest.json found in root or plugin dist." }
        $manifestPath = $manifestCandidates[0]
        $manifestDir = Split-Path $manifestPath -Parent
        Write-Host "📄 Using asset manifest: $manifestPath" -ForegroundColor Green

        # Ensure expected executables & checksums exist before collecting asset list (always on in reissue mode)
        Write-Host "[RC1] Ensuring full executable matrix present" -ForegroundColor Cyan
        $expectedExec = @('na-win-x64.exe', 'na-win-arm64.exe', 'na-linux-x64', 'na-linux-arm64', 'na-macos-x64', 'na-macos-arm64')
        $currentExec = @(Get-ChildItem -Path $rootDist -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -like 'na-*' } | Select-Object -ExpandProperty Name)
        $missingExec = $expectedExec | Where-Object { $_ -notin $currentExec }
        if ($missingExec) {
            Write-Host "[RC2] Missing executables detected: $($missingExec -join ', ') -> publishing" -ForegroundColor Yellow
            $cliProject = Join-Path $RepoRoot "src/c-sharp/NotebookAutomation.Cli/NotebookAutomation.Cli.csproj"
            if (-not (Test-Path $cliProject)) { throw "CLI project not found for completeness publish: $cliProject" }
            $versionData = Get-VersionData
            $semanticVersion = if ($versionData.ManifestExists) { $versionData.ManifestVersion } else { $ReissueVersion }
            Invoke-DotnetPublishMatrix -CliProject $cliProject -PublishRoot $rootDist -SemanticVersion $semanticVersion
        }
        else { Write-Host "[RC2] All expected executables already present." -ForegroundColor Green }

        Write-Host "[RC3] Ensuring checksums.json present & valid" -ForegroundColor Cyan
        try {
            $null = New-OrValidateChecksumsJson -DistDir $rootDist -SemanticVersion $ReissueVersion
            Write-Host "[RC3] checksums.json verified/generated" -ForegroundColor Green
        }
        catch { throw "Completeness checksum step failed: $($_.Exception.Message)" }

        if ($manifestPath -eq (Join-Path $rootDist 'asset-manifest.json')) {
            Write-Host "[RC4] Normalizing root asset-manifest.json" -ForegroundColor Cyan
            try {
                $raw = Get-Content $manifestPath -Raw | ConvertFrom-Json
                if (-not $raw.files) { $raw | Add-Member -NotePropertyName files -NotePropertyValue @() -Force }
                $needAdd = @()
                foreach ($ex in $expectedExec) { if ($raw.files -notcontains $ex) { $needAdd += $ex } }
                if ($raw.files -notcontains 'checksums.json') { $needAdd += 'checksums.json' }
                if ($raw.files -notcontains 'asset-manifest.json') { $needAdd += 'asset-manifest.json' }
                if ($needAdd) {
                    $raw.files += $needAdd
                    ($raw | ConvertTo-Json -Depth 6) | Set-Content -Path $manifestPath -Encoding UTF8
                    Write-Host "[RC4] Added to manifest: $($needAdd -join ', ')" -ForegroundColor Green
                }
                else { Write-Host "[RC4] Manifest already contains required entries" -ForegroundColor Green }
            }
            catch { Write-Warning "[RC4] Failed to normalize asset-manifest.json: $($_.Exception.Message)" }
        }
        Write-Host "[R1] Reading manifest JSON" -ForegroundColor Cyan
        $am = Get-Content $manifestPath -Raw | ConvertFrom-Json
        $assetList = @()
        Write-Host "[R2] Collecting manifest-declared files" -ForegroundColor Cyan
        foreach ($f in $am.files) {
            $fp = Join-Path $manifestDir $f
            if (Test-Path $fp) { $assetList += $fp } else { Write-Warning "Missing file listed in manifest (skipped): $f" }
        }
        Write-Host "[R3] Adding executables from root dist (if not already)" -ForegroundColor Cyan
        $executables = @(Get-ChildItem -Path $rootDist -Filter 'na-*' -File -ErrorAction SilentlyContinue)
        foreach ($exe in $executables) {
            if ($assetList -notcontains $exe.FullName) { $assetList += $exe.FullName }
        }
        Write-Host "[R4] Adding checksums.json & manifest itself if present" -ForegroundColor Cyan
        $checksums = Join-Path $rootDist 'checksums.json'
        if (Test-Path $checksums) { if ($assetList -notcontains $checksums) { $assetList += $checksums } }
        # Always include the asset-manifest.json file itself (for traceability) if we are using the root one or plugin one
        if ($assetList -notcontains $manifestPath) { $assetList += $manifestPath }
        Write-Host "[R5] Final asset list (paths):" -ForegroundColor Cyan
        $assetList | ForEach-Object { Write-Host "   • $_" -ForegroundColor DarkGray }
        Write-Host "🧾 Prepared asset set (${($assetList.Count)}) for reissue" -ForegroundColor Green
        $preFlag = ($ReissueVersion -match '-')
        $ghExe = (Get-Command gh -ErrorAction Stop).Source
        Write-Host "[R6] gh resolved to: $ghExe" -ForegroundColor Cyan
        Write-Host "🗑️  Deleting existing release $reTag" -ForegroundColor Yellow
        try {
            $delOutput = & $ghExe release delete $reTag -y 2>&1
            if ($LASTEXITCODE -ne 0) { Write-Warning "Release delete reported non-zero exit ($LASTEXITCODE). Output: $delOutput" }
        }
        catch { Write-Warning "Exception during delete: $($_.Exception.Message)" }

        Write-Host "🚀 Creating replacement release $reTag" -ForegroundColor Green
        $notes = "Reissued assets for $reTag on $(Get-Date -Format o)"
        # Write notes to temporary file to avoid parameter parsing issues
        $tempNotesFile = Join-Path $env:TEMP "reissue-notes-$(Get-Random).txt"
        $notes | Out-File -FilePath $tempNotesFile -Encoding UTF8
    
        $createArgs = @('release', 'create', $reTag, '--title', $reTag, '--notes-file', $tempNotesFile)
        if ($preFlag) { $createArgs += '--prerelease' }
        $createArgs += $assetList
        Write-Host "[R7] gh create arguments:" -ForegroundColor Yellow
        $createArgs | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
        try {
            $createOutput = & $ghExe @createArgs 2>&1
            if ($LASTEXITCODE -ne 0) { throw "gh release create failed ($LASTEXITCODE): $createOutput" }
        }
        catch {
            Write-Error "Reissue failed: $($_.Exception.Message)"
            Write-Host "TIP: If error mentions a parameter like 'and', run 'Get-Command gh' to ensure no alias, and try invoking with explicit path: & \"$ghExe\" release create ..."
            return
        }
        finally {
            # Clean up temporary file
            if (Test-Path $tempNotesFile) {
                Remove-Item $tempNotesFile -Force
            }
        }
        Write-Host "✅ Reissue complete: https://github.com/danielshue/notebook-automation/releases/tag/$reTag" -ForegroundColor Green
        # Post-release verification (asset presence + checksum integrity)
        Write-Host "[V1] Starting post-release verification" -ForegroundColor Cyan
        $expectedNames = $assetList | ForEach-Object { Split-Path $_ -Leaf } | Sort-Object -Unique
        try {
            $remoteListRaw = & $ghExe release view $reTag --json assets --jq '.assets[].name' 2>$null
            $remoteNames = @()
            if ($remoteListRaw) {
                $remoteNames = ($remoteListRaw -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ }) | Sort-Object -Unique
            }
            else { $remoteNames = @() }
        }
        catch { Write-Warning "[V1] Unable to query remote release assets: $($_.Exception.Message)"; $remoteNames = @() }

        $missingRemote = $expectedNames | Where-Object { $_ -notin $remoteNames }
        $unexpectedRemote = $remoteNames | Where-Object { $_ -notin $expectedNames }
        if ($missingRemote) {
            Write-Warning "[V2] Missing assets on remote release: $($missingRemote -join ', ')"
        }
        else { Write-Host "[V2] All expected assets present on remote release" -ForegroundColor Green }
        if ($unexpectedRemote) {
            Write-Warning "[V2] Unexpected extra assets on remote release: $($unexpectedRemote -join ', ')" 
        }

        # Checksum validation
        $checksumsPath = Join-Path $rootDist 'checksums.json'
        $checksumIssues = @()
        if (Test-Path $checksumsPath) {
            Write-Host "[V3] Validating checksums.json integrity" -ForegroundColor Cyan
            try {
                $checksumsJson = Get-Content $checksumsPath -Raw | ConvertFrom-Json
                $fileProps = $checksumsJson.files | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name
                foreach ($fname in $fileProps) {
                    $localPath = Join-Path $rootDist $fname
                    if (-not (Test-Path $localPath)) { $checksumIssues += "Entry $fname listed but file missing locally"; continue }
                    $actual = (Get-FileHash -Algorithm SHA256 -Path $localPath).Hash.ToLowerInvariant()
                    $recorded = $checksumsJson.files.$fname
                    if ($actual -ne $recorded) { $checksumIssues += "Checksum mismatch for $fname (recorded=$recorded actual=$actual)" }
                }
            }
            catch { $checksumIssues += "Failed to parse/validate checksums.json: $($_.Exception.Message)" }
        }
        else { Write-Warning "[V3] checksums.json not found for verification (expected at $checksumsPath)" }

        if ($checksumIssues.Count -eq 0) { Write-Host "[V4] Checksum validation passed" -ForegroundColor Green } else {
            Write-Warning "[V4] Checksum validation issues:"; $checksumIssues | ForEach-Object { Write-Warning "   - $_" }
        }

        Write-Host "[V5] Post-release verification summary:" -ForegroundColor Cyan
        Write-Host "       Expected assets: $($expectedNames.Count)" -ForegroundColor DarkGray
        Write-Host "       Remote assets:   $($remoteNames.Count)" -ForegroundColor DarkGray
        Write-Host "       Missing:         $($missingRemote.Count)" -ForegroundColor DarkGray
        Write-Host "       Unexpected:      $($unexpectedRemote.Count)" -ForegroundColor DarkGray
        Write-Host "       Checksum issues: $($checksumIssues.Count)" -ForegroundColor DarkGray
        if ($missingRemote -or $checksumIssues) {
            Write-Warning "[V6] Verification completed with warnings (see details above)."
        }
        else { Write-Host "[V6] Verification completed successfully (no issues)" -ForegroundColor Green }
        return
    }

    # Step 8: Create GitHub release if requested
    if ($CreateRelease -and -not $Reissue) {
        Write-Host "🚀 Creating GitHub release"
    
        # GitHub CLI dependency already validated in Test-AllDependencies
    
        # Prepare release assets - plugin files from manifest + executables separately
        $pluginDistDir = Join-Path $RepoRoot "dist"
        $releaseAssets = @()
    
        # Read asset manifest to determine which plugin files to include
        # Note: asset-manifest.json should ONLY contain plugin files (main.js, manifest.json, etc.)
        # Executables are added separately and NOT in the manifest (Obsidian downloads manifest files,
        # but users download executables manually for their platform)
        $assetManifestPath = Join-Path $pluginDistDir "asset-manifest.json"
        if (Test-Path $assetManifestPath) {
            $assetManifest = Get-Content $assetManifestPath | ConvertFrom-Json
        
            Write-Host "   📋 Plugin files from asset-manifest.json: $($assetManifest.files.Count) files"
        
            foreach ($fileName in $assetManifest.files) {
                $filePath = Join-Path $pluginDistDir $fileName
                if (Test-Path $filePath) {
                    $releaseAssets += $filePath
                    Write-Host "   📎 Plugin: $fileName"
                }
                else {
                    Write-Warning "   ⚠️  File listed in manifest but not found: $fileName"
                }
            }
        }
        else {
            throw "Asset manifest not found: $assetManifestPath. Run plugin build first."
        }
    
        # Add executables separately (NOT in asset-manifest.json - these are for manual platform-specific download)
        Write-Host "   📦 Adding platform executables (separate from plugin files)..."
        $expectedExecutables = @(
            'na-win-x64.exe', 'na-win-arm64.exe',
            'na-linux-x64', 'na-linux-arm64',
            'na-macos-x64', 'na-macos-arm64'
        )
        
        $foundExecutables = 0
        foreach ($exeName in $expectedExecutables) {
            $exePath = Join-Path $pluginDistDir $exeName
            if (Test-Path $exePath) {
                $releaseAssets += $exePath
                Write-Host "   📎 Executable: $exeName"
                $foundExecutables++
            }
            else {
                Write-Warning "   ⚠️  Expected executable not found: $exeName"
            }
        }
        
        if ($foundExecutables -eq $expectedExecutables.Count) {
            Write-Host "   ✅ All $foundExecutables executables found"
        }
        else {
            Write-Warning "   ⚠️  Found $foundExecutables of $($expectedExecutables.Count) expected executables"
        }
        
        # Add checksums.json separately (for executable verification)
        $checksumsPath = Join-Path $pluginDistDir "checksums.json"
        if (Test-Path $checksumsPath) {
            $releaseAssets += $checksumsPath
            Write-Host "   📎 Checksums: checksums.json"
        }
        else {
            Write-Warning "   ⚠️  checksums.json not found"
        }
    
        Write-Host "✅ Prepared $($releaseAssets.Count) total release assets ($($assetManifest.files.Count) plugin + $foundExecutables executables + checksums)"
    
        # Create release notes
        $releaseNotes = switch ($Type) {
            "beta" { 
                @"
## Beta Release v$Version

This is a beta release for testing with BRAT (Beta Reviewer's Auto-update Tool).

### Installation via BRAT:
1. Install the BRAT plugin in Obsidian
2. Add this repository: ``danielshue/notebook-automation``
3. BRAT will automatically install and update the plugin

### Changes in this release:
- Beta testing version
- Contains all platform executables
- Ready for BRAT installation

**Note:** This is a pre-release version. Please report any issues on GitHub.
"@
            }
            "stable" { 
                @"
## Stable Release v$Version

This is a stable release of the Notebook Automation plugin.

### Installation:
- Via BRAT: Add repository ``danielshue/notebook-automation``
- Manual: Download and extract to your Obsidian plugins folder

### What's included:
- Plugin files (main.js, manifest.json, styles.css)
- Cross-platform executables for all supported systems
- Ready-to-install package
"@
            }
            "patch" { 
                @"
## Patch Release v$Version

This is a patch release with bug fixes and minor improvements.

### Installation:
- Via BRAT: Will auto-update if you're using BRAT
- Manual: Download and replace your existing installation
"@
            }
        }
    
        # Write release notes to temporary file to avoid parameter parsing issues
        $tempNotesFile = Join-Path $env:TEMP "release-notes-$(Get-Random).md"
        $releaseNotes | Out-File -FilePath $tempNotesFile -Encoding UTF8
    
        # Build gh release command
        $ghArgs = @(
            "release", "create", $tagName,
            "--title", "v$Version",
            "--notes-file", $tempNotesFile
        )
    
        if ($PreRelease) {
            $ghArgs += "--prerelease"
        }
    
        $ghArgs += $releaseAssets
    
        # Execute gh release create
        try {
            & gh @ghArgs
        
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to create GitHub release"
            }
        
            Write-Host "✅ GitHub release created successfully"
            Register-ReleaseCreated
        }
        finally {
            # Clean up temporary file
            if (Test-Path $tempNotesFile) {
                Remove-Item $tempNotesFile -Force
            }
        }
    }

    # Step 9: Summary
    Write-Host ""
    Write-Host "🎉 Version management completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Summary:" -ForegroundColor Yellow
    Write-Host "  Version: $Version" -ForegroundColor White
    Write-Host "  Type: $Type" -ForegroundColor White
    Write-Host "  Tag: $tagName" -ForegroundColor White
    Write-Host "  Release Created: $CreateRelease" -ForegroundColor White
    Write-Host "  Pre-release: $PreRelease" -ForegroundColor White
    Write-Host ""

    if ($Type -eq "beta") {
        Write-Host "Next steps for beta testing:" -ForegroundColor Yellow
        Write-Host "  1. Wait for CI to complete the build process"
        Write-Host "  2. Share the repository URL with beta testers"
        Write-Host "  3. Testers can install via BRAT using: danielshue/notebook-automation"
        Write-Host "  4. Monitor for feedback and issues"
    }
    elseif ($Type -eq "stable") {
        Write-Host "Next steps for stable release:" -ForegroundColor Yellow
        Write-Host "  1. Wait for CI to complete the build process"
        Write-Host "  2. Update documentation with new version"
        Write-Host "  3. Announce the release to users"
        Write-Host "  4. Monitor for any issues"
    }

    Write-Host ""
    Write-Host "GitHub Release: https://github.com/danielshue/notebook-automation/releases/tag/$tagName" -ForegroundColor Cyan

    # Mark successful completion
    Clear-RollbackRequirement

}
catch {
    Write-Host ""
    Write-Host "💥 SCRIPT EXECUTION FAILED" -ForegroundColor Red
    Write-Host "============================" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Location: Line $($_.InvocationInfo.ScriptLineNumber)" -ForegroundColor Red
    Write-Host ""
    
    # Execute appropriate rollback strategy
    Invoke-RollbackStrategy -Reason $_.Exception.Message
    
    Write-Host ""
    Write-Host "❌ Script failed - rollback completed" -ForegroundColor Red
    exit 1
}
