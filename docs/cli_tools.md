# MBA Notebook Automation CLI Tools

This document describes the available command-line tools in the MBA Notebook Automation package.

## CLI Tool Quick Reference

| Tool Name                  | Command                  | Primary Purpose                                                      |
|----------------------------|--------------------------|----------------------------------------------------------------------|
| Generate Markdown          | vault-generate-markdown  | Convert HTML/TXT files to Obsidian markdown                          |
| Convert Markdown           | vault-convert-markdown   | Convert HTML/text files to markdown with more options                |
| Generate Templates         | vault-generate-templates | Create Obsidian note templates with nested tags                      |
| Generate Dataview Queries  | vault-generate-dataview  | Generate example Dataview queries for dashboards                     |
| Ensure Metadata            | vault-ensure-metadata    | Ensure/update YAML frontmatter metadata in markdown files            |
| Generate Index             | vault-generate-index     | Create hierarchical index files for vault navigation                 |
| Extract PDF Pages          | vault-extract-pdf-pages  | Extract specific pages from PDF files                                |
| List OneDrive Folders      | vault-list-folder        | List/search contents of OneDrive folders                             |
| OneDrive Share/Resource    | vault-onedrive-share     | Create shareable links or list files in OneDrive/notebook resources  |

---


## Conversion Tools

### vault-generate-markdown

**Typical Use Cases:**
- Quickly convert a batch of HTML or TXT files to markdown for import into Obsidian.
- Prepare course materials, transcripts, or notes for vault organization.

**Best Practices:**
- Use `--dry-run` to preview changes before writing files.
- Use `--src-dirs` to process multiple input folders at once.

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

**Typical Use Cases:**
- Convert legacy HTML or text notes to markdown for Obsidian.
- Clean up formatting and ensure compatibility with markdown editors.

**Best Practices:**
- Use `--verbose` to see detailed progress.
- Use `--dest-dir` to control output location.

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

**Typical Use Cases:**
- Generate consistent note templates for lectures, assignments, or case studies.
- Standardize metadata and tag structure across your vault.

**Best Practices:**
- Use `--force` to overwrite existing templates if needed.

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

**Typical Use Cases:**
- Create dashboards or summary views in Obsidian using Dataview plugin.
- Quickly generate queries for course, lecture, or assignment tracking.

**Best Practices:**
- Use `--dry-run` to preview generated queries.

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

**Typical Use Cases:**
- Ensure all notes have consistent YAML frontmatter for program, course, and class.
- Update metadata after reorganizing folders or importing new notes.

**Best Practices:**
- Use `--dry-run` to preview changes.

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

**Typical Use Cases:**
- Build or update navigation indexes for large vaults.
- Generate lesson, course, or program indexes automatically.

**Best Practices:**
- Use `--generate-index --dry-run` to see what will be created.

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

### vault-onedrive-share

**Typical Use Cases:**
- Instantly generate a shareable link for a OneDrive file for collaboration or sharing.
- List and browse files in your OneDrive or notebook resources from the CLI.

**Best Practices:**
- Use `--notebook-resource` for files in your configured resources root.
- Use `--verbose` and `--debug` for troubleshooting authentication or API issues.

Create shareable links for OneDrive files or list contents of OneDrive or notebook resources folders. Supports interactive authentication and colorized output.

```bash
# Create a shareable link for a file in OneDrive
vault-onedrive-share --file "Documents/resume.pdf"

# List contents of a folder in OneDrive
vault-onedrive-share --list "Documents"

# Create a shareable link for a file in the notebook resources root
vault-onedrive-share --notebook-resource "Books/textbook.pdf"

# List contents of a folder in the notebook resources root
vault-onedrive-share --notebook-resource-list "Value Chain Management"

# Show detailed output
vault-onedrive-share --file "Documents/resume.pdf" --verbose

# Enable debug logging
vault-onedrive-share --file "Documents/resume.pdf" --debug
```

**Options:**
- `--file <path>`: Path to the file in OneDrive to create a shareable link
- `--list <folder>`: Path to the folder in OneDrive to list contents
- `--notebook-resource <path>`: Path to file in notebook resources root to create a shareable link
- `--notebook-resource-list <folder>`: List contents of a subfolder in notebook resources root
- `--verbose`: Show detailed output and progress information
- `--debug`: Enable debug logging output

**Authentication:**
The first time you run this tool, you will be prompted to authenticate with Microsoft. A browser window will open for you to sign in. Tokens are cached for future use.

### vault-extract-pdf-pages

**Typical Use Cases:**
- Extract specific pages or sections from large PDF files for study or sharing.
- Batch process PDFs for course packets or reading assignments.

**Best Practices:**
- Use page ranges (e.g., `1-5`) and output file options for flexibility.

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

**Typical Use Cases:**
- Browse or search OneDrive folders from the command line.
- Find files by name, ID, or location for further processing.

**Best Practices:**
- Use `--search` to quickly locate files.
- Use `--show-drives` or `--show-root` to explore your OneDrive structure.

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
