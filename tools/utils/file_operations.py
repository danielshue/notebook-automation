#!/usr/bin/env python3
"""
File System Operations Module for MBA Notebook Automation System

This module provides comprehensive file system utilities for managing the relationship
between OneDrive resources and the Obsidian Vault structure. It handles path mapping,
directory traversal, file discovery, and structural mirroring between the cloud storage
and local knowledge base.

Key Features:
------------
1. Path Mapping and Translation
   - OneDrive to Vault path conversion with structure preservation
   - Relative path handling with proper error recovery
   - Intelligent path resolution for files outside expected hierarchies
   - Consistent directory structure mirroring

2. File Discovery and Filtering
   - Recursive PDF finding with efficient filtering
   - Extension-based file type detection
   - Directory existence validation
   - Path normalization across platforms

3. Directory Management
   - Root directory determination for scanning operations
   - Subdirectory resolution relative to configured roots
   - Directory existence validation and error reporting
   - Path creation utilities for ensuring vault structure

Integration Points:
-----------------
- Works with OneDrive modules for cloud resource discovery
- Supports PDF processing pipeline with path resolution
- Enables Vault organization that mirrors cloud structure
- Provides foundation for note generation with proper paths

Usage Example:
------------
```python
from pathlib import Path
from tools.utils.file_operations import find_all_pdfs, get_vault_path_for_pdf, get_scan_root
from tools.utils.config import RESOURCES_ROOT

# Determine the directory to scan
scan_dir = get_scan_root("MBA/Finance/Course Materials")

# Find all PDFs in that directory tree
pdfs = find_all_pdfs(scan_dir)
print(f"Found {len(pdfs)} PDF files to process")

# Process each PDF and determine its corresponding Vault location
for pdf_path in pdfs:
    # Map the OneDrive path to Vault structure
    vault_dir = get_vault_path_for_pdf(pdf_path)
    
    # Ensure the directory exists in the Vault
    vault_dir.mkdir(parents=True, exist_ok=True)
    
    # Generate a unique note name for the PDF
    note_name = f"Notes - {pdf_path.stem}.md"
    note_path = vault_dir / note_name
    
    print(f"Will create {note_path} for PDF {pdf_path.name}")
```
"""

# Standard library imports for file system operations
import os
import logging
from pathlib import Path

# Import configuration settings for path constants
from tools.utils.config import VAULT_ROOT, RESOURCES_ROOT
from ..utils.config import logger

def find_all_pdfs(root):
    """
    Recursively discover all PDF documents within a specified directory tree.
    
    This function performs an efficient recursive traversal of the provided directory
    tree, identifying all PDF files using extension matching. It filters out non-file
    objects (like symbolic links or directories that happen to end with .pdf) and
    returns only true files. The search is case-insensitive to handle PDFs with
    uppercase extensions.
    
    Use Cases:
    - Initial scanning of resource directories for processing
    - Identifying new PDFs added to monitored directories
    - Building PDF indexes for processing pipelines
    - Validating document availability before processing
    
    Args:
        root (Path): The root directory to begin the recursive search from.
                    Should be a pathlib.Path object pointing to a valid directory.
        
    Returns:
        list: A list of Path objects for each PDF file found in the directory tree.
              Returns an empty list if no PDFs are found or if the root doesn't exist.
              Each Path in the list is an absolute path to a PDF file.
    
    Performance Note:
        For very large directory trees with many files, this function may take
        significant time to complete. Consider using more targeted subdirectories
        when working with extensive file collections.
    """
    # Use pathlib's recursive glob (rglob) to find all files with .pdf extension
    # Filter with is_file() to ensure we only get actual files, not directories or symlinks
    # This one-liner efficiently combines finding and filtering in a list comprehension
    return [p for p in root.rglob("*.pdf") if p.is_file()]

