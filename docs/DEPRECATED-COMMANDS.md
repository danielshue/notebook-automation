# Deprecated Commands

This document lists commands that were mentioned in older versions of the documentation but never existed or have been renamed.

## Overview

If you're following older documentation or tutorials, you may encounter commands that don't work. This guide helps you find the correct replacement commands.

---

## Command Replacements

### Never Existed Commands

These commands were documented but never actually implemented:

| Old Command | Status | Replacement | Notes |
|------------|--------|-------------|-------|
| `process <path>` | ❌ Never existed | `video-notes -p <path>` or `pdf-notes -p <path>` | Use the specific command for your file type |
| `config init` | ❌ Never existed | Manually create config file | Copy from `config/config.example.json` |
| `config validate` | ❌ Never existed | `config view` | Shows errors if config is invalid |
| `info stats` | ❌ Never existed | Not currently available | Feature may be added in future |
| `process-pdf <path>` | ❌ Never existed | `pdf-notes -p <path>` | Correct command name |
| `process-video <path>` | ❌ Never existed | `video-notes -p <path>` | Correct command name |
| `batch-process <dir>` | ❌ Never existed | Use `-p <dir>` with video-notes or pdf-notes | Directory processing is automatic |
| `onedrive-auth` | ❌ Never existed | `refresh-token` | Correct command for authentication |
| `onedrive-sync` | ❌ Never existed | `vault vault-sync` | Correct command for sync |

### Renamed Commands

These commands were renamed for clarity and consistency:

| Old Command | New Command | Since Version |
|-------------|-------------|---------------|
| `config show` | `config view` | Initial release |
| `config set <key> <value>` | `config update <key> <value>` | Initial release |

---

## Migration Guide

### If You're Using Old Documentation

**Step 1: Identify the command you're trying to use**

Check the tables above to find the replacement command.

**Step 2: Update your scripts or workflows**

Replace the old command with the correct one:

**Before:**
```bash
# ❌ Old (won't work)
na process "documents/lecture-notes.md"
na config show
na config set "AIService.Provider" "OpenAI"
```

**After:**
```bash
# ✅ Correct
na video-notes -p "documents/lecture-notes.md"  # For videos
na pdf-notes -p "documents/lecture-notes.pdf"   # For PDFs
na config view
na config update "AIService.Provider" "OpenAI"
```

**Step 3: Verify the command works**

Test with the `--help` flag:
```bash
na video-notes --help
na config --help
```

---

## Common Scenarios

### Scenario 1: Processing Files

**Old way (documented but never worked):**
```bash
na process "file.pdf"
na process-pdf "file.pdf"
na batch-process "directory/"
```

**Correct way:**
```bash
# For PDFs
na pdf-notes -p "file.pdf"
na pdf-notes -p "directory/"  # Automatically processes all PDFs

# For videos
na video-notes -p "video.mp4"
na video-notes -p "directory/"  # Automatically processes all videos
```

### Scenario 2: Configuration Management

**Old way (documented but never worked):**
```bash
na config init
na config validate
na config show
na config set "key" "value"
```

**Correct way:**
```bash
# View current configuration
na config view

# Update a configuration value
na config update "key" "value"

# List available configuration keys
na config list-keys

# For initial setup, manually create config file
# Copy from config/config.example.json to config/config.json
```

### Scenario 3: OneDrive Integration

**Old way (documented but never worked):**
```bash
na onedrive-auth
na onedrive-sync --folder "Course Materials"
```

**Correct way:**
```bash
# Authenticate with OneDrive
na refresh-token

# Sync vault structure with OneDrive
na vault vault-sync "path/to/vault"
```

---

## Why These Commands Never Existed

The documented commands in older versions of the documentation were placeholders or planned features that:

1. **Were Never Implemented**: Some commands were documented before implementation and the actual implementation used different names
2. **Were Consolidated**: Multiple planned commands were consolidated into more powerful single commands (e.g., `process-pdf` and `process-video` became `pdf-notes` and `video-notes`)
3. **Were Renamed**: Some commands were renamed for clarity during development

---

## Getting Current Documentation

**Always refer to the latest documentation:**

- **[Quick Start Guide](getting-started/quick-start.md)** - Get started in 5 minutes
- **[Basic Commands](getting-started/basic-commands.md)** - Essential commands
- **[CLI Reference](cli-reference.md)** - Complete command reference
- **[Command Cheat Sheet](cli-cheat-sheet.md)** - Quick reference

**Verify commands with `--help`:**
```bash
na --help                    # List all commands
na <command> --help          # Get help for specific command
```

---

## Reporting Documentation Issues

If you find documentation that references deprecated or non-existent commands:

1. **Check this deprecation guide** for the correct replacement
2. **Verify with `--help`** that the command exists
3. **Report the issue** on [GitHub Issues](https://github.com/danielshue/notebook-automation/issues) so we can update the documentation

---

## Version History

- **Current**: All deprecated commands documented and correct replacements provided
- **Future**: This document will be updated as commands evolve

---

*Last updated: 2025-10-21*
