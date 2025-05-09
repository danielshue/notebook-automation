#!/usr/bin/env python3
"""
Transcript Processing Module

This module provides functions for processing video and audio transcripts,
including cleaning, formatting, and mapping transcripts to videos.
"""

import os
import re
from pathlib import Path
from typing import Dict, List, Optional, Any

def process_transcript(transcript_text: str) -> str:
    """
    Clean and format a transcript for use in notes.
    
    Args:
        transcript_text (str): Raw transcript text
        
    Returns:
        str: Cleaned and formatted transcript text
    """
    # Remove timestamps if present
    cleaned = re.sub(r'\[\d{2}:\d{2}:\d{2}\]', '', transcript_text)
    
    # Remove speaker labels if present
    cleaned = re.sub(r'^Speaker \d+:', '', cleaned, flags=re.MULTILINE)
    
    # Remove extra whitespace
    cleaned = re.sub(r'\s+', ' ', cleaned)
    
    # Split into paragraphs on major pauses or topic changes
    cleaned = re.sub(r'(?&lt;=[.!?])\s+(?=[A-Z])', '\n\n', cleaned)
    
    return cleaned.strip()

def find_matching_video(transcript_path: str, video_dir: str) -> Optional[str]:
    """
    Find the video file that matches a transcript.
    
    Args:
        transcript_path (str): Path to the transcript file
        video_dir (str): Directory to search for matching videos
        
    Returns:
        Optional[str]: Path to the matching video file, if found
    """
    transcript_name = Path(transcript_path).stem
    video_path = Path(video_dir)
    
    # Look for videos with matching names
    video_extensions = ['.mp4', '.mov', '.avi', '.mkv']
    for ext in video_extensions:
        potential_match = video_path / f"{transcript_name}{ext}"
        if potential_match.exists():
            return str(potential_match)
    
    return None

def extract_transcript_metadata(transcript_text: str) -> Dict[str, Any]:
    """
    Extract metadata from transcript content.
    
    Args:
        transcript_text (str): The transcript text to analyze
        
    Returns:
        Dict[str, Any]: Extracted metadata
    """
    metadata = {}
    
    # Try to identify course information
    course_match = re.search(r'Course:\s*([^\n]+)', transcript_text)
    if course_match:
        metadata['course'] = course_match.group(1).strip()
    
    # Try to identify lecture information
    lecture_match = re.search(r'Lecture\s*(\d+)', transcript_text)
    if lecture_match:
        metadata['lecture'] = int(lecture_match.group(1))
    
    # Try to identify topic or title
    title_patterns = [
        r'Title:\s*([^\n]+)',
        r'Topic:\s*([^\n]+)',
        r'Subject:\s*([^\n]+)'
    ]
    
    for pattern in title_patterns:
        match = re.search(pattern, transcript_text)
        if match:
            metadata['title'] = match.group(1).strip()
            break
    
    return metadata

def format_transcript_for_note(transcript_text: str, 
                             include_timestamps: bool = False) -> str:
    """
    Format a transcript for inclusion in a markdown note.
    
    Args:
        transcript_text (str): The transcript text to format
        include_timestamps (bool): Whether to include timestamps
        
    Returns:
        str: Formatted transcript text
    """
    # First clean the transcript
    cleaned = process_transcript(transcript_text)
    
    # Format as a collapsible section
    formatted = [
        "<details>",
        "<summary>Transcript</summary>",
        "",
        cleaned,
        "",
        "</details>"
    ]
    
    return "\n".join(formatted)
