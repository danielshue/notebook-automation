<#
.SYNOPSIS
    Dependency validation and repository verification functions.

.DESCRIPTION
    This module provides functions to validate external dependencies and verify
    repository structure before executing version management operations.

.NOTES
    Module: Quality.Dependencies
    Version: 1.0.0
#>

<#
.SYNOPSIS
    Tests if a command/dependency is available.

.DESCRIPTION
    Checks if a command is available in the system PATH and optionally prompts
    the user to continue if it's not found.

.PARAMETER CommandName
    The command name to check (e.g., "git", "node", "npm").

.PARAMETER DisplayName
    Friendly display name for the command.

.PARAMETER InstallPrompt
    Helpful message about how to install the dependency.

.PARAMETER Required
    If true, prompts user to continue if dependency is not found.

.PARAMETER ThrowOnFailure
    If true, throws an exception when required dependency is not found.

.EXAMPLE
    $gitAvailable = Test-CommandDependency -CommandName "git" -DisplayName "Git" -InstallPrompt "Install from https://git-scm.com"

.OUTPUTS
    Boolean - $true if command is available, $false otherwise.
#>
function Test-CommandDependency {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommandName,
        
        [Parameter(Mandatory = $true)]
        [string]$DisplayName,
        
        [string]$InstallPrompt = "",
        
        [bool]$Required = $true,
        
        [switch]$ThrowOnFailure
    )
    
    try {
        $command = Get-Command $CommandName -ErrorAction Stop
        Write-Host "✅ $DisplayName found: $($command.Source)" -ForegroundColor Green
        return $true
    }
    catch {
        if ($Required) {
            Write-Host "❌ $DisplayName not found in PATH" -ForegroundColor Red
            
            if ($InstallPrompt) {
                Write-Host "   $InstallPrompt" -ForegroundColor Yellow
            }
            Write-Host ""
            
            # Prompt user to continue
            $response = Read-Host "Would you like to continue anyway? This may cause the script to fail later (y/N)"
            if ($response -notmatch '^[yY]') {
                $message = "$DisplayName is required but not installed. Install it and try again."
                if ($ThrowOnFailure) {
                    throw $message
                }
                Write-Host "✗ $message" -ForegroundColor Red
                return $false
            }
            
            Write-Host "⚠️  Continuing without $DisplayName - expect failures if this tool is needed" -ForegroundColor Yellow
            return $false
        }
        else {
            Write-Host "⚠️  $DisplayName not found (optional)" -ForegroundColor Yellow
            return $false
        }
    }
}

<#
.SYNOPSIS
    Validates the repository directory structure.

.DESCRIPTION
    Checks if the current directory is a Git repository and verifies that required
    project files exist in the repository root.

.PARAMETER RepoRoot
    The repository root directory path.

.PARAMETER RequiredFiles
    Array of file paths (relative to repo root) that must exist.

.PARAMETER ThrowOnFailure
    If true, throws an exception when validation fails.

.EXAMPLE
    $valid = Test-RepositoryStructure -RepoRoot "." -ThrowOnFailure

.OUTPUTS
    Boolean - $true if repository structure is valid, $false otherwise.
#>
function Test-RepositoryStructure {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        
        [string[]]$RequiredFiles = @(
            "GitVersion.yml",
            "src/obsidian-plugin/package.json",
            "src/obsidian-plugin/manifest.json",
            "src/c-sharp/NotebookAutomation.sln"
        ),
        
        [switch]$ThrowOnFailure
    )
    
    Write-Host "📁 Validating repository directory..." -ForegroundColor Cyan
    
    # Check if we're in a git repository
    try {
        git rev-parse --git-dir | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Not in a git repository"
        }
    }
    catch {
        $message = "Not running in a git repository. Please run this script from the repository root."
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "❌ $message" -ForegroundColor Red
        return $false
    }
    
    # Check for required project files
    $missingFiles = @()
    foreach ($file in $RequiredFiles) {
        # Normalize path separators
        $normalizedPath = $file -replace '/', [System.IO.Path]::DirectorySeparatorChar
        $fullPath = Join-Path $RepoRoot $normalizedPath
        
        if (-not (Test-Path $fullPath)) {
            $missingFiles += $file
        }
    }
    
    if ($missingFiles.Count -gt 0) {
        Write-Host "❌ Missing required project files:" -ForegroundColor Red
        $missingFiles | ForEach-Object { Write-Host "   - $_" -ForegroundColor Red }
        
        $message = "Please run this script from the repository root directory."
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    Write-Host "✅ Repository directory validation passed" -ForegroundColor Green
    return $true
}

