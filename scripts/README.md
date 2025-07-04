# Build and Utility Scripts

This directory contains various scripts for building, testing, and maintaining the Notebook Automation project.

## Build Scripts

### `build-ci-local.ps1` (PowerShell - Cross-Platform)

A comprehensive local CI build script that mirrors the GitHub Actions CI pipeline. Works on Windows, Linux, and macOS with PowerShell Core.

**Usage:**

```powershell
# Full build with all steps
.\scripts\build-ci-local.ps1

# Skip tests for faster builds
.\scripts\build-ci-local.ps1 -SkipTests

# Skip both tests and formatting
.\scripts\build-ci-local.ps1 -SkipTests -SkipFormat

# Debug configuration
.\scripts\build-ci-local.ps1 -Configuration Debug

# Test all architectures (x64 and ARM64)
.\scripts\build-ci-local.ps1 -TestAllArch

# Plugin-only development mode (fast iteration)
.\scripts\build-ci-local.ps1 -PluginOnly

# Plugin-only with deployment to test vault
.\scripts\build-ci-local.ps1 -PluginOnly -DeployPlugin

# Advanced C# formatting with XML documentation spacing and StyleCop
.\scripts\build-ci-local.ps1 -AdvancedCSharpFormatting

# Check C# test documentation coverage
.\scripts\build-ci-local.ps1 -CheckTestDocumentation

# Comprehensive quality check (advanced formatting + test documentation)
.\scripts\build-ci-local.ps1 -AdvancedCSharpFormatting -CheckTestDocumentation
```

**Build Pipeline Steps:**

1. Clean Solution (optional with `-SkipClean`)
2. Restore Dependencies
3. Code Formatting with `dotnet format` (optional with `-SkipFormat`)
4. Generate Version Information
5. Build Solution
6. Run Tests with Coverage (optional with `-SkipTests`)
7. Test Cross-Platform Publish Operations (Windows, Linux, macOS for both x64 and ARM64)
8. Build Obsidian Plugin (npm install and build)
9. Static Code Analysis

**Plugin Development Mode:**
- Use `-PluginOnly` for rapid plugin development (skips .NET solution build)
- Use `-DeployPlugin` to automatically deploy to test vault after building
- Plugin-only builds are significantly faster (~10-30 seconds vs 2-5 minutes)

**Cross-Platform Support:**
- Publishes executables for all platforms: Windows, Linux, macOS
- Supports both x64 and ARM64 architectures
- Uses the same naming convention as CI: `na-win-x64.exe`, `na-linux-arm64`, `na-macos-x64`, etc.
- Includes coverage report generation with ReportGenerator tool

### When to Use `build-ci-local.ps1`

**For all development scenarios:**
- 🔍 **Before committing** - Full CI validation (`.\scripts\build-ci-local.ps1`)
- ⚡ **Plugin development** - Fast iteration (`.\scripts\build-ci-local.ps1 -PluginOnly -DeployPlugin`)
- 🐛 **Debugging builds** - Mirrors CI exactly
- 🚀 **Release preparation** - Complete build validation
- 🔧 **Plugin-focused development** - Fast plugin-only builds with deployment
- 📦 **Executable integration** - Automatically includes all platform executables
- 🔄 **Quick deployment** - One-command build and deploy to test vault

**Key advantages of the unified approach:**
- **Single script to maintain** - No duplicate logic or inconsistencies
- **Consistent behavior** - Same build process for all scenarios
- **VS Code integration** - Full task support with `Ctrl+Shift+P`
- **Cross-platform support** - Works on Windows, Linux, and macOS
- **Complete CI mirroring** - Local builds match GitHub Actions exactly

## Utility Scripts

### `format-csharp-advanced.ps1`

Advanced C# code formatting script that goes beyond basic `dotnet format` with custom XML documentation spacing rules, StyleCop integration, and project-specific formatting standards.

**Usage:**

