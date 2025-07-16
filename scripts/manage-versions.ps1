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

.PARAMETER ReleaseNotes
    Custom release notes to append to the automatically generated notes

.PARAMETER ChangelogEntry
    Custom changelog entry to add to the [Unreleased] section before processing

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

.EXAMPLE
    .\scripts\manage-versions.ps1 -Type "beta" -CreateRelease -PreRelease -ReleaseNotes "Additional notes for this release"
    Auto-increments beta version with custom release notes

.EXAMPLE
    .\scripts\manage-versions.ps1 -Type "stable" -CreateRelease -ChangelogEntry "- Fixed critical bug in authentication"
    Promotes to stable with custom changelog entry
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$Version,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("beta", "stable", "patch")]
    [string]$Type,
    
    [switch]$CreateRelease,
    [switch]$PreRelease,
    [switch]$SyncOnly,
    
    [Parameter(Mandatory = $false)]
    [string]$ReleaseNotes,
    
    [Parameter(Mandatory = $false)]
    [string]$ChangelogEntry
)

$ErrorActionPreference = "Stop"

# Function to generate comprehensive release notes
function New-ReleaseNotes {
    param(
        [string]$Version,
        [string]$Type,
        [string]$CustomNotes = ""
    )
    
    Write-Host "  📝 Generating release notes..."
    
    # Get the previous version tag to compare against
    $previousTag = git describe --tags --abbrev=0 HEAD~1 2>$null
    $commitRange = if ($previousTag) { "$previousTag..HEAD" } else { "HEAD" }
    
    # Get commit messages since last release
    $commits = git log $commitRange --oneline --no-merges
    
    # Parse changelog for current version (if exists)
    $changelogPath = Join-Path $RepoRoot "CHANGELOG.md"
    $changelogContent = ""
    if (Test-Path $changelogPath) {
        $changelogLines = Get-Content $changelogPath
        $inUnreleased = $false
        $changelogEntries = @()
        
        foreach ($line in $changelogLines) {
            if ($line -match '## \[Unreleased\]') {
                $inUnreleased = $true
                continue
            }
            if ($line -match '## \[[\d\.]+-?(beta|alpha)?\.\d*\]') {
                if ($inUnreleased) {
                    break
                }
                continue
            }
            if ($inUnreleased -and $line -match '^### ') {
                $changelogEntries += $line
            }
            if ($inUnreleased -and $line -match '^- ') {
                $changelogEntries += $line
            }
        }
        
        if ($changelogEntries) {
            $changelogContent = ($changelogEntries -join "`n").Trim()
        }
    }
    
    # Categorize commits by type
    $features = @()
    $fixes = @()
    $chores = @()
    $docs = @()
    $tests = @()
    $refactors = @()
    
    foreach ($commit in $commits) {
        if ($commit -match '^[a-f0-9]+ (.+)$') {
            $message = $matches[1]
            switch -Regex ($message) {
                '^feat(\(.*\))?:' { $features += $message -replace '^feat(\(.*\))?:\s*', '' }
                '^fix(\(.*\))?:' { $fixes += $message -replace '^fix(\(.*\))?:\s*', '' }
                '^docs(\(.*\))?:' { $docs += $message -replace '^docs(\(.*\))?:\s*', '' }
                '^test(\(.*\))?:' { $tests += $message -replace '^test(\(.*\))?:\s*', '' }
                '^refactor(\(.*\))?:' { $refactors += $message -replace '^refactor(\(.*\))?:\s*', '' }
                '^chore(\(.*\))?:' { $chores += $message -replace '^chore(\(.*\))?:\s*', '' }
                default { 
                    # Try to categorize based on content
                    if ($message -match '(add|implement|create|new)') {
                        $features += $message
                    }
                    elseif ($message -match '(fix|resolve|bug|issue)') {
                        $fixes += $message
                    }
                    elseif ($message -match '(update|change|modify|improve)') {
                        $refactors += $message
                    }
                    elseif ($message -match '(test|spec)') {
                        $tests += $message
                    }
                    elseif ($message -match '(doc|readme|comment)') {
                        $docs += $message
                    }
                    else {
                        $chores += $message
                    }
                }
            }
        }
    }
    
    # Generate release type description
    $releaseTypeDescription = switch ($Type) {
        "beta" { "This is a beta release for testing and feedback." }
        "stable" { "This is a stable production release." }
        "patch" { "This is a patch release with bug fixes." }
        default { "This is a maintenance release." }
    }
    
    # Build release notes
    $releaseNotes = @"
## Version $Version

$releaseTypeDescription

"@
    
    # Add changelog content if available
    if ($changelogContent) {
        $releaseNotes += @"
### Changes from Changelog

$changelogContent

"@
    }
    
    # Add commit-based changes
    if ($features -or $fixes -or $refactors -or $docs -or $tests -or $chores) {
        $releaseNotes += "### Changes in This Release`n`n"
        
        if ($features) {
            $releaseNotes += "#### ✨ New Features`n"
            foreach ($feature in $features) {
                $releaseNotes += "- $feature`n"
            }
            $releaseNotes += "`n"
        }
        
        if ($fixes) {
            $releaseNotes += "#### 🐛 Bug Fixes`n"
            foreach ($fix in $fixes) {
                $releaseNotes += "- $fix`n"
            }
            $releaseNotes += "`n"
        }
        
        if ($refactors) {
            $releaseNotes += "#### 🔧 Improvements`n"
            foreach ($refactor in $refactors) {
                $releaseNotes += "- $refactor`n"
            }
            $releaseNotes += "`n"
        }
        
        if ($docs) {
            $releaseNotes += "#### 📚 Documentation`n"
            foreach ($doc in $docs) {
                $releaseNotes += "- $doc`n"
            }
            $releaseNotes += "`n"
        }
        
        if ($tests) {
            $releaseNotes += "#### 🧪 Testing`n"
            foreach ($test in $tests) {
                $releaseNotes += "- $test`n"
            }
            $releaseNotes += "`n"
        }
        
        if ($chores) {
            $releaseNotes += "#### 🏠 Maintenance`n"
            foreach ($chore in $chores) {
                $releaseNotes += "- $chore`n"
            }
            $releaseNotes += "`n"
        }
    }
    
    # Add custom release notes if provided
    if ($CustomNotes) {
        $releaseNotes += @"
### Additional Notes

$CustomNotes

"@
    }
    
    # Add technical details
    $releaseNotes += @"
### Technical Details

- **CLI Version**: $Version
- **Plugin Version**: $Version
- **Compatibility**: Obsidian 0.15.0+
- **Build**: Cross-platform executables (Windows, macOS, Linux)

### 📦 Downloads

#### CLI Application
- Windows: Download `na-win-x64.exe` or `na-win-arm64.exe` from workflow artifacts
- macOS: Download `na-macos-x64` or `na-macos-arm64` from workflow artifacts  
- Linux: Download `na-linux-x64` or `na-linux-arm64` from workflow artifacts

#### Obsidian Plugin
- **BRAT Installation**: Add repository URL and select tag `v$Version`
- **Manual Installation**: Download plugin files from the `notebook-automation-obsidian-plugin` artifact

### 🔧 Installation Instructions

#### CLI Installation
1. Download the appropriate executable for your platform
2. Place it in your PATH or run directly
3. Test installation: `na --version`

#### Plugin Installation (BRAT)
1. Install the BRAT plugin in Obsidian
2. Open BRAT settings
3. Add repository: `danielshue/notebook-automation`
4. Select tag: `v$Version`
5. Enable the plugin in Obsidian settings

### 🐛 Known Issues

"@
    
    # Add any known issues or limitations
    if ($Type -eq "beta") {
        $releaseNotes += @"
- This is a beta release - please report any issues
- Some features may still be in development
- Breaking changes may occur in future beta releases

"@
    }
    
    $releaseNotes += @"
For support, please create an issue in the repository.

### 🔍 Full Changelog

"@
    
    if ($previousTag) {
        $releaseNotes += "View full changes: [v$previousTag...v$Version](https://github.com/danielshue/notebook-automation/compare/v$previousTag...v$Version)"
    }
    else {
        $releaseNotes += "View commits: [v$Version](https://github.com/danielshue/notebook-automation/commits/v$Version)"
    }
    
    return $releaseNotes
}

