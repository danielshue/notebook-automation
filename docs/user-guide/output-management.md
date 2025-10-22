# Output Management

Learn how to control, organize, and manage the output from Notebook Automation processing operations.

## Overview

Notebook Automation generates markdown files with structured content, metadata, and optional AI-generated summaries. Understanding how to manage these outputs is essential for maintaining an organized knowledge base.

## Output Location and Structure

### Default Output Locations

**Single File Processing:**
```bash
# Output is placed in the same directory as the source file
na video-notes -p "documents/lecture.mp4"
# Creates: documents/lecture-video.md
```

**Directory Processing:**
```bash
# Output mirrors the input directory structure
na pdf-notes -p "courses/readings"
# Creates markdown files alongside PDFs
```

### Custom Output Directory

**Override Output Location:**
```bash
na video-notes -p "input/video.mp4" --overwrite-output-dir "output"
# Creates: output/video-video.md
```

**Batch with Custom Output:**
```bash
na pdf-notes -p "input-docs" --overwrite-output-dir "processed-notes"
# All processed files go to processed-notes/
```

### Vault Root Configuration

**Specify Vault Root:**
```bash
na video-notes -p "content.mp4" --override-vault-root "C:\MyVault"
# Output path is calculated relative to vault root
```

**Use in Configuration:**
```json
{
  "Paths": {
    "NotebookVaultFullpathRoot": "C:\\MyVault"
  }
}
```

## Output File Naming

### Automatic Suffix Application

Notebook Automation automatically applies content-type suffixes to output files:

**Video Files:**
```
input: lecture-intro.mp4
output: lecture-intro-video.md
```

**PDF Files:**
```
input: textbook-chapter-01.pdf
output: textbook-chapter-01-pdf.md
```

**HTML Files:**
```
input: course-syllabus.html
output: course-syllabus-html.md
```

### Naming Convention Benefits

**Clear Content Type Identification:**
- Easy to filter by content type in Obsidian
- Prevents filename conflicts between different content types
- Supports consistent organization

**Example Directory:**
```
vault/course-101/
├── lecture-01-video.md
├── lecture-01-slides-pdf.md
├── lecture-01-transcript-pdf.md
└── lecture-01-notes-html.md
```

## Output Content Structure

### Standard Markdown Output

**All outputs include:**
1. YAML frontmatter with metadata
2. Title and source information
3. AI-generated summary (unless `--no-summary`)
4. Main content
5. Metadata section

**Example Output:**
```markdown
---
title: "Introduction to Operations Management"
source: "lecture-01.mp4"
type: "video-note"
processed_date: "2025-01-18"
duration: "45:30"
tags:
  - "course/mba/operations"
  - "type/lecture"
---

# Introduction to Operations Management

## Summary

[AI-generated summary of the lecture content]

## Transcript

[Full transcript with timestamps]

## Metadata

- **Duration**: 45 minutes 30 seconds
- **Processed**: 2025-01-18
```

### PDF-Specific Output

**Additional Features:**
- Extracted annotations
- Page references
- Extracted images (if `--extract-images` used)
- Table of contents

**Image Extraction:**
```bash
na pdf-notes -p "document.pdf" --extract-images
```

Creates:
```
document-pdf.md
document-images/
├── image-001.png
├── image-002.png
└── image-003.png
```

### Video-Specific Output

**Additional Features:**
- Full transcript with timestamps
- Speaker identification (when available)
- Chapter markers
- Key topics with time references

**Example Transcript Section:**
```markdown
## Transcript

[00:00:15] Today we're going to discuss operations management fundamentals...
[00:02:30] The first key concept is process optimization...
[00:05:45] Let's look at an example of supply chain management...
```

## Managing Output Files

### Preventing Overwrites

**Default Behavior:**
```bash
# Existing files are NOT overwritten by default
na pdf-notes -p "document.pdf"
# If document-pdf.md exists, it's skipped (unless it lacks AI content)
```

**Force Overwrite:**
```bash
# Overwrite existing files
na pdf-notes -p "document.pdf" --force
```

**Intelligent Skip Logic:**
- Files without AI-generated content are automatically reprocessed
- Files with AI summaries are skipped unless `--force` is used
- Allows re-running base markdown generation without losing AI content

### Organizing Output

**By Course:**
```
vault/
├── MBA-101/
│   ├── lectures/
│   │   ├── lecture-01-video.md
│   │   └── lecture-02-video.md
│   └── readings/
│       ├── chapter-01-pdf.md
│       └── chapter-02-pdf.md
```

**By Content Type:**
```
vault/
├── videos/
│   ├── lecture-01-video.md
│   └── lecture-02-video.md
├── pdfs/
│   ├── reading-01-pdf.md
│   └── reading-02-pdf.md
```

**By Date:**
```
vault/
├── 2025-01/
│   ├── week-1/
│   │   ├── lecture-video.md
│   │   └── reading-pdf.md
│   └── week-2/
```

### Index Files

**Generate Indexes:**
```bash
# Create index files for directory navigation
na vault generate-index "vault/course-101" --recursive
```

**Index Output:**
```markdown
# Course 101 Index

## Lectures
- [[lecture-01-video]]
- [[lecture-02-video]]

## Readings
- [[chapter-01-pdf]]
- [[chapter-02-pdf]]
```

## Output Quality Control

### Validating Output

**Check Generated Files:**
1. Verify frontmatter is complete
2. Review AI-generated summaries for accuracy
3. Ensure content is properly formatted
4. Check cross-links are valid

