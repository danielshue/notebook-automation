#!/usr/bin/env python3
"""
Notes module for generating markdown notes in Obsidian.

This module handles the creation and updating of markdown notes
for videos, including handling frontmatter, templates, and
inserting AI-generated content.
"""

import os
import re
import sys
import yaml
import logging
from pathlib import Path
from datetime import datetime

from ..utils.config import VAULT_ROOT, RESOURCES_ROOT, OPENAI_API_KEY
from ..transcript.processor import find_transcript_file, get_transcript_content
from ..ai.summarizer import generate_summary_with_openai
from ..utils.paths import normalize_path
from ..utils.config import logger

def remove_extra_yaml_blocks(md: str) -> str:
    """
    Ensure no YAML frontmatter or YAML blocks appear after the initial frontmatter.
    Remove any YAML block (--- ... ---) that appears after the initial frontmatter.
    This is a safeguard in case templates or AI output accidentally include YAML.
    
    Args:
        md (str): The markdown content to process
        
    Returns:
        str: The markdown content with extra YAML blocks removed
    """
    # Find the first frontmatter block
    frontmatter_match = re.match(r'^(---\n.*?---\n)', md, re.DOTALL)
    if frontmatter_match:
        frontmatter = frontmatter_match.group(1)
        rest = md[len(frontmatter):]
    else:
        # No frontmatter, just return as is
        return md
    # Remove any subsequent YAML blocks (--- ... ---) in the rest of the document
    rest = re.sub(r'---\n.*?---\n', '', rest, flags=re.DOTALL)
    return frontmatter + rest


def extract_metadata_from_path(file_path):
    """
    Extract course and program information from file path.
    
    Args:
        file_path (Path or str): Path to the file
        
    Returns:
        dict: Extracted metadata including program and course names
    """
    # Normalize the path first
    file_path = normalize_path(file_path)
    if isinstance(file_path, str):
        file_path = Path(file_path)
    
    # Convert to string and normalize slashes
    path_str = str(file_path).replace('\\', '/')
    
    # Extract course and program metadata from the path
    metadata = {}
    
    # Extract program name - usually the first directory after MBA-Resources
    parts = path_str.split('/')
    
    # Debug logging
    logger.debug(f"Path parts: {parts}")
    
    # Look for common program names in the path
    program_indicators = ['MBA', 'EMBA', 'MsBA', 'Executive MBA', 'Data Analytics', 
                         'Marketing', 'Finance', 'Accounting', 'Leadership', 'Strategy']
    
    # Try to find course name first (usually in depth position -2)
    try:
        # Course is often the parent directory
        course_name = file_path.parent.name
        if course_name and course_name not in ['video', 'videos', 'recordings', 'transcripts', 'materials']:
            # Clean up the course name
            course_name = course_name.replace('_', ' ').replace('-', ' ').strip()
            
            # Sanity check: If it's likely not a course name (too short or just a number),
            # try the grandparent directory
            if len(course_name) <= 3 or course_name.isdigit():
                course_name = file_path.parent.parent.name.replace('_', ' ').replace('-', ' ').strip()
                
            metadata['course'] = course_name
    except Exception as e:
        logger.debug(f"Error extracting course name: {e}")
    
    # Try to find program name (usually higher up in the directory structure)
    try:
        # Find the deepest directory that matches a program indicator
        for part in parts:
            clean_part = part.replace('_', ' ').replace('-', ' ').strip()
            for indicator in program_indicators:
                if indicator.lower() in clean_part.lower() and len(clean_part) > 2:
                    metadata['program'] = clean_part
                    break
                    
        # If no program found in directory names, try to infer from structure
        if 'program' not in metadata and 'course' in metadata:
            # Look at the parts above the course
            if len(parts) >= 4:
                potential_program = parts[-3]
                if potential_program not in ['videos', 'recordings', 'transcripts', 'materials']:
                    metadata['program'] = potential_program.replace('_', ' ').replace('-', ' ')
    except Exception as e:
        logger.debug(f"Error extracting program name: {e}")
    
    return metadata

