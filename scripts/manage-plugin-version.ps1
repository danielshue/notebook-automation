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
    [Parameter(Mandatory = $false)]
    [string]$Version,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("beta", "stable", "patch")]
    [string]$Type = 'beta',
    
    [switch]$CreateRelease,
    
    [switch]$PreRelease,

    # If specified, only rebuild CLI executables & validate they embed current version (no version bump / tagging)
    [switch]$RebuildOnly
)

# Set error handling
$ErrorActionPreference = "Stop"

# Define paths
$RepoRoot = Get-Location
$PluginDir = Join-Path $RepoRoot "src\obsidian-plugin"
$PackageJsonPath = Join-Path $PluginDir "package.json"
$ManifestJsonPath = Join-Path $PluginDir "manifest.json"

if ($RebuildOnly -and -not $Version) {
    # Infer version from manifest if not provided
    if (Test-Path $ManifestJsonPath) {
        $manifestData = Get-Content $ManifestJsonPath | ConvertFrom-Json
        $Version = $manifestData.version
        Write-Host "ℹ️  Inferred current version from manifest: $Version" -ForegroundColor Cyan
    }
    else {
        throw "Cannot infer version (manifest.json missing). Provide -Version explicitly when using -RebuildOnly."
    }
}

if (-not $Version) { throw "-Version is required unless -RebuildOnly with inferable manifest version." }

Write-Host ( $RebuildOnly ? "🔧 Rebuilding executables for existing version: $Version" : "🔧 Managing Obsidian Plugin Version: $Version ($Type)" ) -ForegroundColor Green

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

function Invoke-DotnetPublishMatrix {
    param(
        [string]$CliProject,
        [string]$PublishRoot,
        [string]$SemanticVersion
    )

    Write-Host "🧪 Publishing fresh CLI executables for all platforms" -ForegroundColor Green
    if (-not (Test-Path $CliProject)) { throw "CLI project not found at $CliProject" }
    if (-not (Test-Path $PublishRoot)) { New-Item -ItemType Directory -Path $PublishRoot | Out-Null }

    Get-ChildItem -Path $PublishRoot -File -Filter 'na-*' -ErrorAction SilentlyContinue | ForEach-Object { $_ | Remove-Item -Force }

    $targets = @(
        @{ Rid = 'win-x64'; Out = 'na-win-x64.exe'; Ext = '.exe' },
        @{ Rid = 'win-arm64'; Out = 'na-win-arm64.exe'; Ext = '.exe' },
        @{ Rid = 'linux-x64'; Out = 'na-linux-x64'; Ext = '' },
        @{ Rid = 'linux-arm64'; Out = 'na-linux-arm64'; Ext = '' },
        @{ Rid = 'osx-x64'; Out = 'na-macos-x64'; Ext = '' },
        @{ Rid = 'osx-arm64'; Out = 'na-macos-arm64'; Ext = '' }
    )

    foreach ($t in $targets) {
        $rid = $t.Rid; $outName = $t.Out
        $tempOut = Join-Path $PublishRoot "_temp-$rid"
        if (Test-Path $tempOut) { Remove-Item -Recurse -Force $tempOut -ErrorAction SilentlyContinue }
        Write-Host "  • Publishing $rid → $outName" -ForegroundColor Yellow
        $publishArgs = @('publish', $CliProject, '-c', 'Release', '-r', $rid, '/p:PublishSingleFile=true', '/p:SelfContained=true', '--output', $tempOut)
        $pub = & dotnet @publishArgs 2>&1
        if ($LASTEXITCODE -ne 0) { Write-Host $pub -ForegroundColor Red; throw "Publish failed for $rid" }
        $produced = Join-Path $tempOut ("na" + $t.Ext)
        if (-not (Test-Path $produced)) { throw "Expected binary not found: $produced" }
        $finalPath = Join-Path $PublishRoot $outName
        Copy-Item $produced $finalPath -Force
        if ($IsWindows -eq $false -and $t.Ext -eq '') { try { chmod +x $finalPath 2>$null } catch {} }
        Write-Host "    ✓ $outName" -ForegroundColor Green
        Remove-Item -Recurse -Force $tempOut -ErrorAction SilentlyContinue
    }

    Write-Host "🔍 Validating semantic version in host executables" -ForegroundColor Green
    $hostExecutables = Get-ChildItem -Path $PublishRoot -File | Where-Object { $_.Name -like 'na-*' -and ( ($IsWindows -and $_.Extension -eq '.exe') -or ($IsLinux -and $_.Name -match 'linux') -or ($IsMacOS -and $_.Name -match 'macos') ) }
    foreach ($exe in $hostExecutables) {
        try {
            $raw = & $exe.FullName --version 2>$null
            if ($LASTEXITCODE -ne 0) { throw "Non-zero exit" }
            $lines = $raw -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -and ($_ -notmatch '^-(version|v)$') }
            $verOutput = ($lines -join ' ')
            if ($verOutput -notmatch [Regex]::Escape($SemanticVersion)) { throw "Semantic version $SemanticVersion not detected in output of $($exe.Name)" }
            Write-Host "    ✓ $($exe.Name) version OK" -ForegroundColor Green
        }
        catch { throw "Version validation failed for $($exe.Name): $($_.Exception.Message)" }
    }
}

