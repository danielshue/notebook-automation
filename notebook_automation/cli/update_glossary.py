#!/usr/bin/env python3
"""
Temporary implementation of update_glossary.py to fix issues.

This script provides a command-line interface for updating glossary pages in an Obsidian vault
by adding proper markdown callouts to definition entries. It checks for YAML frontmatter with
'type: glossary' and formats all definition entries using the > [!definition] syntax.

Features:
---------
- Uses default vault path when no directory is provided
- Processes any specified markdown file or all files matching a glossary pattern
- Checks YAML frontmatter for 'type: glossary' before processing
- Transforms plain definitions into stylized Obsidian callouts
- Provides detailed colored logging of transformation operations
- Reports statistics on files processed and terms updated
- Supports both dry-run preview and actual file modifications
"""

import sys
import logging
import argparse
import pathlib
import re
import os
from typing import List, Tuple, Dict, Any
from pathlib import Path

try:
    import yaml
except ImportError:
    try:
        from ruamel.yaml import YAML
        yaml = YAML()
    except ImportError:
        logging.critical("Neither 'yaml' nor 'ruamel.yaml' package is installed. Please install one of them.")
        sys.exit(1)

# Import config utilities
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from notebook_automation.tools.utils.config import setup_logging, load_config_data, find_config_path
from notebook_automation.cli.utils import (
    HEADER, OKBLUE, OKCYAN, OKGREEN, WARNING, FAIL, ENDC, BOLD, BG_BLUE, remove_timestamps_from_logger
)

def configure_logging(verbose=False):
    """Configure logging with color support for the application."""
    global logger
    logger, _ = setup_logging(debug=verbose)
    remove_timestamps_from_logger(logger)
    return logger

def parse_yaml_frontmatter(content):
    """Parse YAML frontmatter from markdown content.
    
    Args:
        content (str): The markdown content with possible frontmatter.
        
    Returns:
        Tuple[Dict[str, Any], str]: A tuple containing the parsed frontmatter as a dictionary
                                    and the content without the frontmatter.
    """
    frontmatter = {}
    content_without_frontmatter = content
    
    if content.startswith('---'):
        parts = content.split('---', 2)
        if len(parts) >= 3:
            try:
                frontmatter = yaml.safe_load(parts[1]) or {}
                content_without_frontmatter = parts[2].lstrip()
            except Exception as e:
                logger.warning(f"{WARNING}Error parsing YAML frontmatter: {e}{ENDC}")
    
    return frontmatter, content_without_frontmatter

