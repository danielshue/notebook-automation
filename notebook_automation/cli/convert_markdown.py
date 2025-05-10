#!/usr/bin/env python3
"""
Markdown Converter CLI for MBA Notebook Automation

This module provides a command-line interface for converting various file formats
to markdown, with support for HTML and text files.

Examples:
    vault-convert-markdown file.html                # Convert single file
    vault-convert-markdown --src-dir ./notes        # Convert all files in directory
    vault-convert-markdown --dry-run file.txt       # Preview conversion
    vault-convert-markdown --verbose *.html         # Show detailed progress
"""

import os
import sys
import glob
import argparse
from pathlib import Path
from typing import List, Optional, Tuple

from ..utils.converters import convert_html_to_markdown, convert_txt_to_markdown


def process_file(
    src_file: str,
    dest_file: str,
    dry_run: bool = False,
    verbose: bool = False
) -> Tuple[bool, Optional[str]]:
    """Process a single file for conversion to markdown.
    
    Args:
        src_file: Path to the source file
        dest_file: Path where to save the converted file
        dry_run: If True, don't write any files
        verbose: If True, show detailed progress
        
    Returns:
        Tuple[bool, Optional[str]]: Success status and error message if any
    """
    try:
        if verbose:
            print(f"Processing: {src_file}")
            
        # Read the source file
        with open(src_file, 'r', encoding='utf-8') as f:
            content = f.read()
            
        # Convert based on file type
        if src_file.lower().endswith('.html'):
            converted_content = convert_html_to_markdown(content)
        elif src_file.lower().endswith('.txt'):
            converted_content = convert_txt_to_markdown(content, Path(src_file).name)
        else:
            return False, f"Unsupported file type: {src_file}"
            
        if dry_run:
            if verbose:
                print(f"Preview of {dest_file}:")
                print("=" * 40)
                print(converted_content[:500] + "..." if len(converted_content) > 500 else converted_content)
                print("=" * 40)
        else:
            # Create the destination directory if needed
            os.makedirs(os.path.dirname(dest_file), exist_ok=True)
            
            # Write the converted content
            with open(dest_file, 'w', encoding='utf-8') as f:
                f.write(converted_content)
                
            if verbose:
                print(f"✅ Created: {dest_file}")
                
        return True, None
        
    except Exception as e:
        return False, str(e)


def process_files(
    src_paths: List[str],
    dest_dir: Optional[str] = None,
    dry_run: bool = False,
    verbose: bool = False
) -> Tuple[int, int]:
    """Process multiple files or directories for conversion.
    
    Args:
        src_paths: List of source files or directories
        dest_dir: Optional destination directory
        dry_run: If True, don't write any files
        verbose: If True, show detailed progress
        
    Returns:
        Tuple[int, int]: Count of (successful, failed) conversions
    """
    success_count = 0
    fail_count = 0
    
    for src_path in src_paths:
        # Handle glob patterns
        if '*' in src_path or '?' in src_path:
            files = glob.glob(src_path)
        else:
            files = [src_path]
            
        for src_file in files:
            src_file = os.path.abspath(src_file)
            
            if os.path.isfile(src_file):
                # Determine destination path
                if dest_dir:
                    rel_path = os.path.relpath(src_file, os.path.dirname(src_file))
                    dest_file = os.path.join(dest_dir, rel_path)
                else:
                    dest_file = os.path.splitext(src_file)[0] + '.md'
                    
                # Process the file
                success, error = process_file(src_file, dest_file, dry_run, verbose)
                if success:
                    success_count += 1
                else:
                    fail_count += 1
                    if error and verbose:
                        print(f"❌ Error processing {src_file}: {error}")
                        
            elif os.path.isdir(src_file):
                # Process all HTML and TXT files in directory
                for ext in ['.html', '.txt']:
                    pattern = os.path.join(src_file, f'**/*{ext}')
                    for file in glob.glob(pattern, recursive=True):
                        rel_path = os.path.relpath(file, src_file)
                        if dest_dir:
                            dest_file = os.path.join(dest_dir, rel_path)
                        else:
                            dest_file = os.path.splitext(file)[0] + '.md'
                            
                        success, error = process_file(file, dest_file, dry_run, verbose)
                        if success:
                            success_count += 1
                        else:
                            fail_count += 1
                            if error and verbose:
                                print(f"❌ Error processing {file}: {error}")
                                
    return success_count, fail_count


def main():
    """Main entry point for the CLI tool."""
    parser = argparse.ArgumentParser(
        description="Convert HTML and text files to Markdown format",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
    vault-convert-markdown file.html                # Convert single file
    vault-convert-markdown --src-dir ./notes        # Convert all files in directory
    vault-convert-markdown --dry-run file.txt       # Preview conversion
    vault-convert-markdown --verbose *.html         # Show detailed progress
"""
    )
    
    parser.add_argument('files', nargs='*',
                       help='Files or directories to convert')
    parser.add_argument('--src-dir',
                       help='Source directory to scan for files')
    parser.add_argument('--dest-dir',
                       help='Destination directory for converted files')
    parser.add_argument('--dry-run', action='store_true',
                       help='Preview changes without writing files')
    parser.add_argument('--verbose', action='store_true',
                       help='Show detailed progress')
    
    args = parser.parse_args()
    
    # Collect source paths
    src_paths = list(args.files)
    if args.src_dir:
        src_paths.append(args.src_dir)
        
    if not src_paths:
        parser.print_help()
        sys.exit(1)
        
    # Process files
    success_count, fail_count = process_files(
        src_paths,
        args.dest_dir,
        args.dry_run,
        args.verbose
    )
    
    # Print summary
    total = success_count + fail_count
    print(f"\nConversion Summary:")
    print(f"  Total files processed: {total}")
    print(f"  Successfully converted: {success_count}")
    if fail_count:
        print(f"  Failed to convert: {fail_count}")
        
    sys.exit(1 if fail_count else 0)


if __name__ == "__main__":
    main()
