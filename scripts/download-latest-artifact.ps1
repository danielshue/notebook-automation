# PowerShell script to download the latest GitHub Actions artifacts for this repo/branch
# Requires GitHub CLI (https://cli.github.com/) and authentication (gh auth login)
# Works on Windows, macOS, and Linux with PowerShell (pwsh) installed
#
# Examples:
#   .\download-latest-artifact.ps1                              # Download all platform executables (auto-detects your platform)
#   pwsh .\download-latest-artifact.ps1 -Platform windows       # Download only Windows executables  
#   pwsh .\download-latest-artifact.ps1 -Platform ubuntu -Arch x64   # Download only Ubuntu executables (arch filter not needed for new format)
#   pwsh .\download-latest-artifact.ps1 -ListOnly               # List available artifacts without downloading
#   pwsh .\download-latest-artifact.ps1 -Version "1.0.1"        # Download artifacts for specific version (SemVer)
#   pwsh .\download-latest-artifact.ps1 -Platform all           # Download all platforms and architectures
#
# Parameters:
#   -Workflow: GitHub Actions workflow file name (default: ci-cross-platform.yml)
#   -Platform: Target platform - "all", "windows", "ubuntu", "macos" (default: all)
#   -Architecture: Target architecture - "all", "x64", "arm64" (Note: not needed for new format since each platform contains both architectures)
#   -Version: Specific SemVer to download (optional, downloads latest if not specified)
#   -ListOnly: Only list available artifacts without downloading
#
# Note: Downloads are placed in ../dist with all executables in a single directory
#       Executables are named like na-win-x64.exe, na-macos-arm64, na-linux-x64, etc.

param(
    [string]$Workflow = "ci-cross-platform.yml",
    [ValidateSet("all", "windows", "ubuntu", "macos")]
    [string]$Platform = "all",  # Options: "all", "windows", "ubuntu", "macos"
    [ValidateSet("all", "x64", "arm64")]
    [string]$Architecture = "all",  # Options: "all", "x64", "arm64"
    [string]$Version = "",  # Specific SemVer to download (optional)
    [switch]$ListOnly  # Only list available artifacts without downloading
)

# Auto-detect current platform and architecture if not specified
if ($Platform -eq "all") {
    # Detect current platform
    if ($IsWindows) {
        $Platform = "windows"
    } elseif ($IsMacOS) {
        $Platform = "macos"
    } elseif ($IsLinux) {
        $Platform = "ubuntu"
    } else {
        # Fallback detection
        $osInfo = [System.Environment]::OSVersion.Platform
        if ($osInfo -eq "Win32NT") {
            $Platform = "windows"
        } elseif ($osInfo -eq "Unix") {
            $unameOutput = uname -s 2>/dev/null
            if ($unameOutput -eq "Darwin") {
                $Platform = "macos"
            } else {
                $Platform = "ubuntu"
            }
        }
    }
    Write-Host "Auto-detected platform: $Platform" -ForegroundColor Yellow
}

if ($Architecture -eq "all") {
    # Detect current architecture
    $arch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
    if ($arch -eq "X64") {
        $Architecture = "x64"
    } elseif ($arch -eq "Arm64") {
        $Architecture = "arm64"
    } else {
        # Fallback detection
        $archInfo = $env:PROCESSOR_ARCHITECTURE
        if ($archInfo -like "*64*" -and $archInfo -notlike "*ARM*") {
            $Architecture = "x64"
        } elseif ($archInfo -like "*ARM*") {
            $Architecture = "arm64"
        } else {
            $Architecture = "x64"  # Default fallback
        }
    }
    Write-Host "Auto-detected architecture: $Architecture" -ForegroundColor Yellow
}

# Validation
if ($Platform -notin @("all", "windows", "ubuntu", "macos")) {
    Write-Host "Error: Platform must be one of: all, windows, ubuntu, macos" -ForegroundColor Red
    exit 1
}

if ($Architecture -notin @("all", "x64", "arm64")) {
    Write-Host "Error: Architecture must be one of: all, x64, arm64" -ForegroundColor Red
    exit 1
}

