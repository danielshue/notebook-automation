#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Manages versioning for the Obsidian plugin, preparing it for BRAT beta testing.

.DESCRIPTION
    This script automates the version management process for the Notebook Automation
    Obsidian plugin, ensuring version consistency across package.json, manifest.json,
    and Git tags for proper BRAT (Beta Reviewer's Auto-update Tool) functionality.

.PARAMETER Version
    The version to set (e.g., "0.1.0-beta.1", "0.1.0")

.PARAMETER Type
    The type of version update: "beta", "stable", or "patch"

.PARAMETER CreateRelease
    Whether to create a GitHub release after tagging

.PARAMETER PreRelease
    Whether to mark the GitHub release as a pre-release (for beta versions)

.EXAMPLE
    .\scripts\manage-plugin-version.ps1 -Version "0.1.0-beta.1" -Type "beta" -CreateRelease -PreRelease
    
.EXAMPLE
    .\scripts\manage-plugin-version.ps1 -Version "0.1.0" -Type "stable" -CreateRelease

.NOTES
    - Requires gh CLI to be installed and authenticated
    - Must be run from the repository root
    - Automatically syncs versions between package.json and manifest.json
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("beta", "stable", "patch")]
    [string]$Type,
    
    [switch]$CreateRelease,
    
    [switch]$PreRelease
)

# Set error handling
$ErrorActionPreference = "Stop"

# Define paths
$RepoRoot = Get-Location
$PluginDir = Join-Path $RepoRoot "src\obsidian-plugin"
$PackageJsonPath = Join-Path $PluginDir "package.json"
$ManifestJsonPath = Join-Path $PluginDir "manifest.json"

Write-Host "🔧 Managing Obsidian Plugin Version: $Version ($Type)" -ForegroundColor Green

# Validation
if (-not (Test-Path $PluginDir)) {
    throw "Plugin directory not found: $PluginDir"
}

if (-not (Test-Path $PackageJsonPath)) {
    throw "package.json not found: $PackageJsonPath"
}

if (-not (Test-Path $ManifestJsonPath)) {
    throw "manifest.json not found: $ManifestJsonPath"
}

# Check if we're in a git repository
try {
    git rev-parse --git-dir | Out-Null
}
catch {
    throw "Not in a git repository"
}

# Check for uncommitted changes
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Warning "⚠️  Uncommitted changes detected:"
    $gitStatus | ForEach-Object { Write-Warning "   $_" }
    $continue = Read-Host "Continue anyway? (y/N)"
    if ($continue -ne 'y' -and $continue -ne 'Y') {
        throw "Aborted due to uncommitted changes"
    }
}

# Debugging: Check variable types and values before Join-Path calls
Write-Host "[DEBUG] RepoRoot: $RepoRoot (Type: $($RepoRoot.GetType().Name))" -ForegroundColor Yellow
Write-Host "[DEBUG] PluginDir: $PluginDir (Type: $($PluginDir.GetType().Name))" -ForegroundColor Yellow
Write-Host "[DEBUG] PackageJsonPath: $PackageJsonPath (Type: $($PackageJsonPath.GetType().Name))" -ForegroundColor Yellow
Write-Host "[DEBUG] ManifestJsonPath: $ManifestJsonPath (Type: $($ManifestJsonPath.GetType().Name))" -ForegroundColor Yellow

# Step 1: Update package.json version
# Check if the specified version is already set in package.json
$packageJson = Get-Content $PackageJsonPath | ConvertFrom-Json
if ($packageJson.version -eq $Version) {
    Write-Host "⚠️  Specified version ($Version) is already set in package.json. Skipping version update." -ForegroundColor Yellow
}
else {
    Write-Host "📝 Updating package.json version to $Version"
    Push-Location $PluginDir
    try {
        npm version $Version --no-git-tag-version
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to update package.json version"
        }
    }
    finally {
        Pop-Location
    }
}

# Step 2: Run version bump script to sync manifest.json
Write-Host "🔄 Syncing manifest.json with package.json"
Push-Location $PluginDir
try {
    npm run version
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to run version bump script"
    }
}
finally {
    Pop-Location
}

# Step 3: Verify versions are synchronized
Write-Host "✅ Verifying version synchronization"
$packageJson = Get-Content $PackageJsonPath | ConvertFrom-Json
$manifestJson = Get-Content $ManifestJsonPath | ConvertFrom-Json

$packageVersion = $packageJson.version
$manifestVersion = $manifestJson.version

Write-Host "   package.json: $packageVersion"
Write-Host "   manifest.json: $manifestVersion"

if ($packageVersion -ne $manifestVersion) {
    throw "Version mismatch: package.json ($packageVersion) != manifest.json ($manifestVersion)"
}

if ($packageVersion -ne $Version) {
    throw "Version mismatch: Expected $Version, got $packageVersion"
}

