#!/usr/bin/env python3
"""
Generate Dataview Queries for Obsidian

This module provides a command-line interface for generating example Dataview queries 
that leverage the MBA note organization's nested tag structure.

Examples:
    vault-generate-dataview                      # Generate in default location
    vault-generate-dataview output.md            # Generate to specific file
    vault-generate-dataview --dry-run           # Preview without writing
"""

import os
import sys
import argparse
from pathlib import Path
from typing import Dict, List, Any, Optional

# Dictionary of query categories and their corresponding queries
DATAVIEW_QUERIES = {
    "Course Content": [
        {
            "title": "Finance Course Materials",
            "description": "Shows all notes related to finance courses",
            "query": '''```dataview
TABLE file.path as "Location", file.cday as "Created"
FROM #mba/course/finance 
SORT file.cday DESC
```'''
        },
        {
            "title": "Course-specific Lecture Notes",
            "description": "Shows lecture notes for a specific course",
            "query": '''```dataview
TABLE file.mtime as "Last Modified"
FROM #type/note/lecture AND #mba/course/finance/corporate-finance
SORT file.mtime DESC
```'''
        }
    ],
    
    "Workflow Management": [
        {
            "title": "Active Assignments",
            "description": "Shows all active assignments across courses",
            "query": '''```dataview
TABLE
  file.cday as "Created",
  file.mtime as "Last Modified",
  regexreplace(list(filter(file.tags, (t) => startswith(t, "mba/course")))[0], "mba/course/([^/]*)/?(.*)?", "$1 $2") as "Course"
FROM #type/assignment AND #status/active 
SORT file.mtime DESC
```'''
        },
        {
            "title": "High Priority Tasks",
            "description": "Shows high priority tasks that need attention",
            "query": '''```dataview
TABLE
  file.cday as "Created", 
  file.mtime as "Last Modified",
  regexreplace(list(filter(file.tags, (t) => startswith(t, "type/")))[0], "type/([^/]*)/?(.*)?", "$1 $2") as "Type"
FROM #priority/high AND #status/active
SORT file.mtime DESC
```'''
        }
    ],
    
    "Content Exploration": [
        {
            "title": "Content by Tool",
            "description": "Shows content categorized by tool/software used",
            "query": '''```dataview
TABLE
  file.folder as "Location",
  regexreplace(list(filter(file.tags, (t) => startswith(t, "mba/course")))[0], "mba/course/([^/]*)/?(.*)?", "$1 $2") as "Course"
FROM #mba/tool/excel
SORT file.folder ASC
```'''
        },
        {
            "title": "Skill Development Tracker",
            "description": "Shows content by skill being developed",
            "query": '''```dataview
TABLE
  file.path as "File",
  regexreplace(list(filter(file.tags, (t) => startswith(t, "mba/skill")))[0], "mba/skill/(.*)", "$1") as "Skill",
  file.mtime as "Last Modified"
FROM #mba/skill
SORT regexreplace(list(filter(file.tags, (t) => startswith(t, "mba/skill")))[0], "mba/skill/(.*)", "$1") ASC
```'''
        }
    ],
    
    "Dashboard Elements": [
        {
            "title": "Course Summary Dashboard",
            "description": "A summary element for course dashboard",
            "query": '''```dataview
TABLE
  length(filter(file.tags, (t) => contains(t, "lecture"))) as "Lectures",
  length(filter(file.tags, (t) => contains(t, "assignment"))) as "Assignments",
  length(filter(file.tags, (t) => contains(t, "case-study"))) as "Case Studies" 
FROM #mba/course/finance/corporate-finance
GROUP BY "Summary"
```'''
        }
    ]
}

