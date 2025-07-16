# PowerShell script to download the latest notebook-automation artifact from GitHub Actions
# Requires GitHub CLI (https://cli.github.com/) and authentication (gh auth login)
# Works on Windows, macOS, and Linux with PowerShell (pwsh) installed
#
# Examples:
#   .\download-latest-artifact.ps1                              # Download complete plugin package
#   pwsh .\download-latest-artifact.ps1 -ListOnly               # List available artifacts without downloading
#
# Parameters:
#   -Workflow: GitHub Actions workflow file name (default: ci-cross-platform.yml)
#   -Version: Specific SemVer to download (not supported for this artifact type)
#   -ListOnly: Only list available artifacts without downloading
#
# Note: Downloads the complete notebook-automation-obsidian-plugin artifact to ../dist/notebook-automation/
#       This includes the Obsidian plugin files (main.js, manifest.json, styles.css, default-config.json, etc.) and 
#       all platform executables (na-win-x64.exe, na-macos-arm64, na-linux-x64, etc.)
#       Ready for direct installation into Obsidian plugins folder.

param(
    [string]$Workflow = "ci-cross-platform.yml",
    [string]$Version = "",  # Version filtering not supported for this artifact type
    [switch]$ListOnly  # Only list available artifacts without downloading
)

Write-Host "Downloading notebook-automation-obsidian-plugin package:" -ForegroundColor Green
Write-Host "  Workflow: $Workflow"
if ($Version) {
    Write-Host "  Version: $Version (note: version filtering not supported for this artifact type)"
}
if ($ListOnly) {
    Write-Host "  Mode: List only (no download)"
}
else {
    Write-Host "  Mode: Download to ../dist/notebook-automation/"
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
}
catch {
    Write-Host "✗ Error: Not in a git repository or git not available" -ForegroundColor Red
    Write-Host "Please run this script from within the repository directory."
    exit 1
}

# Check if GitHub CLI is available and authenticated
try {
    $authStatus = gh auth status 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ GitHub CLI authenticated"
    }
    else {
        Write-Host "✗ GitHub CLI not authenticated" -ForegroundColor Red
        Write-Host "Please run 'gh auth login' first."
        exit 1
    }
}
catch {
    Write-Host "✗ GitHub CLI not available" -ForegroundColor Red
    Write-Host "Please install GitHub CLI: https://cli.github.com/"
    exit 1
}

Write-Host ""

