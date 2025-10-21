# Tag Management Guide

Learn how to maintain consistent tags across your Obsidian vault using the Notebook Automation CLI.

## Overview

The tag management system helps you maintain organized, hierarchical tags in your markdown files. It provides tools to:

- Add nested tags based on directory structure
- Consolidate duplicate tags
- Ensure metadata consistency
- Update frontmatter properties
- Diagnose YAML issues

## Why Tag Management Matters

Well-organized tags enable:
- **Better Navigation**: Find related notes quickly
- **Hierarchy Visualization**: See content relationships
- **Search Efficiency**: Filter by tag categories
- **Obsidian Integration**: Leverage Obsidian's tag features
- **Consistent Organization**: Maintain standards across your vault

## Getting Started

### Basic Tag Command Structure

All tag commands follow this pattern:

```bash
na tag <subcommand> <path> [options]
```

### Available Subcommands

| Command | Purpose |
|---------|---------|
| `add-nested` | Add hierarchical tags based on frontmatter and folder structure |
| `consolidate` | Merge duplicate or similar tags |
| `restructure` | Restructure tags for consistency |
| `clean-index` | Remove tags from index files |
| `add-example` | Add example tags for testing |
| `metadata-check` | Validate metadata consistency |
| `update-frontmatter` | Update specific frontmatter fields |
| `diagnose-yaml` | Find and report YAML syntax issues |

---

## Tag Commands in Detail

### add-nested

Add nested tags based on frontmatter fields and directory hierarchy.

**When to Use:**
- After reorganizing your vault structure
- Setting up a new vault section
- Ensuring tags match folder hierarchy
- Bulk tag updates

**Syntax:**
```bash
na tag add-nested <path> [--override-vault-root <path>]
```

**Examples:**

Add nested tags to entire vault:
```bash
na tag add-nested "vault/" --verbose
```

Add tags to specific project:
```bash
na tag add-nested "vault/projects/MBA"
```

Preview changes:
```bash
na tag add-nested "vault/projects" --dry-run
```

**How It Works:**

1. Scans directory structure
2. Reads frontmatter from each markdown file
3. Generates hierarchical tags like `projects/mba/finance`
4. Updates file frontmatter with nested tags
5. Maintains existing tags while adding hierarchy

**Example Result:**

Before:
```yaml
---
title: "Financial Analysis Notes"
tags: ["finance", "notes"]
---
```

After:
```yaml
---
title: "Financial Analysis Notes"
tags: ["finance", "notes", "projects/mba/finance"]
---
```

---

### consolidate

Consolidate and merge duplicate or similar tags.

**When to Use:**
- Cleaning up inconsistent tags
- Merging variations of same tag
- Standardizing tag names
- After bulk imports

**Syntax:**
```bash
na tag consolidate <path>
```

**Examples:**

Consolidate tags in file:
```bash
na tag consolidate "vault/notes/document.md"
```

Consolidate entire directory:
```bash
na tag consolidate "vault/projects" --verbose
```

**What Gets Consolidated:**

- Duplicate tags: `["tag1", "tag1"]` → `["tag1"]`
- Case variations: `["Finance", "finance"]` → `["finance"]`
- Spacing issues: `["tag-1", "tag 1"]` → `["tag-1"]`

---

### restructure

Restructure tags for consistency across the vault.

**When to Use:**
- Applying new tagging conventions
- Major vault reorganization
- Standardizing tag format
- Fixing historical inconsistencies

**Syntax:**
```bash
na tag restructure <path>
```

**Examples:**

Restructure vault tags:
```bash
na tag restructure "vault/" --verbose
```

Restructure specific section:
```bash
na tag restructure "vault/courses"
```

Preview restructuring:
```bash
na tag restructure "vault/projects" --dry-run
```

---

### metadata-check

Check and enforce metadata consistency in markdown files.

**When to Use:**
- Validating vault integrity
- Finding missing metadata
- Checking tag consistency
- Quality assurance checks

**Syntax:**
```bash
na tag metadata-check <path>
```

**Examples:**

Check entire vault:
```bash
na tag metadata-check "vault/" --verbose
```

Check single file:
```bash
na tag metadata-check "vault/notes/document.md"
```

**What It Checks:**

- ✅ Valid YAML frontmatter syntax
- ✅ Required fields present
- ✅ Tag format consistency
- ✅ Hierarchy alignment with folders
- ✅ No duplicate tags

**Output Example:**
```
Checking: vault/projects/MBA/notes.md
  ✓ Valid YAML
  ✓ Title present
  ✓ Tags consistent
  ⚠ Missing date field
  
Summary: 3 checks passed, 1 warning
```

