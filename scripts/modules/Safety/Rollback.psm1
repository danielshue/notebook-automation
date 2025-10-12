<#
.SYNOPSIS
    Rollback and error recovery functions for version management operations.

.DESCRIPTION
    This module provides rollback tracking and recovery functions for managing
    version control operations, including commit rollback, tag deletion, and
    file restoration.

.NOTES
    Module: Safety.Rollback
    Version: 1.0.0
#>

<#
.SYNOPSIS
    Initializes the rollback tracking system.

.DESCRIPTION
    Sets up the rollback state to track changes that may need to be reverted.
    Captures the initial commit hash and checks for uncommitted changes.

.PARAMETER InitialCommitHash
    Optional initial commit hash. If not provided, will be retrieved from Git.

.PARAMETER CheckUncommittedChanges
    If true, prompts user to continue if there are uncommitted changes.

.EXAMPLE
    $rollbackState = Initialize-RollbackTracking -CheckUncommittedChanges

.OUTPUTS
    Hashtable - Rollback state object with tracking information.
#>
function Initialize-RollbackTracking {
    param(
        [string]$InitialCommitHash = "",
        [switch]$CheckUncommittedChanges
    )
    
    $rollbackState = @{
        InitialCommitHash = ""
        Phase = "PreCommit"
        ModifiedFiles = @()
        CommitCreated = $false
        CommitHash = ""
        TagCreated = $false
        TagName = ""
        ReleaseCreated = $false
        NeedsRollback = $false
    }
    
    # Capture current commit hash
    if ($InitialCommitHash) {
        $rollbackState.InitialCommitHash = $InitialCommitHash
    }
    else {
        try {
            $hash = git rev-parse HEAD 2>$null
            if ($LASTEXITCODE -eq 0) {
                $rollbackState.InitialCommitHash = $hash.Trim()
            }
        }
        catch {
            Write-Host "⚠️  Warning: Could not capture initial commit hash - rollback may be limited" -ForegroundColor Yellow
        }
    }
    
    # Check if workspace is clean
    if ($CheckUncommittedChanges) {
        $status = git status --porcelain 2>$null
        if ($status) {
            Write-Host "⚠️  WARNING: Workspace has uncommitted changes:" -ForegroundColor Yellow
            $status | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
            Write-Host ""
            
            $response = Read-Host "Continue anyway? These changes may interfere with rollback [y/N]"
            if ($response -ne 'y' -and $response -ne 'Y') {
                Write-Host "❌ Aborted by user" -ForegroundColor Red
                throw "Operation cancelled due to uncommitted changes"
            }
        }
    }
    
    Write-Host "✅ Rollback tracking initialized - Phase: PreCommit" -ForegroundColor Green
    return $rollbackState
}

<#
.SYNOPSIS
    Registers a file modification for rollback tracking.

.PARAMETER RollbackState
    The rollback state hashtable.

.PARAMETER FilePath
    Path to the modified file.

.EXAMPLE
    Register-FileModification -RollbackState $state -FilePath "package.json"
#>
function Register-FileModification {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$RollbackState,
        
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )
    
    if ($FilePath -and $FilePath -notin $RollbackState.ModifiedFiles) {
        $RollbackState.ModifiedFiles += $FilePath
        $RollbackState.NeedsRollback = $true
        Write-Host "📝 Registered for rollback: $FilePath" -ForegroundColor DarkGray
    }
}

<#
.SYNOPSIS
    Registers a commit creation for rollback tracking.

.PARAMETER RollbackState
    The rollback state hashtable.

.PARAMETER CommitHash
    The commit hash that was created.

.PARAMETER TagName
    Optional tag name if a tag was also created.

.EXAMPLE
    Register-CommitCreation -RollbackState $state -CommitHash "abc123" -TagName "v1.0.0"
#>
function Register-CommitCreation {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$RollbackState,
        
        [Parameter(Mandatory = $true)]
        [string]$CommitHash,
        
        [string]$TagName = ""
    )
    
    $RollbackState.CommitCreated = $true
    $RollbackState.CommitHash = $CommitHash
    $RollbackState.NeedsRollback = $true
    $RollbackState.Phase = "PostCommit"
    
    if ($TagName) {
        $RollbackState.TagCreated = $true
        $RollbackState.TagName = $TagName
    }
    
    Write-Host "📍 Commit created: $CommitHash $(if($TagName){"(Tag: $TagName)"})" -ForegroundColor Cyan
}

