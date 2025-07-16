# Versioning Strategy

## Overview

The Notebook Automation project uses a **hybrid versioning approach** to handle the complexities of supporting both a .NET CLI application and an Obsidian plugin with different versioning requirements. This document outlines our versioning strategy, tools, and processes.

## � Quick Start

### Auto-Increment Workflow (Recommended)

The easiest way to manage versions is to let the scripts automatically increment based on the current version:

```powershell
# Check current version status
.\scripts\check-version-status.ps1

# Create next beta version automatically
.\scripts\manage-versions.ps1 -Type "beta" -CreateRelease -PreRelease

# Promote beta to stable automatically
.\scripts\manage-versions.ps1 -Type "stable" -CreateRelease

# Create patch release automatically
.\scripts\manage-versions.ps1 -Type "patch" -CreateRelease
```

### Manual Version Workflow

If you need to specify exact versions:

```powershell
# Specify exact version
.\scripts\manage-versions.ps1 -Version "0.2.0-beta.1" -Type "beta" -CreateRelease -PreRelease
```

## �📊 Current Versioning Status

### Component Versions

| Component   | Current Version | Format              | Source         |
| ----------- | --------------- | ------------------- | -------------- |
| **CLI**     | `0.1.0-beta.4`  | Semantic Versioning | GitVersion.yml |
| **Plugin**  | `0.1.0-beta.4`  | Semantic Versioning | manifest.json  |
| **Project** | `0.1.0-beta.4`  | Unified Version     | Synchronized   |

### Version Alignment

✅ **Synchronized**: All components use the same version number for consistency and easier release management.

## 🎯 Versioning Strategy

### 1. Unified Version System

We maintain **one version number** across all components:

- CLI executable versions
- Obsidian plugin versions
- Git tags and GitHub releases
- Documentation references

### 2. Semantic Versioning

