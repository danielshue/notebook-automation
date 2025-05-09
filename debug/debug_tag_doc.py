#!/usr/bin/env python3
"""
Debug version of the Tag Documentation Generator.
This script uses explicit paths for testing.
"""

import os
import sys
from pathlib import Path
import re
import yaml
from collections import defaultdict

# Make sure we're printing output immediately
import functools
print = functools.partial(print, flush=True)

def extract_tags_from_file(file_path):
    """Extract tags from an Obsidian markdown file."""
    tags = []
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
            
        # Extract YAML frontmatter
        yaml_match = re.search(r'^---\s+(.*?)\s+---', content, re.DOTALL)
        if yaml_match:
            try:
                frontmatter = yaml.safe_load(yaml_match.group(1))
                if frontmatter and 'tags' in frontmatter:
                    if isinstance(frontmatter['tags'], list):
                        tags.extend(frontmatter['tags'])
                    else:
                        tags.append(frontmatter['tags'])
            except yaml.YAMLError as ye:
                print(f"YAML error in {file_path}: {ye}")
                
        # Extract inline tags
        inline_tags = re.findall(r'#([a-zA-Z0-9_/\-]+)', content)
        tags.extend(inline_tags)
        
    except Exception as e:
        print(f"Error processing {file_path}: {e}")
        
    return tags

def build_tag_hierarchy(tags):
    """Build a hierarchical structure of nested tags."""
    hierarchy = defaultdict(dict)
    
    for tag in tags:
        parts = tag.split('/')
        current = hierarchy
        
        for i, part in enumerate(parts):
            if i == len(parts) - 1:
                current[part] = current.get(part, {})
            else:
                if part not in current:
                    current[part] = {}
                current = current[part]
                
    return hierarchy

def hierarchy_to_markdown(hierarchy, indent=0):
    """Convert tag hierarchy to markdown format."""
    result = []
    for key, value in sorted(hierarchy.items()):
        result.append(f"{' ' * indent}- {key}")
        if value:
            result.append(hierarchy_to_markdown(value, indent + 2))
    return '\n'.join(result)

def ensure_dir_exists(path):
    """Ensure directory exists for a given file path."""
    try:
        directory = os.path.dirname(path)
        if directory and not os.path.exists(directory):
            os.makedirs(directory)
        return True
    except Exception as e:
        print(f"Failed to create directory for {path}: {e}")
        return False

def generate_tag_documentation(vault_path, output_file="Tag System Reference.md"):
    """Generate complete tag documentation for an Obsidian vault."""
    print(f"Scanning vault: {vault_path}")
    all_tags = []
    file_count = 0
    
    # First check if vault path exists
    if not os.path.exists(vault_path):
        print(f"ERROR: Vault path does not exist: {vault_path}")
        return None
    
    # Walk through vault and collect tags
    try:
        total_files = 0
        processed_files = 0
        
        # First count total markdown files for progress reporting
        for root, _, files in os.walk(vault_path):
            for file in files:
                if file.endswith('.md'):
                    total_files += 1
        
        print(f"Found {total_files} markdown files to process")
        
        # Now process each file with progress updates
        for root, _, files in os.walk(vault_path):
            for file in files:
                if file.endswith('.md'):
                    processed_files += 1
                    if processed_files % 100 == 0 or processed_files == total_files:
                        print(f"Processing files: {processed_files}/{total_files} ({processed_files/total_files*100:.1f}%)")
                    
                    file_path = os.path.join(root, file)
                    tags = extract_tags_from_file(file_path)
                    if tags:
                        all_tags.extend(tags)
                        file_count += 1
        
        print(f"Processed {file_count} files with tags")
        
        # Remove duplicates
        all_tags = list(set(all_tags))
        print(f"Found {len(all_tags)} unique tags")
        
        # Build hierarchy
        hierarchy = build_tag_hierarchy(all_tags)
        
        # Generate markdown
        markdown = hierarchy_to_markdown(hierarchy)
        
        # Create the output file path
        output_path = os.path.join(vault_path, output_file)
        
        # Ensure target directory exists
        if not ensure_dir_exists(output_path):
            print(f"Failed to create directory for {output_path}")
            return None
        
        # Check if file exists
        file_exists = os.path.isfile(output_path)
        
        # Write to file (create if it doesn't exist)
        with open(output_path, 'a' if file_exists else 'w', encoding='utf-8') as f:
            if not file_exists:
                f.write("# Tag System Reference\n\n")
                f.write("This document contains the hierarchical structure of all tags used in the vault.\n")
            f.write("\n\n## All Tags in Use\n\n")
            f.write(markdown)
            
        print(f"Tag documentation written to: {output_path}")
        return output_path
        
    except Exception as e:
        print(f"Failed to generate tag documentation: {e}")
        return None

if __name__ == "__main__":
    # Allow specifying vault path as argument or use default
    if os.name == 'nt':  # Windows
        vault_path = "D:\\Vault\\01_Projects\\MBA"
    else:  # WSL or Linux
        vault_path = "/mnt/d/Vault/01_Projects/MBA"
    
    # Test if the path exists before proceeding
    if not os.path.exists(vault_path):
        print(f"ERROR: The vault path does not exist: {vault_path}")
        print("Available directories in D:/Vault:")
        if os.path.exists("/mnt/d/Vault"):
            for item in os.listdir("/mnt/d/Vault"):
                if os.path.isdir(os.path.join("/mnt/d/Vault", item)):
                    print(f"  - {item}")
        sys.exit(1)
        
    output_file = "Tag System Reference.md"
    
    # Use command line arguments if provided
    if len(sys.argv) > 1:
        vault_path = sys.argv[1]
    if len(sys.argv) > 2:
        output_file = sys.argv[2]
    
    # Convert to Path and then back to string to normalize
    vault_path = str(Path(vault_path))
    
    print(f"Using vault path: {vault_path}")
    result = generate_tag_documentation(vault_path, output_file)
    
    if result:
        print(f"Successfully generated tag documentation at: {result}")
    else:
        print("Failed to generate tag documentation.")
