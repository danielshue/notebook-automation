#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Markdown YAML Frontmatter Tag Processor.

This script recursively scans a folder for markdown (.md) files, extracts fields 
from the YAML frontmatter, and converts them to nested tags in the format 
#field/value. These tags are then added to the existing tags field or a new tags
field is created if it doesn't exist. All tags are enclosed in double quotes.

If a value contains spaces, they are replaced with dashes in the generated tag.
For example, if a field "course" has value "Data Science", the tag will be
"#course/Data-Science".

If a file has 'index-type' in its frontmatter, any tags will be removed as index
files should not have tags.

The script can also be used with --clear-all-tags to remove all tags from all files,
not just index files.

Example:
    $ python add_nested_tags_quoted.py /path/to/folder --verbose
    $ python add_nested_tags_quoted.py /path/to/folder --dry-run
    $ python add_nested_tags_quoted.py --verbose  # Uses default Obsidian vault path
    $ python add_nested_tags_quoted.py /path/to/folder --clear-all-tags  # Removes all tags

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
    from ruamel.yaml.scalarstring import DoubleQuotedScalarString
    USE_RUAMEL = True
    yaml_parser = YAML()
    yaml_parser.preserve_quotes = True
    yaml_parser.width = 4096  # To avoid line wrapping
    yaml_parser.default_flow_style = False
except ImportError:
    import yaml as pyyaml
    USE_RUAMEL = False
    logger.warning("ruamel.yaml not found, using pyyaml instead.")
    logger.warning("Install ruamel.yaml for better formatting preservation: pip install ruamel.yaml")


