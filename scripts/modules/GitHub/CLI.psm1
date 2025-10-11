<#
.SYNOPSIS
    GitHub CLI wrapper functions module.

.DESCRIPTION
    This module provides wrapper functions for GitHub CLI (gh) operations
    including release management, workflow monitoring, and artifact handling.

.NOTES
    Module: GitHub.CLI
    Version: 1.0.0
#>

<#
.SYNOPSIS
    Executes gh run list and returns parsed JSON or throws with stderr.

.PARAMETER Limit
    Maximum number of runs to return.

.PARAMETER Fields
    Array of field names to include in JSON output.

.PARAMETER ThrowOnFailure
    If true, throws an exception when command fails.

.EXAMPLE
    $runs = Invoke-GhRunList -Limit 20 -Fields @('status', 'conclusion', 'name')

.OUTPUTS
    PSCustomObject - Parsed JSON from gh run list, or $null on failure.
#>
function Invoke-GhRunList {
    param(
        [int]$Limit = 20,
        
        [string[]]$Fields = @('status', 'conclusion', 'name', 'headSha'),
        
        [switch]$ThrowOnFailure
    )
    
    $fieldsJson = $Fields -join ','
    
    try {
        # Use Start-Process for better control and stderr capture
        $proc = Start-Process -FilePath "gh" -ArgumentList @(
            "run", "list",
            "--json", $fieldsJson,
            "--limit", $Limit
        ) -NoNewWindow -Wait -PassThru -RedirectStandardOutput "$env:TEMP/gh-output.json" -RedirectStandardError "$env:TEMP/gh-error.txt"
        
        if ($proc.ExitCode -eq 0) {
            $output = Get-Content "$env:TEMP/gh-output.json" -Raw
            Remove-Item "$env:TEMP/gh-output.json" -ErrorAction SilentlyContinue
            Remove-Item "$env:TEMP/gh-error.txt" -ErrorAction SilentlyContinue
            
            if ($output) {
                return $output | ConvertFrom-Json
            }
            return $null
        }
        else {
            $stderr = Get-Content "$env:TEMP/gh-error.txt" -Raw -ErrorAction SilentlyContinue
            Remove-Item "$env:TEMP/gh-output.json" -ErrorAction SilentlyContinue
            Remove-Item "$env:TEMP/gh-error.txt" -ErrorAction SilentlyContinue
            
            $message = "gh run list failed (exit $($proc.ExitCode)): $stderr"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "✗ $message" -ForegroundColor Red
            return $null
        }
    }
    catch {
        Remove-Item "$env:TEMP/gh-output.json" -ErrorAction SilentlyContinue
        Remove-Item "$env:TEMP/gh-error.txt" -ErrorAction SilentlyContinue
        
        if ($ThrowOnFailure) {
            throw
        }
        Write-Host "✗ gh run list error: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

<#
.SYNOPSIS
    Creates a GitHub release.

.PARAMETER Tag
    Tag name for the release (e.g., "v1.0.0").

.PARAMETER Title
    Title of the release.

.PARAMETER Notes
    Release notes content.

.PARAMETER Draft
    Create as a draft release.

.PARAMETER PreRelease
    Mark as a pre-release.

.PARAMETER Assets
    Array of file paths to upload as release assets.

.PARAMETER ThrowOnFailure
    If true, throws an exception when command fails.

.EXAMPLE
    Invoke-GhReleaseCreate -Tag "v1.0.0" -Title "Release 1.0.0" -Notes "Initial release" -Assets @("./dist/app.exe")

.OUTPUTS
    Boolean - $true if release created successfully, $false otherwise.
#>
function Invoke-GhReleaseCreate {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Tag,
        
        [Parameter(Mandatory = $true)]
        [string]$Title,
        
        [string]$Notes = "",
        
        [switch]$Draft,
        [switch]$PreRelease,
        
        [string[]]$Assets = @(),
        
        [switch]$ThrowOnFailure
    )
    
    $ghArgs = @(
        "release", "create", $Tag,
        "--title", $Title
    )
    
    if ($Notes) {
        $ghArgs += @("--notes", $Notes)
    }
    
    if ($Draft) {
        $ghArgs += "--draft"
    }
    
    if ($PreRelease) {
        $ghArgs += "--prerelease"
    }
    
    # Add assets
    foreach ($asset in $Assets) {
        if (Test-Path $asset) {
            $ghArgs += $asset
        }
        else {
            $message = "Asset file not found: $asset"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "⚠️  $message" -ForegroundColor Yellow
        }
    }
    
    try {
        & gh @ghArgs
        
        if ($LASTEXITCODE -eq 0) {
            return $true
        }
        else {
            $message = "gh release create failed with exit code $LASTEXITCODE"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "✗ $message" -ForegroundColor Red
            return $false
        }
    }
    catch {
        if ($ThrowOnFailure) {
            throw
        }
        Write-Host "✗ gh release create error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

<#
.SYNOPSIS
    Deletes a GitHub release.

.PARAMETER Tag
    Tag name of the release to delete.

.PARAMETER Confirm
    If false, deletes without confirmation prompt.

.PARAMETER ThrowOnFailure
    If true, throws an exception when command fails.

.EXAMPLE
    Invoke-GhReleaseDelete -Tag "v1.0.0" -Confirm:$false

.OUTPUTS
    Boolean - $true if release deleted successfully, $false otherwise.
#>
function Invoke-GhReleaseDelete {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Tag,
        
        [bool]$Confirm = $true,
        
        [switch]$ThrowOnFailure
    )
    
    $ghArgs = @("release", "delete", $Tag)
    
    if (-not $Confirm) {
        $ghArgs += "--yes"
    }
    
    try {
        & gh @ghArgs 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            return $true
        }
        else {
            $message = "gh release delete failed with exit code $LASTEXITCODE"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "✗ $message" -ForegroundColor Red
            return $false
        }
    }
    catch {
        if ($ThrowOnFailure) {
            throw
        }
        Write-Host "✗ gh release delete error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

<#
.SYNOPSIS
    Gets workflow runs for a specific commit SHA.

.PARAMETER CommitSha
    The commit SHA to filter workflows by.

.PARAMETER Limit
    Maximum number of runs to return.

.PARAMETER ThrowOnFailure
    If true, throws an exception when command fails.

.EXAMPLE
    $workflows = Get-WorkflowRunsForCommit -CommitSha "abc123" -Limit 50

.OUTPUTS
    PSCustomObject[] - Array of workflow run objects for the commit.
#>
function Get-WorkflowRunsForCommit {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommitSha,
        
        [int]$Limit = 50,
        
        [switch]$ThrowOnFailure
    )
    
    $allWorkflows = Invoke-GhRunList -Limit $Limit -Fields @('status', 'conclusion', 'name', 'url', 'headSha') -ThrowOnFailure:$ThrowOnFailure
    
    if ($null -eq $allWorkflows) {
        return @()
    }
    
    # Ensure $allWorkflows is an array
    if ($allWorkflows -isnot [System.Array]) {
        $allWorkflows = @($allWorkflows)
    }
    
    # Filter to workflows for the specific commit
    $workflows = $allWorkflows | Where-Object { $_.headSha -eq $CommitSha }
    
    return $workflows
}

<#
.SYNOPSIS
    Waits for GitHub Actions workflows to complete for a specific commit.

.PARAMETER CommitSha
    The commit SHA to monitor workflows for.

.PARAMETER TimeoutMinutes
    Maximum time to wait in minutes. Default is 45.

.PARAMETER PollIntervalSeconds
    Interval between status checks in seconds. Default is 15.

.PARAMETER ThrowOnFailure
    If true, throws an exception when workflows fail or timeout.

.EXAMPLE
    Wait-GitHubActionsComplete -CommitSha "abc123" -TimeoutMinutes 30

.OUTPUTS
    Boolean - $true if all workflows completed successfully, $false otherwise.
#>
function Wait-GitHubActionsComplete {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommitSha,
        
        [int]$TimeoutMinutes = 45,
        [int]$PollIntervalSeconds = 15,
        
        [switch]$ThrowOnFailure
    )
    
    $timeoutTime = (Get-Date).AddMinutes($TimeoutMinutes)
    $workflowsCompleted = $false
    $shortSha = $CommitSha.Substring(0, 8)
    
    Write-Host "⏳ Waiting for GitHub Actions to complete for commit $shortSha..." -ForegroundColor Yellow
    Write-Host "   Timeout: $TimeoutMinutes minutes" -ForegroundColor Gray
    
    while ((Get-Date) -lt $timeoutTime -and -not $workflowsCompleted) {
        $workflows = Get-WorkflowRunsForCommit -CommitSha $CommitSha
        
        if ($workflows.Count -eq 0) {
            Write-Host "⏳ No workflows found yet for commit $shortSha, waiting..." -ForegroundColor Yellow
        }
        else {
            $inProgress = $workflows | Where-Object { $_.status -eq "in_progress" -or $_.status -eq "queued" }
            $failed = $workflows | Where-Object { $_.conclusion -eq "failure" -or $_.conclusion -eq "cancelled" }
            $completed = $workflows | Where-Object { $_.status -eq "completed" -and $_.conclusion -eq "success" }
            
            Write-Host "📊 Workflow Status - Total: $($workflows.Count), Success: $($completed.Count), In Progress: $($inProgress.Count), Failed: $($failed.Count)" -ForegroundColor Cyan
            
            if ($failed.Count -gt 0) {
                $failedNames = ($failed | ForEach-Object { $_.name }) -join ", "
                $message = "GitHub Actions workflows failed: $failedNames"
                
                $failed | ForEach-Object {
                    Write-Host "   ❌ $($_.name): $($_.url)" -ForegroundColor Red
                }
                
                if ($ThrowOnFailure) {
                    throw $message
                }
                Write-Host "✗ $message" -ForegroundColor Red
                return $false
            }
            
            if ($workflows.Count -gt 0 -and $inProgress.Count -eq 0 -and $failed.Count -eq 0) {
                Write-Host "✅ All GitHub Actions workflows completed successfully!" -ForegroundColor Green
                $workflowsCompleted = $true
                break
            }
            
            if ($inProgress.Count -gt 0) {
                $inProgressNames = ($inProgress | ForEach-Object { $_.name }) -join ", "
                Write-Host "⏳ Still waiting for: $inProgressNames" -ForegroundColor Yellow
            }
        }
        
        if (-not $workflowsCompleted) {
            Start-Sleep -Seconds $PollIntervalSeconds
        }
    }
    
    if (-not $workflowsCompleted) {
        $message = "GitHub Actions timeout after $TimeoutMinutes minutes"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    return $true
}

# Export all public functions
Export-ModuleMember -Function @(
    'Invoke-GhRunList',
    'Invoke-GhReleaseCreate',
    'Invoke-GhReleaseDelete',
    'Get-WorkflowRunsForCommit',
    'Wait-GitHubActionsComplete'
)
