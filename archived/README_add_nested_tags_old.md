# Markdown Nested Tag Generator

This script processes markdown files with YAML frontmatter to extract fields and convert them into nested tags.

## Features

- Recursively scans a folder for `.md` files
- Parses YAML frontmatter in each file
- Extracts fields like `type`, `course`, `program`, `term`, `status`, etc.
- Generates nested tags in the format `#field/value`
- Updates the `tags:` field in YAML frontmatter:
  - If `tags:` doesn't exist → creates it
  - If `tags:` exists → appends new tags, removes duplicates
- Leaves all other metadata untouched
- Supports `--dry-run` to preview changes without modifying files
- Optional verbose mode to print a summary of changes per file

## Requirements

- Python 3.6+
- Recommended: `ruamel.yaml` library for better YAML formatting preservation

```bash
pip install ruamel.yaml
```

## Usage

Basic usage:

```bash
python add_nested_tags.py /path/to/markdown/folder
```

Use default Obsidian vault path from config.json:

```bash
python add_nested_tags.py
```

Preview changes without modifying files:

```bash
python add_nested_tags.py /path/to/markdown/folder --dry-run
```

Get detailed information about changes:

```bash
python add_nested_tags.py /path/to/markdown/folder --verbose
```

Combine options:

```bash
python add_nested_tags.py /path/to/markdown/folder --dry-run --verbose
```

Run against Obsidian vault with verbose output:

```bash
python add_nested_tags.py --verbose
```

## How It Works

1. The script scans all `.md` files in the specified directory (and subdirectories)
2. For each file with YAML frontmatter (between `---` delimiters), it:
   - Checks if the file has an `index-type` field - if so, it clears any tags since index files should not have tags
   - Otherwise, extracts fields like `type`, `course`, `program`, `term`, `status`, etc.
   - Converts these fields into nested tags (e.g., `type: essay` becomes `#type/essay`)
   - Updates the `tags:` field in the frontmatter, preserving existing tags
3. The script tracks statistics and can provide a summary of changes, including how many index files had their tags cleared

## Supported Fields

The script currently extracts and converts the following fields to nested tags:
- type
- course
- program
- term
- status
- category
- subject
- priority
- project
- area
- resource

You can modify the `tag_fields` list in the script to customize which fields are converted to tags.

## Example

Given a markdown file with this frontmatter:

```yaml
---
title: Finance Midterm Notes
type: lecture notes
course: FIN571
program: MBA
term: Spring 2024
status: in progress
tags: finance, notes
---
```

After running the script, the frontmatter will be updated to:

```yaml
---
title: Finance Midterm Notes
type: lecture notes
course: FIN571
program: MBA
term: Spring 2024
status: in progress
tags: finance, notes, #course/FIN571, #program/MBA, #status/in progress, #term/Spring 2024, #type/lecture notes
---
```