<#
.SYNOPSIS
    Registers a release creation for rollback tracking.

.PARAMETER RollbackState
    The rollback state hashtable.

.EXAMPLE
    Register-ReleaseCreation -RollbackState $state
#>
function Register-ReleaseCreation {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$RollbackState
    )
    
    $RollbackState.ReleaseCreated = $true
    Write-Host "📍 GitHub release created" -ForegroundColor Cyan
}

<#
.SYNOPSIS
    Marks rollback as no longer needed (successful completion).

.PARAMETER RollbackState
    The rollback state hashtable.

.EXAMPLE
    Clear-RollbackRequirement -RollbackState $state
#>
function Clear-RollbackRequirement {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$RollbackState
    )
    
    $RollbackState.NeedsRollback = $false
    $RollbackState.Phase = "Completed"
    Write-Host "✅ Script completed successfully - rollback not needed" -ForegroundColor Green
}

<#
.SYNOPSIS
    Performs pre-commit rollback (restores modified files).

.PARAMETER RollbackState
    The rollback state hashtable.

.PARAMETER ThrowOnFailure
    If true, throws an exception when rollback fails.

.EXAMPLE
    Invoke-PreCommitRollback -RollbackState $state

.OUTPUTS
    Boolean - $true if rollback succeeded, $false otherwise.
#>
function Invoke-PreCommitRollback {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$RollbackState,
        
        [switch]$ThrowOnFailure
    )
    
    Write-Host "🔄 Rollback Strategy: Reverting uncommitted local changes..." -ForegroundColor Yellow
    
    if ($RollbackState.ModifiedFiles.Count -eq 0) {
        Write-Host "   ℹ️  No modified files to rollback" -ForegroundColor Gray
        return $true
    }
    
    $success = $true
    foreach ($file in $RollbackState.ModifiedFiles) {
        try {
            if (Test-Path $file) {
                Write-Host "   ↺ Reverting: $file" -ForegroundColor Gray
                git checkout HEAD -- $file 2>&1 | Out-Null
                
                if ($LASTEXITCODE -ne 0) {
                    Write-Host "   ⚠️  Warning: Could not revert $file" -ForegroundColor Yellow
                    $success = $false
                }
            }
        }
        catch {
            Write-Host "   ❌ Failed to revert $file`: $($_.Exception.Message)" -ForegroundColor Red
            $success = $false
        }
    }
    
    if (-not $success -and $ThrowOnFailure) {
        throw "Pre-commit rollback failed for some files"
    }
    
    return $success
}

<#
.SYNOPSIS
    Performs post-commit rollback (resets commit and deletes tags/releases).

.PARAMETER RollbackState
    The rollback state hashtable.

.PARAMETER ThrowOnFailure
    If true, throws an exception when rollback fails.

.EXAMPLE
    Invoke-PostCommitRollback -RollbackState $state

.OUTPUTS
    Boolean - $true if rollback succeeded, $false otherwise.
