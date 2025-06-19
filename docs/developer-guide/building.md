# Building the Project

Instructions for building, testing, and packaging Notebook Automation.

## Prerequisites

### Required Software

- **.NET 8.0 SDK** - Download from [Microsoft .NET](https://dotnet.microsoft.com/download)
- **Git** - For version control
- **PowerShell** - For build scripts (included with Windows)

### Optional Tools

- **Visual Studio 2022** or **VS Code** - For development
- **Docker** - For containerized builds
- **DocFX** - For documentation generation

## Quick Start

### Clone and Build

```bash
# Clone the repository
git clone https://github.com/your-org/notebook-automation.git
cd notebook-automation

# Build the solution
dotnet build src/c-sharp/NotebookAutomation.sln
```

### Using VS Code Tasks

If using VS Code, you can use the predefined tasks:

1. Open Command Palette (`Ctrl+Shift+P`)
2. Type "Tasks: Run Task"
3. Select from available tasks:
   - `build-dotnet-sln` - Build the solution
   - `local-ci-build` - Full CI build with tests
   - `local-ci-build-quick` - Quick build without tests/formatting

## Build Scripts

### Local CI Build

The main build script replicates the CI environment locally:

```powershell
# Full build with all checks
.\scripts\build-ci-local.ps1

# Skip tests for faster builds
.\scripts\build-ci-local.ps1 -SkipTests

# Quick build (skip tests and formatting)
.\scripts\build-ci-local.ps1 -SkipTests -SkipFormat
```

### Individual Components

```bash
# Build only the solution
dotnet build src/c-sharp/NotebookAutomation.sln

# Run tests
dotnet test src/c-sharp/NotebookAutomation.sln

# Create packages
dotnet pack src/c-sharp/NotebookAutomation.sln
```

## Project Structure

### Solution Organization

```
src/c-sharp/
├── NotebookAutomation.sln          # Main solution file
├── NotebookAutomation.Core/        # Core library
├── NotebookAutomation.CLI/         # Command-line interface
├── NotebookAutomation.Tests/       # Unit tests
└── NotebookAutomation.Integration/ # Integration tests
```

### Key Projects

- **NotebookAutomation.Core** - Main processing logic
- **NotebookAutomation.CLI** - Command-line interface
- **NotebookAutomation.Tests** - Unit and integration tests

## Build Configuration

### Debug vs Release

```bash
# Debug build (default)
dotnet build

# Release build
dotnet build --configuration Release

# Release with optimizations
dotnet build --configuration Release --no-restore
```

### Target Frameworks

The project targets multiple frameworks:
- **.NET 8.0** - Primary target
- **.NET Standard 2.1** - For library compatibility

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test categories
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
```

### Test Configuration

Tests use the `coverlet.runsettings` file for coverage configuration:

```xml
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>opencover,lcov,cobertura</Format>
          <Exclude>[*]*.Tests.*</Exclude>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

## Code Quality

### Formatting

```bash
# Format code
dotnet format src/c-sharp/NotebookAutomation.sln

# Check formatting without fixing
dotnet format src/c-sharp/NotebookAutomation.sln --verify-no-changes
```

### Linting and Analysis

The project uses:
- **EditorConfig** - Consistent code style
- **StyleCop.Analyzers** - Style rule enforcement
- **Microsoft.CodeAnalysis.NetAnalyzers** - Code quality analysis

### File Headers

Add copyright headers to all source files:

```powershell
# Add headers to new files
.\scripts\add-file-headers.ps1 -Path src/c-sharp

# Preview changes without applying
.\scripts\add-file-headers.ps1 -Path src/c-sharp -DryRun
```

## Packaging

### Creating Packages

```bash
# Create NuGet packages
dotnet pack src/c-sharp/NotebookAutomation.sln --configuration Release

# Create executable packages
dotnet publish src/c-sharp/NotebookAutomation.CLI --configuration Release \
  --runtime win-x64 --self-contained
```

### Distribution

Packages are created in:
- `src/c-sharp/*/bin/Release/` - Debug packages
- `src/c-sharp/*/bin/Release/` - Release packages
- `publish/` - Published executables

## Documentation

### Building Documentation

```bash
# Install DocFX (if not already installed)
dotnet tool install -g docfx

# Build documentation
docfx docs/docfx.json --serve
```

### VS Code Tasks for Documentation

- `docfx-init` - Initialize DocFX metadata
- `docfx-build` - Build documentation
- `docfx-serve` - Build and serve documentation
- `docfx-pdf` - Generate PDF documentation

## Continuous Integration

### GitHub Actions

The project uses GitHub Actions for CI/CD:

- **Build and Test** - `.github/workflows/build.yml`
- **Documentation** - `.github/workflows/docs.yml`
- **Release** - `.github/workflows/release.yml`

### Local CI Simulation

Replicate CI environment locally:

```powershell
# Run the same steps as CI
.\scripts\build-ci-local.ps1

# Include all CI checks
.\scripts\build-ci-local.ps1 -IncludeFormatting -IncludeAnalysis
```

## Troubleshooting

### Common Build Issues

**Restore failures:**
```bash
# Clear package caches
dotnet nuget locals all --clear

# Restore with verbose output
dotnet restore --verbosity detailed
```

**Test failures:**
```bash
# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests for specific framework
dotnet test --framework net8.0
```

**Format issues:**
```bash
# Fix formatting automatically
dotnet format src/c-sharp/NotebookAutomation.sln

# Check specific formatting rules
dotnet format src/c-sharp/NotebookAutomation.sln --diagnostics IDE0001
```

### Performance Optimization

**Faster builds:**
- Use `--no-restore` when packages are already restored
- Use `--no-build` for test-only runs
- Enable parallel builds with `-m` flag

**Incremental builds:**
```bash
# Only build changed projects
dotnet build --no-dependencies

# Skip unchanged projects
dotnet build src/c-sharp/NotebookAutomation.sln --no-incremental
```

## Advanced Topics

### Custom Build Targets

Add custom MSBuild targets in `.csproj` files:

```xml
<Target Name="CustomPreBuild" BeforeTargets="PreBuildEvent">
  <Message Text="Running custom pre-build steps..." />
</Target>
```

### Multi-Platform Builds

Build for multiple platforms:

```bash
# Windows
dotnet publish --runtime win-x64 --self-contained

# Linux
dotnet publish --runtime linux-x64 --self-contained

# macOS
dotnet publish --runtime osx-x64 --self-contained
```

### Docker Builds

Build using Docker for consistent environments:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet build src/c-sharp/NotebookAutomation.sln --configuration Release
```

## See Also

- [Contributing Guidelines](contributing.md) - How to contribute to the project
- [Configuration](../configuration/index.md) - Configuring the build environment
- [Troubleshooting](../troubleshooting/index.md) - Common issues and solutions
- [Getting Started](../getting-started/index.md) - Initial setup guide
