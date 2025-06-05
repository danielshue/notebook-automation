# Build and Utility Scripts

This directory contains various scripts for building, testing, and maintaining the Notebook Automation project.

## Build Scripts

### `build-ci-local.ps1` (PowerShell - Windows)

A comprehensive local CI build script that mirrors the GitHub Actions CI pipeline.

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
```

**Build Pipeline Steps:**

1. Clean Solution (optional with `-SkipClean`)
2. Restore Dependencies
3. Code Formatting with `dotnet format` (optional with `-SkipFormat`)
4. Build Solution
5. Run Tests with Coverage (optional with `-SkipTests`)
6. Test Publish Operations
7. Static Code Analysis

### `build-ci-local.sh` (Bash - Linux/macOS)

Cross-platform equivalent of the PowerShell build script with the same functionality.

**Usage:**

```bash
# Full build with all steps
./scripts/build-ci-local.sh

# Skip tests for faster builds
./scripts/build-ci-local.sh --skip-tests

# Skip both tests and formatting
./scripts/build-ci-local.sh --skip-tests --skip-format
```

## Utility Scripts

### `download-latest-artifact.ps1`

Downloads the latest build artifacts from GitHub Actions for testing and deployment purposes.

**Usage:**

```powershell
.\scripts\download-latest-artifact.ps1
```

## VS Code Integration

These scripts are integrated with VS Code tasks. Use `Ctrl+Shift+P` and search for "Tasks: Run Task" to access:

- `local-ci-build` - Full build pipeline
- `local-ci-build-skip-tests` - Build without tests
- `local-ci-build-quick` - Build without tests and formatting
- `dotnet-format-solution` - Format code only

## Requirements

- **PowerShell Scripts**: PowerShell 7+ (Windows/Linux/macOS)
- **Bash Scripts**: Bash shell (Linux/macOS)
- **.NET SDK**: .NET 9.0 or later
- **Git**: For source control operations
