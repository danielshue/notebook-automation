#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Markdown YAML Frontmatter Tag Processor.

This script recursively scans a folder for markdown (.md) files, extracts fields 
from the YAML frontmatter, and converts them to nested tags in the format 
#field/value. These tags are then added to the existing tags field or a new tags
field is created if it doesn't exist.

If a file has 'index-type' in its frontmatter, any tags will be removed as index
files should not have tags.

Example:
    $ mba-add-nested-tags /path/to/folder --verbose
    $ mba-add-nested-tags /path/to/folder --dry-run
    $ mba-add-nested-tags --verbose  # Uses default Obsidian vault path

"""

import argparse
import os
import re
import sys
from pathlib import Path
from typing import Dict, List, Set, Tuple, Optional, Any

# Import from mba_notebook_automation package
from mba_notebook_automation.tools.utils.config import setup_logging, VAULT_LOCAL_ROOT
from mba_notebook_automation.tools.utils.paths import normalize_wsl_path

# Set up logging
logger, failed_logger = setup_logging(debug=False)

# Try to import ruamel.yaml for better YAML formatting preservation
try:
    from ruamel.yaml import YAML
    USE_RUAMEL = True
    yaml_parser = YAML()
    yaml_parser.preserve_quotes = True
    yaml_parser.width = 4096  # To avoid line wrapping
except ImportError:
    import yaml as pyyaml
    USE_RUAMEL = False
    logger.warning("ruamel.yaml not found, using pyyaml instead.")
    logger.warning("Install ruamel.yaml for better formatting preservation: pip install ruamel.yaml")


class MarkdownFrontmatterProcessor:
    """Process markdown files to extract and update YAML frontmatter tags."""

    def __init__(self, dry_run: bool = False, verbose: bool = False):
        """
        Initialize the markdown frontmatter processor.
        
        Args:
            dry_run (bool): If True, no files will be modified.
            verbose (bool): If True, additional information will be logged.
        """
        self.dry_run = dry_run
        self.verbose = verbose
        self.yaml_frontmatter_pattern = re.compile(r'^---\s*\n(.*?)\n---\s*\n', re.DOTALL)
        self.stats = {
            'files_processed': 0,
            'files_modified': 0,
            'tags_added': 0,
            'index_files_cleared': 0,
            'files_with_errors': 0
        }
        # Fields to process for tags
        self.tag_fields = [
            'course',
            'mba-course',
            'lecture',
            'mba-lecture',
            'topic',
            'mba-topic',
            'subjects',
            'professor',
            'university',
            'program',
            'assignment',
            'mba-assignment',
            'type',
            'author'
        ]
    
    def process_directory(self, directory: Path) -> Dict[str, int]:
        """
        Recursively process markdown files in a directory.
        
        Args:
            directory (Path): The directory to process.
            
        Returns:
            Dict[str, int]: Statistics about the processing.
        """
        for item in directory.glob('**/*.md'):
            if item.is_file():
                try:
                    self.process_file(item)
                except Exception as e:
                    logger.error(f"Error processing {item}: {str(e)}")
                    self.stats['files_with_errors'] += 1
                    
        return self.stats
    
    def process_file(self, file_path: Path) -> None:
        """
        Process a single markdown file.
        
        Args:
            file_path (Path): Path to the markdown file.
        """
        if self.verbose:
            logger.info(f"Processing file: {file_path}")
            
        self.stats['files_processed'] += 1
        
        # Read the file content
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
        except UnicodeDecodeError:
            # Try with another encoding if UTF-8 fails
            with open(file_path, 'r', encoding='latin1') as f:
                content = f.read()
                
        # Extract YAML frontmatter
        match = self.yaml_frontmatter_pattern.search(content)
        if not match:
            if self.verbose:
                logger.info(f"No YAML frontmatter found in {file_path}")
            return
            
        yaml_content = match.group(1)
        
        # Parse YAML frontmatter
        try:
            if USE_RUAMEL:
                from io import StringIO
                yaml_stream = StringIO(yaml_content)
                frontmatter = yaml_parser.load(yaml_stream) or {}
            else:
                frontmatter = pyyaml.safe_load(yaml_content) or {}
        except Exception as e:
            logger.error(f"Error parsing YAML in {file_path}: {str(e)}")
            self.stats['files_with_errors'] += 1
            return
            
        # Check if this is an index file
        if 'index-type' in frontmatter:
            if 'tags' in frontmatter and frontmatter['tags']:
                if self.verbose:
                    logger.info(f"Clearing tags from index file: {file_path}")
                if not self.dry_run:
                    frontmatter['tags'] = []
                    self.update_file_frontmatter(file_path, content, frontmatter, match.start(1), match.end(1))
                    self.stats['index_files_cleared'] += 1
                    self.stats['files_modified'] += 1
            return
            
        # Extract tags from frontmatter
        existing_tags = set()
        if 'tags' in frontmatter and frontmatter['tags']:
            # Handle different formats of tags
            if isinstance(frontmatter['tags'], list):
                existing_tags = set(frontmatter['tags'])
            elif isinstance(frontmatter['tags'], str):
                # Split by commas if it's a comma-separated string
                tags = frontmatter['tags'].split(',')
                existing_tags = {tag.strip() for tag in tags}
                
        # Generate new tags from frontmatter fields
        new_tags = set()
        for field in self.tag_fields:
            if field in frontmatter and frontmatter[field]:
                field_value = frontmatter[field]
                
                # Handle different field value formats
                if isinstance(field_value, list):
                    # If it's a list, add a tag for each item
                    for item in field_value:
                        if item:  # Skip empty items
                            tag = f"{field}/{str(item).strip()}"
                            new_tags.add(tag)
                else:
                    # If it's a single value, add one tag
                    tag = f"{field}/{str(field_value).strip()}"
                    new_tags.add(tag)
                    
        # Check if we have new tags to add
        tags_to_add = new_tags - existing_tags
        if tags_to_add:
            if self.verbose:
                logger.info(f"Adding tags to {file_path}: {tags_to_add}")
                
            if not self.dry_run:
                # Update the frontmatter with new tags
                updated_tags = sorted(list(existing_tags.union(new_tags)))
                frontmatter['tags'] = updated_tags
                
                # Update the file content
                self.update_file_frontmatter(file_path, content, frontmatter, match.start(1), match.end(1))
                
                self.stats['tags_added'] += len(tags_to_add)
                self.stats['files_modified'] += 1
                
    def update_file_frontmatter(self, file_path: Path, content: str, 
                               frontmatter: Dict[str, Any], start: int, end: int) -> None:
        """
        Update the YAML frontmatter in a file.
        
        Args:
            file_path (Path): Path to the file to update.
            content (str): Current file content.
            frontmatter (Dict[str, Any]): Updated frontmatter.
            start (int): Start index of YAML content in the file.
            end (int): End index of YAML content in the file.
        """
        try:
            # Format the updated YAML
            if USE_RUAMEL:
                from io import StringIO
                yaml_string = StringIO()
                yaml_parser.dump(frontmatter, yaml_string)
                updated_yaml = yaml_string.getvalue()
            else:
                updated_yaml = pyyaml.dump(frontmatter, default_flow_style=False, sort_keys=False)
                
            # Construct the updated file content
            updated_content = content[:start] + updated_yaml + content[end:]
            
            # Write the updated content to the file
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(updated_content)
                
        except Exception as e:
            logger.error(f"Error updating {file_path}: {str(e)}")
            self.stats['files_with_errors'] += 1


def add_nested_tags(directory: Path, dry_run: bool = False, verbose: bool = False) -> Dict[str, int]:
    """
    Add nested tags to markdown files in a directory.
    
    This function is the main entry point for the tag processing functionality
    and can be imported and used from other modules.
    
    Args:
        directory (Path): The directory to process.
        dry_run (bool): If True, no files will be modified.
        verbose (bool): If True, additional information will be logged.
        
    Returns:
        Dict[str, int]: Statistics about the processing.
    """
    # Process the directory
    processor = MarkdownFrontmatterProcessor(
        dry_run=dry_run, 
        verbose=verbose
    )
    
    logger.info(f"{'[DRY RUN] ' if dry_run else ''}Processing markdown files in: {directory}")
    stats = processor.process_directory(directory)
    return stats


def main() -> None:
    """
    Main entry point for the script.
    
    Parses command line arguments and calls the add_nested_tags function.
    """
    # Parse command line arguments
    parser = argparse.ArgumentParser(
        description='Process markdown files to add nested tags based on YAML frontmatter.'
    )
    parser.add_argument(
        'directory', nargs='?', default=None,
        help='Directory to process (defaults to Obsidian vault)'
    )
    parser.add_argument(
        '--dry-run', action='store_true',
        help='Do not modify files, just show what would be done'
    )
    parser.add_argument(
        '--verbose', action='store_true',
        help='Show more detailed information about processing'
    )
    
    args = parser.parse_args()
    
    # Get directory path
    if args.directory:
        directory = Path(args.directory)
    else:
        directory = Path(normalize_wsl_path(VAULT_LOCAL_ROOT))
        logger.info(f"No directory specified, using Obsidian vault: {directory}")
    
    # Validate directory
    if not directory.exists() or not directory.is_dir():
        logger.error(f"Directory not found or is not a directory: {directory}")
        sys.exit(1)
        
    # Process the directory
    stats = add_nested_tags(directory, dry_run=args.dry_run, verbose=args.verbose)
    
    # Print summary
    print("\nSummary:")
    print(f"Files processed: {stats['files_processed']}")
    print(f"Files modified: {stats['files_modified']}")
    print(f"Tags added: {stats['tags_added']}")
    print(f"Index files cleared: {stats['index_files_cleared']}")
    print(f"Files with errors: {stats['files_with_errors']}")
    
    if args.dry_run:
        print("\nThis was a dry run. No files were modified.")
    

if __name__ == "__main__":
    main()