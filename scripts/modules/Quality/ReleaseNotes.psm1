<#
.SYNOPSIS
    AI-powered release notes generation using GitHub Copilot CLI.

.DESCRIPTION
    This module provides functions to generate release notes using GitHub Copilot CLI
    by analyzing commit history and changes since the last release.

.NOTES
    Module: Quality.ReleaseNotes
    Version: 1.0.0
#>

<#
.SYNOPSIS
    Generates AI-powered release notes using GitHub Copilot CLI.

.DESCRIPTION
    Analyzes commit history since the last release tag and uses GitHub Copilot CLI
    to generate structured release notes with user-facing improvements and changes.

.PARAMETER Version
    The version for which to generate release notes.

.PARAMETER Type
    The release type: "beta", "stable", or "patch".

.PARAMETER PromptTemplatePath
    Path to the release notes prompt template file.

.PARAMETER CommitRange
    Optional specific commit range to analyze (e.g., "v1.0.0..HEAD").

.PARAMETER MaxCommits
    Maximum number of commits to analyze (default: 50).

.PARAMETER Timeout
    Timeout in seconds for Copilot CLI execution (default: 60).

.PARAMETER ThrowOnFailure
    If true, throws an exception when generation fails.

.EXAMPLE
    $notes = New-AIGeneratedReleaseNotes -Version "1.0.0" -Type "stable" -PromptTemplatePath "./prompt.md"

.OUTPUTS
    String - Generated release notes, or $null if generation fails.
#>
function New-AIGeneratedReleaseNotes {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version,
        
        [Parameter(Mandatory = $true)]
        [ValidateSet("beta", "stable", "patch")]
        [string]$Type,
        
        [Parameter(Mandatory = $true)]
        [string]$PromptTemplatePath,
        
        [string]$CommitRange = "",
        
        [int]$MaxCommits = 50,
        
        [int]$Timeout = 60,
        
        [switch]$ThrowOnFailure
    )
    
    Write-Host "🤖 Generating AI-powered release notes with GitHub Copilot..." -ForegroundColor Cyan
    
    # Check if GitHub Copilot CLI is available
    $copilotAvailable = $null -ne (Get-Command "copilot" -ErrorAction SilentlyContinue)
    
    if (-not $copilotAvailable) {
        $message = "GitHub Copilot CLI is not installed. Install with: npm install -g @github/copilot"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $null
    }
    
    # Determine commit range if not specified
    if (-not $CommitRange) {
        $CommitRange = Get-CommitRangeSinceLastRelease -Version $Version -Type $Type
    }
    
    if (-not $CommitRange) {
        $message = "Could not determine commit range for release notes"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $null
    }
    
    # Get commit log
    Write-Host "   📊 Analyzing commits in range: $CommitRange" -ForegroundColor Green
    
    try {
        $commitLog = git log $CommitRange --pretty=format:"%h - %s" --no-merges
        
        if ([string]::IsNullOrWhiteSpace($commitLog)) {
            Write-Host "   ℹ️  No commits found in range $CommitRange" -ForegroundColor Yellow
            return $null
        }
        
        $commitCount = @($commitLog -split "`n").Count
        Write-Host "   📝 Found $commitCount commits to analyze" -ForegroundColor Green
        
        # Limit commits for performance
        if ($commitCount -gt $MaxCommits) {
            Write-Host "   ⚠️  Too many commits ($commitCount), limiting to last $MaxCommits for performance" -ForegroundColor Yellow
            $commitLog = git log $CommitRange --pretty=format:"%h - %s" --no-merges -$MaxCommits
        }
    }
    catch {
        $message = "Failed to get commit log: $($_.Exception.Message)"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $null
    }
    
    # Load and populate prompt template
    if (-not (Test-Path $PromptTemplatePath)) {
        $message = "Prompt template not found: $PromptTemplatePath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $null
    }
    
    $promptTemplate = Get-Content $PromptTemplatePath -Raw
    $prompt = $promptTemplate -replace '\{\{COMMITS\}\}', $commitLog
    
    # Create temporary file for prompt
    $tempPrompt = Join-Path $env:TEMP "copilot-prompt-$(Get-Random).txt"
    $prompt | Out-File -FilePath $tempPrompt -Encoding UTF8
    
    try {
        Write-Host "   ⏱️  Calling Copilot CLI ($Timeout second timeout)..." -ForegroundColor Gray
        
        # Execute Copilot CLI with timeout
        $job = Start-Job -ScriptBlock {
            param($TempFile)
            $input = Get-Content $TempFile -Raw
            $input | copilot
        } -ArgumentList $tempPrompt
        
        $completed = Wait-Job -Job $job -Timeout $Timeout
        
        if ($completed) {
            $output = Receive-Job -Job $job
            Remove-Job -Job $job
            
            # Clean output
            $releaseNotes = Clean-CopilotOutput -RawOutput $output
            
            Write-Host "   ✅ AI release notes generated successfully" -ForegroundColor Green
            return $releaseNotes
        }
        else {
            Stop-Job -Job $job
            Remove-Job -Job $job
            
            $message = "Copilot CLI timed out after $Timeout seconds"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "✗ $message" -ForegroundColor Red
            return $null
        }
    }
    catch {
        $message = "Failed to generate release notes: $($_.Exception.Message)"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $null
    }
    finally {
        # Clean up temporary file
        if (Test-Path $tempPrompt) {
            Remove-Item -Path $tempPrompt -Force -ErrorAction SilentlyContinue
        }
    }
}

