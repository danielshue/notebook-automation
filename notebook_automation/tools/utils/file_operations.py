#!/usr/bin/env python3
"""
File System Operations Module for Notebook Automation System

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
from notebook_automation.tools.utils.file_operations import find_all_pdfs, get_vault_path_for_pdf, get_scan_root
from notebook_automation.tools.utils.config import RESOURCES_ROOT

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
from typing import List, Optional, Tuple, Union, Dict

# Import configuration settings for path constants
from notebook_automation.tools.utils.config import NOTEBOOK_VAULT_ROOT, ONEDRIVE_LOCAL_RESOURCES_ROOT
from ..utils.config import logger

def find_all_pdfs(root: Path) -> list[Path]:
    """Recursively discover all PDF documents within a specified directory tree.
    
    This function performs an efficient recursive traversal of the provided directory
    tree, identifying all PDF files using extension matching. It filters out non-file
    objects (like symbolic links or directories that happen to end with .pdf) and
    returns only true files. The search is case-insensitive to handle PDFs with
    uppercase extensions.
    
    Args:
        root (Path): The root directory to begin the recursive search from.
                    Should be a pathlib.Path object pointing to a valid directory.
        
    Returns:
        list[Path]: A list of Path objects for each PDF file found in the directory tree.
              Returns an empty list if no PDFs are found or if the root doesn't exist.
              Each Path in the list is an absolute path to a PDF file.
    
    Example:
        >>> from pathlib import Path
        >>> pdfs = find_all_pdfs(Path("./course_materials"))
        >>> print(f"Found {len(pdfs)} PDFs")
        Found 12 PDFs
              
    Performance Note:
        For very large directory trees with many files, this function may take
        significant time to complete. Consider using more targeted subdirectories
        when working with extensive file collections.
    """
    # Use pathlib's recursive glob (rglob) to find all files with .pdf extension
    # Filter with is_file() to ensure we only get actual files, not directories or symlinks
    # This one-liner efficiently combines finding and filtering in a list comprehension
    return [p for p in root.rglob("*.pdf") if p.is_file()]

def get_vault_path_for_pdf(onedrive_pdf_path: Path) -> Path:
    """Map an OneDrive PDF path to its corresponding location in the Obsidian Vault.
    
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
              
    Example:
        >>> from pathlib import Path
        >>> pdf_path = Path("/onedrive/courses/finance/lecture1.pdf")
        >>> vault_path = get_vault_path_for_pdf(pdf_path)
        >>> print(vault_path)
        /vault/courses/finance
              be located if it were in the Vault, preserving the folder structure.
              
    Error Handling:
        If the provided path is not within the configured OneDrive root, the function
        falls back to using the PDF's parent directory and logs a warning. This ensures
        the function always returns a usable path even with unexpected inputs.
    """
    try:
        # Get the relative path from OneDrive root
        # This extracts the subfolder structure that should be preserved in the vault
        rel_path = onedrive_pdf_path.relative_to(ONEDRIVE_LOCAL_RESOURCES_ROOT)
        
        # Create the same path structure in the Vault
        # This maps the OneDrive structure into the Obsidian vault
        vault_path = NOTEBOOK_VAULT_ROOT / rel_path
        
        # Return the directory where the note should be placed
        # We use parent() to get the directory, not the file itself
        return vault_path.parent
    except ValueError:
        # If the path is not within OneDrive root, just use its parent directory
        # This provides a fallback for files outside the expected structure
        logger.warning(f"Path {onedrive_pdf_path} is not within OneDrive root")
        return onedrive_pdf_path.parent

def get_scan_root(folder_path: Optional[Union[str, Path]] = None) -> Optional[Path]:
    """Intelligently resolve the root directory to use for file scanning operations.
    
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
    scan_root = ONEDRIVE_LOCAL_RESOURCES_ROOT
    
    # If a specific folder path is provided, resolve it appropriately
    if folder_path:
        # Convert string path to Path object
        scan_root = Path(folder_path)
        
        # If it's a relative path, resolve it against the RESOURCES_ROOT
        if not scan_root.is_absolute():
            scan_root = ONEDRIVE_LOCAL_RESOURCES_ROOT / scan_root
        
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

def find_files_by_extension(root_path: Union[str, Path], 
                      extension: str = ".pdf", 
                      case_sensitive: bool = False) -> List[Path]:
    """Recursively find all files with a specific extension within a directory tree.
    
    This function performs a recursive traversal of the provided directory
    tree, identifying all files with the specified extension. It filters out 
    non-file objects (like symbolic links or directories) and returns only 
    true files. The search is case-insensitive by default, but can be made 
    case-sensitive if needed.
    
    Args:
        root_path (Union[str, Path]): The root directory to begin the recursive search from.
            Should be a pathlib.Path object or a string path to a valid directory.
        extension (str, optional): The file extension to search for, including the period.
            Defaults to ".pdf".
        case_sensitive (bool, optional): Whether to perform case-sensitive matching on extensions.
            Defaults to False (case-insensitive).
        
    Returns:
        List[Path]: A list of Path objects for each matching file found in the directory tree.
            Returns an empty list if no matching files are found or if the root doesn't exist.
            Each Path in the list is an absolute path to a file.
    
    Raises:
        None: The function handles directory existence checks internally, returning an empty list
            if the directory doesn't exist or isn't a directory.
            
    Example:
        >>> # Find all MP4 videos
        >>> videos = find_files_by_extension("/path/to/videos", ".mp4")
        >>> print(f"Found {len(videos)} video files")
        
        >>> # Find all Excel files (case-sensitive)
        >>> excel_files = find_files_by_extension(Path("/path/to/data"), ".XLSX", case_sensitive=True)
    
    Notes:
        For very large directory trees with many files, this function may take
        significant time to complete. Consider using more targeted subdirectories
        when working with extensive file collections.
    """
    # Convert string path to Path object if needed
    if isinstance(root_path, str):
        root_path = Path(root_path)
        
    # Check if directory exists
    if not root_path.exists() or not root_path.is_dir():
        logger.warning(f"Directory {root_path} does not exist or is not a directory")
        return []
        
    result_files = []
    logger.debug(f"Searching for *{extension} files in {root_path}")
    
    # Recursively walk through directory tree
    for root, _, files in os.walk(root_path):
        for file in files:
            # Perform case-sensitive or case-insensitive comparison as specified
            if case_sensitive and file.endswith(extension):
                file_path = Path(os.path.join(root, file))
                result_files.append(file_path)
                logger.debug(f"Found file: {file_path}")
            elif not case_sensitive and file.lower().endswith(extension.lower()):
                file_path = Path(os.path.join(root, file))
                result_files.append(file_path)
                logger.debug(f"Found file: {file_path}")
    
    logger.info(f"Found {len(result_files)} files with extension {extension} in {root_path}")
    return result_files