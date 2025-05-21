# Markdown YAML Frontmatter Tag Processor with Quoted Tags

This script recursively scans a folder for markdown (.md) files, extracts fields from the YAML frontmatter, and converts them to nested tags in the format `#field/value`. These tags are then added to the existing tags field or a new tags field is created if it doesn't exist. All tags are properly enclosed in double quotes in the YAML frontmatter.

## Features

- Extracts fields from YAML frontmatter and generates nested tags
- Ensures all tags in the frontmatter are properly enclosed in double quotes
- Special handling for index files to clear their tags
- Support for dry-run mode to preview changes without writing to files
- Automatically uses the Obsidian vault path if no directory is provided
- Detailed logging and statistics of processed files

## Fields Processed

The script extracts the following fields from the YAML frontmatter:
- `type`
- `course`
- `program`
- `term`
- `status`
- `category`
- `subject`
- `priority`
- `project`
- `area`
- `resource`

## Installation

The script requires Python 3.6 or later. It uses the `ruamel.yaml` library for better YAML formatting preservation, but falls back to the standard `pyyaml` library if `ruamel.yaml` is not available.

To install the required dependencies:

```bash
pip install ruamel.yaml
```

## Usage

```bash
# Run on a specific directory
python tags/add_nested_tags.py /path/to/folder

# Run on a specific directory with verbose output
python tags/add_nested_tags.py /path/to/folder --verbose

# Perform a dry run (no changes will be written to files)
python tags/add_nested_tags.py /path/to/folder --dry-run

# Use the default Obsidian vault path as configured in tools.utils.config
python tags/add_nested_tags.py --verbose
```

## Example

If a markdown file has the following YAML frontmatter:

```yaml
---
title: My Document
type: note
course: MBA101
---
```

After running the script, the frontmatter will be updated to:

```yaml
---
title: My Document
type: note
course: MBA101
tags:
  - "#type/note"
  - "#course/MBA101"
---
```

Note that each tag in the `tags` list is properly enclosed in double quotes.

## Special Handling for Index Files

If a file has `index-type` in its frontmatter, the script will clear any existing tags as index files should not have tags.

## Dependencies

- Python 3.6+
- `ruamel.yaml` (recommended) or `pyyaml`
- Local `tools` package for configuration and logging

## License

This project is licensed under the terms of the MIT license.