# Get the latest successful run ID for the workflow on the current branch
# Correct the gh run list command to properly format the --json flag
$run = gh run list --workflow $Workflow --branch $branch --limit 1 --json "databaseId,status,conclusion" | ConvertFrom-Json

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
}
catch {
    Write-Host "Error fetching artifacts: $($_.Exception.Message)"
    Write-Host "Trying alternative method..."
    
    # Alternative: List all artifacts and filter by run
    try {
        $allArtifactsJson = gh api repos/:owner/:repo/actions/artifacts --jq '.artifacts'
        $allArtifacts = $allArtifactsJson | ConvertFrom-Json
        $artifacts = $allArtifacts | Where-Object { $_.workflow_run.id -eq $runId }
    }
    catch {
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
    $_.name -eq "notebook-automation-obsidian-plugin" 
} | Sort-Object -Property created_at -Descending | Select-Object -First 1

if (-not $filteredArtifacts) {
    Write-Host "No suitable artifact found containing the notebook-automation-obsidian-plugin." -ForegroundColor Yellow
    exit 1
}

# Apply version filter if specified
if ($Version) {
    Write-Host "Version filtering not supported for this artifact type." -ForegroundColor Yellow
    Write-Host "The notebook-automation-obsidian-plugin artifact contains the latest build from the workflow run."
    Write-Host "Use specific workflow run or tag-based releases for version control."
}

# Use the notebook-automation-obsidian-plugin artifact
Write-Host "Using the notebook-automation-obsidian-plugin artifact (contains complete plugin package)..."

if (-not $filteredArtifacts) {
    Write-Host "No notebook-automation-obsidian-plugin artifact found"
    Write-Host "Available artifacts:"
    $artifacts | ForEach-Object {
        Write-Host "  - $($_.name)"
    }
    exit 1
}

Write-Host "Found notebook-automation-obsidian-plugin artifact to download:"
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
        
        # Extract the complete plugin package directly - the artifact contains the plugin directory structure
        Expand-Archive -Path "$DistPath/$($artifact.name).zip" -DestinationPath $DistPath -Force
        
        Write-Host "✓ Downloaded complete plugin package: $($artifact.name)"
        Write-Host "  Plugin files and executables available in: $DistPath"
        
        Remove-Item "$DistPath/$($artifact.name).zip"
        $downloadedAny = $true
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
        }
        else {
            $sizeKB = [math]::Round($_.Length / 1KB, 2)
            Write-Host "  📄 $relativePath ($sizeKB KB)"
        }
    }
    
    Write-Host ""
    Write-Host "Package summary:"
    
    # Check in the detected plugin directory
    $summaryPluginDir = $null
    $summaryPluginFiles = $null
    $summaryExecutables = $null
    
    # First, check if files are in a notebook-automation subdirectory
    $possibleDir = Join-Path $DistPath "notebook-automation"
    if (Test-Path $possibleDir) {
        $testFiles = Get-ChildItem -Path "$possibleDir" -File -ErrorAction SilentlyContinue
        if ($testFiles -and ($testFiles | Where-Object { $_.Name -in @("main.js", "manifest.json", "styles.css") }).Count -ge 3) {
            $summaryPluginDir = $possibleDir
        }
    }
    
    # If not found in subdirectory, check if files are directly in dist folder
    if (-not $summaryPluginDir) {
        $testFiles = Get-ChildItem -Path "$DistPath" -File -ErrorAction SilentlyContinue
        if ($testFiles -and ($testFiles | Where-Object { $_.Name -in @("main.js", "manifest.json", "styles.css") }).Count -ge 3) {
            $summaryPluginDir = $DistPath
        }
    }
    
    if ($summaryPluginDir) {
        # Check for required plugin files
        $requiredFiles = @("main.js", "manifest.json", "styles.css", "default-config.json", "metadata-schema.yml", "BaseBlockTemplate.yml", "chunk_summary_prompt.md", "final_summary_prompt.md")
        $summaryPluginFiles = Get-ChildItem -Path "$summaryPluginDir" -File | Where-Object { 
            $_.Name -in $requiredFiles
        }
        
        # Check for executables
        $summaryExecutables = Get-ChildItem -Path "$summaryPluginDir" -File | Where-Object { 
            $_.Name -like "na-*"
        }
        
        if ($summaryPluginFiles.Count -ge 4) {
            # At least the core files should be present
            Write-Host "  ✓ Found complete Obsidian plugin package:"
            Write-Host "    Plugin files: $($summaryPluginFiles.Count)/$($requiredFiles.Count)"
            Write-Host "    Executables: $($summaryExecutables.Count)"
            Write-Host ""
            Write-Host "  Plugin files found:"
            $summaryPluginFiles | ForEach-Object {
                Write-Host "    - $($_.Name)"
            }
            Write-Host ""
            Write-Host "  Installation: Copy the contents to your Obsidian plugins folder"
            if ($summaryPluginDir -ne $DistPath) {
                Write-Host "    Source: $summaryPluginDir"
            }
        }
        
        if ($summaryExecutables) {
            Write-Host ""
            Write-Host "  Available executables:"
            
            # Group by platform for better display
            $windowsExes = $summaryExecutables | Where-Object { $_.Name -like "na-win-*" }
            $macosExes = $summaryExecutables | Where-Object { $_.Name -like "na-macos-*" }
            $linuxExes = $summaryExecutables | Where-Object { $_.Name -like "na-linux-*" }
            
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
        }
    }
    else {
        Write-Host "  ✗ Plugin directory not found"
        Write-Host "  Contents of dist directory:"
        Get-ChildItem -Path "$DistPath" | ForEach-Object {
            Write-Host "    - $($_.Name)"
        }
    }
    
    # Show usage instructions
    Write-Host ""
    Write-Host "Usage Instructions:" -ForegroundColor Cyan
    if ($summaryPluginDir -and $summaryPluginDir -ne $DistPath) {
        Write-Host "  1. Copy the 'notebook-automation' directory to your Obsidian plugins folder"
        Write-Host "  2. Enable the plugin in Obsidian settings"
        Write-Host "  3. The plugin will automatically use the appropriate executable for your platform"
        Write-Host ""
        Write-Host "Manual executable usage:" -ForegroundColor Cyan
        Write-Host "  Windows x64:  $summaryPluginDir/na-win-x64.exe --help"
        Write-Host "  Windows ARM64: $summaryPluginDir/na-win-arm64.exe --help"
        Write-Host "  macOS x64:    $summaryPluginDir/na-macos-x64 --help"
        Write-Host "  macOS ARM64:  $summaryPluginDir/na-macos-arm64 --help"
        Write-Host "  Linux x64:    $summaryPluginDir/na-linux-x64 --help"
        Write-Host "  Linux ARM64:  $summaryPluginDir/na-linux-arm64 --help"
    }
    else {
        Write-Host "  1. Copy the contents of the dist directory to your Obsidian plugins folder"
        Write-Host "  2. Enable the plugin in Obsidian settings"
        Write-Host "  3. The plugin will automatically use the appropriate executable for your platform"
        Write-Host ""
        Write-Host "Manual executable usage:" -ForegroundColor Cyan
        Write-Host "  Windows x64:  $DistPath/na-win-x64.exe --help"
        Write-Host "  Windows ARM64: $DistPath/na-win-arm64.exe --help"
        Write-Host "  macOS x64:    $DistPath/na-macos-x64 --help"
        Write-Host "  macOS ARM64:  $DistPath/na-macos-arm64 --help"
        Write-Host "  Linux x64:    $DistPath/na-linux-x64 --help"
        Write-Host "  Linux ARM64:  $DistPath/na-linux-arm64 --help"
    }
}
else {
    Write-Host "No artifacts were downloaded."
}

