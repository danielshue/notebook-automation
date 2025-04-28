#!/usr/bin/env python3
"""
Standalone transcript finder script for debugging.
This script tests the transcript finding functionality without running the entire process.
"""

import os
import sys
import logging
import re
from pathlib import Path
import argparse
from tools.utils.config import logger
from tools.utils.config import RESOURCES_ROOT, VAULT_ROOT, ONEDRIVE_BASE


def find_transcript_file(video_path):
    """
    Find a transcript file that corresponds to a video file.
    Standalone version for debugging.
    """
    if isinstance(video_path, str):
        video_path = Path(video_path)
    
    print(f"\n{'='*80}")
    print(f"TRANSCRIPT FINDER DEBUG")
    print(f"{'='*80}")
    print(f"Video path: {video_path}")
    print(f"Video exists: {video_path.exists()}")
    print(f"Video parent: {video_path.parent}")
    print(f"Video parent exists: {video_path.parent.exists()}")
    print(f"{'='*80}")
    
    # First check: Direct exact match in same directory (same name, different extension)
    direct_txt_match = video_path.with_suffix('.txt')
    print(f"Checking direct match: {direct_txt_match}")
    print(f"Direct match exists: {direct_txt_match.exists()}")
    
    if direct_txt_match.exists():
        print(f"FOUND TRANSCRIPT: Direct match at {direct_txt_match}")
        return direct_txt_match
    
    # Generate possible transcript file names with a wider range of patterns
    video_name = video_path.stem
    print(f"Video stem name: {video_name}")
    
    # Add more patterns to make transcript detection more robust
    transcript_name_patterns = [
        # Standard formats - most common first
        f"{video_name}.txt",
        f"{video_name}.md",
        # Additional formats
        f"{video_name} Transcript.md",
        f"{video_name}-Transcript.md",
        f"{video_name} transcript.md",
        f"{video_name}-transcript.md",
        f"{video_name}_Transcript.md",
        f"{video_name}_transcript.md",
        
        # Common variations
        f"{video_name} Transcript.txt",
        f"{video_name}-Transcript.txt",
        f"{video_name} transcript.txt",
        f"{video_name}-transcript.txt",
        f"{video_name}_Transcript.txt",
        f"{video_name}_transcript.txt",
    ]
    
    # List files in the directory
    onedrive_dir = video_path.parent
    if onedrive_dir.exists():
        print(f"\nListing files in directory: {onedrive_dir}")
        files = list(onedrive_dir.iterdir())
        for file in files:
            print(f"  - {file.name}")
        
        # List only txt files
        txt_files = list(onedrive_dir.glob("*.txt"))
        print(f"\nTXT files in directory ({len(txt_files)}):")
        for file in txt_files:
            print(f"  - {file.name}")
        
        # Check each pattern systematically
        print(f"\nChecking patterns:")
        for pattern in transcript_name_patterns:
            transcript_path = onedrive_dir / pattern
            print(f"  - Checking: {transcript_path.name} (exists: {transcript_path.exists()})")
            if transcript_path.exists():
                print(f"\nFOUND TRANSCRIPT: Pattern match at {transcript_path}")
                return transcript_path
        
        # If no match found with standard patterns, use a more direct approach
        # If there's exactly one .txt file in the folder, it's likely the transcript
        if len(txt_files) == 1:
            print(f"\nFOUND TRANSCRIPT: Single TXT file in directory: {txt_files[0]}")
            return txt_files[0]
    
    print(f"\nNo transcript file found for {video_name}")
    return None

def main():
    parser = argparse.ArgumentParser(description="Test transcript finding for a video file")
    parser.add_argument("video_path", help="Path to the video file (relative to OneDrive base)")
    args = parser.parse_args()
    
    # Normalize path
    video_path = args.video_path.strip("/")
    
    # Build full path
    if not video_path.startswith("/"):
        full_path = RESOURCES_ROOT / video_path
    else:
        full_path = Path(video_path)
    
    # Find transcript
    transcript = find_transcript_file(full_path)
    
    if transcript:
        print(f"\nTRANSCRIPT FOUND: {transcript}")
        print(f"File exists: {transcript.exists()}")
        if transcript.exists():
            try:
                size_kb = transcript.stat().st_size / 1024
                print(f"File size: {size_kb:.2f} KB")
                
                # Try to read the first few lines
                try:
                    with open(transcript, 'r', encoding='utf-8', errors='ignore') as f:
                        first_lines = []
                        for i in range(5):
                            line = f.readline().strip()
                            if line:
                                first_lines.append(line)
                            if i >= 4:
                                break
                    
                    print(f"\nFirst lines of transcript:")
                    for i, line in enumerate(first_lines):
                        print(f"{i+1}: {line[:80]}")
                except Exception as e:
                    print(f"Error reading file: {e}")
            except Exception as e:
                print(f"Error getting file stats: {e}")
    else:
        print("\nNo transcript found.")

if __name__ == "__main__":
    main()
