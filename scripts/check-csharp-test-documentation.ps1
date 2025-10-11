#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Finds all test methods missing XML documentation and reports them
.DESCRIPTION
    This script scans all C# test files and identifies test methods that are missing XML documentation comments.
    It helps ensure comprehensive documentation coverage for all test methods.
#>

param(
    [Parameter(HelpMessage = "Path to the test projects directory")]
    [string]$TestPath = ".",

    [Parameter(HelpMessage = "Show verbose output")]
    [switch]$VerboseOutput
)

$ErrorActionPreference = "Stop"

# Import required modules
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ModulesDir = Join-Path $ScriptDir "modules"
Import-Module (Join-Path $ModulesDir "Core\Logging.psm1") -Force

# Colors for output
$Red = [System.ConsoleColor]::Red
$Green = [System.ConsoleColor]::Green
$Yellow = [System.ConsoleColor]::Yellow
$Cyan = [System.ConsoleColor]::Cyan

function Find-TestMethodsMissingDocumentation {
    param([string]$TestDirectory)

    Write-ColoredOutput "🔍 Scanning for test methods missing XML documentation..." $Cyan

    $testFiles = Get-ChildItem -Path $TestDirectory -Recurse -Filter "*.cs" | Where-Object {
        $_.FullName -like "*Test*" -and $_.Name -notlike "*GlobalUsings*"
    }

    $totalMethods = 0
    $undocumentedMethods = 0
    $undocumentedFiles = @()

    foreach ($file in $testFiles) {
        $content = Get-Content $file.FullName -Raw
        
        # Skip empty files
        if ([string]::IsNullOrWhiteSpace($content)) {
            continue
        }
        
        $lines = Get-Content $file.FullName

        # Find all test methods using regex
        $testMethodPattern = '(?ms)(\[TestMethod\]\s*(?:\[.*?\]\s*)*)(public\s+(?:async\s+)?(?:Task|void)\s+(\w+)\s*\([^)]*\))'
        $matches = [regex]::Matches($content, $testMethodPattern)

        $fileMethods = @()

        foreach ($match in $matches) {
            $totalMethods++
            $methodName = $match.Groups[3].Value
            $methodStart = $match.Index

            # Check if there's XML documentation before the [TestMethod] attribute
            $beforeMethod = $content.Substring(0, $methodStart)
            $lastSummaryIndex = $beforeMethod.LastIndexOf("/// <summary>")
            $lastTestMethodIndex = $beforeMethod.LastIndexOf("[TestMethod]")

            # If there's no summary, or if there's another [TestMethod] after the last summary, it's undocumented
            if ($lastSummaryIndex -eq -1 -or ($lastTestMethodIndex -ne -1 -and $lastTestMethodIndex -gt $lastSummaryIndex)) {
                $undocumentedMethods++
                $fileMethods += $methodName

                if ($VerboseOutput) {
                    Write-ColoredOutput "  ❌ $methodName" $Red
                }
            }
            elseif ($Verbose) {
                Write-ColoredOutput "  ✅ $methodName" $Green
            }
        }

        if ($fileMethods.Count -gt 0) {
            $relativePath = $file.FullName.Replace((Get-Location).Path, "").TrimStart('\', '/')
            $undocumentedFiles += [PSCustomObject]@{
                File    = $relativePath
                Methods = $fileMethods
                Count   = $fileMethods.Count
            }

            Write-ColoredOutput "📄 $relativePath - $($fileMethods.Count) undocumented methods" $Yellow
        }
        elseif ($Verbose -and $matches.Count -gt 0) {
            $relativePath = $file.FullName.Replace((Get-Location).Path, "").TrimStart('\', '/')
            Write-ColoredOutput "📄 $relativePath - All methods documented ✅" $Green
        }
    }

    Write-ColoredOutput "`n📊 Summary:" $Cyan
    Write-ColoredOutput "Total test methods found: $totalMethods" ([System.ConsoleColor]::White)
    Write-ColoredOutput "Undocumented methods: $undocumentedMethods" $Red
    Write-ColoredOutput "Documentation coverage: $([math]::Round((($totalMethods - $undocumentedMethods) / $totalMethods) * 100, 1))%" $Green

    if ($undocumentedFiles.Count -gt 0) {
        Write-ColoredOutput "`n🔧 Files needing documentation updates:" $Yellow
        foreach ($fileInfo in $undocumentedFiles) {
            Write-ColoredOutput "  $($fileInfo.File) ($($fileInfo.Count) methods)" $Yellow
            foreach ($method in $fileInfo.Methods) {
                Write-ColoredOutput "    - $method" $Red
            }
        }
    }
    else {
        Write-ColoredOutput "`n🎉 All test methods are properly documented!" $Green
    }
}

# Main execution
Write-ColoredOutput "📝 Test Method Documentation Checker" $Cyan
Write-ColoredOutput "=====================================" $Cyan

Find-TestMethodsMissingDocumentation -TestDirectory $TestPath

Write-ColoredOutput "`n✅ Documentation check complete!" $Green