# Function to update CHANGELOG.md with new version
function Update-Changelog {
    param(
        [string]$Version,
        [string]$Type,
        [string]$CustomEntry = ""
    )
    
    Write-Host "  📝 Updating CHANGELOG.md..."
    
    $changelogPath = Join-Path $RepoRoot "CHANGELOG.md"
    if (-not (Test-Path $changelogPath)) {
        Write-Host "  ⚠️  CHANGELOG.md not found, skipping update"
        return
    }
    
    # Add custom changelog entry if provided
    if ($CustomEntry) {
        # Find the [Unreleased] section and add custom entry
        for ($i = 0; $i -lt $changelogContent.Length; $i++) {
            if ($changelogContent[$i] -match '## \[Unreleased\]') {
                # Look for the first ### section and add entry there
                for ($j = $i + 1; $j -lt $changelogContent.Length; $j++) {
                    if ($changelogContent[$j] -match '^### ') {
                        # Find the first empty line or existing entry
                        for ($k = $j + 1; $k -lt $changelogContent.Length; $k++) {
                            if ($changelogContent[$k] -match '^- TBD' -or $changelogContent[$k] -eq "") {
                                if ($changelogContent[$k] -match '^- TBD') {
                                    $changelogContent[$k] = $CustomEntry
                                }
                                else {
                                    # Insert before this line
                                    $changelogContent = $changelogContent[0..($k - 1)] + $CustomEntry + $changelogContent[$k..($changelogContent.Length - 1)]
                                }
                                break
                            }
                        }
                        break
                    }
                }
                break
            }
        }
        Set-Content -Path $changelogPath -Value $changelogContent
        Write-Host "  ✅ Added custom changelog entry: $CustomEntry"
    }
    
    $changelogContent = Get-Content $changelogPath
    $currentDate = Get-Date -Format "yyyy-MM-dd"
    
    # Find the [Unreleased] section
    $unreleasedIndex = -1
    for ($i = 0; $i -lt $changelogContent.Length; $i++) {
        if ($changelogContent[$i] -match '## \[Unreleased\]') {
            $unreleasedIndex = $i
            break
        }
    }
    
    if ($unreleasedIndex -eq -1) {
        Write-Host "  ⚠️  [Unreleased] section not found in CHANGELOG.md, skipping update"
        return
    }
    
    # Create new version entry
    $versionEntry = "## [$Version] - $currentDate"
    
    # Insert new version after unreleased section
    # Find the next ## heading after unreleased
    $nextHeadingIndex = -1
    for ($i = $unreleasedIndex + 1; $i -lt $changelogContent.Length; $i++) {
        if ($changelogContent[$i] -match '^## \[') {
            $nextHeadingIndex = $i
            break
        }
    }
    
    # Insert the new version entry
    if ($nextHeadingIndex -ne -1) {
        # Insert before the next version
        $newContent = $changelogContent[0..($nextHeadingIndex - 1)] + "" + $versionEntry + "" + $changelogContent[$nextHeadingIndex..($changelogContent.Length - 1)]
    }
    else {
        # Append at the end
        $newContent = $changelogContent + "" + $versionEntry + ""
    }
    
    # Reset the [Unreleased] section
    for ($i = 0; $i -lt $newContent.Length; $i++) {
        if ($newContent[$i] -match '## \[Unreleased\]') {
            # Look for the next few lines and reset them
            for ($j = $i + 1; $j -lt $newContent.Length -and $j -lt $i + 10; $j++) {
                if ($newContent[$j] -match '^### Added') {
                    $newContent[$j + 1] = ""
                    $newContent[$j + 2] = "- TBD"
                    break
                }
            }
            for ($j = $i + 1; $j -lt $newContent.Length -and $j -lt $i + 15; $j++) {
                if ($newContent[$j] -match '^### Fixed') {
                    $newContent[$j + 1] = ""
                    $newContent[$j + 2] = "- TBD"
                    break
                }
            }
            for ($j = $i + 1; $j -lt $newContent.Length -and $j -lt $i + 20; $j++) {
                if ($newContent[$j] -match '^### Changed') {
                    $newContent[$j + 1] = ""
                    $newContent[$j + 2] = "- TBD"
                    break
                }
            }
            break
        }
    }
    
    # Write back to file
    Set-Content -Path $changelogPath -Value $newContent
    Write-Host "  ✅ CHANGELOG.md updated with version $Version"
}

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
    }
    else {
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
                }
                else {
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
                }
                else {
                    # Increment minor version for new stable release
                    $nextVersion = "$major.$($minor + 1).0"
                    Write-Host "Incrementing minor version: $CurrentVersion → $nextVersion" -ForegroundColor Green
                    return $nextVersion
                }
            }
            "patch" {
                if ($prerelease) {
                    throw "Cannot create patch release from prerelease version. Use 'stable' type first."
                }
                else {
                    # Increment patch version
                    $nextVersion = "$major.$minor.$($patch + 1)"
                    Write-Host "Incrementing patch version: $CurrentVersion → $nextVersion" -ForegroundColor Green
                    return $nextVersion
                }
            }
        }
    }
    else {
        throw "Invalid version format: $CurrentVersion. Expected format: x.y.z or x.y.z-prerelease.build"
    }
}

