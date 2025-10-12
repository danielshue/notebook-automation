<#
.SYNOPSIS
    Version management and synchronization module.

.DESCRIPTION
    This module provides functions for managing versions across multiple files
    (package.json, manifest.json, Git tags) and synchronizing version information.

.NOTES
    Module: Version.Management
    Version: 1.0.0
#>

<#
.SYNOPSIS
    Gets version data from package.json and manifest.json files.

.PARAMETER PluginPath
    Path to the plugin directory containing package.json and manifest.json.

.EXAMPLE
    $versionData = Get-VersionData -PluginPath "./src/obsidian-plugin"

.OUTPUTS
    Hashtable - Version data with ManifestExists, PackageExists, ManifestVersion, PackageVersion, and GitVersion keys.
#>
function Get-VersionData {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PluginPath
    )
    
    $manifestJsonPath = Join-Path $PluginPath "manifest.json"
    $packageJsonPath = Join-Path $PluginPath "package.json"
    
    $data = @{
        ManifestExists  = Test-Path $manifestJsonPath
        PackageExists   = Test-Path $packageJsonPath
        ManifestData    = $null
        PackageData     = $null
        ManifestVersion = $null
        PackageVersion  = $null
        GitVersion      = $null
    }
    
    if ($data.ManifestExists) {
        $data.ManifestData = Get-Content $manifestJsonPath | ConvertFrom-Json
        $data.ManifestVersion = $data.ManifestData.version
    }
    
    if ($data.PackageExists) {
        $data.PackageData = Get-Content $packageJsonPath | ConvertFrom-Json
        $data.PackageVersion = $data.PackageData.version
    }
    
    # Get Git version (latest tag)
    try {
        $gitTag = git describe --tags --abbrev=0 2>$null
        if ($LASTEXITCODE -eq 0 -and $gitTag) {
            # Remove 'v' prefix if present
            $data.GitVersion = $gitTag.TrimStart('v')
        }
    }
    catch {
        # No tags found or git not available
    }
    
    return $data
}

<#
.SYNOPSIS
    Synchronizes version across package.json and manifest.json.

.PARAMETER PluginPath
    Path to the plugin directory.

.PARAMETER Version
    Version string to synchronize to (e.g., "1.0.0" or "1.0.0-beta.1").

.PARAMETER ThrowOnFailure
    If true, throws an exception when synchronization fails.

.EXAMPLE
    Sync-PluginVersion -PluginPath "./src/obsidian-plugin" -Version "1.0.0" -ThrowOnFailure

.OUTPUTS
    Boolean - $true if synchronization succeeded, $false otherwise.
#>
function Sync-PluginVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PluginPath,
        
        [Parameter(Mandatory = $true)]
        [string]$Version,
        
        [switch]$ThrowOnFailure
    )
    
    $manifestJsonPath = Join-Path $PluginPath "manifest.json"
    $packageJsonPath = Join-Path $PluginPath "package.json"
    
    $success = $true
    
    # Update manifest.json
    if (Test-Path $manifestJsonPath) {
        try {
            $manifestJson = Get-Content $manifestJsonPath | ConvertFrom-Json
            $manifestJson.version = $Version
            $manifestJson | ConvertTo-Json -Depth 100 | Set-Content $manifestJsonPath
            Write-Host "✓ Updated manifest.json to version $Version" -ForegroundColor Green
        }
        catch {
            $message = "Failed to update manifest.json: $($_.Exception.Message)"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "✗ $message" -ForegroundColor Red
            $success = $false
        }
    }
    else {
        $message = "manifest.json not found: $manifestJsonPath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        $success = $false
    }
    
    # Update package.json
    if (Test-Path $packageJsonPath) {
        try {
            $packageJson = Get-Content $packageJsonPath | ConvertFrom-Json
            $packageJson.version = $Version
            $packageJson | ConvertTo-Json -Depth 100 | Set-Content $packageJsonPath
            Write-Host "✓ Updated package.json to version $Version" -ForegroundColor Green
        }
        catch {
            $message = "Failed to update package.json: $($_.Exception.Message)"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "✗ $message" -ForegroundColor Red
            $success = $false
        }
    }
    else {
        $message = "package.json not found: $packageJsonPath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        $success = $false
    }
    
    return $success
}

<#
.SYNOPSIS
    Creates a Git tag for a version.

.PARAMETER Version
    Version string (e.g., "1.0.0" or "1.0.0-beta.1").