**Metadata Validation:**
```bash
# Check metadata consistency
na tag metadata-check "vault/course-101" --verbose
```

### Cleaning Up Output

**Remove Failed Outputs:**
```bash
# Use retry-failed to reprocess only failures
na pdf-notes -p "documents" --retry-failed
```

**Consolidate Tags:**
```bash
# Fix duplicate or inconsistent tags
na tag consolidate "vault/course-101"
```

### Enhancing Output

**Add Tags:**
```bash
# Apply hierarchical tags based on folder structure
na tag add-nested "vault/course-101"
```

**Update Metadata:**
```bash
# Update specific frontmatter fields
na tag update-frontmatter "vault/notes.md" "course" "MBA-101"
```

## Backup and Version Control

### Backup Strategies

**Before Major Operations:**
```bash
# Create a backup before batch processing
cp -r vault vault-backup-2025-01-18
```

**Regular Backups:**
- Daily backups of vault directory
- Version control with Git
- Cloud sync with OneDrive

### Git Integration

**Track Changes:**
```bash
# Initialize git repository
git init

# Track all markdown files
git add *.md

# Commit processed outputs
git commit -m "Add processed lecture notes"
```

**Benefits:**
- Version history of all notes
- Easy rollback if needed
- Collaboration support
- Change tracking

## Output Formats and Export

### Markdown Output

**Standard Format:**
- Compatible with Obsidian
- Can be used in other markdown editors
- Easy to version control
- Plain text for longevity

### Export Options

**From Obsidian:**
- Export to PDF
- Export to HTML
- Publish as website
- Share as documents

**Conversion Tools:**
```bash
# Use pandoc for format conversion (external tool)
pandoc notes.md -o notes.pdf
pandoc notes.md -o notes.docx
```

## Output Metadata Management

### Frontmatter Fields

**Standard Fields:**
```yaml
---
title: "Document Title"
source: "original-file.ext"
type: "video-note|pdf-note|markdown-note"
processed_date: "2025-01-18"
tags: ["tag1", "tag2"]
---
```

**Custom Fields:**
```yaml
---
# Standard fields
title: "Lecture 1"
# Custom fields
course: "MBA-101"
instructor: "Dr. Smith"
semester: "Spring 2025"
priority: "high"
---
```

### Bulk Metadata Updates

**Update Multiple Files:**
```bash
# Update frontmatter field across multiple files
for file in vault/course-101/*.md; do
  na tag update-frontmatter "$file" "course" "MBA-101"
done
```

**Ensure Metadata Consistency:**
```bash
# Add missing metadata fields
na vault ensure-metadata "vault/course-101"
```

## Advanced Output Management

### Template Customization

**Location:**
Templates are in the `templates/` directory within the application.

**Customizable Elements:**
- Frontmatter structure
- Content sections
- Formatting styles
- Metadata fields

### Output Post-Processing

**Custom Scripts:**
```bash
# Example: Add custom header to all generated files
for file in output/*.md; do
  echo "<!-- Processed with Notebook Automation -->" | cat - "$file" > temp && mv temp "$file"
done
```

### Integration with Other Tools

**Obsidian Plugins:**
- Dataview: Query output metadata
- Templater: Further customize outputs
- Tag Wrangler: Manage output tags
- Excalidraw: Add diagrams to outputs

## Troubleshooting Output Issues

### Common Problems

**Problem:** Output files have incorrect encoding
**Solution:** Check source file encoding; ensure UTF-8 is used

**Problem:** Images not extracted from PDFs
**Solution:** Use `--extract-images` flag; verify PDF has extractable images

**Problem:** AI summaries are missing
**Solution:** Check AI service configuration; ensure API keys are valid

**Problem:** Frontmatter is malformed
**Solution:** Validate YAML syntax; check for special characters

### Output Validation

**Check Output Quality:**
```bash
# Use debug mode to see detailed processing information
na pdf-notes -p "document.pdf" --debug

# Use dry-run to preview without creating files
na pdf-notes -p "documents" --dry-run
```

**Review Logs:**
- Check application logs for errors
- Review processing warnings
- Monitor API call results

## Best Practices

1. **Use consistent output directory structure** for easy organization
2. **Generate indexes regularly** to maintain navigation
3. **Backup before major batch operations** to prevent data loss
4. **Validate AI-generated content** for accuracy
5. **Use tags consistently** for easy filtering and searching
6. **Keep source files separate** from processed outputs
7. **Document your organization scheme** for future reference
8. **Review and curate** generated content regularly

## Related Documentation

- [File Processing](file-processing.md) - Detailed processing options
- [Batch Operations](batch-operations.md) - Bulk processing strategies
- [Tag Management](tag-management.md) - Organize outputs with tags
- [Vault Synchronization](vault-synchronization.md) - Sync and structure

## Example Output Management Workflows

### Academic Course Management

```bash
# 1. Process course materials
na video-notes -p "course-videos" --overwrite-output-dir "vault/MBA-101/lectures"
na pdf-notes -p "course-pdfs" --overwrite-output-dir "vault/MBA-101/readings"

# 2. Apply organizational tags
na tag add-nested "vault/MBA-101"

# 3. Generate course index
na vault generate-index "vault/MBA-101" --recursive

# 4. Validate metadata
na tag metadata-check "vault/MBA-101" --verbose
```

### Research Paper Library

```bash
# 1. Process papers
na pdf-notes -p "research-papers" --extract-images --overwrite-output-dir "vault/research"

# 2. Ensure metadata consistency
na vault ensure-metadata "vault/research"

# 3. Generate research index
na vault generate-index "vault/research"
```