# Step 4: Build the plugin
Write-Host "🔨 Building plugin"
Push-Location $PluginDir
try {
    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build plugin"
    }
    
    # The new build script automatically handles executable preservation
    Write-Host "✅ Build completed with executable preservation"
}
finally {
    Pop-Location
}

# Step 5: Verify build artifacts
$buildArtifacts = @(
    Join-Path $PluginDir "dist\main.js"
    Join-Path $PluginDir "manifest.json"
    Join-Path $PluginDir "styles.css"
)

foreach ($artifact in $buildArtifacts) {
    if (-not (Test-Path $artifact)) {
        throw "Build artifact missing: $artifact"
    }
}

Write-Host "✅ Build artifacts verified"

# Step 6: Commit changes
Write-Host "📝 Committing version changes"
$commitMessage = switch ($Type) {
    "beta" { "feat: prepare v$Version for BRAT beta testing" }
    "stable" { "release: v$Version stable release" }
    "patch" { "fix: patch release v$Version" }
    default { "chore: version bump to v$Version" }
}

git add $PackageJsonPath, $ManifestJsonPath
git commit -m $commitMessage

# Step 7: Create and push tag
$tagName = "v$Version"
Write-Host "🏷️  Creating tag: $tagName"
git tag $tagName
git push origin $tagName

# Step 8: Create GitHub release if requested
if ($CreateRelease) {
    Write-Host "🚀 Creating GitHub release"
    
    # Check if gh CLI is available
    try {
        gh --version | Out-Null
    }
    catch {
        throw "GitHub CLI (gh) not found. Install it to create releases automatically."
    }
    
    # Prepare release assets
    $releaseAssets = @(
        Join-Path $PluginDir "dist\main.js"
        Join-Path $PluginDir "manifest.json"
        Join-Path $PluginDir "styles.css"
    )
    
    # Create release notes
    $releaseNotes = switch ($Type) {
        "beta" { 
            @"
## Beta Release v$Version

This is a beta release for testing with BRAT (Beta Reviewer's Auto-update Tool).

### Installation via BRAT:
1. Install the BRAT plugin in Obsidian
2. Add this repository: ``danielshue/notebook-automation``
3. BRAT will automatically install and update the plugin

### Changes in this release:
- Beta testing version
- Contains all platform executables
- Ready for BRAT installation

**Note:** This is a pre-release version. Please report any issues on GitHub.
"@
        }
        "stable" { 
            @"
## Stable Release v$Version

This is a stable release of the Notebook Automation plugin.

### Installation:
- Via BRAT: Add repository ``danielshue/notebook-automation``
- Manual: Download and extract to your Obsidian plugins folder

### What's included:
- Plugin files (main.js, manifest.json, styles.css)
- Cross-platform executables for all supported systems
- Ready-to-install package
"@
        }
        "patch" { 
            @"
## Patch Release v$Version

This is a patch release with bug fixes and minor improvements.

### Installation:
- Via BRAT: Will auto-update if you're using BRAT
- Manual: Download and replace your existing installation
"@
        }
    }
    
    # Build gh release command
    $ghArgs = @(
        "release", "create", $tagName,
        "--title", "v$Version"
        "--notes", $releaseNotes
    )
    
    if ($PreRelease) {
        $ghArgs += "--prerelease"
    }
    
    $ghArgs += $releaseAssets
    
    # Execute gh release create
    & gh @ghArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create GitHub release"
    }
    
    Write-Host "✅ GitHub release created successfully"
}

# Step 9: Summary
Write-Host ""
Write-Host "🎉 Version management completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "  Version: $Version" -ForegroundColor White
Write-Host "  Type: $Type" -ForegroundColor White
Write-Host "  Tag: $tagName" -ForegroundColor White
Write-Host "  Release Created: $CreateRelease" -ForegroundColor White
Write-Host "  Pre-release: $PreRelease" -ForegroundColor White
Write-Host ""

if ($Type -eq "beta") {
    Write-Host "Next steps for beta testing:" -ForegroundColor Yellow
    Write-Host "  1. Wait for CI to complete the build process"
    Write-Host "  2. Share the repository URL with beta testers"
    Write-Host "  3. Testers can install via BRAT using: danielshue/notebook-automation"
    Write-Host "  4. Monitor for feedback and issues"
}
elseif ($Type -eq "stable") {
    Write-Host "Next steps for stable release:" -ForegroundColor Yellow
    Write-Host "  1. Wait for CI to complete the build process"
    Write-Host "  2. Update documentation with new version"
    Write-Host "  3. Announce the release to users"
    Write-Host "  4. Monitor for any issues"
}

Write-Host ""
Write-Host "GitHub Release: https://github.com/danielshue/notebook-automation/releases/tag/$tagName" -ForegroundColor Cyan
