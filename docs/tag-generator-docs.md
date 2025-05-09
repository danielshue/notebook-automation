# Nested Tag Generator Documentation

This tool automatically generates and manages nested tags in your Markdown files based on YAML frontmatter fields.

## Features

- **Tag Generation**: Convert frontmatter fields to nested tags (`#field/value`)
- **Index File Handling**: Automatically clears tags from index files
- **Default Vault Path**: Uses your Obsidian vault path if no directory is specified
- **Dry-Run Mode**: Preview changes without modifying files
- **Summary Reports**: Shows what was changed and where

## Usage Examples:

### Process Your Entire Vault

```bash
python add_nested_tags.py
```

### Try It Without Making Changes

```bash
python add_nested_tags.py --dry-run
```

### See Detailed Information About Changes

```bash
python add_nested_tags.py --verbose
```

### Process a Specific Folder

```bash
python add_nested_tags.py /path/to/folder
```

## Tag Fields

The script automatically processes these fields from frontmatter:
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

## Special Handling

Files with an `index-type` field in their frontmatter will have any tags cleared because index files should not have tags.

## How to Integrate

1. Make sure YAML frontmatter is at the top of your Markdown files
2. Run the script with your preferred options
3. Check the summary to see what was changed
4. Run periodically to keep your tags updated

For more information, see the full README_add_nested_tags.md
