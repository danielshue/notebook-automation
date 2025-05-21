"""
Transcript Tools

This module provides tools for processing video and audio transcripts:
- Transcript cleaning and formatting
- Video-transcript mapping
- Transcript search and indexing
- Transcript metadata extraction
"""

from notebook_automation.tools.transcript.processor import (
    process_transcript,
    find_matching_video,
    extract_transcript_metadata,
    format_transcript_for_note
)