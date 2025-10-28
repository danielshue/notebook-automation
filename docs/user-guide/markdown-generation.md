# Markdown Generation Guide

Learn how to convert HTML, TXT, and EPUB files to markdown using the Notebook Automation CLI.

## Overview

The markdown generation system enables you to convert various document formats into well-structured markdown files suitable for your Obsidian vault. It supports:

- **HTML Files**: Web articles, exported web pages, HTML documents
- **TXT Files**: Plain text documents with intelligent formatting
- **EPUB Files**: eBooks and digital publications
- **Frontmatter Extraction**: Pull metadata from existing markdown files
- **OneDrive Integration**: Generate share links for converted content

## Why Markdown Generation Matters

Converting content to markdown provides:
- ✅ Unified format across all your notes
- ✅ Better integration with Obsidian features
- ✅ Improved searchability and linking
- ✅ Consistent structure and metadata
- ✅ Preservation of content with local storage

---

## Core Concepts

### Supported Formats

**HTML (.html, .htm)**
- Web articles and saved pages
- Exported documentation
- HTML-formatted notes
- Preserves structure and formatting

**TXT (.txt)**
- Plain text documents
- ASCII/UTF-8 encoded files
- Simple notes and lists
- Converts to structured markdown

**EPUB (.epub)**
- Digital books
- Course materials
- Technical documentation
- Preserves chapters and structure

### Output Structure

Generated markdown files include:
- **Frontmatter**: Metadata with title, source, type, processing date
- **Formatted Content**: Properly structured markdown
- **Preserved Links**: Internal and external references
- **Image References**: Links to embedded images
- **OneDrive Links**: Share links when applicable

---

## Getting Started

### Basic Command Structure

```bash
na generate-markdown -p <path> [options]
```

### Required Options

- `-p, --path <path>` - Path to file or directory containing source files

### Optional Flags

- `--override-vault-root <path>` - Override default vault root from config
- `--extract-from-markdown` - Extract HTML from OneDrive paths in markdown frontmatter
- `--no-share-links` - Disable OneDrive share link generation
- `--verbose, -v` - Show detailed processing information
- `--dry-run` - Preview operations without making changes

---

## Common Use Cases

### Convert Single HTML File

Convert a web article to markdown:

```bash
na generate-markdown -p "articles/web-article.html"
```

**Result:**
- Creates `web-article.md` with formatted content
- Adds frontmatter with metadata
- Preserves links and structure

### Convert EPUB Book

Convert an eBook to markdown:

```bash
na generate-markdown -p "books/course-textbook.epub"
```

**Result:**
- Creates markdown file for the book
- Preserves chapter structure
- Extracts metadata (title, author)
- Maintains table of contents

### Batch Convert Directory

Convert all supported files in a directory:

```bash
na generate-markdown -p "documents/to-convert/" --verbose
```

**Result:**
- Processes all HTML, TXT, and EPUB files
- Creates markdown for each file
- Shows progress for each conversion
- Skips already converted files (unless forced)

### Extract from Markdown Frontmatter

Extract HTML content referenced in existing markdown files:

```bash
na generate-markdown -p "vault/notes/" --extract-from-markdown
```

**Use Case:**
When you have markdown files with OneDrive HTML paths in frontmatter:

```yaml
---
title: "Course Overview"
onedrive_relative_path: "Courses/MBA/overview.html"
---
```

This command extracts the HTML from OneDrive and converts it to markdown content.

---

## Detailed Examples

### Example 1: Convert Web Articles for Research

**Scenario:** You've saved several web articles as HTML and want them in your research vault.

```bash
# Convert single article
na generate-markdown -p "research/articles/article-1.html"

# Convert entire articles folder
na generate-markdown -p "research/articles/" --verbose

# Preview what would be converted
na generate-markdown -p "research/articles/" --dry-run
```

**Tips:**
- Use `--verbose` to see each file being processed
- Check output in your vault after conversion
- Review frontmatter for accuracy

### Example 2: Import Course eBooks

**Scenario:** You have course textbooks in EPUB format.

```bash
# Convert single textbook
na generate-markdown -p "courses/textbooks/economics-101.epub"

# Convert all course textbooks
na generate-markdown -p "courses/textbooks/" --verbose
```

**Tips:**
- EPUB files may be large; conversion can take time
- Review chapter structure in generated markdown
- Large books may benefit from manual organization

### Example 3: Convert Plain Text Notes

**Scenario:** You have plain text notes to import into Obsidian.

```bash
# Convert text file with custom vault root
na generate-markdown -p "notes/lecture-notes.txt" \
  --override-vault-root "C:\MyVault"

# Batch convert text files
na generate-markdown -p "notes/" --verbose
```

### Example 4: OneDrive Integration

**Scenario:** Extract HTML from OneDrive paths stored in markdown frontmatter.

```bash
# Extract HTML from markdown files
na generate-markdown -p "vault/courses/" \
  --extract-from-markdown \
  --verbose
```

**When to Use:**
- You have document placeholders referencing HTML in OneDrive
- Want to bring cloud HTML content into local markdown
- Need to process referenced content in bulk

---

## Workflows

### Workflow 1: Research Article Collection

**Goal:** Build a research library from web articles.

**Steps:**

1. **Save articles as HTML** from your browser

2. **Organize in a folder:**
   ```
   research/
   └── articles/
       ├── article-1.html
       ├── article-2.html
       └── article-3.html
   ```

3. **Convert to markdown:**
   ```bash
   na generate-markdown -p "research/articles/" --verbose
   ```

4. **Review and organize:**
   - Check generated markdown in vault
   - Add additional tags and metadata
   - Create index files for navigation

### Workflow 2: eBook Course Material Import

**Goal:** Import course textbooks into Obsidian vault.

**Steps:**

