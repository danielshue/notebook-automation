#!/usr/bin/env python
"""
Markdown Generator from HTML and TXT - Part of the MBA Notebook Automation toolkit

This script converts HTML and plain text files into properly formatted markdown files
for the Obsidian vault. It preserves directory structure and handles conversion
of both file formats appropriately.

Features:
- Converts HTML to markdown while preserving content structure
- Handles plain text files with proper formatting
- Maintains directory hierarchy in output
- Supports dry run for testing
- Provides detailed output with verbose mode
- Integrates with Obsidian vault structure and settings

Usage:
    python generate_markdown.py [options]

Options:
    --src-dirs DIRS   Source directories to scan (default: current directory)
    --dest-dir DIR    Destination directory (default: Obsidian vault)
    --dry-run         Show what would be done without making changes
    --verbose, -v     Print detailed conversion information
    --debug          Enable debug logging
"""

import os
import sys
import re
import argparse
from pathlib import Path
import urllib.parse

# Import from tools package
from ..utils.converters import process_file as convert_to_markdown
from ..tools.utils.config import setup_logging, VAULT_LOCAL_ROOT
from ..tools.utils.paths import normalize_wsl_path

# Set up logging
logger, failed_logger = setup_logging(debug=False)

# Default patterns
NOTEBOOK_PATTERN = r'.*\.(html|txt)$'

def parse_args():
    """Parse command-line arguments for markdown generation."""
    parser = argparse.ArgumentParser(
        description='Generate Markdown files from HTML and TXT sources in Obsidian vault.'
    )
    parser.add_argument(
        '--src-dirs',
        nargs='+',
        help='List of source directories to scan for HTML and TXT files. Defaults to current directory.'
    )
    parser.add_argument(
        '--dest-dir',
        help='Destination directory for Markdown files. Defaults to the Obsidian vault.'
    )
    parser.add_argument(
        '--dry-run',
        action='store_true',
        help='Show what would be done without making changes'
    )
    parser.add_argument(
        '--verbose', '-v',
        action='store_true',
        help='Print detailed information about the conversion process'
    )
    parser.add_argument(
        '--debug',
        action='store_true',
        help='Enable debug logging'
    )
    return parser.parse_args()

def process_vault_file(src_file, dest_dir, src_dir=None, dry_run=False, verbose=False):
    """Process a single file for conversion and integration into the vault.
    
    Args:
        src_file (str): Path to the source file
        dest_dir (str): Path to the destination directory
        src_dir (str, optional): Base source directory for relative path calculation
        dry_run (bool): If True, don't write any files
        verbose (bool): If True, print detailed information
    
    Returns:
        bool: True if successful, False if there was an error
    """
    try:
        # Determine the relative path and create the corresponding destination path
        rel_path = os.path.relpath(src_file, start=src_dir if src_dir else os.path.dirname(src_file))
        dest_file = os.path.join(dest_dir, rel_path)
        dest_file = os.path.splitext(dest_file)[0] + '.md'  # Change extension to .md

        if verbose:
            logger.info(f"Processing file: {src_file}")
            logger.info(f"Destination: {dest_file}")

        # Use shared conversion utility
        success, error = convert_to_markdown(src_file, dest_file, dry_run=dry_run)
        
        if success:
            logger.info(f"{'[DRY RUN] Would convert' if dry_run else 'Converted'}: {src_file} -> {dest_file}")
        else:
            failed_logger.error(f"Error processing {src_file}: {error}")
            if verbose:
                logger.error(f"Error details: {error}")
                
        return success
        
    except Exception as e:
        failed_logger.error(f"Error processing file {src_file}: {str(e)}")
        if verbose:
            logger.error(f"Error details: {str(e)}")
        return False

def generate_markdown(src_dirs, dest_dir, dry_run=False, verbose=False):
    """Generate markdown files from HTML and TXT sources in the specified directories.
    
    Args:
        src_dirs (list): List of source directories to scan
        dest_dir (str): Destination directory for markdown files
        dry_run (bool): If True, don't write any files
        verbose (bool): If True, print detailed information
    
    Returns:
        tuple: (processed_count, success_count, error_count)
    """
    processed = 0
    success = 0
    errors = 0
    
    if verbose:
        logger.info("Starting markdown generation")
        logger.info(f"Source directories: {', '.join(src_dirs)}")
        logger.info(f"Destination directory: {dest_dir}")
        logger.info(f"{'[DRY RUN] ' if dry_run else ''}Processing files...")

    for src_dir in src_dirs:
        for root, dirs, files in os.walk(src_dir):
            for file in files:
                if re.match(NOTEBOOK_PATTERN, file):
                    processed += 1
                    src_file = os.path.join(root, file)
                    if process_vault_file(src_file, dest_dir, src_dir, dry_run, verbose):
                        success += 1
                    else:
                        errors += 1

    # Print summary
    logger.info("\nMarkdown Generation Summary:")
    logger.info(f"Files processed: {processed}")
    logger.info(f"Successfully converted: {success}")
    logger.info(f"Errors: {errors}")
    
    if dry_run:
        logger.info("\nThis was a dry run. No files were modified.")
        
    return processed, success, errors

def main():
    """Main entry point for the script."""
    args = parse_args()
    
    # Set up logging with debug if requested
    if args.debug:
        setup_logging(debug=True)
        
    # Determine source directories
    src_dirs = args.src_dirs or ['.']
    
    # Determine destination directory
    if args.dest_dir:
        dest_dir = args.dest_dir
    else:
        dest_dir = normalize_wsl_path(VAULT_LOCAL_ROOT)
        logger.info(f"No destination directory specified, using Obsidian vault: {dest_dir}")

    # Validate directories
    for src_dir in src_dirs:
        if not os.path.exists(src_dir):
            logger.error(f"Source directory not found: {src_dir}")
            return 1
            
    if not os.path.exists(dest_dir):
        if args.dry_run:
            logger.warning(f"Destination directory doesn't exist: {dest_dir}")
        else:
            try:
                os.makedirs(dest_dir, exist_ok=True)
            except Exception as e:
                logger.error(f"Could not create destination directory: {e}")
                return 1
    
    # Generate markdown files
    try:
        processed, success, errors = generate_markdown(
            src_dirs,
            dest_dir,
            dry_run=args.dry_run,
            verbose=args.verbose
        )
        return 0 if errors == 0 else 1
    except Exception as e:
        logger.error(f"Error during markdown generation: {e}")
        return 1

if __name__ == "__main__":
    main()