if ($RebuildOnly) {
    $cliProject = Join-Path $RepoRoot "src\c-sharp\NotebookAutomation.Cli\NotebookAutomation.Cli.csproj"
    $publishRoot = Join-Path $RepoRoot 'dist'
    Invoke-DotnetPublishMatrix -CliProject $cliProject -PublishRoot $publishRoot -SemanticVersion $Version
    Write-Host "✅ Rebuild-only complete." -ForegroundColor Green
    return
}

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

# Step 3b: Update CLI compile-time version constant
$versionConstantsPath = Join-Path $RepoRoot "src\c-sharp\NotebookAutomation.Cli\VersionConstants.cs"
if (Test-Path $versionConstantsPath) {
    Write-Host "🧩 Updating VersionConstants.cs (compile-time injection)" -ForegroundColor Green
    # Build VersionConstants.cs content safely with proper escaping for quotes
    $versionConstantsContent = @(
        "// <auto-generated>",
        "//  This file is generated during version bump operations.",
        "//  Do not edit manually; update via version management scripts.",
        "// </auto-generated>",
        "",
        "namespace NotebookAutomation.Cli;",
        "",
        "internal static class VersionConstants",
        "{",
        "    /// <summary>",
        "    /// The current plugin release (semantic) version synchronized with manifest.json.",
        "    /// </summary>",
        "    public const string PluginReleaseVersion = `"$Version`";",
        "}"
    ) -join "`n"
    Set-Content -Path $versionConstantsPath -Value $versionConstantsContent -Encoding UTF8
    git add $versionConstantsPath
    Write-Host "✅ VersionConstants.cs updated" -ForegroundColor Green
}
else {
    Write-Warning "VersionConstants.cs not found at $versionConstantsPath (skipping compile-time constant update)"
}

Invoke-DotnetPublishMatrix -CliProject (Join-Path $RepoRoot "src\c-sharp\NotebookAutomation.Cli\NotebookAutomation.Cli.csproj") -PublishRoot (Join-Path $RepoRoot 'dist') -SemanticVersion $Version

# Guard: Ensure only expected executable naming (post-publish)
function Assert-NaExecutableSet {
    param(
        [string]$DistPath,
        [string]$ExpectedVersion
    )

    if (-not (Test-Path $DistPath)) { throw "Dist path not found: $DistPath" }
    $executables = Get-ChildItem -Path $DistPath -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -like 'na-*' }

    $expected = @(
        'na-win-x64.exe', 'na-win-arm64.exe',
        'na-linux-x64', 'na-linux-arm64',
        'na-macos-x64', 'na-macos-arm64'
    )

    $legacy = $executables | Where-Object { $_.Name -like 'na-osx-*' }
    if ($legacy) {
        Write-Host "❌ Legacy osx-named executables detected:" -ForegroundColor Red
        $legacy | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
        throw "Legacy executable names (na-osx-*) present. Aborting."
    }

    # Check for unexpected extras
    $names = $executables.Name
    $unexpected = $names | Where-Object { $_ -notin $expected }
    if ($unexpected) {
        Write-Host "❌ Unexpected executables present:" -ForegroundColor Red
        $unexpected | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
        throw "Unexpected executables found in dist."
    }

    # Ensure all expected exist
    $missing = $expected | Where-Object { $_ -notin $names }
    if ($missing) {
        Write-Host "❌ Missing expected executables:" -ForegroundColor Red
        $missing | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
        throw "One or more expected executables missing."
    }

    # Semantic version validation (best-effort)
    foreach ($exe in $executables) {
        try {
            $output = & $exe.FullName --version 2>$null
            if ($LASTEXITCODE -ne 0 -or ([string]::IsNullOrWhiteSpace($output))) { throw "No output" }
            if ($output -notmatch [Regex]::Escape($ExpectedVersion)) {
                throw "Version string '$ExpectedVersion' not found in output for $($exe.Name)"
            }
            Write-Host "   ✓ $($exe.Name) version OK" -ForegroundColor Green
        }
        catch {
            throw "Version validation failed for $($exe.Name): $($_.Exception.Message)"
        }
    }

    Write-Host "✅ Executable naming & version validation passed" -ForegroundColor Green
}

Assert-NaExecutableSet -DistPath (Join-Path $RepoRoot 'dist') -ExpectedVersion $Version

