#!/usr/bin/env python3
"""
Transcript Processing Module for Educational Video Transcripts

This module provides comprehensive utilities for handling educational video transcripts
within the MBA Notebook Automation system. It includes sophisticated transcript file 
discovery algorithms and intelligent content cleaning to transform raw transcripts into 
high-quality input for AI summarization.

Key Features:
------------
1. Intelligent Transcript Discovery
   - Multi-location search across vault and OneDrive locations
   - Support for diverse naming conventions and patterns
   - Progressive fallback strategies with increasing flexibility
   - Detailed logging for transparency and troubleshooting

2. Advanced Content Cleaning
   - Removal of structural elements like frontmatter and headers
   - Elimination of timestamps in multiple formats
   - Speaker annotation removal with context awareness
   - Markdown formatting normalization
   - Filtering of non-informative content and filler words

3. Error Resilience
   - Multi-encoding support for file reading
   - Graceful degradation through fallback strategies
   - Detailed error logging and diagnostic information
   - Content quality validation with warnings for problematic transcripts

Integration Points:
-----------------
- Works with video metadata extraction pipeline
- Feeds into AI summarization system
- Supports both OneDrive and local vault file structures
- Handles diverse transcript formats from different recording systems

Usage Example:
------------
```python
from pathlib import Path
from tools.transcript.processor import find_transcript_file, get_transcript_content

# Define paths for the video and vault
video_path = "/path/to/lecture_video.mp4"
vault_root = Path("/path/to/obsidian_vault")

# Find the transcript file
transcript_path = find_transcript_file(video_path, vault_root)

if transcript_path:
    # Extract and clean the transcript content
    cleaned_content = get_transcript_content(transcript_path)
    
    if cleaned_content:
        # Use the cleaned content for further processing
        # For example, pass to AI summarization
        from tools.ai.summarizer import generate_summary
        summary = generate_summary(cleaned_content)
        print(f"Successfully processed transcript: {transcript_path.name}")
    else:
        print(f"Failed to extract content from transcript: {transcript_path}")
else:
    print(f"No transcript found for video: {video_path}")
```
"""

import re
from pathlib import Path

from ..utils.config import ONEDRIVE_LOCAL_RESOURCES_ROOT, logger

# Use the logger from config module instead of creating a new one

