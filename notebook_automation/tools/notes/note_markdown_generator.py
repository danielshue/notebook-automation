#!/usr/bin/env python3
"""Note Markdown Generator Module.

This module provides functions for generating markdown notes from various sources
like PDFs and videos, following the MBA notebook structure. It handles template
loading, metadata extraction, and note file generation with consistent formatting.

The module supports creating notes for PDF documents and video lectures, and includes
templates with appropriate YAML frontmatter and sections for content organization.

Usage:
    from notebook_automation.tools.notes.note_markdown_generator import (
        create_or_update_markdown_note_for_pdf,
        create_or_update_markdown_note_for_video
    )
    
    # Generate a PDF note
    pdf_note_path = create_or_update_markdown_note_for_pdf(
        "path/to/document.pdf",
        "path/to/output/note.md",
        "https://onedrive.com/link-to-pdf",
        "Summary text for the PDF"
    )
    
    # Generate a video note with transcript
    video_note_path = create_or_update_markdown_note_for_video(
        "path/to/lecture.mp4",
        "path/to/output/note.md",
        "https://onedrive.com/link-to-video",
        transcript="Full transcript text",
        summary="Summary of the video content"
    )
"""

import os
from datetime import datetime
from pathlib import Path
from typing import Dict, Optional, Any, List
import logging
import re

from ruamel.yaml import YAML
from notebook_automation.tools.metadata.path_metadata import (
    extract_metadata_from_path
)

# Configure module logger
logger = logging.getLogger(__name__)
logging.getLogger('openai').setLevel(logging.ERROR)
logging.getLogger('requests').setLevel(logging.ERROR)

# Regular expression to match ANSI escape codes (colors and formatting)
ANSI_ESCAPE_PATTERN = re.compile(r'\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])')

# Alternative regex for non-escape sequences like [32m
BRACKET_COLOR_PATTERN = re.compile(r'\[\d{1,2}m')

def strip_ansi_codes(text: str) -> str:
    """Remove ANSI escape codes (colors, formatting) from text.
    
    Args:
        text (str): Text that may contain ANSI escape codes
        
    Returns:
        str: Clean text without ANSI codes
    """
    if not text:
        return ""
    # First replace standard ANSI escape codes (starting with escape character)
    result = ANSI_ESCAPE_PATTERN.sub('', text)
    # Then replace bracket-style color codes like [32m
    result = BRACKET_COLOR_PATTERN.sub('', result)
    return result

yaml = YAML()
yaml.preserve_quotes = True
yaml.width = 4096  # Prevent line wrapping

def get_note_template(note_type: str) -> str:
    """Get the template for a specific type of note.
    
    Retrieves the appropriate markdown template for generating notes based on 
    the specified note type. Templates include placeholders for metadata and content.
    
    Args:
        note_type (str): Type of note ('pdf', 'video', etc.)
        
    Returns:
        str: Template content with placeholders for metadata and sections
        
    Example:
        >>> template = get_note_template('pdf')
        >>> formatted = template.format(title='Document', course='Finance', program='MBA')
    """
    templates = {
        'pdf': """---
title: {title}
course: {course}
program: {program}
type: pdf-notes
date: {date}
pdf-link: {pdf_link}
tags:
  - type/reference
  - course/{course_tag}
---
# {title}

{summary}

## References
- [{title}]({pdf_link})

## Notes
{notes}

""",
        'video': """---
title: {title}
course: {course}
program: {program}
type: video-reference
date: {date}
video-link: {video_link}
tags:
  - type/video
  - course/{course_tag}
---
# {title}

{summary}

## References
- [Video Recording]({video_link})

## Notes
{notes}

"""
    }
    
    return templates.get(note_type, templates['pdf'])

def extract_yaml_tags(content: str) -> List[str]:
    """Extract tags from a YAML block in the content.
    
    Args:
        content (str): The markdown content that may contain YAML frontmatter
        
    Returns:
        List[str]: List of tags found in the YAML frontmatter
    """
    tags = []
    if not content:
        return tags
        
    # Look for YAML frontmatter between --- markers
    yaml_blocks = content.split('---')
    for block in yaml_blocks:
        if 'tags:' in block:
            # Extract tags section
            tag_lines = block.split('tags:')[1].split('\n')
            for line in tag_lines:
                # Look for lines that start with - and optional quote
                if line.strip().startswith('-') and '"' in line:
                    tag = line.split('"')[1].strip()
                    if tag:
                        tags.append(tag)
                elif line.strip().startswith('-'):
                    tag = line.strip()[1:].strip()
                    if tag:
                        tags.append(tag)
    return tags

