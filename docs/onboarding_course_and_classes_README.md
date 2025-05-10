---
title: Onboarding New Course
date-created: Thursday, May 8th 2025, 9:52:11 am
date-modified: Thursday, May 8th 2025, 3:15:00 pm
---
# Onboarding Course or Class (multiple classes in a course)

## Table of Contents
- [Initial Setup and Configuration](#initial-setup-and-configuration)
- [General Process Flow for Onboarding a New Course](#general-process-flow-for-onboarding-a-new-course)
- [Onboard New Class Steps](#onboard-new-class-steps)
- [Ad-hoc Utilities to Run](#ad-hoc-utilities-to-run)
- [Workspace Organization](#workspace-organization)
- [Troubleshooting](#troubleshooting)
- [Regular Maintenance Tasks](#regular-maintenance-tasks)

## Initial Setup and Configuration

Before you can start using the MBA Notebook Automation tools, you need to set up your environment properly:

1. **Configure the workspace** by running:
   ```pwsh
   notebook-configure show
   ```
   Or, for interactive setup:
   ```pwsh
   python configure.py
   ```
   This interactive script will:
   - Set the path to your Obsidian vault
   - Configure the OneDrive MBA-Resources folder location
   - Set up API credentials if needed
   - Create necessary configuration files

2. **Verify your config.json** file contains correct paths:
   - `vault_path`: Path to your Obsidian vault
   - `onedrive_resources_path`: Path to your OneDrive MBA-Resources folder
   - `mba_vault_folder`: The folder within your vault where MBA notes are stored

3. **Install required Python packages**:
   ```pwsh
   pip install -e .
   ```

## General Process Flow for Onboarding a New Course

TODO: This process should be automated.

1. Download the course using the Windows application that scrapes all of the course content from Coursera.

2. Move videos and transcripts to the MBA OneDrive location. For example, copy the contents into the OneDrive Resources folder that is defined in the configuration:
   - Example path: `[UserAccount]\OneDrive\Education\MBA-Resources`

3. Convert HTML files to Markdown by running:
   ```pwsh
   python generate_markdown_from_html_and_text_in_vault.py /path/to/downloaded/course
   ```
   This places newly created markdown files into a mirrored folder location in the Vault. Vault definition is configurable in `config.json`.

4. Ensure metadata consistency by running:
   ```pwsh
   python ensure_consistent_metadata.py
   ```
   This makes sure the Course, Class, and Program metadata fields are correctly set. Note: this may need to be run again after any manual edits.

5. Convert videos & transcripts to markdown by running:
   ```pwsh
   python generate_video_meta_from_onedrive.py /path/to/downloaded/course
   ```
   - After creating the video notes, update tags by running:
     ```pwsh
     notebook-add-nested-tags /path/to/downloaded/course --verbose
     ```
    

## Onboard New Class Steps

TODO: This process should be automated.

1. Create the "Case Studies" and "Required Reading" folders for each class folder in the corresponding Vault location.

2. Create the "Live Class" folder for each "Module" that is part of each class in the corresponding Vault location.

3. Download and parse all required reading into PDF files based on assignments.

4. Process PDF files by running:
   ```pwsh
   python generate_pdf_notes_from_onedrive.py
   ```

5. (Optional) Upload PDF files to Readwise Reader for additional processing.

6. Create assignments using the Templater script:
   ```pwsh
   # In Obsidian, use the template from:
   Templater/class_assignments.md
   ```
   Fill out all required TODOs along with due dates.

7. Create a class dashboard using the Templater script:
   ```pwsh
   # In Obsidian, use the template from:
   Templater/class_dashboard.md
   ```
   This will contain Instructions, Required Readings, Videos, Case Studies, Assignments, and Tasks.

## Ad-hoc Utilities to Run

1. Ensure proper tag structure throughout your vault:
   ```pwsh
   # Add nested tags based on frontmatter
   notebook-add-nested-tags . --verbose

   # Clean tags from index files
   notebook-clean-index-tags . --verbose

   # Consolidate tags to reduce duplication
   notebook-consolidate-tags . --verbose
   ```

2. After attending a "Live Class", import the video and transcript note files:
   ```bash
   # List transcripts with matching videos
   python list_transcripts_with_videos.py
   
   # Process transcripts for video notes
   python process_transcript_stage2.py
   ```

## Workspace Organization

The MBA Notebook Automation workspace is organized as follows:

- **Root Directory**: Contains primary scripts for essential functionality
  - `configure.py` - Setup and configuration
  - `ensure_consistent_metadata.py` - Ensures consistent metadata across files
  - `generate_markdown_from_html_and_text_in_vault.py` - Converts HTML to Markdown
  - `generate_pdf_notes_from_onedrive.py` - Creates notes from PDF files
  - `generate_video_meta_from_onedrive.py` - Processes video files
  - `generate-index_in_vault.py` - Creates index structure
  - `process_transcript_stage2.py` - Processes transcripts

- **`/tags`**: Contains tag management scripts
  - `add_nested_tags.py` - Main script for tags based on frontmatter
  - `clean_index_tags.py` - Removes tags from index files
  - `consolidate_tags.py` - Reduces tag duplication
  - `generate_tag_doc.py` - Creates tag documentation
  - `restructure_tags.py` - Improves tag hierarchy

- **`/docs`**: Contains all documentation files
  - `index.md` - Main documentation index
  - `final_organization.md` - Organization overview
  - `primary_scripts.md` - Script reference
  - Various README files for specific features

- **`/data`**: Contains JSON output and mapping files
  - JSON results from script runs
  - Mapping files for transcripts and videos
 
- **`/utilities`**: Contains utility scripts
  - `extract_pdf_pages.py` - Extracts specific pages from PDFs
  - `list_folder_contents.py` - Lists folder contents

- **`/tools`**: Contains core package functionality
  - Reusable modules used by multiple scripts
  - Core functionality organized in a package structure

- **`/logs`**: Contains log files from script execution
  - Each script generates its own log file
  - Log files are named after the scripts that create them

For more information about the workspace organization and available scripts, see:
- [Primary Scripts Reference](primary_scripts.md) - Quick reference for the most important scripts
- [Final Organization Structure](final_organization.md) - Detailed overview of the workspace structure

## Troubleshooting

If you encounter issues with any script:

1. **Check log files** in the `/logs` directory:
   ```bash
   # View the most recent log file for a specific script
   Get-Content -Path "logs\generate_pdf_notes_from_onedrive.log" -Tail 20
   
   # List all log files sorted by modification date
   Get-ChildItem -Path "logs\*.log" | Sort-Object LastWriteTime -Descending
   ```

2. **Use debug scripts** in the `/debug` directory for troubleshooting:
   ```bash
   # For tag-related issues
   python debug/debug_tag_doc.py
   
   # For transcript processing issues
   python debug/debug_transcript_finder.py
   ```

3. **Verify configuration** using:
   ```bash
   # Run the configuration script again
   python configure.py
   
   # View current configuration
   Get-Content -Path "config.json"
   ```
   
   The `configure.py` script must be run for every new installation to ensure proper paths are set for:
   - The MBA Vault folder inside your Obsidian vault
   - The OneDrive MBA-Resources location that stores videos, PDFs, and other non-markdown files
   
4. **Common issues and solutions**:
   
   - **Authentication errors**: Delete the `cache/token_cache.bin` file and run the script again to re-authenticate
   - **File not found errors**: Ensure the paths in `config.json` are correct and files exist in the specified locations
   - **Import errors**: Make sure you're running the scripts from the root directory of the project
   - **Tag issues**: Run `notebook-add-nested-tags . --verbose` to rebuild tag structure if tags are missing or malformed
## CLI Tools Quick Reference

All major tag and note management tasks are now available as CLI entry points after installation:

- `notebook-add-nested-tags`      — Add nested tags to notes based on YAML frontmatter
- `notebook-clean-index-tags`     — Remove all tags from index files
- `notebook-consolidate-tags`     — Consolidate/sort tags in notes, remove duplicates
- `notebook-generate-tag-doc`     — Generate tag usage and hierarchy documentation
- `notebook-restructure-tags`     — Restructure tags for consistency (e.g., lowercase, dashes)
- `notebook-tag-manager`          — Advanced tag management, refactoring, and analysis

All tools support `--help` for usage and `--verbose` for colorized, detailed output.

## Regular Maintenance Tasks

To keep your MBA Notebook system running smoothly, perform these maintenance tasks regularly:

1. **Update tag structure** weekly or after adding significant content:
   ```bash
   python tags/add_nested_tags.py
   python tags/clean_index_tags.py
   ```

2. **Verify metadata consistency** monthly:
   ```bash
   python ensure_consistent_metadata.py
   ```

3. **Generate index files** after adding new courses or classes:
   ```bash
   python generate-index_in_vault.py
   ```

4. **Check for orphaned files** periodically:
   ```bash
   # Find markdown files without proper links
   # (Script to be created)
   python utilities/find_orphaned_files.py
   ```

5. **Clean up unused data files** quarterly:
   ```bash
   # Remove JSON files older than 90 days from data directory
   Get-ChildItem -Path "data\*.json" | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-90) } | Remove-Item
   ```