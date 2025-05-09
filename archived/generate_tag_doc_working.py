#!/usr/bin/env python3
"""
Obsidian Tag Documentation Generator.

This script scans an Obsidian vault directory and generates documentation
of all tags in use, organizing them by hierarchy.

Usage:
    python generate_tag_doc.py [vault_path] [output_file]

Args:
    vault_path (str, optional): Path to the Obsidian vault.
    output_file (str, optional): Name of output file.

Returns:
    Writes a markdown file with the complete tag hierarchy
"""

import os
import re
import sys
import yaml
from collections import defaultdict
from pathlib import Path

def extract_tags_from_file(file_path):
    """Extract tags from an Obsidian markdown file.
    
    Args:
        file_path (str): Path to markdown file
        
    Returns:
        list: List of tags found in the file
    """
    tags = []
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
              # Extract YAML frontmatter
        yaml_match = re.search(r'^---\s+(.*?)\s+---', content, re.DOTALL)
        if yaml_match:
            try:
                frontmatter = yaml.safe_load(yaml_match.group(1))
                if frontmatter and isinstance(frontmatter, dict) and 'tags' in frontmatter:
                    if isinstance(frontmatter['tags'], list):
                        # Filter out None values
                        valid_tags = [tag for tag in frontmatter['tags'] if tag]
                        tags.extend(valid_tags)
                    elif frontmatter['tags']:  # Check if not None/empty
                        tags.append(frontmatter['tags'])
            except yaml.YAMLError:
                print(f"YAML parsing error in {file_path}")
                
        # Extract inline tags
        inline_tags = re.findall(r'#([a-zA-Z0-9_/\-]+)', content)
        tags.extend(inline_tags)
        
    except Exception as e:
        print(f"Error processing {file_path}: {e}")
        
    return tags

def build_tag_hierarchy(tags):
    """Build a hierarchical structure of nested tags.
    
    Args:
        tags (list): List of all tags
        
    Returns:
        dict: Nested dictionary representing tag hierarchy
    """
    hierarchy = defaultdict(dict)
    
    for tag in tags:
        # Skip None values or empty strings
        if not tag:
            continue
            
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
    """Convert tag hierarchy to markdown format.
    
    Args:
        hierarchy (dict): Nested dictionary of tags
        indent (int): Current indentation level
        
    Returns:
        str: Markdown formatted tag hierarchy
    """
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
    """Generate complete tag documentation for an Obsidian vault.
    
    Args:
        vault_path (str): Path to Obsidian vault
        output_file (str): Output file name
    
    Returns:
        str: Path to generated file if successful, None otherwise
    """
    if not os.path.exists(vault_path):
        print(f"ERROR: Vault path does not exist: {vault_path}")
        return None
    
    print(f"Scanning vault: {vault_path}")
    all_tags = []
    file_count = 0
    
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
        
        # Process each file
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
        
        if len(all_tags) == 0:
            print("No tags found in the vault.")
            return None
        
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
        
        # Write to file
        try:
            with open(output_path, 'w', encoding='utf-8') as f:
                f.write("# Tag System Reference\n\n")
                f.write("This document contains the hierarchical structure of all tags used in the vault.\n")
                f.write(f"\nGenerated documentation for {file_count} files with {len(all_tags)} unique tags.\n")
                f.write("\n## All Tags in Use\n\n")
                f.write(markdown)
                
            print(f"Tag documentation written to: {output_path}")
            return output_path
            
        except Exception as e:
            print(f"Error writing to file {output_path}: {e}")
            return None
            
    except Exception as e:
        print(f"Failed to generate tag documentation: {e}")
        return None

if __name__ == "__main__":
    # Set default vault path based on environment
    if os.name == 'nt':  # Windows
        default_vault = "D:\\Vault\\01_Projects\\MBA"
    else:  # WSL or Linux
        default_vault = "/mnt/d/Vault/01_Projects/MBA"
    
    # Parse command line arguments
    vault_path = default_vault
    output_file = "Tag System Reference.md"
    
    if len(sys.argv) > 1:
        vault_path = sys.argv[1]
    if len(sys.argv) > 2:
        output_file = sys.argv[2]
    
    # Convert to Path and back to string to normalize
    vault_path = str(Path(vault_path))
    
    # Run the tag documentation generator
    result = generate_tag_documentation(vault_path, output_file)
    
    if result:
        print(f"Successfully generated tag documentation at: {result}")
    else:
        print("Failed to generate tag documentation.")