Write-Host "Downloading artifacts for:" -ForegroundColor Green
Write-Host "  Workflow: $Workflow"
Write-Host "  Platform: $Platform"
Write-Host "  Architecture: $Architecture"
if ($Version) {
    Write-Host "  Version: $Version"
}
if ($ListOnly) {
    Write-Host "  Mode: List only (no download)"
} else {
    Write-Host "  Mode: Download to $DistPath"
}
Write-Host ""

# Ensure dist folder exists - use absolute path relative to script location
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DistPath = Join-Path (Split-Path -Parent $ScriptDir) "dist"
if (-not (Test-Path $DistPath)) {
    New-Item -ItemType Directory -Path $DistPath
}

# Delete contents of dist folder first (unless ListOnly)
if (-not $ListOnly) {
    Write-Host "Cleaning dist folder..."
    Get-ChildItem -Path $DistPath -Recurse | Remove-Item -Recurse -Force
}

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check if we're in a git repository
try {
    $branch = git rev-parse --abbrev-ref HEAD
    Write-Host "✓ Git repository found, current branch: $branch"
} catch {
    Write-Host "✗ Error: Not in a git repository or git not available" -ForegroundColor Red
    Write-Host "Please run this script from within the repository directory."
    exit 1
}

# Check if GitHub CLI is available and authenticated
try {
    $authStatus = gh auth status 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ GitHub CLI authenticated"
    } else {
        Write-Host "✗ GitHub CLI not authenticated" -ForegroundColor Red
        Write-Host "Please run 'gh auth login' first."
        exit 1
    }
} catch {
    Write-Host "✗ GitHub CLI not available" -ForegroundColor Red
    Write-Host "Please install GitHub CLI: https://cli.github.com/"
    exit 1
}

Write-Host ""

# Get the latest successful run ID for the workflow on the current branch
$run = gh run list --workflow $Workflow --branch $branch --limit 1 --json databaseId,status,conclusion | ConvertFrom-Json

if (-not $run -or -not $run[0].databaseId) {
    Write-Host "No workflow run found for $Workflow on branch $branch."
    exit 1
}

$runId = $run[0].databaseId
$runStatus = $run[0].status
$runConclusion = $run[0].conclusion

Write-Host "Found run $runId with status: $runStatus, conclusion: $runConclusion"

# Get all artifacts for this run using the correct command
try {
    $artifactsJson = gh api repos/:owner/:repo/actions/runs/$runId/artifacts --jq '.artifacts'
    $artifacts = $artifactsJson | ConvertFrom-Json
} catch {
    Write-Host "Error fetching artifacts: $($_.Exception.Message)"
    Write-Host "Trying alternative method..."
    
    # Alternative: List all artifacts and filter by run
    try {
        $allArtifactsJson = gh api repos/:owner/:repo/actions/artifacts --jq '.artifacts'
        $allArtifacts = $allArtifactsJson | ConvertFrom-Json
        $artifacts = $allArtifacts | Where-Object { $_.workflow_run.id -eq $runId }
    } catch {
        Write-Host "Failed to fetch artifacts with alternative method: $($_.Exception.Message)"
        exit 1
    }
}

if (-not $artifacts -or $artifacts.Count -eq 0) {
    Write-Host "No artifacts found for run $runId"
    exit 1
}

Write-Host "Available artifacts:"
$artifacts | ForEach-Object {
    $downloadable = if ($_.expired -eq $false) { "downloadable" } else { "expired" }
    Write-Host "  - $($_.name) ($downloadable, $([math]::Round($_.size_in_bytes / 1MB, 2)) MB)"
}

# Filter artifacts based on parameters
$filteredArtifacts = $artifacts | Where-Object { 
    $_.name -like "executables-*" -or $_.name -like "all-platform-executables-*"
}