```powershell
# Apply advanced formatting fixes
.\scripts\format-csharp-advanced.ps1 -Path "src/c-sharp" -Fix

# Verify formatting without making changes
.\scripts\format-csharp-advanced.ps1 -Path "src/c-sharp" -Verify

# Use default path (current directory)
.\scripts\format-csharp-advanced.ps1 -Fix
```

**Features:**
- **XML Documentation Spacing**: Enforces proper blank lines around XML docs
- **StyleCop Integration**: Runs StyleCop analysis and reports violations
- **Method Spacing**: Ensures proper spacing between methods and classes
- **dotnet format**: Includes standard .NET formatting
- **Fix/Verify Modes**: Can both apply fixes or just verify compliance

**When to use:**
- Before committing code changes for consistent formatting
- When setting up new development environments
- For enforcing project-specific formatting standards beyond basic tools

### `check-csharp-test-documentation.ps1`

C# test documentation coverage checker that scans test methods for missing XML documentation. Ensures comprehensive documentation standards across all test methods.

**Usage:**

```powershell
# Check test documentation coverage
.\scripts\check-csharp-test-documentation.ps1 -TestPath "src/c-sharp"

# With verbose output
.\scripts\check-csharp-test-documentation.ps1 -TestPath "src/c-sharp" -VerboseOutput
```

**Features:**
- **Test Method Detection**: Finds all `[TestMethod]` attributed methods
- **Documentation Coverage**: Reports missing XML documentation
- **File-by-File Analysis**: Shows which files need documentation
- **Summary Statistics**: Provides coverage percentages and totals

**When to use:**
- Before committing test changes to ensure documentation standards
- During code reviews to validate test documentation
- As part of automated quality checks in CI/CD pipelines

### `manage-plugin-version.ps1`

