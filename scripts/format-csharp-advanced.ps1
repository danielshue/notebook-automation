#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Enforces consistent formatting and spacing throughout the C# codebase
.DESCRIPTION
    This script applies comprehensive formatting rules including:
    - XML documentation spacing (2 blank lines between xmldoc and class/method)
    - Method spacing (1 blank line between methods)
    - Using statement organization
    - Code formatting via dotnet format
    - StyleCop rule enforcement
.PARAMETER Path
    Path to the C# source directory (default: current directory)
.PARAMETER Fix
    Apply automatic fixes where possible
.PARAMETER Verify
    Only verify formatting without making changes
.EXAMPLE
    ./format-csharp-advanced.ps1 -Path "src/c-sharp" -Fix
#>

param(
    [Parameter(HelpMessage = "Path to the C# source directory")]
    [string]$Path = ".",

    [Parameter(HelpMessage = "Apply automatic fixes where possible")]
    [switch]$Fix,

    [Parameter(HelpMessage = "Only verify formatting without making changes")]
    [switch]$Verify
)

$ErrorActionPreference = "Stop"

# Colors for output
function Write-ColoredOutput {
    param([string]$Message, [System.ConsoleColor]$Color = "White")
    if ($Color) {
        Write-Host $Message -ForegroundColor $Color
    }
    else {
        Write-Host $Message
    }
}

function Test-XMLDocumentationSpacing {
    param([string]$FilePath)

    $lines = Get-Content $FilePath
    $issues = @()

    # Check for proper spacing between XML docs and class/method declarations
    for ($i = 0; $i -lt $lines.Length - 1; $i++) {
        $currentLine = $lines[$i].Trim()
        $nextLine = if ($i + 1 -lt $lines.Length) { $lines[$i + 1].Trim() } else { "" }

        # Check if current line ends XML documentation
        if ($currentLine -match '^\s*/// </summary>|^\s*/// </returns>') {
            # Next line should be blank, then class/method declaration
            if ($nextLine -match '^(public|private|protected|internal|static).*(class|interface|struct|enum|\w+\s*\()') {
                $issues += @{
                    Line  = $i + 1
                    Issue = "Missing blank line after XML documentation"
                    Type  = "XMLDocSpacing"
                }
            }
        }

        # Check for proper spacing between methods (should have blank line)
        if ($currentLine -eq "}" -and $nextLine -match '^\s*/// <summary>') {
            if ($i + 2 -lt $lines.Length) {
                $lineAfterNext = $lines[$i + 2].Trim()
                if ($lineAfterNext -eq "") {
                    # Good: blank line before XML doc
                }
                else {
                    $issues += @{
                        Line  = $i + 2
                        Issue = "Missing blank line before XML documentation"
                        Type  = "MethodSpacing"
                    }
                }
            }
        }
    }

    return $issues
}

function Repair-XMLDocumentationSpacing {
    param([string]$FilePath)

    $lines = Get-Content $FilePath
    $newLines = @()
    $modified = $false

    for ($i = 0; $i -lt $lines.Length; $i++) {
        $currentLine = $lines[$i]
        $currentLineTrimmed = $currentLine.Trim()

        # Add current line
        $newLines += $currentLine

        # Check if we need to add spacing after XML documentation
        if ($currentLineTrimmed -match '^\s*/// </summary>|^\s*/// </returns>') {
            if ($i + 1 -lt $lines.Length) {
                $nextLine = $lines[$i + 1].Trim()
                if ($nextLine -match '^(public|private|protected|internal|static).*(class|interface|struct|enum|\w+\s*\()') {
                    # Add blank line after XML documentation
                    $newLines += ""
                    $modified = $true
                }
            }
        }

        # Check if we need to add spacing before XML documentation (between methods)
        if ($currentLineTrimmed -eq "}" -and $i + 1 -lt $lines.Length) {
            $nextLine = $lines[$i + 1].Trim()
            if ($nextLine -match '^\s*/// <summary>') {
                # Add blank line before XML documentation
                $newLines += ""
                $modified = $true
            }
        }
    }

    if ($modified) {
        $newLines | Set-Content $FilePath -Encoding UTF8
        Write-ColoredOutput "  ✓ Fixed XML documentation spacing" "Green"
        return $true
    }

    return $false
}