def load_templates():
    """Load YAML templates from the metadata.yaml file in the script directory."""
    script_dir = Path(os.path.dirname(os.path.abspath(sys.argv[0]))).resolve()
    yaml_path = script_dir / "metadata.yaml"

    if not yaml_path.exists():
        logger.error(f"metadata.yaml not found at {yaml_path}")
        return {}

    try:
        with open(yaml_path, 'r', encoding='utf-8') as f:
            templates_list = list(yaml.safe_load_all(f))
        templates = {}
        for doc in templates_list:
            if isinstance(doc, dict) and 'template-type' in doc:
                templates[doc['template-type']] = doc
        logger.info(f"Loaded {len(templates)} templates from metadata.yaml: {', '.join(templates.keys())}")
        return templates
    except Exception as e:
        logger.error(f"Error loading templates: {e}")
        return {}

def create_markdown_note_for_video(video_path, share_link, vault_root, video_item, args=None):
    """
    Create a markdown note for a video in the Obsidian vault.
    
    Args:
        video_path (Path): Path to the video file
        share_link (str): OneDrive shareable link for the video
        vault_root (Path): Root directory of the Obsidian vault
        video_item (dict): Video metadata from OneDrive
        args (Namespace): Command line arguments
        
    Returns:
        Path: Path to the created markdown note
    """
    if isinstance(video_path, str):
        video_path = Path(video_path)
    
    logger.info(f"Creating markdown note for video: {video_path}")
    
    # Get templates
    templates = load_templates()
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

    # Determine the corresponding vault path (preserve OneDrive structure under VAULT_ROOT)
    try:
        rel_path = video_path.relative_to(RESOURCES_ROOT)
    except ValueError:
        # If not under RESOURCES_ROOT, just use the filename and parent
        rel_path = Path(video_path.parts[-2]) / video_path.name
    vault_path = vault_root / rel_path
    vault_dir = vault_path.parent
    os.makedirs(vault_dir, exist_ok=True)

    # Determine the note filename - append -video suffix for Obsidian icon support
    video_name = video_path.stem
    note_name = f"{video_name}-video.md"
    note_path = vault_dir / note_name
    
    # Extract video metadata
    video_created_date = video_item.get('createdDateTime', '').split('T')[0]  # Just the date part
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
            
            # Load the custom prompt templates if available
            custom_summary_prompt = load_prompt_template("final_summary_prompt")
            
            # Generate summary with the custom prompt if available
            ai_summary = generate_summary_with_openai(
                transcript_text, 
                video_name, 
                course_name,
                program_name,
                custom_prompt=custom_summary_prompt
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
    
    # After formatting the template, remove any extra YAML blocks
    formatted_template = remove_extra_yaml_blocks(formatted_template)
    
    # Write the note file
    logger.debug(f"RESOURCES_ROOT: {RESOURCES_ROOT}")
    logger.debug(f"video_path: {video_path}")
    logger.debug(f"rel_path for vault: {rel_path}")
    logger.debug(f"vault_root: {vault_root}")
    logger.debug(f"vault_dir: {vault_dir}")
    logger.debug(f"note_path: {note_path}")
    with open(note_path, 'w', encoding='utf-8') as f:
        f.write(formatted_template)
    logger.info(f"Created markdown note: {note_path}")
    return note_path

def update_markdown_note_for_video(note_path, share_link, video_item, transcript_text=None, args=None):
    """
    Update an existing markdown note for a video.
    
    Args:
        note_path (Path): Path to the markdown note
        share_link (str): OneDrive shareable link for the video
        video_item (dict): Video metadata from OneDrive
        transcript_text (str): Transcript text if available
        args (Namespace): Command line arguments
        
    Returns:
        bool: True if the note was updated, False otherwise
    """
    if isinstance(note_path, str):
        note_path = Path(note_path)
    
    logger.info(f"Updating markdown note: {note_path}")
    
    try:
        # Read the existing note content
        with open(note_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Check if the note is writable (based on auto-generated-state)
        if 'auto-generated-state: readonly' in content:
            logger.info(f"Note {note_path} is marked as readonly. Not updating.")
            return False
        
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
        
        # Update video metadata
        frontmatter['video-uploaded'] = video_created_date
        frontmatter['video-size'] = f"{video_size_mb:.2f} MB"
        
        # Extract metadata for better summary generation
        video_name = frontmatter.get('title', note_path.stem.replace('-video', ''))
        course_name = frontmatter.get('course', '')
        program_name = frontmatter.get('program', '')
        
        # Generate summary and tags if we have a transcript and OpenAI is enabled
        ai_summary = None
        if transcript_text and not (args and getattr(args, 'no_summary', False)):
            if OPENAI_API_KEY:
                logger.info("Updating AI summary and tags with OpenAI...")
                
                # Generate summary
                ai_summary = generate_summary_with_openai(
                    transcript_text, 
                    video_name, 
                    course_name,
                    program_name
                )
                
                # Generate tags (only if no custom tags exist or --force is used)
                # if args and getattr(args, 'force', False) or 'tags' not in frontmatter or not frontmatter['tags']:
                #    tags = generate_tags_with_openai(transcript_text, video_name, course_name, program_name)
                #    frontmatter['tags'] = tags
        
        # Build the new frontmatter
        new_frontmatter = yaml.dump(frontmatter, default_flow_style=False)
        new_content = f"---\n{new_frontmatter}---\n"
        
        # Append the rest of the content (remove old frontmatter)
        rest_content = re.sub(r'^---\n.*?---\n', '', content, flags=re.DOTALL)
        new_content += rest_content
        
        # Update the AI-generated summary if available
        if ai_summary:
            # Try to insert the summary in place of the old one
            summary_marker = "# 🎓 Educational Video Summary"
            notes_marker = "## 📝 Notes"
            
            if summary_marker in new_content and notes_marker in new_content:
                # Replace everything between the summary marker and notes marker
                new_content = re.sub(
                    f"{summary_marker}.*?{notes_marker}",
                    f"{ai_summary}\n\n{notes_marker}",
                    new_content,
                    flags=re.DOTALL
                )
            elif summary_marker in new_content:
                # Replace from the summary marker to the end of the file or to two consecutive newlines
                new_content = re.sub(
                    rf"{summary_marker}.*?(\n\n|\Z)",
                    f"{ai_summary}\n\n",
                    new_content,
                    flags=re.DOTALL
                )
        
        # Remove any extra YAML blocks
        new_content = remove_extra_yaml_blocks(new_content)
        
        # Write the updated note
        with open(note_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        
        logger.info(f"Updated markdown note: {note_path}")
        return True
        
    except Exception as e:
        logger.error(f"Error updating markdown note {note_path}: {e}")
        return False

def load_prompt_template(prompt_name):
    """
    Load a prompt template from a markdown file.
    
    Args:
        prompt_name (str): Name of the prompt template file without extension
        
    Returns:
        str: Content of the prompt template file or None if not found
    """
    # Define the prompt directory path
    prompt_dir = Path(__file__).parent.parent.parent / "prompts"
    
    # Check if the file exists with .md extension
    prompt_file = prompt_dir / f"{prompt_name}.md"
    
    if not prompt_file.exists():
        logger.warning(f"Prompt template file not found: {prompt_file}")
        return None
    
    try:
        with open(prompt_file, "r", encoding="utf-8") as f:
            content = f.read()
            # Remove the frontmatter comment if present
            content = re.sub(r'<!--\s*filepath:.*?-->\s*', '', content, flags=re.DOTALL)
            logger.info(f"Loaded prompt template from {prompt_file}")
            return content
    except Exception as e:
        logger.error(f"Error loading prompt template: {e}")
        return None
