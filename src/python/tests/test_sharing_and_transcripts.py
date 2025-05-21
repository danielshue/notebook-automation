#!/usr/bin/env python3
"""
Test OneDrive Sharing and Transcript Stage 2 Processing

This script tests the OneDrive sharing functionality and the Stage 2 transcript processing.
It's useful for verifying that the sharing links can be generated and that the transcript
processing works as expected before running the full pipeline.

Usage:
    python test_sharing_and_transcripts.py [--onedrive-path PATH] [--transcript-path PATH]
"""

import os
import sys
import argparse
import logging
from pathlib import Path

# Import from tools package
from tools.utils.config import setup_logging
from tools.onedrive.sharing import create_sharing_link
from tools.auth.graph_auth import authenticate_graph_api

# Set up logging
logger, failed_logger = setup_logging()

def test_authentication():
    """Test authentication with Microsoft Graph API"""
    print("Testing Microsoft Graph API authentication...")
    token = authenticate_graph_api()
    if token:
        print("✅ Authentication successful")
        return True
    else:
        print("❌ Authentication failed")
        return False

def test_sharing(file_path):
    """Test creating a sharing link for a file"""
    print(f"Testing sharing link creation for: {file_path}")
    
    # Check if the file exists locally before trying to share
    local_path = Path(file_path)
    if not local_path.exists():
        print(f"❌ File does not exist locally: {local_path}")
        return False
    
    # Try to create a sharing link
    link = create_sharing_link(file_path)
    if link:
        print(f"✅ Sharing link created: {link}")
        return True
    else:
        print("❌ Failed to create sharing link")
        print("  This could be because:")
        print("  - The file path is not correctly formatted for OneDrive")
        print("  - The file doesn't exist in OneDrive")
        print("  - Authentication issues with Microsoft Graph API")
        return False

def test_transcript_processing(transcript_path, video_path=None):
    """Test transcript processing functionality"""
    print(f"Testing transcript processing for: {transcript_path}")
    
    try:
        # First check if the transcript file exists
        transcript = Path(transcript_path)
        if not transcript.exists():
            print(f"❌ Transcript file not found: {transcript_path}")
            return False
        
        # Read the transcript file
        with open(transcript, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Check if it has YAML frontmatter
        has_yaml = content.startswith('---')
        print(f"  - Has YAML frontmatter: {'✅' if has_yaml else '❌'}")
        
        # If we have a video path, validate it exists
        if video_path:
            video = Path(video_path)
            print(f"  - Video exists: {'✅' if video.exists() else '❌'}")
            
            # Check if transcripts can be properly paired with videos
            from tools.transcript.processor import find_transcript_file
            matching_transcript = find_transcript_file(video, Path(transcript_path).parent.parent)
            if matching_transcript:
                print(f"  - Transcript found for video: ✅")
                print(f"    Found: {matching_transcript}")
            else:
                print(f"  - Transcript found for video: ❌")
        
        print("✅ Transcript processing tests complete")
        return True
    except Exception as e:
        print(f"❌ Error during transcript processing tests: {e}")
        return False

def main():
    parser = argparse.ArgumentParser(description="Test OneDrive sharing and transcript processing.")
    parser.add_argument("--onedrive-path", help="Path to a test file in OneDrive")
    parser.add_argument("--transcript-path", help="Path to a test transcript file")
    parser.add_argument("--verbose", "-v", action="store_true", help="Enable verbose logging")
    args = parser.parse_args()

    print("=== Testing OneDrive Sharing and Transcript Processing ===\n")
    
    # Configure logging level based on args
    if args.verbose:
        logger.setLevel(logging.DEBUG)
    
    # Test authentication
    if not test_authentication():
        print("Authentication failed. Exiting.")
        sys.exit(1)
    
    # Test sharing link creation if a path was provided
    if args.onedrive_path:
        test_sharing(args.onedrive_path)
    else:
        print("\nℹ️ No OneDrive path provided. Skipping sharing link test.")
        print("  To test sharing, run with: --onedrive-path PATH")
    
    # Test transcript processing if a path was provided
    if args.transcript_path:
        print()  # Add a blank line
        test_transcript_processing(args.transcript_path, args.onedrive_path)
    else:
        print("\nℹ️ No transcript path provided. Skipping transcript processing test.")
        print("  To test transcript processing, run with: --transcript-path PATH")
    
    print("\nTests completed.")

if __name__ == "__main__":
    main()
