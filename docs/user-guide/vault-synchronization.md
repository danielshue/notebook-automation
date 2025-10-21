# Vault Synchronization Guide

Learn how to synchronize your Obsidian vault with OneDrive using the Notebook Automation CLI.

## Overview

The vault synchronization system creates a seamless connection between your Obsidian vault and OneDrive, enabling:

- **Document Placeholders**: Markdown files that reference OneDrive content
- **Bidirectional Sync**: Keep vault structure aligned with OneDrive
- **Location-Agnostic Design**: Work across different devices and platforms
- **Hierarchical Organization**: Maintain consistent folder structures
- **Collaborative Workflows**: Share vault organization without sharing content

## Why Vault Synchronization Matters

Traditional file sync has limitations:
- ❌ Large files slow down vault loading
- ❌ Syncing all content isn't always desired
- ❌ Different team members may have different file locations
- ❌ Hard to separate notes from source files

Vault synchronization solves these:
- ✅ Lightweight placeholder files in vault
- ✅ Actual content stays in OneDrive
- ✅ Each user maps to their own OneDrive location
- ✅ Clean separation between notes and source files
- ✅ Works across Windows, macOS, and Linux

---

## Core Concepts

### Document Placeholders

Document placeholders are markdown files that reference OneDrive content without containing it.

**Example Placeholder:**
```markdown
---
title: "Operations Management Lecture Video"
template-type: video-reference
onedrive_relative_path: "MBA/Operations/Lectures/lecture-01.mp4"
---

# Operations Management Lecture Video

This is a reference to video content stored in OneDrive.

**Location**: OneDrive/MBA/Operations/Lectures/lecture-01.mp4
**Process with**: `na video-notes -p "path/to/this/file.md"`
```

### Location-Agnostic Architecture

The system uses relative paths for portability:

**Vault Structure (Same for everyone):**
```
vault/
└── Projects/
    └── MBA/
        └── Operations/
            └── lecture-01-video.md
```

**OneDrive Structure (Different per user):**
```
User A: C:\Users\Alice\OneDrive\Education\MBA\Operations\lecture-01.mp4
User B: /Users/Bob/OneDrive/School/MBA/Operations/lecture-01.mp4
User C: D:\MyCloud\MBA\Operations\lecture-01.mp4
```

**Configuration (User-specific):**
```json
{
  "onedrive_fullpath_root": "C:\\Users\\Alice\\OneDrive\\",
  "onedrive_resources_basepath": "Education",
  "notebook_vault_fullpath_root": "C:\\Vault\\",
  "notebook_vault_resources_basepath": "Projects"
}
```

The CLI combines:
- Relative path from placeholder: `MBA/Operations/lecture-01.mp4`
- User's OneDrive config: `C:\Users\Alice\OneDrive\Education`
- Result: `C:\Users\Alice\OneDrive\Education\MBA\Operations\lecture-01.mp4`

---

## Getting Started

### Prerequisites

1. **OneDrive Account**: Microsoft account with OneDrive access
2. **OneDrive Installed**: OneDrive client installed and syncing
3. **Configuration**: NotebookAutomation configuration file set up

### Initial Setup

#### Step 1: Authenticate with OneDrive

```bash
na refresh-token
```

This opens a browser for Microsoft authentication and stores your credentials securely.

#### Step 2: Configure Paths

View current configuration:
```bash
na config view
```

Update OneDrive path:
```bash
na config update "Paths.OnedriveFullpathRoot" "C:\Users\YourName\OneDrive"
```

Update OneDrive resources base:
```bash
na config update "Paths.OnedriveResourcesBasepath" "Education"
```

Update vault path:
```bash
na config update "Paths.NotebookVaultFullpathRoot" "C:\MyVault"
```

Update vault resources base:
```bash
na config update "Paths.NotebookVaultResourcesBasepath" "Projects"
```

#### Step 3: Verify Configuration

```bash
na config view
```

