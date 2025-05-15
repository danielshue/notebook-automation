#!/usr/bin/env python
"""
Markdown Conversion Utilities for Notebook Automation.

This module provides shared functionality for converting various file formats to markdown,
used by both the markdown generator and index generator tools. It supports HTML and text files
conversion with proper formatting and metadata extraction.

Usage:
    from notebook_automation.utils.converters import convert_html_to_markdown, process_file
    
    # Convert HTML content to markdown
    markdown_content = convert_html_to_markdown("<h1>Hello World</h1>")
    
    # Process an entire file
    success, error = process_file("document.html", "document.md")
"""

import os
import html2text
from pathlib import Path
from typing import Optional, Tuple, Union

def convert_html_to_markdown(html_content: str) -> str:
    """Convert HTML content to markdown format.
    
    Transforms HTML content into well-formatted markdown text while preserving
    important elements like images, links, tables and text emphasis.
    
    Args:
        html_content (str): The HTML content to convert.
        
    Returns:
        str: The converted markdown content.
        
    Example:
        >>> convert_html_to_markdown("<h1>Title</h1><p>Paragraph with <b>bold</b> text.</p>")
        '# Title\\n\\nParagraph with **bold** text.\\n\\n'
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
    
    Converts plain text content to markdown, optionally adding a title header
    derived from the filename if provided.
    
    Args:
        txt_content (str): The text content to convert.
        filename (str, optional): Original filename for metadata extraction.
        
    Returns:
        str: The converted markdown content.
        
    Example:
        >>> convert_txt_to_markdown("Simple text content", "important-note.txt")
        '# Important Note\\n\\nSimple text content'
    """
    # For text files, we might want to add some basic formatting
    # or extract title from filename for headers, etc.
    if filename:
        title = Path(filename).stem.replace('-', ' ').replace('_', ' ').title()
        return f"# {title}\n\n{txt_content}"
    return txt_content

def process_file(
    src_file: Union[str, Path],
    dest_file: Union[str, Path],
    dry_run: bool = False,
) -> Tuple[bool, Optional[str]]:
    """Process a single file for conversion to markdown.
    
    Reads a source file, detects its type, converts it to markdown format
    and writes the result to the destination file. Supports HTML and text files.
    
    Args:
        src_file (Union[str, Path]): Path to the source file to convert.
        dest_file (Union[str, Path]): Path where to save the converted markdown file.
        dry_run (bool): If True, perform all operations but don't write any files.
            Useful for validation without making changes.
        
    Returns:
        Tuple[bool, Optional[str]]: A tuple containing:
            - bool: Success status (True if conversion was successful)
            - Optional[str]: Error message if any, None otherwise
            
    Raises:
        FileNotFoundError: When the source file cannot be found.
        PermissionError: When the destination file cannot be written.
            
    Example:
        >>> success, error = process_file("documents/page.html", "notes/page.md")
        >>> if success:
        ...     print("File converted successfully")
        ... else:
        ...     print(f"Error: {error}")
    """
    try:
        src_path = Path(src_file)
        dest_path = Path(dest_file)
        
        # Read the source file
        with open(src_path, 'r', encoding='utf-8') as f:
            content = f.read()
            
        # Convert based on file type
        if str(src_path).endswith('.html'):
            converted_content = convert_html_to_markdown(content)
        elif str(src_path).endswith('.txt'):
            converted_content = convert_txt_to_markdown(content, src_path.name)
        else:
            return False, f"Unsupported file type: {src_path}"
            
        if not dry_run:
            # Create the destination directory if needed
            os.makedirs(dest_path.parent, exist_ok=True)
            
            # Write the converted content
            with open(dest_path, 'w', encoding='utf-8') as f:
                f.write(converted_content)
                
        return True, None
        
    except FileNotFoundError as e:
        return False, f"Source file not found: {e}"
    except PermissionError as e:
        return False, f"Permission error writing destination file: {e}"
    except Exception as e:
        return False, str(e)
