# PowerShell script to build and deploy the Obsidian plugin to the test vault
# Works on Windows and macOS (with PowerShell Core)

param(
    [string]$PluginName = "notebook-automation",
    [string]$VaultPluginsPath = "../../tests/obsidian-vault/Obsidian Vault Test/.obsidian/plugins"
)

$ErrorActionPreference = "Stop"

Write-Host "Building the plugin..."

# Run the build command (adjust if you use yarn or a different script)
npm install
npm run build

# Define source and destination paths
$SourcePath = $PSScriptRoot
$DestPath = Join-Path -Path $PSScriptRoot -ChildPath $VaultPluginsPath
$DestPath = Join-Path -Path $DestPath -ChildPath $PluginName

Write-Host "Source path: $SourcePath"
Write-Host "Destination path: $DestPath"

# Ensure destination directory exists
if (-not (Test-Path $DestPath)) {
    Write-Host "Creating plugin directory at $DestPath"
    New-Item -ItemType Directory -Path $DestPath -Force | Out-Null
} else {
    Write-Host "Plugin directory already exists at $DestPath"
}


# Determine platform and set na executable source
$naSource = $null
if ($IsWindows) {
    $naSource = "../../dist/all-platform-executables-1.0.1-19/published-executables-windows-latest-x64-1.0.1-19/na.exe"
    if (-not (Test-Path $naSource)) {
        Write-Warning "Windows na executable not found at $naSource. Trying fallback locations..."
        # Try alternative Windows paths or skip
        $naSource = $null
    }
} elseif ($IsMacOS) {
    $naSource = "../../dist/all-platform-executables-1.0.1-19/published-executables-macos-latest-x64-1.0.1-19/na"
} else {
    # Linux - try x64 first, then arm64
    $naSource = "../../dist/all-platform-executables-1.0.1-19/published-executables-ubuntu-latest-x64-1.0.1-19/na"
    if (-not (Test-Path $naSource)) {
        $naSource = "../../dist/all-platform-executables-1.0.1-19/published-executables-ubuntu-latest-arm64-1.0.1-19/na"
    }
}

$FilesToCopy = @("main.js", "manifest.json")


# Ensure na executable is included in the deployment and copied directly to the plugin directory in the vault
if ($naSource -and (Test-Path $naSource)) {
    $naExecutableName = Split-Path $naSource -Leaf
    $vaultNaPath = Join-Path $DestPath $naExecutableName
    Copy-Item $naSource $vaultNaPath -Force
    Write-Host "Copied platform-specific na executable ($naExecutableName) to plugin vault directory: $vaultNaPath"
    # Also copy to local plugin source directory for dev/test parity
    $localNaPath = Join-Path $SourcePath $naExecutableName
    Copy-Item $naSource $localNaPath -Force
    Write-Host "Copied platform-specific na executable ($naExecutableName) to plugin source directory: $localNaPath"
    $FilesToCopy += @($naExecutableName)
} else {
    Write-Warning "na executable not found at $naSource. Plugin may not function properly without the CLI executable."
}

# Copy all required files
foreach ($file in $FilesToCopy) {
    $src = Join-Path $SourcePath $file
    $dst = Join-Path $DestPath $file
    if (Test-Path $src) {
        Copy-Item $src $dst -Force
        Write-Host "Copied $file to $DestPath"
    } else {
        Write-Warning "$file not found in $SourcePath"
    }
}


# Make sure na is executable on non-Windows (also in local plugin dir for dev/test)
if (-not $IsWindows) {
    $naPath = Join-Path $DestPath "na"
    $localNaPath = Join-Path $SourcePath "na"
    if (Test-Path $naPath) {
        chmod +x $naPath
        Write-Host "Set executable permissions for na in plugin directory."
    }
    if (Test-Path $localNaPath) {
        chmod +x $localNaPath
        Write-Host "Set executable permissions for na in local source directory."
    }
}

Write-Host "Plugin deployed to $DestPath. Reload plugins in Obsidian to see changes."
