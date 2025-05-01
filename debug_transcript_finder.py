#!/usr/bin/env python3
"""
Debug script to test the transcript file finder functionality.

This script provides a simple way to test the transcript finder with a specific video
file to see if it can properly locate the corresponding transcript.
"""

import sys
import logging
from pathlib import Path

# Configure logging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.StreamHandler(),
        logging.FileHandler('transcript_finder_debug.log')
    ]
)
logger = logging.getLogger()

# Import the functionality we want to test
from tools.transcript.processor import find_transcript_file, get_transcript_content
from tools.utils.config import ONEDRIVE_LOCAL_RESOURCES_ROOT, VAULT_LOCAL_ROOT

def test_find_transcript(video_path):
    """
    Test finding a transcript for the specified video path.
    
    Args:
        video_path (str): Path to the video file
    """
    logger.info(f"Testing transcript finder with video: {video_path}")
    
    # Convert to Path object if string
    if isinstance(video_path, str):
        video_path = Path(video_path)
        
    logger.info(f"Video parent directory: {video_path.parent}")
    
    # Try to find the transcript using OneDrive root
    logger.info(f"Searching using OneDrive root: {ONEDRIVE_LOCAL_RESOURCES_ROOT}")
    transcript_path = find_transcript_file(video_path, ONEDRIVE_LOCAL_RESOURCES_ROOT)
    
    if transcript_path:
        logger.info(f"✅ Found transcript using OneDrive root: {transcript_path}")
        # Try to read the content
        content = get_transcript_content(transcript_path)
        content_preview = content[:200] + "..." if content else None
        logger.info(f"Transcript content preview: {content_preview}")
    else:
        logger.warning(f"❌ No transcript found using OneDrive root")
    
    # Try to find the transcript using Vault root
    logger.info(f"Searching using Vault root: {VAULT_LOCAL_ROOT}")
    transcript_path = find_transcript_file(video_path, VAULT_LOCAL_ROOT)
    
    if transcript_path:
        logger.info(f"✅ Found transcript using Vault root: {transcript_path}")
        # Try to read the content
        content = get_transcript_content(transcript_path)
        content_preview = content[:200] + "..." if content else None
        logger.info(f"Transcript content preview: {content_preview}")
    else:
        logger.warning(f"❌ No transcript found using Vault root")
        
    return transcript_path

def main():
    """Main function to run the transcript finder test."""
    if len(sys.argv) < 2:
        print("Usage: python debug_transcript_finder.py <path_to_video_file>")
        sys.exit(1)
    
    video_path = sys.argv[1]
    logger.info(f"Starting transcript finder debug for: {video_path}")
    
    transcript_path = test_find_transcript(video_path)
    
    if transcript_path:
        print(f"✅ Found transcript: {transcript_path}")
    else:
        print(f"❌ No transcript found for: {video_path}")
    
    print(f"Details logged to transcript_finder_debug.log")

if __name__ == "__main__":
    main()
