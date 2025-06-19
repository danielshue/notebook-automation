# Getting Started

This guide will help you get Notebook Automation up and running quickly.

## Prerequisites

Before installing Notebook Automation, ensure you have:

- **.NET 9.0 SDK** or later ([Download here](https://dotnet.microsoft.com/download))
- **Windows OS** (primary support), Linux/macOS (experimental)
- **OpenAI API Key** or **Azure OpenAI** access for AI features
- **Git** for cloning the repository

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/danielshue/notebook-automation.git
cd notebook-automation
```

### 2. Build the Solution

```bash
dotnet build src/c-sharp/NotebookAutomation.sln
```

### 3. Configure the Application

Copy the example configuration:

```bash
cp config/config.json config/my-config.json
```

Edit `config/my-config.json` and update:

```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "your-openai-api-key",
    "Model": "gpt-4",
    "Temperature": 0.3
  },
  "Paths": {
    "VaultPath": "C:\\path\\to\\your\\obsidian\\vault",
    "OutputPath": "C:\\path\\to\\output"
  }
}
```

### 4. Test the Installation

```bash
dotnet run --project src/c-sharp/NotebookAutomation.Console -- --help
```

## First Steps

### Process Your First PDF

```bash
dotnet run --project src/c-sharp/NotebookAutomation.Console -- process-pdf "path/to/document.pdf"
```

### Set up OneDrive Integration

1. Configure OneDrive credentials in your config file
2. Test the connection:

```bash
dotnet run --project src/c-sharp/NotebookAutomation.Console -- onedrive-test
```

## Next Steps

- Read the [User Guide](../user-guide/index.md) for detailed usage instructions
- See [Configuration](../configuration/index.md) for advanced settings
- Try the [Tutorials](../tutorials/index.md) for hands-on examples

## Getting Help

If you encounter issues:

1. Check the [Troubleshooting Guide](../troubleshooting/index.md)
2. Review the logs in your configured log directory
3. Open an issue on [GitHub](https://github.com/danielshue/notebook-automation/issues)

## Overview

Notebook Automation is designed to transform the way you manage educational content. Whether you're working with course materials from online platforms like Coursera, edX, or your own educational resources, this tool helps you create a structured, searchable knowledge base in Obsidian.

## Prerequisites

- **For AI features**: OpenAI API key or Azure Cognitive Services
- **For OneDrive integration**: Microsoft Graph API access (optional)
- **No .NET SDK required** when using pre-built executables

## Quick Start

1. **[Download](installation.md)** the latest release
2. **Configure** your settings with `na config`
3. **Process** your first PDF or video file
4. **Explore** your generated Obsidian vault

Ready to begin? Start with [Installation](installation.md)!
