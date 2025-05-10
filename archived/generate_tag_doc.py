"""
DEPRECATED: This script has been migrated to mba_notebook_automation/cli/generate_tag_doc.py
Please use the new CLI package version for all future work.
"""
"""
Obsidian Tag Documentation Generator.

This script scans an Obsidian vault directory and generates documentation
of all tags in use, organizing them by hierarchy.

Args:
    vault_path (str): Path to the Obsidian vault

Returns:
    Writes a markdown file with the complete tag hierarchy
"""

import os
import re
import yaml
from collections import defaultdict

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
                if frontmatter and 'tags' in frontmatter:
                    if isinstance(frontmatter['tags'], list):
                        tags.extend(frontmatter['tags'])
                    else:
                        tags.append(frontmatter['tags'])
            except yaml.YAMLError:
                pass
                
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

def generate_tag_documentation(vault_path, output_file="Tag System Reference.md"):
    """Generate complete tag documentation for an Obsidian vault.
    
    Args:
        vault_path (str): Path to Obsidian vault
        output_file (str): Output file name
    """
    all_tags = []
    
    # Walk through vault and collect tags
    for root, _, files in os.walk(vault_path):
        for file in files:
            if file.endswith('.md'):
                file_path = os.path.join(root, file)
                tags = extract_tags_from_file(file_path)
                all_tags.extend(tags)
    
    # Remove duplicates
    all_tags = list(set(all_tags))
    
    # Build hierarchy
    hierarchy = build_tag_hierarchy(all_tags)
      # Generate markdown
    markdown = hierarchy_to_markdown(hierarchy)
    
    # Create the output file path
    output_path = os.path.join(vault_path, output_file)
    
    # Check if file exists
    file_exists = os.path.isfile(output_path)
    
    # Write to file (create if it doesn't exist)
    with open(output_path, 'a' if file_exists else 'w') as f:
        if not file_exists:
            f.write("# Tag System Reference\n\n")
            f.write("This document contains the hierarchical structure of all tags used in the vault.\n")
        f.write("\n\n## All Tags in Use\n\n")
        f.write(markdown)

# Example usage
if __name__ == "__main__":
    import sys
    
    # Use command line argument if provided, otherwise use default
    if len(sys.argv) > 1:
        vault_path = sys.argv[1]
    else:
        vault_path = "D:\\Vault"  # Adjust to your vault path
    
    # Normalize the path to handle both Windows and Unix style paths
    vault_path = os.path.normpath(vault_path)
    
    print(f"Generating tag documentation for vault: {vault_path}")
    generate_tag_documentation(vault_path)