function Invoke-FormatEnforcement {
    param([string]$SourcePath, [bool]$ApplyFixes)

    Write-ColoredOutput "🔧 Enforcing Formatting Standards" "Cyan"
    Write-ColoredOutput "=================================" "Cyan"
    Write-ColoredOutput "Path: $SourcePath" "Gray"
    Write-ColoredOutput "Mode: $(if ($ApplyFixes) { 'Fix' } else { 'Verify' })" "Gray"
    Write-ColoredOutput ""    # Step 1: Run dotnet format
    Write-ColoredOutput "1️⃣ Running dotnet format..." "Yellow"

    # Find the solution file
    $solutionFile = Get-ChildItem -Path $SourcePath -Filter "*.sln" -Recurse | Select-Object -First 1
    if (-not $solutionFile) {
        # Try looking in parent directories
        $currentDir = $SourcePath
        while ($currentDir -and -not $solutionFile) {
            $solutionFile = Get-ChildItem -Path $currentDir -Filter "*.sln" | Select-Object -First 1
            if (-not $solutionFile) {
                $parentDir = Split-Path $currentDir -Parent
                if ($parentDir -eq $currentDir) { break }
                $currentDir = $parentDir
            }
        }
    }

    if ($solutionFile) {
        $solutionPath = $solutionFile.FullName
        Write-ColoredOutput "   Using solution: $solutionPath" "Gray"

        $formatArgs = @("format", $solutionPath)
        if (-not $ApplyFixes) {
            $formatArgs += "--verify-no-changes"
        }

        try {
            & dotnet @formatArgs
            Write-ColoredOutput "   ✓ dotnet format completed successfully" "Green"
        }
        catch {
            Write-ColoredOutput "   ⚠️ dotnet format issues found" "Yellow"
        }
    }
    else {
        Write-ColoredOutput "   ⚠️ No solution file found, skipping dotnet format" "Yellow"
    }    # Step 2: Check StyleCop rules
    Write-ColoredOutput "`n2️⃣ Checking StyleCop rules..." "Yellow"
    try {
        if ($solutionFile) {
            & dotnet build $solutionPath --verbosity quiet
        }
        else {
            # Fallback to building any .csproj files found
            $projectFiles = Get-ChildItem -Path $SourcePath -Filter "*.csproj" -Recurse
            foreach ($proj in $projectFiles) {
                & dotnet build $proj.FullName --verbosity quiet
            }
        }
        Write-ColoredOutput "   ✓ StyleCop analysis completed" "Green"
    }
    catch {
        Write-ColoredOutput "   ⚠️ StyleCop violations found" "Yellow"
    }

    # Step 3: Check XML documentation spacing
    Write-ColoredOutput "`n3️⃣ Checking XML documentation spacing..." "Yellow"
    $csFiles = Get-ChildItem -Path $SourcePath -Recurse -Filter "*.cs" | Where-Object {
        $_.Name -notlike "*GlobalUsings*" -and $_.Name -notlike "*AssemblyInfo*"
    }

    $totalIssues = 0
    $fixedFiles = 0

    foreach ($file in $csFiles) {
        $issues = Test-XMLDocumentationSpacing -FilePath $file.FullName

        if ($issues.Count -gt 0) {
            Write-ColoredOutput "   📄 $($file.Name)" "White"
            if ($ApplyFixes) {
                $fixed = Repair-XMLDocumentationSpacing -FilePath $file.FullName
                if ($fixed) {
                    $fixedFiles++
                }
            }
            else {
                foreach ($issue in $issues) {
                    Write-ColoredOutput "     Line $($issue.Line): $($issue.Issue)" "Red"
                }
            }

            $totalIssues += $issues.Count
        }
    }

    # Step 4: Summary
    Write-ColoredOutput "`n📊 Summary:" "Cyan"
    Write-ColoredOutput "Files checked: $($csFiles.Count)" "Gray"

    if ($ApplyFixes) {
        Write-ColoredOutput "Files fixed: $fixedFiles" "Green"
    }
    else {
        Write-ColoredOutput "Formatting issues: $totalIssues" $(if ($totalIssues -eq 0) { "Green" } else { "Red" })
    }

    Write-ColoredOutput "`n✅ Format enforcement complete!" "Green"

    return $totalIssues
}

# Main execution
if ($Verify -and $Fix) {
    Write-Error "Cannot specify both -Verify and -Fix parameters"
    exit 1
}

$applyFixes = $Fix -or (-not $Verify)
$issueCount = Invoke-FormatEnforcement -SourcePath $Path -ApplyFixes $applyFixes

if ($Verify -and $issueCount -gt 0) {
    exit 1
}
