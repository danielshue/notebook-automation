#!/usr/bin/env python3
"""
Note Markdown Generator Module

This module provides functions for generating markdown notes from various sources
like PDFs and videos, following the MBA notebook structure.
"""

import os
from datetime import datetime
from pathlib import Path
from typing import Dict, Optional, Any, List

from ruamel.yaml import YAML

from mba_notebook_automation.tools.metadata.path_metadata import (
    extract_metadata_from_path,
    infer_course_and_program
)

yaml = YAML()
yaml.preserve_quotes = True
yaml.width = 4096  # Prevent line wrapping

def get_note_template(note_type: str) -> str:
    """
    Get the template for a specific type of note.
    
    Args:
        note_type (str): Type of note ('pdf', 'video', etc.)
        
    Returns:
        str: Template content
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
    """
    Create or update a markdown note for a PDF file.
    
    Args:
        pdf_path (str): Path to the PDF file
        output_path (str): Path where the markdown note should be saved
        pdf_link (str): Shareable link to the PDF
        summary (Optional[str]): Summary text for the PDF
        metadata (Optional[Dict[str, Any]]): Additional metadata
        
    Returns:
        str: Path to the created or updated note
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
    """
    Create or update a markdown note for a video file.
    
    Args:
        video_path (str): Path to the video file
        output_path (str): Path where the markdown note should be saved
        video_link (str): Shareable link to the video
        transcript (Optional[str]): Transcript text
        summary (Optional[str]): Summary text for the video
        metadata (Optional[Dict[str, Any]]): Additional metadata
        
    Returns:
        str: Path to the created or updated note
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