We follow [Semantic Versioning](https://semver.org/) principles:

```text
MAJOR.MINOR.PATCH[-PRERELEASE.BUILD]
```

- **MAJOR**: Breaking changes that affect user workflows
- **MINOR**: New features that are backward compatible
- **PATCH**: Bug fixes that don't change functionality
- **PRERELEASE**: Beta testing versions (e.g., `-beta.1`, `-beta.2`)

### 3. Version Formats

#### Standard Format

```text
0.1.0-beta.4
```

#### Legacy CLI Format (Being Phased Out)

```text
1.0.0-0.25196.0 (commit)
```

*Note: This GitVersion automatic format is being replaced with manual semantic versioning.*

## 🔧 Versioning Tools

### 1. Core Scripts

| Script                     | Purpose                                                                                         | Usage                                                                   |
| -------------------------- | ----------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| `sync-versions.ps1`        | Synchronize versions across components                                                          | `.\scripts\sync-versions.ps1`                                           |
| `manage-versions.ps1`      | Complete version management with auto-increment, automatic release notes, and changelog updates | `.\scripts\manage-versions.ps1 -Type "beta" -CreateRelease -PreRelease` |
| `check-version-status.ps1` | Check current version alignment                                                                 | `.\scripts\check-version-status.ps1`                                    |

### 2. Configuration Files

| File                    | Purpose                     | Current Value                |
| ----------------------- | --------------------------- | ---------------------------- |
| `GitVersion.yml`        | CLI version configuration   | `next-version: 0.1.0-beta.4` |
| `manifest.json`         | Plugin version              | `"version": "0.1.0-beta.4"`  |
| `package.json`          | NPM package version         | `"version": "0.1.0-beta.4"`  |
| `Directory.Build.props` | .NET build version fallback | `GitVersion_SemVer`          |

## 🔄 Version Management Workflow

### 📋 When to Run Version Management Scripts

**You need to manually run the version management scripts in these scenarios:**

| Scenario                   | When to Run                                               | Command Example                                                         |
| -------------------------- | --------------------------------------------------------- | ----------------------------------------------------------------------- |
| **Ready for Beta Testing** | After completing features for user testing                | `.\scripts\manage-versions.ps1 -Type "beta" -CreateRelease -PreRelease` |
| **Stable Release Ready**   | When beta testing is complete and ready for production    | `.\scripts\manage-versions.ps1 -Type "stable" -CreateRelease`           |
| **Bug Fix Release**        | After fixing critical bugs in a stable release            | `.\scripts\manage-versions.ps1 -Type "patch" -CreateRelease`            |
| **New Feature Cycle**      | Starting development of new features after stable release | `.\scripts\manage-versions.ps1 -Type "beta" -CreateRelease -PreRelease` |
| **Hotfix Emergency**       | Critical bugs requiring immediate patch                   | `.\scripts\manage-versions.ps1 -Type "patch" -CreateRelease`            |

### 🚫 When NOT to Run Scripts

**Don't run version scripts for:**

- ❌ Every commit or small change
- ❌ Work-in-progress features
- ❌ Documentation updates only
- ❌ Internal refactoring without user impact
- ❌ CI/CD configuration changes

### 🎯 Recommended Release Cadence

| Release Type | Frequency         | Purpose                           |
| ------------ | ----------------- | --------------------------------- |
| **Beta**     | Weekly/Bi-weekly  | Feature testing and user feedback |
| **Stable**   | Monthly/Quarterly | Production-ready releases         |
| **Patch**    | As needed         | Critical bug fixes only           |

### Development Workflow

1. **Check Current Status**

   ```powershell
   .\scripts\check-version-status.ps1
   ```

2. **Sync Versions (if needed)**

   ```powershell
   .\scripts\sync-versions.ps1 -BuildAfterSync
   ```

3. **Test Components**

   - Test CLI: `dotnet run --project src/c-sharp/NotebookAutomation.Cli -- --version`
   - Test Plugin: Build and verify in Obsidian

### Release Workflow

The version management scripts now automatically generate comprehensive release notes and update the CHANGELOG.md file.

#### Auto-Increment Beta Release

```powershell
.\scripts\manage-versions.ps1 -Type "beta" -CreateRelease -PreRelease
```

**What happens automatically:**

- ✅ Increments beta version number
- ✅ Updates all version files (manifest.json, package.json, GitVersion.yml)
- ✅ Generates release notes from git commits and changelog
- ✅ Updates CHANGELOG.md with new version entry
- ✅ Creates git tag and pushes to GitHub
- ✅ Creates GitHub release with comprehensive notes

#### Auto-Increment Stable Release

```powershell
.\scripts\manage-versions.ps1 -Type "stable" -CreateRelease
```

**What happens automatically:**

- ✅ Promotes beta to stable or increments minor version
- ✅ Synchronizes all version files
- ✅ Generates production-ready release notes
- ✅ Updates CHANGELOG.md
- ✅ Creates stable release on GitHub

#### Auto-Increment Patch Release

```powershell
.\scripts\manage-versions.ps1 -Type "patch" -CreateRelease
```

**What happens automatically:**

- ✅ Increments patch version number
- ✅ Updates version files
- ✅ Generates bug fix release notes
- ✅ Updates CHANGELOG.md
- ✅ Creates patch release on GitHub

#### Manual Version Specification

```powershell
# Specify exact version for beta
.\scripts\manage-versions.ps1 -Version "0.1.0-beta.5" -Type "beta" -CreateRelease -PreRelease

# Specify exact version for stable
.\scripts\manage-versions.ps1 -Version "0.1.0" -Type "stable" -CreateRelease

# Specify exact version for patch
.\scripts\manage-versions.ps1 -Version "0.1.1" -Type "patch" -CreateRelease
```

#### Advanced Options

```powershell
# Add custom release notes
.\scripts\manage-versions.ps1 -Type "beta" -CreateRelease -PreRelease -ReleaseNotes "Special beta features included"

# Add custom changelog entry before processing
.\scripts\manage-versions.ps1 -Type "stable" -CreateRelease -ChangelogEntry "- Fixed critical authentication bug"

# Combine custom notes and changelog
.\scripts\manage-versions.ps1 -Type "patch" -CreateRelease -ChangelogEntry "- Emergency security fix" -ReleaseNotes "This is a critical security update"
```

### 🤖 Automatic Release Notes Generation

The scripts now automatically generate comprehensive release notes that include:

#### 📝 Content Sources

| Source           | Information Extracted                                            |
| ---------------- | ---------------------------------------------------------------- |
| **Git Commits**  | Categorized by type (features, fixes, improvements, docs, tests) |
| **CHANGELOG.md** | Unreleased section content                                       |
| **Version Type** | Release type description (beta, stable, patch)                   |
| **Repository**   | Download links and installation instructions                     |

#### 🏷️ Release Notes Structure

The generated release notes include:

1. **Version Information** - Version number and release type
2. **Changelog Content** - From the [Unreleased] section
3. **Categorized Changes** - From git commits:
   - ✨ New Features
   - 🐛 Bug Fixes  
   - 🔧 Improvements
   - 📚 Documentation
   - 🧪 Testing
   - 🏠 Maintenance
4. **Technical Details** - Version compatibility and build info
5. **Download Instructions** - Platform-specific installation guides
6. **Known Issues** - Beta warnings and limitations
7. **Full Changelog** - Link to compare view on GitHub

#### 🔄 CHANGELOG.md Updates

The script automatically:

- ✅ Moves [Unreleased] content to new version section
- ✅ Adds current date to version entry
- ✅ Resets [Unreleased] sections to "TBD"
- ✅ Maintains proper changelog format
- ✅ Commits changelog updates with version changes

## 🎯 Version Management Examples

### Common Scenarios

| Current Version | Command                                                                 | Result         | Use Case          |
| --------------- | ----------------------------------------------------------------------- | -------------- | ----------------- |
| `0.1.0-beta.4`  | `.\scripts\manage-versions.ps1 -Type "beta" -CreateRelease -PreRelease` | `0.1.0-beta.5` | Next beta release |
| `0.1.0-beta.5`  | `.\scripts\manage-versions.ps1 -Type "stable" -CreateRelease`           | `0.1.0`        | Promote to stable |
| `0.1.0`         | `.\scripts\manage-versions.ps1 -Type "patch" -CreateRelease`            | `0.1.1`        | Bug fix release   |
| `0.1.0`         | `.\scripts\manage-versions.ps1 -Type "beta" -CreateRelease -PreRelease` | `0.1.1-beta.1` | New beta series   |
| `0.1.0`         | `.\scripts\manage-versions.ps1 -Type "stable" -CreateRelease`           | `0.2.0`        | New minor release |

### Quick Commands

```powershell
# Most common workflow - increment current version
.\scripts\manage-versions.ps1 -Type "beta" -CreateRelease -PreRelease

# Sync versions without release
.\scripts\manage-versions.ps1 -Type "beta" -SyncOnly

# Override with specific version
.\scripts\manage-versions.ps1 -Version "0.2.0-beta.1" -Type "beta" -CreateRelease -PreRelease

# Add changelog entry during sync (without release)
.\scripts\manage-versions.ps1 -Type "beta" -SyncOnly -ChangelogEntry "- Added new feature X"
```

## 📝 Version History

### Current Release Series: 0.1.x

| Version        | Date       | Type | Notes                                |
| -------------- | ---------- | ---- | ------------------------------------ |
| `0.1.0-beta.4` | Current    | Beta | Synchronized CLI and plugin versions |
| `0.1.0-beta.3` | 2025-07-06 | Beta | Enhanced metadata extraction         |
| `0.1.0-beta.2` | 2025-07-06 | Beta | BRAT compatibility improvements      |
| `0.1.0-beta.1` | 2025-07-03 | Beta | Initial beta release                 |

### Planned Releases

| Version        | Target | Type   | Planned Features                     |
| -------------- | ------ | ------ | ------------------------------------ |
| `0.1.0-beta.5` | TBD    | Beta   | Version synchronization improvements |
| `0.1.0`        | TBD    | Stable | First stable release                 |
| `0.2.0`        | TBD    | Minor  | Additional automation features       |

## 🛠️ Technical Implementation

### CLI Versioning

The CLI uses **GitVersion** with manual override capability:

```csharp
// AppVersion.cs - Complex version parsing for legacy support
public record AppVersion(
    int Major,
    int Minor, 
    int Patch,
    int Branch,
    int DateCode,
    int Build,
    string Commit
);

// VersionHelper.cs - Version retrieval and display
public static class VersionHelper
{
    public static string GetDetailedVersionInfo();
    public static string GetVersion();
}
```

### Plugin Versioning

The plugin uses standard semantic versioning:

```json
// manifest.json
{
  "version": "0.1.0-beta.4",
  "minAppVersion": "0.15.0"
}

// package.json
{
  "version": "0.1.0-beta.4",
  "name": "notebook-automation"
}
```

### GitVersion Configuration

```yaml
# GitVersion.yml
next-version: 0.1.0-beta.4
mode: ContinuousDelivery
branches:
  main:
    mode: ContinuousDelivery
    tag: ''
  develop:
    mode: ContinuousDelivery
    tag: 'beta'
```

## 🔍 Version Comparison Guide

### For Users

| What You See    | Where                    | Meaning                |
| --------------- | ------------------------ | ---------------------- |
| `0.1.0-beta.4`  | Obsidian Plugin Settings | Current plugin version |
| `0.1.0-beta.4`  | CLI `--version`          | Current CLI version    |
| `v0.1.0-beta.4` | GitHub Releases          | Git tag for releases   |

### For Developers

| Location                            | Current Value  | Purpose                |
| ----------------------------------- | -------------- | ---------------------- |
| `src/obsidian-plugin/manifest.json` | `0.1.0-beta.4` | Plugin runtime version |
| `src/obsidian-plugin/package.json`  | `0.1.0-beta.4` | NPM build version      |
| `GitVersion.yml`                    | `0.1.0-beta.4` | CLI build version      |
| `Directory.Build.props`             | `0.1.0-beta.4` | .NET fallback version  |

## 🎯 Best Practices

### 1. Always Use Scripts

- **Don't** manually edit version files
- **Do** use the provided PowerShell scripts
- **Always** verify version alignment before releases

### 2. Version Increment Rules

The scripts automatically handle version increments based on the current version and type:

#### Beta Versions

- **From beta to beta**: Increment prerelease number (e.g., `0.1.0-beta.4` → `0.1.0-beta.5`)
- **From stable to beta**: Create new beta series (e.g., `0.1.0` → `0.1.1-beta.1`)

#### Stable Releases

- **From beta to stable**: Remove prerelease suffix (e.g., `0.1.0-beta.4` → `0.1.0`)
- **From stable to stable**: Increment minor number (e.g., `0.1.0` → `0.2.0`)

#### Patch Releases

- **From stable only**: Increment patch number (e.g., `0.1.0` → `0.1.1`)
- **Cannot patch from beta**: Must promote to stable first

#### Version Override

- **Manual specification**: Use `-Version` parameter to specify exact version
- **Auto-increment**: Omit `-Version` to automatically increment based on type

### 3. Release Tagging

- Use `v` prefix for all tags: `v0.1.0-beta.4`
- Mark pre-release versions as "pre-release" in GitHub
- Include release notes with installation instructions

### 4. BRAT Compatibility

For Obsidian plugin distribution via BRAT:

- Ensure GitHub releases include `main.js`, `manifest.json`, and `styles.css`
- Use semantic versioning for proper update notifications
- Mark beta versions as pre-releases

## 🔧 Troubleshooting

### Common Issues

#### Version Mismatch

```text
❌ VERSION MISMATCH DETECTED
Found versions: 0.1.0-beta.4, 0.1.0-beta.3
```

**Solution:**

```powershell
.\scripts\sync-versions.ps1 -PluginVersion "0.1.0-beta.4" -BuildAfterSync
```

#### CLI Shows Old Version Format

```text
CLI: 1.0.0-0.25196.0 (current)
```

**Solution:**

```powershell
.\scripts\sync-versions.ps1 -BuildAfterSync
```

#### Plugin Not Loading

Check version compatibility in `manifest.json`:

```json
{
  "minAppVersion": "0.15.0"
}
```

### Debug Commands

```powershell
# Check all version information
.\scripts\check-version-status.ps1 -Detailed

# Verify CLI version
dotnet run --project src/c-sharp/NotebookAutomation.Cli -- --version

# Check plugin files
Get-Content src/obsidian-plugin/manifest.json | ConvertFrom-Json | Select-Object version
Get-Content src/obsidian-plugin/package.json | ConvertFrom-Json | Select-Object version

# Verify Git tags
git tag --list | Sort-Object
```

## 📚 Related Documentation

- [BRAT Setup Guide](./brat-obsidian-plugin-brat-setup.md)
- [Scripts Documentation](../scripts/README.md)
- [Changelog](../CHANGELOG.md)
- [Plugin Development](./plugin-development.md)

## 🔄 Migration Notes

### From Legacy GitVersion to Unified Versioning

The project has transitioned from automatic GitVersion generation to manual semantic versioning:

**Old System:**

- CLI: `1.0.0-0.25196.0` (automatic)
- Plugin: `0.1.0-beta.4` (manual)

**New System:**

- CLI: `0.1.0-beta.4` (unified)
- Plugin: `0.1.0-beta.4` (unified)

This change improves:

- Version consistency across components
- Easier release management
- Better user experience
- Simplified BRAT compatibility

---

*This document is maintained as part of the Notebook Automation project documentation. For questions or suggestions, please create an issue in the repository.*
