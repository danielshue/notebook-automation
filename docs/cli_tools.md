# MBA Notebook Automation CLI Tools

This document describes the available command-line tools in the MBA Notebook Automation package.

## Conversion Tools

### vault-generate-markdown

Convert HTML and TXT files to properly formatted markdown for Obsidian vault.

```bash
# Convert files in current directory to Obsidian vault
vault-generate-markdown

# Convert files from multiple sources
vault-generate-markdown --src-dirs ./notes ./documents

# Specify custom destination directory
vault-generate-markdown --dest-dir /path/to/vault

# Preview changes without modifying files
vault-generate-markdown --dry-run --verbose
```

### vault-convert-markdown

Convert HTML and text files to properly formatted markdown.

```bash
# Convert a single file
vault-convert-markdown file.html

# Convert all files in a directory
vault-convert-markdown --src-dir ./notes

# Preview conversion without writing files
vault-convert-markdown --dry-run file.txt

# Show detailed progress during conversion
vault-convert-markdown --verbose *.html

# Specify custom destination directory
vault-convert-markdown --dest-dir /path/to/output file.html
```

## Template Tools

### vault-generate-templates

Generate Obsidian templates with nested tags for different types of notes (MBA Lecture Notes, Case Studies, Assignments, etc.).

```bash
# Generate templates in default location
vault-generate-templates

# Generate templates in a specific folder
vault-generate-templates --template-path /path/to/templates

# Force overwrite existing templates
vault-generate-templates --force

# Show detailed information during generation
vault-generate-templates --verbose
```

### vault-generate-dataview

Generate example Dataview queries that leverage the MBA tag structure. These queries can be used to create dynamic views and dashboards in Obsidian.

```bash
# Generate queries in default location
vault-generate-dataview

# Generate in a specific location
vault-generate-dataview --output /path/to/file.md

# Preview without making changes
vault-generate-dataview --dry-run
```

## Metadata Tools

### vault-ensure-metadata

Ensures consistent metadata in markdown files based on directory structure, updating program, course, and class fields in YAML frontmatter.

```bash
# Update metadata in the default Obsidian vault
vault-ensure-metadata

# Update metadata in a specific directory
vault-ensure-metadata /path/to/directory

# Preview changes without modifying files
vault-ensure-metadata --dry-run

# Show detailed information about changes
vault-ensure-metadata --verbose
```

## Index Tools

### vault-generate-index

Generate hierarchical index files for an Obsidian vault, supporting multiple levels of organization.

```bash
# Generate all indexes
vault-generate-index --source /path/to/vault --generate-index

# Generate only specific type of indexes
vault-generate-index --source /path/to/vault --index-type lesson-index

# Convert HTML/TXT files to markdown and generate indexes
vault-generate-index --source /path/to/vault --all

# Show what would be done without making changes
vault-generate-index --source /path/to/vault --generate-index --dry-run
```

## PDF Tools

### vault-extract-pdf-pages

Extract specific pages from a PDF file. Supports page ranges and multiple PDFs.

```bash
# Extract pages 1-5 to auto-named output
vault-extract-pdf-pages input.pdf 1-5

# Extract to specific output file
vault-extract-pdf-pages input.pdf 1-5 output.pdf

# Extract from PDFs in a directory
vault-extract-pdf-pages directory/ 1,3,5

# Handle paths with spaces
vault-extract-pdf-pages "path with spaces" 1-5
```

### vault-list-folder

List contents of OneDrive folders and search for files.

```bash
# List contents of a folder
vault-list-folder "path/to/folder"

# Search for a specific file
vault-list-folder --search filename.pdf

# Show available drives
vault-list-folder --show-drives

# Show root contents
vault-list-folder --show-root

# Access file by ID
vault-list-folder --file-id {id}

# Use alternate base path
vault-list-folder --base "alt/base" path/to/folder
```

## Installation

To install or update the CLI tools:

```bash
# Development install
pip install -e .

# Or for production
pip install .
```

After installation, all tools will be available directly from the command line.
