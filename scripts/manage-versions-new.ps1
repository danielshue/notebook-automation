#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Comprehensive version management for CLI and Obsidian plugin components.

.DESCRIPTION
    This script provides unified version management across the entire notebook-automation
    project, ensuring consistency between CLI and plugin components while maintaining
    Obsidian plugin compatibility. When no version is specified, it automatically
    increments the current version based on the specified type.

.PARAMETER Version
    The version to set (e.g., "0.1.0-beta.5", "0.1.0"). If not specified, the current
    version will be automatically incremented based on the Type parameter.

.PARAMETER Type
    The type of version update: "beta", "stable", or "patch"
    - "beta": Increments beta number (0.1.0-beta.4 → 0.1.0-beta.5) or creates new beta (0.1.0 → 0.1.1-beta.1)
    - "stable": Removes prerelease suffix (0.1.0-beta.4 → 0.1.0) or increments minor (0.1.0 → 0.2.0)
    - "patch": Increments patch number (0.1.0 → 0.1.1)

.PARAMETER CreateRelease
    Whether to create a GitHub release after tagging

.PARAMETER PreRelease
    Whether to mark the GitHub release as a pre-release (for beta versions)

.PARAMETER SyncOnly
    Only synchronize versions without creating releases

.EXAMPLE
    .\scripts\manage-versions.ps1 -Type "beta" -CreateRelease -PreRelease
    Auto-increments beta version (0.1.0-beta.4 → 0.1.0-beta.5)
    
.EXAMPLE
    .\scripts\manage-versions.ps1 -Type "stable" -CreateRelease
    Promotes beta to stable (0.1.0-beta.4 → 0.1.0)

.EXAMPLE
    .\scripts\manage-versions.ps1 -Type "patch" -CreateRelease
    Increments patch version (0.1.0 → 0.1.1)
    
.EXAMPLE
    .\scripts\manage-versions.ps1 -Version "0.1.0-beta.5" -Type "beta" -CreateRelease -PreRelease
    Uses specific version instead of auto-increment
    
.EXAMPLE
    .\scripts\manage-versions.ps1 -Version "0.1.0" -Type "stable" -CreateRelease
    Uses specific version for stable release

.EXAMPLE
    .\scripts\manage-versions.ps1 -Type "beta" -SyncOnly
    Auto-increments beta version without creating release
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$Version,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("beta", "stable", "patch")]
    [string]$Type,
    
    [switch]$CreateRelease,
    [switch]$PreRelease,
    [switch]$SyncOnly
)

$ErrorActionPreference = "Stop"

# Define paths
$RepoRoot = Get-Location
$PluginRoot = Join-Path $RepoRoot "src\obsidian-plugin"
$CliRoot = Join-Path $RepoRoot "src\c-sharp"
$ManifestPath = Join-Path $PluginRoot "manifest.json"
$PackageJsonPath = Join-Path $PluginRoot "package.json"
$GitVersionPath = Join-Path $RepoRoot "GitVersion.yml"
$DirectoryBuildPropsPath = Join-Path $CliRoot "Directory.Build.props"

# Function to get current version from manifest.json
function Get-CurrentVersion {
    if (Test-Path $ManifestPath) {
        $manifest = Get-Content $ManifestPath | ConvertFrom-Json
        return $manifest.version
    } else {
        throw "manifest.json not found at: $ManifestPath"
    }
}

