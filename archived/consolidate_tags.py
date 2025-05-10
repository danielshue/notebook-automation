"""
DEPRECATED: This script has been superseded by a new CLI version.
Please use the updated CLI in mba_notebook_automation/cli/consolidate_tags.py for future work.

General Tag Consolidation Tool
-----------------------------
This script consolidates multiple tags in markdown notes to a single, most specific nested tag.
It analyzes the tag structure in each note, determines the most appropriate hierarchical tag,
and updates the YAML frontmatter accordingly.

Features:
- Analyze and consolidate tags in markdown files
- Supports dry-run and debug output
- Optionally keep structural tags in addition to the main tag

Usage Examples:
  python consolidate_tags.py --vault /path/to/vault
  python consolidate_tags.py --dry-run --debug
  python consolidate_tags.py --help
"""

import os
import re
import sys
import argparse
from pathlib import Path
import logging
import yaml
from collections import Counter

# Import from the package
try:
    from mba_notebook_automation.tools.utils.config import setup_logging, VAULT_LOCAL_ROOT
    logger, failed_logger = setup_logging(debug=False)
except ImportError:
    # Fallback if the module can't be imported
    logger = logging.getLogger(__name__)
    failed_logger = logging.getLogger('failed_files')
    logging.basicConfig(level=logging.INFO)
    VAULT_LOCAL_ROOT = None

# Tag hierarchy priorities - higher number means higher priority/specificity
# This determines which tag to keep when consolidating
TAG_PRIORITIES = {
    # Primary categories
    'mba': 100,
    'course': 90,
    'program': 80,
    'type': 70,
    'term': 60,
    'status': 50,
    
    # Subcategories by priority (course > skill > type > tool)
    'mba/course': 200,  # Highest priority - subject matter
    'mba/skill': 150,   # Second priority - skills developed
    'type': 120,        # Third priority - content type
    'mba/tool': 110,    # Fourth priority - tools used
    'type/note': 80,
    'type/assignment': 80,
    'type/project': 80,
    'type/resource': 80,
    
    # Course-related tags (highest priority)
    'mba/course/finance': 210,
    'mba/course/accounting': 210,
    'mba/course/marketing': 210,
    'mba/course/strategy': 210,
    'mba/course/operations': 210,
    'mba/course/finance/corporate-finance': 220,
    'mba/course/finance/investments': 220,
    'mba/course/finance/valuation': 220,
    'mba/course/accounting/financial': 220,
    'mba/course/accounting/managerial': 220,
    'mba/course/accounting/audit': 220,
    'mba/course/accounting/tax': 220,
    'mba/course/marketing/digital': 220,
    'mba/course/marketing/strategy': 220,
    'mba/course/marketing/consumer-behavior': 220,
    'mba/course/marketing/analytics': 220,
    'mba/course/strategy/corporate': 220,
    'mba/course/strategy/competitive': 220,
    'mba/course/strategy/global': 220,
    'mba/course/strategy/innovation': 220,
    'mba/course/operations/supply-chain': 220,
    'mba/course/operations/project-management': 220,
    'mba/course/operations/quality-management': 220,
    'mba/course/operations/process-design': 220,
    
    # Skill-related tags (second priority)
    'mba/skill/quantitative': 160,
    'mba/skill/qualitative': 160,
    'mba/skill/presentation': 160,
    'mba/skill/negotiation': 160,
    'mba/skill/leadership': 160,
    
    # Type-related tags (third priority)
    'type/note/lecture': 130,
    'type/note/case-study': 130,
    'type/note/literature': 130,
    'type/note/meeting': 130,
    'type/assignment/individual': 130,
    'type/assignment/group': 130,
    'type/assignment/draft': 130,
    'type/assignment/final': 130,
    'type/project/proposal': 130,
    'type/project/active': 130,
    'type/project/completed': 130,
    'type/project/archived': 130,
    'type/resource/textbook': 130,
    'type/resource/article': 130,
    'type/resource/paper': 130,
    'type/resource/presentation': 130,
    'type/resource/template': 130,
    
    # Tool-related tags (fourth priority)
    'mba/tool/excel': 120,
    'mba/tool/python': 120,
    'mba/tool/r': 120,
    'mba/tool/powerbi': 120,
    'mba/tool/tableau': 120,
}

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