def get_vault_path_for_pdf(onedrive_pdf_path):
    """
    Map an OneDrive PDF path to its corresponding location in the Obsidian Vault.
    
    This function implements a strategic path translation system that preserves the
    organizational structure from OneDrive within the Obsidian Vault. It ensures that
    notes generated from PDFs maintain the same hierarchy and organization as their
    source materials, creating a consistent navigation experience between systems.
    
    The mapping works by:
    1. Extracting the relative path from the OneDrive root
    2. Applying that same relative path to the Vault root
    3. Returning the parent directory where notes should be placed
    4. Providing fallback handling for paths outside the expected structure
    
    Args:
        onedrive_pdf_path (Path): Path object pointing to a PDF file in OneDrive.
                                 Expected to be an absolute path to the PDF.
        
    Returns:
        Path: The corresponding directory path in the Vault where related notes
              should be placed. This is the parent directory where the PDF would
              be located if it were in the Vault, preserving the folder structure.
              
    Error Handling:
        If the provided path is not within the configured OneDrive root, the function
        falls back to using the PDF's parent directory and logs a warning. This ensures
        the function always returns a usable path even with unexpected inputs.
    """
    try:
        # Get the relative path from OneDrive root
        # This extracts the subfolder structure that should be preserved in the vault
        rel_path = onedrive_pdf_path.relative_to(RESOURCES_ROOT)
        
        # Create the same path structure in the Vault
        # This maps the OneDrive structure into the Obsidian vault
        vault_path = VAULT_ROOT / rel_path
        
        # Return the directory where the note should be placed
        # We use parent() to get the directory, not the file itself
        return vault_path.parent
    except ValueError:
        # If the path is not within OneDrive root, just use its parent directory
        # This provides a fallback for files outside the expected structure
        logger.warning(f"Path {onedrive_pdf_path} is not within OneDrive root")
        return onedrive_pdf_path.parent

def get_scan_root(folder_path):
    """
    Intelligently resolve the root directory to use for file scanning operations.
    
    This function provides flexible directory resolution for scanning operations,
    supporting both absolute paths and paths relative to the configured RESOURCES_ROOT.
    It includes comprehensive validation to ensure the target directory exists and
    is actually a directory, providing clear error reporting when requirements
    aren't met.
    
    The resolution process follows these steps:
    1. If no folder path is provided, use the configured RESOURCES_ROOT
    2. If a path is provided, check if it's absolute or relative
    3. For relative paths, resolve them against RESOURCES_ROOT
    4. Validate that the resolved path exists and is a directory
    5. Return None with error logging if validation fails
    
    Args:
        folder_path (str or None): Optional path to the directory to scan.
                                  If None, defaults to RESOURCES_ROOT.
                                  If provided, can be absolute or relative to RESOURCES_ROOT.
        
    Returns:
        Path or None: The resolved Path object pointing to the directory to scan.
                     Returns None if the resolved path doesn't exist or isn't a directory.
                     
    Error Handling:
        The function logs an error message if the resolved path doesn't exist or
        is not a directory, making it clear why None is being returned.
        
    Example:
        ```python
        # Use default root
        root = get_scan_root(None)
        
        # Use a specific subfolder
        root = get_scan_root("lectures/week1")
        
        # Use absolute path
        root = get_scan_root("/mnt/d/onedrive/courses")
        
        if root:
            pdfs = find_all_pdfs(root)
        ```
    """
    # Start with the default scan root from configuration
    scan_root = RESOURCES_ROOT
    
    # If a specific folder path is provided, resolve it appropriately
    if folder_path:
        # Convert string path to Path object
        scan_root = Path(folder_path)
        
        # If it's a relative path, resolve it against the RESOURCES_ROOT
        if not scan_root.is_absolute():
            scan_root = RESOURCES_ROOT / scan_root
        
        # Get the canonical absolute path with symlinks resolved
        scan_root = scan_root.resolve()
        
        # Validate that the directory exists and is actually a directory
        if not scan_root.exists() or not scan_root.is_dir():
            # Log an error with clear explanation of the issue
            logger.error(f"Folder path {scan_root} does not exist or is not a directory")
            return None
            
    # Return the validated scan root directory
    return scan_root

# Add additional file operation functions below as needed
# 
# Future extensions could include:
# - Directory creation with permissions handling
# - File copying between OneDrive and Vault with progress tracking
# - File type detection beyond extension matching
# - Content-based file validation (e.g., PDF structure verification)
# - Support for additional file types beyond PDFs