def extract_content_after_yaml(content: str) -> str:
    """Extract the actual content after YAML frontmatter blocks.
    
    Args:
        content (str): The markdown content that may contain YAML frontmatter
        
    Returns:
        str: Content with YAML frontmatter blocks removed
    """
    if not content:
        return ""
        
    # Split on YAML markers
    parts = content.split('---')
    
    # If no YAML markers, return as is
    if len(parts) <= 2:
        return content.strip()
        
    cleaned_parts = []
    for i, part in enumerate(parts):
        if i > 0:  # Skip first part (before first ---)
            # Skip if this looks like YAML frontmatter
            if ('tags:' in part or 
                'title:' in part or 
                'date:' in part or 
                'type:' in part):
                continue
            # Keep actual content
            cleaned_part = part.strip()
            if cleaned_part:
                cleaned_parts.append(cleaned_part)
                
    return '\n\n'.join(cleaned_parts)

def merge_yaml_frontmatter(template_content: str, ai_content: str) -> str:
    """Merge YAML frontmatter from AI content with template content.
    
    Args:
        template_content (str): The template markdown with base frontmatter
        ai_content (str): The AI-generated content that may have additional frontmatter
        
    Returns:
        str: Merged content with combined frontmatter and tags
    """
    if not ai_content:
        return template_content
        
    # Extract all tags from AI content
    ai_tags = extract_yaml_tags(ai_content)
    
    # Remove any yaml blocks from AI content to avoid duplication
    content_parts = ai_content.split('---')
    cleaned_content = ''
    for i, part in enumerate(content_parts):
        if i > 0 and i < len(content_parts) - 1:  # Skip frontmatter blocks
            if 'tags:' not in part:  # Keep non-tag content
                cleaned_content += part
        elif i == len(content_parts) - 1:  # Last part
            cleaned_content += part
            
    # Extract the main content after YAML blocks
    main_content = extract_content_after_yaml(cleaned_content)
    
    # Find the tags section in the template
    template_parts = template_content.split('tags:')
    if len(template_parts) != 2:
        return template_content
        
    # Combine existing template tags with AI tags
    existing_tags = extract_yaml_tags(template_content)
    all_tags = list(set(existing_tags + ai_tags))  # Remove duplicates
    all_tags.sort()  # Sort for consistency
    
    # Rebuild tags section
    tags_section = 'tags:\n'
    for tag in all_tags:
        if tag.strip():
            tags_section += f'  - "{tag}"\n'
            
    # Combine everything, making sure there's proper spacing
    result = (template_parts[0].rstrip() + '\n' + 
             tags_section + 
             '---\n\n' + 
             main_content)
    return result