def generate_query_document(output_path: Path, dry_run: bool = False) -> bool:
    """Generate a markdown document with example Dataview queries.
    
    Creates a comprehensive reference document containing example Dataview queries
    organized by category. The document includes proper YAML frontmatter, descriptions
    for each query, and usage tips. This serves as a reference guide for users to
    leverage the nested tag structure in their own Obsidian vault.
    
    Args:
        output_path (Path): Path where the output document should be saved
        dry_run (bool, optional): If True, show what would be written without
            actually making changes to the filesystem. Defaults to False.
        
    Returns:
        bool: True if document was successfully generated or simulated in dry run,
            False if any errors were encountered
            
    Example:
        >>> generate_query_document(Path("Reference/Dataview Queries.md"))
        Creating document: Reference/Dataview Queries.md
        Successfully created Dataview query reference document!
        True
    """
    try:
        # Prepare document content
        content = [
            "---",
            "tags:",
            "  - type/resource/reference",
            "  - mba/tool/dataview",
            "  - status/reference",
            "---",
            "",
            "# MBA Nested Tag Structure: Example Dataview Queries",
            "",
            "This document contains example Dataview queries that leverage the nested tag structure",
            "to create powerful views and dashboards for your MBA content.",
            "",
            "Each query includes a description and can be copied directly into your notes.",
            ""
        ]

        # Add each category and its queries
        for category, queries in DATAVIEW_QUERIES.items():
            content.append(f"## {category}\n")
            
            for query in queries:
                content.append(f"### {query['title']}\n")
                content.append(f"{query['description']}\n")
                content.append(f"{query['query']}\n")
        
        # Add usage tips
        content.extend([
            "## Tips for Custom Queries",
            "",
            "### Working with Nested Tags",
            "",
            "- Use the `startswith` function for tag category filtering: `startswith(t, \"mba/course\")`",
            "- Use regex to extract parts of tag hierarchies: `regexreplace(tag, \"mba/course/([^/]*)\", \"$1\")`",
            "- Combine multiple tag conditions: `#type/note/lecture AND #mba/course/finance`",
            "",
            "### Useful Dataview Functions",
            "",
            "- `list(filter(file.tags, (t) => startswith(t, \"prefix\")))` - Get tags matching a prefix",
            "- `regexreplace(tag, pattern, replacement)` - Extract parts of tag hierarchies",
            "- `length(filter(file.tags, condition))` - Count tags matching a condition",
            "",
            "### Dashboard Techniques",
            "",
            "- Use GROUP BY to create summary tables",
            "- SORT and LIMIT to control result size",
            "- Combine with file metadata like file.cday and file.mtime",
        ])

        # Preview or write the content
        if dry_run:
            print("\nPreview of query document content:")
            print("="*40)
            print("\n".join(content))
            print("="*40)
            print(f"\nFile would be written to: {output_path}")
            return True

        # Ensure output directory exists
        output_path.parent.mkdir(parents=True, exist_ok=True)

        # Write the content
        output_path.write_text("\n".join(content), encoding="utf-8")
        print(f"\nDataview query document created at: {output_path}")
        return True
        
    except Exception as e:
        print(f"Error generating query document: {e}")
        return False

def main():
    """Main entry point for the CLI tool."""
    parser = argparse.ArgumentParser(
        description="Generate Dataview queries for Obsidian",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
    vault-generate-dataview                      # Generate in default location
    vault-generate-dataview output.md            # Generate to specific file
    vault-generate-dataview --dry-run           # Preview without writing
"""
    )
    
    parser.add_argument("output_path", nargs="?", type=Path,
                       help="Path for output file (default: Dataview-Queries.md in vault)")
    parser.add_argument("--dry-run", action="store_true",
                       help="Preview content without writing file")
    parser.add_argument("--force", action="store_true",
                       help="Overwrite existing file if it exists")
    
    args = parser.parse_args()
    
    # Use default output path if not provided
    if not args.output_path:
        if os.name == "nt":  # Windows
            args.output_path = Path("D:/Vault/01_Projects/MBA/Resources/Dataview-Queries.md")
        else:  # WSL/Linux
            args.output_path = Path("/mnt/d/Vault/01_Projects/MBA/Resources/Dataview-Queries.md")

    # Check if file exists
    if args.output_path.exists() and not args.force and not args.dry_run:
        print(f"\nError: Output file already exists: {args.output_path}")
        print("Use --force to overwrite or --dry-run to preview")
        return 1

    # Generate the document
    success = generate_query_document(args.output_path, args.dry_run)
    return 0 if success else 1

if __name__ == "__main__":
    sys.exit(main())
