<#
.SYNOPSIS
    Obsidian plugin build operations module.

.DESCRIPTION
    This module provides functions for building and managing Obsidian plugins
    using npm, including installation, building, and deployment operations.

.NOTES
    Module: Build.PluginBuild
    Version: 1.0.0
#>

<#
.SYNOPSIS
    Installs npm dependencies for a plugin.

.PARAMETER PluginPath
    Path to the plugin directory containing package.json.

.PARAMETER ThrowOnFailure
    If true, throws an exception when install fails.

.EXAMPLE
    Invoke-PluginNpmInstall -PluginPath "./src/obsidian-plugin" -ThrowOnFailure

.OUTPUTS
    Boolean - $true if install succeeded, $false otherwise.
#>
function Invoke-PluginNpmInstall {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PluginPath,
        
        [switch]$ThrowOnFailure
    )
    
    if (-not (Test-Path $PluginPath)) {
        $message = "Plugin directory not found: $PluginPath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    $packageJson = Join-Path $PluginPath "package.json"
    if (-not (Test-Path $packageJson)) {
        $message = "package.json not found in: $PluginPath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    Push-Location $PluginPath
    try {
        npm install
        
        if ($LASTEXITCODE -ne 0) {
            $message = "npm install failed with exit code $LASTEXITCODE"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "✗ $message" -ForegroundColor Red
            return $false
        }
        
        return $true
    }
    finally {
        Pop-Location
    }
}

<#
.SYNOPSIS
    Builds an Obsidian plugin using npm.

.PARAMETER PluginPath
    Path to the plugin directory containing package.json.

.PARAMETER BuildCommand
    npm script to run for building. Default is "build".

.PARAMETER ThrowOnFailure
    If true, throws an exception when build fails.

.EXAMPLE
    Invoke-PluginBuild -PluginPath "./src/obsidian-plugin" -ThrowOnFailure

.OUTPUTS
    Boolean - $true if build succeeded, $false otherwise.
#>
function Invoke-PluginBuild {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PluginPath,
        
        [string]$BuildCommand = "build",
        
        [switch]$ThrowOnFailure
    )
    
    if (-not (Test-Path $PluginPath)) {
        $message = "Plugin directory not found: $PluginPath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    $packageJson = Join-Path $PluginPath "package.json"
    if (-not (Test-Path $packageJson)) {
        $message = "package.json not found in: $PluginPath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    Push-Location $PluginPath
    try {
        npm run $BuildCommand
        
        if ($LASTEXITCODE -ne 0) {
            $message = "npm run $BuildCommand failed with exit code $LASTEXITCODE"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "✗ $message" -ForegroundColor Red
            return $false
        }
        
        return $true
    }
    finally {
        Pop-Location
    }
}

<#
.SYNOPSIS
    Installs and builds an Obsidian plugin in one operation.

.PARAMETER PluginPath
    Path to the plugin directory containing package.json.

.PARAMETER BuildCommand
    npm script to run for building. Default is "build".

.PARAMETER SkipInstall
    Skip npm install if dependencies are already installed.

.PARAMETER ThrowOnFailure
    If true, throws an exception when operations fail.

.EXAMPLE
    Invoke-PluginInstallAndBuild -PluginPath "./src/obsidian-plugin" -ThrowOnFailure

.OUTPUTS
    Boolean - $true if both operations succeeded, $false otherwise.
#>
function Invoke-PluginInstallAndBuild {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PluginPath,
        
        [string]$BuildCommand = "build",
        
        [switch]$SkipInstall,
        
        [switch]$ThrowOnFailure
    )
    
    if (-not $SkipInstall) {
        $installResult = Invoke-PluginNpmInstall -PluginPath $PluginPath -ThrowOnFailure:$ThrowOnFailure
        if (-not $installResult) {
            return $false
        }
    }
    
    $buildResult = Invoke-PluginBuild -PluginPath $PluginPath -BuildCommand $BuildCommand -ThrowOnFailure:$ThrowOnFailure
    return $buildResult
}

<#
.SYNOPSIS
    Updates the version in plugin manifest and package.json files.

.PARAMETER PluginPath
    Path to the plugin directory.

.PARAMETER Version
    Version string to set (e.g., "1.0.0" or "1.0.0-beta.1").

.PARAMETER UpdatePackageJson
    Also update package.json version.

.PARAMETER UpdateManifestJson
    Also update manifest.json version.

.PARAMETER ThrowOnFailure
    If true, throws an exception when update fails.

.EXAMPLE
    Update-PluginVersion -PluginPath "./src/obsidian-plugin" -Version "1.0.0" -UpdatePackageJson -UpdateManifestJson

.OUTPUTS
    Boolean - $true if update succeeded, $false otherwise.
#>
function Update-PluginVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PluginPath,
        
        [Parameter(Mandatory = $true)]
        [string]$Version,
        
        [switch]$UpdatePackageJson,
        [switch]$UpdateManifestJson,
        
        [switch]$ThrowOnFailure
    )
    
    if (-not (Test-Path $PluginPath)) {
        $message = "Plugin directory not found: $PluginPath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    $updated = $false
    
    if ($UpdatePackageJson) {
        $packageJsonPath = Join-Path $PluginPath "package.json"
        if (Test-Path $packageJsonPath) {
            try {
                $packageJson = Get-Content $packageJsonPath | ConvertFrom-Json
                $packageJson.version = $Version
                $packageJson | ConvertTo-Json -Depth 100 | Set-Content $packageJsonPath
                Write-Host "✓ Updated package.json to version $Version" -ForegroundColor Green
                $updated = $true
            }
            catch {
                $message = "Failed to update package.json: $($_.Exception.Message)"
                if ($ThrowOnFailure) {
                    throw $message
                }
                Write-Host "✗ $message" -ForegroundColor Red
                return $false
            }
        }
    }
    
    if ($UpdateManifestJson) {
        $manifestJsonPath = Join-Path $PluginPath "manifest.json"
        if (Test-Path $manifestJsonPath) {
            try {
                $manifestJson = Get-Content $manifestJsonPath | ConvertFrom-Json
                $manifestJson.version = $Version
                $manifestJson | ConvertTo-Json -Depth 100 | Set-Content $manifestJsonPath
                Write-Host "✓ Updated manifest.json to version $Version" -ForegroundColor Green
                $updated = $true
            }
            catch {
                $message = "Failed to update manifest.json: $($_.Exception.Message)"
                if ($ThrowOnFailure) {
                    throw $message
                }
                Write-Host "✗ $message" -ForegroundColor Red
                return $false
            }
        }
    }
    
    return $updated
}

