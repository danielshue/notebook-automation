# MBA Notebook Automation Tool

A comprehensive tool for managing MBA course notes in an Obsidian vault. This script automates the process of converting course materials, generating index files, and organizing content for easy navigation and retrieval.

## Features

### File Conversion
- Converts HTML files to Markdown with proper formatting
- Converts transcript TXT files to Markdown
- Cleans up filenames by removing numbering prefixes and improving readability
- Adds YAML frontmatter with template type and auto-generated state properties
- Automatically adds appropriate titles to all generated files

### Index Generation
- Creates a hierarchical structure of index files for easy navigation
- Supports a 4-level hierarchy: Main → Course → Module → Lesson
- Generates Obsidian-compatible wiki-links between index levels
- Creates back-navigation links for seamless browsing
- Respects "readonly" marked files to prevent overwriting customized content

### Content Organization
- Automatically categorizes content into readings, videos, transcripts, etc.
- Adds appropriate icons for different content types
- Implements a tagging system for enhanced content discovery
- Supports structural and cognitive tags

## Directory Structure

The tool expects and generates the following directory structure:
```
- Root (main-index)
  - Course Folders (course-index)
    - Module Folders (module-index)
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
pip install html2text
```

## Usage

The tool provides several command-line options:

- **Convert files only**:
  ```
  python notebook-generator.py --convert --source <path>
  ```

- **Generate indexes only**:
  ```
  python notebook-generator.py --generate-indexes --source <path>
  ```

- **Process single index**:
  ```
  python notebook-generator.py --single-index --source <path>
  ```

- **Perform both conversion and index generation**:
  ```
  python notebook-generator.py --all --source <path>
  ```

## Examples

Convert HTML/TXT files and generate indexes for a specific course:
```
python notebook-generator.py --all --source /path/to/accounting-for-managers
```

Regenerate a single index file after adding new content:
```
python notebook-generator.py --single-index --source /path/to/accounting-for-managers/module-1
```

## Templater Integration

The tool includes Templater templates for Obsidian that can be used to create standardized notes:

- **Lesson Note**: Template for creating structured lesson notes with sections for summary, key points, questions, and action items.

## Command Line Arguments

- `--source`: Directory to process - can be any level in the hierarchy (required)
- `--convert`: Convert HTML and TXT files to Markdown
- `--generate-indexes`: Generate indexes for the directory structure
- `--single-index`: Regenerate only the index for the directory specified by --source
- `--all`: Perform both conversion and index generation

## Requirements

- Python 3.6+
- html2text library
- Obsidian (for viewing and working with the generated files)

## License

MIT

## Author

Daniel Shue