def create_or_update_markdown_note_for_pdf(
    pdf_path: str,
    output_path: str,
    pdf_link: str,
    summary: Optional[str] = None,
    metadata: Optional[Dict[str, Any]] = None
) -> str:
    """Create or update a markdown note for a PDF file.
    
    Generates a new markdown note or updates an existing one for a PDF file.
    The note includes metadata, summary, and reference link to the PDF.
    Metadata is either provided or extracted from the file path.
    
    Args:
        pdf_path (str): Path to the PDF file
        output_path (str): Path where the markdown note should be saved
        pdf_link (str): Shareable link to the PDF
        summary (Optional[str]): Summary text for the PDF. If None, a placeholder is added.
        metadata (Optional[Dict[str, Any]]): Additional metadata including title, course, 
            program, and date. If None, metadata is extracted from the path.
        
    Returns:
        str: Path to the created or updated note
        
    Example:
        >>> note_path = create_or_update_markdown_note_for_pdf(
        ...     "path/to/Finance-Basics.pdf",
        ...     "notes/finance-basics.md",
        ...     "https://onedrive.com/link-to-pdf",
        ...     "This document covers basic financial concepts."
        ... )
    """

    logger.debug(f"note_markdown_generator:Generated summary:\n{summary[:500]}...")
    
    # Clean the pdf_link by stripping any ANSI color/formatting codes
    clean_pdf_link = strip_ansi_codes(pdf_link) if pdf_link else ""
    logger.debug(f"PDF link before cleaning: {pdf_link}")
    logger.debug(f"PDF link after cleaning: {clean_pdf_link}")
    
    # Extract metadata from path if not provided
    if metadata is None:
        metadata = extract_metadata_from_path(pdf_path)
    
    # Add current date if not present
    if 'date' not in metadata:
        metadata['date'] = datetime.now().strftime('%Y-%m-%d')
    
    # Clean up course tag
    course_tag = metadata.get('course', '').lower().replace('-', '/')
    
    # Filter out generic OpenAI responses that aren't actual summaries
    filtered_summary = summary
    if summary:
        # Check for common patterns in OpenAI responses that indicate it's not a real summary
        generic_patterns = [
            "please provide the transcript",
            "certainly! please provide",
            "once you share the relevant text",
            "i'll generate a clear and insightful summary",
            "i'd be happy to help you summarize",
            "i would need the transcript",
            "i need more information",
            "without the content of the pdf",
            "would need the actual content",
            "without accessing the content"
        ]
        
        contains_generic_response = any(pattern in summary.lower() for pattern in generic_patterns)
        
        # More detailed logging
        if contains_generic_response:
            logger.warning(f"Detected generic OpenAI response instead of actual summary for PDF {Path(pdf_path).name}. Replacing with placeholder.")
            logger.warning(f"Original summary content: {summary[:100]}...")
            filtered_summary = None
        else:
            # Check if the summary is unusually short (might be incomplete)
            if len(summary.strip().split()) < 15:  # Fewer than 15 words
                logger.warning(f"Summary appears too short ({len(summary.strip().split())} words). Replacing with placeholder.")
                filtered_summary = None

    # Create base note content from template    
    base_content = f"""---
title: {metadata.get('title', Path(pdf_path).stem)}
course: {metadata.get('course', 'MBA Course')}
program: {metadata.get('program', 'MBA')}
date: {metadata['date']}
pdf-link: {clean_pdf_link}
permalink: {metadata.get('permalink', '')}
tags:
  - type/reference
  - course/operations-management
---
# {metadata.get('title', Path(pdf_path).stem)}
"""

    # Merge the AI summary (which might contain YAML frontmatter) with our base content
    merged_content = merge_yaml_frontmatter(base_content, filtered_summary if filtered_summary else "Add summary here")
    
    # Add references and notes sections
    note_content = f"""{merged_content.rstrip()}

## References
- [{Path(pdf_path).stem}]({clean_pdf_link})

## Notes
"""
    
    # Ensure output directory exists
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    
    # If the note exists, preserve everything after the '## Notes' section
    if os.path.exists(output_path):
        with open(output_path, 'r', encoding='utf-8') as f:
            existing = f.read()
        notes_idx = existing.find('## Notes')
        if notes_idx != -1:
            # Find the start of the notes content
            notes_content_idx = existing.find('\n', notes_idx)
            if notes_content_idx != -1:
                notes_content = existing[notes_content_idx:]
                # Remove trailing whitespace from generated note before appending
                note_content = note_content.rstrip() + notes_content    # Write the note
    try:
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(note_content)
    except OSError as e:
        # Log the error and raise it for further handling
        logger.error(f"Error writing to file {output_path}: {e}")
        raise
    return output_path

