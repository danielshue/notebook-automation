# User Guide

This guide covers the main features and workflows for using Notebook Automation.

## Overview

Notebook Automation processes various types of educational content and transforms them into structured Obsidian notes. The tool supports:

- PDF documents
- Video files (with transcription)
- OneDrive content synchronization
- Batch processing of multiple files

## Basic Workflows

### Processing Individual Files

#### PDF Processing

Process a single PDF document:

```bash
na pdf-notes -p "path/to/document.pdf"
```

Common options:
- `-p, --path`: Path to PDF file or directory (required)
- `--extract-images`: Extract images from PDFs
- `--no-summary`: Skip AI-powered summary generation
- `--force`: Overwrite existing notes
- `--verbose`: Enable verbose logging

#### Video Processing

Process video files with transcription:

```bash
na video-notes -p "path/to/video.mp4"
```

Common options:
- `-p, --path`: Path to video file or directory (required)
- `--no-summary`: Skip AI-powered summary generation
- `--force`: Overwrite existing notes
- `--verbose`: Enable verbose logging

Requirements:
- AI service configured for transcription and summarization

### Batch Processing

Both `video-notes` and `pdf-notes` commands automatically handle batch processing when you provide a directory path:

```bash
# Process all PDFs in a directory
na pdf-notes -p "path/to/directory" --verbose

# Process all videos in a directory
na video-notes -p "path/to/directory" --verbose
```

Tips for batch processing:
- Use `--dry-run` to preview what will be processed
- Use `--retry-failed` to reprocess only failed files
- Use `--verbose` to see detailed progress
- The tool automatically detects and processes all supported files in the directory

### OneDrive Integration

#### Initial Setup

1. Configure OneDrive credentials in your config file
2. Authenticate with OneDrive:

```bash
na refresh-token
```

This command opens a browser window for authentication and stores the refresh token securely.

#### Synchronize Vault Structure

Synchronize your vault directory structure with OneDrive:

```bash
na vault vault-sync "path/to/vault"
```

This creates a bidirectional sync of folder structures (not file contents) between your Obsidian vault and OneDrive, enabling:
- Document placeholder creation for OneDrive files
- Consistent organization across devices
- Seamless integration between local and cloud content

Options:
- `--verbose`: Show detailed sync progress
- `--dry-run`: Preview sync actions without making changes

## Generated Note Structure

Notebook Automation generates structured notes with consistent formatting:

### PDF Notes

```markdown
---
title: "Document Title"
source: "original-file.pdf"
type: "pdf-note"
processed_date: "2025-01-18"
tags: ["course/subject", "document-type"]
---

# Document Title

## Summary

[AI-generated summary of the document]

## Key Points

- Important concept 1
- Important concept 2
- Important concept 3

## Content

[Processed document content with proper formatting]

## Metadata

- **Pages**: 15
- **File Size**: 2.3 MB
- **Processing Date**: 2025-01-18
```

### Video Notes

```markdown
---
title: "Lecture Title"
source: "lecture-video.mp4"
type: "video-note"
duration: "45:30"
processed_date: "2025-01-18"
tags: ["course/subject", "lecture"]
---

# Lecture Title

## Summary

[AI-generated summary of the video content]

## Transcript

[Full transcript with timestamps]

## Key Topics

- Topic 1 (timestamp: 05:30)
- Topic 2 (timestamp: 15:45)
- Topic 3 (timestamp: 32:10)

## Metadata

- **Duration**: 45 minutes 30 seconds
- **File Size**: 1.2 GB
- **Processing Date**: 2025-01-18
```

## Advanced Features

### Metadata Extraction

Notebook Automation automatically extracts and organizes metadata:

- **Document properties**: Title, author, creation date
- **Course structure**: Subject, module, lesson
- **File information**: Size, format, processing date

### Tag Hierarchy

The tool generates hierarchical tags based on content structure:

```
course/
├── mba/
│   ├── finance/
│   │   ├── corporate-finance/
│   │   └── investment-analysis/
│   └── marketing/
│       ├── digital-marketing/
│       └── brand-management/
```

### Cross-Linking

Generated notes include automatic cross-references:

- Links to related documents
- References to course materials
- Connections between concepts

## Customization

### Templates

Customize note generation by modifying templates in the `templates/` directory:

- `pdf-note-template.md`: PDF note structure
- `video-note-template.md`: Video note structure
- `metadata-template.yaml`: Metadata format

### Processing Options

Configure processing behavior in your config file:

```json
{
  "Processing": {
    "GenerateSummaries": true,
    "ExtractMetadata": true,
    "CreateCrossLinks": true,
    "UseHierarchicalTags": true,
    "ChunkSize": 4000
  }
}
```

## Quality Control

### Review Generated Content

Always review generated notes for:

- Accuracy of extracted content
- Relevance of AI-generated summaries
- Proper tag assignment
- Correct cross-linking

### Monitoring Progress

Monitor processing progress through:

- Console output with real-time status
- Log files with detailed processing information
- Progress tracking for batch operations

## Best Practices

### File Organization

- Use consistent naming conventions for source files
- Organize source materials in logical directory structures
- Keep processing configurations in version control

### Performance Optimization

- Process files in smaller batches for better performance
- Use appropriate chunk sizes for your content type
- Monitor system resources during large batch operations

### Error Handling

- Check logs for processing errors
- Retry failed operations with different settings
- Report persistent issues on GitHub

## Troubleshooting

Common issues and solutions:

### Processing Errors

**Problem**: "API key not found"
**Solution**: Verify API key configuration in environment variables or config file

**Problem**: "File access denied"
**Solution**: Check file permissions and ensure files aren't locked by other applications

**Problem**: "Out of memory during processing"
**Solution**: Reduce batch size or chunk size in configuration

### Quality Issues

**Problem**: Poor summary quality
**Solution**: Adjust AI model temperature or try different prompts

**Problem**: Incorrect metadata extraction
**Solution**: Check document format and consider manual metadata entry

For more detailed troubleshooting, see the [Troubleshooting Guide](../troubleshooting/index.md).

This section provides detailed guidance on using all features of Notebook Automation, from basic file processing to advanced configuration and automation workflows.

## Getting Help

If you need assistance:

- Check the [FAQ](../getting-started/faq.md) for common questions
- Review [Troubleshooting](../troubleshooting/index.md) for solutions to common issues
- Visit [GitHub Discussions](https://github.com/danielshue/notebook-automation/discussions) for community support