# Step 3c: Generate or validate checksums.json for distributed executables
function New-OrValidateChecksumsJson {
    param(
        [string]$DistDir,
        [string]$SemanticVersion
    )

    if (-not (Test-Path $DistDir)) { throw "Dist directory not found: $DistDir" }
    $expected = @('na-win-x64.exe', 'na-win-arm64.exe', 'na-linux-x64', 'na-linux-arm64', 'na-macos-x64', 'na-macos-arm64')
    $executables = Get-ChildItem -Path $DistDir -File | Where-Object { $_.Name -in $expected }
    $missing = $expected | Where-Object { $_ -notin $executables.Name }
    if ($missing) { throw "Cannot create checksums.json - missing executables: $($missing -join ', ')" }

    $checksumsPath = Join-Path $DistDir 'checksums.json'
    $algorithm = 'SHA256'
    $hashMap = @{}
    foreach ($exe in $executables) {
        $hash = (Get-FileHash -Algorithm SHA256 -Path $exe.FullName).Hash.ToLowerInvariant()
        $hashMap[$exe.Name] = $hash
    }

    if (Test-Path $checksumsPath) {
        try {
            $existing = Get-Content $checksumsPath -Raw | ConvertFrom-Json
            $existingFiles = $existing.files | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name
            # Validate presence
            foreach ($name in $expected) { if ($name -notin $existingFiles) { throw "checksums.json missing entry for $name" } }
            # Validate hash equality
            foreach ($name in $expected) {
                $currentHash = $hashMap[$name]
                $recorded = $existing.files.$name
                if ($currentHash -ne $recorded) { throw "Checksum mismatch for $name (recorded=$recorded actual=$currentHash)" }
            }
            Write-Host "✅ Existing checksums.json verified" -ForegroundColor Green
            return $checksumsPath
        }
        catch {
            throw "checksums.json validation failed: $($_.Exception.Message)"
        }
    }
    else {
        $payload = [ordered]@{
            version      = $SemanticVersion
            algorithm    = $algorithm
            generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
            files        = $hashMap
        }
        ($payload | ConvertTo-Json -Depth 5) | Set-Content -Path $checksumsPath -Encoding UTF8
        Write-Host "🧾 Generated checksums.json" -ForegroundColor Green
        return $checksumsPath
    }
}

$distDirRoot = Join-Path $RepoRoot 'dist'
$checksumsFilePath = New-OrValidateChecksumsJson -DistDir $distDirRoot -SemanticVersion $Version

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
    # Copy manifest.json to repository root for BRAT compatibility
    $repoRootManifest = Join-Path $RepoRoot "manifest.json"
    Copy-Item -Path $ManifestJsonPath -Destination $repoRootManifest -Force
    Write-Host "✅ Copied manifest.json to repository root for BRAT compatibility"

    # Copy checksums.json into plugin dist & ensure asset-manifest includes it
    $pluginChecksumsTarget = Join-Path $PluginDir 'dist' 'checksums.json'
    if (Test-Path $checksumsFilePath) {
        Copy-Item $checksumsFilePath $pluginChecksumsTarget -Force
        Write-Host "✅ Copied checksums.json into plugin dist" -ForegroundColor Green
    }

    $assetManifestPath = Join-Path $PluginDir 'dist' 'asset-manifest.json'
    if (Test-Path $assetManifestPath) {
        try {
            $am = Get-Content $assetManifestPath -Raw | ConvertFrom-Json
            if (-not ($am.files -contains 'checksums.json')) {
                $am.files += 'checksums.json'
                ($am | ConvertTo-Json -Depth 5) | Set-Content -Path $assetManifestPath -Encoding UTF8
                Write-Host "🛠️  Updated asset-manifest.json to include checksums.json" -ForegroundColor Green
            }
        }
        catch { Write-Warning "Failed to update asset-manifest.json: $($_.Exception.Message)" }
    }
}
finally {
    Pop-Location
}

# Step 5: Verify build artifacts
$distDir = Join-Path $RepoRoot "dist"
$distFiles = Get-ChildItem -Path $distDir | Select-Object -ExpandProperty Name
Write-Host "[DEBUG] Files in dist directory:" -ForegroundColor Yellow
$distFiles | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
$buildArtifacts = @(
    Join-Path $distDir "main.js"
    Join-Path $distDir "manifest.json"
    Join-Path $distDir "styles.css"
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
    
    # Prepare release assets - include only files listed in asset manifest
    $pluginDistDir = Join-Path $RepoRoot "src\obsidian-plugin\dist"
    $releaseAssets = @()
    
    # Read asset manifest to determine which files to include
    $assetManifestPath = Join-Path $pluginDistDir "asset-manifest.json"
    if (Test-Path $assetManifestPath) {
        $assetManifest = Get-Content $assetManifestPath | ConvertFrom-Json
        
        Write-Host "   📋 Using asset manifest with $($assetManifest.files.Count) files"
        
        foreach ($fileName in $assetManifest.files) {
            $filePath = Join-Path $pluginDistDir $fileName
            if (Test-Path $filePath) {
                $releaseAssets += $filePath
                Write-Host "   📎 Adding to release: $fileName"
            }
            else {
                Write-Warning "   ⚠️  File listed in manifest but not found: $fileName"
            }
        }
    }
    else {
        throw "Asset manifest not found: $assetManifestPath. Run plugin build first."
    }
    
    Write-Host "✅ Prepared $($releaseAssets.Count) release assets from dist directory"
    
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
