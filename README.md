
# Notebook Automation

Notebook Automation is a comprehensive Python toolkit for managing online course notes, resources, and metadata in an Obsidian vault. It automates the conversion, organization, and enrichment of course materials, supporting advanced workflows for PDF and video reference note generation, OneDrive integration, and AI-powered summaries.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Configuration](#configuration)
- [CLI Tools](#cli-tools)
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
2. Install the required dependencies:
   ```bash
   pip install pyyaml html2text requests msal openai python-dotenv cryptography urllib3
   ```
3. (Recommended) Install in development mode:
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

See [docs/cli_tools.md](docs/cli_tools.md) for full details and usage examples.

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

```
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
config.json, metadata.yaml, setup.py, ...
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

- Python 3.6+
- pyyaml, html2text, requests, msal, openai, python-dotenv, cryptography, urllib3
- Obsidian (for viewing and working with the generated files)


## Testing

All non-trivial functions are covered by unit tests in `/tests`. Run tests with:

```bash
pytest
```


## License

MIT

## Author

Daniel Shue
