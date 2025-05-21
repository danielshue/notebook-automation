#!/usr/bin/env python3
"""
Obsidian Tag Documentation Generator.

This script scans an Obsidian vault directory and generates documentation
of all tags in use, organizing them by hierarchy.

Args:
    vault_path (str, optional): Path to the Obsidian vault. If not provided, 
                               uses the configured vault path.

Returns:
    Writes a markdown file with the complete tag hierarchy
"""

import os
import re
import yaml
import sys
from collections import defaultdict
from pathlib import Path

# Import utilities from tools
from tools.utils.config import setup_logging, VAULT_LOCAL_ROOT
from tools.utils.paths import normalize_path

# Set up logging
logger, failed_logger = setup_logging()

def extract_tags_from_file(file_path):
    """Extract tags from an Obsidian markdown file.
    
    Args:
        file_path (str or Path): Path to markdown file
        
    Returns:
        list: List of tags found in the file
    """
    tags = []
    try:
        # Normalize and ensure path is a string
        file_path = str(normalize_path(file_path))
        
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
                logger.warning(f"YAML error in {file_path}: {ye}")
                
        # Extract inline tags
        inline_tags = re.findall(r'#([a-zA-Z0-9_/\-]+)', content)
        tags.extend(inline_tags)
        
    except Exception as e:
        failed_logger.error(f"Error processing {file_path}: {e}")
        
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
    """Ensure directory exists for a given file path.
    
    Args:
        path (str or Path): File path
    
    Returns:
        bool: True if successful, False otherwise
    """
    try:
        directory = os.path.dirname(path)
        if directory and not os.path.exists(directory):
            os.makedirs(directory)
        return True
    except Exception as e:
        logger.error(f"Failed to create directory for {path}: {e}")
        return False

def generate_tag_documentation(vault_path=None, output_file="Tag System Reference.md"):
    """Generate complete tag documentation for an Obsidian vault.
    
    Args:
        vault_path (str, optional): Path to Obsidian vault. Defaults to VAULT_LOCAL_ROOT.
        output_file (str): Output file name
    
    Returns:
        str: Path to the generated file if successful, None otherwise
    """    # Use configured vault path if none provided
    if vault_path is None:
        vault_path = VAULT_LOCAL_ROOT
    
    # Normalize vault path
    vault_path = normalize_path(vault_path)
    
    # Check if vault path exists
    if not os.path.exists(vault_path):
        failed_logger.error(f"Vault path does not exist: {vault_path}")
        return None
    
    logger.info(f"Scanning vault: {vault_path}")
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
        
        logger.info(f"Found {total_files} markdown files to process")
        
        # Now process each file with progress updates
        for root, _, files in os.walk(vault_path):
            for file in files:
                if file.endswith('.md'):
                    processed_files += 1
                    if processed_files % 100 == 0 or processed_files == total_files:
                        logger.info(f"Processing files: {processed_files}/{total_files} ({processed_files/total_files*100:.1f}%)")
                    
                    file_path = os.path.join(root, file)
                    tags = extract_tags_from_file(file_path)
                    if tags:
                        all_tags.extend(tags)
                        file_count += 1
        
        logger.info(f"Processed {file_count} files with tags")
        
        # Remove duplicates
        all_tags = list(set(all_tags))
        logger.info(f"Found {len(all_tags)} unique tags")
        
        # Build hierarchy
        hierarchy = build_tag_hierarchy(all_tags)
        
        # Generate markdown
        markdown = hierarchy_to_markdown(hierarchy)
        
        # Create the output file path
        output_path = os.path.join(vault_path, output_file)
        
        # Ensure target directory exists
        if not ensure_dir_exists(output_path):
            logger.error(f"Failed to create directory for {output_path}")
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
            
        logger.info(f"Tag documentation written to: {output_path}")
        return output_path
        
    except Exception as e:
        failed_logger.error(f"Failed to generate tag documentation: {e}")
        return None

# Example usage
if __name__ == "__main__":
    # Use command line argument if provided, otherwise use default
    vault_path = None
    if len(sys.argv) > 1:
        vault_path = sys.argv[1]
    
    result = generate_tag_documentation(vault_path)
    if result:
        print(f"Successfully generated tag documentation at: {result}")
    else:
        print("Failed to generate tag documentation. See log for details.")