# Apply version filter if specified
if ($Version) {
    Write-Host "Filtering for version: $Version"
    $filteredArtifacts = $filteredArtifacts | Where-Object { 
        $_.name -like "*$Version*"
    }
    
    if (-not $filteredArtifacts) {
        Write-Host "No artifacts found for version '$Version'. Available versions:" -ForegroundColor Yellow
        $artifacts | Where-Object { $_.name -like "*executables*" } | ForEach-Object {
            # Try to extract SemVer from artifact name
            if ($_.name -match "executables-.*?-(\d+\.\d+\.\d+.*?)$") {
                Write-Host "  - $($matches[1])"
            } elseif ($_.name -match "all-platform-executables-(\d+\.\d+\.\d+.*?)$") {
                Write-Host "  - $($matches[1])"
            }
        } | Sort-Object | Get-Unique
        exit 1
    }
}

# Apply platform filter
if ($Platform -ne "all") {
    $platformMap = @{
        "windows" = "win"
        "ubuntu" = "linux" 
        "macos" = "macos"
    }
    
    $platformName = $platformMap[$Platform.ToLower()]
    if ($platformName) {
        $filteredArtifacts = $filteredArtifacts | Where-Object { 
            $_.name -like "*executables-$platformName-*" -or $_.name -like "all-platform-executables-*"
        }
    }
}

# Apply architecture filter - not needed for new format since each platform artifact contains both architectures
# if ($Architecture -ne "all") {
#     $filteredArtifacts = $filteredArtifacts | Where-Object { 
#         $_.name -like "*$Architecture*" -or $_.name -like "all-platform-executables-*"
#     }
# }

if (-not $filteredArtifacts) {
    Write-Host "No matching artifacts found for Platform='$Platform', Architecture='$Architecture'"
    Write-Host "Available executable artifacts:"
    $artifacts | Where-Object { $_.name -like "*executables*" } | ForEach-Object {
        Write-Host "  - $($_.name)"
    }
    exit 1
}

Write-Host "Filtered artifacts to download:"
$filteredArtifacts | ForEach-Object {
    Write-Host "  - $($_.name)"
}

if ($ListOnly) {
    Write-Host "List-only mode. Exiting without downloading."
    exit 0
}

# Download each filtered artifact
$downloadedAny = $false
foreach ($artifact in $filteredArtifacts) {
    Write-Host "Downloading artifact '$($artifact.name)'..."
    
    if ($artifact.expired -eq $true) {
        Write-Host "✗ Skipped: $($artifact.name) - Artifact has expired"
        continue
    }
    
    try {
        # Use the artifact download command with the artifact ID
        gh api repos/:owner/:repo/actions/artifacts/$($artifact.id)/zip --method GET > "$DistPath/$($artifact.name).zip"
        
        # Extract directly to dist folder (all executables go in one place)
        $extractPath = $DistPath
        
        # Extract the zip file
        Expand-Archive -Path "$DistPath/$($artifact.name).zip" -DestinationPath $extractPath -Force
        Remove-Item "$DistPath/$($artifact.name).zip"
        
        $downloadedAny = $true
        Write-Host "✓ Downloaded: $($artifact.name)"
    }
    catch {
        Write-Host "✗ Failed to download: $($artifact.name) - $($_.Exception.Message)"
    }
}

