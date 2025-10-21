# Basic Commands

Learn the essential commands for using Notebook Automation effectively.

## Command Structure

All commands follow this basic structure:

```bash
na [command] [options] [arguments]
```

**Note:** On Windows, you may need to use `.\na.exe` or the full path to the executable. The examples below use `na` for brevity, but both forms work.

**Available Commands:**
- `video-notes` - Process video files
- `pdf-notes` - Process PDF files
- `generate-markdown` - Convert HTML/TXT/EPUB to markdown
- `tag` - Tag management operations
- `vault` - Vault management and synchronization
- `config` - Configuration management
- `refresh-token` - OneDrive authentication

**Global Options:**
- `--config, -c <path>`: Path to configuration file
- `--debug, -d`: Enable debug output
- `--verbose, -v`: Enable verbose output
- `--dry-run`: Simulate actions without making changes
- `--help, -h`: Show help information

## Core Commands

### Video Processing

Process video files to extract transcripts, generate summaries, and create structured notes.

**Basic syntax:**

```powershell
na video-notes -p <path> [options]
```

**Examples:**

Process a single video file:

```powershell
na video-notes -p "documents/lecture-video.mp4"
```

Process all videos in a directory:

```powershell
na video-notes -p "documents/videos/" --verbose
```

Process with custom output directory:

```powershell
na video-notes -p "lecture.mp4" --overwrite-output-dir "results/"
```

**Common Options:**

- `-p, --path <path>`: Path to video file or directory (required)
- `--no-summary`: Skip AI summary generation
- `--force`: Overwrite existing notes
- `--retry-failed`: Retry only failed files from previous run
- `--verbose, -v`: Enable verbose logging
- `--dry-run`: Preview operations without executing
- `--config, -c <path>`: Use custom configuration file

### PDF Processing

Process PDF files to extract text, annotations, and generate structured notes.

**Basic syntax:**

```powershell
na pdf-notes -p <path> [options]
```

**Examples:**

Process a single PDF file:

```powershell
na pdf-notes -p "documents/textbook-chapter.pdf"
```

Process all PDFs in a directory:

```powershell
na pdf-notes -p "documents/pdfs/" --verbose
```

Extract images from PDFs:

```powershell
na pdf-notes -p "document.pdf" --extract-images
```

**Common Options:**

- `-p, --path <path>`: Path to PDF file or directory (required)
- `--extract-images`: Extract images from PDFs
- `--no-summary`: Skip AI summary generation
- `--force`: Overwrite existing notes
- `--retry-failed`: Retry only failed files from previous run
- `--verbose, -v`: Enable verbose logging
- `--dry-run`: Preview operations without executing
- `--config, -c <path>`: Use custom configuration file

### Configuration Management

Manage application configuration and settings.

**View current configuration:**

```powershell
na config view
```

**Update configuration values:**

```powershell
na config update <key> <value>
```

**Examples:**

```powershell
na config update "AIService.Provider" "OpenAI"
na config update "Paths.NotebookVaultFullpathRoot" "C:\MyVault"
```

**List available configuration keys:**

```powershell
na config list-keys
```

**View user secrets status:**

```powershell
na config secrets
```

**Note:** Configuration files are typically located in the `config/` directory. You can specify a custom config file with `--config` or `-c` flag.

### General Commands

Get information about the application.

**Show version:**

```powershell
na --version
```

**Show help:**

```powershell
na --help
na [command] --help  # Command-specific help
```

**Examples:**

```powershell
na --help
na video-notes --help
na config --help
```

### Tag Management

Manage tags in markdown files and ensure consistency across your vault.

**Add nested tags:**

```powershell
na tag add-nested <path>
```

**Consolidate duplicate tags:**

```powershell
na tag consolidate <path>
```

**Check metadata consistency:**

```powershell
na tag metadata-check <path>
```

**Update frontmatter:**

```powershell
na tag update-frontmatter <path> <key> <value>
```

**Examples:**

