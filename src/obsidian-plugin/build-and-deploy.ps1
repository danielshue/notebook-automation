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
$DestPath = Resolve-Path (Join-Path -Path $PSScriptRoot -ChildPath $VaultPluginsPath)
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


# Determine available executables and copy all of them
$distPath = "../../dist"
$executablesFound = @()


# Look for executables in dist folder (new flat structure)
if (Test-Path $distPath) {
    $availableExecutables = Get-ChildItem -Path $distPath -File | Where-Object { $_.Name -like "na-*" }
    
    if ($availableExecutables) {
        Write-Host "Found executables in flat dist structure:"
        foreach ($exe in $availableExecutables) {
            Write-Host "  - $($exe.Name)"
            $executablesFound += $exe.FullName
        }
    } else {
        Write-Host "No executables found in flat dist structure, trying old structure..."
        
        # Fallback: Look for old structure with version directories
        $versionDirs = Get-ChildItem -Path $distPath -Directory | Where-Object { $_.Name -like "*executables*" }
        foreach ($versionDir in $versionDirs) {
            $versionExes = Get-ChildItem -Path $versionDir.FullName -File -Recurse | Where-Object { $_.Name -like "na*" }
            foreach ($exe in $versionExes) {
                Write-Host "  - $($exe.Name) (from $($versionDir.Name))"
                $executablesFound += $exe.FullName
            }
        }
    }
}


# Only copy manifest.json, default-config.json, and main.js from the source directory
$FilesToCopy = @("manifest.json", "default-config.json")

# Always copy main.js from dist
$distMainJs = Join-Path $SourcePath "dist/main.js"
if (Test-Path $distMainJs) {
    $mainJsDest = Join-Path $DestPath "main.js"
    Copy-Item $distMainJs $mainJsDest -Force
    Write-Host "Copied main.js from dist to plugin directory: $mainJsDest"
    # Also copy to local plugin source directory for dev/test parity
    $localMainJs = Join-Path $SourcePath "main.js"
    Copy-Item $distMainJs $localMainJs -Force
    Write-Host "Copied main.js from dist to local source: $localMainJs"
    $FilesToCopy += @("main.js")
} else {
    Write-Warning "main.js not found in dist directory. Build may have failed."
}

# Copy all found executables
if ($executablesFound.Count -gt 0) {
    Write-Host "Copying $($executablesFound.Count) executables to plugin directories..."
    
    foreach ($exePath in $executablesFound) {
        $exeName = Split-Path $exePath -Leaf
        
        # Copy to vault plugin directory
        $vaultExePath = Join-Path $DestPath $exeName
        Copy-Item $exePath $vaultExePath -Force
        Write-Host "Copied $exeName to plugin vault directory: $vaultExePath"
        
        # Copy to dist directory for dev/test parity
        $localExePath = Join-Path $SourcePath "dist/$exeName"
        Copy-Item $exePath $localExePath -Force
        Write-Host "Copied $exeName to dist directory: $localExePath"
        
        # Do not add executables to $FilesToCopy; they are already copied above
        
        # Set executable permissions on non-Windows
        if (-not $IsWindows -and -not $exeName.EndsWith(".exe")) {
            if (Get-Command chmod -ErrorAction SilentlyContinue) {
                & chmod +x $vaultExePath
                & chmod +x $localExePath
                Write-Host "Set executable permissions for $exeName"
            }
        }
    }
} else {
    Write-Warning "No na executables found in $distPath. Plugin may not function properly without the CLI executables."
    Write-Host "Expected structure: $distPath/na-win-x64.exe, na-macos-arm64, etc."
}


# Copy all required files (manifest.json, default-config.json, main.js) from source directory
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

Write-Host "Plugin deployed to $DestPath. Reload plugins in Obsidian to see changes."
