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
import difflib
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


def normalize_path(path_str: str, allow_file: bool = False) -> Path:
    """
    Normalize a path string, prepending the Vault Root if necessary.
    
    This function checks if the provided path is relative or absolute,
    and prepends the Vault Root if it's relative. It then verifies
    that the path exists and is a directory (or file if allow_file is True).
    
    Args:
        path_str (str): The path string to normalize.
        allow_file (bool): Whether to allow the path to be a file. Default is False.
        
    Returns:
        Path: A resolved Path object with absolute path.
        
    Raises:
        ValueError: If the resulting path doesn't exist or is not a directory/file.
    """
    # Normalize path separators (handle both / and \)
    path_str = os.path.normpath(path_str)
    
    # Create a Path object
    path = Path(path_str)
    
    # Check if this is an absolute path
    was_relative = False
    if not path.is_absolute():
        # If it's a relative path, prepend the Vault Root
        notebook_vault_root = Path(normalize_wsl_path(VAULT_LOCAL_ROOT))
        path = notebook_vault_root / path
        was_relative = True
    
    # Resolve any symlinks, .. references, etc.
    path = path.resolve()
    
    # Check if the path exists
    if not path.exists():
        if was_relative:
            # Try to find matching directories recursively
            potential_matches = find_matching_directories(path_str)
            
            if potential_matches:
                suggestion_msg = "\nDid you mean one of these?\n" + "\n".join(f"- {p}" for p in potential_matches[:5])
                if len(potential_matches) > 5:
                    suggestion_msg += f"\n(and {len(potential_matches) - 5} more...)"
                
                raise ValueError(f"Path does not exist: {path}\n"
                                f"The relative path '{path_str}' was not found inside the Vault Root '{VAULT_LOCAL_ROOT}'"
                                f"{suggestion_msg}")
            else:
                raise ValueError(f"Path does not exist: {path}\n"
                                f"The relative path '{path_str}' was not found inside the Vault Root '{VAULT_LOCAL_ROOT}'")
        else:
            raise ValueError(f"Path does not exist: {path}")
    
    # Check if it's a directory or file
    if allow_file:
        if not path.is_dir() and not path.is_file():
            raise ValueError(f"Path exists but is neither a directory nor a file: {path}")
    else:
        if not path.is_dir():
            raise ValueError(f"Path exists but is not a directory: {path}")
        
    return path


def find_matching_directories(target_dir_name: str) -> list:
    """
    Find directories that match or contain parts of the target directory name.
    
    This function searches for directories in the vault that might match what the user 
    is looking for, helping to provide suggestions when a path is not found.
    
    Args:
        target_dir_name (str): The directory name to search for.
        
    Returns:
        list: A list of potential matching directory paths (relative to vault root).
    """
    notebook_vault_root = Path(normalize_wsl_path(VAULT_LOCAL_ROOT))
    matches = []
    
    # Split the target into parts for partial matching
    target_parts = target_dir_name.lower().replace('\\', '/').split('/')
    last_part = target_parts[-1].strip() if target_parts else ""
    
    # Walk through the vault directory
    for root, dirs, _ in os.walk(notebook_vault_root):
        for dir_name in dirs:
            # Match based on the last part of the path
            if (last_part and last_part in dir_name.lower()) or (target_dir_name.lower() in dir_name.lower()):
                rel_path = os.path.relpath(os.path.join(root, dir_name), notebook_vault_root)
                # Convert to forward slashes for consistency
                rel_path = rel_path.replace('\\', '/')
                # Don't include dot paths
                if not rel_path.startswith('.'):
                    matches.append(rel_path)
    
    return sorted(matches)