# Function to increment version based on type
function Get-NextVersion {
    param(
        [string]$CurrentVersion,
        [string]$Type
    )
    
    Write-Host "Current version: $CurrentVersion" -ForegroundColor Yellow
    
    # Parse current version
    if ($CurrentVersion -match '^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z]+)\.(\d+))?$') {
        $major = [int]$matches[1]
        $minor = [int]$matches[2]
        $patch = [int]$matches[3]
        $prerelease = $matches[4]
        $build = if ($matches[5]) { [int]$matches[5] } else { 0 }
        
        switch ($Type) {
            "beta" {
                if ($prerelease -eq "beta") {
                    # Increment beta build number
                    $nextVersion = "$major.$minor.$patch-beta.$($build + 1)"
                    Write-Host "Incrementing beta build: $CurrentVersion → $nextVersion" -ForegroundColor Green
                    return $nextVersion
                } else {
                    # Start new beta series from stable version
                    $nextVersion = "$major.$minor.$($patch + 1)-beta.1"
                    Write-Host "Starting new beta series: $CurrentVersion → $nextVersion" -ForegroundColor Green
                    return $nextVersion
                }
            }
            "stable" {
                if ($prerelease -eq "beta") {
                    # Promote beta to stable
                    $nextVersion = "$major.$minor.$patch"
                    Write-Host "Promoting beta to stable: $CurrentVersion → $nextVersion" -ForegroundColor Green
                    return $nextVersion
                } else {
                    # Increment minor version for new stable release
                    $nextVersion = "$major.$($minor + 1).0"
                    Write-Host "Incrementing minor version: $CurrentVersion → $nextVersion" -ForegroundColor Green
                    return $nextVersion
                }
            }
            "patch" {
                if ($prerelease) {
                    throw "Cannot create patch release from prerelease version. Use 'stable' type first."
                } else {
                    # Increment patch version
                    $nextVersion = "$major.$minor.$($patch + 1)"
                    Write-Host "Incrementing patch version: $CurrentVersion → $nextVersion" -ForegroundColor Green
                    return $nextVersion
                }
            }
        }
    } else {
        throw "Invalid version format: $CurrentVersion. Expected format: x.y.z or x.y.z-prerelease.build"
    }
}

# Determine the target version
if ($Version) {
    $targetVersion = $Version
    Write-Host "Using specified version: $targetVersion" -ForegroundColor Yellow
} else {
    $currentVersion = Get-CurrentVersion
    $targetVersion = Get-NextVersion -CurrentVersion $currentVersion -Type $Type
    Write-Host "Auto-generated version: $targetVersion" -ForegroundColor Yellow
}

Write-Host "🔄 Managing versions for CLI and Plugin components" -ForegroundColor Green
Write-Host "Target version: $targetVersion" -ForegroundColor Yellow
Write-Host "Type: $Type" -ForegroundColor Yellow

# Step 1: Validate version format
if (-not ($targetVersion -match '^\d+\.\d+\.\d+(-\w+\.\d+)?$')) {
    throw "Invalid version format: $targetVersion. Expected format: x.y.z or x.y.z-prerelease.build"
}

# Step 2: Update Plugin files (manifest.json and package.json)
Write-Host ""
Write-Host "📱 Updating Plugin files..." -ForegroundColor Blue

# Update manifest.json
if (Test-Path $ManifestPath) {
    $manifest = Get-Content $ManifestPath | ConvertFrom-Json
    $manifest.version = $targetVersion
    $manifest | ConvertTo-Json -Depth 10 | Set-Content $ManifestPath
    Write-Host "✅ manifest.json updated to: $targetVersion"
} else {
    throw "manifest.json not found at: $ManifestPath"
}

# Update package.json
if (Test-Path $PackageJsonPath) {
    $packageJson = Get-Content $PackageJsonPath | ConvertFrom-Json
    $packageJson.version = $targetVersion
    $packageJson | ConvertTo-Json -Depth 10 | Set-Content $PackageJsonPath
    Write-Host "✅ package.json updated to: $targetVersion"
} else {
    throw "package.json not found at: $PackageJsonPath"
}

# Step 3: Update CLI versioning
Write-Host ""
Write-Host "💻 Updating CLI versioning..." -ForegroundColor Blue

# Update GitVersion.yml
$gitVersionContent = @"
next-version: $targetVersion
mode: ContinuousDelivery
branches:
  main:
    mode: ContinuousDelivery
    tag: ''
  develop:
    mode: ContinuousDelivery
    tag: 'beta'
"@

Set-Content -Path $GitVersionPath -Value $gitVersionContent
Write-Host "✅ GitVersion.yml updated to: $targetVersion"

# Update Directory.Build.props fallback version
if (Test-Path $DirectoryBuildPropsPath) {
    $directoryBuildContent = Get-Content $DirectoryBuildPropsPath -Raw
    $updatedContent = $directoryBuildContent -replace 'GitVersion_SemVer>.*?</GitVersion_SemVer>', "GitVersion_SemVer>$targetVersion</GitVersion_SemVer>"
    
    # If the GitVersion_SemVer doesn't exist, add it
    if ($updatedContent -notmatch 'GitVersion_SemVer>') {
        $updatedContent = $updatedContent -replace '(GitVersion_SemVer>0\.1\.0-fallback)', "$targetVersion"
    }
    
    Set-Content -Path $DirectoryBuildPropsPath -Value $updatedContent
    Write-Host "✅ Directory.Build.props updated"
}

