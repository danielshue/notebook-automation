<#
.SYNOPSIS
    Standalone test script for AI-generated release notes with output cleaning.

.DESCRIPTION
    This script isolates the AI release notes generation logic from manage-version.ps1
    for independent testing without running the full release process.

.PARAMETER FromTag
    Starting tag for commit range (e.g., "v0.1.0-beta.30")

.PARAMETER ToTag
    Ending tag for commit range (defaults to "HEAD")

.PARAMETER CommitCount
    Number of recent commits to analyze if no tags specified (default: 10)

.EXAMPLE
    .\test-ai-release-notes.ps1 -FromTag "v0.1.0-beta.30"
    
.EXAMPLE
    .\test-ai-release-notes.ps1 -FromTag "v0.1.0-beta.30" -ToTag "v0.1.0-beta.31"
    
.EXAMPLE
    .\test-ai-release-notes.ps1 -CommitCount 5
#>

param(
    [string]$FromTag,
    [string]$ToTag = "HEAD",
    [int]$CommitCount = 10
)

$ErrorActionPreference = "Stop"

Write-Host "🧪 AI Release Notes Generation Test" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host ""

# Get repository root
$RepoRoot = Get-Location
$promptTemplatePath = Join-Path $RepoRoot "scripts\release-notes-prompt.md"

# Validate prompt template exists
if (-not (Test-Path $promptTemplatePath)) {
    throw "Release notes prompt template not found: $promptTemplatePath"
}

# Check for GitHub Copilot CLI
Write-Host "🔍 Checking for GitHub Copilot CLI..." -ForegroundColor Cyan
try {
    $copilotVersion = copilot --version 2>&1 | Out-String
    Write-Host "✅ GitHub Copilot CLI found: $($copilotVersion.Trim())" -ForegroundColor Green
}
catch {
    throw "GitHub Copilot CLI not found. Install with: npm install -g @github/copilot"
}

Write-Host ""

# Get commit range
if ($FromTag) {
    Write-Host "📋 Fetching commits from $FromTag to $ToTag..." -ForegroundColor Cyan
    $commitRange = "$FromTag..$ToTag"
    $commits = git log $commitRange --pretty=format:"%h - %s (%an, %ar)" 2>&1
}
else {
    Write-Host "📋 Fetching last $CommitCount commits..." -ForegroundColor Cyan
    $commits = git log -n $CommitCount --pretty=format:"%h - %s (%an, %ar)" 2>&1
}

if ($LASTEXITCODE -ne 0) {
    throw "Failed to fetch commits: $commits"
}

Write-Host "✅ Retrieved commits:" -ForegroundColor Green
$commits | ForEach-Object { Write-Host "   $_" -ForegroundColor DarkGray }
Write-Host ""

# Load prompt template
Write-Host "📝 Loading prompt template..." -ForegroundColor Cyan
$promptTemplate = Get-Content -Path $promptTemplatePath -Raw

# Replace placeholder with commits
$fullPrompt = $promptTemplate -replace '\{\{COMMITS\}\}', $commits
Write-Host "✅ Prompt prepared (${($fullPrompt.Length)} characters)" -ForegroundColor Green
Write-Host ""

# Save prompt to temp file for inspection
$tempPromptPath = Join-Path $env:TEMP "test-release-notes-prompt.txt"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($tempPromptPath, $fullPrompt, $utf8NoBom)
Write-Host "💾 Full prompt saved to: $tempPromptPath" -ForegroundColor Blue
Write-Host ""

# Execute GitHub Copilot CLI
Write-Host "🤖 Executing GitHub Copilot CLI (timeout: 60s)..." -ForegroundColor Cyan
$job = Start-Job -ScriptBlock {
    param($prompt)
    # Set output encoding to UTF-8 in the job
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
    
    copilot -p $prompt 2>&1
} -ArgumentList $fullPrompt

$completed = Wait-Job -Job $job -Timeout 60
if (-not $completed) {
    Stop-Job -Job $job
    Remove-Job -Job $job
    throw "GitHub Copilot CLI execution timed out after 60 seconds"
}

