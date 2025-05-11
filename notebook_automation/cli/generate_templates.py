#!/usr/bin/env python3
"""
Generate Obsidian Templates with Nested Tags

This script creates Obsidian template files with the nested tag structure
for different types of notes.

The script generates several template types:
- Lecture Note
- Case Study
- Assignment
- Group Project
- Course Dashboard
- Finance Note
- Literature Review

Usage:
    python generate_obsidian_templates.py [--template-path PATH] [--force] [--verbose]

Arguments:
    --template-path PATH  Path to the template folder. If not specified, uses default locations
    --force              Force overwrite existing templates
    --verbose, -v        Print detailed information about template generation

Examples:
    python generate_obsidian_templates.py
    python generate_obsidian_templates.py --template-path "/path/to/templates"
    python generate_obsidian_templates.py --force --verbose
"""

import os
import sys
import yaml
import argparse
from pathlib import Path

def create_template(template_path: str, template_name: str, tags: list, 
                template_content: str, force: bool = False, verbose: bool = False) -> bool:
    """Create an Obsidian template with the specified tags.
    
    Generates a markdown template file with YAML frontmatter containing the specified
    tags and template content. Creates the template directory if it doesn't exist
    and handles file existence checks based on the force parameter.
    
    Args:
        template_path (str): Path to the template folder
        template_name (str): Name of the template file (without extension)
        tags (list): List of tags to include in the frontmatter
        template_content (str): Content to include after the frontmatter
        force (bool): Whether to overwrite existing templates. Defaults to False.
        verbose (bool): Whether to print detailed information. Defaults to False.
        
    Returns:
        bool: True if template was created successfully, False otherwise
        
    Example:
        >>> create_template("/templates", "lecture-note", ["type/lecture", "course/finance"], 
        ...                "# Lecture Notes\\n\\n## Summary", force=True)
        Created template: /templates/lecture-note.md
        True
    """
    os.makedirs(template_path, exist_ok=True)
    
    file_path = os.path.join(template_path, f"{template_name}.md")
    
    # Check if file exists and force not set
    if os.path.exists(file_path) and not force:
        if verbose:
            print(f"Skipping existing template: {file_path} (use --force to overwrite)")
        return False
    
    try:
        # Create YAML frontmatter
        frontmatter = {
            'tags': tags
        }
        
        yaml_text = yaml.dump(frontmatter, default_flow_style=False)
        content = f"---\n{yaml_text}---\n\n{template_content}"
        
        # Write to file
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
            
        if verbose:
            if os.path.exists(file_path):
                print(f"Updated template: {file_path}")
            else:
                print(f"Created template: {file_path}")
        return True
        
    except Exception as e:
        print(f"Error creating template {template_name}: {e}")
        return False