<#
.SYNOPSIS
    Deploys plugin files to a test vault.

.PARAMETER PluginPath
    Path to the plugin directory containing built files.

.PARAMETER VaultPath
    Path to the Obsidian vault's .obsidian/plugins directory.

.PARAMETER PluginName
    Name of the plugin directory in the vault.

.PARAMETER ThrowOnFailure
    If true, throws an exception when deployment fails.

.EXAMPLE
    Deploy-PluginToVault -PluginPath "./src/obsidian-plugin" -VaultPath "C:/MyVault/.obsidian/plugins" -PluginName "my-plugin"

.OUTPUTS
    Boolean - $true if deployment succeeded, $false otherwise.
#>
function Deploy-PluginToVault {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PluginPath,
        
        [Parameter(Mandatory = $true)]
        [string]$VaultPath,
        
        [Parameter(Mandatory = $true)]
        [string]$PluginName,
        
        [switch]$ThrowOnFailure
    )
    
    $distPath = Join-Path $PluginPath "dist"
    if (-not (Test-Path $distPath)) {
        $message = "Plugin dist directory not found: $distPath. Build the plugin first."
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    if (-not (Test-Path $VaultPath)) {
        $message = "Vault plugins directory not found: $VaultPath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    $targetPath = Join-Path $VaultPath $PluginName
    
    try {
        # Create target directory if it doesn't exist
        if (-not (Test-Path $targetPath)) {
            New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
        }
        
        # Copy plugin files
        $filesToCopy = @("main.js", "manifest.json", "styles.css")
        foreach ($file in $filesToCopy) {
            $sourcePath = Join-Path $distPath $file
            if (Test-Path $sourcePath) {
                Copy-Item -Path $sourcePath -Destination $targetPath -Force
                Write-Host "✓ Copied $file to vault" -ForegroundColor Green
            }
        }
        
        Write-Host "✓ Plugin deployed to: $targetPath" -ForegroundColor Green
        return $true
    }
    catch {
        $message = "Failed to deploy plugin: $($_.Exception.Message)"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
}

# Export all public functions
Export-ModuleMember -Function @(
    'Invoke-PluginNpmInstall',
    'Invoke-PluginBuild',
    'Invoke-PluginInstallAndBuild',
    'Update-PluginVersion',
    'Deploy-PluginToVault'
)