Comprehensive version management script for the Obsidian plugin, specifically designed for BRAT (Beta Reviewer's Auto-update Tool) compatibility. Automates the entire release process from version updates to GitHub release creation.

**Usage:**

```powershell
# Create a beta release for BRAT testing
.\scripts\manage-plugin-version.ps1 -Version "0.1.0-beta.1" -Type "beta" -CreateRelease -PreRelease

# Create a stable release
.\scripts\manage-plugin-version.ps1 -Version "0.1.0" -Type "stable" -CreateRelease

# Create a patch release
.\scripts\manage-plugin-version.ps1 -Version "0.1.1" -Type "patch" -CreateRelease

# Update version only (no release)
.\scripts\manage-plugin-version.ps1 -Version "0.2.0-beta.1" -Type "beta"
```

**Features:**
- **Version Synchronization**: Automatically syncs versions between `package.json` and `manifest.json`
- **Git Integration**: Creates commits and tags with appropriate conventional commit messages
- **GitHub Release Creation**: Automatically creates GitHub releases with proper release notes
- **BRAT Compatibility**: Ensures proper versioning and file structure for BRAT beta testing
- **Build Validation**: Runs full plugin build and verifies all required artifacts
- **Executable Preservation**: Maintains cross-platform executables during version updates
- **Pre-release Support**: Handles both stable and pre-release versions appropriately

**Process Steps:**
1. **Version Update**: Updates `package.json` using `npm version`
2. **Synchronization**: Runs version sync script to update `manifest.json`
3. **Validation**: Verifies version consistency across files
4. **Build**: Runs complete plugin build with executable preservation
5. **Git Operations**: Commits changes and creates version tag
6. **Release Creation**: Optionally creates GitHub release with assets
7. **Documentation**: Provides next steps and release URL

**Release Types:**
- **Beta** (`-Type "beta"`): Creates pre-release for BRAT testing with beta-specific release notes
- **Stable** (`-Type "stable"`): Creates stable release with full documentation
- **Patch** (`-Type "patch"`): Creates patch release for bug fixes

**BRAT Integration:**
- Automatically formats releases for BRAT compatibility
- Includes installation instructions in release notes
- Maintains proper semantic versioning for beta tracking
- Ensures all required plugin files are included in releases

**When to use:**
- Before releasing new plugin versions for beta testing
- When preparing stable releases for general availability
- For automated version management in CI/CD pipelines
- To ensure consistent versioning across all plugin files

### `test-brat-workflow.ps1`

Comprehensive BRAT (Beta Reviewer's Auto-update Tool) compatibility testing script that validates the entire plugin build and release workflow. Ensures all requirements are met before creating releases for beta testing.

**Usage:**

```powershell
# Test complete BRAT workflow
.\scripts\test-brat-workflow.ps1
```

**Features:**
- **Prerequisites Check**: Verifies npm dependencies and development environment
- **Build Validation**: Runs complete plugin build and verifies success
- **BRAT File Verification**: Ensures all required files (main.js, manifest.json, styles.css) are present
- **Manifest Validation**: Validates manifest.json structure and required fields
- **Executable Detection**: Checks for cross-platform executables from CI builds
- **Installation Simulation**: Simulates BRAT installation process in temporary directory
- **Version Consistency**: Verifies version synchronization between package.json and manifest.json
- **Release Readiness**: Provides complete readiness assessment for BRAT releases

**Process Steps:**
1. **Environment Check**: Validates plugin directory and npm dependencies
2. **Build Process**: Runs `npm run build` with full validation
3. **File Verification**: Checks all BRAT-required files are present and valid
4. **Manifest Parsing**: Validates JSON structure and required fields
5. **Executable Audit**: Reports on available cross-platform executables
6. **BRAT Simulation**: Creates temporary installation to test plugin structure
7. **Version Sync Check**: Ensures consistent versioning across configuration files
8. **Summary Report**: Provides detailed readiness assessment and next steps

**When to use:**
- Before creating any plugin releases (beta or stable)
- After making changes to plugin configuration or build process
- To troubleshoot BRAT installation issues
- As part of release validation workflow
- Before sharing plugin with beta testers

### `download-latest-artifact.ps1`

Downloads the latest notebook-automation artifact from GitHub Actions, which includes both the Obsidian plugin files and all platform executables ready for installation.

**Usage:**

```powershell
# Download complete plugin package
.\scripts\download-latest-artifact.ps1

# List available artifacts without downloading
.\scripts\download-latest-artifact.ps1 -ListOnly

# Download specific version
.\scripts\download-latest-artifact.ps1 -Version "1.0.1"
```

**Output Structure:**
- Downloads to `../dist/notebook-automation/`
- Contains plugin files: `main.js`, `manifest.json`, `styles.css`
- Contains all platform executables: `na-win-x64.exe`, `na-linux-arm64`, `na-macos-arm64`, etc.
- Ready for direct installation into Obsidian plugins folder

## VS Code Integration

These scripts are integrated with VS Code tasks. Use `Ctrl+Shift+P` and search for "Tasks: Run Task" to access:

- `local-ci-build` - Full build pipeline
- `local-ci-build-skip-tests` - Build without tests
- `local-ci-build-quick` - Build without tests and formatting
- `local-ci-build-advanced-csharp-formatting` - Full build with advanced C# formatting
- `local-ci-build-test-documentation` - Full build with test documentation checking
- `local-ci-build-comprehensive-quality` - Full build with advanced formatting AND test documentation
- `plugin-build` - Build only the Obsidian plugin (fast)
- `plugin-build-deploy` - Build and deploy plugin to test vault
- `plugin-version-beta` - Create beta release with version prompt
- `plugin-version-stable` - Create stable release with version prompt
- `plugin-version-patch` - Create patch release with version prompt
- `test-brat-workflow` - Test complete BRAT compatibility and readiness
- `dotnet-format-solution` - Format code only
- `format-csharp-advanced` - Apply advanced C# formatting with XML doc spacing
- `format-csharp-advanced-verify` - Verify advanced C# formatting compliance
- `check-csharp-test-documentation` - Check C# test documentation coverage

## Requirements

- **PowerShell Scripts**: PowerShell 7+ (Windows/Linux/macOS)
- **.NET SDK**: .NET 9.0 or later
- **Node.js**: 18+ with npm (for Obsidian plugin builds)
- **Git**: For source control operations