def process_file(file_path, dry_run=False, force=False):
    """Process a glossary markdown file to add callouts to definitions.
    
    Args:
        file_path (pathlib.Path): Path to the glossary markdown file.
        dry_run (bool): If True, don't make actual changes to the file. Defaults to False.
        force (bool): If True, process the file regardless of frontmatter type. Defaults to False.
        
    Returns:
        Tuple[int, int]: Count of (processed definitions, modified definitions)
    """
    if not file_path.exists():
        logger.error(f"{FAIL}File not found: {file_path}{ENDC}")
        return 0, 0
    
    logger.debug(f"{OKCYAN}Examining file: {file_path}{ENDC}")
    
    try:
        with open(file_path, 'r', encoding='utf-8') as file:
            content = file.read()
    except Exception as e:
        logger.error(f"{FAIL}Error reading file {file_path}: {e}{ENDC}")
        return 0, 0
    
    # Parse YAML frontmatter
    frontmatter, _ = parse_yaml_frontmatter(content)
    
    # Check if the file is a glossary
    if not force and frontmatter.get('type') != 'glossary':
        logger.info(f"{WARNING}Skipping non-glossary file: {file_path}{ENDC}")
        if force:
            logger.info(f"{OKBLUE}Processing anyway due to --force flag{ENDC}")
        return 0, 0
    
    logger.info(f"{OKBLUE}Processing glossary file: {BOLD}{file_path}{ENDC}")
      # Process line by line
    pattern = r'^\*\*[\w\s\-\'.,:()]+\*\*.*$'
    lines = content.splitlines()
    new_lines = []
    processed = 0
    modified = 0
    
    # Track terms being modified for detailed reporting
    modified_terms = []
    
    i = 0
    while i < len(lines):
        line = lines[i]
        
        # Check if this line matches our definition pattern
        if re.match(pattern, line):
            processed += 1
            
            # Extract the term for better reporting
            term_match = re.search(r'^\*\*([\w\s\-\'.,:()/\\]+)\*\*', line)
            term = term_match.group(1) if term_match else "unknown term"
            
            # Check if it's already in a callout
            if i > 0 and lines[i-1].strip() == '> [!definition]':
                new_lines.append(line)
            else:
                # Add the callout
                new_lines.append('> [!definition]')
                new_lines.append(f'> {line}')
                modified += 1
                modified_terms.append(term.strip())
                logger.debug(f"{OKGREEN}Adding callout for term: {BOLD}{term.strip()}{ENDC}")
        else:
            new_lines.append(line)
        
        i += 1
    
    # Generate status message
    if processed == 0:
        logger.info(f"{WARNING}No definition entries found in {file_path}{ENDC}")
        return 0, 0
    
    # Write the changes if not in dry_run mode
    if modified > 0 and not dry_run:
        try:
            with open(file_path, 'w', encoding='utf-8') as file:
                file.write('\n'.join(new_lines))
            terms_list = ", ".join(modified_terms[:3])
            if len(modified_terms) > 3:
                terms_list += f", and {len(modified_terms) - 3} more"
            logger.info(f"{OKGREEN}Updated {modified} definitions in {file_path} ({terms_list}){ENDC}")
        except Exception as e:
            logger.error(f"{FAIL}Error writing to file {file_path}: {e}{ENDC}")
            return processed, 0
    
    status = f"{OKCYAN}Would update{ENDC}" if dry_run else f"{OKGREEN}Updated{ENDC}"
    percentage = int((modified / processed) * 100) if processed > 0 else 0
    logger.info(f"{status} {modified} of {processed} definitions ({percentage}%) in {file_path}")
    
    return processed, modified

