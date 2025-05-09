# Tag Management Scripts

This directory contains scripts for managing tags in Obsidian markdown files. These scripts help organize, add, clean, consolidate, and document tags in your Obsidian vault.

## Main Scripts

- **add_nested_tags.py** - Primary script to add nested tags based on YAML frontmatter fields
- **clean_index_tags.py** - Script to clean tags from index files
- **consolidate_tags.py** - Script to consolidate duplicate or similar tags
- **generate_tag_doc.py** - Generate documentation of tag usage and hierarchy
- **restructure_tags.py** - Restructure tags into a better hierarchy
- **tag_manager.py** - Core tag management utilities

## Demo and Examples

- **demo_add_nested_tags.py** - Demonstration of the add_nested_tags.py script
- **add_example_tags.py** - Script to add example tags to files

## Development Versions

- **add_nested_tags_fixed.py** - Fixed version of add_nested_tags.py
- **add_nested_tags_updated.py** - Updated version of add_nested_tags.py

## Usage

Most scripts follow a similar pattern for command-line usage:

```bash
python tags/add_nested_tags.py /path/to/folder --verbose
python tags/generate_tag_doc.py /path/to/vault
```

See the documentation in the `docs` directory for detailed usage instructions for each script.
