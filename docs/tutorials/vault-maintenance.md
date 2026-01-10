# Vault Maintenance Guide

Keep your knowledge base healthy, fast, and organized with these maintenance routines.

## Overview

As your vault grows to hundreds or thousands of notes, entropy sets in. Links break, tags become messy, and indexes get outdated. Notebook Automation includes tools to help you fight this entropy.

**Maintenance Tasks:**
1. 🧹 Cleaning Indexes
2. 🏷️ consolidating Tags
3. 🩺 Metadata Health Checks
4. 💾 Backup Strategies

---

## Part 1: Managing Indexes

Indexes are crucial for navigation, but they can become cluttered or outdated if files are moved manually.

### Regenerate All Indexes
The "Force" option is your friend here. It rebuilds every `_index.md` file from scratch based on the *current* folder structure.

```bash
na vault generate-index "vault/" --recursive --force
```

### Cleaning Orphaned Indexes
If you delete a folder, the index might remain. Use a periodic clean-up script (or manual check) to remove `_index.md` files in empty directories.

*(Future feature: `na vault clean` is planned for v2)*

## Part 2: Tag Hygiene

Inconsistent tagging (e.g., `#finance` vs `#Finance` vs `#fin`) makes retrieval hard.

### 1. Consistent Hierarchies
Use the `tag add-nested` command to enforce folder-based tagging. This ensures that every file in `Courses/Biology/Week1` automatically gets `#Courses/Biology/Week1`.

```bash
na tag add-nested "vault/" --verbose
```

### 2. Identifying Outliers
Use the `ensure-metadata` command to find files that are missing required tags or fields.

```bash
na vault ensure-metadata "vault/" --verbose
```
*Look at the output to see which files were updated or flagged.*

## Part 3: Metadata Health Check

Ensure all your notes conform to your schema. This is critical if you use plugins like Dataview in Obsidian.

**Common Issues:**
- Missing `created` dates, `type` fields, or `tags`.
- Malformed YAML frontmatter.

**The Fix:**
Run a "Metadata Check" (simulated via `ensure-metadata` currently) to repair missing standard fields.

```bash
na vault ensure-metadata "vault/"
```

## Part 4: Backup Strategy

Your vault is valuable. Treat it that way.

### The "3-2-1" Rule for Vaults
- **3 Copies** of your data
- **2 Different MediaTypes** (Local Drive + Cloud)
- **1 Offsite** (Cloud usually covers this)

### Automated Git Backup
Since your vault is just text files, Git is the best backup tool.

**setup-backup.sh:**
```bash
#!/bin/bash
cd "vault/"
git add .
git commit -m "Auto-backup: $(date)"
git push origin main
```
*Schedule this to run daily via Cron (Linux/Mac) or Task Scheduler (Windows).*

## Summary

**Weekly Maintenance Routine:**
1. **Sync**: `git pull` (if using multiple devices)
2. **Tag**: `na tag add-nested "vault/"`
3. **Index**: `na vault generate-index "vault/" --recursive --force`
4. **Backup**: `git push`

**Next:** Explore [Advanced Configuration](../configuration/index.md) to tweak these tools further.
