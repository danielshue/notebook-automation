<#
.SYNOPSIS
    GitHub Actions artifact download and management functions.

.DESCRIPTION
    This module provides functions to download and manage artifacts from GitHub Actions
    workflow runs, ensuring cross-platform executable availability.

.NOTES
    Module: GitHub.Artifacts
    Version: 1.0.0
#>

<#
.SYNOPSIS
    Downloads CI-built executables from GitHub Actions artifacts.

.DESCRIPTION
    Invokes the artifact download script to retrieve the latest successful build
    artifacts from GitHub Actions, including cross-platform executables.

.PARAMETER DownloadScriptPath
    Path to the download-latest-artifact.ps1 script.

.PARAMETER TargetPath
    Target directory for downloaded artifacts.

.PARAMETER ClearBeforeDownload
    If true, clears the target directory before downloading.

.PARAMETER ThrowOnFailure
    If true, throws an exception when download fails.

.EXAMPLE
    Invoke-CIArtifactDownload -DownloadScriptPath "./download-latest-artifact.ps1" -TargetPath "./dist"

.OUTPUTS
    String - Path to the directory containing downloaded executables, or $null on failure.
#>
function Invoke-CIArtifactDownload {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DownloadScriptPath,
        
        [Parameter(Mandatory = $true)]
        [string]$TargetPath,
        
        [switch]$ClearBeforeDownload,
        
        [switch]$ThrowOnFailure
    )
    
    Write-Host "📦 Downloading CI-built executables from GitHub Actions..." -ForegroundColor Cyan
    
    # Validate download script exists
    if (-not (Test-Path $DownloadScriptPath)) {
        $message = "Artifact download script not found: $DownloadScriptPath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $null
    }
    
    # Clear target directory if requested
    if ($ClearBeforeDownload -and (Test-Path $TargetPath)) {
        Write-Host "🗑️  Clearing existing target directory for clean CI artifact download..." -ForegroundColor Yellow
        try {
            Remove-Item -Path $TargetPath -Recurse -Force
        }
        catch {
            $message = "Failed to clear target directory: $($_.Exception.Message)"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "✗ $message" -ForegroundColor Red
            return $null
        }
    }
    
    # Ensure target directory exists
    if (-not (Test-Path $TargetPath)) {
        New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
    }
    
    # Run the download script
    try {
        Write-Host "   🔽 Running artifact download script..." -ForegroundColor Gray
        $originalLocation = Get-Location
        
        try {
            Set-Location (Split-Path $DownloadScriptPath -Parent)
            
            & pwsh -ExecutionPolicy Bypass -File $DownloadScriptPath
            
            if ($LASTEXITCODE -ne 0) {
                throw "Artifact download script failed with exit code $LASTEXITCODE"
            }
        }
        finally {
            Set-Location $originalLocation
        }
        
        # Find downloaded executables
        $executablePath = Find-DownloadedExecutables -TargetPath $TargetPath -ThrowOnFailure:$ThrowOnFailure
        
        if ($executablePath) {
            Write-Host "✅ CI artifacts downloaded successfully" -ForegroundColor Green
        }
        
        return $executablePath
    }
    catch {
        $message = "Failed to download artifacts: $($_.Exception.Message)"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $null
    }
}

<#
.SYNOPSIS
    Finds downloaded executables in artifact directory.

.DESCRIPTION
    Searches the artifact download directory for cross-platform executables,
    checking multiple possible locations.

.PARAMETER TargetPath
    The target directory where artifacts were downloaded.

.PARAMETER ThrowOnFailure
    If true, throws an exception when executables are not found.

.EXAMPLE
    $path = Find-DownloadedExecutables -TargetPath "./dist"

.OUTPUTS
    String - Path to directory containing executables, or $null if not found.