```powershell
na tag add-nested "vault/projects"
na tag consolidate "vault/notes/document.md"
na tag metadata-check "vault/" --verbose
```

For complete tag management documentation, see the [Tag Management Guide](../user-guide/tag-management.md).

### Vault Management

Manage your Obsidian vault structure and synchronization.

**Generate index files:**

```powershell
na vault generate-index <path>
```

**Ensure metadata consistency:**

```powershell
na vault ensure-metadata <path>
```

**Synchronize with OneDrive:**

```powershell
na vault vault-sync <vault-path>
```

**Examples:**

```powershell
na vault generate-index "vault/courses" --recursive
na vault ensure-metadata "vault/projects"
na vault vault-sync "C:\MyVault" --verbose
```

For complete vault management documentation, see the [Vault Synchronization Guide](../user-guide/vault-synchronization.md).

### Markdown Generation

Generate markdown files from HTML, TXT, and EPUB sources.

**Basic syntax:**

```powershell
na generate-markdown -p <path> [options]
```

**Examples:**

```powershell
na generate-markdown -p "articles/web-content.html"
na generate-markdown -p "books/textbook.epub"
na generate-markdown -p "notes/" --verbose
```

**Common Options:**

- `-p, --path <path>`: Path to source file or directory
- `--extract-from-markdown`: Extract HTML from OneDrive path in frontmatter
- `--no-share-links`: Disable OneDrive share link generation
- `--verbose, -v`: Enable verbose logging

### OneDrive Authentication

Refresh OneDrive authentication token for Microsoft Graph API access.

**Refresh token:**

```powershell
na refresh-token
```

This command is used to authenticate with OneDrive and refresh expired tokens. Run this if you encounter authentication errors when using features that access OneDrive.

## Common Usage Patterns

### Processing Course Content

Process videos and PDFs for a course:

```powershell
na video-notes -p "courses/MBA/lectures/" --verbose
na pdf-notes -p "courses/MBA/readings/" --extract-images
```

### Batch Processing with Custom Config

Process multiple directories with specific settings:

```powershell
na video-notes -p "course1/videos/" --config "academic-config.json"
na pdf-notes -p "course1/pdfs/" --config "academic-config.json"
```

### Preview Operations

Check what will be processed without executing:

```powershell
na video-notes -p "lectures/" --dry-run
na pdf-notes -p "documents/" --dry-run --verbose
```

### Organize and Sync Vault

Generate indexes and sync with OneDrive:

```powershell
# Generate index files for navigation
na vault generate-index "vault/projects" --recursive

# Ensure metadata consistency
na vault ensure-metadata "vault/projects"

# Sync vault structure with OneDrive
na vault vault-sync "vault/"
```

## Tips and Best Practices

### File Paths

- Use quotes around paths containing spaces
- Use forward slashes (/) or double backslashes (\\\\) on Windows
- Relative paths are resolved from the current working directory

### Output Organization

- Use descriptive output directory names
- The CLI automatically uses configured vault paths
- Override output directory with `--overwrite-output-dir` when needed:

  ```bash
  na video-notes -p "video.mp4" --overwrite-output-dir "custom-output/"
  ```

### Configuration Management

- Keep different configuration files for different use cases
- Use version control to track configuration changes
- View current configuration with `na config view` before making changes
- Use `na config list-keys` to see all available configuration options

### Performance Optimization

- Use `--verbose` for troubleshooting, but avoid it for large batches
- Process smaller batches if memory usage becomes an issue
- Configure AI service rate limits appropriately

## Next Steps

- [Configuration Guide](../configuration/index.md) - Set up AI services and customize behavior
- [User Guide](../user-guide/index.md) - Advanced usage scenarios
- [Tag Management](../user-guide/tag-management.md) - Maintain consistent tagging
- [Vault Synchronization](../user-guide/vault-synchronization.md) - OneDrive integration
- [Troubleshooting](../troubleshooting/index.md) - Common issues and solutions
- [CLI Reference](../cli-reference.md) - Complete command reference (coming soon)
