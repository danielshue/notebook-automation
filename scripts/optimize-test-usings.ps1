#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Optimize using statements in test projects by removing redundant imports that are now in GlobalUsings.cs
.DESCRIPTION
    This script removes commonly used using statements from test files since they are now defined in GlobalUsings.cs
#>

$coreTestsPath = "src/c-sharp/NotebookAutomation.Core.Tests"
$cliTestsPath = "src/c-sharp/NotebookAutomation.Cli.Tests"

# Using statements that should be removed from Core.Tests files
$coreGlobalUsings = @(
    "using System;",
    "using System.Collections.Generic;",
    "using System.IO;",
    "using System.Linq;",
    "using System.Text.Json;",
    "using System.Threading;",
    "using System.Threading.Tasks;",
    "using Microsoft.VisualStudio.TestTools.UnitTesting;",
    "using Microsoft.Extensions.Configuration;",
    "using Microsoft.Extensions.DependencyInjection;",
    "using Microsoft.Extensions.Logging;",
    "using Microsoft.Extensions.Logging.Abstractions;",
    "using Moq;",
    "using NotebookAutomation.Core.Configuration;",
    "using NotebookAutomation.Core.Services;",
    "using NotebookAutomation.Core.Utils;",
    "using NotebookAutomation.Core.Tools.Shared;",
    "using NotebookAutomation.Core.Tools.TagManagement;",
    "using NotebookAutomation.Core.Tools.VideoProcessing;",
    "using NotebookAutomation.Core.Tools.PdfProcessing;",
    "using NotebookAutomation.Core.Tools.MarkdownGeneration;",
    "using NotebookAutomation.Core.Tests.Helpers;"
)

# Using statements that should be removed from CLI.Tests files
$cliGlobalUsings = @(
    "using System;",
    "using System.CommandLine;",
    "using System.CommandLine.Parsing;",
    "using System.IO;",
    "using System.Linq;",
    "using System.Threading;",
    "using System.Threading.Tasks;",
    "using Microsoft.VisualStudio.TestTools.UnitTesting;",
    "using Microsoft.Extensions.Logging;",
    "using Moq;",
    "using NotebookAutomation.Cli.Commands;",
    "using NotebookAutomation.Cli.Utilities;",
    "using NotebookAutomation.Core.Configuration;"
)

function Remove-RedundantUsings {
    param(
        [string]$FilePath,
        [string[]]$GlobalUsings
    )
    Write-Host "Processing: $FilePath" -ForegroundColor Green

    $lines = Get-Content $FilePath

    $modifiedLines = @()
    $inUsingSection = $false
    $usingsRemoved = 0

    foreach ($line in $lines) {
        $trimmedLine = $line.Trim()

        # Check if we're in the using section
        if ($trimmedLine.StartsWith("using ") -and -not $trimmedLine.StartsWith("using static")) {
            $inUsingSection = $true

            # Check if this using should be removed
            $shouldRemove = $false
            foreach ($globalUsing in $GlobalUsings) {
                if ($trimmedLine -eq $globalUsing.Trim()) {
                    $shouldRemove = $true
                    $usingsRemoved++
                    break
                }
            }

            if (-not $shouldRemove) {
                $modifiedLines += $line
            }
        }
        elseif ($inUsingSection -and ($trimmedLine -eq "" -or $trimmedLine.StartsWith("namespace"))) {
            # End of using section
            $inUsingSection = $false
            $modifiedLines += $line
        }
        else {
            $modifiedLines += $line
        }
    }

    if ($usingsRemoved -gt 0) {
        $modifiedLines | Set-Content $FilePath
        Write-Host "  Removed $usingsRemoved redundant using statements" -ForegroundColor Yellow
    }
    else {
        Write-Host "  No redundant using statements found" -ForegroundColor Gray
    }
}

# Process Core.Tests files
Write-Host "Optimizing NotebookAutomation.Core.Tests files..." -ForegroundColor Cyan
$coreTestFiles = Get-ChildItem -Path $coreTestsPath -Recurse -Filter "*.cs" | Where-Object { $_.Name -ne "GlobalUsings.cs" }

foreach ($file in $coreTestFiles) {
    Remove-RedundantUsings -FilePath $file.FullName -GlobalUsings $coreGlobalUsings
}

# Process CLI.Tests files
Write-Host "`nOptimizing NotebookAutomation.Cli.Tests files..." -ForegroundColor Cyan
$cliTestFiles = Get-ChildItem -Path $cliTestsPath -Recurse -Filter "*.cs" | Where-Object { $_.Name -ne "GlobalUsings.cs" }

foreach ($file in $cliTestFiles) {
    Remove-RedundantUsings -FilePath $file.FullName -GlobalUsings $cliGlobalUsings
}

Write-Host "`nOptimization complete!" -ForegroundColor Green