$rawOutput = Receive-Job -Job $job | Out-String
Remove-Job -Job $job

if ([string]::IsNullOrWhiteSpace($rawOutput)) {
    throw "GitHub Copilot CLI returned empty output"
}

Write-Host "✅ Received response (${($rawOutput.Length)} characters)" -ForegroundColor Green
Write-Host ""

# Save raw output
$tempRawPath = Join-Path $env:TEMP "test-release-notes-raw.txt"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($tempRawPath, $rawOutput, $utf8NoBom)
Write-Host "💾 Raw output saved to: $tempRawPath" -ForegroundColor Blue
Write-Host ""

# Display raw output
Write-Host "📄 Raw Output:" -ForegroundColor Yellow
Write-Host "============================================" -ForegroundColor DarkGray
Write-Host $rawOutput -ForegroundColor White
Write-Host "============================================" -ForegroundColor DarkGray
Write-Host ""

# Apply comprehensive output cleaning
Write-Host "🧹 Applying output cleaning filters..." -ForegroundColor Cyan

$cleanedOutput = $rawOutput `
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
    -replace "(?s)^.*?(?=#\s)", "" `
    -replace "(?m)^\s+(###)", '$1' `
    -replace "(?m)^\s+(-)", '$1' `
    -replace "(?m)^\s*$\n", "`n" `
    -replace "\n{3,}", "`n`n"

$cleanedOutput = $cleanedOutput.Trim()

Write-Host "✅ Cleaning complete" -ForegroundColor Green
Write-Host ""

# Save cleaned output
$tempCleanedPath = Join-Path $env:TEMP "test-release-notes-cleaned.md"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($tempCleanedPath, $cleanedOutput, $utf8NoBom)
Write-Host "💾 Cleaned output saved to: $tempCleanedPath" -ForegroundColor Blue
Write-Host ""

# Display cleaned output
Write-Host "📄 Cleaned Output:" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor DarkGray
Write-Host $cleanedOutput -ForegroundColor White
Write-Host "============================================" -ForegroundColor DarkGray
Write-Host ""

# Analysis
Write-Host "📊 Analysis:" -ForegroundColor Yellow
Write-Host "   Raw length:     $($rawOutput.Length) characters" -ForegroundColor White
Write-Host "   Cleaned length: $($cleanedOutput.Length) characters" -ForegroundColor White
Write-Host "   Removed:        $(($rawOutput.Length - $cleanedOutput.Length)) characters" -ForegroundColor White
Write-Host ""

# Check for unwanted patterns in cleaned output
$issues = @()

if ($cleanedOutput -match "Total usage") {
    $issues += "❌ 'Total usage' still present"
}

if ($cleanedOutput -match "Total duration") {
    $issues += "❌ 'Total duration' still present"
}

if ($cleanedOutput -match "Total code changes") {
    $issues += "❌ 'Total code changes' still present"
}

if ($cleanedOutput -match "cache read|cache write") {
    $issues += "❌ Cache statistics still present"
}

if ($cleanedOutput -match "^●") {
    $issues += "❌ Bullet prefix (●) still present"
}

if ($cleanedOutput -notmatch "^#\s") {
    $issues += "⚠️  Doesn't start with # title header"
}

if ($issues.Count -eq 0) {
    Write-Host "✅ Quality Check: PASSED - No unwanted patterns detected" -ForegroundColor Green
}
else {
    Write-Host "⚠️  Quality Check: ISSUES FOUND" -ForegroundColor Yellow
    $issues | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
}

Write-Host ""
Write-Host "📁 Test files saved to:" -ForegroundColor Blue
Write-Host "   Prompt:  $tempPromptPath" -ForegroundColor DarkGray
Write-Host "   Raw:     $tempRawPath" -ForegroundColor DarkGray
Write-Host "   Cleaned: $tempCleanedPath" -ForegroundColor DarkGray
Write-Host ""
Write-Host "✅ Test complete!" -ForegroundColor Green
