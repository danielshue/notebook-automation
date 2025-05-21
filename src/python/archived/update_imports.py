#!/usr/bin/env python3
"""
Import Statement Migration Tool for Notebook Automation

This script scans Python files and updates import statements to use the new
package structure. It can handle both basic imports and imports with aliasing.

Examples:
    # Update a single file
    python update_imports.py path/to/file.py
    
    # Update all Python files in a directory (recursively)
    python update_imports.py path/to/directory --recursive
    
    # Dry run to see what would be changed without making changes
    python update_imports.py path/to/file.py --dry-run
"""

import os
import re
import sys
import argparse
from pathlib import Path
from typing import List, Dict, Pattern, Optional


# Regular expression patterns for different types of imports
IMPORT_PATTERNS = [
    re.compile(r'^(\s*from\s+)(?:notebook_automation|tools)(\.[^\s]+\s+import\s+.+)$'),
    re.compile(r'^(\s*import\s+)(?:notebook_automation|tools)(\.[^\s]+)$'),
    
    # import tags
    re.compile(r'^(\s*import\s+)(tags)(\s*)$'),
    
    # from utilities import xyz
    re.compile(r'^(\s*from\s+)(utilities)(\s+import\s+.+)$'),
    
    # import utilities
    re.compile(r'^(\s*import\s+)(utilities)(\s*)$'),
    
    # from obsidian import xyz
    re.compile(r'^(\s*from\s+)(obsidian)(\s+import\s+.+)$'),
    
    # import obsidian
    re.compile(r'^(\s*import\s+)(obsidian)(\s*)$')
]


def update_file(file_path: str, dry_run: bool = False) -> int:
    """
    Update import statements in a file to use the new package structure.
    
    Args:
        file_path: Path to the file to update
        dry_run: If True, don't write changes to file
        
    Returns:
        Number of lines changed
    """
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
        
    lines = content.splitlines()
    changes = 0
      for i, line in enumerate(lines):
        for pattern in IMPORT_PATTERNS:
            match = pattern.match(line)
            if match:
                # Extract components
                prefix = match.group(1)
                suffix = match.group(2)
                if len(match.groups()) > 2:
                    trailing = match.group(3)
                else:
                    trailing = ""
                
                # Create updated line
                updated_line = f"{prefix}notebook_automation{suffix}{trailing}"
                
                if updated_line != line:
                    print(f"File: {file_path}, Line {i+1}")
                    print(f"  Old: {line}")
                    print(f"  New: {updated_line}")
                    lines[i] = updated_line
                    changes += 1
    
    if changes > 0 and not dry_run:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(lines))
            
        print(f"Updated {changes} imports in {file_path}")
    elif changes > 0:
        print(f"Would update {changes} imports in {file_path} (dry run)")
    
    return changes


def process_directory(directory_path: str, recursive: bool = False, dry_run: bool = False) -> int:
    """
    Process all Python files in a directory.
    
    Args:
        directory_path: Path to the directory to process
        recursive: If True, recursively process subdirectories
        dry_run: If True, don't write changes to files
        
    Returns:
        Total number of changes made
    """
    path = Path(directory_path)
    total_changes = 0
    
    # Get all Python files
    if recursive:
        python_files = list(path.glob('**/*.py'))
    else:
        python_files = list(path.glob('*.py'))
        
    print(f"Found {len(python_files)} Python files in {directory_path}")
    
    for file_path in python_files:
        # Skip files in the notebook_automation package directory
        if str(file_path).startswith(str(Path(directory_path) / 'notebook_automation')):
            continue
            
        changes = update_file(str(file_path), dry_run)
        total_changes += changes
        
    return total_changes


def main():
    """Main entry point for the script."""
    parser = argparse.ArgumentParser(
        description="Update import statements to use the new package structure."
    )
    parser.add_argument(
        "path", 
        help="Path to the file or directory to process"
    )
    parser.add_argument(
        "--recursive", "-r", action="store_true",
        help="Recursively process directories"
    )
    parser.add_argument(
        "--dry-run", "-n", action="store_true",
        help="Don't modify files, just show what would change"
    )
    
    args = parser.parse_args()
    path = args.path
    recursive = args.recursive
    dry_run = args.dry_run
    
    # Check if the path exists
    if not os.path.exists(path):
        print(f"Error: Path not found: {path}")
        return 1
        
    # Process based on whether the path is a file or directory
    if os.path.isfile(path):
        if not path.endswith('.py'):
            print(f"Error: {path} is not a Python file")
            return 1
            
        changes = update_file(path, dry_run)
        if changes == 0:
            print(f"No changes needed in {path}")
    else:
        changes = process_directory(path, recursive, dry_run)
        if changes == 0:
            print(f"No changes needed in any files in {path}")
    
    # Print summary
    if changes > 0:
        if dry_run:
            print(f"Would update {changes} import statements (dry run)")
        else:
            print(f"Updated {changes} import statements")
    
    return 0


if __name__ == "__main__":
    sys.exit(main())