def main():
    """Main entry point for the glossary update tool.
    
    Parses command-line arguments, sets up logging, and processes glossary files
    to update definition entries with proper markdown callout syntax.
    
    Returns:
        int: Exit code (0 for success, non-zero for failure).
    """
    parser = argparse.ArgumentParser(
        description="Update glossary files with proper markdown callouts for definitions",
        formatter_class=argparse.RawDescriptionHelpFormatter
    )
    
    # Define a mutually exclusive group for file input methods
    input_group = parser.add_mutually_exclusive_group()
    input_group.add_argument(
        'files',
        nargs='*',
        type=pathlib.Path,
        help="Glossary markdown files to process",
        default=[]
    )
    input_group.add_argument(
        '--directory', '-d',
        type=pathlib.Path,
        help="Process all glossary files in this directory"
    )
    
    # Other arguments
    parser.add_argument(
        '--pattern', '-p',
        default="*Glossary*.md",
        help="File pattern to match (default: '*Glossary*.md')"
    )
    parser.add_argument(
        '--config', '-c',
        help="Path to config.json (optional)"
    )
    parser.add_argument(
        '--dry-run',
        action='store_true',
        help="Preview changes without modifying files"
    )
    parser.add_argument(
        '--verbose', '-v',
        action='store_true',
        help="Enable verbose logging"
    )
    parser.add_argument(
        '--force', '-f',
        action='store_true',
        help="Process files even if they don't have 'type: glossary' in frontmatter"
    )
    
    args = parser.parse_args()
    logger = configure_logging(args.verbose)
    
    # Set config path if provided
    if args.config:
        # Use absolute path to ensure consistency
        config_path = str(Path(args.config).absolute())
        os.environ["NOTEBOOK_CONFIG_PATH"] = config_path
        
    # Display which config.json file is being used
    try:
        config_path = os.environ.get("NOTEBOOK_CONFIG_PATH") or find_config_path()
        print(f"{OKCYAN}Using configuration file: {config_path}{ENDC}")
    except Exception as e:
        print(f"Could not determine config file path: {e}")
    
    try:
        # Get configuration
        config = load_config_data(args.config)
        vault_path = Path(config.get("vault_root", "."))
        
        files_to_process = []
        
        # Get list of files to process
        if args.files:
            files_to_process = args.files
            logger.info(f"{OKBLUE}Processing {len(files_to_process)} specified file(s){ENDC}")
        elif args.directory:
            directory = args.directory
            if not directory.exists() or not directory.is_dir():
                logger.error(f"{FAIL}Directory not found or not a directory: {directory}{ENDC}")
                return 1
                
            logger.info(f"{OKBLUE}Searching for files in: {BOLD}{directory}{ENDC}")
            files_to_process = list(directory.glob(args.pattern))
        else:
            # Use default vault path
            directory = vault_path
            logger.info(f"{OKBLUE}Using default vault path: {BOLD}{directory}{ENDC}")
            logger.info(f"{OKBLUE}Searching for files matching pattern: {BOLD}{args.pattern}{ENDC}")
            files_to_process = list(directory.glob(f"**/{args.pattern}"))
        
        print(f"[DEBUG] Files to process: {[str(f) for f in files_to_process]}")
        if not files_to_process:
            pattern_info = f"pattern '{args.pattern}'" if args.directory or not args.files else ""
            location_info = f" in {args.directory}" if args.directory else f" in vault {vault_path}" if not args.files else ""
            print(f"{WARNING}No files matching {pattern_info} found{location_info}{ENDC}")
            return 0
        print(f"{OKBLUE}Found {BOLD}{len(files_to_process)}{ENDC}{OKBLUE} potential glossary files{ENDC}")
        
        # Process each file
        total_processed = 0
        total_modified = 0
        processed_files = 0
        skipped_files = 0
        found_files = len(files_to_process)
        
        # Show a header for the processing phase
        mode_indicator = f"{OKCYAN}[DRY RUN]{ENDC} " if args.dry_run else ""
        print(f"\n{HEADER}{mode_indicator}Processing glossary files...{ENDC}")
        
        for file_path in files_to_process:
            p, m = process_file(file_path, args.dry_run, args.force)
            if p > 0:
                processed_files += 1
                total_processed += p
                total_modified += m
            else:
                skipped_files += 1
        
        # Generate summary report
        print(f"\n{BG_BLUE}GLOSSARY UPDATE SUMMARY{ENDC}")
        print(f"{BOLD}Files:{ENDC}")
        print(f"  {OKCYAN}Found:{ENDC}     {found_files}")
        print(f"  {OKCYAN}Processed:{ENDC} {processed_files}")
        print(f"  {OKCYAN}Skipped:{ENDC}   {skipped_files}")
        print(f"{BOLD}Definitions:{ENDC}")
        print(f"  {OKCYAN}Found:{ENDC}     {total_processed}")
        print(f"  {OKCYAN}Updated:{ENDC}   {total_modified}")
        percentage = int((total_modified / total_processed) * 100) if total_processed > 0 else 0
        mode = f"{OKCYAN}[DRY RUN]{ENDC} " if args.dry_run else ""
        if total_modified > 0:
            print(f"\n{mode}{OKGREEN}Successfully updated {total_modified} of {total_processed} "
                  f"definitions ({percentage}%) across {processed_files} files.{ENDC}")
        else:
            if total_processed > 0:
                print(f"\n{OKGREEN}No definitions required updating across {processed_files} files.{ENDC}")
            else:
                print(f"\n{WARNING}No definitions found in the specified files.{ENDC}")
        
        return 0
    
    except Exception as e:
        logging.error(f"Unexpected error: {e}")
        if args.verbose:
            import traceback
            traceback.print_exc()
        return 1

if __name__ == "__main__":
    sys.exit(main())#!/usr/bin/env python3
"""
Glossary Update CLI for Digital Note Management System

This script provides a command-line interface for updating glossary pages in an Obsidian vault
by adding proper markdown callouts to definition entries. It checks for YAML frontmatter with
'type: glossary' and formats all definition entries using the > [!definition] syntax.

Features:
---------
- Uses default vault path when no directory is provided
- Processes any specified markdown file or all files matching a glossary pattern
- Checks YAML frontmatter for 'type: glossary' before processing
- Transforms plain definitions into stylized Obsidian callouts
- Provides detailed colored logging of transformation operations
- Reports statistics on files processed and terms updated
- Supports both dry-run preview and actual file modifications
"""

