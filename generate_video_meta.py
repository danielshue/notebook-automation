#!/usr/bin/env python3
"""
Video Metadata Generator for OneDrive Files

Generates shareable links for videos in OneDrive and creates
reference notes with AI-powered summaries in Obsidian vault.
"""

import argparse
import os
from pathlib import Path

from tools.utils.config import setup_logging
from tools.notes.video_markdown_generator import load_templates
from tools.auth.microsoft_auth import authenticate_graph_api

# Set up logging
logger, failed_logger = setup_logging()

def parse_arguments():
    """Parse command-line arguments."""
    parser = argparse.ArgumentParser(
        description="Generate shareable OneDrive links for videos and create reference notes in Obsidian vault."
    )
    
    # File or folder selection options (mutually exclusive)
    file_group = parser.add_mutually_exclusive_group()
    file_group.add_argument(
        "-f", "--single-file", 
        help="Process only a single file (path relative to OneDrive base path)"
    )
    
    file_group.add_argument(
        "--folder", 
        help="Process all video files in a directory (path relative to OneDrive base path)"
    )
    
    parser.add_argument(
        "--no-summary", 
        action="store_true",
        help="Disable OpenAI summary generation for transcripts"
    )
    
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Perform a dry run without creating actual links or notes"
    )
    
    parser.add_argument(
        "--debug",
        action="store_true",
        help="Enable debug logging"
    )
    
    parser.add_argument(
        "--retry-failed",
        action="store_true",
        help="Retry only previously failed files from failed_files.json"
    )
    
    parser.add_argument(
        "--force",
        action="store_true",
        help="Force processing of videos that already have notes (overwrite existing)"
    )
    
    parser.add_argument(
        "--timeout",
        type=int,
        default=15,
        help="Set API request timeout in seconds (default: 15)"
    )
    
    parser.add_argument(
        "--refresh-auth",
        action="store_true",
        help="Force refresh the Microsoft Graph API authentication cache (ignore cached tokens)"
    )
    
    parser.add_argument(
        "--no-share-links",
        action="store_true",
        help="Skip creating OneDrive shared links (faster for testing)"
    )
    
    return parser.parse_args()

def main():
    # Parse command-line arguments
    args = parse_arguments()
    
    # Configure logging level if debug flag is set
    if args.debug:
        logger.setLevel(logging.DEBUG)
        for handler in logger.handlers:
            handler.setLevel(logging.DEBUG)
        print("🐛 Debug logging enabled")
    
    # Load templates
    templates = load_templates()
    
    # Use the --refresh-auth flag to force refresh the token cache
    access_token = authenticate_graph_api(force_refresh=args.refresh_auth)
    headers = {"Authorization": f"Bearer {access_token}"}
    results_file = 'video_links_results.json'

    # Process specified files or directories
    if args.retry_failed:
        print("🔄 Retrying previously failed files...")
        from tools import retry_failed_files
        retry_failed_files(headers, templates, results_file)
    else:
        # Determine if the input is a file or directory
        input_path = args.single_file or args.folder
        
        if input_path:
            print(f"🔍 Processing specified path: {input_path}")
            # If path is an OneDrive path that starts with slash, handle it directly
            from tools import process_path
            processed, errors = process_path(input_path, headers, templates, results_file, args)
            print(f"✅ Processed {len(processed)} videos, ❌ {len(errors)} errors.")
        else:
            # Fallback to original behavior if no path is provided
            print("📂 Processing all videos in OneDrive...")
            from tools import process_videos_for_sharing
            process_videos_for_sharing(args)

if __name__ == "__main__":
    import logging
    main()
