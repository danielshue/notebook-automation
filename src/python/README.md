---
auto-generated-state: writable
date-created: 2025-06-06
publisher: University of Illinois at Urbana-Champaign
tags: ''
---

# Notebook Automation

Notebook Automation is a comprehensive Python toolkit for managing online course notes, resources, and metadata in an Obsidian vault. It automates the conversion, organization, and enrichment of course materials, supporting advanced workflows for PDF and video reference note generation, OneDrive integration, and AI-powered summaries.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Configuration](#configuration)
- [CLI Tools](#cli-tools)
- [Building Executables](#building-executables)
- [Usage](#usage)
- [Examples](#examples)
- [Directory Structure](#directory-structure)
- [Testing](#testing)
- [Documentation](#documentation)
- [License](#license)
- [Author](#author)

## Features

- **Automated Tag Management:** Add, restructure, and document hierarchical tags for all notes
- **Content Conversion:** Convert HTML, TXT, PDF, and video resources to Obsidian-ready markdown
- **Index & Dashboard Generation:** Build multi-level indexes and dashboards for easy navigation
- **Metadata Consistency:** Ensure and update YAML frontmatter and metadata across all notes
- **OneDrive Integration:** Securely access, link, and share files from OneDrive using Microsoft Graph
- **AI-Powered Summaries:** Generate summaries and tags using OpenAI (where configured)
- **Robust Logging:** Centralized, configurable logging for all scripts and tools
- **Extensible CLI:** All major functionality is available as pip-installed commands and standalone EXEs

## Installation

1. Clone this repository:

   ```bash
   git clone https://github.com/danielshue/notebook-automation.git
   ```

2. Create and activate a virtual environment:

   ```bash
   # Navigate to project directory
   cd notebook-automation

   # Create virtual environment
   python -m venv venv

   # Activate virtual environment
   # On Windows
   venv\Scripts\activate

   # On Linux/macOS
   source venv/bin/activate
   ```

3. Install the required dependencies:

   ```bash
   pip install -e .
   ```

   For EPUB conversion, you must also install [Pandoc](https://pandoc.org/installing.html):

   ```bash
   # On Ubuntu/WSL:
   sudo apt-get install pandoc
   # On MacOS (Homebrew):
   brew install pandoc
   # On Windows: Download and install from https://github.com/jgm/pandoc/releases
   ```

4. (Recommended) Install in development mode:

   ```bash
   pip install -e .
   ```

## Configuration

All tools use a centralized `config.json` (default: `~/.notebook_automation/config.json`).
See [docs/configuration_guide.md](docs/configuration_guide.md) for all options.

## CLI Tools

All CLI tools are available as both pip-installed commands and standalone EXEs (see [docs/cli_tools.md](docs/cli_tools.md)):

| Command                        | Description                                 |
|--------------------------------|---------------------------------------------|
| vault-add-nested-tags          | Add nested tags to notes                    |
| vault-add-example-tags         | Add example tags to notes                   |
| vault-clean-index-tags         | Clean up index tags                         |
| vault-consolidate-tags         | Consolidate tags across notes               |
| vault-generate-tag-doc         | Generate tag documentation                  |
| vault-restructure-tags         | Restructure tag hierarchies                 |
| vault-generate-video-meta      | Generate video metadata                     |
| vault-ensure-metadata          | Ensure consistent note metadata             |
| vault-create-class-dashboards  | Create dashboards for classes               |
| vault-generate-pdf-notes       | Generate notes from PDFs                    |
| vault-generate-markdown        | Generate markdown from source               |
| vault-generate-templates       | Generate note templates                     |
| vault-extract-pdf-pages        | Extract pages from PDFs                     |
| vault-generate-dataview        | Generate dataview queries                   |
| vault-list-folder              | List contents of a folder                   |
| vault-onedrive-share           | Share files via OneDrive                    |
| vault-tag-manager              | Manage tags in notes                        |
| vault-convert-markdown         | Convert markdown formats                    |
| vault-configure                | Configure vault settings                    |
| vault-generate-index           | Generate a vault index                      |
| vault-update-glossary          | Update glossary pages with callouts         |

See [docs/cli_tools.md](docs/cli_tools.md) for full details and usage examples.

## Building Executables

**Recommended:** Build executables under a Bash shell (Linux, macOS, or Windows Subsystem for Linux/WSL). This ensures correct path handling and compatibility with the provided `.spec` files and accompanying longer file names that may not work well in Windows CMD or PowerShell.

**Note:** The `build_all_exes.sh` script is designed for Linux/macOS/WSL. If you are using Windows, consider using WSL or Git Bash to run the script.

To build standalone executable files for all CLI tools:

1. Make sure you have PyInstaller installed in your (activated) virtual environment:

   ```bash
   pip install pyinstaller
   ```

2. Build all executables using the included script:

   ```bash
   # On Linux/macOS/WSL (recommended)
   chmod +x build_all_exes.sh
   ./build_all_exes.sh
   ```

   > **Note:** On Windows, it is strongly recommended to use WSL or Git Bash to run this script. Native Windows shells may not handle paths correctly for this project.

3. After building, the executable files will be available in the `dist/` directory:

   - `dist/add_nested_tags`
   - `dist/clean_index_tags`
   - `dist/configure`
   - And others...

These executables can be distributed and run on Windows systems without requiring Python installation.

## Usage

After installation, use the CLI entry points for all tag and note management tasks. For example:

```bash
# Add nested tags to all notes in the current directory (dry run, verbose)
vault-add-nested-tags . --dry-run --verbose

# Clean tags from all index files in a directory
vault-clean-index-tags ./notes --verbose

# Consolidate tags in all notes
vault-consolidate-tags ./notes --verbose

# Generate tag documentation
vault-generate-tag-doc --help
```

For PDF and video processing, use the following CLI tools:

```bash
vault-generate-pdf-notes --folder <folder> [--force] [--dry-run]
vault-generate-video-meta --folder <folder> [--force] [--dry-run]
```

All tools support `--help` for usage and `--verbose` for colorized, detailed output.

## Examples

Convert HTML/TXT files and generate indexes for a specific course:

```bash
vault-generate-markdown --all --source /path/to/accounting-for-managers
```

Generate PDF notes for all files in a OneDrive folder:

```bash
vault-generate-pdf-notes --folder "Value Chain Management/Managerial Accounting Business Decisions/Case Studies" --force
```

Generate video notes for a single video file:

```bash
vault-generate-video-meta -f "Value Chain Management/Managerial Accounting Business Decisions/Module 1/Video1.mp4" --force
```

## Directory Structure

docs/                     # Documentation, guides, and feature docs
tests/                    # Unit and integration tests
archived/                 # Older and backup scripts
debug/                    # Debugging and troubleshooting scripts
data/, cache/, logs/      # Data, cache, and log directories
config/config.json, config/metadata.yaml, setup.py, ...

```text
notebook_automation/
├── cli/                  # All CLI entry points (pip/EXE)
├── tools/                # Core functionality modules (utils, pdf, auth, etc.)
├── tags/                 # Tag management scripts
├── obsidian/             # Obsidian-specific tools
├── video/                # Video processing tools
├── utilities/            # General helper scripts
├── __init__.py           # Package metadata
├── ...
docs/                     # Documentation, guides, and feature docs
tests/                    # Unit and integration tests
archived/                 # Older and backup scripts
debug/                    # Debugging and troubleshooting scripts
data/, cache/, logs/      # Data, cache, and log directories
config/config.json, config/metadata.yaml, setup.py, ...
```

See [docs/final_organization.md](docs/final_organization.md) for a full directory breakdown.

## Testing

All non-trivial functions are covered by unit tests in `/tests`. Run tests with:

```bash
pytest
```

## Documentation

- [CLI Tools Reference](docs/cli_tools.md)
- [Workspace Organization](docs/final_organization.md)
- [Project Backlog & Roadmap](docs/project_backlog.md)
- [Configuration Guide](docs/configuration_guide.md)

## Templater Integration

The tool includes Templater templates for Obsidian that can be used to create standardized notes:

- **Lesson Note**: Template for creating structured lesson notes with sections for summary, key points, questions, and action items.

## CLI Arguments

All CLI tools support `--help` for full argument details. Common options include:

- `--verbose` : Enable colorized, detailed output (per-file progress, summary)
- `--dry-run` : Show what would be changed, but do not modify files
- `--folder`  : Specify a folder to process (for PDF/video tools)
- `--force`   : Overwrite existing files/notes

See each tool's `--help` for more options.

## Requirements

- Python 3.8+
- ruamel.yaml, html2text, requests, msal, openai, python-dotenv, cryptography, urllib3
- tqdm, retry, loguru, python-docx, colorlog, beautifulsoup4, pymsteams
- pypandoc (for EPUB to Markdown conversion)
- openai (for AI-powered summaries and transcript processing)
- Pandoc (required for EPUB conversion, must be installed separately)
- Obsidian (for viewing and working with the generated files)

## Testing for Non-Trivial Functions

All non-trivial functions are covered by unit tests in `/tests`. Run tests with:

```bash
pytest
```

## License

MIT

## Author

Dan Shue
