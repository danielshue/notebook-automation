# Download

Get started with Notebook Automation by downloading the latest release for your platform.

## Latest Release

[![Latest Release](https://img.shields.io/github/v/release/danielshue/notebook-automation?label=Download&color=brightgreen)](https://github.com/danielshue/notebook-automation/releases/latest)

Visit our [GitHub Releases](https://github.com/danielshue/notebook-automation/releases/latest) page to download the latest version.

## System Requirements

### Supported Platforms

- **Windows 10/11** (x64, ARM64)
- **macOS** (x64, ARM64/Apple Silicon)
- **Linux** (x64, ARM64)

### Prerequisites

- **.NET 10.0 Runtime** or later
- **8GB RAM** recommended for processing large documents
- **PowerShell** (for build scripts, if building from source)

## Installation Options

### Option 1: Download Pre-built Binary (Recommended)

1. Go to the [latest release](https://github.com/danielshue/notebook-automation/releases/latest) page
2. Download the appropriate package for your platform:
   - **Windows**: `notebook-automation-win-x64.zip`
   - **macOS (Intel)**: `notebook-automation-osx-x64.tar.gz`
   - **macOS (Apple Silicon)**: `notebook-automation-osx-arm64.tar.gz`
   - **Linux**: `notebook-automation-linux-x64.tar.gz`
3. Extract the archive to your preferred location
4. Add the executable to your system PATH

### Option 2: Install via .NET Tool

```bash
dotnet tool install --global NotebookAutomation.Cli
```

### Option 3: Build from Source

For developers who want to build from source:

```bash
git clone https://github.com/danielshue/notebook-automation.git
cd notebook-automation
dotnet build src/c-sharp/NotebookAutomation.sln --configuration Release
```

See the [Developer Guide](developer/building.md) for detailed build instructions.

## Obsidian Plugin

The Obsidian plugin provides native integration within your Obsidian vault.

### Installation via BRAT

1. Install the [BRAT plugin](https://github.com/TfTHacker/obsidian42-brat) in Obsidian
2. Add the repository: `danielshue/notebook-automation`
3. Enable the Notebook Automation plugin in Obsidian settings

See the [Plugin Setup Guide](getting-started/obsidian-plugin-setup.md) for detailed instructions.

## Verify Installation

After installation, verify that the CLI is working:

```bash
# Check version
na --version

# View available commands
na --help
```

## Next Steps

- **[Quick Start Guide](getting-started/quick-start.md)** - Get up and running in 5 minutes
- **[Configuration](configuration/index.md)** - Set up AI services and OneDrive integration
- **[User Guide](user-guide/index.md)** - Learn about all features and workflows

## Upgrade Guide

See the [Migration Guide](migration-guide.md) for instructions on upgrading from previous versions.

## Support

If you encounter issues during installation:

- Check the [FAQ](getting-started/faq.md)
- Review [Troubleshooting](troubleshooting/index.md)
- Ask for help in [GitHub Discussions](https://github.com/danielshue/notebook-automation/discussions)
- Report bugs in [GitHub Issues](https://github.com/danielshue/notebook-automation/issues)

---

**[📖 Documentation Home](index.md)** • **[🚀 Quick Start](getting-started/quick-start.md)** • **[💡 Features](features/index.md)**
