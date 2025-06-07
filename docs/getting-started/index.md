# Getting Started

Welcome to Notebook Automation! This guide will help you get up and running quickly with the tool for automating notebook processing and metadata extraction.

## What is Notebook Automation?

Notebook Automation is a powerful command-line tool designed to process academic notebooks, extract metadata, and generate structured documentation. It supports various file formats and provides intelligent content analysis using AI services.

## Key Features

- **Multi-format Support**: Process Markdown, HTML, and text files
- **AI-Powered Analysis**: Extract meaningful metadata using OpenAI, Azure OpenAI, or IBM Foundry
- **Flexible Configuration**: Customize processing through configuration files and command-line options
- **Batch Processing**: Handle multiple files and directories efficiently
- **Structured Output**: Generate well-organized metadata and documentation

## Quick Start

### 1. Installation

Download the latest release from the [GitHub releases page](https://github.com/your-repo/notebook-automation/releases) or build from source:

```bash
git clone https://github.com/your-repo/notebook-automation.git
cd notebook-automation
dotnet build src/c-sharp/NotebookAutomation.sln
```

### 2. Basic Usage

Process a single file:

```powershell
.\na.exe process "path/to/your/notebook.md"
```

Process a directory:

```powershell
.\na.exe process "path/to/notebooks/" --recursive
```

### 3. Configuration

Create a configuration file to customize processing:

```powershell
.\na.exe config init
```

This creates a `config.json` file in your current directory. Edit it to configure AI services, output paths, and processing options.

### 4. View Results

After processing, check the output directory (default: `./output`) for:

- Extracted metadata files
- Generated summaries
- Processing logs

## Next Steps

- [Installation Guide](installation.md) - Detailed installation instructions
- [Basic Commands](basic-commands.md) - Learn essential commands
- [Configuration](../configuration/index.md) - Set up AI services and customize behavior
- [User Guide](../user-guide/index.md) - Comprehensive usage documentation

## Need Help?

- Check our [Troubleshooting Guide](../troubleshooting/index.md)
- Review the [FAQ](faq.md)
- Browse the [API Documentation](../api/index.md)