def create_or_update_markdown_note_for_video(
    video_path: str,
    output_path: str,
    video_link: str,
    transcript: Optional[str] = None,
    summary: Optional[str] = None,
    metadata: Optional[Dict[str, Any]] = None
) -> str:
    """Create or update a markdown note for a video file.
    
    Generates a new markdown note or updates an existing one for a video file.
    The note includes metadata, summary, transcript, and reference link to the video.
    Metadata is either provided or extracted from the file path.
    
    Args:
        video_path (str): Path to the video file
        output_path (str): Path where the markdown note should be saved
        video_link (str): Shareable link to the video
        transcript (Optional[str]): Transcript text of the video. If None, a placeholder is added.
        summary (Optional[str]): Summary text for the video. If None, a placeholder is added.
        metadata (Optional[Dict[str, Any]]): Additional metadata including title, course, 
            program, and date. If None, metadata is extracted from the path.
        
    Returns:
        str: Path to the created or updated note
        
    Example:
        >>> note_path = create_or_update_markdown_note_for_video(
        ...     "path/to/Finance-Lecture.mp4",
        ...     "notes/finance-lecture.md",
        ...     "https://onedrive.com/link-to-video",
        ...     "This is the transcript of the lecture.",
        ...     "This lecture covers basic financial concepts."
        ... )
    """
    # Clean the video_link by stripping any ANSI color/formatting codes
    clean_video_link = strip_ansi_codes(video_link) if video_link else ""
    logger.debug(f"Video link before cleaning: {video_link}")
    logger.debug(f"Video link after cleaning: {clean_video_link}")
    
    # Extract metadata from path if not provided
    if metadata is None:
        metadata = extract_metadata_from_path(video_path)
    
    # Add current date if not present
    if 'date' not in metadata:
        metadata['date'] = datetime.now().strftime('%Y-%m-%d')
    
    # Clean up course tag
    course_tag = metadata.get('course', '').lower().replace('-', '/')
    
    # Filter out generic OpenAI responses that aren't actual summaries
    filtered_summary = summary
    if summary:
        # Check for common patterns in OpenAI responses that indicate it's not a real summary
        generic_patterns = [
            "please provide the transcript",
            "certainly! please provide",
            "once you share the relevant text",
            "i'll generate a clear and insightful summary",
            "i'd be happy to help you summarize",
            "i would need the transcript",
            "i need more information",
            "without the content of the video",
            "would need the actual content",
            "without accessing the content"
        ]
        
        contains_generic_response = any(pattern in summary.lower() for pattern in generic_patterns)
        
        # More detailed logging
        if contains_generic_response:
            logger.warning(f"Detected generic OpenAI response instead of actual summary for video {Path(video_path).name}. Replacing with placeholder.")
            logger.warning(f"Original summary content: {summary[:100]}...")
            filtered_summary = None
        else:
            # Check if the summary is unusually short (might be incomplete)
            if len(summary.strip().split()) < 15:  # Fewer than 15 words
                logger.warning(f"Summary appears too short ({len(summary.strip().split())} words). Replacing with placeholder.")
                filtered_summary = None    # Create base note content from template
    base_content = f"""---
title: {metadata.get('title', Path(video_path).stem)}
course: {metadata.get('course', 'Operations Management')}  # Provide default instead of null
program: {metadata.get('program', 'MBA')}
type: video-reference
date: {metadata['date']}
video-link: {clean_video_link}
permalink: {metadata.get('permalink', '')}
tags:
  - type/video
  - course/{course_tag or 'operations-management'} # Provide default if course_tag is empty
---
# {metadata.get('title', Path(video_path).stem)}
"""

    # Merge the AI summary (which might contain YAML frontmatter) with our base content
    merged_content = merge_yaml_frontmatter(base_content, filtered_summary if filtered_summary else "Add summary here")
    
    # Add references and notes sections
    note_content = f"""{merged_content.rstrip()}

## References
- [Video Recording]({clean_video_link})

## Notes
"""
    
    # Ensure output directory exists
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    
    # If the note exists, preserve everything after the '## Notes' section
    if os.path.exists(output_path):
        with open(output_path, 'r', encoding='utf-8') as f:
            existing = f.read()
        notes_idx = existing.find('## Notes')
        if notes_idx != -1:
            # Find the start of the notes content
            notes_content_idx = existing.find('\n', notes_idx)
            if notes_content_idx != -1:
                notes_content = existing[notes_content_idx:]
                # Remove trailing whitespace from generated note before appending
                note_content = note_content.rstrip() + notes_content    # Write the note
    try:
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(note_content)
    except OSError as e:
        logger.error(
            "Failed to write note to '%s': %s", output_path, e, exc_info=True
        )
        raise
    return output_path