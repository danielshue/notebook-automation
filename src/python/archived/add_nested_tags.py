DEPRECATED: This script has been migrated to mba_notebook_automation/cli/add_nested_tags.py
Please use the new CLI package version for all future work.
"""
#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Markdown YAML Frontmatter Tag Processor.

This script recursively scans a folder for markdown (.md) files, extracts fields 
from the YAML frontmatter, and converts them to nested tags in the format 
#field/value. These tags are then added to the existing tags field or a new tags
field is created if it doesn't exist.

Example:
    $ python add_nested_tags.py /path/to/folder --verbose
    $ python add_nested_tags.py /path/to/folder --dry-run

"""

"""
DEPRECATED: This script has been superseded by a new CLI version.
Please use the updated CLI in mba_notebook_automation/cli/add_nested_tags.py for future work.

General Markdown Tag Automation Tool
-----------------------------------
This script recursively scans a directory for markdown (.md) files, extracts fields from YAML frontmatter,
and generates nested tags in the format #field/value. Tags are added to the existing tags field or a new
tags field is created if missing.

Features:
- Recursively process markdown files in a directory
- Extract and convert YAML frontmatter fields to nested tags
- Supports dry-run and verbose output modes
- Preserves YAML formatting when ruamel.yaml is available

Usage Examples:
  python add_nested_tags.py /path/to/folder --verbose
  python add_nested_tags.py /path/to/folder --dry-run
  python add_nested_tags.py --help
"""

import argparse
import os
import re
import sys
from pathlib import Path
from typing import Dict, List, Set, Tuple, Optional, Any