def find_transcript_file(video_path, vault_root):
    """
    Find a transcript file that corresponds to a video file using multi-stage search.
    
    This function implements a comprehensive, multi-stage search algorithm for
    locating transcript files associated with educational videos. It employs
    a progressive strategy that starts with strict matching and gradually
    relaxes constraints until a suitable transcript is found or all options
    are exhausted.
    
    Search Strategy (in order of execution):
    1. Direct extension matching (.txt) in the same directory
    2. Pattern matching in the vault directory corresponding to the video
    3. Pattern matching in the parent directory of the vault directory
    4. Pattern matching in the OneDrive directory where the original video is located
    5. Pattern matching in standard transcript subdirectories
    6. Advanced partial name matching with suffix removal
    7. Last resort: Using the only .txt file in the directory if just one exists
    
    The function supports over 18 different naming patterns for transcript files,
    accommodating various naming conventions used in educational content.
    
    Args:
        video_path (str or Path): Path to the video file for which to find a transcript.
                                 Can be an absolute path or relative path.
        vault_root (Path): Root directory of the Obsidian vault where the processed
                          notes will be stored.
        
    Returns:
        Path: A Path object pointing to the transcript file if found.
              None if no suitable transcript file could be located after
              exhausting all search strategies.
              
    Logging:
        Logs detailed information about the search process at various levels:
        - INFO: Major steps and results of the search
        - DEBUG: Detailed information about directories searched and patterns tried
        
    Note:
        The function prioritizes accuracy (finding the correct transcript) over
        performance, as transcript discovery is typically a one-time operation
        per video in the processing pipeline.
    """
    if isinstance(video_path, str):
        video_path = Path(video_path)
    
    # Print video path info for debugging
    logger.info(f"Searching for transcript file for video: {video_path}")
    logger.info(f"Video parent directory: {video_path.parent}")
    
    # First check: Direct exact match in same directory (same name, different extension)
    direct_txt_match = video_path.with_suffix('.txt')
    if direct_txt_match.exists():
        logger.info(f"Found exact matching transcript file: {direct_txt_match}")
        return direct_txt_match
    
    # Generate possible transcript file names with a wider range of patterns
    # These patterns cover the most common naming conventions for transcript files
    # in educational contexts, including various separator styles and terminology
    video_name = video_path.stem
    
    # Log the base video name for debugging purposes
    logger.info(f"Video base name: {video_name}")
    
    # Comprehensive pattern list for transcript file detection
    # This extensive list helps handle diverse naming conventions from
    # different content providers and transcription services
    transcript_name_patterns = [
        f"{video_name}.txt",                       # Simple extension change
        f"{video_name}.md",                        # Markdown variant
        f"{video_name} Transcript.md",             # Space separator
        f"{video_name}-Transcript.md",             # Hyphen separator
        f"{video_name} transcript.md",             # Lowercase variant
        f"{video_name}-transcript.md",             # Hyphen + lowercase
        f"{video_name}_Transcript.md",             # Underscore separator
        f"{video_name}_transcript.md",             # Underscore + lowercase
        f"{video_name} Transcript.txt",            # TXT variants of the same patterns
        f"{video_name}-Transcript.txt",
        f"{video_name} transcript.txt",
        f"{video_name}-transcript.txt",
        f"{video_name}_Transcript.txt",
        f"{video_name}_transcript.txt",
        # Special handling for live session videos with standardized patterns
        video_name.replace('-Live-Session', ' Live Session Transcript') + ".md",
        video_name.replace('-Live-Session', ' Live Session Transcript') + ".txt",
        # Additional variants with dash separator
        f"{video_name} - Transcript.md",
        f"{video_name} - Transcript.txt"
    ]
    
    logger.debug(f"Looking for transcript for video: {video_name}")
    logger.debug(f"Searching with patterns: {', '.join(transcript_name_patterns[:5])}...")
    
    # 1. First, check if transcript exists in the same directory as where the video reference would be in the vault
    try:
        # Determine vault directory from video path
        rel_path = video_path.relative_to(ONEDRIVE_LOCAL_RESOURCES_ROOT)
        vault_dir = vault_root / rel_path.parent
        logger.debug(f"Checking vault directory: {vault_dir}")
        
        if vault_dir.exists():
            for pattern in transcript_name_patterns:
                transcript_path = vault_dir / pattern
                if transcript_path.exists():
                    logger.info(f"Found transcript file in vault directory: {transcript_path}")
                    return transcript_path
    except ValueError:
        logger.debug(f"Video path not relative to RESOURCES_ROOT: {video_path}")
    
    # 2. Check parent directory in vault
    parent_dir = vault_dir.parent
    if parent_dir.exists():
        logger.debug(f"Checking vault parent directory: {parent_dir}")
        for pattern in transcript_name_patterns:
            transcript_path = parent_dir / pattern
            if transcript_path.exists():
                logger.info(f"Found transcript file in vault parent directory: {transcript_path}")
                return transcript_path
    
    # 3. Check in the OneDrive source directory where the video is located
    try:
        onedrive_dir = video_path.parent
        if onedrive_dir.exists():
            logger.debug(f"Searching OneDrive source directory: {onedrive_dir}")
            
            # First list all txt files in the directory for debugging
            txt_files = list(onedrive_dir.glob("*.txt"))
            logger.info(f"Available TXT files in directory ({len(txt_files)}): {', '.join([f.name for f in txt_files])}")
            
            # Check each pattern systematically
            for pattern in transcript_name_patterns:
                transcript_path = onedrive_dir / pattern
                logger.debug(f"Checking for: {transcript_path}")
                if transcript_path.exists():
                    logger.info(f"Found transcript file in OneDrive directory: {transcript_path}")
                    return transcript_path
            
            # If no match found with standard patterns, try a more direct approach
            video_stem_lower = video_name.lower()
            
            for txt_file in txt_files:
                # Check if the video name is contained within the txt file name
                if video_stem_lower in txt_file.stem.lower():
                    logger.info(f"Found potential transcript by name match: {txt_file}")
                    return txt_file
    except Exception as e:
        logger.debug(f"Error checking OneDrive directory: {e}")
    
    # 4. Check in common transcript subdirectories
    common_transcript_dirs = ["Transcripts", "Transcript", "transcripts", "transcript"]
    
    # Check in vault directory's potential transcript subdirectories
    for subdir in common_transcript_dirs:
        transcript_subdir = vault_dir / subdir
        if transcript_subdir.exists():
            logger.debug(f"Searching vault transcript subdirectory: {transcript_subdir}")
            for pattern in transcript_name_patterns:
                transcript_path = transcript_subdir / pattern
                if transcript_path.exists():
                    logger.info(f"Found transcript file in transcript subdirectory: {transcript_path}")
                    return transcript_path
    
    # Check in OneDrive directory's potential transcript subdirectories
    try:
        for subdir in common_transcript_dirs:
            transcript_dir = video_path.parent / subdir
            if transcript_dir.exists():
                logger.debug(f"Searching OneDrive transcript subdirectory: {transcript_dir}")
                for pattern in transcript_name_patterns:
                    transcript_path = transcript_dir / pattern
                    if transcript_path.exists():
                        logger.info(f"Found transcript file in OneDrive transcript subdirectory: {transcript_path}")
                        return transcript_path
    except Exception as e:
        logger.debug(f"Error checking OneDrive transcript directories: {e}")
    
    # 5. More sophisticated partial name matching 
    try:
        onedrive_dir = video_path.parent
        if onedrive_dir.exists():
            logger.debug(f"Trying advanced partial name matches in: {onedrive_dir}")
            
            # Get all txt files in the directory
            txt_files = list(onedrive_dir.glob("*.txt"))
            
            # Try different matching strategies
            matched_file = None
            
            # Strategy 1: Match by removing number suffixes
            if not matched_file:
                base_name_match = re.match(r'(.+?)[-_\s]*\d+$', video_name.lower())
                if base_name_match:
                    base_name = base_name_match.group(1)
                    logger.debug(f"Extracted base name without numeric suffix: {base_name}")
                    
                    for file in txt_files:
                        if base_name in file.stem.lower():
                            logger.info(f"Found potential transcript via base name match: {file}")
                            matched_file = file
                            break
            
            # Return the matched file if found
            if matched_file:
                return matched_file
            
    except Exception as e:
        logger.debug(f"Error during advanced partial name matching: {e}")
    
    # 6. Final attempt - if all else fails and there's just one text file, use it
    try:
        onedrive_dir = video_path.parent
        if onedrive_dir.exists():
            txt_files = list(onedrive_dir.glob("*.txt"))
            if len(txt_files) == 1:
                logger.info(f"Last resort: Using the only text file in directory: {txt_files[0]}")
                return txt_files[0]
    except Exception as e:
        logger.debug(f"Error in final attempt: {e}")
    
    # If no transcript file found after all these attempts
    logger.info(f"No transcript file found for {video_name} after exhaustive search")
    return None