# Determine the target version
if ($Version) {
    $targetVersion = $Version
    Write-Host "Using specified version: $targetVersion" -ForegroundColor Yellow
}
else {
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
}
else {
    throw "manifest.json not found at: $ManifestPath"
}

# Update package.json
if (Test-Path $PackageJsonPath) {
    $packageJson = Get-Content $PackageJsonPath | ConvertFrom-Json
    $packageJson.version = $targetVersion
    $packageJson | ConvertTo-Json -Depth 10 | Set-Content $PackageJsonPath
    Write-Host "✅ package.json updated to: $targetVersion"
}
else {
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
}
catch {
    Write-Error "CLI build failed: $($_.Exception.Message)"
}

# Build Plugin
Write-Host "Building Plugin..."
Push-Location $PluginRoot
try {
    npm run build | Out-Null
    Write-Host "✅ Plugin build successful"
}
catch {
    Write-Error "Plugin build failed: $($_.Exception.Message)"
}
finally {
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
}
else {
    Write-Host "❌ Version mismatch detected!" -ForegroundColor Red
}

# Step 6: Update changelog and prepare for release
Write-Host ""
Write-Host "📝 Updating changelog..." -ForegroundColor Blue

# Update CHANGELOG.md (always do this if changelog entry provided)
if ($ChangelogEntry) {
    Update-Changelog -Version $targetVersion -Type $Type -CustomEntry $ChangelogEntry
}

# Step 7: Git operations (if not SyncOnly)
if (-not $SyncOnly) {
    Write-Host ""
    Write-Host "📝 Committing version changes..." -ForegroundColor Blue
    
    # Update CHANGELOG.md if not already done
    if (-not $ChangelogEntry) {
        Update-Changelog -Version $targetVersion -Type $Type
    }
    
    # Stage all version files
    git add $ManifestPath $PackageJsonPath $GitVersionPath $DirectoryBuildPropsPath
    
    # Also stage CHANGELOG.md if it was updated
    $changelogPath = Join-Path $RepoRoot "CHANGELOG.md"
    if (Test-Path $changelogPath) {
        git add $changelogPath
    }
    
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
        
        # Generate comprehensive release notes
        $releaseNotes = New-ReleaseNotes -Version $targetVersion -Type $Type -CustomNotes $ReleaseNotes
        
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
