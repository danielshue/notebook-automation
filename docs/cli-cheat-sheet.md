# CLI Command Cheat Sheet

Quick reference for Notebook Automation CLI commands.

---

## 📋 Quick Command Reference

| Command | Purpose | Example |
|---------|---------|---------|
| `na video-notes -p <path>` | Process video files | `na video-notes -p "lecture.mp4"` |
| `na pdf-notes -p <path>` | Process PDF files | `na pdf-notes -p "document.pdf"` |
| `na video-transcripts consolidate --path <folder>` | Combine transcript notes into a single markdown file | `na video-transcripts consolidate --path "Course/Module"` |
| `na generate-markdown -p <path>` | Convert HTML/TXT/EPUB | `na generate-markdown -p "article.html"` |
| `na vault generate-index <path>` | Generate indexes | `na vault generate-index "vault/"` |
| `na vault vault-sync <path>` | Sync with OneDrive | `na vault vault-sync "vault/"` |
| `na tag add-nested <path>` | Add hierarchical tags | `na tag add-nested "vault/"` |
| `na tag consolidate <path>` | Consolidate tags | `na tag consolidate "vault/"` |
| `na config view` | View configuration | `na config view` |
| `na config update <key> <val>` | Update config | `na config update "key" "value"` |
| `na refresh-token` | Authenticate OneDrive | `na refresh-token` |

---

## 🎯 Most Common Commands

### Process Content

```bash
# Process single video
na video-notes -p "video.mp4"

# Process all videos in directory
na video-notes -p "lectures/" --verbose

# Consolidate lesson transcripts into a class note
na video-transcripts consolidate --path "MBA/Finance/Module"

# Process single PDF
na pdf-notes -p "document.pdf"

# Process all PDFs with images
na pdf-notes -p "readings/" --extract-images

# Convert HTML to markdown
na generate-markdown -p "article.html"
```

### Manage Vault

```bash
# Generate indexes
na vault generate-index "vault/" --recursive

# Sync with OneDrive
na vault vault-sync "vault/"

# Ensure metadata consistency
na vault ensure-metadata "vault/"

# Clean all indexes
na vault clean-index "vault/"
```

### Manage Tags

```bash
# Add hierarchical tags
na tag add-nested "vault/"

# Consolidate duplicate tags
na tag consolidate "vault/"

# Check metadata consistency
na tag metadata-check "vault/"

# Update frontmatter field
na tag update-frontmatter "note.md" "status" "done"

# Diagnose YAML issues
na tag diagnose-yaml "vault/"
```

### Configuration

```bash
# View current configuration
na config view

# List available keys
na config list-keys

# Update a key
na config update "Paths.NotebookVaultFullpathRoot" "C:\Vault"

# View secrets status
na config secrets
```

---

## 🚀 Common Workflows

### Process Course Content

```bash
# 1. Process videos
na video-notes -p "course/lectures/" --verbose

# 2. Process PDFs
na pdf-notes -p "course/readings/" --extract-images

# 3. Generate indexes
na vault generate-index "course/" --recursive

# 4. Add tags
na tag add-nested "course/"
```

### Set Up New Vault

```bash
# 1. Authenticate
na refresh-token

# 2. Sync structure
na vault vault-sync "vault/"

# 3. Generate indexes
na vault generate-index "vault/" --recursive

# 4. Add tags
na tag add-nested "vault/"
```

### Organize Existing Vault

```bash
# 1. Add hierarchical tags
na tag add-nested "vault/"

# 2. Consolidate duplicates
na tag consolidate "vault/"

# 3. Regenerate indexes
na vault generate-index "vault/" --recursive --force

# 4. Check metadata
na vault ensure-metadata "vault/"
```

---

## ⚙️ Global Options

| Option | Alias | Description |
|--------|-------|-------------|
| `--config <path>` | `-c` | Use custom config file |
| `--debug` | `-d` | Enable debug output |
| `--verbose` | `-v` | Enable verbose output |
| `--dry-run` | | Preview without changes |
| `--help` | `-h` | Show help |
| `--version` | | Show version |

### Usage Examples

```bash
# Use custom config
na video-notes -p "video.mp4" --config "custom.json"

# Enable verbose output
na pdf-notes -p "docs/" --verbose

# Preview changes
na tag add-nested "vault/" --dry-run

# Debug errors
na video-notes -p "video.mp4" --debug
```

---

## 📁 video-notes Options

| Option | Description |
|--------|-------------|
| `-p, --path` | Path to video or directory (required) |
| `--no-summary` | Skip AI summary generation |
| `--force` | Overwrite existing notes |
| `--retry-failed` | Retry failed files only |
| `--timeout <sec>` | Set API timeout |
| `--refresh-auth` | Refresh OneDrive authentication |
| `--no-share-links` | Skip OneDrive share links |

