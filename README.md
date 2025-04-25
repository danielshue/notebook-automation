# Notebook Automation Tool

A comprehensive toolkit for managing course notes and resources in an Obsidian vault. The suite of scripts automates the process of converting course materials, generating index files, and organizing content for easy navigation and retrieval. It also supports advanced workflows for PDF and video reference note generation with OneDrive integration and AI-powered summaries.

## Features

### File Conversion
- Converts HTML files to Markdown with proper formatting
- Converts transcript TXT files to Markdown
- Cleans up filenames by removing numbering prefixes and improving readability
- Adds YAML frontmatter with template type and auto-generated state properties
- Automatically adds appropriate titles to all generated files
- Before creating a file, the tool checks if it already exists in the target directory. If it does, the file will only be overwritten if the `--force` flag is set; otherwise, it will be skipped and a message will be shown.

### Index Generation
- Creates a hierarchical structure of index files for easy navigation
- Supports a 6-level hierarchy: Main → Program → Course → Class → Module → Lesson
- Generates Obsidian-compatible wiki-links between index levels
- Creates back-navigation links for seamless browsing (with correct filenames)
- Respects "readonly" marked files to prevent overwriting customized content

### PDF & Video Reference Note Generation
- Scans PDFs and videos from OneDrive MBA-Resources folder
- Authenticates with Microsoft Graph API for secure access to OneDrive
- Creates shareable links for PDFs and videos stored in OneDrive
- Generates markdown notes in the Obsidian vault with both local file:// links and shareable OneDrive links
- Extracts text from PDFs and transcripts, generating AI-powered summaries and tags (OpenAI integration)
- Infers course and program information from file paths
- Maintains consistent folder structure between OneDrive and Obsidian vault
- Robust error handling, retry mechanism, and secure token management
- Preserves user modifications to notes (respects auto-generated-state flag)
- Supports dry-run, retry, and force-overwrite modes

### Content Organization
- Automatically categorizes content into readings, videos, transcripts, etc.
- Adds appropriate icons for different content types
- Implements a tagging system for enhanced content discovery
- Supports structural, cognitive, and workflow tags

## Directory Structure

The tool expects and generates the following directory structure:
```
- Root (main-index)
  - Program Folders (program-index)
    - Course Folders (course-index)
      - Class Folders (class-index)
        - Case Study Folders (case-study-index)
        - Module Folders (module-index)
          - Live Session Folder (live-session-index)
          - Lesson Folders (lesson-index)
            - Content Files (readings, videos, transcripts, etc.)
```

## Installation

1. Clone this repository:
```
git clone https://github.com/danielshue/notebook-automation.git
```

2. Install the required dependencies:
```
pip install pyyaml html2text requests msal openai python-dotenv cryptography urllib3
```

## Usage

The toolkit provides several command-line options across its scripts:

- **Convert files only**:
  ```
  python generate_markdown_from_html_and_text_in_vault.py --convert --source <path>
  ```

- **Generate indexes only**:
  ```
  python generate-index_in_vault.py --generate-index --source <path>
  ```

- **Generate PDF notes from OneDrive**:
  ```
  python generate_pdf_notes_from_onedrive.py --folder <folder> [--force] [--dry-run] [--no-share-links]
  ```

- **Generate video notes from OneDrive**:
  ```
  python generate_video_meta_from_onedrive.py --folder <folder> [--force] [--dry-run] [--no-summary]
  ```

- **Perform both conversion and index generation**:
  ```
  python generate-index_in_vault.py --all --source <path>
  ```

## Examples

Convert HTML/TXT files and generate indexes for a specific course:
```
python generate_markdown_from_html_and_text_in_vault.py --all --source /path/to/accounting-for-managers
```

Generate PDF notes for all files in a OneDrive folder:
```
python generate_pdf_notes_from_onedrive.py --folder "Value Chain Management/Managerial Accounting Business Decisions/Case Studies" --force
```

Generate video notes for a single video file:
```
python generate_video_meta_from_onedrive.py -f "Value Chain Management/Managerial Accounting Business Decisions/Module 1/Video1.mp4" --force
```

## Templater Integration

The tool includes Templater templates for Obsidian that can be used to create standardized notes:

- **Lesson Note**: Template for creating structured lesson notes with sections for summary, key points, questions, and action items.

## Command Line Arguments

- `--source`: Directory to process - can be any level in the hierarchy (required for most scripts)
- `--convert`: Convert HTML and TXT files to Markdown
- `--generate-index`: Generate indexes for the directory structure
- `--all`: Perform both conversion and index generation
- `--folder`: Process all PDFs or videos in a specific OneDrive subfolder
- `-f`, `--single-file`: Process only a single PDF or video file
- `--force`: Overwrite existing notes/files if they exist
- `--dry-run`: Test without making changes
- `--no-share-links`: Skip OneDrive shared links (for PDF script)
- `--no-summary`: Skip OpenAI summary generation (for video script)
- `--retry-failed`: Only retry previously failed files
- `--timeout`: Set custom API request timeout (seconds)
- `--debug`: Enable debug logging

## Requirements

- Python 3.6+
- pyyaml, html2text, requests, msal, openai, python-dotenv, cryptography, urllib3
- Obsidian (for viewing and working with the generated files)

## License

MIT

## Author

Daniel Shue