---

### update-frontmatter

Update or add specific key-value pairs in frontmatter.

**When to Use:**
- Bulk updating metadata fields
- Adding new frontmatter properties
- Correcting metadata values
- Setting status or flags

**Syntax:**
```bash
na tag update-frontmatter <path> <key> <value>
```

**Examples:**

Set status field:
```bash
na tag update-frontmatter "note.md" "status" "reviewed"
```

Add course tag:
```bash
na tag update-frontmatter "document.md" "course" "MBA"
```

Update date:
```bash
na tag update-frontmatter "note.md" "updated" "2025-10-21"
```

**Bulk Updates:**

Use with find/xargs for bulk operations:
```bash
# Unix/Linux/macOS
find vault/projects -name "*.md" -exec na tag update-frontmatter {} "status" "draft" \;

# PowerShell
Get-ChildItem vault/projects -Filter *.md -Recurse | ForEach-Object {
    na tag update-frontmatter $_.FullName "status" "draft"
}
```

---

### diagnose-yaml

Diagnose YAML frontmatter issues in markdown files.

**When to Use:**
- Troubleshooting parsing errors
- Finding malformed YAML
- Validating bulk imports
- Pre-processing checks

**Syntax:**
```bash
na tag diagnose-yaml <path>
```

**Examples:**

Diagnose entire vault:
```bash
na tag diagnose-yaml "vault/" --verbose
```

Check specific file:
```bash
na tag diagnose-yaml "problematic-note.md"
```

**Common Issues Found:**

❌ **Invalid YAML syntax:**
```yaml
---
title: "Test
tags: [unmatched
---
```

❌ **Missing delimiters:**
```yaml
title: "Test"
tags: ["example"]
---
```

❌ **Wrong indentation:**
```yaml
---
title: "Test"
  tags: ["example"]
---
```

**Output Example:**
```
Diagnosing: vault/notes/document.md
  ❌ YAML Error: Unexpected end of stream
  Line 3: Missing closing quote
  
Fix: Add closing quote to title field
```

---

### clean-index

Remove tags from index files to keep them clean.

**When to Use:**
- Index files don't need tags
- Cleaning up auto-generated indexes
- Maintaining clean navigation files

**Syntax:**
```bash
na tag clean-index <path>
```

**Examples:**

Clean index files in directory:
```bash
na tag clean-index "vault/projects"
```

Clean all indexes in vault:
```bash
na tag clean-index "vault/" --verbose
```

---

### add-example

Add example tags to a file for demonstration or testing.

**When to Use:**
- Creating templates
- Testing tag functionality
- Demonstrating tag structure
- Training purposes

**Syntax:**
```bash
na tag add-example <path>
```

**Examples:**

Add examples to template:
```bash
na tag add-example "vault/templates/note-template.md"
```

---

## Common Workflows

### Initial Vault Setup

Set up tags for a new vault or section:

```bash
# 1. Check current state
na tag metadata-check "vault/" --verbose

# 2. Add hierarchical tags
na tag add-nested "vault/"

# 3. Consolidate any duplicates
na tag consolidate "vault/"

# 4. Verify results
na tag metadata-check "vault/"
```

### After Vault Reorganization

Update tags after moving files:

```bash
# 1. Preview changes
na tag add-nested "vault/reorganized-section" --dry-run

# 2. Apply changes
na tag add-nested "vault/reorganized-section" --verbose

# 3. Consolidate
na tag consolidate "vault/reorganized-section"

# 4. Check consistency
na tag metadata-check "vault/reorganized-section"
```

### Clean Up Existing Vault

Clean and standardize tags in existing vault:

```bash
# 1. Diagnose issues
na tag diagnose-yaml "vault/" --verbose

# 2. Consolidate duplicates
na tag consolidate "vault/"

# 3. Restructure for consistency
na tag restructure "vault/"

# 4. Add hierarchical structure
na tag add-nested "vault/"

# 5. Final check
na tag metadata-check "vault/"
```

### Bulk Metadata Updates

Update metadata across multiple files:

```bash
# Set status on all project files
na tag update-frontmatter "vault/projects/**/*.md" "status" "active"

# Add review date
na tag update-frontmatter "vault/notes/**/*.md" "reviewed" "2025-10-21"
```

---

## Tag Naming Conventions

### Best Practices

**Use hierarchical tags:**
```yaml
tags: ["courses/mba/finance", "courses/mba/operations"]
```

**Use kebab-case or underscores:**
```yaml
tags: ["machine-learning", "data_science"]
```