import sys
import logging
import argparse
import pathlib
import re
import os
from typing import List, Tuple, Dict, Any
from pathlib import Path

try:
    import yaml
except ImportError:
    try:
        from ruamel.yaml import YAML
        yaml = YAML()
    except ImportError:
        logging.critical("Neither 'yaml' nor 'ruamel.yaml' package is installed. Please install one of them.")
        sys.exit(1)

# Import config utilities
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from notebook_automation.tools.utils.config import setup_logging, load_config_data, find_config_path
from notebook_automation.cli.utils import (
    HEADER, OKBLUE, OKCYAN, OKGREEN, WARNING, FAIL, ENDC, BOLD, BG_BLUE, remove_timestamps_from_logger
)

def configure_logging(verbose=False):
    """Configure logging with color support for the application."""
    global logger
    logger, _ = setup_logging(debug=verbose)
    remove_timestamps_from_logger(logger)
    return logger

def parse_yaml_frontmatter(content):
    """Parse YAML frontmatter from markdown content.
    
    Args:
        content (str): The markdown content with possible frontmatter.
        
    Returns:
        Tuple[Dict[str, Any], str]: A tuple containing the parsed frontmatter as a dictionary
                                    and the content without the frontmatter.
    """
    frontmatter = {}
    content_without_frontmatter = content
    
    if content.startswith('---'):
        parts = content.split('---', 2)
        if len(parts) >= 3:
            try:
                frontmatter = yaml.safe_load(parts[1]) or {}
                content_without_frontmatter = parts[2].lstrip()
            except Exception as e:
                logger.warning(f"{WARNING}Error parsing YAML frontmatter: {e}{ENDC}")
    
    return frontmatter, content_without_frontmatter

def process_file(file_path, dry_run=False, force=False):
    """Process a glossary markdown file to add callouts to definitions.
    
    Args:
        file_path (pathlib.Path): Path to the glossary markdown file.
        dry_run (bool): If True, don't make actual changes to the file. Defaults to False.
        force (bool): If True, process the file regardless of frontmatter type. Defaults to False.
        
    Returns:
        Tuple[int, int]: Count of (processed definitions, modified definitions)
    """
    if not file_path.exists():
        logger.error(f"{FAIL}File not found: {file_path}{ENDC}")
        return 0, 0
    
    logger.debug(f"{OKCYAN}Examining file: {file_path}{ENDC}")
    
    try:
        with open(file_path, 'r', encoding='utf-8') as file:
            content = file.read()
    except Exception as e:
        logger.error(f"{FAIL}Error reading file {file_path}: {e}{ENDC}")
        return 0, 0
    
    # Parse YAML frontmatter
    frontmatter, _ = parse_yaml_frontmatter(content)
    
    # Check if the file is a glossary
    if not force and frontmatter.get('type') != 'glossary':
        logger.info(f"{WARNING}Skipping non-glossary file: {file_path}{ENDC}")
        if force:
            logger.info(f"{OKBLUE}Processing anyway due to --force flag{ENDC}")
        return 0, 0
    
    logger.info(f"{OKBLUE}Processing glossary file: {BOLD}{file_path}{ENDC}")
    # Process line by line
    pattern = r'^\*\*[\w\s\-\'.,:()]+\*\*.*$'
    lines = content.splitlines()
    new_lines = []
    processed = 0
    modified = 0
    # Track terms being modified for detailed reporting
    modified_terms = []
    i = 0
    while i < len(lines):
        line = lines[i]
        # Check if this line matches our definition pattern
        if re.match(pattern, line):
            processed += 1
            # Extract the term for better reporting
            term_match = re.search(r'^\*\*([\w\s\-\'.,:()/\\]+)\*\*', line)
            term = term_match.group(1) if term_match else "unknown term"
            # Check if it's already in a callout
            if i > 0 and lines[i-1].strip() == '> [!definition]':
                new_lines.append(line)
            else:
                # Add the callout
                new_lines.append('> [!definition]')
                new_lines.append(f'> {line}')
                modified += 1
                modified_terms.append(term.strip())
                logger.debug(f"{OKGREEN}Adding callout for term: {BOLD}{term.strip()}{ENDC}")
        else:
            new_lines.append(line)
        i += 1
    # Generate status message
    if processed == 0:
        logger.info(f"{WARNING}No definition entries found in {file_path}{ENDC}")
        return 0, 0
    # Write the changes if not in dry_run mode
    if modified > 0 and not dry_run:
        try:
            with open(file_path, 'w', encoding='utf-8') as file:
                file.write('\n'.join(new_lines))
            terms_list = ", ".join(modified_terms[:3])
            if len(modified_terms) > 3:
                terms_list += f", and {len(modified_terms) - 3} more"
            logger.info(f"{OKGREEN}Updated {modified} definitions in {file_path} ({terms_list}){ENDC}")
        except Exception as e:
            logger.error(f"{FAIL}Error writing to file {file_path}: {e}{ENDC}")
            return processed, 0
    status = f"{OKCYAN}Would update{ENDC}" if dry_run else f"{OKGREEN}Updated{ENDC}"
    percentage = int((modified / processed) * 100) if processed > 0 else 0
    logger.info(f"{status} {modified} of {processed} definitions ({percentage}%) in {file_path}")
    return processed, modified