<#
.SYNOPSIS
    Tests all required dependencies for version management operations.

.DESCRIPTION
    Validates that all required external tools (Git, GitHub CLI, dotnet, npm, etc.)
    are available and the repository structure is correct.

.PARAMETER RepoRoot
    The repository root directory path.

.PARAMETER RequireGitHubCLI
    If true, GitHub CLI is required (default: true).

.PARAMETER RequireDotNet
    If true, .NET SDK is required (default: true).

.PARAMETER RequireNode
    If true, Node.js and npm are required (default: true).

.PARAMETER ThrowOnFailure
    If true, throws an exception when critical dependencies are missing.

.EXAMPLE
    $allValid = Test-AllDependencies -RepoRoot "." -ThrowOnFailure

.OUTPUTS
    Boolean - $true if all required dependencies are available, $false otherwise.
#>
function Test-AllDependencies {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        
        [bool]$RequireGitHubCLI = $true,
        [bool]$RequireDotNet = $true,
        [bool]$RequireNode = $true,
        
        [switch]$ThrowOnFailure
    )
    
    Write-Host "🔍 Checking dependencies..." -ForegroundColor Cyan
    Write-Host ""
    
    # Test repository structure first
    $repoValid = Test-RepositoryStructure -RepoRoot $RepoRoot -ThrowOnFailure:$ThrowOnFailure
    if (-not $repoValid -and $ThrowOnFailure) {
        throw "Repository structure validation failed"
    }
    Write-Host ""
    
    $allValid = $repoValid
    
    # Core dependencies (always required)
    $gitAvailable = Test-CommandDependency `
        -CommandName "git" `
        -DisplayName "Git" `
        -InstallPrompt "Install Git from: https://git-scm.com/downloads" `
        -Required $true `
        -ThrowOnFailure:$ThrowOnFailure
    
    $allValid = $allValid -and $gitAvailable
    
    # GitHub CLI
    if ($RequireGitHubCLI) {
        $ghAvailable = Test-CommandDependency `
            -CommandName "gh" `
            -DisplayName "GitHub CLI" `
            -InstallPrompt "Install GitHub CLI from: https://cli.github.com/" `
            -Required $true `
            -ThrowOnFailure:$ThrowOnFailure
        
        $allValid = $allValid -and $ghAvailable
    }
    
    # .NET SDK
    if ($RequireDotNet) {
        $dotnetAvailable = Test-CommandDependency `
            -CommandName "dotnet" `
            -DisplayName ".NET SDK" `
            -InstallPrompt "Install .NET SDK from: https://dotnet.microsoft.com/download" `
            -Required $true `
            -ThrowOnFailure:$ThrowOnFailure
        
        $allValid = $allValid -and $dotnetAvailable
    }
    
    # Node.js
    if ($RequireNode) {
        $nodeAvailable = Test-CommandDependency `
            -CommandName "node" `
            -DisplayName "Node.js" `
            -InstallPrompt "Install Node.js from: https://nodejs.org/" `
            -Required $true `
            -ThrowOnFailure:$ThrowOnFailure
        
        $allValid = $allValid -and $nodeAvailable
        
        # npm (usually comes with Node.js)
        $npmAvailable = Test-CommandDependency `
            -CommandName "npm" `
            -DisplayName "npm" `
            -InstallPrompt "npm should be installed with Node.js. Reinstall Node.js if missing." `
            -Required $true `
            -ThrowOnFailure:$ThrowOnFailure
        
        $allValid = $allValid -and $npmAvailable
    }
    
    # Optional: GitHub Copilot CLI for AI release notes
    Test-CommandDependency `
        -CommandName "copilot" `
        -DisplayName "GitHub Copilot CLI (optional, for AI release notes)" `
        -InstallPrompt "Install with: npm install -g @github/copilot" `
        -Required $false | Out-Null
    
    Write-Host ""
    if ($allValid) {
        Write-Host "✅ All required dependencies are available" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  Some required dependencies are missing" -ForegroundColor Yellow
    }
    Write-Host ""
    
    return $allValid
}

# Export all public functions
Export-ModuleMember -Function @(
    'Test-CommandDependency',
    'Test-RepositoryStructure',
    'Test-AllDependencies'
)
