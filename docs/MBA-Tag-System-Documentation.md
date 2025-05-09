```markdown
# MBA Obsidian Vault: Nested Tag System Automation

This document outlines the complete nested tagging system for MBA study materials and the automation tools we've created to implement and maintain it.

## Overview

We've created a hierarchical tagging system using Obsidian's nested tags feature with forward slashes as separators. This enables multi-dimensional organization while maintaining a clean, manageable structure.

### Core Benefits
- **Multi-dimensional organization**: Cross-cutting categories that work alongside folder structure
- **Consistent taxonomy**: Standardized naming and hierarchy
- **Enhanced filtering**: Powerful dataview queries for content discovery
- **Flexible workflow**: Status and priority tracking
- **Course management**: Subject-area organization independent of course schedule

## Tag Structure

```
type/           # Content type categorization
  note/         # Different types of notes
    lecture
    case-study
    literature
    meeting
  assignment/   # Assignment categories
    individual
    group
    draft
    final
  project/      # Project statuses
    proposal
    active
    completed
    archived
  resource/     # Resource types
    textbook
    article
    paper
    presentation
    template

mba/            # MBA-specific content
  course/       # Course categories
    finance/    # Finance subcategories
      corporate-finance
      investments
      valuation
    accounting/
      financial
      managerial
      audit
      tax
    marketing/
      digital
      strategy
      consumer-behavior
      analytics
    strategy/
      corporate
      competitive
      global
      innovation
    operations/
      supply-chain
      project-management
      quality-management
      process-design
  skill/        # Skills developed
    quantitative
    qualitative
    presentation
    negotiation
    leadership
  tool/         # Tools learned/used
    excel
    python
    r
    powerbi
    tableau

status/         # Workflow status
  active
  review
  complete
  archived

priority/       # Priority level
  high
  medium
  low
```

## Automation Tools

We've developed several Python scripts to implement and maintain this tagging system:

### 1. Tag Documentation Generator

**Script:** `generate_tag_doc_working.py`

Scans all notes in your vault and creates a comprehensive documentation of your tag hierarchy:
- Extracts tags from both YAML frontmatter and inline tags
- Builds a nested hierarchy visualization
- Identifies orphaned tags
- Shows usage counts

**Usage:**
```bash
python generate_tag_doc_working.py
```

### 2. Tag Restructuring Tool

**Script:** `restructure_tags.py`

Automatically applies appropriate tags to notes based on:
- Document location in folder structure
- Content patterns
- Document type identification

**Usage:**
```bash
# Simulation mode (recommended first)
python restructure_tags.py

# Apply changes
python restructure_tags.py --apply
```

### 3. Test Restructuring

**Script:** `test_restructure_tags.py`

Tests the tag restructuring on a small sample of files:
- Samples files from different folders
- Shows what tags would be applied
- Helps verify before applying to entire vault

**Usage:**
```bash
# Basic usage
python test_restructure_tags.py

# Advanced options
python test_restructure_tags.py --sample-size 5 --specific-types lecture,case-study
```

### 4. Example Tags Addition

**Script:** `add_example_tags.py`

Adds example nested tags to a specific file:
- Helps understand the tag structure in practice
- Useful for testing tag visualization in Obsidian

**Usage:**
```bash
python add_example_tags.py path/to/file.md
```

### 5. Template Generator

**Script:** `generate_obsidian_templates.py`

Creates Obsidian templates with the nested tag structure:
- Different templates for different note types
- Pre-populated with appropriate tags
- Customized content structure for each type

**Usage:**
```bash
python generate_obsidian_templates.py [template_folder_path]
```

### 6. Dataview Query Generator

**Script:** `generate_dataview_queries.py`

Creates a reference note with example Dataview queries:
- Queries for course content organization
- Workflow management
- Content exploration
- Dashboard elements

**Usage:**
```bash
python generate_dataview_queries.py [output_file_path]
```

## Implementation Strategy

1. **Initial Setup**
   - Run template generator to create note templates
   - Run dataview query generator for reference queries

2. **Testing**
   - Add example tags to a few notes using `add_example_tags.py`
   - Test tag restructuring on sample files with `test_restructure_tags.py`

3. **Full Implementation**
   - Run `restructure_tags.py --apply` to update all notes
   - Generate tag documentation with `generate_tag_doc_working.py`

4. **Ongoing Maintenance**
   - Use templates for new notes
   - Periodically run tag restructuring to catch inconsistencies
   - Update tag documentation as your system evolves

## Recommendations for Daily Use

1. **Creating New Notes**
   - Use templates for consistent structure
   - Place notes in appropriate folders
   - Let the tag restructuring tool handle tag consistency

2. **Finding Information**
   - Create dashboard notes with Dataview queries
   - Use the tag pane to navigate by category
   - Create custom queries for specific needs

3. **System Maintenance**
   - Run tag restructuring monthly
   - Update templates as needs evolve
   - Regenerate tag documentation periodically

## Future Enhancements

- Custom property extraction for metadata
- Integration with periodic review systems
- Course-specific dashboards
- Progress tracking for courses and projects
```