class MetadataUpdater:

    def __init__(self, dry_run: bool = False, verbose: bool = False, program: str = None):
        """
        Initialize the metadata updater.
        
        Args:
            dry_run (bool): If True, don't write changes to files
            verbose (bool): If True, print detailed information about changes
            program (str, optional): If provided, override program detection and use this value
        """
        self.dry_run = dry_run
        self.verbose = verbose
        self.program_override = program
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
        try:
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
                    logger.warning(f"Error parsing YAML frontmatter, falling back to manual parsing: {e}")
                    # Try manual parsing as a fallback for any YAML error
                    frontmatter_dict = self._parse_frontmatter_with_duplicates(frontmatter_yaml)
                    return frontmatter_dict, frontmatter_text, remaining_content
        except Exception as e:
            # Catch-all for any other errors in the extraction process
            logger.warning(f"Error extracting frontmatter: {e}")
            return {}, "", content
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
            'program': self.program_override,  # Use the override if provided
            'course': None,
            'class': None
        }
        
        # If program is explicitly provided, we don't need special path handling for program
        if self.program_override:
            if self.verbose:
                logger.info(f"Using explicit program override: {self.program_override}")
        
        # Special case handling for Value Chain Management
        path_str = str(file_path)
        
        # Detect Value Chain Management in the path, prioritizing it over other detection methods
        if not self.program_override and ("Value Chain Management" in path_str):
            # Set the program explicitly
            info['program'] = "Value Chain Management"
            if self.verbose:
                logger.info(f"Found 'Value Chain Management' in path, using it as program name")
            
            # Extract course and class info from the path structure
            parts = path_str.split(os.sep)
            
            if "Value Chain Management" in parts:
                vcm_idx = parts.index("Value Chain Management")
                
                # Check if this is the "01_Projects" structure which has additional levels 
                if vcm_idx + 1 < len(parts) and "01_Projects" == parts[vcm_idx + 1]:
                    # Skip the "01_Projects" level and use the next one for course
                    if vcm_idx + 2 < len(parts):
                        info['course'] = parts[vcm_idx + 2]
                        if self.verbose:
                            logger.info(f"Found course in Value Chain Management path: {info['course']}")
                        
                        # Class would be the next level
                        if vcm_idx + 3 < len(parts):
                            info['class'] = parts[vcm_idx + 3]
                            if self.verbose:
                                logger.info(f"Found class in Value Chain Management path: {info['class']}")
                else:
                    # Normal case - course is directly after VCM in the path
                    if vcm_idx + 1 < len(parts):
                        info['course'] = parts[vcm_idx + 1]
                        if self.verbose:
                            logger.info(f"Found course in Value Chain Management path: {info['course']}")
                        
                        # Class would be the next level
                        if vcm_idx + 2 < len(parts):
                            info['class'] = parts[vcm_idx + 2]
                            if self.verbose:
                                logger.info(f"Found class in Value Chain Management path: {info['class']}")
            
            if self.verbose:
                logger.info(f"Value Chain Management path analysis: program='{info['program']}', course='{info['course']}', class='{info['class']}'")
            
            # Return early since we've determined the hierarchy for VCM
            return info
        # Start from the file's directory and move up the tree
        current_dir = file_path.parent
        root_path = Path(VAULT_LOCAL_ROOT).resolve()
        
        # IMPORTANT: For programs, we want the highest level (closest to root)
        # For courses and classes, we want the closest to the file (deepest level)
        highest_program_dir = None  # Track the highest directory with program-index
        course_level = -1
        class_level = -1
        current_level = 0
        
        # Look through index files in parent directories
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
                        # But don't override existing Value Chain Management setting
                        if index_type == 'program-index' and info['program'] is None:
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
        
        # If program is still None but we have a reasonable guess based on directory structure
        if info['program'] is None and file_path.parts and not self.program_override:
            # Use only the folders between the vault root and the file (excluding the filename)
            try:
                relevant = file_path.relative_to(VAULT_LOCAL_ROOT).parts[:-1]
            except Exception:
                # Fallback: previous logic
                parts = list(file_path.parts)
                skip_folders = {'01_Projects'}
                filtered = [p for p in parts if p not in skip_folders]
                relevant = filtered

            # Walk up the directory tree from the file's parent, looking for program-index.md
            program_title = None
            current_dir = file_path.parent
            root_path = Path(VAULT_LOCAL_ROOT).resolve()
            while current_dir != root_path.parent:
                program_index = current_dir / "program-index.md"
                if program_index.exists():
                    try:
                        with open(program_index, 'r', encoding='utf-8') as f:
                            content = f.read()
                        frontmatter, _, _ = self.extract_frontmatter(content)
                        if frontmatter and 'title' in frontmatter:
                            program_title = frontmatter['title']
                            break
                    except Exception:
                        pass
                current_dir = current_dir.parent
            if program_title:
                info['program'] = program_title
            # Always assign course and class as the next two folders after program, if available
            if len(relevant) >= 1:
                info['program'] = info['program'] or relevant[0]
            if len(relevant) >= 2:
                info['course'] = relevant[1]
            if len(relevant) >= 3:
                info['class'] = relevant[2]
            if self.verbose:
                logger.info(f"Path fallback: program='{info['program']}', course='{info['course']}', class='{info['class']}'")
            if len(relevant) == 0:
                # Default program if nothing else works
                info['program'] = "MBA Program"
                if self.verbose:
                    logger.info(f"No program identifier found, using default: {info['program']}")
        
        # Debug logging to help understand the final hierarchy
        if self.verbose:
            logger.info(f"Final metadata info: Program='{info['program']}', Course='{info['course']}', Class='{info['class']}'")
        
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
    
    def inspect_file(self, filepath: Path) -> Dict[str, Any]:
        """
        Inspect a single markdown file and return its metadata information without modifying it.
        
        Args:
            filepath (Path): Path to the markdown file to inspect
            
        Returns:
            dict: Dictionary with program, course, and class information
        """
        try:
            # Check if it's a markdown file
            if not str(filepath).lower().endswith('.md'):
                return {
                    'file': str(filepath),
                    'error': "Not a markdown file (must have .md extension)",
                    'program': None,
                    'course': None, 
                    'class': None,
                    'frontmatter': None
                }
            
            # Read the file content
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Extract frontmatter
            frontmatter, _, _ = self.extract_frontmatter(content)
            
            # Find metadata info from directory structure
            metadata_info = self.find_parent_index_info(filepath)
            
            # Combine existing frontmatter with directory-based info
            result = {
                'file': str(filepath),
                'program': frontmatter.get('program') if frontmatter else None,
                'course': frontmatter.get('course') if frontmatter else None,
                'class': frontmatter.get('class') if frontmatter else None,
                'frontmatter': frontmatter,
                'dir_program': metadata_info['program'],
                'dir_course': metadata_info['course'],
                'dir_class': metadata_info['class'],
            }
            
            return result
            
        except Exception as e:
            return {
                'file': str(filepath),
                'error': str(e),
                'program': None,
                'course': None,
                'class': None,
                'frontmatter': None
            }
    