if ($downloadedAny) {
    Write-Host ""
    Write-Host "Download completed! Contents of ${DistPath}:"
    Get-ChildItem -Path "$DistPath" -Recurse | ForEach-Object {
        $relativePath = $_.FullName.Replace((Resolve-Path $DistPath).Path, '')
        if ($_.PSIsContainer) {
            Write-Host "  📁 $relativePath"
        } else {
            $sizeKB = [math]::Round($_.Length / 1KB, 2)
            Write-Host "  📄 $relativePath ($sizeKB KB)"
        }
    }
    
    Write-Host ""
    Write-Host "Platform summary:"
    
    # Check for executables using the new naming convention (all in dist folder)
    $executables = Get-ChildItem -Path "$DistPath" -File | Where-Object { 
        $_.Name -like "na-*" -or $_.Name -eq "na" -or $_.Name -eq "na.exe" 
    }
    
    if ($executables) {
        Write-Host "  ✓ Found $($executables.Count) executable(s) in dist folder:"
        
        # Group by platform for better display
        $windowsExes = $executables | Where-Object { $_.Name -like "na-win-*" }
        $macosExes = $executables | Where-Object { $_.Name -like "na-macos-*" }
        $linuxExes = $executables | Where-Object { $_.Name -like "na-linux-*" }
        $otherExes = $executables | Where-Object { $_.Name -notlike "na-win-*" -and $_.Name -notlike "na-macos-*" -and $_.Name -notlike "na-linux-*" }
        
        if ($windowsExes) {
            Write-Host "    Windows:"
            $windowsExes | ForEach-Object {
                $sizeKB = [math]::Round($_.Length / 1KB, 2)
                Write-Host "      - $($_.Name) ($sizeKB KB)"
            }
        }
        
        if ($macosExes) {
            Write-Host "    macOS:"
            $macosExes | ForEach-Object {
                $sizeKB = [math]::Round($_.Length / 1KB, 2)
                Write-Host "      - $($_.Name) ($sizeKB KB)"
            }
        }
        
        if ($linuxExes) {
            Write-Host "    Linux:"
            $linuxExes | ForEach-Object {
                $sizeKB = [math]::Round($_.Length / 1KB, 2)
                Write-Host "      - $($_.Name) ($sizeKB KB)"
            }
        }
        
        if ($otherExes) {
            Write-Host "    Other:"
            $otherExes | ForEach-Object {
                $sizeKB = [math]::Round($_.Length / 1KB, 2)
                Write-Host "      - $($_.Name) ($sizeKB KB)"
            }
        }
    } else {
        Write-Host "  ✗ No executables found in dist folder"
    }
    
    # Show usage instructions
    Write-Host ""
    Write-Host "Usage Instructions:" -ForegroundColor Cyan
    
    # Find the actual SemVer from downloaded artifacts
    $downloadedVersion = ""
    $sampleArtifact = $filteredArtifacts | Select-Object -First 1
    if ($sampleArtifact.name -match "executables-.*?-(\d+\.\d+\.\d+.*?)$") {
        $downloadedVersion = $matches[1]
    } elseif ($sampleArtifact.name -match "all-platform-executables-(\d+\.\d+\.\d+.*?)$") {
        $downloadedVersion = $matches[1]
    }
    
    # Clean up version string - SemVer should be clean already
    if (-not $downloadedVersion -and $sampleArtifact.name -match "executables-.*?-(.+?)$") {
        $downloadedVersion = $matches[1]
    }
    
    if ($downloadedVersion) {
        Write-Host "  Windows x64:  $DistPath/na-win-x64.exe --help"
        Write-Host "  Windows ARM64: $DistPath/na-win-arm64.exe --help"
        Write-Host "  macOS x64:    $DistPath/na-macos-x64 --help"
        Write-Host "  macOS ARM64:  $DistPath/na-macos-arm64 --help"
        Write-Host "  Linux x64:    $DistPath/na-linux-x64 --help"
        Write-Host "  Linux ARM64:  $DistPath/na-linux-arm64 --help"
    } else {
        Write-Host "  Windows x64:  $DistPath/na-win-x64.exe --help"
        Write-Host "  Windows ARM64: $DistPath/na-win-arm64.exe --help"
        Write-Host "  macOS x64:    $DistPath/na-macos-x64 --help"
        Write-Host "  macOS ARM64:  $DistPath/na-macos-arm64 --help"
        Write-Host "  Linux x64:    $DistPath/na-linux-x64 --help"
        Write-Host "  Linux ARM64:  $DistPath/na-linux-arm64 --help"
    }
    
    # Show specific command for current platform if auto-detected
    if ($Platform -ne "all" -and $Architecture -ne "all") {
        $platformMap = @{
            "windows" = "win"
            "ubuntu" = "linux"
            "macos" = "macos"
        }
        
        $platformPrefix = $platformMap[$Platform.ToLower()]
        $extension = if ($Platform -eq "windows") { ".exe" } else { "" }
        
        Write-Host ""
        Write-Host "Quick start for your platform (auto-detected):" -ForegroundColor Green
        Write-Host "  $DistPath/na-$platformPrefix-$Architecture$extension --help"
    }
} else {
    Write-Host "No artifacts were downloaded."
}