# Ensure Obsidian plugin files are extracted - check multiple possible locations
$pluginDir = $null
$corePluginFiles = $null

# First, check if files are in a notebook-automation subdirectory
$possiblePluginDir = Join-Path $DistPath "notebook-automation"
if (Test-Path $possiblePluginDir) {
    $testFiles = Get-ChildItem -Path "$possiblePluginDir" -File -ErrorAction SilentlyContinue | Where-Object {
        $_.Name -in @("main.js", "manifest.json", "styles.css")
    }
    if ($testFiles -and $testFiles.Count -ge 3) {
        $pluginDir = $possiblePluginDir
        $corePluginFiles = $testFiles
    }
}

# If not found in subdirectory, check if files are directly in dist folder
if (-not $corePluginFiles) {
    $testFiles = Get-ChildItem -Path "$DistPath" -File -ErrorAction SilentlyContinue | Where-Object {
        $_.Name -in @("main.js", "manifest.json", "styles.css")
    }
    if ($testFiles -and $testFiles.Count -ge 3) {
        $pluginDir = $DistPath
        $corePluginFiles = $testFiles
    }
}

if (-not $corePluginFiles -or $corePluginFiles.Count -lt 3) {
    Write-Host "✗ Core Obsidian plugin files not found in the extracted artifact." -ForegroundColor Red
    Write-Host "Expected: main.js, manifest.json, styles.css"
    Write-Host "Searched in:"
    Write-Host "  - $possiblePluginDir"
    Write-Host "  - $DistPath"
    Write-Host "Contents of dist directory:"
    Get-ChildItem -Path "$DistPath" -Recurse | ForEach-Object {
        $relativePath = $_.FullName.Replace($DistPath, "")
        Write-Host "  $relativePath"
    }
    if ($corePluginFiles) {
        Write-Host "Found: $($corePluginFiles.Name -join ', ')"
    }
    exit 1
}

Write-Host "✓ Found core Obsidian plugin files:"
$corePluginFiles | ForEach-Object {
    Write-Host "  - $($_.Name)"
}

# Check for additional plugin files
$additionalFiles = Get-ChildItem -Path "$pluginDir" -File | Where-Object {
    $_.Name -in @("default-config.json", "metadata-schema.yml", "BaseBlockTemplate.yml", "chunk_summary_prompt.md", "final_summary_prompt.md")
}

if ($additionalFiles) {
    Write-Host ""
    Write-Host "✓ Found additional plugin files:"
    $additionalFiles | ForEach-Object {
        Write-Host "  - $($_.Name)"
    }
}
