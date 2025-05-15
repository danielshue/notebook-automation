#!/usr/bin/env python3
"""Transcript Processing Module.

This module provides functions for processing video and audio transcripts,
including cleaning, formatting, and mapping transcripts to videos. It handles
transcript text normalization, removal of timestamps, speaker labels, and other
common artifacts from automated speech recognition output.

The module also includes functionality to find matching video files for transcript
files based on filename patterns and similarities.

Usage:
    from notebook_automation.tools.transcript.processor import (
        process_transcript, 
        find_matching_video, 
        generate_transcript_video_mapping
    )
    
    # Clean and format a raw transcript
    cleaned_transcript = process_transcript(raw_transcript_text)
    
    # Find a video file that matches a transcript
    video_path = find_matching_video("path/to/transcript.txt", "path/to/videos/")
    
    # Generate mapping between transcripts and videos
    mapping = generate_transcript_video_mapping(
        "path/to/transcripts/", 
        "path/to/videos/"
    )
"""

import os
import re
from pathlib import Path
from typing import Dict, List, Optional, Any

def process_transcript(transcript_text: str) -> str:
    """Clean and format a transcript for use in notes.
    
    This function takes raw transcript text, typically generated from automatic
    speech recognition systems, and processes it to be more readable in notes.
    Processing steps include removing timestamps, speaker labels, normalizing
    whitespace, and splitting text into logical paragraphs.
    
    Args:
        transcript_text (str): Raw transcript text from an audio/video transcription
            
    Returns:
        str: Cleaned and formatted transcript text ready for inclusion in notes
            
    Example:
        >>> raw_text = "[00:01:23] Speaker 1: Hello, this is a test."
        >>> process_transcript(raw_text)
        'Hello, this is a test.'
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
    """Find the video file that matches a transcript based on filename similarity.
    
    This function attempts to locate a video file that corresponds to a given transcript
    by comparing filenames. It handles various common filename patterns and variations
    to maximize the chances of finding a correct match, even when naming conventions
    aren't completely consistent.
    
    Args:
        transcript_path (str): Path to the transcript file
        video_dir (str): Directory to search for matching videos
        
    Returns:
        Optional[str]: Path to the matching video file if found, None otherwise
        
    Raises:
        FileNotFoundError: If the video directory doesn't exist
        
    Example:
        >>> find_matching_video("/transcripts/lecture1.txt", "/videos")
        '/videos/lecture1.mp4'
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
    """Extract metadata from transcript content.
    
    Analyzes transcript text to automatically identify and extract metadata 
    like course name, lecture number, and title/topic. Uses regular expressions
    to identify common patterns in transcript headers or content.
    
    Args:
        transcript_text (str): The transcript text to analyze
        
    Returns:
        Dict[str, Any]: Extracted metadata with keys such as 'course', 'lecture', 
            and 'title' if found in the text
            
    Example:
        >>> text = "Course: Finance 101\\nLecture 3\\nTitle: Cash Flow Analysis"
        >>> extract_transcript_metadata(text)
        {'course': 'Finance 101', 'lecture': 3, 'title': 'Cash Flow Analysis'}
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
    """Format a transcript for inclusion in a markdown note.
    
    Processes the transcript text and formats it for optimal readability within a 
    markdown note. By default, it creates a collapsible section with the transcript
    content, allowing users to expand it only when needed.
    
    Args:
        transcript_text (str): The transcript text to format
        include_timestamps (bool): Whether to include timestamps in the formatted output.
            When True, preserves timestamp information. Defaults to False.
        
    Returns:
        str: Formatted transcript text with HTML details/summary tags for collapsible display
        
    Example:
        >>> formatted = format_transcript_for_note("This is a transcript of the lecture.")
        >>> print(formatted)
        '<details>
        <summary>Transcript</summary>
        
        This is a transcript of the lecture.
        
        </details>'
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

def generate_transcript_video_mapping(transcript_dir: str, video_dir: str) -> Dict[str, str]:
    """Generate a mapping between transcript files and their matching video files.
    
    Scans directories containing transcripts and videos to create a dictionary
    mapping each transcript file to its corresponding video file, based on
    filename similarities.
    
    Args:
        transcript_dir (str): Directory containing transcript files
        video_dir (str): Directory containing video files
        
    Returns:
        Dict[str, str]: Dictionary with transcript paths as keys and matching
            video paths as values. Only includes transcripts with matching videos.
            
    Example:
        >>> mapping = generate_transcript_video_mapping("/transcripts", "/videos")
        >>> print(mapping)
        {'/transcripts/lecture1.txt': '/videos/lecture1.mp4', 
         '/transcripts/lecture2.txt': '/videos/lecture2.mp4'}
    """
    mapping = {}
    transcript_path = Path(transcript_dir)
    
    # Find all potential transcript files
    transcript_extensions = ['.txt', '.srt', '.vtt']
    transcript_files = []
    for ext in transcript_extensions:
        transcript_files.extend(list(transcript_path.glob(f"**/*{ext}")))
    
    # For each transcript, try to find a matching video
    for transcript in transcript_files:
        video = find_matching_video(str(transcript), video_dir)
        if video:
            mapping[str(transcript)] = video
    
    return mapping