Check that paths are correct and accessible.

---

## Vault Sync Command

### Basic Sync

Synchronize vault with OneDrive structure:

```bash
na vault vault-sync "path/to/vault"
```

**What Happens:**
1. Scans OneDrive folder structure
2. Creates corresponding folders in vault
3. Generates document placeholder markdown files
4. Preserves relative paths for portability
5. Skips already-synced content (incremental)

### Preview Sync

See what will be synced without making changes:

```bash
na vault vault-sync "path/to/vault" --dry-run --verbose
```

**Output Example:**
```
Scanning OneDrive: C:\Users\Alice\OneDrive\Education
Vault root: C:\Vault\Projects

Will create:
  ✓ vault/Projects/MBA/
  ✓ vault/Projects/MBA/Operations/
  ✓ vault/Projects/MBA/Operations/lecture-01-video.md (placeholder)
  ✓ vault/Projects/MBA/Operations/lecture-02-video.md (placeholder)
  ✓ vault/Projects/MBA/Finance/
  ✓ vault/Projects/MBA/Finance/textbook-ch1-pdf.md (placeholder)

6 items will be created
0 items already exist
```

### Verbose Sync

See detailed progress:

```bash
na vault vault-sync "vault/" --verbose
```

---

## Synchronization Modes

### Bidirectional Sync (Default)

Syncs in both directions:
- OneDrive → Vault: Creates placeholders for OneDrive files
- Vault → OneDrive: Detects placeholders without source files

```bash
na vault vault-sync "vault/"
```

### Important Notes

**What Gets Synced:**
- ✅ Folder structures
- ✅ File references (as placeholders)
- ❌ Actual file content (stays in OneDrive)

**File Types Supported:**
- Videos (`.mp4`, `.avi`, `.mov`, etc.)
- PDFs (`.pdf`)
- HTML (`.html`, `.htm`)
- TXT (`.txt`)
- EPUB (`.epub`)

---

## Common Workflows

### Initial Vault Setup

Set up a new vault with OneDrive integration:

```bash
# 1. Authenticate
na refresh-token

# 2. Configure paths
na config update "Paths.OnedriveFullpathRoot" "C:\Users\Name\OneDrive"
na config update "Paths.NotebookVaultFullpathRoot" "C:\MyVault"

# 3. Preview sync
na vault vault-sync "C:\MyVault" --dry-run --verbose

# 4. Execute sync
na vault vault-sync "C:\MyVault" --verbose

# 5. Generate indexes
na vault generate-index "C:\MyVault" --recursive
```

### Adding New OneDrive Content

After adding files to OneDrive, sync to create placeholders:

```bash
# Sync to create new placeholders
na vault vault-sync "vault/" --verbose

# Generate updated indexes
na vault generate-index "vault/" --recursive --force
```

### Processing Placeholder Content

Process the actual content referenced by placeholders:

```bash
# Process all videos
na video-notes -p "vault/Projects/MBA/Operations/" --verbose

# Process all PDFs
na pdf-notes -p "vault/Projects/MBA/Finance/" --extract-images

# Process single placeholder
na video-notes -p "vault/Projects/MBA/Operations/lecture-01-video.md"
```

### Moving to New Computer

Set up the vault on a new computer:

```bash
# 1. Clone/copy vault (placeholders only, ~lightweight)
# 2. Authenticate on new computer
na refresh-token

# 3. Update configuration for new paths
na config update "Paths.OnedriveFullpathRoot" "/Users/NewUser/OneDrive"
na config update "Paths.NotebookVaultFullpathRoot" "/Users/NewUser/MyVault"

# 4. Verify sync
na vault vault-sync "/Users/NewUser/MyVault" --dry-run --verbose

# Everything works - placeholders resolve to local OneDrive paths
```

### Team Collaboration

Share vault via Git without sharing content:

