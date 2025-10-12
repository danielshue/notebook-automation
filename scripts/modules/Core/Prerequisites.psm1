<#
.SYNOPSIS
    Prerequisite validation functions for external dependencies.

.DESCRIPTION
    This module provides functions to validate that required external tools and
    dependencies are available and properly configured before running operations.

.NOTES
    Module: Core.Prerequisites
    Version: 1.0.0
#>

<#
.SYNOPSIS
    Tests if GitHub CLI is installed and authenticated.

.DESCRIPTION
    Checks for the presence of the GitHub CLI (gh) and verifies authentication status.
    Provides helpful error messages with installation and authentication instructions.

.PARAMETER ThrowOnFailure
    If true, throws an exception when validation fails. If false, returns $false.

.EXAMPLE
    Test-GitHubCLI -ThrowOnFailure
    # Throws exception if GitHub CLI is not available or not authenticated

.EXAMPLE
    if (Test-GitHubCLI) { Write-Host "GitHub CLI is ready" }
    # Returns $true/$false without throwing

.OUTPUTS
    Boolean - $true if GitHub CLI is available and authenticated, $false otherwise.
#>
function Test-GitHubCLI {
    param(
        [switch]$ThrowOnFailure
    )
    
    # Check if GitHub CLI is available
    try {
        $ghVersion = gh --version 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) {
            if ($ThrowOnFailure) {
                throw "GitHub CLI not available"
            }
            Write-Host "✗ GitHub CLI not available" -ForegroundColor Red
            Write-Host "Please install GitHub CLI: https://cli.github.com/" -ForegroundColor Yellow
            return $false
        }
    }
    catch {
        if ($ThrowOnFailure) {
            throw "GitHub CLI not available. Please install from: https://cli.github.com/"
        }
        Write-Host "✗ GitHub CLI not available" -ForegroundColor Red
        Write-Host "Please install GitHub CLI: https://cli.github.com/" -ForegroundColor Yellow
        return $false
    }
    
    # Check if authenticated
    try {
        $authStatus = gh auth status 2>&1 | Out-String
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ GitHub CLI authenticated" -ForegroundColor Green
            return $true
        }
        else {
            if ($ThrowOnFailure) {
                throw "GitHub CLI not authenticated. Please run 'gh auth login' first."
            }
            Write-Host "✗ GitHub CLI not authenticated" -ForegroundColor Red
            Write-Host "Please run 'gh auth login' first." -ForegroundColor Yellow
            return $false
        }
    }
    catch {
        if ($ThrowOnFailure) {
            throw "GitHub CLI not authenticated. Please run 'gh auth login' first."
        }
        Write-Host "✗ GitHub CLI not authenticated" -ForegroundColor Red
        Write-Host "Please run 'gh auth login' first." -ForegroundColor Yellow
        return $false
    }
}

<#
.SYNOPSIS
    Tests if we're in a Git repository.

.DESCRIPTION
    Checks if the current directory is within a Git repository and optionally
    returns the current branch name.

.PARAMETER ThrowOnFailure
    If true, throws an exception when not in a Git repository.

.PARAMETER GetBranch
    If true, returns the current branch name instead of a boolean.

.EXAMPLE
    Test-GitRepository -ThrowOnFailure
    # Throws if not in a Git repository

.EXAMPLE
    $branch = Test-GitRepository -GetBranch
    # Returns branch name or $null

.OUTPUTS
    Boolean or String - Depending on -GetBranch parameter.
#>
function Test-GitRepository {
    param(
        [switch]$ThrowOnFailure,
        [switch]$GetBranch
    )
    
    try {
        $branch = git rev-parse --abbrev-ref HEAD 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Not in a git repository"
        }
        
        if ($GetBranch) {
            return $branch
        }
        
        Write-Host "✓ Git repository found, current branch: $branch" -ForegroundColor Green
        return $true
    }
    catch {
        if ($ThrowOnFailure) {
            throw "Not in a git repository or git not available. Please run this script from within the repository directory."
        }
        
        if ($GetBranch) {
            return $null
        }
        
        Write-Host "✗ Error: Not in a git repository or git not available" -ForegroundColor Red
        Write-Host "Please run this script from within the repository directory." -ForegroundColor Yellow
        return $false
    }
}

