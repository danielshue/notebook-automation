---
auto-generated-state: writable
date-created: 2025-06-06
publisher: University of Illinois at Urbana-Champaign
tags: ''
---

# MBA Notebook Tag Consolidator

This script helps simplify your MBA notebook tagging system by consolidating multiple nested tags into a single, most specific tag for each note.

## Problem

Having too many tags per note makes navigation difficult and clutters tag searches. Even with nested tags, having multiple nested tags on each note can create redundancy and make it hard to find specific content.

## Solution

This script analyzes your current tag structure and:

1. Keeps only the most specific, relevant tag for each note
2. Optionally preserves structural tags (index/, structure/)
3. Updates all Markdown files in your vault in a single operation

## How it works

The script uses a priority system to determine which tag is most specific and relevant:
- More nested levels of tags are considered more specific (e.g., `mba/course/finance/investments` is more specific than `mba/course/finance`)
- The script has built-in priorities for different tag categories (e.g., subject-matter tags are prioritized over organizational tags)

## Usage

```bash
python consolidate_tags.py --vault /path/to/vault
```

### Options:

- `--vault` - Path to your Obsidian vault
- `--dry-run` - Preview changes without modifying files
- `--debug` - Show verbose debugging information
- `--keep-structural` - Preserve structural tags (index/, structure/) in addition to the main content tag
- `--priority` - Choose which tag category to prioritize when selecting the tag to keep:
  - `course` (default) - Prioritize subject-matter tags like `mba/course/finance/investments`
  - `skill` - Prioritize skill-based tags like `mba/skill/leadership`
  - `type` - Prioritize content-type tags like `type/note/case-study`
  - `tool` - Prioritize tool-related tags like `mba/tool/excel`

### Examples:

```bash
# Preview changes prioritizing course tags (default)
python tags/consolidate_tags.py --dry-run

# Apply changes prioritizing skill tags 
python tags/consolidate_tags.py --priority skill

# Preview changes prioritizing tool tags and keeping structural tags
python tags/consolidate_tags.py --dry-run --priority tool --keep-structural
```

## Combined with clean_index_tags.py

This script complements your existing `tags/clean_index_tags.py` script:
- `tags/clean_index_tags.py` removes tags from index pages
- `tags/consolidate_tags.py` consolidates multiple tags to a single tag on content pages

Used together, these scripts will significantly clean up your tagging system while maintaining the ability to find content through your hierarchical tag structure.