# Import from tools package
from tools.utils.config import setup_logging, VAULT_LOCAL_ROOT
from tools.utils.paths import normalize_wsl_path

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
            dry_run (bool): If True, don't write changes to files
            verbose (bool): If True, print detailed information about changes
        """
        self.dry_run = dry_run
        self.verbose = verbose
        # Fields to extract from frontmatter and convert to nested tags
        self.tag_fields = [
            'type', 'course', 'program', 'term', 'status', 'category',
            'subject', 'priority', 'project', 'area', 'resource'
        ]        # Statistics tracking
        self.stats = {
            'files_processed': 0,
            'files_modified': 0,
            'files_with_errors': 0,
            'tags_added': 0,
            'index_files_cleared': 0
        }

    def extract_frontmatter(self, content: str) -> Tuple[Optional[Dict], str, str]:
        """
        Extract YAML frontmatter from markdown content.
        
        Args:
            content (str): The content of the markdown file
            
        Returns:
            tuple: (
                frontmatter_dict (dict or None): Parsed frontmatter as dict or None if no frontmatter,
                frontmatter_text (str): Raw frontmatter text,
                content_without_frontmatter (str): Remaining content after frontmatter
            )
        """
        # Match frontmatter between --- delimiters
        pattern = r'^---\s*\n(.*?)\n---\s*\n'
        match = re.match(pattern, content, re.DOTALL)
        
        if not match:
            return None, "", content
            
        frontmatter_text = match.group(0)
        frontmatter_yaml = match.group(1)
        remaining_content = content[len(frontmatter_text):]
        
        try:
            if USE_RUAMEL:
                from io import StringIO
                stream = StringIO(frontmatter_yaml)
                frontmatter_dict = yaml_parser.load(stream)
            else:
                frontmatter_dict = pyyaml.safe_load(frontmatter_yaml)
                
            if not isinstance(frontmatter_dict, dict):
                frontmatter_dict = {}
                
            return frontmatter_dict, frontmatter_text, remaining_content
        except Exception as e:
            logger.warning(f"Error parsing YAML frontmatter: {e}")
            return None, frontmatter_text, remaining_content

    def generate_nested_tags(self, frontmatter: Dict) -> Set[str]:
        """
        Generate nested tags from frontmatter fields.
        
        Args:
            frontmatter (dict): The frontmatter dictionary
            
        Returns:
            set: Set of generated nested tags
        """
        tags = set()
        
        for field in self.tag_fields:
            if field in frontmatter and frontmatter[field]:
                # Handle both string values and list values
                if isinstance(frontmatter[field], str):
                    # Skip empty strings
                    if frontmatter[field].strip():
                        tags.add(f"#{field}/{frontmatter[field].strip()}")
                elif isinstance(frontmatter[field], list):
                    for value in frontmatter[field]:
                        if value and isinstance(value, str) and value.strip():
                            tags.add(f"#{field}/{value.strip()}")
                
        return tags

    def update_frontmatter(self, frontmatter: Dict, new_tags: Set[str]) -> Tuple[Dict, int]:
        """
        Update frontmatter with new nested tags.
        
        Args:
            frontmatter (dict): The frontmatter dictionary
            new_tags (set): Set of new tags to add
            
        Returns:
            tuple: (updated_frontmatter, count_of_added_tags)
        """
        existing_tags = []
        tags_added = 0
        
        # Get existing tags
        if 'tags' in frontmatter:
            if isinstance(frontmatter['tags'], str):
                # Convert comma-separated string to list
                existing_tags = [tag.strip() for tag in frontmatter['tags'].split(',')]
            elif isinstance(frontmatter['tags'], list):
                existing_tags = frontmatter['tags']
        
        # Convert to set for easier deduplication
        existing_tags_set = set(existing_tags)
        
        # Find tags to add (those not already in existing tags)
        tags_to_add = new_tags - existing_tags_set
        tags_added = len(tags_to_add)
        
        # Update the tags in the frontmatter
        if tags_to_add:
            all_tags = list(existing_tags_set.union(new_tags))
            # Sort tags for consistency
            all_tags.sort()
            frontmatter['tags'] = all_tags
            
        return frontmatter, tags_added

    def process_file(self, filepath: Path) -> Dict[str, Any]:
        """
        Process a single markdown file.
        
        Args:
            filepath (Path): Path to the markdown file
            
        Returns:
            dict: Statistics about the processing of this file
        """
        file_stats = {
            'file': str(filepath),
            'tags_added': 0,
            'modified': False,
            'error': None
        }
        
        try:
            # Read the file content
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
                  # Extract frontmatter
            frontmatter, frontmatter_text, remaining_content = self.extract_frontmatter(content)
            
            if frontmatter is None:
                file_stats['error'] = "No valid YAML frontmatter found"
                return file_stats
            
            # Special handling for index files - clear any tags
            if 'index-type' in frontmatter:
                # If it's an index file, check if it has tags that need to be cleared
                had_tags = False                
                if 'tags' in frontmatter and frontmatter['tags']:
                    had_tags = True  # Flag that we found and will remove tags
                    frontmatter['tags'] = []  # Clear the tags
                    file_stats['modified'] = True
                    file_stats['tags_added'] = 0
                    file_stats['index_cleared'] = True  # Flag that this was an index file with tags cleared
                    
                    if not self.dry_run:
                        # Convert updated frontmatter back to YAML
                        if USE_RUAMEL:
                            from io import StringIO
                            string_stream = StringIO()
                            yaml_parser.dump(frontmatter, string_stream)
                            updated_yaml = string_stream.getvalue()
                        else:
                            updated_yaml = pyyaml.safe_dump(
                                frontmatter,
                                default_flow_style=False,
                                allow_unicode=True
                            )
                        
                        updated_content = f"---\n{updated_yaml}---\n{remaining_content}"
                        
                        # Write the updated content back to the file
                        with open(filepath, 'w', encoding='utf-8') as f:
                            f.write(updated_content)
                    
                    if self.verbose and had_tags:
                        print(f"\nFile: {filepath}")
                        print(f"Cleared tags from index file")
                
                return file_stats  # Skip further processing for index files
                
            # Generate nested tags from frontmatter fields for non-index files
            new_tags = self.generate_nested_tags(frontmatter)
            
            # Update frontmatter with new tags
            updated_frontmatter, tags_added = self.update_frontmatter(frontmatter, new_tags)
            file_stats['tags_added'] = tags_added
            
            # If tags were added, update the file
            if tags_added > 0:
                file_stats['modified'] = True
                
                if not self.dry_run:
                    # Convert updated frontmatter back to YAML
                    if USE_RUAMEL:
                        from io import StringIO
                        string_stream = StringIO()
                        yaml_parser.dump(updated_frontmatter, string_stream)
                        updated_yaml = string_stream.getvalue()
                    else:
                        updated_yaml = pyyaml.safe_dump(
                            updated_frontmatter, 
                            default_flow_style=False,
                            allow_unicode=True
                        )
                    
                    updated_content = f"---\n{updated_yaml}---\n{remaining_content}"
                    
                    # Write the updated content back to the file
                    with open(filepath, 'w', encoding='utf-8') as f:
                        f.write(updated_content)
                
                if self.verbose:
                    print(f"\nFile: {filepath}")
                    print(f"Added {tags_added} new tags:")
                    for tag in sorted(new_tags - set(updated_frontmatter.get('tags', []))):
                        print(f"  {tag}")
            
        except Exception as e:
            file_stats['error'] = str(e)
            logger.error(f"Error processing file {filepath}: {e}")
            
        return file_stats    
    
    def process_directory(self, directory: Path) -> Dict[str, int]:
        """
        Recursively process all markdown files in a directory.
        
        Args:
            directory (Path): Directory to process
            
        Returns:
            dict: Statistics about the processing
        """
        # Reset statistics
        self.stats = {
            'files_processed': 0,
            'files_modified': 0,
            'files_with_errors': 0,
            'tags_added': 0
        }
        
        # Walk through the directory recursively
        for root, _, files in os.walk(directory):
            for file in files:
                if file.lower().endswith('.md'):
                    filepath = Path(root) / file
                    
                    file_stats = self.process_file(filepath)
                    
                    # Update overall statistics
                    self.stats['files_processed'] += 1
                    if file_stats['modified']:
                        self.stats['files_modified'] += 1
                    if file_stats['error']:
                        self.stats['files_with_errors'] += 1
                        print(f"Error processing file {filepath}: {file_stats['error']}")
                    self.stats['tags_added'] += file_stats['tags_added']
        
        return self.stats


def main():
    """Main entry point for the script."""
    # Parse command line arguments
    parser = argparse.ArgumentParser(
        description="""
DEPRECATED: Use the new CLI version if available.

Automate the addition of nested tags to markdown files based on YAML frontmatter fields.
Recursively scans a directory, extracts frontmatter fields, and generates #field/value tags.

Features:
  - Recursively process markdown files
  - Extract and convert YAML frontmatter fields to nested tags
  - Supports dry-run and verbose output

Examples:
  python add_nested_tags.py /path/to/folder --verbose
  python add_nested_tags.py /path/to/folder --dry-run
        """
    )
    parser.add_argument(
        "directory",
        nargs="?",
        help="Directory to recursively scan for markdown files (default: configured vault root)"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Simulate changes without writing to files"
    )
    parser.add_argument(
        "--verbose", "-v",
        action="store_true",
        help="Show detailed information about tag processing"
    )
    
    args = parser.parse_args()
    
    # Use specified directory or default to VAULT_LOCAL_ROOT
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
    processor = MarkdownFrontmatterProcessor(
        dry_run=args.dry_run, 
        verbose=args.verbose
    )
    
    print(f"{'[DRY RUN] ' if args.dry_run else ''}Processing markdown files in: {directory}")
    stats = processor.process_directory(directory)
    
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