def get_transcript_content(transcript_path):
    """
    Extract and clean content from a transcript file for optimal AI summarization.
    
    This function implements a sophisticated multi-step cleaning pipeline that
    transforms raw transcript files into clean, well-formatted text optimized
    for downstream natural language processing and AI summarization. The cleaning
    process addresses numerous issues common in educational transcripts,
    particularly those from auto-generated sources.
    
    Cleaning Pipeline Stages:
    1. Structural Element Removal
       - YAML frontmatter elimination
       - Transcript headers and footers removal
       
    2. Temporal Annotation Cleaning
       - Multiple timestamp formats detection and removal
       - Time code normalization
       
    3. Speaker Annotation Processing
       - Named speaker label removal
       - Generic speaker role label removal
       
    4. Formatting Normalization
       - Markdown element removal
       - Emphasis and formatting tag cleanup
       
    5. Content Quality Enhancement
       - Filler word removal
       - Non-informative line filtering
       - Punctuation and spacing standardization
       
    Handles these transcript formats:
    - Markdown files with frontmatter
    - Plain text files with various encodings
    - Auto-generated transcripts with timestamps
    - Human-edited transcripts with speaker annotations
    - Multi-speaker interview/panel transcripts
    - AI-generated transcripts with common artifacts
    
    Args:
        transcript_path (Path): Path to the transcript file to process
        
    Returns:
        str: Clean, well-formatted transcript text optimized for AI processing.
             Returns None if file reading or processing fails critically.
             
    Quality Control:
        The function performs validation of the cleaning results, including:
        - Size comparison between original and cleaned content
        - Warning generation for potentially over-aggressive cleaning
        - Detailed logging of the cleaning process and outcomes
    """
    try:
        # Attempt to read the file with different encodings
        # This handling is essential as transcripts come from various sources
        # with inconsistent encoding practices
        content = None
        encodings = ['utf-8', 'latin-1', 'cp1252', 'ascii']
        
        # Try each encoding in order of likelihood until successful
        for encoding in encodings:
            try:
                with open(transcript_path, 'r', encoding=encoding) as f:
                    content = f.read()
                    logger.debug(f"Successfully read file with {encoding} encoding")
                    break
            except Exception as e:
                logger.error(f"Error reading transcript file with {encoding}: {e}")
                # Continue to the next encoding option
        
        # If all encoding attempts fail, give up
        if not content:
            logger.error(f"Could not read transcript file with any encoding: {transcript_path}")
            return None
            
        # Log original content size for comparison after cleaning
        original_size = len(content)
        logger.debug(f"Original transcript content size: {original_size} characters")
        
        # STEP 1: Remove structural elements
        
        # Remove YAML frontmatter if present (common in Markdown transcripts)
        # This pattern matches content between --- delimiters at the start of the file
        content = re.sub(r'^---\s.*?---\s', '', content, flags=re.DOTALL)
        
        # Remove common transcript headers that contain metadata rather than content
        # These headers often include the word "Transcript" in various formats
        content = re.sub(r'^(Transcript|TRANSCRIPT|Video Transcript|AUTO-GENERATED TRANSCRIPT).*?\n+', '', content, flags=re.IGNORECASE)
        
        # Remove standard footer messages that indicate the end of the transcript
        # These don't contain actual transcript content and should be removed
        content = re.sub(r'\n+(End of|END OF|End)\s+(Transcript|TRANSCRIPT|Video).*?$', '', content, flags=re.IGNORECASE)
        
        # STEP 2: Clean up timestamps and speaker annotations
        
        # Remove common timestamp patterns from various transcript services
        # HH:MM:SS formats in brackets, parentheses, or at start of line
        content = re.sub(r'\[?\d{1,2}:\d{2}(:\d{2})?\]?(\s*->)?\s*', '', content)
        content = re.sub(r'\(\d{1,2}:\d{2}(:\d{2})?\)(\s*->)?\s*', '', content)
        content = re.sub(r'^(\d{1,2}:\d{2}(:\d{2})?)(\s|$)', '', content, flags=re.MULTILINE)  # Timestamps at line start
        
        # Remove speaker annotations from various transcript formats
        # First handle known role labels (Speaker 1, Instructor, etc.)
        content = re.sub(r'^\s*(Speaker\s*\d+|Instructor|Student|Moderator|Interviewer|Interviewee|Host|Guest|Professor):\s*', '', content, flags=re.MULTILINE)
        
        # Then remove more general speaker patterns (proper names followed by colons)
        # This pattern identifies capitalized names that might be speaker identifiers 
        content = re.sub(r'^\s*[A-Z][a-zA-Z\s\.\-]*:\s*', '', content, flags=re.MULTILINE)
        
        # STEP 3: Clean up markdown and formatting
        
        # Remove markdown headings (# Heading) which often separate transcript sections
        # but aren't part of the actual spoken content
        content = re.sub(r'#+\s.*?\n', '', content)
        
        # Remove formatting marks while preserving the text they surround
        # Bold (**text**), italic (*text*), and underline (_text_) formatting
        content = re.sub(r'\*\*(.*?)\*\*', r'\1', content)  # Remove bold formatting
        content = re.sub(r'\*(.*?)\*', r'\1', content)      # Remove italic formatting
        content = re.sub(r'_{1,2}(.*?)_{1,2}', r'\1', content)  # Remove underline formatting
        
        # STEP 4: Handle auto-generated transcript issues
        
        # Remove filler words that don't add meaningful content but
        # are common in spoken language and auto-generated transcripts
        content = re.sub(r'(?:um|uh|er|ah|like)\s', ' ', content, flags=re.IGNORECASE)  # Remove filler words
        
        # Remove lines that are likely transcript errors or non-informative
        lines = content.split('\n')
        filtered_lines = []
        for line in lines:
            # Skip very short lines (often just noise in transcripts)
            # Lines under 3 characters rarely contain meaningful content
            if len(line.strip()) < 3:
                continue
                
            # Skip lines that are just punctuation or single words
            # These are often errors or orphaned fragments in auto-generated transcripts
            if len(line.strip().split()) < 2 and not re.search(r'[a-zA-Z]{3,}', line):
                continue
                
            # Add the line if it passes all filters
            filtered_lines.append(line)
        
        # Rebuild content from filtered lines
        content = '\n'.join(filtered_lines)
        
        # STEP 5: Final cleanup and whitespace handling
        
        # Normalize spacing throughout the document for consistency
        # Replace multiple spaces with a single space
        content = re.sub(r'\s{2,}', ' ', content)
        
        # Normalize paragraph breaks to make content more readable
        # Replace 3+ newlines with just 2 (standard paragraph break)
        content = re.sub(r'\n{3,}', '\n\n', content)
        
        # Fix common punctuation issues in transcripts
        # Ensure proper space after sentence endings for readability
        content = re.sub(r'([.!?])\s*([a-zA-Z])', r'\1 \2', content)
        
        # Remove erroneous spaces before punctuation marks
        content = re.sub(r'\s([.,;:!?])', r'\1', content)
        
        # Log cleaned content size and calculate cleanup efficiency metrics
        cleaned_content = content.strip()
        cleaned_size = len(cleaned_content)
        logger.info(f"Cleaned transcript: {cleaned_size} characters ({cleaned_size/original_size:.1%} of original)")
        
        # Quality control check: if too much content was removed, it might indicate
        # overly aggressive cleaning or a problematic transcript format
        # The 0.3 threshold (70% reduction) is based on empirical testing with
        # various transcript formats
        if cleaned_size < original_size * 0.3 and original_size > 1000:
            logger.warning(f"Cleaning removed over 70% of transcript content. Check transcript quality.")
            # We still return the cleaned content despite the warning, as it may still be usable
        
        return cleaned_content
    except Exception as e:
        logger.error(f"Error processing transcript file {transcript_path}: {e}")
        return None
