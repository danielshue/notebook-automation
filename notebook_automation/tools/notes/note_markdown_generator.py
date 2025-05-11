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

from ruamel.yaml import YAML

from notebook_automation.tools.metadata.path_metadata import (
    extract_metadata_from_path,
    infer_course_and_program
)

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

## Summary

{summary}

## Key Points

{key_points}

## References

- [{title}]({pdf_link})
""",
        'video': """---
title: {title}
course: {course}
program: {program}
type: video-notes
date: {date}
video-link: {video_link}
tags:
  - type/reference
  - course/{course_tag}
---

# {title}

## Summary

{summary}

## Key Points

{key_points}

## Transcript

{transcript}

## References

- [Video Recording]({video_link})
"""
    }
    
    return templates.get(note_type, templates['pdf'])

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
    # Extract metadata from path if not provided
    if metadata is None:
        metadata = extract_metadata_from_path(pdf_path)
    
    # Add current date if not present
    if 'date' not in metadata:
        metadata['date'] = datetime.now().strftime('%Y-%m-%d')
    
    # Clean up course tag
    course_tag = metadata.get('course', '').lower().replace('-', '/')
    
    # Prepare template variables
    template_vars = {
        'title': metadata.get('title', Path(pdf_path).stem),
        'course': metadata.get('course', ''),
        'program': metadata.get('program', 'MBA'),
        'date': metadata['date'],
        'pdf_link': pdf_link,
        'course_tag': course_tag,
        'summary': summary or "TODO: Add summary",
        'key_points': "TODO: Add key points"
    }
    
    # Get and fill template
    template = get_note_template('pdf')
    note_content = template.format(**template_vars)
    
    # Ensure output directory exists
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    
    # Write the note
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(note_content)
    
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
    # Extract metadata from path if not provided
    if metadata is None:
        metadata = extract_metadata_from_path(video_path)
    
    # Add current date if not present
    if 'date' not in metadata:
        metadata['date'] = datetime.now().strftime('%Y-%m-%d')
    
    # Clean up course tag
    course_tag = metadata.get('course', '').lower().replace('-', '/')
    
    # Prepare template variables
    template_vars = {
        'title': metadata.get('title', Path(video_path).stem),
        'course': metadata.get('course', ''),
        'program': metadata.get('program', 'MBA'),
        'date': metadata['date'],
        'video_link': video_link,
        'course_tag': course_tag,
        'summary': summary or "TODO: Add summary",
        'key_points': "TODO: Add key points",
        'transcript': transcript or "No transcript available."
    }
    
    # Get and fill template
    template = get_note_template('video')
    note_content = template.format(**template_vars)
    
    # Ensure output directory exists
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    
    # Write the note
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(note_content)
    
    return output_path