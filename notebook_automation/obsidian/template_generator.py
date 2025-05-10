#!/usr/bin/env python3
"""
Generate Obsidian Templates with Nested Tags

This script creates Obsidian template files with the nested tag structure
for different types of notes.

Usage:
    python generate_obsidian_templates.py <template_folder_path>
"""

import os
import sys
import yaml
from pathlib import Path

def create_template(template_path, template_name, tags, template_content):
    """
    Create an Obsidian template with the specified tags.
    
    Args:
        template_path (str): Path to the template folder
        template_name (str): Name of the template file (without extension)
        tags (list): List of tags to include
        template_content (str): Content to include after the frontmatter
        
    Returns:
        bool: True if successful, False otherwise
    """
    os.makedirs(template_path, exist_ok=True)
    
    file_path = os.path.join(template_path, f"{template_name}.md")
    
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
            
        print(f"Created template: {file_path}")
        return True
        
    except Exception as e:
        print(f"Error creating template {template_name}: {e}")
        return False

def generate_all_templates(template_folder):
    """
    Generate all templates for the MBA vault.
    
    Args:
        template_folder (str): Path to the template folder
        
    Returns:
        int: Number of templates created
    """
    created = 0
    
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
        if create_template(template_folder, template["name"], template["tags"], template["content"]):
            created += 1
    
    return created

if __name__ == "__main__":
    # Determine template folder path
    if len(sys.argv) > 1:
        template_path = sys.argv[1]
    else:
        # Default template folder locations
        if os.name == 'nt':  # Windows
            template_path = "D:\\Vault\\01_Projects\\MBA\\Templates"
        else:  # WSL or Linux
            template_path = "/mnt/d/Vault/01_Projects/MBA/Templates"
    
    # Generate templates
    num_created = generate_all_templates(template_path)
    print(f"Successfully created {num_created} templates in {template_path}")