def choose_most_specific_tag(tags):
    """
    Choose the most specific tag from a list of tags based on priority.
    
    Args:
        tags (list): List of tag strings
        
    Returns:
        str: The most specific tag to keep
    """
    # If only one tag or no tags, just return it
    if len(tags) <= 1:
        return tags[0] if tags else None

    # Calculate the score for each tag based on its specificity
    tag_scores = []
    for tag in tags:
        # Start with the base score for the tag
        score = TAG_PRIORITIES.get(tag, 0)
        
        # Add additional points for more nested levels
        nested_levels = tag.count('/')
        score += nested_levels * 10
        
        # Store the score with the tag
        tag_scores.append((tag, score))
    
    # Sort by score in descending order and return the highest scoring tag
    if tag_scores:
        tag_scores.sort(key=lambda x: x[1], reverse=True)
        return tag_scores[0][0]
    
    return None

def get_all_parent_tags(tag):
    """
    Get all parent tags of a given tag.
    
    Args:
        tag (str): A tag like 'mba/course/finance/investments'
        
    Returns:
        list: All parent tags ['mba', 'mba/course', 'mba/course/finance']
    """
    parents = []
    parts = tag.split('/')
    
    for i in range(1, len(parts)):
        parents.append('/'.join(parts[:i]))
    
    return parents

def consolidate_frontmatter_tags(frontmatter_text, keep_structural=False):
    """
    Consolidate multiple tags in YAML frontmatter to a single specific tag.
    
    Args:
        frontmatter_text (str): YAML frontmatter content
        keep_structural (bool): Whether to keep structural tags (index/, structure/)
        
    Returns:
        tuple: (modified YAML frontmatter, original tag count, new tag count,
                most specific tag chosen)
    """
    try:
        # Parse the frontmatter
        data = yaml.safe_load(frontmatter_text)
        
        # Check if tags exist in frontmatter
        if 'tags' not in data or not data['tags']:
            return frontmatter_text, 0, 0, None
        
        # Count original tags
        original_tag_count = len(data['tags'])
        
        # Separate structural tags if needed
        structural_tags = []
        content_tags = []
        
        for tag in data['tags']:
            if isinstance(tag, str):
                if any(tag.startswith(prefix) for prefix in ['index/', 'structure/']):
                    structural_tags.append(tag)
                else:
                    content_tags.append(tag)
        
        # Choose the most specific content tag
        chosen_tag = choose_most_specific_tag(content_tags)
        
        # Update tags in frontmatter
        new_tags = []
        if chosen_tag:
            new_tags.append(chosen_tag)
        
        # Keep structural tags if specified
        if keep_structural:
            new_tags.extend(structural_tags)
        
        # Update tags in the data
        if new_tags:
            data['tags'] = new_tags
        else:
            del data['tags']
        
        # Convert back to YAML
        return yaml.dump(data, sort_keys=False, default_flow_style=False), original_tag_count, len(new_tags), chosen_tag
    
    except Exception as e:
        logger.error(f"Error processing frontmatter: {e}")
        return frontmatter_text, 0, 0, None

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

def process_file(filepath, dry_run=False, keep_structural=False):
    """
    Process a file to consolidate tags.
    
    Args:
        filepath (Path): Path to the file
        dry_run (bool): If True, don't actually modify the file
        keep_structural (bool): Whether to keep structural tags
        
    Returns:
        tuple: (bool of whether file was modified, original tag count,
                new tag count, most specific tag chosen)
    """
    try:
        with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
            
        # Extract frontmatter
        frontmatter_match = re.search(r'---\s(.*?)\s---', content, re.DOTALL)
        if not frontmatter_match:
            logger.debug(f"No frontmatter found in {filepath}")
            return False, 0, 0, None
            
        original_frontmatter = frontmatter_match.group(1)
        cleaned_frontmatter, orig_count, new_count, chosen_tag = consolidate_frontmatter_tags(
            original_frontmatter, keep_structural)
        
        # If frontmatter wasn't changed, don't need to modify file
        if original_frontmatter == cleaned_frontmatter:
            logger.debug(f"No changes needed for {filepath}")
            return False, orig_count, new_count, chosen_tag
                
        # Replace frontmatter in content
        modified_content = content.replace(
            f"---\n{original_frontmatter}\n---",
            f"---\n{cleaned_frontmatter}\n---"
        )
        
        # Clean inline tags from main content
        modified_content = remove_inline_tags(modified_content)
        
        if dry_run:
            logger.info(f"Would consolidate tags for {filepath} - " 
                       f"from {orig_count} to {new_count} tags, " 
                       f"keeping {chosen_tag}")
            return True, orig_count, new_count, chosen_tag
            
        # Write back the modified content
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(modified_content)
            
        logger.info(f"Consolidated tags for {filepath} - "
                   f"from {orig_count} to {new_count} tags, "
                   f"keeping {chosen_tag}")
        return True, orig_count, new_count, chosen_tag
        
    except Exception as e:
        logger.error(f"Error processing {filepath}: {e}")
        return False, 0, 0, None

