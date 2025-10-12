<#
.SYNOPSIS
    Cross-platform detection and utility functions.

.DESCRIPTION
    This module provides platform detection (Windows, Linux, macOS) and cross-platform
    utility functions for path handling and file permissions. It ensures compatibility
    across different PowerShell versions and operating systems.

.NOTES
    Module: Core.Platform
    Version: 1.0.0
#>

<#
.SYNOPSIS
    Initializes platform detection variables for cross-PowerShell version compatibility.

.DESCRIPTION
    Sets script-level variables $IsWindows, $IsLinux, and $IsMacOS for platform detection.
    Provides fallback detection for older PowerShell versions that don't have built-in
    platform variables.

.EXAMPLE
    Initialize-PlatformDetection
    if ($script:IsWindows) { Write-Host "Running on Windows" }
#>
function Initialize-PlatformDetection {
    # Define platform detection variables for compatibility with older PowerShell versions
    if (-not (Test-Path variable:IsWindows)) {
        try {
            $script:IsWindows = ([System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT)
        }
        catch {
            # Fallback for very old PowerShell versions
            $script:IsWindows = ($env:OS -eq "Windows_NT")
        }
    }
    else {
        $script:IsWindows = $IsWindows
    }
    
    if (-not (Test-Path variable:IsLinux)) {
        try {
            $script:IsLinux = ([System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Unix) -and 
                (-not [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX))
        }
        catch {
            # Fallback detection
            $script:IsLinux = (-not $script:IsWindows -and -not $script:IsMacOS -and (Test-Path "/proc/version"))
        }
    }
    else {
        $script:IsLinux = $IsLinux
    }
    
    if (-not (Test-Path variable:IsMacOS)) {
        try {
            $script:IsMacOS = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)
        }
        catch {
            # Fallback detection  
            $script:IsMacOS = (-not $script:IsWindows -and (Test-Path "/System/Library/CoreServices/SystemVersion.plist"))
        }
    }
    else {
        $script:IsMacOS = $IsMacOS
    }
    
    # Export to parent scope for easy access
    $caller = Get-Variable -Scope 1 -ErrorAction SilentlyContinue
    if ($caller) {
        Set-Variable -Name "IsWindows" -Value $script:IsWindows -Scope 1 -ErrorAction SilentlyContinue
        Set-Variable -Name "IsLinux" -Value $script:IsLinux -Scope 1 -ErrorAction SilentlyContinue
        Set-Variable -Name "IsMacOS" -Value $script:IsMacOS -Scope 1 -ErrorAction SilentlyContinue
    }
}

<#
.SYNOPSIS
    Constructs a cross-platform file path from multiple parts.

.DESCRIPTION
    Joins path parts using the appropriate path separator for the current platform.
    This is a convenience function that wraps Join-Path for multiple segments.

.PARAMETER PathParts
    Array of path segments to join.

.EXAMPLE
    $path = Join-CrossPlatformPath @("src", "c-sharp", "NotebookAutomation.sln")

.OUTPUTS
    String - The combined path using platform-appropriate separators.
#>
function Join-CrossPlatformPath {
    param([string[]]$PathParts)
    
    if (-not $PathParts -or $PathParts.Length -eq 0) {
        return ""
    }
    
    $result = $PathParts[0]
    for ($i = 1; $i -lt $PathParts.Length; $i++) {
        $result = Join-Path $result $PathParts[$i]
    }
    return $result
}

<#
.SYNOPSIS
    Sets executable permission on a file for Unix-based systems.

.DESCRIPTION
    On Linux and macOS, makes a file executable using chmod +x.
    On Windows, this function does nothing.

.PARAMETER FilePath
    The path to the file to make executable.

.EXAMPLE
    Set-ExecutablePermission -FilePath "./na-linux-x64"
#>
function Set-ExecutablePermission {
    param([string]$FilePath)
    
    # Initialize platform detection if not already done
    if (-not (Get-Variable -Name "IsWindows" -Scope Script -ErrorAction SilentlyContinue)) {
        Initialize-PlatformDetection
    }
    
    if (-not $script:IsWindows) {
        try {
            if (Get-Command "chmod" -ErrorAction SilentlyContinue) {
                chmod +x $FilePath 2>$null
            }
        }
        catch {
            # Silently fail - not critical
        }
    }
}

<#
.SYNOPSIS
    Gets the current platform name as a string.

.DESCRIPTION
    Returns "windows", "linux", "macos", or "unknown" based on platform detection.

.EXAMPLE
    $platform = Get-PlatformName
    Write-Host "Running on: $platform"

.OUTPUTS
    String - The platform name (windows, linux, macos, or unknown).
#>
function Get-PlatformName {
    # Initialize platform detection if not already done
    if (-not (Get-Variable -Name "IsWindows" -Scope Script -ErrorAction SilentlyContinue)) {
        Initialize-PlatformDetection
    }
    
    if ($script:IsWindows) { return "windows" }
    elseif ($script:IsLinux) { return "linux" }
    elseif ($script:IsMacOS) { return "macos" }
    else { return "unknown" }
}

# Initialize platform detection when module is imported
Initialize-PlatformDetection

# Export all public functions and variables
Export-ModuleMember -Function @(
    'Initialize-PlatformDetection',
    'Join-CrossPlatformPath',
    'Set-ExecutablePermission',
    'Get-PlatformName'
) -Variable @(
    'IsWindows',
    'IsLinux',
    'IsMacOS'
)