def main():
    """Main entry point for the glossary update tool.
    
    Returns:
        int: Exit code (0 for success, non-zero for failure).
    """
    parser = argparse.ArgumentParser(
        description="Update glossary files with proper markdown callouts for definitions",
        formatter_class=argparse.RawDescriptionHelpFormatter
    )
    # Define a mutually exclusive group for file input methods
    input_group = parser.add_mutually_exclusive_group()
    input_group.add_argument(
        'files',
        nargs='*',
        type=pathlib.Path,
        help="Glossary markdown files to process",
        default=[]
    )
    input_group.add_argument(
        '--directory', '-d',
        type=pathlib.Path,
        help="Process all glossary files in this directory"
    )
    # Other arguments
    parser.add_argument(
        '--pattern', '-p',
        default="*Glossary*.md",
        help="File pattern to match (default: '*Glossary*.md')"
    )
    parser.add_argument(
        '--config', '-c',
        help="Path to config.json (optional)"
    )
    parser.add_argument(
        '--dry-run',
        action='store_true',
        help="Preview changes without modifying files"
    )
    parser.add_argument(
        '--verbose', '-v',
        action='store_true',
        help="Enable verbose logging"
    )
    parser.add_argument(
        '--force', '-f',
        action='store_true',
        help="Process files even if they don't have 'type: glossary' in frontmatter"
    )
    args = parser.parse_args()
    logger = configure_logging(args.verbose)
    
    # Set config path if provided
    if args.config:
        # Use absolute path to ensure consistency
        config_path = str(Path(args.config).absolute())
        os.environ["NOTEBOOK_CONFIG_PATH"] = config_path
        
    # Display which config.json file is being used
    try:
        config_path = os.environ.get("NOTEBOOK_CONFIG_PATH") or find_config_path()
        print(f"{OKCYAN}Using configuration file: {config_path}{ENDC}")
    except Exception as e:
        print(f"Could not determine config file path: {e}")
    
    try:
        # Get configuration
        config = load_config_data(args.config)
        vault_path = Path(config.get("vault_root", "."))
        
        files_to_process = []
        
        # Get list of files to process
        if args.files:
            files_to_process = args.files
            logger.info(f"{OKBLUE}Processing {len(files_to_process)} specified file(s){ENDC}")
        elif args.directory:
            directory = args.directory
            if not directory.exists() or not directory.is_dir():
                logger.error(f"{FAIL}Directory not found or not a directory: {directory}{ENDC}")
                return 1
                
            logger.info(f"{OKBLUE}Searching for files in: {BOLD}{directory}{ENDC}")
            files_to_process = list(directory.glob(args.pattern))
        else:
            # Use default vault path
            directory = vault_path
            logger.info(f"{OKBLUE}Using default vault path: {BOLD}{directory}{ENDC}")
            logger.info(f"{OKBLUE}Searching for files matching pattern: {BOLD}{args.pattern}{ENDC}")
            files_to_process = list(directory.glob(f"**/{args.pattern}"))
        
        print(f"[DEBUG] Files to process: {[str(f) for f in files_to_process]}")
        if not files_to_process:
            pattern_info = f"pattern '{args.pattern}'" if args.directory or not args.files else ""
            location_info = f" in {args.directory}" if args.directory else f" in vault {vault_path}" if not args.files else ""
            print(f"{WARNING}No files matching {pattern_info} found{location_info}{ENDC}")
            return 0
        print(f"{OKBLUE}Found {BOLD}{len(files_to_process)}{ENDC}{OKBLUE} potential glossary files{ENDC}")
        
        # Process each file
        total_processed = 0
        total_modified = 0
        processed_files = 0
        skipped_files = 0
        found_files = len(files_to_process)
        
        # Show a header for the processing phase
        mode_indicator = f"{OKCYAN}[DRY RUN]{ENDC} " if args.dry_run else ""
        print(f"\n{HEADER}{mode_indicator}Processing glossary files...{ENDC}")
        
        for file_path in files_to_process:
            p, m = process_file(file_path, args.dry_run, args.force)
            if p > 0:
                processed_files += 1
                total_processed += p
                total_modified += m
            else:
                skipped_files += 1
        
        # Generate summary report
        print(f"\n{BG_BLUE}GLOSSARY UPDATE SUMMARY{ENDC}")
        print(f"{BOLD}Files:{ENDC}")
        print(f"  {OKCYAN}Found:{ENDC}     {found_files}")
        print(f"  {OKCYAN}Processed:{ENDC} {processed_files}")
        print(f"  {OKCYAN}Skipped:{ENDC}   {skipped_files}")
        print(f"{BOLD}Definitions:{ENDC}")
        print(f"  {OKCYAN}Found:{ENDC}     {total_processed}")
        print(f"  {OKCYAN}Updated:{ENDC}   {total_modified}")
        percentage = int((total_modified / total_processed) * 100) if total_processed > 0 else 0
        mode = f"{OKCYAN}[DRY RUN]{ENDC} " if args.dry_run else ""
        if total_modified > 0:
            print(f"\n{mode}{OKGREEN}Successfully updated {total_modified} of {total_processed} "
                  f"definitions ({percentage}%) across {processed_files} files.{ENDC}")
        else:
            if total_processed > 0:
                print(f"\n{OKGREEN}No definitions required updating across {processed_files} files.{ENDC}")
            else:
                print(f"\n{WARNING}No definitions found in the specified files.{ENDC}")
        
        return 0
    
    except Exception as e:
        logging.error(f"Unexpected error: {e}")
        if args.verbose:
            import traceback
            traceback.print_exc()
        return 1

