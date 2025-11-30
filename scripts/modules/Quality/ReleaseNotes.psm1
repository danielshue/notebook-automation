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
        
        [string]$PreviousVersion = "",
        
        [string]$ChecksumsJsonPath = "",
        
        [int]$MaxCommits = 50,
        
        [int]$Timeout = 60,
        
        [switch]$ThrowOnFailure
    )
    
    Write-Host "🤖 Generating AI-powered release notes with GitHub Copilot..." -ForegroundColor Cyan
    
    # Check if GitHub Copilot CLI is available
    $copilotAvailable = $null -ne (Get-Command "copilot" -ErrorAction SilentlyContinue)
    
    if (-not $copilotAvailable) {
        $message = @"
GitHub Copilot CLI is not installed or not available in PATH.

To install GitHub Copilot CLI:
1. Install via npm: npm install -g @githubnext/github-copilot-cli
2. Or install via GitHub CLI: gh extension install github/gh-copilot
3. Authenticate: gh auth login (if using gh extension)

For more information: https://docs.github.com/en/copilot/github-copilot-in-the-cli
"@
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ GitHub Copilot CLI not available" -ForegroundColor Red
        Write-Host $message -ForegroundColor Yellow
        return $null
    }
    
    # Determine commit range if not specified
    if (-not $CommitRange) {
        $rangeResult = Get-CommitRangeSinceLastRelease -Version $Version -Type $Type
        $CommitRange = $rangeResult.Range
        $PreviousVersion = $rangeResult.PreviousVersion
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
    $prompt = $prompt -replace '\{\{VERSION\}\}', $Version
    $prompt = $prompt -replace '\{\{PREVIOUS_VERSION\}\}', $PreviousVersion
    
    # Add checksum information if available
    if ($ChecksumsJsonPath -and (Test-Path $ChecksumsJsonPath)) {
        try {
            $checksums = Get-Content $ChecksumsJsonPath | ConvertFrom-Json
            $checksumText = "SHA256 Checksums from build:`n"
            foreach ($file in $checksums.files.PSObject.Properties) {
                $checksumText += "- $($file.Name): $($file.Value)`n"
            }
            $prompt = $prompt -replace '\{\{CHECKSUMS\}\}', $checksumText
        }
        catch {
            Write-Host "   ⚠️  Could not read checksums file: $($_.Exception.Message)" -ForegroundColor Yellow
            $prompt = $prompt -replace '\{\{CHECKSUMS\}\}', "Checksums will be available in the release"
        }
    }
    else {
        $prompt = $prompt -replace '\{\{CHECKSUMS\}\}', "Checksums will be available in the release"
    }
    
    # Create temporary file for prompt
    $tempDir = if ($env:TEMP) { $env:TEMP } elseif ($env:TMPDIR) { $env:TMPDIR } else { "/tmp" }
    $tempPrompt = Join-Path $tempDir "copilot-prompt-$(Get-Random).txt"
    $prompt | Out-File -FilePath $tempPrompt -Encoding utf8NoBOM
    
    try {
        Write-Host "   ⏱️  Calling Copilot CLI ($Timeout second timeout)..." -ForegroundColor Gray
        
        # Execute Copilot CLI with timeout and proper UTF-8 encoding
        # Use -p parameter instead of piping to avoid interactive stdin blocking
        $job = Start-Job -ScriptBlock {
            param($PromptText)
            try {
                # Set console encoding to UTF-8 to properly handle emoji output
                [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
                [Console]::InputEncoding = [System.Text.Encoding]::UTF8
                $PSDefaultParameterValues['*:Encoding'] = 'utf8'
                
                # Use copilot -p to pass prompt as argument (not stdin) to avoid blocking
                $output = copilot -p $PromptText 2>&1
                return $output
            }
            catch {
                return "ERROR: $($_.Exception.Message)"
            }
        } -ArgumentList $prompt
        
        # Wait for job completion with error handling for blocked state
        try {
            $completed = Wait-Job -Job $job -Timeout $Timeout -ErrorAction Stop
        }
        catch {
            # Handle case where job is blocked waiting for user interaction
            if ($_.Exception.Message -match "blocked waiting for user interaction") {
                Stop-Job -Job $job -ErrorAction SilentlyContinue
                Remove-Job -Job $job -ErrorAction SilentlyContinue
                throw "Copilot CLI is waiting for user interaction (interactive mode not supported)"
            }
            throw
        }
        
        if ($completed) {
            $output = Receive-Job -Job $job -ErrorAction SilentlyContinue
            Remove-Job -Job $job
            
            # Handle different output types
            if ($output -is [System.Object[]]) {
                $outputString = ($output | Where-Object { $_ -ne $null }) -join "`n"
            }
            elseif ($output) {
                $outputString = $output.ToString()
            }
            else {
                $outputString = ""
            }
            
            # Format output  
            $releaseNotes = Format-CopilotOutput -RawOutput $outputString
            
            if ([string]::IsNullOrWhiteSpace($releaseNotes)) {
                throw "Copilot CLI returned empty output"
            }
            
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
            return @{
                Range           = "HEAD~10..HEAD"
                PreviousVersion = "HEAD~10"
            }
        }
        else {
            Write-Host "   📊 Comparing changes since $previousTag (last $Type release)" -ForegroundColor Green
            return @{
                Range           = "$previousTag..HEAD"
                PreviousVersion = $previousTag
            }
        }
    }
    catch {
        Write-Host "   ⚠️  Failed to get previous release tag: $($_.Exception.Message)" -ForegroundColor Yellow
        return @{
            Range           = "HEAD~10..HEAD"
            PreviousVersion = "HEAD~10"
        }
    }
}

<#
.SYNOPSIS
    Formats Copilot CLI output to remove formatting artifacts.

.PARAMETER RawOutput
    The raw output from Copilot CLI.

.EXAMPLE
<#
.SYNOPSIS
    Formats Copilot CLI output to remove formatting artifacts.

.PARAMETER RawOutput
    The raw output from Copilot CLI.

.EXAMPLE
    $formatted = Format-CopilotOutput -RawOutput $output

.OUTPUTS
    String - Formatted release notes.
#>
function Format-CopilotOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RawOutput
    )
    
    # Apply comprehensive output cleaning (matching test-ai-release-notes.ps1)
    $formatted = $RawOutput `
        -replace "(?m)^●\s*", "" `
        -replace "(?m)^✓.*$", "" `
        -replace "(?m)^↪.*$", "" `
        -replace "(?m)^\$.*$", "" `
        -replace "(?m)^Total usage.*$", "" `
        -replace "(?m)^Total duration.*$", "" `
        -replace "(?m)^Total code changes.*$", "" `
        -replace "(?m)^Usage by model.*$", "" `
        -replace "(?m)^.*cache read.*$", "" `
        -replace "(?m)^.*cache write.*$", "" `
        -replace "(?m)^System\.Management\.Automation\.RemoteException.*$", "" `
        -replace "(?s)^.*?(?=#\s)", "" `
        -replace "(?m)^\s+(###)", '$1' `
        -replace "(?m)^\s+(-)", '$1' `
        -replace "(?m)^\s*$\n", "`n" `
        -replace "\n{3,}", "`n`n"
    
    # Remove ANSI escape codes
    $formatted = $formatted -replace '\x1b\[[0-9;]*m', ''
    
    # Fix broken URLs by joining lines that were split
    $formatted = $formatted -replace '(?m)compare/([^\s]+)\s*\r?\n\s*\.([^)]+)\)', 'compare/$1.$2)'
    
    # Trim whitespace
    $formatted = $formatted.Trim()
    
    return $formatted
}

# Export all public functions
Export-ModuleMember -Function @(
    'New-AIGeneratedReleaseNotes',
    'Get-CommitRangeSinceLastRelease',
    'Format-CopilotOutput'
)