**Repository Structure:**
```
vault-repo/
├── .gitignore          (excludes generated notes)
├── Projects/
│   └── MBA/
│       ├── Operations/
│       │   ├── lecture-01-video.md  (placeholder - in Git)
│       │   └── lecture-01.md        (generated note - ignored)
│       └── Finance/
│           ├── textbook-ch1-pdf.md  (placeholder - in Git)
│           └── textbook-ch1.md      (generated note - ignored)
└── config/
    └── config.example.json          (template - in Git)
```

**Each team member:**
1. Clones vault repo
2. Has their own OneDrive with source files
3. Configures local paths
4. Syncs to resolve placeholders to their OneDrive
5. Processes content locally

---

## Document Placeholder Details

### Placeholder Naming Convention

Placeholders follow consistent naming:

- **Video files**: `filename-video.md`
  - Example: `03_01_defining-operations-video.md`
- **PDF files**: `filename-pdf.md`
  - Example: `case-study-analysis-pdf.md`
- **HTML files**: `filename-html.md`
  - Example: `course-instructions-html.md`

### Placeholder Frontmatter

Generated placeholders contain:

```yaml
---
title: "Document Title"
template-type: video-reference | pdf-reference | html-reference
onedrive_relative_path: "MBA/Operations/Lectures/video.mp4"
created: 2025-10-21
---
```

**Key Fields:**
- `template-type`: Indicates content type for processing
- `onedrive_relative_path`: Relative path from OneDrive resources base
- Additional fields added during processing

### Processing Placeholders

Convert placeholders to full notes:

```bash
# Video placeholder → Full video note
na video-notes -p "lecture-01-video.md"

# PDF placeholder → Full PDF note
na pdf-notes -p "textbook-ch1-pdf.md"

# HTML placeholder → Markdown note
na generate-markdown -p "article-html.md" --extract-from-markdown
```

---

## Advanced Configuration

### Custom Path Mapping

Configure complex path structures:

```json
{
  "Paths": {
    "OnedriveFullpathRoot": "C:\\Users\\Name\\OneDrive\\",
    "OnedriveResourcesBasepath": "Work\\Education\\Courses",
    "NotebookVaultFullpathRoot": "D:\\Vaults\\Work\\",
    "NotebookVaultResourcesBasepath": "01_Projects\\02_MBA"
  }
}
```

**Result:**
- OneDrive source: `C:\Users\Name\OneDrive\Work\Education\Courses\MBA\..`
- Vault location: `D:\Vaults\Work\01_Projects\02_MBA\MBA\..`

### Multiple Vault Support

Sync different vault sections to different OneDrive locations:

```bash
# Sync work content
na vault vault-sync "vault/Work" --config "config-work.json"

# Sync personal content
na vault vault-sync "vault/Personal" --config "config-personal.json"
```

---

## Troubleshooting

### Authentication Failures

**Problem:** "Authentication failed" or "Token expired"

**Solutions:**
```bash
# Refresh authentication
na refresh-token

# Verify config
na config view

# Test with debug
na vault vault-sync "vault/" --debug
```

### Path Resolution Errors

**Problem:** "File not found" or "Path not accessible"

**Solutions:**

1. **Verify OneDrive is syncing:**
   - Check OneDrive client status
   - Ensure files are downloaded (not cloud-only)

2. **Check configuration:**
   ```bash
   na config view
   ```

3. **Verify paths are absolute:**
   ```bash
   # Windows
   C:\Users\Name\OneDrive\Education
   
   # macOS/Linux
   /Users/name/OneDrive/Education
   ```

4. **Test path access:**
   ```bash
   # Windows
   dir "C:\Users\Name\OneDrive\Education"
   
   # macOS/Linux
   ls "/Users/name/OneDrive/Education"
   ```

### Placeholders Not Created

**Problem:** Sync completes but no placeholders appear

**Solutions:**

1. **Check file types:**
   - Only supported file types get placeholders
   - Supported: `.mp4`, `.pdf`, `.html`, `.txt`, `.epub`