.PARAMETER AddVPrefix
    Add 'v' prefix to the tag (e.g., "v1.0.0").

.PARAMETER Message
    Tag message (optional for annotated tags).

.PARAMETER ThrowOnFailure
    If true, throws an exception when tag creation fails.

.EXAMPLE
    New-GitVersionTag -Version "1.0.0" -AddVPrefix -Message "Release 1.0.0" -ThrowOnFailure

.OUTPUTS
    Boolean - $true if tag created successfully, $false otherwise.
#>
function New-GitVersionTag {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version,
        
        [switch]$AddVPrefix,
        
        [string]$Message = "",
        
        [switch]$ThrowOnFailure
    )
    
    $tag = if ($AddVPrefix) { "v$Version" } else { $Version }
    
    try {
        if ($Message) {
            # Create annotated tag
            git tag -a $tag -m $Message
        }
        else {
            # Create lightweight tag
            git tag $tag
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Created Git tag: $tag" -ForegroundColor Green
            return $true
        }
        else {
            $message = "git tag failed with exit code $LASTEXITCODE"
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
        Write-Host "✗ git tag error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

<#
.SYNOPSIS
    Pushes Git tags to remote repository.

.PARAMETER Tag
    Specific tag to push (optional - pushes all tags if not specified).

.PARAMETER ThrowOnFailure
    If true, throws an exception when push fails.

.EXAMPLE
    Push-GitVersionTag -Tag "v1.0.0" -ThrowOnFailure

.OUTPUTS
    Boolean - $true if push succeeded, $false otherwise.
#>
function Push-GitVersionTag {
    param(
        [string]$Tag = "",
        
        [switch]$ThrowOnFailure
    )
    
    try {
        if ($Tag) {
            git push origin $Tag
        }
        else {
            git push --tags
        }
        
        if ($LASTEXITCODE -eq 0) {
            $msg = if ($Tag) { "tag $Tag" } else { "all tags" }
            Write-Host "✓ Pushed $msg to remote" -ForegroundColor Green
            return $true
        }
        else {
            $message = "git push failed with exit code $LASTEXITCODE"
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
        Write-Host "✗ git push error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

<#
.SYNOPSIS
    Validates a version format.

.PARAMETER Version
    Version string to validate.

.PARAMETER AllowPreRelease
    Allow pre-release versions (e.g., "1.0.0-beta.1").

.EXAMPLE
    $isValid = Test-VersionFormat -Version "1.0.0" -AllowPreRelease

.OUTPUTS
    Boolean - $true if version format is valid, $false otherwise.
#>
function Test-VersionFormat {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version,
        
        [switch]$AllowPreRelease
    )
    
    if ($AllowPreRelease) {
        # Allow semantic versioning with pre-release tags
        # e.g., 1.0.0, 1.0.0-beta.1, 1.0.0-rc.2
        return $Version -match '^(\d+)\.(\d+)\.(\d+)(-[a-zA-Z0-9\.\-]+)?$'
    }
    else {
        # Only allow standard semantic versioning
        # e.g., 1.0.0
        return $Version -match '^(\d+)\.(\d+)\.(\d+)$'
    }
}

<#
.SYNOPSIS
    Gets the current Git commit SHA.

.PARAMETER Short
    Return short SHA (8 characters).

.PARAMETER ThrowOnFailure
    If true, throws an exception when command fails.

.EXAMPLE
    $sha = Get-GitCommitSha -Short

.OUTPUTS
    String - Git commit SHA, or $null on failure.
#>
function Get-GitCommitSha {
    param(
        [switch]$Short,
        [switch]$ThrowOnFailure
    )
    
    try {
        if ($Short) {
            $sha = git rev-parse --short HEAD
        }
        else {
            $sha = git rev-parse HEAD
        }
        
        if ($LASTEXITCODE -eq 0) {
            return $sha.Trim()
        }
        else {
            $message = "git rev-parse failed with exit code $LASTEXITCODE"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "✗ $message" -ForegroundColor Red
            return $null
        }
    }
    catch {
        if ($ThrowOnFailure) {
            throw
        }
        Write-Host "✗ git rev-parse error: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Export all public functions
Export-ModuleMember -Function @(
    'Get-VersionData',
    'Sync-PluginVersion',
    'New-GitVersionTag',
    'Push-GitVersionTag',
    'Test-VersionFormat',
    'Get-GitCommitSha'
)
