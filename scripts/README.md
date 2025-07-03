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

# Enhanced formatting with XML documentation spacing
.\scripts\build-ci-local.ps1 -EnhancedFormatting
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
- `local-ci-build-enhanced-formatting` - Full build with advanced C# formatting
- `plugin-build` - Build only the Obsidian plugin (fast)
- `plugin-build-deploy` - Build and deploy plugin to test vault
- `dotnet-format-solution` - Format code only
- `format-csharp-advanced` - Apply advanced C# formatting with XML doc spacing
- `format-csharp-advanced-verify` - Verify advanced C# formatting compliance

## Requirements

- **PowerShell Scripts**: PowerShell 7+ (Windows/Linux/macOS)
- **.NET SDK**: .NET 9.0 or later
- **Node.js**: 18+ with npm (for Obsidian plugin builds)
- **Git**: For source control operations
