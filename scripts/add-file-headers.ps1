#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Adds standardized file headers to all C# files in the project
.DESCRIPTION
    This script automatically adds or updates file headers for all C# files with:
    - Copyright notice
    - License information
    - Project name and description
    - Author information
    - Creation/modification dates
.PARAMETER Path
    Path to the C# source directory (default: "src/c-sharp")
.PARAMETER DryRun
    Preview changes without applying them
.PARAMETER Force
    Overwrite existing headers
.PARAMETER Author
    Author name for the header (default: from git config)
.PARAMETER Company
    Company name for copyright (default: "Notebook Automation Project")
.EXAMPLE
    ./add-file-headers.ps1 -Path "src/c-sharp" -Author "John Doe"
#>

param(
    [Parameter(HelpMessage = "Path to the C# source directory")]
    [string]$Path = "src/c-sharp",

    [Parameter(HelpMessage = "Preview changes without applying them")]
    [switch]$DryRun,

    [Parameter(HelpMessage = "Overwrite existing headers")]
    [switch]$Force,

    [Parameter(HelpMessage = "Author name for the header")]
    [string]$Author = "",

    [Parameter(HelpMessage = "Company name for copyright")]
    [string]$Company = "Notebook Automation Project"
)

$ErrorActionPreference = "Stop"

# Colors for output
function Write-ColoredOutput {
    param([string]$Message, [System.ConsoleColor]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Get-GitAuthor {
    try {
        $gitName = git config user.name 2>$null
        $gitEmail = git config user.email 2>$null
        if ($gitName -and $gitEmail) {
            return "$gitName <$gitEmail>"
        }
        elseif ($gitName) {
            return $gitName
        }
        else {
            return $env:USERNAME
        }
    }
    catch {
        return $env:USERNAME
    }
}

function Get-FileHeader {
    param(
        [string]$FilePath,
        [string]$Author,
        [string]$Company
    )

    $fileName = Split-Path $FilePath -Leaf
    $currentYear = (Get-Date).Year
    $relativePath = $FilePath -replace [regex]::Escape((Get-Location).Path), "."
    $relativePath = $relativePath -replace "\\", "/"

    return @"
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

"@
}

function Test-HasFileHeader {
    param([string]$FilePath)    $content = Get-Content $FilePath -Raw -ErrorAction SilentlyContinue
    return $content -match "^\s*//\s*Licensed under the MIT License"
}

function Add-HeaderToFile {
    param(
        [string]$FilePath,
        [string]$Header,
        [bool]$Force
    )

    $content = Get-Content $FilePath -Raw
    $hasHeader = Test-HasFileHeader $FilePath

    if ($hasHeader -and -not $Force) {
        Write-ColoredOutput "   ⏭️  Skipping (header exists): $(Split-Path $FilePath -Leaf)" "Yellow"
        return $false
    }

    if ($hasHeader -and $Force) {
        # Remove existing header (everything before first non-comment, non-using, non-namespace line)
        $lines = $content -split "`r?`n"
        $startIndex = 0

        for ($i = 0; $i -lt $lines.Length; $i++) {
            $line = $lines[$i].Trim()
            if ($line -match "^//") {
                continue # Skip comment lines
            }
            if ($line -match "^#nullable" -or $line -match "^using" -or $line -eq "") {
                continue # Skip directives, usings, and empty lines
            }
            if ($line -match "^namespace") {
                $startIndex = $i
                break
            }
            # If we hit any other code, this is where we insert
            $startIndex = $i
            break
        }

        $newContent = $Header + ($lines[$startIndex..($lines.Length - 1)] -join "`n")
        Write-ColoredOutput "   🔄 Updating header: $(Split-Path $FilePath -Leaf)" "Blue"
    }
    else {
        # Add header at the beginning
        $newContent = $Header + $content
        Write-ColoredOutput "   ➕ Adding header: $(Split-Path $FilePath -Leaf)" "Green"
    }

    if (-not $DryRun) {
        Set-Content $FilePath $newContent -NoNewline
    }

    return $true
}

# Main execution
Write-ColoredOutput "📄 Adding File Headers to C# Project" "Cyan"
Write-ColoredOutput "=====================================" "Cyan"

if (-not (Test-Path $Path)) {
    Write-ColoredOutput "❌ Path not found: $Path" "Red"
    exit 1
}

if (-not $Author) {
    $Author = Get-GitAuthor
}

Write-ColoredOutput "📁 Path: $Path" "White"
Write-ColoredOutput "👤 Author: $Author" "White"
Write-ColoredOutput "🏢 Company: $Company" "White"
Write-ColoredOutput "🔧 Mode: $(if ($DryRun) { 'DRY RUN' } else { 'APPLY CHANGES' })" "White"
Write-ColoredOutput "💪 Force: $(if ($Force) { 'YES' } else { 'NO' })" "White"
Write-ColoredOutput "" "White"

# Find all C# files
$csFiles = Get-ChildItem -Path $Path -Recurse -Filter "*.cs" | Where-Object {
    $_.FullName -notmatch "\\bin\\" -and
    $_.FullName -notmatch "\\obj\\" -and
    $_.Name -ne "GlobalAssemblyInfo.cs" -and
    $_.Name -ne "AssemblyInfo.cs"
}

Write-ColoredOutput "🔍 Found $($csFiles.Count) C# files to process" "White"
Write-ColoredOutput "" "White"

$processedCount = 0
$skippedCount = 0
$addedCount = 0
$updatedCount = 0

foreach ($file in $csFiles) {
    $processedCount++
    $header = Get-FileHeader -FilePath $file.FullName -Author $Author -Company $Company
    $hadHeader = Test-HasFileHeader $file.FullName

    $wasProcessed = Add-HeaderToFile -FilePath $file.FullName -Header $header -Force $Force

    if ($wasProcessed) {
        if ($hadHeader) {
            $updatedCount++
        }
        else {
            $addedCount++
        }
    }
    else {
        $skippedCount++
    }
}

Write-ColoredOutput "" "White"
Write-ColoredOutput "📊 Summary:" "Cyan"
Write-ColoredOutput "Files processed: $processedCount" "White"
Write-ColoredOutput "Headers added: $addedCount" "Green"
Write-ColoredOutput "Headers updated: $updatedCount" "Blue"
Write-ColoredOutput "Files skipped: $skippedCount" "Yellow"

if ($DryRun) {
    Write-ColoredOutput "" "White"
    Write-ColoredOutput "🔍 This was a DRY RUN - no files were modified" "Yellow"
    Write-ColoredOutput "Run without -DryRun to apply changes" "Yellow"
}
else {
    Write-ColoredOutput "" "White"
    Write-ColoredOutput "✅ File headers applied successfully!" "Green"
}
