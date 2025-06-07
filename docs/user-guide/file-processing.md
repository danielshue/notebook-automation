# File Processing

Learn how to process different file types and handle various document structures effectively.

## Supported File Types

Notebook Automation can process multiple file formats with intelligent content extraction:

### Markdown Files (.md, .markdown)

- **Best for**: Academic notes, documentation, README files
- **Features**: Full markdown parsing, metadata extraction from frontmatter
- **Example**:

  ```powershell
  .\na.exe process "lecture-notes.md"
  ```

### HTML Files (.html, .htm)

- **Best for**: Web-based content, exported documents
- **Features**: HTML parsing, text extraction, link analysis
- **Example**:

  ```powershell
  .\na.exe process "exported-notes.html"
  ```

### Text Files (.txt)

- **Best for**: Plain text notes, transcripts
- **Features**: Raw text processing, paragraph detection
- **Example**:

  ```powershell
  .\na.exe process "meeting-notes.txt"
  ```

### Rich Text Files (.rtf)

- **Best for**: Formatted documents from word processors
- **Features**: Formatting preservation, text extraction
- **Example**:

  ```powershell
  .\na.exe process "formatted-document.rtf"
  ```

## Content Structure Recognition

The tool automatically recognizes and processes different content structures:

### Academic Documents

- **Lecture notes** with date, course, and topic information
- **Research papers** with abstracts, sections, and citations
- **Study guides** with questions, answers, and explanations

### Technical Documentation

- **API documentation** with endpoints, parameters, and examples
- **User manuals** with procedures, troubleshooting, and references
- **Code documentation** with functions, classes, and usage examples

### General Content

- **Meeting notes** with attendees, agenda, and action items
- **Project documentation** with requirements, specifications, and timelines
- **Knowledge base articles** with categories, tags, and relationships

## Processing Options

### Single File Processing

Process individual files with specific options:

```powershell
# Basic processing
.\na.exe process "document.md"

# With custom output location
.\na.exe process "document.md" --output "results/"

# With specific configuration
.\na.exe process "document.md" --config "academic-config.json"
```

### Multiple File Processing

Process multiple files at once:

```powershell
# Process all markdown files in directory
.\na.exe process "notes/*.md"

# Process specific file types
.\na.exe process "docs/" --include "*.md,*.html" --recursive
```

### Selective Processing

Use filters to process only specific files:

```powershell
# Include only certain patterns
.\na.exe process "docs/" --include "lecture-*.md" --recursive

# Exclude certain patterns
.\na.exe process "docs/" --exclude "draft-*,temp-*" --recursive

# Date-based filtering
.\na.exe process "notes/" --modified-after "2024-01-01" --recursive
```

## Content Extraction Features

### Metadata Extraction

Automatically extract structured metadata:

- **Document properties**: Title, author, creation date, modification date
- **Content structure**: Headings, sections, subsections
- **Academic metadata**: Course codes, topics, keywords
- **Technical metadata**: Programming languages, frameworks, APIs

### Text Analysis

Intelligent text analysis and processing:

- **Summary generation**: Create concise summaries of key points
- **Keyword extraction**: Identify important terms and concepts
- **Topic identification**: Categorize content by subject matter
- **Language detection**: Identify document language and technical terms

### Relationship Detection

Identify connections between documents:

- **Cross-references**: Links between related documents
- **Citation analysis**: Academic references and bibliography
- **Dependency mapping**: Technical documentation relationships
- **Topic clustering**: Group related content automatically

## Output Customization

### Metadata Formats

Choose output format for extracted metadata:

```powershell
# JSON output (default)
.\na.exe process "notes/" --metadata-format json

# YAML output
.\na.exe process "notes/" --metadata-format yaml

# XML output
.\na.exe process "notes/" --metadata-format xml
```

### Summary Styles

Customize summary generation:

```powershell
# Academic style (formal, detailed)
.\na.exe process "papers/" --summary-style academic

# Technical style (precise, structured)
.\na.exe process "docs/" --summary-style technical

# Casual style (conversational, accessible)
.\na.exe process "notes/" --summary-style casual
```

### Content Levels

Control depth of content analysis:

```powershell
# Basic processing (fast, essential metadata only)
.\na.exe process "docs/" --level basic

# Standard processing (balanced analysis)
.\na.exe process "docs/" --level standard

# Deep processing (comprehensive analysis)
.\na.exe process "docs/" --level deep
```

## Quality Control

### Validation

Ensure processing quality with validation options:

```powershell
# Validate file accessibility
.\na.exe process "docs/" --validate-access

# Check file encoding
.\na.exe process "docs/" --validate-encoding

# Verify output completeness
.\na.exe process "docs/" --validate-output
```

### Error Handling

Configure how errors are handled:

```powershell
# Continue on errors (default)
.\na.exe process "docs/" --on-error continue

# Stop on first error
.\na.exe process "docs/" --on-error stop

# Skip problematic files
.\na.exe process "docs/" --on-error skip
```

### Progress Monitoring

Track processing progress:

```powershell
# Show detailed progress
.\na.exe process "docs/" --progress detailed

# Show progress bar only
.\na.exe process "docs/" --progress bar

# Silent processing
.\na.exe process "docs/" --progress none
```

## Best Practices

### File Organization

- **Use consistent naming conventions** for related documents
- **Organize files by topic, date, or project** in directory structures
- **Keep source files and processed outputs separate**

### Processing Strategy

- **Start with small samples** to test configuration and output quality
- **Use appropriate content levels** for your needs (basic for speed, deep for analysis)
- **Configure error handling** based on your tolerance for incomplete processing

### Performance Optimization

- **Process similar file types together** for better caching
- **Use appropriate batch sizes** for your system resources
- **Monitor memory usage** during large batch operations

## Troubleshooting

### Common Issues

**Files not being processed:**

- Check file permissions and accessibility
- Verify file format is supported
- Review include/exclude patterns

**Poor quality extraction:**

- Adjust content analysis level
- Check document structure and formatting
- Review AI service configuration

**Slow processing:**

- Reduce batch size
- Use faster AI models
- Optimize rate limiting settings

For more troubleshooting help, see the [Troubleshooting Guide](../troubleshooting/index.md).

## Next Steps

- [Batch Operations](batch-operations.md) - Process multiple files efficiently
- [Output Management](output-management.md) - Organize and customize results
- [Performance Tuning](performance-tuning.md) - Optimize processing speed