1. **Collect EPUB files:**
   ```
   courses/
   └── textbooks/
       ├── textbook-chapter-1.epub
       └── textbook-chapter-2.epub
   ```

2. **Convert with custom settings:**
   ```bash
   na generate-markdown -p "courses/textbooks/" \
     --verbose \
     --override-vault-root "D:\CourseVault"
   ```

3. **Post-process:**
   - Review chapter structure
   - Add course-specific tags
   - Link to related notes

### Workflow 3: Legacy Content Migration

**Goal:** Migrate old TXT/HTML notes into current vault.

**Steps:**

1. **Preview conversion:**
   ```bash
   na generate-markdown -p "legacy-notes/" --dry-run --verbose
   ```

2. **Perform conversion:**
   ```bash
   na generate-markdown -p "legacy-notes/" --verbose
   ```

3. **Clean up:**
   - Review all generated files
   - Update metadata as needed
   - Remove source files after verification

---

## Integration with Other Commands

### With vault Commands

Generate markdown, then organize vault:

```bash
# Convert content
na generate-markdown -p "new-content/"

# Generate vault indexes
na vault generate-index "vault/"

# Ensure metadata consistency
na vault ensure-metadata "vault/"
```

### With tag Commands

Generate markdown, then manage tags:

```bash
# Convert content
na generate-markdown -p "articles/"

# Add hierarchical tags
na tag add-nested "vault/"

# Consolidate tags
na tag consolidate "vault/"
```

### With Processing Commands

Convert first, then process with AI:

```bash
# Convert HTML course material
na generate-markdown -p "courses/materials/"

# Process PDFs in same directory
na pdf-notes -p "courses/materials/" --verbose
```

---

## Output Format

### Generated Markdown Structure

**Example output for HTML article:**

```markdown
---
title: "Article Title from HTML"
source: "original-article.html"
type: "generated-markdown"
source_format: "html"
processed_date: "2025-10-28"
onedrive_share_link: "https://onedrive.live.com/..."
---

# Article Title from HTML

[Article content converted to markdown format...]

## Metadata

- **Source**: original-article.html
- **Format**: HTML
- **Processed**: 2025-10-28
```

**Example output for EPUB:**

```markdown
---
title: "Book Title"
author: "Book Author"
source: "original-book.epub"
type: "generated-markdown"
source_format: "epub"
processed_date: "2025-10-28"
---

# Book Title

## Chapter 1

[Chapter content...]

## Chapter 2

[Chapter content...]
```

---

## Troubleshooting

### Common Issues

**Problem: "File not found" error**

**Solution:**
- Verify file path is correct
- Use absolute paths or paths relative to working directory
- Check file exists and is readable

**Problem: Malformed HTML not converting properly**

**Solution:**
- Review source HTML for validity
- Try opening in browser first
- Consider manual cleanup before conversion

**Problem: EPUB conversion incomplete**

**Solution:**
- Check EPUB file isn't corrupted
- Large EPUBs may take longer
- Review logs with `--verbose` flag

**Problem: Generated markdown missing content**

**Solution:**
- Check source file encoding (should be UTF-8)
- Review source file for unusual formatting
- Try `--verbose` to see processing details

### Performance Considerations

**Large files:**
- EPUB conversion can be memory-intensive
- Consider splitting large files before conversion
- Monitor system resources during batch operations

**Batch operations:**
- Use `--dry-run` first to estimate scope
- Process in smaller batches if needed
- Use `--verbose` to track progress

---

## Best Practices

### File Organization

**Before conversion:**
```
source/
├── articles/     # HTML files to convert
├── books/        # EPUB files to convert
└── notes/        # TXT files to convert
```

**After conversion:**
```
vault/
├── articles/     # Converted markdown
├── books/        # Converted markdown
└── notes/        # Converted markdown
```

### Metadata Management

- Review frontmatter after conversion
- Add custom tags for organization
- Update titles if auto-extracted ones aren't ideal
- Add relevant dates and context

### Quality Control

**Always:**
1. Preview with `--dry-run` first
2. Use `--verbose` for important conversions
3. Review generated markdown for accuracy
4. Check links and references work
5. Verify images are accessible

### Vault Organization

**Recommended structure:**
- Keep converted files in dedicated folders
- Use consistent naming conventions
- Generate indexes after bulk conversions
- Maintain source files until verification complete

---

## Advanced Usage

### Custom Vault Roots

Override default vault location:

```bash
na generate-markdown -p "content/" \
  --override-vault-root "D:\AlternateVault"
```

### Disable Share Links

Skip OneDrive share link generation:

```bash
na generate-markdown -p "content/" --no-share-links
```

**When to use:**
- Processing files not in OneDrive
- Don't need sharing capability
- Faster processing needed

### Extract from Frontmatter

Process files referenced in markdown frontmatter:

```bash
na generate-markdown -p "vault/" \
  --extract-from-markdown \
  --verbose
```

**Looks for:**
```yaml
onedrive_relative_path: "path/to/file.html"
```

---

## Related Commands

- **[vault vault-sync](vault-synchronization.md)** - Sync vault with OneDrive
- **[pdf-notes](../getting-started/basic-commands.md#pdf-processing)** - Process PDF files
- **[video-notes](../getting-started/basic-commands.md#video-processing)** - Process video files
- **[tag commands](tag-management.md)** - Manage tags in generated files

---

## Additional Resources

- **[CLI Reference](../cli-reference.md#generate-markdown-command)** - Complete command documentation
- **[Command Cheat Sheet](../cli-cheat-sheet.md)** - Quick reference
- **[Troubleshooting Guide](../troubleshooting/index.md)** - Common issues and solutions
- **[File Processing Guide](file-processing.md)** - General file processing tips

---

*Last updated: 2025-10-28*