if __name__ == "__main__":
    sys.exit(main())
#!/usr/bin/env python3
"""
Glossary Update CLI for Digital Note Management System

This script provides a command-line interface for updating glossary pages in an Obsidian vault
by adding proper markdown callouts to definition entries. It searches for definitions starting
with "**word**" patterns between alphabetical headers (## A, ## B, etc.) and transforms them 
into proper callouts using the > [!definition] syntax.

Features:
---------
- Processes any specified markdown file or all files matching a glossary pattern
- Intelligently identifies definition entries based on the **term** pattern
- Transforms plain definitions into stylized Obsidian callouts
- Preserves existing formatting and content
- Handles various edge cases (already formatted entries, non-standard headers)
- Provides detailed logging of transformation operations
- Supports both dry-run preview and actual file modifications
- Checks YAML frontmatter for 'type: glossary' before processing

Usage Examples:
---------------
    # Update a specific glossary file
    $ python update_glossary.py path/to/glossary.md
    
    # Process multiple glossary files
    $ python update_glossary.py path/to/glossary1.md path/to/glossary2.md
    
    # Preview changes without modifying files (dry run)
    $ python update_glossary.py --dry-run path/to/glossary.md
    
    # Process all files in a directory matching a pattern
    $ python update_glossary.py --directory path/to/glossaries --pattern "*Glossary*.md"

Arguments:
----------
    file            One or more glossary markdown files to process
    --dry-run       Preview changes without modifying files
    --directory     Process all files in the specified directory
    --pattern       File pattern to match (default: "*Glossary*.md")
    --verbose       Enable verbose logging
"""

import argparse
import pathlib
import re
import sys
import logging
import yaml
from typing import List, Tuple, Optional, Dict, Any
from pathlib import Path


