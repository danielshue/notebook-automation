#!/usr/bin/env python
"""
MBA Notebook Index Tag Cleaner

This script removes tags from index pages in the MBA notebook vault structure.
It identifies index pages by filename pattern and content, then modifies their
YAML frontmatter to remove specified tags while preserving essential metadata.

The script helps maintain a clean tagging strategy by ensuring index pages
don't pollute tag searches with structural organization markers.

Args:
    --vault: Path to the Obsidian vault root (defaults to configured VAULT_LOCAL_ROOT)
    --dry-run: Show what would be changed without actually modifying files
    --debug: Enable verbose debug output

Example:
    python clean_index_tags.py --vault /path/to/vault
    python clean_index_tags.py --dry-run --debug
"""

import os
import re
import sys
import argparse
from pathlib import Path
import logging
import yaml

# Try to import from your existing setup
try:
    from tools.utils.config import setup_logging, VAULT_LOCAL_ROOT
    logger, failed_logger = setup_logging(debug=False)
except ImportError:
    # Fallback if the module can't be imported
    logger = logging.getLogger(__name__)
    failed_logger = logging.getLogger('failed_files')
    logging.basicConfig(level=logging.INFO)
    VAULT_LOCAL_ROOT = None

def is_index_file(filepath):
    """
    Determine if a file is an index page based on name and content.
    
    Args:
        filepath (Path): Path to the file to check
        
    Returns:
        bool: True if the file is an index page, False otherwise
    """
    # Check filename patterns common for index files
    filename = filepath.name.lower()
    if "index" in filename:
        return True
    
    # Check if file ends with the parent folder name (common pattern for index files)
    parent_name = filepath.parent.name.lower()
    if filename.lower() == f"{parent_name}.md":
        return True
        
    # Check content for index page indicators
    try:
        with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read(2000)  # Read just enough to check frontmatter
            
            # Check for template-type in frontmatter that indicates an index
            if re.search(r'template-type:\s*(.*-index)', content, re.IGNORECASE):
                return True
    except Exception as e:
        logger.error(f"Error reading {filepath}: {e}")
    
    return False

def clean_frontmatter_tags(frontmatter_text):
    """
    Remove tags from YAML frontmatter while preserving structure.
    
    Args:
        frontmatter_text (str): YAML frontmatter content
        
    Returns:
        str: Cleaned YAML frontmatter without content tags
    """
    try:
        # Parse the frontmatter
        data = yaml.safe_load(frontmatter_text)
        
        # Check if tags exist in frontmatter
        if 'tags' not in data:
            return frontmatter_text
            
        # Preserve only structural tags
        structural_tags = []
        for tag in data['tags']:
            if isinstance(tag, str) and any(tag.startswith(prefix) for prefix in ['index/', 'structure/']):
                structural_tags.append(tag)
        
        # Either remove tags completely or keep only structural ones
        if structural_tags:
            data['tags'] = structural_tags
        else:
            del data['tags']
            
        # Convert back to YAML
        return yaml.dump(data, sort_keys=False, default_flow_style=False)
    except Exception as e:
        logger.error(f"Error processing frontmatter: {e}")
        return frontmatter_text

def remove_inline_tags(content):
    """
    Remove inline Markdown tags from content.
    
    Args:
        content (str): Markdown content
        
    Returns:
        str: Content with inline tags removed
    """
    # Remove #tag-style inline tags (but not within code blocks)
    lines = content.split('\n')
    in_code_block = False
    cleaned_lines = []
    
    for line in lines:
        # Track code blocks
        if line.strip().startswith('```'):
            in_code_block = not in_code_block
            cleaned_lines.append(line)
            continue
            
        if not in_code_block:
            # Remove standalone tag lines
            if re.match(r'^\s*#[\w/-]+\s*$', line):
                continue
                
            # Remove inline tags
            line = re.sub(r'\s#[\w/-]+', ' ', line)
        
        cleaned_lines.append(line)
    
    return '\n'.join(cleaned_lines)

def process_index_file(filepath, dry_run=False):
    """
    Process an index file to remove tags.
    
    Args:
        filepath (Path): Path to the index file
        dry_run (bool): If True, don't actually modify the file
        
    Returns:
        bool: True if file was modified, False otherwise
    """
    try:
        with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
            
        # Extract frontmatter
        frontmatter_match = re.search(r'---\s(.*?)\s---', content, re.DOTALL)
        if not frontmatter_match:
            logger.debug(f"No frontmatter found in {filepath}")
            return False
            
        original_frontmatter = frontmatter_match.group(1)
        cleaned_frontmatter = clean_frontmatter_tags(original_frontmatter)
        
        # If frontmatter wasn't changed, don't need to modify file
        if original_frontmatter == cleaned_frontmatter:
            remainder = content[frontmatter_match.end():]
            if not re.search(r'#[\w/-]+', remainder):
                logger.debug(f"No changes needed for {filepath}")
                return False
                
        # Replace frontmatter in content
        modified_content = content.replace(
            f"---\n{original_frontmatter}\n---",
            f"---\n{cleaned_frontmatter}\n---"
        )
        
        # Clean inline tags from main content
        modified_content = remove_inline_tags(modified_content)
        
        if dry_run:
            logger.info(f"Would clean tags from {filepath}")
            return True
            
        # Write back the modified content
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(modified_content)
            
        logger.info(f"Cleaned tags from {filepath}")
        return True
        
    except Exception as e:
        logger.error(f"Error processing {filepath}: {e}")
        return False

def process_vault(vault_path, dry_run=False, debug=False):
    """
    Process all index files in the vault.
    
    Args:
        vault_path (Path): Path to the vault root
        dry_run (bool): If True, don't actually modify files
        debug (bool): If True, enable verbose logging
    """
    if debug:
        logger.setLevel(logging.DEBUG)
        
    logger.info(f"Processing vault: {vault_path}")
    if dry_run:
        logger.info("DRY RUN MODE - no files will be modified")
        
    modified_count = 0
    processed_count = 0
    
    # Walk through the vault
    for root, dirs, files in os.walk(vault_path):
        # Skip hidden directories
        dirs[:] = [d for d in dirs if not d.startswith('.')]
        
        path = Path(root)
        
        # Process markdown files
        for file in files:
            if not file.endswith('.md'):
                continue
                
            filepath = path / file
            
            # Check if this is an index file
            if is_index_file(filepath):
                processed_count += 1
                if process_index_file(filepath, dry_run):
                    modified_count += 1
    
    logger.info(f"Processed {processed_count} index files")
    logger.info(f"Modified {modified_count} files")

def main():
    """Main entry point for the script."""
    parser = argparse.ArgumentParser(description="Clean tags from MBA notebook index pages")
    parser.add_argument('--vault', type=str, help='Path to the Obsidian vault')
    parser.add_argument('--dry-run', action='store_true', help='Show what would be changed without modifying files')
    parser.add_argument('--debug', action='store_true', help='Enable debug logging')
    args = parser.parse_args()
    
    # Determine vault path
    vault_path = args.vault
    if not vault_path:
        if VAULT_LOCAL_ROOT:
            vault_path = VAULT_LOCAL_ROOT
        else:
            print("Error: No vault path provided and VAULT_LOCAL_ROOT not available")
            print("Please specify vault path with --vault argument")
            sys.exit(1)
            
    process_vault(Path(vault_path), args.dry_run, args.debug)

if __name__ == "__main__":
    main()