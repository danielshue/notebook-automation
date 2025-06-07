# Installation Guide

This guide covers different ways to install and set up Notebook Automation.

## System Requirements

- **.NET 9.0 Runtime** or later
- **Windows, macOS, or Linux**
- **Minimum 4GB RAM** (8GB recommended for large document processing)
- **500MB free disk space**

## Installation Options

### Option 1: Download Pre-built Binary (Recommended)

1. Go to the [GitHub Releases page](https://github.com/your-repo/notebook-automation/releases)
2. Download the appropriate package for your platform:
   - `notebook-automation-win-x64.zip` for Windows 64-bit
   - `notebook-automation-win-arm64.zip` for Windows ARM64
   - `notebook-automation-linux-x64.tar.gz` for Linux 64-bit
   - `notebook-automation-osx-x64.tar.gz` for macOS Intel
   - `notebook-automation-osx-arm64.tar.gz` for macOS Apple Silicon

3. Extract the archive to your preferred location
4. Add the extraction directory to your system PATH (optional)

### Option 2: Build from Source

#### Prerequisites

- **.NET 9.0 SDK**
- **Git**

#### Steps

1. Clone the repository:

   ```powershell
   git clone https://github.com/your-repo/notebook-automation.git
   cd notebook-automation
   ```

2. Build the solution:

   ```powershell
   dotnet build src/c-sharp/NotebookAutomation.sln --configuration Release
   ```

3. Publish the CLI application:

   ```powershell
   dotnet publish src/c-sharp/NotebookAutomation.Cli/NotebookAutomation.Cli.csproj --configuration Release --output ./publish
   ```

4. The executable `na.exe` (Windows) or `na` (Linux/macOS) will be in the `./publish` directory.

## Verification

Verify your installation by running:

```powershell
.\na.exe --version
```

You should see output similar to:

```
Notebook Automation CLI v1.0.0
.NET 9.0.0
```

## Configuration Setup

After installation, you'll want to set up configuration for AI services:

1. Initialize configuration:

   ```powershell
   .\na.exe config init
   ```

2. Edit the generated `config.json` file to add your AI service credentials
3. Test the configuration:

   ```powershell
   .\na.exe config validate
   ```

## Next Steps

- [Basic Commands](basic-commands.md) - Learn essential commands
- [Configuration](../configuration/index.md) - Set up AI services
- [User Guide](../user-guide/index.md) - Comprehensive usage guide

## Troubleshooting Installation

### Common Issues

**"dotnet command not found"**

- Install the .NET SDK from [Microsoft's website](https://dotnet.microsoft.com/download)

**Permission denied errors**

- On Linux/macOS, make the executable file executable:

  ```powershell
  chmod +x na
  ```

#### Missing dependencies

- Ensure you have the .NET runtime installed for your platform

For more help, see our [Troubleshooting Guide](../troubleshooting/index.md).
