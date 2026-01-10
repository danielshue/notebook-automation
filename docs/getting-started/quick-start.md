# Quick Start Guide

Get up and running with Notebook Automation CLI in 5 minutes.

## Installation

### Prerequisites

- .NET 9.0 SDK or later
- Windows 10/11, Linux, or macOS

### Download

Download the latest release from [GitHub Releases](https://github.com/danielshue/notebook-automation/releases/latest).

### Verify Installation

```bash
na --version
```

You should see version information displayed.

---

## First Steps

### 1. Check Available Commands

```bash
na --help
```

This shows all available commands and options.

### 2. View Configuration

```bash
na config view
```

This displays your current configuration settings.

### 3. Update Basic Paths

Set your vault location:

```bash
na config update "Paths.NotebookVaultFullpathRoot" "C:\MyVault"
```

Set your OneDrive location (if using OneDrive features):

```bash
na config update "Paths.OnedriveFullpathRoot" "C:\Users\YourName\OneDrive"
```

---

## Your First Tasks

### Process a PDF

```bash
na pdf-notes -p "path/to/document.pdf"
```

**What happens:**
1. Extracts text from PDF
2. Generates AI summary (if configured)
3. Creates structured markdown note
4. Saves to your vault

### Process a Video

```bash
na video-notes -p "path/to/video.mp4"
```

**What happens:**
1. Extracts audio and transcript
2. Generates AI summary
3. Creates structured markdown note with timestamps
4. Saves to your vault

### Generate Index Files

```bash
na vault generate-index "path/to/vault" --recursive
```

**What happens:**
1. Scans directory structure
2. Creates index.md files for navigation
3. Organizes content hierarchically

---

## Quick Command Reference

### Most Used Commands

```bash
# Process files
na video-notes -p "video.mp4"
na pdf-notes -p "document.pdf"

# Manage vault
na vault generate-index "vault/"
na vault vault-sync "vault/"

# Manage tags
na tag add-nested "vault/"
na tag consolidate "vault/"

# Configuration
na config view
na config update "key" "value"

# OneDrive
na refresh-token
```

### Helpful Options

```bash
--verbose, -v      # See detailed progress
--dry-run          # Preview without making changes
--debug, -d        # Show detailed error information
--help, -h         # Get help for any command
```

---

## Common Workflows

### Workflow 1: Process Course Content

```bash
# 1. Process lecture videos
na video-notes -p "course/lectures/" --verbose

# 2. Process reading PDFs
na pdf-notes -p "course/readings/" --extract-images

# 3. Generate navigation indexes
na vault generate-index "course/" --recursive

# 4. Add hierarchical tags
na tag add-nested "course/"
```

### Workflow 2: Set Up Vault with OneDrive

```bash
# 1. Authenticate with OneDrive
na refresh-token

# 2. Sync vault structure with OneDrive
na vault vault-sync "vault/"

# 3. Generate indexes
na vault generate-index "vault/" --recursive

# 4. Process placeholder content
na video-notes -p "vault/lectures/"
na pdf-notes -p "vault/readings/"
```

### Workflow 3: Organize Existing Vault

```bash
# 1. Add hierarchical tags
na tag add-nested "vault/" --verbose

# 2. Consolidate duplicate tags
na tag consolidate "vault/"

# 3. Generate fresh indexes
na vault generate-index "vault/" --recursive --force

# 4. Check metadata consistency
na tag metadata-check "vault/"
```

---

## Tips for Success

### 1. Use Dry Run First

Before processing large directories, preview with `--dry-run`:

```bash
na video-notes -p "large-directory/" --dry-run --verbose
```

### 2. Configure AI Service

For best results, configure your AI service:

```bash
na config update "AIService.Provider" "OpenAI"
na config update "AIService.ApiKey" "your-api-key"
```

See [Configuration Guide](../configuration/index.md) for details.

### 3. Check Logs for Errors

If something goes wrong, use `--debug`:

```bash
na video-notes -p "problematic-file.mp4" --debug
```

### 4. Use Config Files for Different Projects

Create separate config files:

```bash
na video-notes -p "work-project/" --config "work-config.json"
na video-notes -p "personal-project/" --config "personal-config.json"
```

### 5. Process Incrementally

Don't process everything at once:

```bash
# Process by section
na video-notes -p "course/week1/"
na video-notes -p "course/week2/"

# Or use retry-failed for errors
na video-notes -p "course/" --retry-failed
```

### 6. File Path Best Practices

- **Use quotes** around paths containing spaces or special characters.
- **Use standard separators**: Forward slashes (`/`) work on all platforms. On Windows, double backslashes (`\\`) are also supported.
- **Relative paths** are resolved from where you run the command.

### 7. Output Organization

- The CLI automatically uses your configured vault paths.
- **Override output** for specific items if needed:

```bash
na video-notes -p "video.mp4" --overwrite-output-dir "custom-output/"
```

---

## Troubleshooting Quick Fixes

### "Command not found"

```bash
# Use full path to executable
/path/to/na.exe --help

# Or on Windows
.\na.exe --help
```

### "Configuration file not found"

```bash
# Specify config path
na --config "path/to/config.json" video-notes -p "file.mp4"
```

### "File not found" or "Path not accessible"

```bash
# Use absolute paths
na video-notes -p "C:\Full\Path\To\video.mp4"

# Or check current directory
pwd  # Linux/macOS
cd   # Windows
```

### "API key not set"

```bash
# Update API configuration
na config update "AIService.ApiKey" "your-key-here"
```

### "OneDrive authentication failed"

```bash
# Refresh token
na refresh-token
```

---

## Next Steps

### Learn More

- **[CLI Reference](../cli-reference.md)** - Complete command reference
- **[Configuration Guide](../configuration/index.md)** - Configuration options
- **[User Guide](../user-guide/index.md)** - Advanced workflows

### Feature Guides

- **[Tag Management](../user-guide/tag-management.md)** - Organize with tags
- **[Vault Synchronization](../user-guide/vault-synchronization.md)** - OneDrive integration

### Get Help

- **GitHub Issues**: [Report problems](https://github.com/danielshue/notebook-automation/issues)
- **Discussions**: [Ask questions](https://github.com/danielshue/notebook-automation/discussions)
- **Documentation**: [Full docs](../index.md)

---

## Quick Wins

### 5-Minute Tasks

**Generate indexes for existing vault:**
```bash
na vault generate-index "vault/" --recursive
```

**Add hierarchical tags:**
```bash
na tag add-nested "vault/"
```

**Process a single document:**
```bash
na pdf-notes -p "important-doc.pdf" --verbose
```

### 15-Minute Tasks

**Set up OneDrive integration:**
```bash
na refresh-token
na vault vault-sync "vault/"
na vault generate-index "vault/" --recursive
```

**Process course lecture folder:**
```bash
na video-notes -p "course/lectures/" --verbose
na vault generate-index "course/" --recursive
```

### 30-Minute Tasks

**Complete vault organization:**
```bash
na tag add-nested "vault/"
na tag consolidate "vault/"
na vault generate-index "vault/" --recursive --force
na vault ensure-metadata "vault/"
```

---

*Get started now and transform your educational content management in minutes!*

---

*Last updated: 2025-10-21*
