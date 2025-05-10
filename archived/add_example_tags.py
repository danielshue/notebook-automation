"""
DEPRECATED: This script has been migrated to mba_notebook_automation/cli/add_example_tags.py
Please use the new CLI package version for all future work.
"""
"""
Add Example Tags to Vault File

This script adds example nested tag structure to a specified markdown file
in the vault to demonstrate the hierarchical tagging system.

Usage:
    python add_example_tags.py <file_path>
"""

import os
import re
import sys
import yaml
from pathlib import Path

def add_example_tags(file_path):
    """Add example nested tags to an existing markdown file.
    
    Args:
        file_path (str): Path to markdown file to update
        
    Returns:
        bool: True if successful, False otherwise
    """
    if not os.path.exists(file_path):
        print(f"File does not exist: {file_path}")
        return False
    
    print(f"Adding example tags to: {file_path}")
    
    # Example nested tags to add
    example_tags = [
        "type/note/lecture",
        "type/note/reference",
        "mba/course/finance/corporate-finance",
        "mba/course/accounting/financial",
        "mba/skill/quantitative",
        "mba/tool/excel",
        "status/active",
        "priority/high"
    ]
    
    try:
        # Read the file content
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Check if it already has YAML frontmatter
        yaml_match = re.search(r'^---\s+(.*?)\s+---', content, re.DOTALL)
        
        if yaml_match:
            # Extract existing frontmatter
            frontmatter_text = yaml_match.group(1)
            try:
                frontmatter = yaml.safe_load(frontmatter_text)
                if not isinstance(frontmatter, dict):
                    frontmatter = {}
            except:
                frontmatter = {}
                
            # Add or update tags
            if 'tags' in frontmatter and isinstance(frontmatter['tags'], list):
                # Append new tags to existing ones
                frontmatter['tags'].extend(example_tags)
                # Remove duplicates while preserving order
                seen = set()
                frontmatter['tags'] = [tag for tag in frontmatter['tags'] 
                                     if not (tag in seen or seen.add(tag))]
            else:
                frontmatter['tags'] = example_tags
                
            # Convert back to YAML
            new_frontmatter = yaml.dump(frontmatter, default_flow_style=False)
            
            # Replace old frontmatter with new one
            new_content = content.replace(yaml_match.group(0), f"---\n{new_frontmatter}---")
            
        else:
            # No frontmatter exists, add one
            tags_yaml = yaml.dump({'tags': example_tags}, default_flow_style=False)
            new_content = f"---\n{tags_yaml}---\n\n{content}"
        
        # Write the modified content back
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
            
        print(f"Successfully added example tags to {file_path}")
        return True
        
    except Exception as e:
        print(f"Error adding tags to {file_path}: {e}")
        return False

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python add_example_tags.py <file_path>")
        sys.exit(1)
        
    file_path = sys.argv[1]
    add_example_tags(file_path)
