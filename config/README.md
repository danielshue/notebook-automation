---
auto-generated-state: writable
date-created: 2025-06-06
publisher: University of Illinois at Urbana-Champaign
tags: ''
---

# Configuration Files

This directory contains the configuration files for the Notebook Automation project.

## Files

### config.json

The main configuration file containing all application settings including:

- **Paths Configuration**: File paths for vault, OneDrive, and other directories
- **Microsoft Graph API**: Settings for OneDrive integration
- **AI Services**: Configuration for Azure OpenAI and other AI providers
- **Processing Options**: Default settings for PDF processing, video handling, etc.

**Search Locations**: The application searches for this file in the following order:

1. Explicitly specified path via command line
2. Current working directory
3. `~/.notebook-automation/config.json` (user profile)
4. Application directory
5. `./config/config.json` (this location)
6. `./src/c-sharp/config.json` (development)

### metadata.yaml

Content type definitions and templates for metadata extraction including:

- **Document Templates**: Structured templates for different content types
- **Tag Hierarchies**: Predefined tag structures for course organization
- **Processing Rules**: Rules for extracting and organizing content metadata

## Usage

### Python Tools

```bash
# Use default config.json search locations
vault-generate-index

# Specify custom config file
vault-generate-index -c /path/to/custom/config.json
```

### C# Application

```bash
# Use default config.json search locations
NotebookAutomation.exe --command vault

# Specify custom config file
NotebookAutomation.exe --config /path/to/custom/config.json --command vault
```

## Security Notes

- The `config.json` file may contain sensitive information (API keys, file paths)
- Use environment variables or user secrets for production deployments
- Never commit actual API keys or sensitive data to version control
- Consider using separate config files for different environments (dev, test, prod)

## Environment-Specific Configurations

You can create environment-specific config files:

- `config.json` - Default/development configuration
- `config.prod.json` - Production configuration
- `config.test.json` - Test configuration

Load them using the `-c` or `--config` parameter.
