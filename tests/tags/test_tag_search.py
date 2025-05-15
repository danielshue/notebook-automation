#!/usr/bin/env python3
"""
Simple test script to check if any files have tags.
"""

import os
import re
import sys

def main():
    """Check if any markdown files have tags."""
    vault_path = "/mnt/d/Vault/01_Projects/MBA"
    
    if not os.path.exists(vault_path):
        print(f"ERROR: Path does not exist: {vault_path}")
        return
    
    print(f"Checking for tags in: {vault_path}")
    
    # List directories to ensure we can see the content
    print("\nDirectories in vault path:")
    try:
        for item in os.listdir(vault_path):
            path = os.path.join(vault_path, item)
            if os.path.isdir(path):
                print(f"  - {item}/")
            elif os.path.isfile(path):
                print(f"  - {item}")
    except Exception as e:
        print(f"Error listing directories: {e}")
    
    found_files = 0
    checked_files = 0
    
    for root, _, files in os.walk(vault_path):
        for file in sorted(files):
            if file.endswith('.md'):
                checked_files += 1
                
                if checked_files > 100:  # Limit to first 100 files for speed
                    print(f"Stopping after checking {checked_files} files")
                    break
                
                path = os.path.join(root, file)
                
                try:
                    with open(path, 'r', encoding='utf-8') as f:
                        content = f.read(5000)  # Read just the first 5000 bytes
                        
                        # Check for tags in YAML frontmatter
                        if 'tags:' in content.lower():
                            print(f"Found YAML tags in: {os.path.relpath(path, vault_path)}")
                            found_files += 1
                        
                        # Check for inline tags
                        elif re.search(r'#([a-zA-Z0-9_/\-]+)', content):
                            print(f"Found inline tags in: {os.path.relpath(path, vault_path)}")
                            found_files += 1
                
                except Exception as e:
                    print(f"Error reading {path}: {e}")
        
        if checked_files > 100:
            break
    
    print(f"Found {found_files} files with tags out of {checked_files} checked files")

if __name__ == "__main__":
    main()
