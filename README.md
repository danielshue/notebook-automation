---
auto-generated-state: writable
date-created: 2025-06-06
publisher: University of Illinois at Urbana-Champaign
tags: ''
---

# Notebook Automation

A comprehensive, cross-platform toolkit for automating the management and organization of educational content in Obsidian vaults. This project provides intelligent content processing, metadata extraction, and seamless integration between OneDrive resources and Obsidian knowledge management systems.

## 🌟 Overview

Notebook Automation transforms the way you manage course materials, whether for MBA programs, online courses, or any structured educational content. It automates the tedious tasks of organizing files, extracting metadata, generating summaries, and maintaining consistency across your knowledge base.

### Key Capabilities

- **🤖 Intelligent Content Processing**: Automatically converts PDFs, videos, HTML, and other formats to structured Markdown notes
- **📊 Metadata Extraction**: Smart detection of course hierarchy, programs, modules, and lessons from file paths and content
- **🏷️ Advanced Tag Management**: Hierarchical tag generation and consolidation for enhanced content discovery
- **☁️ OneDrive Integration**: Seamless file access, sharing, and synchronization with Microsoft Graph API
- **🧠 AI-Powered Summaries**: OpenAI and Azure AI integration for generating content summaries and insights
- **📚 Index Generation**: Automated creation of navigation structures and dashboards
- **🔄 Cross-Platform Support**: Available as both Python CLI tools and C# executables

## 🏗️ Architecture

The project is structured as a dual-language solution to provide maximum flexibility and deployment options:

```text
notebook-automation/
├── src/
│   ├── c-sharp/          # .NET Core CLI application
│   │   ├── NotebookAutomation.Core/      # Core business logic
│   │   ├── NotebookAutomation.Cli/       # Command-line interface
│   │   └── NotebookAutomation.*.Tests/   # Unit tests
│   ├── python/           # Python toolkit
│   │   ├── notebook_automation/          # Core package
│   │   │   ├── cli/                     # CLI entry points
│   │   │   ├── tools/                   # Processing modules
│   │   │   └── utilities/               # Helper functions
│   │   └── tests/                       # Test suite
│   └── tests/            # Integration tests
├── docs/                 # Documentation
├── scripts/              # Build and utility scripts
├── config/               # Configuration files
│   ├── config.json       # Configuration template
│   └── metadata.yaml     # Content type definitions
```

## ⚡ Quick Start

### Prerequisites

- **For Python**: Python 3.8+ and pip
- **For C#**: .NET 9.0 SDK
- **For AI features**: OpenAI API key or Azure Cognitive Services
- **For OneDrive**: Microsoft Graph API access (optional)

### Installation

#### Option 1: Python Package (Recommended for Linux/macOS)

```bash
# Clone the repository
git clone https://github.com/danielshue/notebook-automation.git
cd notebook-automation/src/python

# Install with pip
pip install -e .

# Verify installation
vault-add-nested-tags --help
```

#### Option 2: C# Executable (Recommended for Windows)

```bash
# Build from source
cd src/c-sharp
dotnet build NotebookAutomation.sln

# Or use pre-built executable
# Download from GitHub releases
notebookautomation.exe --help
```

### Initial Configuration

```bash
# Create default configuration
vault-configure create

# View current settings
vault-configure show

# Update specific settings
vault-configure update notebook_vault_root "/path/to/your/vault"
```

## 🚀 Core Features

### Content Processing Commands

#### PDF Processing

Convert PDF documents to structured Markdown notes with metadata:

```bash
# Process single PDF
vault-generate-pdf-notes --input document.pdf

# Process entire directory
vault-generate-pdf-notes --input /path/to/pdfs --force

# With AI summarization
vault-generate-pdf-notes --input document.pdf --summary
```

#### Video Processing

Extract metadata and create reference notes for video content:

```bash
# Process video files
vault-generate-video-meta --input /path/to/videos

# Include transcript processing
vault-generate-video-meta --input video.mp4 --transcript

# Skip OneDrive linking
vault-generate-video-meta --input /path/to/videos --no-share-links
```

#### Markdown Generation

Convert HTML, TXT, and other formats to Obsidian-compatible Markdown:

```bash
# Convert all supported files in directory
vault-generate-markdown /path/to/source

# Dry run to preview changes
vault-generate-markdown /path/to/source --dry-run --verbose
```

### Tag Management

#### Hierarchical Tag Generation

Automatically generate nested tags based on content metadata:

```bash
# Add nested tags to all notes
vault-add-nested-tags /path/to/notes --verbose

# Dry run to preview changes
vault-add-nested-tags /path/to/notes --dry-run
```

#### Tag Maintenance

Clean up and consolidate existing tags:

