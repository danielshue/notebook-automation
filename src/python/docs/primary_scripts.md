# MBA Notebook Automation - Primary Scripts Reference

This document provides a quick reference list of the primary scripts in the MBA Notebook Automation toolkit, organized by their function.

## Core Functionality

| Script | Purpose | 
|--------|---------|
| `configure.py` | Main configuration script for setting up the automation environment |
| `generate_markdown_from_html_and_text_in_vault.py` | Core script for converting HTML/text to Markdown |
| `generate_pdf_notes_from_onedrive.py` | Creates notes from PDF files in OneDrive |
| `generate_video_meta_from_onedrive.py` | Extracts metadata from video files in OneDrive |
| `generate-index_in_vault.py` | Generates the hierarchical index structure |
| `ensure_consistent_metadata.py` | Ensures all files have consistent metadata |

## Tag Management (in `/tags` directory)

| Script | Purpose |
|--------|---------|
| `add_nested_tags.py` | Main script to add nested tags based on YAML frontmatter |
| `clean_index_tags.py` | Removes tags from index files |
| `consolidate_tags.py` | Consolidates multiple tags into more focused ones |
| `generate_tag_doc.py` | Generates documentation about tags |

## Supporting Functionality

| Script | Purpose |
|--------|---------|
| `onedrive_share.py` | Handles OneDrive sharing functionality |
| `get_onedrive_shareable_link.py` | Generates shareable links for OneDrive files |
| `list_transcripts_with_videos.py` | Maps transcripts to their corresponding videos |
| `process_transcript_stage2.py` | Processes transcripts for video notes |

## Usage

All primary scripts follow a similar pattern for command-line usage:

```bash
python script_name.py [--options] [path]
```

For detailed documentation on each script, see the individual README files in the [`/docs`](../docs/) directory.
