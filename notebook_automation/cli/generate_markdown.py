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
from colorlog import ColoredFormatter
from tqdm import tqdm

# Import from tools package
from ..utils.converters import process_file as convert_to_markdown
from ..tools.utils.config import setup_logging, VAULT_LOCAL_ROOT
from ..tools.utils.paths import normalize_wsl_path
from notebook_automation.cli.utils import OKCYAN, ENDC


# Enhanced logging setup with colorlog
def setup_colored_logging(debug: bool = False):
    """Set up colorized logging for CLI output."""
    import logging
    handler = logging.StreamHandler()
    formatter = ColoredFormatter(
        "%(log_color)s%(levelname)-8s%(reset)s %(message)s",
        log_colors={
            'DEBUG':    'cyan',
            'INFO':     'green',
            'WARNING':  'yellow',
            'ERROR':    'red',
            'CRITICAL': 'red,bg_white',
        }
    )
    handler.setFormatter(formatter)
    root_logger = logging.getLogger()
    for h in root_logger.handlers[:]:
        root_logger.removeHandler(h)
    root_logger.addHandler(handler)
    root_logger.setLevel(logging.DEBUG if debug else logging.INFO)
    return root_logger, root_logger  # Use same logger for both

logger, failed_logger = setup_colored_logging(debug=False)

# Default patterns
NOTEBOOK_PATTERN = r'.*\.(html|txt|epub)$'