#>
function Invoke-PostCommitRollback {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$RollbackState,
        
        [switch]$ThrowOnFailure
    )
    
    Write-Host "🔄 Rollback Strategy: Reverting commit, tag, and release..." -ForegroundColor Yellow
    
    $success = $true
    
    # Delete GitHub release if created
    if ($RollbackState.ReleaseCreated -and $RollbackState.TagName) {
        Write-Host "   🗑️  Deleting GitHub release: $($RollbackState.TagName)" -ForegroundColor Yellow
        try {
            gh release delete $RollbackState.TagName --yes 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "   ✅ Deleted GitHub release" -ForegroundColor Green
            }
            else {
                Write-Host "   ⚠️  Could not delete GitHub release (may not exist)" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "   ⚠️  Error deleting release: $($_.Exception.Message)" -ForegroundColor Yellow
            $success = $false
        }
    }
    
    # Delete Git tag if created
    if ($RollbackState.TagCreated -and $RollbackState.TagName) {
        Write-Host "   🗑️  Deleting Git tag: $($RollbackState.TagName)" -ForegroundColor Yellow
        try {
            # Delete local tag
            git tag -d $RollbackState.TagName 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "   ✅ Deleted local tag" -ForegroundColor Green
            }
            
            # Delete remote tag if it was pushed
            git push origin --delete "refs/tags/$($RollbackState.TagName)" 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "   ✅ Deleted remote tag" -ForegroundColor Green
            }
        }
        catch {
            Write-Host "   ⚠️  Error deleting tag: $($_.Exception.Message)" -ForegroundColor Yellow
            $success = $false
        }
    }
    
    # Reset commit if created
    if ($RollbackState.CommitCreated -and $RollbackState.InitialCommitHash) {
        Write-Host "   ↺ Resetting to commit: $($RollbackState.InitialCommitHash)" -ForegroundColor Yellow
        try {
            git reset --hard $RollbackState.InitialCommitHash 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "   ✅ Reset to initial commit" -ForegroundColor Green
                
                # Force push to remote if needed (WARNING: destructive)
                Write-Host "   ⚠️  Warning: Remote may still have the rolled-back commit" -ForegroundColor Yellow
                Write-Host "   💡 If needed, manually run: git push --force origin HEAD" -ForegroundColor Cyan
            }
            else {
                Write-Host "   ❌ Failed to reset commit" -ForegroundColor Red
                $success = $false
            }
        }
        catch {
            Write-Host "   ❌ Error resetting commit: $($_.Exception.Message)" -ForegroundColor Red
            $success = $false
        }
    }
    
    if (-not $success -and $ThrowOnFailure) {
        throw "Post-commit rollback failed"
    }
    
    return $success
}

<#
.SYNOPSIS
    Executes the appropriate rollback strategy based on current phase.

.PARAMETER RollbackState
    The rollback state hashtable.

.PARAMETER Reason
    Optional reason for the rollback.

.PARAMETER ThrowOnFailure
    If true, throws an exception when rollback fails.

.EXAMPLE
    Invoke-RollbackStrategy -RollbackState $state -Reason "User cancellation"

.OUTPUTS
    Boolean - $true if rollback succeeded, $false otherwise.
#>
function Invoke-RollbackStrategy {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$RollbackState,
        
        [string]$Reason = "",
        
        [switch]$ThrowOnFailure
    )
    
    if (-not $RollbackState.NeedsRollback) {
        Write-Host "ℹ️  No rollback needed" -ForegroundColor Gray
        return $true
    }
    
    Write-Host ""
    Write-Host "🔄 INITIATING ROLLBACK" -ForegroundColor Yellow
    Write-Host "======================" -ForegroundColor Yellow
    if ($Reason) {
        Write-Host "Reason: $Reason" -ForegroundColor Yellow
    }
    Write-Host "Current Phase: $($RollbackState.Phase)" -ForegroundColor Yellow
    Write-Host ""
    
    $success = $false
    
    switch ($RollbackState.Phase) {
        "PreCommit" {
            $success = Invoke-PreCommitRollback -RollbackState $RollbackState -ThrowOnFailure:$ThrowOnFailure
        }
        "PostCommit" {
            # Try both strategies - first post-commit, then pre-commit for remaining changes
            $postSuccess = Invoke-PostCommitRollback -RollbackState $RollbackState
            $preSuccess = Invoke-PreCommitRollback -RollbackState $RollbackState
            $success = $postSuccess -and $preSuccess
            
            if (-not $success -and $ThrowOnFailure) {
                throw "Rollback failed in PostCommit phase"
            }
        }
        default {
            Write-Host "⚠️  Unknown phase: $($RollbackState.Phase) - attempting pre-commit rollback" -ForegroundColor Yellow
            $success = Invoke-PreCommitRollback -RollbackState $RollbackState -ThrowOnFailure:$ThrowOnFailure
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
    
    return $success
}

# Export all public functions
Export-ModuleMember -Function @(
    'Initialize-RollbackTracking',
    'Register-FileModification',
    'Register-CommitCreation',
    'Register-ReleaseCreation',
    'Clear-RollbackRequirement',
    'Invoke-PreCommitRollback',
    'Invoke-PostCommitRollback',
    'Invoke-RollbackStrategy'
)