def main():
    """Main entry point for the script."""
    # Parse command line arguments
    parser = argparse.ArgumentParser(
        description="Ensure consistent metadata in markdown files based on directory structure."
    )
    parser.add_argument(
        "path", 
        nargs="?",
        help="Path to process - can be a directory (to recursively scan all .md files) "
             "or a single .md file to check/update. Can be either an absolute path or "
             "a path relative to the Vault Root. Defaults to the entire Obsidian vault."
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
        "--inspect", "-i",
        action="store_true",
        help="Just inspect and display metadata without making changes"
    )
    parser.add_argument(
        "--program",
        help="Explicitly set program name for files (e.g., 'Value Chain Management')"
    )
    
    args = parser.parse_args()
      
    # Create the updater instance
    updater = MetadataUpdater(
        dry_run=args.dry_run or args.inspect,  # Don't modify files in inspect mode
        verbose=args.verbose,
        program=args.program  # Pass the program argument to the updater
    )
    
    # Use specified path or default to VAULT_LOCAL_ROOT
    if args.path:
        try:
            # Use the normalize_path function to handle both absolute and relative paths
            # Allow file paths in addition to directories
            path = normalize_path(args.path, allow_file=True)
            logger.info(f"Using path: {path}")
            # Determine if it's a file or directory
            is_file = path.is_file()
        except ValueError as e:
            logger.error(f"Error with path: {e}")
            sys.exit(1)
    else:
        # If no path is given, use the root of the vault
        path = Path(normalize_wsl_path(VAULT_LOCAL_ROOT))
        logger.info(f"No path specified, using Obsidian vault: {path}")
        # Determine if it's a file or directory
        is_file = path.is_file()
    
    # Handle inspection mode
    if args.inspect:
        if is_file:
            # Inspect a single file
            print(f"Inspecting metadata for: {path}")
            metadata = updater.inspect_file(path)
            print("\nFile Metadata Report:")
            print(f"File: {metadata['file']}")
            print("\nCurrent Frontmatter Values:")
            print(f"  Program: {metadata['program'] or 'Not set'}")
            print(f"  Course:  {metadata['course'] or 'Not set'}")
            print(f"  Class:   {metadata['class'] or 'Not set'}")
            print("\nDirectory Structure Values:")
            print(f"  Program: {metadata['dir_program'] or 'Not found'}")
            print(f"  Course:  {metadata['dir_course'] or 'Not found'}")
            print(f"  Class:   {metadata['dir_class'] or 'Not found'}")
            print("\nDiscrepancies:")
            if metadata['program'] != metadata['dir_program']:
                print(f"  Program: '{metadata['program']}' in file vs '{metadata['dir_program']}' from directory")
            if metadata['course'] != metadata['dir_course']:
                print(f"  Course: '{metadata['course']}' in file vs '{metadata['dir_course']}' from directory")
            if metadata['class'] != metadata['dir_class']:
                print(f"  Class: '{metadata['class']}' in file vs '{metadata['dir_class']}' from directory")
            if (metadata['program'] == metadata['dir_program'] and 
                metadata['course'] == metadata['dir_course'] and 
                metadata['class'] == metadata['dir_class']):
                print("  None - metadata is consistent with directory structure")
            if 'error' in metadata and metadata['error']:
                print(f"\nError: {metadata['error']}")
        else:
            # Inspect all markdown files in the directory recursively
            print(f"Inspecting all markdown files in: {path}\n")
            for root, _, files in os.walk(path):
                for file in files:
                    if file.lower().endswith('.md'):
                        filepath = Path(root) / file
                        metadata = updater.inspect_file(filepath)
                        print("File Metadata Report:")
                        print(f"File: {metadata['file']}")
                        print("Current Frontmatter Values:")
                        print(f"  Program: {metadata['program'] or 'Not set'}")
                        print(f"  Course:  {metadata['course'] or 'Not set'}")
                        print(f"  Class:   {metadata['class'] or 'Not set'}")
                        print("Directory Structure Values:")
                        print(f"  Program: {metadata['dir_program'] or 'Not found'}")
                        print(f"  Course:  {metadata['dir_course'] or 'Not found'}")
                        print(f"  Class:   {metadata['dir_class'] or 'Not found'}")
                        print("Discrepancies:")
                        if metadata['program'] != metadata['dir_program']:
                            print(f"  Program: '{metadata['program']}' in file vs '{metadata['dir_program']}' from directory")
                        if metadata['course'] != metadata['dir_course']:
                            print(f"  Course: '{metadata['course']}' in file vs '{metadata['dir_course']}' from directory")
                        if metadata['class'] != metadata['dir_class']:
                            print(f"  Class: '{metadata['class']}' in file vs '{metadata['dir_class']}' from directory")
                        if (metadata['program'] == metadata['dir_program'] and 
                            metadata['course'] == metadata['dir_course'] and 
                            metadata['class'] == metadata['dir_class']):
                            print("  None - metadata is consistent with directory structure")
                        if 'error' in metadata and metadata['error']:
                            print(f"Error: {metadata['error']}")
                        print("-")
    else:
        # Process a single file or an entire directory
        if is_file:
            print(f"{'[DRY RUN] ' if args.dry_run else ''}Processing file: {path}")
            file_stats = updater.process_file(path)
            if file_stats['error']:
                print(f"Error: {file_stats['error']}")
            elif file_stats['modified']:
                print("File was modified with the following updates:")
                if file_stats['program_updated']:
                    print("  - Program field updated")
                if file_stats['course_updated']:
                    print("  - Course field updated")
                if file_stats['class_updated']:
                    print("  - Class field updated")
            else:
                print("No updates were needed - file already has correct metadata")
        else:
            print(f"{'[DRY RUN] ' if args.dry_run else ''}Ensuring consistent metadata in markdown files in: {path}")
            stats = updater.process_directory(path)
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