# Step 4: Build and validate
Write-Host ""
Write-Host "🔨 Building and validating..." -ForegroundColor Blue

# Build CLI
Write-Host "Building CLI..."
try {
    dotnet build $CliRoot\NotebookAutomation.sln --configuration Release | Out-Null
    Write-Host "✅ CLI build successful"
} catch {
    Write-Error "CLI build failed: $($_.Exception.Message)"
}

# Build Plugin
Write-Host "Building Plugin..."
Push-Location $PluginRoot
try {
    npm run build | Out-Null
    Write-Host "✅ Plugin build successful"
} catch {
    Write-Error "Plugin build failed: $($_.Exception.Message)"
} finally {
    Pop-Location
}

# Step 5: Version verification
Write-Host ""
Write-Host "🔍 Verifying synchronized versions..." -ForegroundColor Green

# Check all version files
$manifest = Get-Content $ManifestPath | ConvertFrom-Json
$packageJson = Get-Content $PackageJsonPath | ConvertFrom-Json
$gitVersionContent = Get-Content $GitVersionPath

Write-Host "  Plugin manifest.json: $($manifest.version)" -ForegroundColor White
Write-Host "  Plugin package.json: $($packageJson.version)" -ForegroundColor White
Write-Host "  GitVersion.yml: $($gitVersionContent | Select-String 'next-version' | ForEach-Object { $_.ToString().Split(':')[1].Trim() })" -ForegroundColor White

# Verify all versions match
$allVersionsMatch = ($manifest.version -eq $targetVersion) -and ($packageJson.version -eq $targetVersion)
if ($allVersionsMatch) {
    Write-Host "✅ All versions synchronized successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Version mismatch detected!" -ForegroundColor Red
}

# Step 6: Git operations (if not SyncOnly)
if (-not $SyncOnly) {
    Write-Host ""
    Write-Host "📝 Committing version changes..." -ForegroundColor Blue
    
    # Stage all version files
    git add $ManifestPath $PackageJsonPath $GitVersionPath $DirectoryBuildPropsPath
    
    # Commit changes
    $commitMessage = "chore: bump version to $targetVersion"
    git commit -m $commitMessage
    Write-Host "✅ Changes committed: $commitMessage"
    
    # Create tag
    $tagName = "v$targetVersion"
    git tag -a $tagName -m "Version $targetVersion"
    Write-Host "✅ Tag created: $tagName"
    
    # Push changes and tags
    git push origin main
    git push origin $tagName
    Write-Host "✅ Changes and tags pushed to origin"
    
    # Step 7: Create GitHub release (if requested)
    if ($CreateRelease) {
        Write-Host ""
        Write-Host "🚀 Creating GitHub release..." -ForegroundColor Blue
        
        $releaseArgs = @("release", "create", $tagName, "--title", "Version $targetVersion")
        
        if ($PreRelease) {
            $releaseArgs += "--prerelease"
        }
        
        # Generate release notes
        $releaseNotes = @"
## Version $targetVersion

### Changes
- Version synchronized across CLI and Plugin components
- CLI version: $targetVersion
- Plugin version: $targetVersion

### Downloads
- CLI executables available in workflow artifacts
- Plugin available for BRAT installation: $tagName

### Installation
#### CLI
Download the appropriate executable for your platform from the workflow artifacts.

#### Plugin
For BRAT users: Add this repository URL and select tag $tagName
"@
        
        $releaseArgs += "--notes", $releaseNotes
        
        gh @releaseArgs
        Write-Host "✅ GitHub release created: $tagName"
    }
}

# Step 8: Final summary
Write-Host ""
Write-Host "🎉 Version management completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "  Version: $targetVersion" -ForegroundColor White
Write-Host "  Type: $Type" -ForegroundColor White
Write-Host "  CLI and Plugin synchronized: ✅" -ForegroundColor Green
if (-not $SyncOnly) {
    Write-Host "  Git tag created: v$targetVersion" -ForegroundColor White
    if ($CreateRelease) {
        Write-Host "  GitHub release created: ✅" -ForegroundColor Green
    }
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Test both CLI and plugin with version $targetVersion"
Write-Host "  2. Verify BRAT installation works with tag v$targetVersion"
Write-Host "  3. Update documentation if needed"