```bash
# Remove duplicates and sort tags
vault-consolidate-tags /path/to/notes

# Clean tags from index files
vault-clean-index-tags /path/to/notes

# Restructure tag format
vault-restructure-tags /path/to/notes
```

### Metadata Management

#### Automatic Metadata Extraction

Ensure consistent metadata across your vault:

```bash
# Update metadata for all files
vault-ensure-metadata /path/to/vault

# Process specific directory
vault-ensure-metadata /path/to/course --program "Data Science"

# Inspect metadata without changes
vault-ensure-metadata /path/to/notes --inspect
```

#### Index Generation

Create navigation structures and dashboards:

```bash
# Generate course dashboards
vault-create-class-dashboards /path/to/course

# Create comprehensive indexes
vault-generate-indexes /path/to/vault
```

## 🔧 Configuration

The system uses a centralized `config/config.json` file for all settings. Key configuration areas include:

### Paths Configuration

```json
{
  "paths": {
    "onedrive_fullpath_root": "C:/Users/username/OneDrive",
    "notebook_vault_fullpath_root": "D:/Vault/Projects/Education",
    "metadata_file": "config/metadata.yaml",
    "logging_dir": "logs"
  }
}
```

### AI Services

```json
{
  "aiservice": {
    "provider": "azure",
    "azure": {
      "endpoint": "https://your-resource.cognitiveservices.azure.com/",
      "deployment": "gpt-4o",
      "model": "gpt-4o"
    }
  }
}
```

### Microsoft Graph Integration

```json
{
  "microsoft_graph": {
    "client_id": "your-app-id",
    "scopes": ["Files.ReadWrite.All", "Sites.Read.All"]
  }
}
```

For detailed configuration options, see [Configuration Guide](docs/UserSecrets.md).

## 📖 Documentation

- **[Metadata Extraction System](docs/Metadata-Extraction-System.md)** - How the system intelligently extracts and assigns metadata
- **[User Secrets Guide](docs/UserSecrets.md)** - Secure configuration of API keys and credentials
- **[Python Documentation](src/python/README.md)** - Python-specific features and CLI tools
- **[C# Documentation](src/c-sharp/README.md)** - .NET implementation and development setup

## 🧪 Development & Testing

### Building the Project

#### Python Development

```bash
cd src/python
pip install -e ".[dev]"
pytest tests/
```

#### C# Development

```bash
cd src/c-sharp
dotnet restore
dotnet build
dotnet test
```

### Local CI Build

Test the complete build pipeline locally:

```bash
# Full build with tests
pwsh -File scripts/build-ci-local.ps1

# Quick build without tests
pwsh -File scripts/build-ci-local.ps1 -SkipTests

# Format code only
pwsh -File scripts/build-ci-local.ps1 -SkipTests -SkipFormat
```

### Available VS Code Tasks

- `build-dotnet-sln` - Build the C# solution
- `local-ci-build` - Run complete CI pipeline locally
- `local-ci-build-skip-tests` - Build without running tests
- `dotnet-format-solution` - Format C# code

## 🤝 Contributing

1. **Fork the repository** and create a feature branch
2. **Follow coding standards**:
   - C#: Microsoft conventions with modern C# features
   - Python: PEP 8 with comprehensive docstrings
3. **Add tests** for new functionality
4. **Update documentation** as needed
5. **Submit a pull request** with clear description

### Coding Guidelines

- Use dependency injection for testability
- Implement comprehensive error handling
- Follow the existing project structure
- Write XML documentation for C# and docstrings for Python
- Ensure cross-platform compatibility

## 📊 Project Status

### Current Features

- ✅ PDF to Markdown conversion with metadata
- ✅ Video metadata extraction and note generation
- ✅ Hierarchical tag management system
- ✅ OneDrive integration via Microsoft Graph
- ✅ AI-powered content summarization
- ✅ Cross-platform CLI tools (Python + C#)
- ✅ Automated metadata extraction
- ✅ Index and dashboard generation

### Roadmap

- 🔄 Enhanced AI integration with multiple providers
- 🔄 Web-based dashboard for vault management
- 🔄 Plugin system for custom processors
- 🔄 Real-time synchronization with cloud services
- 🔄 Enhanced mobile support

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙋 Support

- **Issues**: Report bugs or request features on [GitHub Issues](https://github.com/danielshue/notebook-automation/issues)
- **Discussions**: Join the community on [GitHub Discussions](https://github.com/danielshue/notebook-automation/discussions)
- **Documentation**: Browse the [docs](docs/) folder for detailed guides

---

Made with ❤️ for the education and knowledge management community
<!-- CI Build Test - 06/16/2025 08:58:41 -->