**Example:**
```bash
na video-notes -p "lectures/" --no-summary --force --verbose
```

---

## 📄 pdf-notes Options

| Option | Description |
|--------|-------------|
| `-p, --path` | Path to PDF or directory (required) |
| `--extract-images` | Extract images from PDFs |
| `--no-summary` | Skip AI summary generation |
| `--force` | Overwrite existing notes |
| `--retry-failed` | Retry failed files only |
| `--timeout <sec>` | Set API timeout |

**Example:**
```bash
na pdf-notes -p "readings/" --extract-images --verbose
```

---

## 🏷️ tag Subcommands

| Subcommand | Purpose |
|------------|---------|
| `add-nested <path>` | Add hierarchical tags from folder structure |
| `clean-index <path>` | Remove tags from index files |
| `consolidate <path>` | Merge duplicate tags |
| `restructure <path>` | Standardize tag format |
| `add-example <path>` | Add example tags |
| `metadata-check <path>` | Validate metadata consistency |
| `update-frontmatter <path> <key> <val>` | Update frontmatter field |
| `diagnose-yaml <path>` | Find YAML syntax errors |

---

## 🗄️ vault Subcommands

| Subcommand | Purpose |
|------------|---------|
| `generate-index <path>` | Generate index files |
| `ensure-metadata <path>` | Ensure metadata consistency |
| `clean-index <path>` | Delete all index files |
| `vault-sync <path>` | Sync with OneDrive |

### vault generate-index Options

```bash
# Generate with options
na vault generate-index "vault/" \
  --recursive \
  --force \
  --type course class module
```

---

## ⚡ Quick Tips

### Preview Before Execution

```bash
# Always use --dry-run for large operations
na video-notes -p "large-directory/" --dry-run --verbose
na tag add-nested "vault/" --dry-run
```

### Use Verbose for Progress

```bash
# See what's happening
na video-notes -p "videos/" --verbose
na pdf-notes -p "pdfs/" --verbose
```

### Debug Errors

```bash
# Get detailed error information
na video-notes -p "problematic-video.mp4" --debug
```

### Process Incrementally

```bash
# Don't process everything at once
na video-notes -p "course/week1/"
na video-notes -p "course/week2/"

# Or retry only failures
na video-notes -p "course/" --retry-failed
```

---

## 🔧 Configuration Quick Reference

### Common Keys

```bash
# Paths
na config update "Paths.NotebookVaultFullpathRoot" "C:\Vault"
na config update "Paths.OnedriveFullpathRoot" "C:\Users\Name\OneDrive"

# AI Service
na config update "AIService.Provider" "OpenAI"
na config update "AIService.Model" "gpt-4"
```

### View All Keys

```bash
na config list-keys
```

---

## 🆘 Emergency Commands

### Something Went Wrong?

```bash
# View current config
na config view

# Debug the issue
na <command> --debug

# Preview what will happen
na <command> --dry-run --verbose
```

### Need to Start Over?

```bash
# Clean all indexes
na vault clean-index "vault/"

# Regenerate everything
na vault generate-index "vault/" --recursive --force
```

### Authentication Issues?

```bash
# Refresh OneDrive token
na refresh-token
```

---

## 📚 Documentation Links

- **[Quick Start](getting-started/quick-start.md)** - Get started in 5 minutes
- **[Basic Commands](getting-started/basic-commands.md)** - Essential commands
- **[CLI Reference](cli-reference.md)** - Complete reference
- **[Tag Management](user-guide/tag-management.md)** - Tag organization
- **[Vault Sync](user-guide/vault-synchronization.md)** - OneDrive integration

---

## 💡 Pro Tips

### Combine Commands for Workflows

```bash
# Complete course setup
na refresh-token && \
na vault vault-sync "vault/" && \
na video-notes -p "vault/lectures/" && \
na pdf-notes -p "vault/readings/" && \
na vault generate-index "vault/" --recursive
```

### Use Aliases (Bash/Zsh)

```bash
# Add to .bashrc or .zshrc
alias nav='na video-notes -p'
alias nap='na pdf-notes -p'
alias nai='na vault generate-index'
alias nat='na tag add-nested'

# Usage
nav "lecture.mp4"
nap "document.pdf"
nai "vault/"
```

### PowerShell Functions

```powershell
# Add to PowerShell profile
function nav { na video-notes -p $args }
function nap { na pdf-notes -p $args }
function nai { na vault generate-index $args }
function nat { na tag add-nested $args }

# Usage
nav "lecture.mp4"
nap "document.pdf"
nai "vault/"
```

---

**Print this sheet for quick reference!**

---

*Last updated: 2025-10-21*