class MarkdownFrontmatterProcessor:
    """Process markdown files to extract and update YAML frontmatter tags."""

    def __init__(self, dry_run: bool = False, verbose: bool = False, clear_all_tags: bool = False):
        """
        Initialize the markdown frontmatter processor.
        
        Args:
            dry_run (bool): If True, don't write changes to files
            verbose (bool): If True, print detailed information about changes
            clear_all_tags (bool): If True, clear all tags from all files
        """
        self.dry_run = dry_run
        self.verbose = verbose
        self.clear_all_tags = clear_all_tags
        # Fields to extract from frontmatter and convert to nested tags
        self.tag_fields = [
            'type', 'course', 'program', 'term', 'status', 'category',
            'subject', 'priority', 'project', 'area', 'resource'
        ]
        # Statistics tracking
        self.stats = {
            'files_processed': 0,
            'files_modified': 0,
            'files_with_errors': 0,
            'tags_added': 0,
            'index_files_cleared': 0,
            'tags_cleared': 0
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
                        # Replace spaces with dashes in tag values
                        tag_value = frontmatter[field].strip().replace(' ', '-')
                        tags.add(f"#{field}/{tag_value}")
                elif isinstance(frontmatter[field], list):
                    for value in frontmatter[field]:
                        if value and isinstance(value, str) and value.strip():
                            # Replace spaces with dashes in tag values
                            tag_value = value.strip().replace(' ', '-')
                            tags.add(f"#{field}/{tag_value}")
                
        return tags

    def ensure_quoted_tags(self, tags_list: List[str]) -> List:
        """
        Ensure all tags are properly double-quoted.
        
        Args:
            tags_list (list): List of tag strings
            
        Returns:
            list: List with all tags properly quoted
        """
        if USE_RUAMEL:
            # For ruamel.yaml, use DoubleQuotedScalarString to ensure double quotes
            return [DoubleQuotedScalarString(tag) for tag in tags_list]
        else:
            # For PyYAML, we'll handle the quoting in the YAML dumper
            # Just return the original list as we'll use a custom representer
            return tags_list

    def update_frontmatter(self, frontmatter: Dict, new_tags: Set[str]) -> Tuple[Dict, int]:
        """
        Update frontmatter with new nested tags, ensuring they are double-quoted.
        
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
            # Ensure all tags are double-quoted
            frontmatter['tags'] = self.ensure_quoted_tags(all_tags)
            
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
            'error': None,
            'index_cleared': False,
            'tags_cleared': False
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
            
            # Check if this is an index file (has index-type key)
            is_index_file = 'index-type' in frontmatter
            
            # Special handling for index files - always clear tags regardless of whether they currently exist
            if is_index_file:
                # Clear tags for index files
                if 'tags' in frontmatter:
                    frontmatter['tags'] = []  # Clear the tags
                    file_stats['modified'] = True
                    file_stats['index_cleared'] = True
                    
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
                    
                    if self.verbose:
                        print(f"\nFile: {filepath}")
                        print(f"Cleared tags from index file")
                
                # Skip any further processing for index files to prevent adding new tags
                return file_stats
            
            # Check if we should clear all tags (only if clear_all_tags is True and it's not an index file)
            if self.clear_all_tags and 'tags' in frontmatter and frontmatter['tags']:
                # Flag that we found and will remove tags
                frontmatter['tags'] = []  # Clear the tags
                file_stats['modified'] = True
                file_stats['tags_cleared'] = True
                
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
                
                if self.verbose:
                    print(f"\nFile: {filepath}")
                    print(f"Cleared tags from file (--clear-all-tags)")
                
                return file_stats  # Skip further processing for files with cleared tags
                
            # Skip further processing if clear_all_tags is True (even if no tags were found)
            if self.clear_all_tags:
                return file_stats
                
            # For non-index files, generate nested tags from frontmatter fields
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
                        # Set up a custom string representer for PyYAML to enforce double quotes
                        def quoted_presenter(dumper, data):
                            return dumper.represent_scalar('tag:yaml.org,2002:str', data, style='"')
                        
                        # Apply the custom representer for strings in the tags list
                        if 'tags' in updated_frontmatter and updated_frontmatter['tags']:
                            orig_tags = updated_frontmatter['tags']
                            # Create a copy to preserve the original
                            tag_strings = []
                            for tag in orig_tags:
                                if not isinstance(tag, str):
                                    tag = str(tag)
                                tag_strings.append(tag)
                            
                            # Replace the tags with the string versions before dumping
                            updated_frontmatter['tags'] = tag_strings
                            
                            # Register the string presenter
                            pyyaml.add_representer(str, quoted_presenter)
                            
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
            'tags_added': 0,
            'index_files_cleared': 0,
            'tags_cleared': 0
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
                    
                    # Track different types of operations
                    if file_stats.get('index_cleared', False):
                        self.stats['index_files_cleared'] += 1
                    elif file_stats.get('tags_cleared', False):
                        self.stats['tags_cleared'] += 1
                    else:
                        self.stats['tags_added'] += file_stats['tags_added']
        
        return self.stats


def main():
    """Main entry point for the script."""
    # Parse command line arguments
    parser = argparse.ArgumentParser(
        description="Process markdown files to add nested tags based on frontmatter fields."
    )
    parser.add_argument(
        "directory", 
        nargs="?",
        help="Directory to recursively scan for markdown files (defaults to Obsidian vault)"
    )
    parser.add_argument(
        "--dry-run", 
        action="store_true", 
        help="Don't write changes to files, just simulate"
    )
    parser.add_argument(
        "--verbose", "-v", 
        action="store_true", 
        help="Print detailed information about changes"
    )
    parser.add_argument(
        "--clear-all-tags",
        action="store_true",
        help="Clear all tags from all files, not just index files"
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
        verbose=args.verbose,
        clear_all_tags=args.clear_all_tags
    )
    
    print(f"{'[DRY RUN] ' if args.dry_run else ''}{'[CLEAR ALL TAGS] ' if args.clear_all_tags else ''}Processing markdown files in: {directory}")
    stats = processor.process_directory(directory)
    
    # Print summary
    print("\nSummary:")
    print(f"Files processed: {stats['files_processed']}")
    print(f"Files modified: {stats['files_modified']}")
    
    # Show appropriate statistics based on mode
    if args.clear_all_tags:
        print(f"Regular files cleared: {stats['tags_cleared']}")
        print(f"Index files cleared: {stats['index_files_cleared']}")
        print(f"Total files cleared: {stats['tags_cleared'] + stats['index_files_cleared']}")
    else:
        print(f"Tags added: {stats['tags_added']}")
        print(f"Index files cleared: {stats['index_files_cleared']}")
    
    print(f"Files with errors: {stats['files_with_errors']}")
    
    if args.dry_run:
        print("\nThis was a dry run. No files were modified.")
    

if __name__ == "__main__":
    main()