def configure_logging(verbose: bool = False) -> None:
    """Configure logging settings for the application.
    
    Args:
        verbose (bool): Whether to enable verbose logging. Defaults to False.
    """
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(asctime)s - %(levelname)s - %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
        handlers=[logging.StreamHandler()]
    )


def find_glossary_files(directory: pathlib.Path, pattern: str) -> List[pathlib.Path]:
    """Find all files matching the specified pattern in the directory.
    
    Args:
        directory (pathlib.Path): Directory to search in.
        pattern (str): Glob pattern to match files.
        
    Returns:
        List[pathlib.Path]: List of matching files.
    """
    return list(directory.glob(pattern))


definition_pattern = r'^\*\*[\w\s\-\'.,:()]+\*\*.*$'  # Pattern to match lines starting with bold text


def process_glossary_file(file_path: pathlib.Path, dry_run: bool = False, force: bool = False) -> Tuple[int, int]:
    """Process a glossary markdown file to add callouts to definitions.
    
    Args:
        file_path (pathlib.Path): Path to the glossary markdown file.
        dry_run (bool): If True, don't make actual changes to the file. Defaults to False.
        force (bool): If True, process the file regardless of frontmatter type. Defaults to False.
        
    Returns:
        Tuple[int, int]: Count of (processed definitions, modified definitions)
    """
    if not file_path.exists():
        logging.error(f"File not found: {file_path}")
        return 0, 0

    logging.info(f"Examining file: {file_path}")
    
    # Read the content of the file
    try:
        with open(file_path, 'r', encoding='utf-8') as file:
            content = file.read()
    except Exception as e:
        logging.error(f"Error reading file {file_path}: {e}")
        return 0, 0
        
    # Parse YAML frontmatter
    frontmatter, content_without_frontmatter = parse_yaml_frontmatter(content)
    
    # Check if the file is a glossary
    if not force and frontmatter.get('type') != 'glossary':
        logging.info(f"Skipping non-glossary file: {file_path} (use --force to override)")
        return 0, 0
        
    logging.info(f"Processing glossary file: {file_path}")
    
    # Regular expression to find definition entries    # We'll work line by line instead of using regex
    lines = content.splitlines()
    new_lines = []
    processed = 0
    modified = 0
    # Track terms being modified for detailed reporting
    modified_terms = []
    i = 0
    while i < len(lines):
        line = lines[i]
        # Check if this line matches our definition pattern
        if re.match(pattern, line):
            processed += 1
            
            # Extract the term for better reporting
            term_match = re.search(r'^\*\*([\w\s\-\'.,:()/\\]+)\*\*', line)
            term = term_match.group(1) if term_match else "unknown term"
            
            # Check if it's already in a callout
            if i > 0 and lines[i-1].strip() == '> [!definition]':
                new_lines.append(line)
            else:
                # Add the callout
                new_lines.append('> [!definition]')
                new_lines.append(f'> {line}')
                modified += 1
                modified_terms.append(term.strip())
                logger.debug(f"{OKGREEN}Adding callout for term: {BOLD}{term.strip()}{ENDC}")
        else:
            new_lines.append(line)
        
        i += 1
    
    # Generate status message
    if processed == 0:
        logger.info(f"{WARNING}No definition entries found in {file_path}{ENDC}")
        return 0, 0
    
    # Write the changes if not in dry_run mode
    if modified > 0 and not dry_run:
        try:
            with open(file_path, 'w', encoding='utf-8') as file:
                file.write('\n'.join(new_lines))
            terms_list = ", ".join(modified_terms[:3])
            if len(modified_terms) > 3:
                terms_list += f", and {len(modified_terms) - 3} more"
            logger.info(f"{OKGREEN}Updated {modified} definitions in {file_path} ({terms_list}){ENDC}")
        except Exception as e:
            logger.error(f"{FAIL}Error writing to file {file_path}: {e}{ENDC}")
            return processed, 0
    status = f"{OKCYAN}Would update{ENDC}" if dry_run else f"{OKGREEN}Updated{ENDC}"
    percentage = int((modified / processed) * 100) if processed > 0 else 0
    logger.info(f"{status} {modified} of {processed} definitions ({percentage}%) in {file_path}")
    return processed, modified

