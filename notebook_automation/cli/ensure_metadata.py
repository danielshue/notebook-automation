#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Consistent Metadata Enforcer for Markdown Files

This script recursively scans a folder structure for markdown (.md) files and ensures 
that each file has consistent program, course, and class metadata in its YAML frontmatter.
The script determines the correct values based on the file's location in the directory
structure and the index files found in parent directories.

Directory Structure Expected:
- Root (main-index)
  - Program Folders (program-index)
    - Course Folders (course-index)
      - Class Folders (class-index)
        - Case Study Folders (case-study-index)
        - Module Folders (module-index)
          - Live Session Folder (live-session-index)
          - Lesson Folders (lesson-index)
            - Content Files (readings, videos, transcripts, etc.)

The script will find the appropriate program, course, and class for each markdown file
by looking for index files in parent directories, and update the YAML frontmatter
accordingly.

Example:
    $ python ensure_consistent_metadata.py /path/to/vault --verbose
    $ python ensure_consistent_metadata.py /path/to/vault --dry-run
    $ python ensure_consistent_metadata.py --verbose  # Uses default Obsidian vault path

"""

import argparse
import os
import re
import sys
from pathlib import Path
from typing import Dict, List, Optional, Tuple, Union, Any

# Import from tools package
from notebook_automation.tools.utils.config import setup_logging, VAULT_LOCAL_ROOT
from notebook_automation.tools.utils.paths import normalize_wsl_path

# Set up logging
logger, failed_logger = setup_logging(debug=False)

# Try to import ruamel.yaml for better YAML formatting preservation
try:
    from ruamel.yaml import YAML
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


class MetadataUpdater:
    """Updates markdown files with consistent metadata based on directory structure."""

    def __init__(self, dry_run: bool = False, verbose: bool = False):
        """
        Initialize the metadata updater.
        
        Args:
            dry_run (bool): If True, don't write changes to files
            verbose (bool): If True, print detailed information about changes
        """
        self.dry_run = dry_run
        self.verbose = verbose
        self.stats = {
            'files_processed': 0,
            'files_modified': 0,
            'files_with_errors': 0,
            'program_updated': 0,
            'course_updated': 0,
            'class_updated': 0
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
            # First try to parse with standard YAML parser (will fail on duplicates)
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
            # Check if this is a duplicate key error
            if "found duplicate key" in str(e):
                logger.warning(f"Found duplicate keys in frontmatter, attempting to fix: {e}")
                # Manual parsing to handle duplicate keys (keeping the last occurrence)
                frontmatter_dict = self._parse_frontmatter_with_duplicates(frontmatter_yaml)
                return frontmatter_dict, frontmatter_text, remaining_content            
            else:
                logger.warning(f"Error parsing YAML frontmatter: {e}")
                return None, frontmatter_text, remaining_content
            
    def _parse_frontmatter_with_duplicates(self, yaml_content: str) -> Dict[str, Any]:
        """Parse YAML frontmatter manually, handling duplicate keys by keeping the last occurrence.
        
        This function provides a fallback parsing method when the standard YAML parser
        fails due to duplicate keys in the frontmatter. It implements a simple line-by-line
        parser that extracts key-value pairs while overwriting previous values when
        duplicate keys are encountered.
        
        Args:
            yaml_content (str): The YAML content to parse as a string
            
        Returns:
            Dict[str, Any]: Parsed frontmatter as dictionary with all keys normalized
                and duplicate keys resolved (keeping the last occurrence)
                
        Example:
            >>> yaml_text = "title: Document\\ntags: [tag1]\\ntitle: Updated Title"
            >>> updater = MetadataUpdater()
            >>> updater._parse_frontmatter_with_duplicates(yaml_text)
            {'title': 'Updated Title', 'tags': ['tag1']}
        """
        result = {}
        # Simple line-by-line parser to handle duplicate keys
        lines = yaml_content.split('\n')
        for line in lines:
            line = line.strip()
            if not line or line.startswith('#'):
                continue
                
            # Look for key-value pairs (handling both "key: value" and "key:value" formats)
            match = re.match(r'^([^:]+):\s*(.*)$', line)
            if match:
                key = match.group(1).strip()
                value = match.group(2).strip()
                
                # If this is a duplicate key, we'll just overwrite the previous value
                if key in result:
                    logger.warning(f"Duplicate key '{key}' found in frontmatter, using the last value: '{value}'")
                  # Handle quoted strings
                if (value.startswith('"') and value.endswith('"')) or \
                   (value.startswith("'") and value.endswith("'")):
                    value = value[1:-1]
                
                result[key] = value
        
        return result
        
    def find_parent_index_info(self, file_path: Path) -> dict:
        """
        Find program, course, and class information by scanning parent directories for index files.
        
        Args:
            file_path (Path): Path to the markdown file
            
        Returns:
            dict: Dictionary with program, course, and class information
        """
        info = {
            'program': None,
            'course': None,
            'class': None
        }
        
        # Start from the file's directory and move up the tree
        current_dir = file_path.parent
        root_path = Path(VAULT_LOCAL_ROOT).resolve()
        
        # IMPORTANT: For programs, we want the highest level (closest to root)
        # For courses and classes, we want the closest to the file (deepest level)
        highest_program_dir = None  # Track the highest directory with program-index
        course_level = -1
        class_level = -1
        current_level = 0
        
        while current_dir.resolve() >= root_path:
            # Look for index files in the current directory
            index_files_found = list(current_dir.glob('*.md'))
            
            for index_file in index_files_found:
                try:
                    with open(index_file, 'r', encoding='utf-8') as f:
                        content = f.read()
                    
                    frontmatter, _, _ = self.extract_frontmatter(content)
                    if frontmatter and 'index-type' in frontmatter:
                        index_type = frontmatter.get('index-type')
                        dir_name = current_dir.name
                        
                        # For program, we want the highest level (closest to root)
                        if index_type == 'program-index':
                            # Only update if we haven't found a program yet, or if this is higher in the hierarchy
                            if highest_program_dir is None or current_dir.resolve().parents >= highest_program_dir.resolve().parents:
                                highest_program_dir = current_dir
                                info['program'] = frontmatter.get('title') or dir_name
                                if self.verbose:
                                    logger.info(f"Found program: {info['program']} at {current_dir}")
                        
                        # For course and class, we want the deepest level (closest to the file)
                        elif index_type == 'course-index' and current_level > course_level:
                            course_level = current_level
                            info['course'] = frontmatter.get('title') or dir_name
                            if self.verbose:
                                logger.info(f"Found course: {info['course']} at {current_dir}")
                        
                        elif index_type == 'class-index' and current_level > class_level:
                            class_level = current_level
                            info['class'] = frontmatter.get('title') or dir_name
                            if self.verbose:
                                logger.info(f"Found class: {info['class']} at {current_dir}")
                except Exception as e:
                    logger.warning(f"Error processing index file {index_file}: {e}")
              # Move up to the parent directory
            current_dir = current_dir.parent
            current_level += 1
            
        # Debug logging to help understand the hierarchy
        if self.verbose:
            logger.info(f"Final metadata info: Program='{info['program']}', Course='{info['course']}', Class='{info['class']}'")
            
        # If program is still None but we have a reasonable guess based on directory structure
        if info['program'] is None and file_path.parts:
            # Look in the path structure for potential program names
            # We assume programs are at the base of the Notebook directory
            mba_parts = []
            for part in file_path.parts:
                if part.lower() == "mba":
                    mba_parts.append(part)
                    break
                mba_parts.append(part)
              # If we have parts after MBA, the first one might be our program
            if len(mba_parts) > 0 and "MBA" in [p.upper() for p in mba_parts]:
                idx = [p.upper() for p in mba_parts].index("MBA")
                if idx+1 < len(file_path.parts):
                    potential_program = file_path.parts[idx+1]
                    info['program'] = potential_program
                    if self.verbose:
                        logger.info(f"No program-index found, using directory name as fallback: {potential_program}")
                    
                    # Try to find course from the next directory level after program
                    if idx+2 < len(file_path.parts) and info['course'] is None:
                        potential_course = file_path.parts[idx+2]
                        info['course'] = potential_course
                        if self.verbose:
                            logger.info(f"No course-index found, using directory name as fallback: {potential_course}")
                        
                        # Try to find class from the next directory level after course
                        if idx+3 < len(file_path.parts) and info['class'] is None:
                            potential_class = file_path.parts[idx+3]
                            info['class'] = potential_class
                            if self.verbose:
                                logger.info(f"No class-index found, using directory name as fallback: {potential_class}")
            # If we couldn't find MBA in the path but we still need to guess
            elif len(file_path.parts) >= 3 and info['program'] is None:
                # For paths without MBA marker, try to infer structure based on path depth
                # Assuming the structure might be:
                # [0: accounting-for-managers]/[1: course]/[2: lesson]/[3: file]
                info['class'] = file_path.parts[0]
                if self.verbose:
                    logger.info(f"No MBA in path, using first dir as fallback class: {info['class']}")
                
                # Use second dir component for course if available
                if len(file_path.parts) > 1:
                    course_name = file_path.parts[1]
                    # Clean up the course name by removing numbers and underscores
                    clean_course = re.sub(r'^\d+_', '', course_name)
                    clean_course = clean_course.replace('_', ' ').replace('-', ' ')
                    info['course'] = clean_course
                    if self.verbose:
                        logger.info(f"Using second dir component as fallback course: {info['course']}")
                
                # Default program if nothing else works
                if info['program'] is None:
                    info['program'] = "MBA Program"
                    if self.verbose:
                        logger.info(f"No program identifier found, using default: {info['program']}")
        
        return info

    def update_frontmatter(self, frontmatter: Dict, metadata_info: Dict) -> Tuple[Dict, Dict]:
        """
        Update frontmatter with consistent program, course, and class metadata.
        
        Args:
            frontmatter (dict): The frontmatter dictionary
            metadata_info (dict): Dictionary with program, course, and class information
            
        Returns:
            tuple: (updated_frontmatter, update_stats)
        """
        update_stats = {
            'program_updated': False,
            'course_updated': False,
            'class_updated': False
        }
        
        # Update program if needed and if we found program info
        if metadata_info['program'] and (not frontmatter.get('program') or 
                                        frontmatter.get('program') != metadata_info['program']):
            frontmatter['program'] = metadata_info['program']
            update_stats['program_updated'] = True
        
        # Update course if needed and if we found course info
        if metadata_info['course'] and (not frontmatter.get('course') or 
                                       frontmatter.get('course') != metadata_info['course']):
            frontmatter['course'] = metadata_info['course']
            update_stats['course_updated'] = True
        
        # Update class if needed and if we found class info
        if metadata_info['class'] and (not frontmatter.get('class') or 
                                      frontmatter.get('class') != metadata_info['class']):
            frontmatter['class'] = metadata_info['class']
            update_stats['class_updated'] = True
            
        return frontmatter, update_stats

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
            'modified': False,
            'error': None,
            'program_updated': False,
            'course_updated': False,
            'class_updated': False
        }
        
        try:
            # Read the file content
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
                
            # Extract frontmatter
            frontmatter, frontmatter_text, remaining_content = self.extract_frontmatter(content)
            
            if frontmatter is None:
                # Create new frontmatter if none exists
                frontmatter = {}
            
            # Skip processing for index files (we don't want to modify them)
            if 'index-type' in frontmatter:
                if self.verbose:
                    logger.info(f"Skipping index file: {filepath}")
                return file_stats
            
            # Find parent directory info
            metadata_info = self.find_parent_index_info(filepath)
            
            # Update frontmatter with consistent metadata
            updated_frontmatter, update_stats = self.update_frontmatter(frontmatter, metadata_info)
            
            # Update file stats
            file_stats.update(update_stats)
            
            # Check if any updates were made
            if any(update_stats.values()):
                file_stats['modified'] = True
                
                # Only update the file if not in dry run mode
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
                    
                    # Check if we had existing frontmatter or need to create it
                    if frontmatter_text:
                        # Replace existing frontmatter
                        updated_content = f"---\n{updated_yaml}---\n{remaining_content}"
                    else:
                        # Create new frontmatter
                        updated_content = f"---\n{updated_yaml}---\n\n{content}"
                    
                    # Write the updated content back to the file
                    with open(filepath, 'w', encoding='utf-8') as f:
                        f.write(updated_content)
                
                # Output information if verbose mode is enabled
                if self.verbose:
                    print(f"\nFile: {filepath}")
                    updates = []
                    if update_stats['program_updated']:
                        updates.append(f"Program: {metadata_info['program']}")
                    if update_stats['course_updated']:
                        updates.append(f"Course: {metadata_info['course']}")
                    if update_stats['class_updated']:
                        updates.append(f"Class: {metadata_info['class']}")
                    print(f"Updated: {', '.join(updates)}")
            
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
            'program_updated': 0,
            'course_updated': 0,
            'class_updated': 0
        }
        
        # Walk through the directory recursively
        for root, _, files in os.walk(directory):
            for file in files:
                if file.lower().endswith('.md'):
                    filepath = Path(root) / file
                    
                    if self.verbose:
                        print(f"Processing: {filepath.relative_to(directory)}")
                    
                    file_stats = self.process_file(filepath)
                    
                    # Update overall statistics
                    self.stats['files_processed'] += 1
                    if file_stats['modified']:
                        self.stats['files_modified'] += 1
                    if file_stats['error']:
                        self.stats['files_with_errors'] += 1
                        print(f"Error processing file {filepath}: {file_stats['error']}")
                    if file_stats['program_updated']:
                        self.stats['program_updated'] += 1
                    if file_stats['course_updated']:
                        self.stats['course_updated'] += 1
                    if file_stats['class_updated']:
                        self.stats['class_updated'] += 1
        
        return self.stats


def main():
    """Main entry point for the script."""
    # Parse command line arguments
    parser = argparse.ArgumentParser(
        description="Ensure consistent metadata in markdown files based on directory structure."
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
    updater = MetadataUpdater(
        dry_run=args.dry_run,
        verbose=args.verbose
    )
    
    print(f"{'[DRY RUN] ' if args.dry_run else ''}Ensuring consistent metadata in markdown files in: {directory}")
    stats = updater.process_directory(directory)
    
    # Print summary
    print("\nSummary:")
    print(f"Files processed: {stats['files_processed']}")
    print(f"Files modified: {stats['files_modified']}")
    print(f"Program fields updated: {stats['program_updated']}")
    print(f"Course fields updated: {stats['course_updated']}")
    print(f"Class fields updated: {stats['class_updated']}")
    print(f"Files with errors: {stats['files_with_errors']}")
    
    if args.dry_run:
        print("\nThis was a dry run. No files were modified.")
    

if __name__ == "__main__":
    main()