#!/usr/bin/env python3
"""
Notes module for generating markdown notes in Obsidian.

This module handles the creation and updating of markdown notes
for videos, including handling frontmatter, templates, and
inserting AI-generated content.
"""

import os
import re
import yaml
import os
import re
import yaml
from pathlib import Path
from datetime import datetime

from ..utils.config import RESOURCES_ROOT, OPENAI_API_KEY
from ..transcript.processor import find_transcript_file, get_transcript_content
from ..ai.summarizer import generate_summary_with_openai
from ..utils.config import logger
from ..metadata.path_metadata import extract_metadata_from_path, load_metadata_templates
from ..ai import prompt_utils

def create_or_update_markdown_note_for_video(video_path, share_link, vault_root, video_item, args=None, dry_run=False):
    """
    Create or update a markdown note for a video in the Obsidian vault.
    If the note exists and is writable, update it. Otherwise, create a new note.
    If dry_run is True, log actions instead of executing them.
    """
    if isinstance(video_path, str):
        video_path = Path(video_path)
    video_name = video_path.stem

    # Determine the corresponding vault path (preserve OneDrive structure under VAULT_ROOT)
    try:
        rel_path = video_path.relative_to(RESOURCES_ROOT)
    except ValueError:
        rel_path = Path(video_path.parts[-2]) / video_path.name
    vault_path = vault_root / rel_path
    vault_dir = vault_path.parent
    note_name = f"{video_name}-video.md"
    note_path = vault_dir / note_name

    if dry_run:
        logger.info(f"[DRY RUN] Would create directory: {vault_dir}")
    else:
        os.makedirs(vault_dir, exist_ok=True)

    # If note exists, try to update it
    if note_path.exists():
        logger.info(f"Note exists, attempting update: {note_path}")
        if dry_run:
            logger.info(f"[DRY RUN] Would read and update note: {note_path}")
            return note_path
        # Read the existing note content
        with open(note_path, 'r', encoding='utf-8') as f:
            content = f.read()
        if 'auto-generated-state: readonly' in content:
            logger.info(f"Note {note_path} is marked as readonly. Not updating.")
            return note_path
        # Parse YAML frontmatter
        frontmatter_match = re.search(r'^---\n(.*?)---\n', content, re.DOTALL)
        if frontmatter_match:
            frontmatter_text = frontmatter_match.group(1)
            try:
                frontmatter = yaml.safe_load(frontmatter_text)
            except Exception as e:
                logger.error(f"Error parsing frontmatter: {e}")
                frontmatter = {}
        else:
            frontmatter = {}
        # Update OneDrive sharing link
        if share_link:
            frontmatter['onedrive-sharing-link'] = share_link
        # Extract video metadata
        video_created_date = video_item.get('createdDateTime', '').split('T')[0]
        video_size_bytes = video_item.get('size', 0)
        video_size_mb = video_size_bytes / (1024 * 1024) if video_size_bytes else 0
        frontmatter['video-uploaded'] = video_created_date
        frontmatter['video-size'] = f"{video_size_mb:.2f} MB"
        video_name_val = frontmatter.get('title', video_name)
        course_name = frontmatter.get('course', '')
        program_name = frontmatter.get('program', '')
        # Find transcript if available
        transcript_path = None
        transcript_text = None
        if not args or not getattr(args, 'no_transcript', False):
            transcript_path = find_transcript_file(video_path, vault_root)
            if transcript_path:
                transcript_text = get_transcript_content(transcript_path)
        # Generate summary if transcript and OpenAI enabled
        ai_summary = None
        if transcript_text and not (args and getattr(args, 'no_summary', False)):
            if OPENAI_API_KEY:
                logger.info("Updating AI summary and tags with OpenAI...")
                # Use prompt_utils to get system and user prompts
                metadata = {
                    'video_name': video_name_val,
                    'course_name': course_name,
                    'program_name': program_name,
                    'transcript_text': transcript_text,
                    'share_link': share_link,
                    # ...add more as needed...
                }
                system_prompt = prompt_utils.get_video_system_prompt(metadata)
                user_prompt = prompt_utils.get_video_user_prompt(metadata)
                ai_summary = generate_summary_with_openai(
                    transcript_text,
                    system_prompt=system_prompt,
                    user_prompt=user_prompt,
                    metadata=metadata
                )
        # Build the new frontmatter
        new_frontmatter = yaml.dump(frontmatter, default_flow_style=False)
        new_content = f"---\n{new_frontmatter}---\n"
        rest_content = re.sub(r'^---\n.*?---\n', '', content, flags=re.DOTALL)
        new_content += rest_content
        # Update the AI-generated summary if available
        if ai_summary:
            summary_marker = "# 🎓 Educational Video Summary"
            notes_marker = "## 📝 Notes"
            if summary_marker in new_content and notes_marker in new_content:
                new_content = re.sub(
                    f"{summary_marker}.*?{notes_marker}",
                    f"{ai_summary}\n\n{notes_marker}",
                    new_content,
                    flags=re.DOTALL
                )
            elif summary_marker in new_content:
                new_content = re.sub(
                    rf"{summary_marker}.*?(\n\n|\Z)",
                    f"{ai_summary}\n\n",
                    new_content,
                    flags=re.DOTALL
                )
        if not dry_run:
            with open(note_path, 'w', encoding='utf-8') as f:
                f.write(new_content)
        logger.info(f"Updated markdown note: {note_path}")
        return note_path

    # If note does not exist, create it
    logger.info(f"Creating markdown note for video: {video_path}")
    if dry_run:
        logger.info(f"[DRY RUN] Would create markdown note: {note_path}")
        return note_path
    templates = load_metadata_templates()
    video_template = templates.get('video-reference', {}).get('content', '')
    if not video_template:
        logger.warning("No video template found in metadata file. Using minimal template.")
        video_template = """---
auto-generated-state: writable  # Don't change this field, used for automation to know if it can update this note
template-type: video-reference
title: "{title}"
date-created: "{date_created}"
tags: {tags}
onedrive-video-path: "{onedrive_video_path}"
onedrive-sharing-link: "{onedrive_sharing_link}"
status: unread # unread, in-progress, completed
completion-date: 
review-date:
comprehension: # high, medium, low
---

# ▶️ {title}

📅 Video Reference

## Video Link
[🖥️ Watch Video]({onedrive_sharing_link})

{summary}

## 📝 Notes

"""

    # Extract video metadata
    video_created_date = video_item.get('createdDateTime', '').split('T')[0]
    video_modified_date = video_item.get('lastModifiedDateTime', '').split('T')[0]
    video_size_bytes = video_item.get('size', 0)
    video_size_mb = video_size_bytes / (1024 * 1024) if video_size_bytes else 0
    
    # Extract course and program metadata from path
    metadata = extract_metadata_from_path(video_path)
    course_name = metadata.get('course', '')
    program_name = metadata.get('program', '')
    
    # Extract the relative path from OneDrive
    onedrive_path = video_item.get('parentReference', {}).get('path', '')
    if '/root:/' in onedrive_path:
        onedrive_relative_path = onedrive_path.split('/root:/')[1]
    else:
        onedrive_relative_path = ''
    
    onedrive_video_path = f"{onedrive_relative_path}/{video_item.get('name', '')}" if onedrive_relative_path else video_item.get('name', '')
    
    # Find a transcript file if available
    transcript_path = None
    transcript_text = None
    if not args or not getattr(args, 'no_transcript', False):
        transcript_path = find_transcript_file(video_path, vault_root)
        logger.info(f"Transcript search result: {transcript_path}")
        
        if transcript_path:
            transcript_text = get_transcript_content(transcript_path)
      # Generate summary and tags using OpenAI if transcript is available
    ai_summary = None
    tags = ['video', 'course-materials']
    
    if transcript_text and not (args and getattr(args, 'no_summary', False)):
        if OPENAI_API_KEY:
            logger.info("Generating AI summary and tags with OpenAI...")
            print("  ├─ Generating AI summary and tags with OpenAI...")
            # Use prompt_utils to get system and user prompts
            metadata = {
                'video_name': video_name,
                'course_name': course_name,
                'program_name': program_name,
                'transcript_text': transcript_text,
                'share_link': share_link,
                # ...add more as needed...
            }
            system_prompt = prompt_utils.get_video_system_prompt(metadata)
            user_prompt = prompt_utils.get_video_user_prompt(metadata)
            
            ai_summary = generate_summary_with_openai(
                transcript_text,
                system_prompt=system_prompt,
                user_prompt=user_prompt,
                metadata=metadata,
                dry_run=dry_run
            )
            
            # Use summary to generate tags (simple heuristic: extract keywords from summary)
            if ai_summary:
                tags = list(set([w.strip('#.,:;!?()[]{}"\'') for w in ai_summary.split() if len(w) > 3]))[:8] + ['video', 'course-materials']
            logger.info(f"Generated {len(tags)} tags: {', '.join(tags[:5])}...")
            print(f"  │  └─ Generated {len(tags)} tags ✓")
    
    # Format the template with the video metadata
    title = video_name.replace('-', ' ')
    date_created = datetime.now().strftime('%Y-%m-%d')
    
    # Create a formatted file
    formatted_template = video_template.format(
        title=title,
        date_created=date_created,
        tags=tags,
        onedrive_video_path=onedrive_video_path,
        onedrive_sharing_link=share_link,
        video_created_date=video_created_date,
        video_modified_date=video_modified_date,
        video_size=f"{video_size_mb:.2f} MB",
        summary=ai_summary if ai_summary else "## AI Summary\n\nNo transcript found or OpenAI API key not configured."
    )
    
    # Write the note file
    logger.debug(f"RESOURCES_ROOT: {RESOURCES_ROOT}")
    logger.debug(f"video_path: {video_path}")
    logger.debug(f"rel_path for vault: {rel_path}")
    logger.debug(f"vault_root: {vault_root}")
    logger.debug(f"vault_dir: {vault_dir}")
    logger.debug(f"note_path: {note_path}")
    if not dry_run:
        with open(note_path, 'w', encoding='utf-8') as f:
            f.write(formatted_template)
    logger.info(f"Created markdown note: {note_path}")
    return note_path


def load_prompt_template(prompt_name):
    """
    Load a prompt template from the prompts directory by name (without extension).
    Returns the template string or None if not found.
    """
    prompts_dir = Path(__file__).parent.parent.parent / 'prompts'
    prompt_path = prompts_dir / f"{prompt_name}.md"
    if not prompt_path.exists():
        logger.warning(f"Prompt file not found: {prompt_path}")
        return None
    with open(prompt_path, 'r', encoding='utf-8') as f:
        return f.read()