def process_vault(vault_path, dry_run=False, debug=False, keep_structural=False):
    """
    Process all markdown files in the vault to consolidate tags.
    
    Args:
        vault_path (Path): Path to the vault root
        dry_run (bool): If True, don't actually modify files
        debug (bool): If True, enable verbose logging
        keep_structural (bool): Whether to keep structural tags
    """
    if debug:
        logger.setLevel(logging.DEBUG)
        
    logger.info(f"Processing vault: {vault_path}")
    if dry_run:
        logger.info("DRY RUN MODE - no files will be modified")
        
    modified_count = 0
    processed_count = 0
    total_orig_tags = 0
    total_new_tags = 0
    tag_stats = Counter()
    
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
            
            # Skip index files as they're handled by clean_index_tags.py
            if is_index_file(filepath):
                continue
                
            processed_count += 1
            modified, orig_count, new_count, chosen_tag = process_file(
                filepath, dry_run, keep_structural)
            
            if modified:
                modified_count += 1
                total_orig_tags += orig_count
                total_new_tags += new_count
                
                if chosen_tag:
                    tag_stats[chosen_tag] += 1
    
    # Print statistics
    logger.info(f"Processed {processed_count} files")
    logger.info(f"Modified {modified_count} files")
    logger.info(f"Reduced from {total_orig_tags} to {total_new_tags} total tags")
    
    # Show tag distribution
    if tag_stats:
        logger.info("Top tags used:")
        for tag, count in tag_stats.most_common(10):
            logger.info(f"  {tag}: {count}")

def main():
    """Main entry point for the script."""
    parser = argparse.ArgumentParser(
        description="""
DEPRECATED: Use the new CLI version if available.

Consolidate tags in markdown notes to a single, most specific nested tag.
Analyzes tag structure, determines the most appropriate hierarchical tag, and updates YAML frontmatter.

Features:
  - Analyze and consolidate tags in markdown files
  - Supports dry-run and debug output
  - Optionally keep structural tags in addition to the main tag

Examples:
  python consolidate_tags.py --vault /path/to/vault
  python consolidate_tags.py --dry-run --debug
        """
    )
    parser.add_argument('--vault', type=str, help='Path to the root directory to scan for markdown files (default: configured vault root)')
    parser.add_argument('--dry-run', action='store_true', help='Simulate changes without modifying files')
    parser.add_argument('--debug', action='store_true', help='Show verbose debug output')
    parser.add_argument('--keep-structural', action='store_true', help='Keep structural tags (e.g., index/, structure/) in addition to the main tag')
    parser.add_argument('--priority', type=str, choices=['course', 'skill', 'type', 'tool'], default='course', help='Tag category to prioritize when consolidating (default: course)')
    args = parser.parse_args()
    
    # Adjust tag priorities based on user preference
    if args.priority == 'skill':
        # Boost skill tags to highest priority
        for key in TAG_PRIORITIES:
            if 'skill' in key:
                TAG_PRIORITIES[key] += 100
    elif args.priority == 'type':
        # Boost type tags to highest priority
        for key in TAG_PRIORITIES:
            if key.startswith('type/'):
                TAG_PRIORITIES[key] += 150
    elif args.priority == 'tool':
        # Boost tool tags to highest priority
        for key in TAG_PRIORITIES:
            if 'tool' in key:
                TAG_PRIORITIES[key] += 150
    # Default is course priority which is already set up
    
    # Determine vault path
    vault_path = args.vault
    if not vault_path:
        if VAULT_LOCAL_ROOT:
            vault_path = VAULT_LOCAL_ROOT
        else:
            print("Error: No vault path provided and VAULT_LOCAL_ROOT not available")
            print("Please specify vault path with --vault argument")
            sys.exit(1)
            
    process_vault(Path(vault_path), args.dry_run, args.debug, args.keep_structural)

if __name__ == "__main__":
    main()
