#!/usr/bin/env python3
"""
Markdown Converter CLI for MBA Notebook Automation

This module provides a command-line interface for converting various file formats
to markdown, with support for HTML and text files.

**Note:** Files that have a corresponding `.mp4` file (with the same base name) in the same directory
will be skipped and not converted. This is to allow a separate process to handle those files.

Examples:
    vault-convert-markdown file.html                # Convert single file
    vault-convert-markdown --src-dir ./notes        # Convert all files in directory
    vault-convert-markdown --dry-run file.txt       # Preview conversion
    vault-convert-markdown --verbose *.html         # Show detailed progress
"""
import re
import os
import sys
import glob
import argparse
from pathlib import Path
from typing import List, Optional, Tuple

from ..utils.converters import convert_html_to_markdown, convert_txt_to_markdown

# Import needed for config display
from notebook_automation.cli.utils import OKCYAN, ENDC


def process_file(
    src_file: str,
    dest_file: str,
    dry_run: bool = False,
    verbose: bool = False
) -> Tuple[bool, Optional[str]]:
    """Process a single file for conversion to markdown.
    
    Detects the file type based on extension and applies the appropriate conversion
    method. Currently supports HTML and text files. Creates the output directory
    if it doesn't exist and handles various error conditions.
    
    Args:
        src_file (str): Path to the source file to be converted
        dest_file (str): Path where to save the converted markdown file
        dry_run (bool): If True, don't write any files, just simulate. Defaults to False.
        verbose (bool): If True, show detailed progress information. Defaults to False.
        
    Returns:
        Tuple[bool, Optional[str]]: A tuple containing:
            - bool: Success status (True if conversion succeeded)
            - Optional[str]: Error message if an error occurred, None otherwise
            
    Example:
        >>> success, error = process_file("document.html", "document.md")
        >>> if success:
        ...     print("Conversion successful")
        ... else:
        ...     print(f"Conversion failed: {error}")
        Conversion successful
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


def has_corresponding_mp4(file_path: Path) -> bool:
    """
    Return True if a corresponding .mp4 file exists for a given file, handling language codes and multi-dot names.
    Handles files like foo.en.txt, foo.en.srt, foo.zh-cn.txt, foo.txt, foo.srt, etc.
    """
    name = file_path.name
    # Try to match language code pattern (2-5 lowercase letters, optionally hyphenated, possibly multiple dots)
    # Examples: foo.en.txt, foo.en.srt, foo.zh-cn.txt, foo.pt-br.srt, foo.txt, foo.srt
    # We want to check for foo.mp4
    # Remove all extensions until the first non-language extension
    parts = name.split('.')
    if len(parts) >= 3 and parts[-1] in {'txt', 'srt'}:
        lang_part = parts[-2]
        if re.fullmatch(r'[a-z]{2,5}(-[a-z]{2,5})?', lang_part, re.IGNORECASE):
            base = '.'.join(parts[:-2])
            mp4_file = file_path.with_name(base + '.mp4')
        else:
            base = '.'.join(parts[:-1])
            mp4_file = file_path.with_name(base + '.mp4')
    else:
        mp4_file = file_path.with_suffix('.mp4')
    # Debug output for troubleshooting
    if os.environ.get('CONVERT_DEBUG') == '1':
        print(f"[DEBUG] Checking for mp4: {mp4_file} (exists: {mp4_file.exists()}) for source: {file_path}")
    return mp4_file.exists()

def process_files(
    src_paths: List[str],
    dest_dir: Optional[str] = None,
    dry_run: bool = False,
    verbose: bool = False
) -> Tuple[int, int]:
    """Process multiple files or directories for conversion.
    
    Handles a list of source paths which could be files, directories, or glob patterns.
    For each path, identifies the appropriate files to convert and processes them
    with the process_file function. Tracks and returns statistics about the conversion
    process.
    
    Args:
        src_paths (List[str]): List of source files, directories, or glob patterns
        dest_dir (Optional[str]): Optional destination directory for converted files.
            If None, converted files are placed alongside source files.
        dry_run (bool): If True, don't write any files, just simulate. Defaults to False.
        verbose (bool): If True, show detailed progress information. Defaults to False.
        
    Returns:
        Tuple[int, int]: A tuple containing:
            - int: Number of successful conversions
            - int: Number of failed conversions
            
    Example:
        >>> success, failed = process_files(["docs/*.html", "notes.txt"], "markdown_output")
        >>> print(f"Converted {success} files successfully, {failed} files failed")
        Converted 5 files successfully, 0 files failed
    """
    success_count = 0
    fail_count = 0
    skipped_due_to_mp4 = 0

    for src_path in src_paths:
        # Handle glob patterns
        if '*' in src_path or '?' in src_path:
            files = glob.glob(src_path)
        else:
            files = [src_path]
            
        for src_file in files:
            src_file = os.path.abspath(src_file)

            if os.path.isfile(src_file):
                file_path = Path(src_file)
                # Only process .html and .txt files
                ext = file_path.suffix.lower()
                if ext not in {'.html', '.txt'}:
                    if verbose:
                        print(f"⏭️  Skipping {src_file} (unsupported file type)")
                    continue
                skip = has_corresponding_mp4(file_path)
                if os.environ.get('CONVERT_DEBUG') == '1':
                    print(f"[DEBUG] {src_file}: skip={skip}")
                if skip:
                    skipped_due_to_mp4 += 1
                    if verbose:
                        print(f"⏭️  Skipping {src_file} (corresponding .mp4 exists)")
                    continue
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
                        file_path = Path(file)
                        ext_lc = file_path.suffix.lower()
                        if ext_lc not in {'.html', '.txt'}:
                            if verbose:
                                print(f"⏭️  Skipping {file} (unsupported file type)")
                            continue
                        if has_corresponding_mp4(file_path):
                            skipped_due_to_mp4 += 1
                            if verbose:
                                print(f"⏭️  Skipping {file} (corresponding .mp4 exists)")
                            continue
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

    return success_count, fail_count, skipped_due_to_mp4


def main() -> None:
    """Main entry point for the markdown conversion CLI tool.
    
    Parses command line arguments, collects source files to process (either individual
    files or all supported files in a directory), and performs the conversions.
    Provides summary statistics about the conversion process.
    
    Args:
        None
        
    Returns:
        None: This function doesn't return a value
        
    Example:
        When called from the command line:
        $ vault-convert-markdown --src-dir ./documents --verbose
    """
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
    parser.add_argument('-c', '--config', type=str, default=None,
                       help='Path to config.json file')
    
    args = parser.parse_args()
    
    # Set config path if provided
    if args.config:
        # Use absolute path to ensure consistency
        config_path = str(Path(args.config).absolute())
        os.environ["NOTEBOOK_CONFIG_PATH"] = config_path

    # Display which config.json file is being used
    try:
        from notebook_automation.tools.utils.config import find_config_path
        config_path = os.environ.get("NOTEBOOK_CONFIG_PATH") or find_config_path()
        print(f"{OKCYAN}Using configuration file: {config_path}{ENDC}")
    except Exception as e:
        print(f"Could not determine config file path: {e}")
    
    # Collect source paths
    src_paths = list(args.files)
    if args.src_dir:
        src_paths.append(args.src_dir)
        
    if not src_paths:
        parser.print_help()
        sys.exit(1)
        
    # Process files
    success_count, fail_count, skipped_due_to_mp4 = process_files(
        src_paths,
        args.dest_dir,
        args.dry_run,
        args.verbose
    )

    # Print summary
    total = success_count + fail_count + skipped_due_to_mp4
    print(f"\nConversion Summary:")
    print(f"  Total files considered: {total}")
    print(f"  Successfully converted: {success_count}")
    if skipped_due_to_mp4:
        print(f"  Skipped due to .mp4 present: {skipped_due_to_mp4}")
    if fail_count:
        print(f"  Failed to convert: {fail_count}")

    sys.exit(1 if fail_count else 0)


if __name__ == "__main__":
    main()
