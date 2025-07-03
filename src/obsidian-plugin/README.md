# Notebook Automation Obsidian Plugin

## Overview

This directory contains the source code for the **Notebook Automation Obsidian Plugin**, an extension for [Obsidian](https://obsidian.md/) that integrates advanced automation, metadata extraction, and cross-platform CLI capabilities into your Obsidian vault.

## Features

- Seamless integration with the Notebook Automation CLI tools
- Automated metadata extraction and processing for notes
- Cross-platform support (Windows, macOS, Linux)
- Customizable plugin configuration and default settings
- Ships with platform-specific CLI executables for advanced operations
- Modern TypeScript codebase with robust build and deployment scripts

## Installation

1. **Build the plugin:**
   - Use the unified build script from the repository root:
     ```powershell
     # Fast plugin development build and deploy
     .\scripts\build-ci-local.ps1 -PluginOnly -DeployPlugin
     
     # Full CI build with plugin deployment
     .\scripts\build-ci-local.ps1 -DeployPlugin
     ```
2. **Copy to your Obsidian vault:**
   - The script will copy `main.js`, `manifest.json`, `default-config.json`, `styles.css`, and all required CLI executables to your vault's `.obsidian/plugins/notebook-automation` directory.
3. **Enable the plugin in Obsidian:**
   - Open Obsidian, go to Settings → Community plugins, and enable "Notebook Automation".

## Usage

- Use the plugin's commands and UI to trigger automation tasks, extract metadata, and interact with the CLI tools.
- Configuration can be customized via `default-config.json` or the plugin settings panel (if available).
- For advanced CLI usage, see the main project documentation.

## Development

### Build & Deploy

**From repository root (recommended):**
```powershell
# Fast plugin development iteration
.\scripts\build-ci-local.ps1 -PluginOnly -DeployPlugin

# Full build with CI validation + plugin deployment  
.\scripts\build-ci-local.ps1 -DeployPlugin

# Plugin build only (no deployment)
.\scripts\build-ci-local.ps1 -PluginOnly
```

**Manual build (from plugin directory):**
```bash
npm install
npm run build
```

The unified build script will:
- Install dependencies (`npm install`)
- Build the TypeScript source (`npm run build`)
- Copy all required files and the platform-specific CLI executables (for Windows, macOS, and Linux) to your test vault
- Provide full CI validation when using the complete build pipeline

### File Structure

- `main.ts` / `main.js` — Plugin entry point (TypeScript/JavaScript)
- `manifest.json` — Obsidian plugin manifest
- `default-config.json` — Default configuration for the plugin
- `styles.css` — Plugin styles
- `dist/` — Compiled JS and CLI executables
- Use `../../scripts/build-ci-local.ps1` for unified build and deployment

## Contributing

Contributions are welcome! Please see the main repository's guidelines for code style, testing, and pull requests. For plugin-specific changes, ensure you:
- Follow the established build and deployment process
- Test changes in a local Obsidian vault
- Update documentation as needed

## Security

- No sensitive information is stored in this plugin
- All user inputs are sanitized before processing
- See the main repository's SECURITY.md for more details

## License

This plugin is part of the [Notebook Automation](https://github.com/danielshue/notebook-automation) project and is licensed under the MIT License.