def generate_all_templates(template_folder: str, force: bool = False, verbose: bool = False) -> int:
    """Generate all templates for the MBA vault.
    
    Creates a set of predefined templates with appropriate tags and content structure
    for various MBA note types including lecture notes, case studies, assignments,
    and more. Each template includes YAML frontmatter and markdown content structure.
    
    Args:
        template_folder (str): Path to the template folder where files will be created
        force (bool): Whether to overwrite existing templates. Defaults to False.
        verbose (bool): Whether to print detailed information during generation. 
            Defaults to False.
        
    Returns:
        int: Number of templates successfully created
        
    Example:
        >>> count = generate_all_templates("/path/to/templates", force=True, verbose=True)
        Generating templates in: /path/to/templates
        Force overwrite: enabled
        Created template: /path/to/templates/Lecture Note.md
        Created template: /path/to/templates/Case Study.md
        Generation complete.
        2 templates were created.
        >>> print(count)
        2
    """
    created = 0
    if verbose:
        print(f"Generating templates in: {template_folder}")
        print("Force overwrite:", "enabled" if force else "disabled")
    
    # Define templates with their tags and content
    templates = [
        {
            "name": "MBA Lecture Note",
            "tags": ["type/note/lecture", "mba/course/", "status/active"],
            "content": """# {{title}}

## Key Concepts

- 

## Notes

-

## Questions & Follow-ups

-

## References

-
"""
        },
        {
            "name": "MBA Case Study",
            "tags": ["type/note/case-study", "mba/course/", "status/active"],
            "content": """# {{title}}

## Case Overview

## Key Issues

## Analysis

## Recommendations

## Lessons Learned

## References
"""
        },
        {
            "name": "MBA Assignment",
            "tags": ["type/assignment/individual", "mba/course/", "status/active", "priority/high"],
            "content": """# {{title}}

## Assignment Overview

## Requirements

## Work

## References

## Submission Notes
"""
        },
        {
            "name": "MBA Group Project",
            "tags": ["type/project/active", "mba/course/", "status/active", "priority/high"],
            "content": """# {{title}}

## Project Overview

## Team Members

- 

## Timeline

- Start date: 
- Milestones:
- Due date: 

## Deliverables

- 

## Progress

-

## References
"""
        },
        {
            "name": "MBA Course Dashboard",
            "tags": ["type/note/dashboard", "mba/course/"],
            "content": """# {{title}} Dashboard

## Course Information

- Professor: 
- Schedule: 
- Office Hours: 

## Key Dates

- 

## Assignments

```dataview
TABLE file.cday as "Created", file.mtime as "Last Modified" 
FROM #type/assignment AND #mba/course/ AND "{{title}}"
SORT file.mtime DESC
```

## Notes

```dataview
TABLE file.cday as "Created", file.mtime as "Last Modified" 
FROM #type/note/lecture AND #mba/course/ AND "{{title}}"
SORT file.mtime DESC
```

## Resources

```dataview
TABLE file.cday as "Created", file.mtime as "Last Modified" 
FROM #type/resource AND #mba/course/ AND "{{title}}"
SORT file.mtime DESC
```
"""
        },
        {
            "name": "MBA Finance Note",
            "tags": ["type/note/lecture", "mba/course/finance", "status/active"],
            "content": """# {{title}}

## Key Financial Concepts

-

## Formulas & Models

-

## Analysis & Applications

-

## Examples

-

## Questions & Follow-ups

-

## References

-
"""
        },
        {
            "name": "MBA Literature Review",
            "tags": ["type/note/literature", "mba/course/", "status/active"],
            "content": """# {{title}}

## Citation
Author(s): 
Year: 
Source: 

## Key Points

-

## Methodology

-

## Findings

-

## Relevance to My Research/Project

-

## Critical Analysis

-

## Quotable Passages

-
"""
        }
    ]
      # Create each template
    for template in templates:
        if create_template(
            template_folder,
            template["name"],
            template["tags"],
            template["content"],
            force=force,
            verbose=verbose
        ):
            created += 1
    
    if verbose:
        print(f"Generation complete.")
        if created == 0:
            print("No templates were created.")
        elif created == 1:
            print("1 template was created.")
        else:
            print(f"{created} templates were created.")
    
    return created

def main() -> None:
    """Main entry point for the template generation CLI tool.
    
    Parses command line arguments, determines the template destination directory
    based on the current operating system or user specification, and invokes
    the template generation functions. Provides summary output of the results.
    
    Args:
        None
        
    Returns:
        None: This function doesn't return a value
        
    Example:
        When called from the command line:
        $ notebook-generate-templates --force --verbose
    """
    parser = argparse.ArgumentParser(description="Generate Obsidian templates with nested tags.")
    parser.add_argument(
        "--template-path",
        default=None,
        help="Path to the template folder. If not specified, uses default locations based on OS."
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Force overwrite existing templates"
    )
    parser.add_argument(
        "--verbose", "-v",
        action="store_true",
        help="Print detailed information about template generation"
    )
    
    args = parser.parse_args()
    
    # Determine template folder path
    template_path = args.template_path
    if not template_path:
        if os.name == 'nt':  # Windows
            template_path = "D:\\Vault\\01_Projects\\MBA\\Templates"
        else:  # WSL or Linux
            template_path = "/mnt/d/Vault/01_Projects/MBA/Templates"
      # Generate templates
    num_created = generate_all_templates(
        template_path,
        force=args.force,
        verbose=args.verbose
    )
    return 0

if __name__ == "__main__":
    sys.exit(main())