#>
function Find-DownloadedExecutables {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TargetPath,
        
        [switch]$ThrowOnFailure
    )
    
    # Check possible locations for executables
    $possiblePaths = @(
        (Join-Path $TargetPath "notebook-automation"),
        $TargetPath
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $executables = Get-ChildItem -Path $path -File | Where-Object { $_.Name -like "na-*" }
            
            if ($executables.Count -gt 0) {
                Write-Host "   ✅ Found $($executables.Count) executables in: $path" -ForegroundColor Green
                return $path
            }
        }
    }
    
    # No executables found
    $message = "No executables found in downloaded artifacts. Checked: $($possiblePaths -join ', ')"
    if ($ThrowOnFailure) {
        throw $message
    }
    Write-Host "✗ $message" -ForegroundColor Red
    return $null
}

<#
.SYNOPSIS
    Copies downloaded executables to target location.

.DESCRIPTION
    Copies CI-built executables from the download directory to the target directory,
    setting appropriate permissions for Unix systems.

.PARAMETER SourcePath
    Source directory containing executables.

.PARAMETER TargetPath
    Target directory for executables.

.PARAMETER SetExecutablePermissions
    If true, sets executable permissions on Unix systems.

.PARAMETER ThrowOnFailure
    If true, throws an exception when copy fails.

.EXAMPLE
    Copy-DownloadedExecutables -SourcePath "./dist/notebook-automation" -TargetPath "./dist"

.OUTPUTS
    Boolean - $true if copy succeeded, $false otherwise.
#>
function Copy-DownloadedExecutables {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourcePath,
        
        [Parameter(Mandatory = $true)]
        [string]$TargetPath,
        
        [switch]$SetExecutablePermissions,
        
        [switch]$ThrowOnFailure
    )
    
    # Get executables from source
    $executables = Get-ChildItem -Path $SourcePath -File | Where-Object { $_.Name -like "na-*" }
    
    if ($executables.Count -eq 0) {
        $message = "No executables found in source path: $SourcePath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    # Check if source and target are the same
    $normalizedSource = [System.IO.Path]::GetFullPath($SourcePath)
    $normalizedTarget = [System.IO.Path]::GetFullPath($TargetPath)
    
    if ($normalizedSource -eq $normalizedTarget) {
        Write-Host "✅ Executables already in target location - no copy needed" -ForegroundColor Green
        
        # Still set permissions if requested
        if ($SetExecutablePermissions) {
            foreach ($exe in $executables) {
                Set-ExecutableFilePermission -FilePath $exe.FullName
            }
        }
        
        return $true
    }
    
    # Ensure target directory exists
    if (-not (Test-Path $TargetPath)) {
        New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
    }
    
    # Copy executables
    try {
        foreach ($exe in $executables) {
            $targetFile = Join-Path $TargetPath $exe.Name
            Copy-Item -Path $exe.FullName -Destination $targetFile -Force
            Write-Host "   📋 Copied: $($exe.Name)" -ForegroundColor Gray
            
            # Set executable permission if requested
            if ($SetExecutablePermissions) {
                Set-ExecutableFilePermission -FilePath $targetFile
            }
        }
        
        Write-Host "✅ Copied $($executables.Count) executables to target location" -ForegroundColor Green
        return $true
    }
    catch {
        $message = "Failed to copy executables: $($_.Exception.Message)"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
}

<#
.SYNOPSIS
    Sets executable permission on a file (Unix only).

.PARAMETER FilePath
    Path to the file.

.EXAMPLE
    Set-ExecutableFilePermission -FilePath "./na-linux-x64"
#>
function Set-ExecutableFilePermission {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )
    
    # Only set permissions on non-Windows systems
    if ($PSVersionTable.Platform -ne 'Win32NT' -and $PSVersionTable.Platform -ne 'Windows') {
        try {
            if (Get-Command "chmod" -ErrorAction SilentlyContinue) {
                chmod +x $FilePath 2>$null
                Write-Host "   🔒 Set executable permission: $FilePath" -ForegroundColor DarkGray
            }
        }
        catch {
            Write-Host "   ⚠️  Could not set executable permission: $FilePath" -ForegroundColor Yellow
        }
    }
}

# Export all public functions
Export-ModuleMember -Function @(
    'Invoke-CIArtifactDownload',
    'Find-DownloadedExecutables',
    'Copy-DownloadedExecutables',
    'Set-ExecutableFilePermission'
)