def parse_args():
    """Parse command-line arguments for markdown generation."""
    parser = argparse.ArgumentParser(
        description='Generate Markdown files from HTML, EPUB, and TXT sources, mirroring the full OneDrive MBA-Resources structure in the Obsidian vault.'
    )
    parser.add_argument(
        '--src-dirs',
        nargs='+',
        help='List of source directories to scan for HTML, EPUB, and TXT files. Defaults to current directory.'
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
    parser.add_argument(
        '-c', '--config',
        help='Path to config.json file (optional, will use default locations if not specified)'
    )
    return parser.parse_args()

def process_vault_file(src_file: str, dry_run: bool = False, verbose: bool = False, show_console: bool = False) -> bool:
    """Process a single file for conversion and integration into the vault, mirroring OneDrive structure.

    Args:
        src_file (str): Path to the source file
        dry_run (bool): If True, don't write any files
        verbose (bool): If True, print detailed information
        show_console (bool): If True, print a concise status line to the console (for progress bar)

    Returns:
        bool: True if successful, False if there was an error

    Raises:
        ValueError: If the file is not under the OneDrive resources root

    Example:
        >>> process_vault_file('/mnt/c/Users/me/OneDrive/MBA-Resources/Program/Course/file.html')
        True
    """
    from notebook_automation.tools.utils.config import ONEDRIVE_LOCAL_RESOURCES_ROOT, NOTEBOOK_VAULT_ROOT
    try:
        src_file_path = Path(src_file).resolve()
        try:
            rel_path = src_file_path.relative_to(Path(ONEDRIVE_LOCAL_RESOURCES_ROOT).resolve())
        except ValueError:
            failed_logger.error(f"File {src_file} is not under the OneDrive resources root: {ONEDRIVE_LOCAL_RESOURCES_ROOT}")
            return False
        dest_file = Path(NOTEBOOK_VAULT_ROOT) / rel_path
        dest_file = dest_file.with_suffix('.md')


        # Print a concise, colorized, and aligned status line for the progress bar using tqdm.write
        if show_console:
            from tqdm import tqdm
            # Color codes (works in most modern terminals)
            GREEN = '\033[92m'
            YELLOW = '\033[93m'
            RED = '\033[91m'
            RESET = '\033[0m'
            BOLD = '\033[1m'
            # Symbols
            CHECK = '✅'
            WARN = '⚠️'
            CROSS = '❌'
            # Truncate and align paths (show last 2 path parts if too long)
            def format_path(path, width=45):
                path = str(path)
                if len(path) <= width:
                    return path.ljust(width)
                parts = path.split(os.sep)
                if len(parts) > 2:
                    return ('...%s%s%s' % (os.sep, os.sep.join(parts[-2:]), '' if path.endswith(os.sep) else '')).ljust(width)
                return ('...' + path[-(width-3):]).ljust(width)

            short_src = format_path(src_file_path)
            short_dest = format_path(dest_file)
            if dry_run:
                color = YELLOW
                symbol = WARN
                status = f"[DRY RUN]"
            else:
                color = GREEN
                symbol = CHECK
                status = "[OK]"
            tqdm.write(f"{color}{status} {symbol} {short_src} → {short_dest}{RESET}")

        if verbose:
            logger.info(f"Processing file: {src_file}")
            logger.info(f"Destination: {dest_file}")

        # Use shared conversion utility for html/txt, or pypandoc for epub
        ext = src_file_path.suffix.lower()
        if ext == '.epub':
            try:
                import pypandoc
            except ImportError:
                failed_logger.error("pypandoc is required to convert EPUB files. Please install it with 'pip install pypandoc' and ensure pandoc is available.")
                return False
            if dry_run:
                if not show_console:
                    logger.info(f"[DRY RUN] Would convert EPUB: {src_file} -> {dest_file}")
                return True
            try:
                output = pypandoc.convert_file(str(src_file_path), 'md', outputfile=str(dest_file))
                if not show_console:
                    logger.info(f"Converted EPUB: {src_file} -> {dest_file}")
                return True
            except Exception as e:
                failed_logger.error(f"Error converting EPUB {src_file}: {e}")
                if verbose:
                    logger.error(f"Error details: {e}")
                return False
        else:
            success, error = convert_to_markdown(str(src_file_path), str(dest_file), dry_run=dry_run)
            if success:
                if not show_console:
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

def generate_markdown(src_dirs: list[str], dry_run: bool = False, verbose: bool = False) -> tuple[int, int, int]:
    """Generate markdown files from HTML and TXT sources, mirroring OneDrive structure in the vault.

    Args:
        src_dirs (list): List of source directories to scan
        dry_run (bool): If True, don't write any files
        verbose (bool): If True, print detailed information

    Returns:
        tuple: (processed_count, success_count, error_count)

    Example:
        >>> generate_markdown(['./MBA-Resources/Program/Course'], dry_run=True)
        (10, 10, 0)
    """

    # Counters for reporting
    total_files_looked_at = 0
    txt_files_found = 0
    html_files_found = 0
    epub_files_found = 0
    mp4_files_found = 0
    pdf_files_found = 0
    other_files_found = 0
    other_extensions = set()
    txt_files_to_convert = 0
    html_files_to_convert = 0
    epub_files_to_convert = 0
    txt_files_converted = 0
    html_files_converted = 0
    epub_files_converted = 0
    skipped_due_to_mp4 = 0
    errors = 0

    from notebook_automation.tools.utils.config import ONEDRIVE_LOCAL_RESOURCES_ROOT, NOTEBOOK_VAULT_ROOT

    if verbose:
        logger.info("Starting markdown generation")
        logger.info(f"Source directories: {', '.join(src_dirs)}")
        logger.info(f"Vault root: {NOTEBOOK_VAULT_ROOT}")
        logger.info(f"OneDrive resources root: {ONEDRIVE_LOCAL_RESOURCES_ROOT}")
        logger.info(f"{'[DRY RUN] ' if dry_run else ''}Processing files...")

    # Import has_corresponding_mp4 from convert_markdown.py
    from notebook_automation.cli.convert_markdown import has_corresponding_mp4

    for src_dir in src_dirs:
        for root, dirs, files in os.walk(src_dir):
            for file in files:
                total_files_looked_at += 1
                src_file = os.path.join(root, file)
                ext = Path(src_file).suffix.lower()
                # Count file types
                if ext == '.txt':
                    txt_files_found += 1
                elif ext == '.html':
                    html_files_found += 1
                elif ext == '.epub':
                    epub_files_found += 1
                elif ext == '.mp4':
                    mp4_files_found += 1
                elif ext == '.pdf':
                    pdf_files_found += 1
                else:
                    other_files_found += 1
                    other_extensions.add(ext)

                # Only process .html, .txt, .epub for conversion
                if ext not in {'.html', '.txt', '.epub'}:
                    continue

                # For .html and .txt, skip if .mp4 exists
                if ext in {'.html', '.txt'} and has_corresponding_mp4(Path(src_file)):
                    skipped_due_to_mp4 += 1
                    if verbose:
                        logger.info(f"Skipping {src_file} (corresponding .mp4 exists)")
                    continue

                # Count files to convert by type
                if ext == '.txt':
                    txt_files_to_convert += 1
                elif ext == '.html':
                    html_files_to_convert += 1
                elif ext == '.epub':
                    epub_files_to_convert += 1

                # Convert file
                result = process_vault_file(src_file, dry_run, verbose)
                if result:
                    if ext == '.txt':
                        txt_files_converted += 1
                    elif ext == '.html':
                        html_files_converted += 1
                    elif ext == '.epub':
                        epub_files_converted += 1
                else:
                    errors += 1

    # Print summary
    logger.info("\nMarkdown Generation Summary:")
    logger.info(f"Total files looked at: {total_files_looked_at}")
    logger.info(f"  .txt files found:   {txt_files_found}")
    logger.info(f"  .html files found:  {html_files_found}")
    logger.info(f"  .epub files found:  {epub_files_found}")
    logger.info(f"  .mp4 files found:   {mp4_files_found}")
    logger.info(f"  .pdf files found:   {pdf_files_found}")
    logger.info(f"  Other files found:  {other_files_found}")
    if other_extensions:
        logger.info(f"  Unique other extensions: {sorted(other_extensions)}")
    logger.info(f"\nFiles to convert by type:")
    logger.info(f"  .txt to convert:    {txt_files_to_convert}")
    logger.info(f"  .html to convert:   {html_files_to_convert}")
    logger.info(f"  .epub to convert:   {epub_files_to_convert}")
    logger.info(f"  Skipped due to .mp4: {skipped_due_to_mp4}")
    logger.info(f"\nFiles converted by type:")
    logger.info(f"  .txt converted:     {txt_files_converted}")
    logger.info(f"  .html converted:    {html_files_converted}")
    logger.info(f"  .epub converted:    {epub_files_converted}")
    logger.info(f"Errors:              {errors}")

    if dry_run:
        logger.info("\nThis was a dry run. No files were modified.")

    # Return: total looked at, total converted, errors, txt files skipped due to mp4, unique other extensions, pdf count
    return (
        total_files_looked_at,
        txt_files_converted + html_files_converted + epub_files_converted,
        errors,
        skipped_due_to_mp4,
        other_extensions,
        pdf_files_found
    )


def main() -> int:
    """Main entry point for the script."""
    args = parse_args()

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

    # Set up color logging with debug if requested
    global logger, failed_logger
    if args.debug:
        logger, failed_logger = setup_colored_logging(debug=True)

    # Determine source directories
    src_dirs = args.src_dirs or ['.']

    # Validate source directories
    for src_dir in src_dirs:
        if not os.path.exists(src_dir):
            logger.error(f"Source directory not found: {src_dir}")
            return 1

    # Validate OneDrive and vault roots
    from notebook_automation.tools.utils.config import ONEDRIVE_LOCAL_RESOURCES_ROOT, NOTEBOOK_VAULT_ROOT
    if not os.path.exists(ONEDRIVE_LOCAL_RESOURCES_ROOT):
        logger.error(f"OneDrive resources root not found: {ONEDRIVE_LOCAL_RESOURCES_ROOT}")
        return 1
    if not os.path.exists(NOTEBOOK_VAULT_ROOT):
        if args.dry_run:
            logger.warning(f"Vault root directory doesn't exist: {NOTEBOOK_VAULT_ROOT}")
        else:
            try:
                os.makedirs(NOTEBOOK_VAULT_ROOT, exist_ok=True)
            except Exception as e:
                logger.error(f"Could not create vault root directory: {e}")
                return 1

    # Generate markdown files and print detailed summary
    try:
        total_looked_at, total_converted, errors, txt_skipped_due_to_mp4, other_extensions, pdf_files_found = generate_markdown(
            src_dirs, args.dry_run, args.verbose)

        SEP = '\n' + '-' * 48
        GREEN = '\033[92m'
        YELLOW = '\033[93m'
        RED = '\033[91m'
        RESET = '\033[0m'
        logger.info(f"{SEP}\nSummary of findings:")
        logger.info(f"  {GREEN}Total files looked at:           {total_looked_at}{RESET}")
        logger.info(f"  {GREEN}Successfully converted:          {total_converted}{RESET}")
        logger.info(f"  {YELLOW}.txt files skipped due to .mp4: {txt_skipped_due_to_mp4}{RESET}")
        logger.info(f"  {GREEN}.pdf files found:                 {pdf_files_found}{RESET}")
        logger.info(f"  {RED}Errors:                         {errors}{RESET}")
        if other_extensions:
            logger.info(f"  Unique other extensions found: {sorted(other_extensions)}")
        logger.info(SEP)
        if args.dry_run:
            logger.info(f"{YELLOW}This was a dry run. No files were modified.{RESET}")
        return 0 if errors == 0 else 1
    except Exception as e:
        logger.error(f"Error during markdown generation: {e}")
        return 1

if __name__ == "__main__":
    main()