**Be consistent:**
```yaml
# Good - consistent
tags: ["project-a", "project-b", "project-c"]

# Bad - inconsistent
tags: ["Project-A", "projectB", "project_c"]
```

**Avoid special characters:**
```yaml
# Good
tags: ["course-notes"]

# Bad
tags: ["course@notes!", "course#1"]
```

### Recommended Tag Hierarchy

```
vault/
├── courses/
│   ├── mba/
│   │   ├── finance/
│   │   └── operations/
│   └── certifications/
├── projects/
│   ├── work/
│   └── personal/
└── resources/
    ├── books/
    └── articles/
```

Corresponding tags:
```yaml
tags: 
  - "courses/mba/finance"
  - "projects/work"
  - "resources/books"
```

---

## Integration with Obsidian

### Tag Pane

Tags added by the CLI appear in Obsidian's tag pane:

1. Open Obsidian
2. Open tag pane (View → Show tag pane)
3. See hierarchical tags with expandable structure
4. Click tags to filter notes

### Tag Search

Use Obsidian search with CLI-generated tags:

```
tag:#courses/mba/finance
tag:#projects AND tag:#active
```

### Graph View

Hierarchical tags create better graph visualizations:
- Related notes connect through shared tag hierarchy
- Tag-based grouping shows content relationships
- Color-coding by tag category

---

## Troubleshooting

### Tags Not Appearing

**Problem:** Tags added but not visible in Obsidian

**Solutions:**
1. Reload vault in Obsidian (Ctrl+R / Cmd+R)
2. Check YAML syntax with diagnose-yaml
3. Verify tag format (no special characters)
4. Check file isn't excluded in Obsidian settings

### Duplicate Tags After Processing

**Problem:** Same tags appear multiple times

**Solution:**
```bash
na tag consolidate "vault/" --verbose
```

### YAML Parsing Errors

**Problem:** Frontmatter won't parse

**Solution:**
```bash
# Diagnose issues
na tag diagnose-yaml "problematic-file.md"

# Common fixes:
# - Add missing closing quotes
# - Fix indentation (2 spaces)
# - Ensure proper delimiters (---)
```

### Tags Don't Match Hierarchy

**Problem:** Tags don't reflect folder structure

**Solution:**
```bash
# Regenerate nested tags
na tag add-nested "vault/" --verbose

# Check results
na tag metadata-check "vault/"
```

### Performance Issues with Large Vaults

**Problem:** Slow processing on large vaults

**Solutions:**
1. Process sections separately:
   ```bash
   na tag add-nested "vault/section1"
   na tag add-nested "vault/section2"
   ```

2. Use dry-run first:
   ```bash
   na tag add-nested "vault/" --dry-run
   ```

3. Process specific file types:
   ```bash
   find vault -name "*.md" -type f | xargs -I {} na tag consolidate {}
   ```

---

## Advanced Usage

### Custom Tag Hierarchies

Create custom hierarchies beyond folder structure:

```bash
# Add tags based on frontmatter
na tag add-nested "vault/" --override-vault-root "custom-root"
```

### Automated Tag Maintenance

Schedule regular tag maintenance:

**PowerShell (Windows):**
```powershell
# weekly-tag-maintenance.ps1
na tag diagnose-yaml "vault/" --verbose | Out-File "logs/tag-check.log"
na tag consolidate "vault/"
na tag metadata-check "vault/" --verbose
```

**Bash (Linux/macOS):**
```bash
#!/bin/bash
# weekly-tag-maintenance.sh
na tag diagnose-yaml "vault/" --verbose > logs/tag-check.log
na tag consolidate "vault/"
na tag metadata-check "vault/" --verbose
```

### Integration with Git Hooks

Validate tags before committing:

```bash
# .git/hooks/pre-commit
#!/bin/bash
na tag diagnose-yaml "vault/" --verbose
if [ $? -ne 0 ]; then
    echo "YAML errors found. Fix before committing."
    exit 1
fi
```

---

## Related Commands

- [`vault generate-index`](../cli-reference.md#vault-generate-index) - Generate indexes with consistent tags
- [`vault ensure-metadata`](../cli-reference.md#vault-ensure-metadata) - Ensure metadata matches hierarchy
- [`config view`](../cli-reference.md#config-view) - View current configuration

---

## See Also

- [CLI Reference](../cli-reference.md) - Complete command reference
- [Vault Synchronization](vault-synchronization.md) - OneDrive integration
- [Basic Commands](../getting-started/basic-commands.md) - Getting started guide

---

*Last updated: 2025-10-21*