def parse_yaml_frontmatter(content: str) -> Tuple[Dict[str, Any], str]:
    """Parse YAML frontmatter from markdown content.
    
    Args:
        content (str): The markdown content with possible frontmatter.
        
    Returns:
        Tuple[Dict[str, Any], str]: A tuple containing the parsed frontmatter as a dictionary
                                    and the content without the frontmatter.
    """
    frontmatter = {}
    content_without_frontmatter = content
    
    # Check if the content has YAML frontmatter
    if content.startswith('---'):
        # Find the closing '---'
        parts = content.split('---', 2)
        if len(parts) >= 3:
            try:
                # Parse the YAML content
                frontmatter = yaml.safe_load(parts[1]) or {}
                # Content without frontmatter
                content_without_frontmatter = parts[2].lstrip()
            except Exception as e:
                logging.warning(f"Error parsing YAML frontmatter: {e}")
    
    return frontmatter, content_without_frontmatter


def main() -> int:
    """Main entry point for the glossary update tool.
    
    Returns:
        int: Exit code (0 for success, non-zero for failure).
    """
    parser = argparse.ArgumentParser(
        description="Update glossary files with proper markdown callouts for definitions",
        formatter_class=argparse.RawDescriptionHelpFormatter
    )
    
    # Define a mutually exclusive group for file input methods
    input_group = parser.add_mutually_exclusive_group(required=True)
    input_group.add_argument(
        'files',
        nargs='*',
        type=pathlib.Path,
        help="Glossary markdown files to process",
        default=[]
    )
    input_group.add_argument(
        '--directory', '-d',
        type=pathlib.Path,
        help="Process all glossary files in this directory"
    )
    
    # Other arguments
    parser.add_argument(
        '--pattern', '-p',
        default="*Glossary*.md",
        help="File pattern to match (default: '*Glossary*.md')"    )
    parser.add_argument(
        '--dry-run',
        action='store_true',
        help="Preview changes without modifying files"
    )
    parser.add_argument(
        '--verbose', '-v',
        action='store_true',
        help="Enable verbose logging"
    )
    parser.add_argument(
        '--force', '-f',
        action='store_true',
        help="Process files even if they don't have 'type: glossary' in frontmatter"
    )
    
    args = parser.parse_args()
    configure_logging(args.verbose)
    
    # Set config path if provided
    if args.config:
        # Use absolute path to ensure consistency
        config_path = str(Path(args.config).absolute())
        os.environ["NOTEBOOK_CONFIG_PATH"] = config_path
        
    # Display which config.json file is being used
    try:
        config_path = os.environ.get("NOTEBOOK_CONFIG_PATH") or find_config_path()
        print(f"{OKCYAN}Using configuration file: {config_path}{ENDC}")
    except Exception as e:
        print(f"Could not determine config file path: {e}")
    
    try:
        files_to_process: List[pathlib.Path] = []
        
        # Get list of files to process
        if args.directory:
            if not args.directory.exists() or not args.directory.is_dir():
                logging.error(f"Directory not found or not a directory: {args.directory}")
                return 1
                
            files_to_process = find_glossary_files(args.directory, args.pattern)
            if not files_to_process:
                logging.warning(f"No files matching pattern '{args.pattern}' found in {args.directory}")
                return 0
        else:
            files_to_process = args.files
            
        # Process each file        total_processed = 0
        total_modified = 0
        
        for file_path in files_to_process:
            processed, modified = process_glossary_file(file_path, args.dry_run, args.force)
            total_processed += processed
            total_modified += modified
        
        mode = "Dry run: " if args.dry_run else ""
        if total_modified > 0:
            logging.info(f"{mode}Successfully updated {total_modified} of {total_processed} "
                        f"definitions across {len(files_to_process)} files.")
        else:
            if total_processed > 0:
                logging.info(f"No definitions required updating across {len(files_to_process)} files.")
            else:
                logging.warning("No definitions found in the specified files.")
        
        return 0
    
    except Exception as e:
        logging.error(f"Unexpected error: {e}")
        if args.verbose:
            import traceback
            traceback.print_exc()
        return 1


if __name__ == "__main__":
    sys.exit(main())