2. **Verify vault path:**
   ```bash
   na vault vault-sync "vault/" --dry-run --verbose
   ```

3. **Check permissions:**
   - Ensure write access to vault directory

### Duplicate Placeholders

**Problem:** Multiple placeholders for same file

**Solution:**
```bash
# Clean up duplicates
find vault -name "*-video.md" | sort | uniq -d

# Manually remove duplicates or re-sync
na vault vault-sync "vault/" --verbose
```

### Sync Takes Too Long

**Problem:** Slow sync on large OneDrive

**Solutions:**

1. **Sync specific sections:**
   ```bash
   na vault vault-sync "vault/Projects/MBA/Operations"
   na vault vault-sync "vault/Projects/MBA/Finance"
   ```

2. **Use dry-run first:**
   ```bash
   na vault vault-sync "vault/" --dry-run
   ```

3. **Process incrementally:**
   - Sync creates only new placeholders
   - Subsequent syncs are faster

---

## Integration with Other Commands

### After Sync Workflow

Complete workflow after vault sync:

```bash
# 1. Sync structure
na vault vault-sync "vault/Projects" --verbose

# 2. Generate indexes
na vault generate-index "vault/Projects" --recursive

# 3. Add hierarchical tags
na tag add-nested "vault/Projects"

# 4. Process content
na video-notes -p "vault/Projects/lectures/"
na pdf-notes -p "vault/Projects/readings/"

# 5. Ensure metadata consistency
na vault ensure-metadata "vault/Projects"
```

### Automated Sync

Schedule regular syncs:

**PowerShell (Windows):**
```powershell
# sync-vault-daily.ps1
na refresh-token
na vault vault-sync "vault/" --verbose
na vault generate-index "vault/" --recursive --force
```

**Bash (Linux/macOS):**
```bash
#!/bin/bash
# sync-vault-daily.sh
na refresh-token
na vault vault-sync "vault/" --verbose
na vault generate-index "vault/" --recursive --force
```

---

## Best Practices

### Organization

1. **Consistent Structure:**
   - Maintain same hierarchy in OneDrive and vault
   - Use descriptive folder names
   - Follow naming conventions

2. **Separation of Concerns:**
   - Source files in OneDrive
   - Generated notes in vault
   - Use `.gitignore` for generated content

3. **Regular Syncs:**
   - Sync after adding OneDrive content
   - Sync before major vault changes
   - Sync when switching devices

### Collaboration

1. **Share Placeholders, Not Content:**
   - Commit placeholders to Git
   - Ignore generated notes
   - Each team member has own content

2. **Document Configuration:**
   - Provide `config.example.json`
   - Document path requirements
   - Include setup instructions

3. **Version Control:**
   ```gitignore
   # .gitignore
   **/lecture-01.md          # Generated video note
   **/textbook-ch1.md        # Generated PDF note
   !**/lecture-01-video.md   # Keep placeholder
   !**/textbook-ch1-pdf.md   # Keep placeholder
   ```

### Performance

1. **Incremental Syncs:**
   - Sync only changed sections
   - Use dry-run to preview large syncs
   - Schedule syncs during off-hours

2. **Content Processing:**
   - Process on-demand, not all at once
   - Use `--retry-failed` for failed items
   - Process by section or course

---

## Related Commands

- [`refresh-token`](../cli-reference.md#refresh-token-command) - OneDrive authentication
- [`vault generate-index`](../cli-reference.md#vault-generate-index) - Generate navigation indexes
- [`vault ensure-metadata`](../cli-reference.md#vault-ensure-metadata) - Metadata consistency
- [`config view`](../cli-reference.md#config-view) - View configuration

---

## See Also

- [CLI Reference](../cli-reference.md) - Complete command reference
- [Tag Management](tag-management.md) - Organize with tags
- [Configuration Guide](../configuration/index.md) - Setup and configuration

---

*Last updated: 2025-10-21*