<#
.SYNOPSIS
    Tests if .NET SDK is available.

.DESCRIPTION
    Checks for the presence of the .NET SDK and optionally validates minimum version.

.PARAMETER MinimumVersion
    Optional minimum version required (e.g., "8.0").

.PARAMETER ThrowOnFailure
    If true, throws an exception when validation fails.

.EXAMPLE
    Test-DotNetSDK -MinimumVersion "8.0" -ThrowOnFailure

.OUTPUTS
    Boolean - $true if .NET SDK is available and meets version requirements.
#>
function Test-DotNetSDK {
    param(
        [string]$MinimumVersion = "",
        [switch]$ThrowOnFailure
    )
    
    try {
        $dotnetVersion = dotnet --version 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw ".NET SDK not available"
        }
        
        if ($MinimumVersion) {
            $current = [version]$dotnetVersion
            $minimum = [version]$MinimumVersion
            
            if ($current -lt $minimum) {
                if ($ThrowOnFailure) {
                    throw ".NET SDK version $dotnetVersion is below minimum required version $MinimumVersion"
                }
                Write-Host "✗ .NET SDK version $dotnetVersion is below minimum required version $MinimumVersion" -ForegroundColor Red
                return $false
            }
        }
        
        Write-Host "✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green
        return $true
    }
    catch {
        if ($ThrowOnFailure) {
            throw ".NET SDK not available. Please install from: https://dotnet.microsoft.com/download"
        }
        Write-Host "✗ .NET SDK not available" -ForegroundColor Red
        Write-Host "Please install .NET SDK: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        return $false
    }
}

<#
.SYNOPSIS
    Tests if Node.js is available.

.DESCRIPTION
    Checks for the presence of Node.js and optionally validates minimum version.

.PARAMETER MinimumVersion
    Optional minimum version required (e.g., "18.0").

.PARAMETER ThrowOnFailure
    If true, throws an exception when validation fails.

.EXAMPLE
    Test-NodeJS -MinimumVersion "18.0" -ThrowOnFailure

.OUTPUTS
    Boolean - $true if Node.js is available and meets version requirements.
#>
function Test-NodeJS {
    param(
        [string]$MinimumVersion = "",
        [switch]$ThrowOnFailure
    )
    
    try {
        $nodeVersion = node --version 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Node.js not available"
        }
        
        # Remove 'v' prefix if present
        $nodeVersion = $nodeVersion.TrimStart('v')
        
        if ($MinimumVersion) {
            $current = [version]$nodeVersion
            $minimum = [version]$MinimumVersion
            
            if ($current -lt $minimum) {
                if ($ThrowOnFailure) {
                    throw "Node.js version $nodeVersion is below minimum required version $MinimumVersion"
                }
                Write-Host "✗ Node.js version $nodeVersion is below minimum required version $MinimumVersion" -ForegroundColor Red
                return $false
            }
        }
        
        Write-Host "✓ Node.js version: v$nodeVersion" -ForegroundColor Green
        return $true
    }
    catch {
        if ($ThrowOnFailure) {
            throw "Node.js not available. Please install from: https://nodejs.org/"
        }
        Write-Host "✗ Node.js not available" -ForegroundColor Red
        Write-Host "Please install Node.js: https://nodejs.org/" -ForegroundColor Yellow
        return $false
    }
}

# Export all public functions
Export-ModuleMember -Function @(
    'Test-GitHubCLI',
    'Test-GitRepository',
    'Test-DotNetSDK',
    'Test-NodeJS'
)
