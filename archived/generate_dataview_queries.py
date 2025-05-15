#!/usr/bin/env python3
"""
Generate Dataview Queries for MBA Tag Structure

This script generates example Dataview queries that leverage the nested
tag structure for MBA notes organization.

Usage:
    python generate_dataview_queries.py <output_file_path>
"""

import os
import sys

# Dictionary of query categories and their corresponding queries
DATAVIEW_QUERIES = {
    "Course Content": [
        {
            "title": "Finance Course Materials",
            "description": "Shows all notes related to finance courses",
            "query": """
```dataview
TABLE file.path as "Location", file.cday as "Created"
FROM #mba/course/finance 
SORT file.cday DESC
```
"""
        },
        {
            "title": "Course-specific Lecture Notes",
            "description": "Shows lecture notes for a specific course",
            "query": """
```dataview
TABLE file.mtime as "Last Modified"
FROM #type/note/lecture AND #mba/course/finance/corporate-finance
SORT file.mtime DESC
```
"""
        },
        {
            "title": "Recent Case Studies",
            "description": "Displays recently modified case studies",
            "query": """
```dataview
TABLE file.mtime as "Last Modified"
FROM #type/note/case-study
SORT file.mtime DESC
LIMIT 10
```
"""
        }
    ],
    
    "Workflow Management": [
        {
            "title": "Active Assignments",
            "description": "Shows all active assignments across courses",
            "query": """
```dataview
TABLE
  file.cday as "Created",
  file.mtime as "Last Modified",
  regexreplace(list(filter(file.tags, (t) => startswith(t, "mba/course")))[0], "mba/course/([^/]*)/?(.*)?", "$1 $2") as "Course"
FROM #type/assignment AND #status/active 
SORT file.mtime DESC
```
"""
        },
        {
            "title": "High Priority Tasks",
            "description": "Shows high priority tasks that need attention",
            "query": """
```dataview
TABLE
  file.cday as "Created", 
  file.mtime as "Last Modified",
  regexreplace(list(filter(file.tags, (t) => startswith(t, "type/")))[0], "type/([^/]*)/?(.*)?", "$1 $2") as "Type"
FROM #priority/high AND #status/active
SORT file.mtime DESC
```
"""
        },
        {
            "title": "Upcoming Group Projects",
            "description": "Shows active group projects",
            "query": """
```dataview
TABLE file.mtime as "Last Modified"
FROM #type/project/active AND #type/assignment/group
SORT file.mtime DESC
```
"""
        }
    ],
    
    "Content Exploration": [
        {
            "title": "Content by Tool",
            "description": "Shows content categorized by tool/software used",
            "query": """
```dataview
TABLE
  file.folder as "Location",
  regexreplace(list(filter(file.tags, (t) => startswith(t, "mba/course")))[0], "mba/course/([^/]*)/?(.*)?", "$1 $2") as "Course"
FROM #mba/tool/excel
SORT file.folder ASC
```
"""
        },
        {
            "title": "Skill Development Tracker",
            "description": "Shows content by skill being developed",
            "query": """
```dataview
TABLE
  file.path as "File",
  regexreplace(list(filter(file.tags, (t) => startswith(t, "mba/skill")))[0], "mba/skill/(.*)", "$1") as "Skill",
  file.mtime as "Last Modified"
FROM #mba/skill
SORT regexreplace(list(filter(file.tags, (t) => startswith(t, "mba/skill")))[0], "mba/skill/(.*)", "$1") ASC
```
"""
        }
    ],
    
    "Dashboard Elements": [
        {
            "title": "Course Summary Dashboard Element",
            "description": "A summary element for course dashboard",
            "query": """
```dataview
TABLE
  length(filter(file.tags, (t) => contains(t, "lecture"))) as "Lectures",
  length(filter(file.tags, (t) => contains(t, "assignment"))) as "Assignments",
  length(filter(file.tags, (t) => contains(t, "case-study"))) as "Case Studies" 
FROM #mba/course/finance/corporate-finance
GROUP BY "Summary"
```
"""
        },
        {
            "title": "Content Types by Course",
            "description": "Shows distribution of content types by course",
            "query": """
```dataview
TABLE
  length(rows) as "Count"
FROM #mba/course
GROUP BY regexreplace(list(filter(file.tags, (t) => startswith(t, "mba/course")))[0], "mba/course/([^/]*)/?(.*)?", "$1 $2") as "Course", 
         regexreplace(list(filter(file.tags, (t) => startswith(t, "type")))[0], "type/([^/]*)/?(.*)?", "$1 $2") as "Type"
SORT Course ASC
```
"""
        }
    ]
}

def generate_query_document(output_path):
    """
    Generate a markdown document with example Dataview queries.
    
    Args:
        output_path (str): Path to save the output document
        
    Returns:
        bool: True if successful, False otherwise
    """
    try:
        with open(output_path, 'w', encoding='utf-8') as f:
            # Write document header
            f.write("""---
tags:
  - type/resource/reference
  - mba/tool/dataview
  - status/reference
---

# MBA Nested Tag Structure: Example Dataview Queries

This document contains example Dataview queries that leverage the nested tag structure
to create powerful views and dashboards for your MBA content.

Each query includes a description and can be copied directly into your notes.

""")

            # Write each category and its queries
            for category, queries in DATAVIEW_QUERIES.items():
                f.write(f"## {category}\n\n")
                
                for i, query in enumerate(queries, 1):
                    f.write(f"### {query['title']}\n\n")
                    f.write(f"{query['description']}\n\n")
                    f.write(f"{query['query']}\n\n")
            
            # Write usage tips
            f.write("""## Tips for Custom Queries

### Working with Nested Tags

- Use the `startswith` function for tag category filtering: `startswith(t, "mba/course")`
- Use regex to extract parts of tag hierarchies: `regexreplace(tag, "mba/course/([^/]*)", "$1")`
- Combine multiple tag conditions: `#type/note/lecture AND #mba/course/finance`

### Useful Dataview Functions

- `list(filter(file.tags, (t) => startswith(t, "prefix")))` - Get tags matching a prefix
- `regexreplace(tag, pattern, replacement)` - Extract parts of tag hierarchies
- `length(filter(file.tags, condition))` - Count tags matching a condition

### Dashboard Techniques

- Use GROUP BY to create summary tables
- SORT and LIMIT to control result size
- Combine with file metadata like file.cday and file.mtime
""")

        print(f"Dataview query document created at: {output_path}")
        return True
        
    except Exception as e:
        print(f"Error generating query document: {e}")
        return False

if __name__ == "__main__":
    # Determine output path
    if len(sys.argv) > 1:
        output_path = sys.argv[1]
    else:
        # Default output location
        if os.name == 'nt':  # Windows
            output_path = "D:\\Vault\\01_Projects\\MBA\\Resources\\Dataview-Queries.md"
        else:  # WSL or Linux
            output_path = "/mnt/d/Vault/01_Projects/MBA/Resources/Dataview-Queries.md"
    
    # Make sure directory exists
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    
    # Generate the document
    generate_query_document(output_path)