<#
.SYNOPSIS
    Determines the commit range since the last release.

.PARAMETER Version
    The current version.

.PARAMETER Type
    The release type: "beta", "stable", or "patch".

.EXAMPLE
    $range = Get-CommitRangeSinceLastRelease -Version "1.0.0" -Type "stable"

.OUTPUTS
    String - Commit range (e.g., "v0.9.0..HEAD") or "HEAD~10..HEAD" if no previous release found.
#>
function Get-CommitRangeSinceLastRelease {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version,
        
        [Parameter(Mandatory = $true)]
        [ValidateSet("beta", "stable", "patch")]
        [string]$Type
    )
    
    try {
        $tags = git tag --sort=-version:refname
        $currentTag = "v$Version"
        
        # Determine if we're looking for beta or stable releases
        $isBeta = $Version -match "-beta\."
        
        # Filter tags by type and find the most recent one before current
        $previousTag = $null
        foreach ($tag in $tags) {
            if ($tag -eq $currentTag) { continue }  # Skip current tag if it exists
            
            $tagIsBeta = $tag -match "-beta\."
            
            # Match release types (beta with beta, stable with stable)
            if ($isBeta -eq $tagIsBeta) {
                $previousTag = $tag
                break
            }
        }
        
        if (-not $previousTag) {
            Write-Host "   ℹ️  No previous $Type release found - comparing last 10 commits" -ForegroundColor Yellow
            return "HEAD~10..HEAD"
        }
        else {
            Write-Host "   📊 Comparing changes since $previousTag (last $Type release)" -ForegroundColor Green
            return "$previousTag..HEAD"
        }
    }
    catch {
        Write-Host "   ⚠️  Failed to get previous release tag: $($_.Exception.Message)" -ForegroundColor Yellow
        return "HEAD~10..HEAD"  # Fallback
    }
}

<#
.SYNOPSIS
    Cleans Copilot CLI output to remove formatting artifacts.

.PARAMETER RawOutput
    The raw output from Copilot CLI.

.EXAMPLE
    $cleaned = Clean-CopilotOutput -RawOutput $output

.OUTPUTS
    String - Cleaned release notes.
#>
function Clean-CopilotOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RawOutput
    )
    
    # Remove ANSI escape codes
    $cleaned = $RawOutput -replace '\x1b\[[0-9;]*m', ''
    
    # Remove common CLI artifacts
    $cleaned = $cleaned -replace '^\s*copilot>\s*', '', 'Multiline'
    $cleaned = $cleaned -replace '^\s*\|.*?\|?\s*$', '', 'Multiline'
    
    # Trim whitespace
    $cleaned = $cleaned.Trim()
    
    return $cleaned
}

# Export all public functions
Export-ModuleMember -Function @(
    'New-AIGeneratedReleaseNotes',
    'Get-CommitRangeSinceLastRelease',
    'Clean-CopilotOutput'
)
