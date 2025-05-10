#!/usr/bin/env python
"""
Markdown Conversion Utilities for Notebook Automation

This module provides shared functionality for converting various file formats to markdown,
used by both the markdown generator and index generator tools.
"""

import os
import html2text
from pathlib import Path
from typing import Optional, Tuple

def convert_html_to_markdown(html_content: str) -> str:
    """Convert HTML content to markdown format.
    
    Args:
        html_content (str): The HTML content to convert
        
    Returns:
        str: The converted markdown content
    """
    h = html2text.HTML2Text()
    h.body_width = 0  # Don't wrap lines
    h.ignore_images = False
    h.ignore_links = False
    h.ignore_emphasis = False
    h.ignore_tables = False
    return h.handle(html_content)

def convert_txt_to_markdown(txt_content: str, filename: Optional[str] = None) -> str:
    """Convert text content to markdown format.
    
    Args:
        txt_content (str): The text content to convert
        filename (str, optional): Original filename for metadata
        
    Returns:
        str: The converted markdown content
    """
    # For text files, we might want to add some basic formatting
    # or extract title from filename for headers, etc.
    if filename:
        title = Path(filename).stem.replace('-', ' ').replace('_', ' ').title()
        return f"# {title}\n\n{txt_content}"
    return txt_content

def process_file(
    src_file: str,
    dest_file: str,
    dry_run: bool = False,
) -> Tuple[bool, Optional[str]]:
    """Process a single file for conversion to markdown.
    
    Args:
        src_file (str): Path to the source file
        dest_file (str): Path where to save the converted file
        dry_run (bool): If True, don't write any files
        
    Returns:
        Tuple[bool, Optional[str]]: Success status and error message if any
    """
    try:
        # Read the source file
        with open(src_file, 'r', encoding='utf-8') as f:
            content = f.read()
            
        # Convert based on file type
        if src_file.endswith('.html'):
            converted_content = convert_html_to_markdown(content)
        elif src_file.endswith('.txt'):
            converted_content = convert_txt_to_markdown(content, Path(src_file).name)
        else:
            return False, f"Unsupported file type: {src_file}"
            
        if not dry_run:
            # Create the destination directory if needed
            os.makedirs(os.path.dirname(dest_file), exist_ok=True)
            
            # Write the converted content
            with open(dest_file, 'w', encoding='utf-8') as f:
                f.write(converted_content)
                
        return True, None
        
    except Exception as e:
        return False, str(e)
