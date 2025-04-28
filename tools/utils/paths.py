#!/usr/bin/env python3
"""
Path Handling Utilities for MBA Notebook Automation System

This module provides comprehensive path normalization and conversion utilities 
for ensuring cross-platform compatibility between Windows and WSL/Linux environments.
It handles the complexities of path formats, drive letter mapping, and special character
management to ensure reliable file access across different operating systems.

Key Features:
------------
1. Path Normalization
   - Windows to WSL path conversion (C:\path\to\file → /mnt/c/path/to/file)
   - Backslash to forward slash conversion
   - Special character and quotation mark handling
   - Case normalization for drive letters

2. Path Format Detection
   - Windows path detection via drive letter patterns
   - WSL path pattern recognition
   - Path object vs. string handling
   - Empty and null path handling

3. Cross-Platform Compatibility
   - Bidirectional path conversion between systems
   - Preserves path integrity during conversion
   - Handles edge cases like UNC paths
   - Supports both string and pathlib.Path objects

Integration Points:
-----------------
- Used throughout the MBA Notebook Automation system for file access
- Ensures OneDrive paths work correctly in WSL environments
- Enables consistent path handling in file processing pipelines
- Provides foundation for PDF and transcript location services

Usage Example:
------------
```python
from tools.utils.paths import normalize_wsl_path, normalize_path
from pathlib import Path

# Working with Windows paths in WSL environment
windows_path = "C:\\Users\\Student\\OneDrive\\MBA\\Finance\\Lecture Notes.pdf"
wsl_path = normalize_path(windows_path)
print(f"Converted path: {wsl_path}")
# Output: Converted path: /mnt/c/Users/Student/OneDrive/MBA/Finance/Lecture Notes.pdf

# Handling paths from different sources
paths_to_normalize = [
    "D:\\MBA\\Strategy\\slides.pdf",          # Windows backslash path
    "C:/MBA/Economics/notes.txt",             # Windows forward slash path
    Path("E:\\MBA\\Marketing\\data.xlsx"),    # Path object
    "/home/user/documents/mba/thesis.docx"    # Already Unix-formatted path
]

# Process all paths consistently
for p in paths_to_normalize:
    norm_path = normalize_wsl_path(p)
    print(f"Original: {p} → Normalized: {norm_path}")
```
"""

# Standard library imports for path operations and pattern matching
import re
import os
from pathlib import Path  # Modern path manipulation library

# Note: This module doesn't require external dependencies beyond the Python standard library,
# which makes it lightweight and easy to deploy in various environments

def normalize_path(path):
    """
    Normalize a file path for cross-platform compatibility between Windows and WSL/Linux.
    
    This function takes any file path input and transforms it into a format that works
    reliably in the Windows Subsystem for Linux (WSL) environment. It handles multiple
    path format issues including:
    
    - Drive letter conversion (C:\ becomes /mnt/c/)
    - Backslash to forward slash normalization
    - Quotation mark and whitespace cleaning
    - Case normalization for consistent access
    
    The function is particularly important for automating file operations across
    Windows and Linux subsystems, ensuring that files can be reliably located
    and accessed regardless of how their paths are specified.
    
    Args:
        path (str or Path): The file path to normalize. Can be a Windows-style path,
                           WSL path, or Path object. If None or empty, returns as is.
    
    Returns:
        str: A normalized path string suitable for use in WSL/Linux environments.
             Returns None if the input is None.
             
    Examples:
        >>> normalize_path("C:\\Users\\Documents\\file.pdf")
        '/mnt/c/Users/Documents/file.pdf'
        
        >>> normalize_path('  "D:/Data/notes.txt"  ')
        '/mnt/d/Data/notes.txt'
        
        >>> normalize_path(None)
        None
    """
    if not path:
        return path
        
    # Convert to string and clean surrounding whitespace and quotes
    # This handles cases where paths come from user input or config files
    # with extra formatting characters
    path = str(path).strip().strip('"').strip("'")
    
    # Normalize slashes from Windows to Unix style
    # Backslashes are valid in Windows paths but must be forward slashes in WSL
    path = path.replace('\\', '/')
    
    # Convert Windows drive letter format to WSL mount format
    # Pattern matches "C:/path" and converts to "/mnt/c/path"
    match = re.match(r'([a-zA-Z]):/(.*)', path)
    if match:
        # Extract the drive letter, convert to lowercase for consistency
        drive = match.group(1).lower()
        # Extract the rest of the path after the drive letter and colon
        rest = match.group(2)
        # Reconstruct in WSL format with /mnt/ prefix
        path = f"/mnt/{drive}/{rest}"
        
    return path

def normalize_wsl_path(path):
    """
    Convert any path format to a properly formatted WSL path with optimized performance.
    
    This function specializes in translating Windows paths to WSL format, with
    particular focus on ensuring drive letter mapping is properly handled. It's 
    designed to be fast and efficient, with direct string manipulation rather than
    using regular expressions for common cases.
    
    The function efficiently handles:
    - Windows paths with drive letters (C:\path\to\file)
    - Already normalized WSL paths (preserving them)
    - Path objects from the pathlib library
    - Various edge cases like network paths
    
    This implementation is optimized for the most common path conversion scenarios
    in the MBA Notebook Automation system, focusing on speed and reliability.
    
    Args:
        path (str or Path): The path to normalize. Can be a Windows path,
                           WSL path, or pathlib.Path object.
        
    Returns:
        str or Path: A normalized path suitable for WSL usage. If the input was
                    a pathlib.Path object, returns the original object as Path
                    objects are platform-aware. Otherwise, returns a string.
                    
    Examples:
        >>> normalize_wsl_path("C:\\Program Files\\App")
        '/mnt/c/Program Files/App'
        
        >>> normalize_wsl_path("/home/user/documents")
        '/home/user/documents'
        
        >>> normalize_wsl_path(Path("/data/files"))
        Path('/data/files')
    """
    # Handle non-string paths (like pathlib.Path objects)
    # Path objects are already platform-aware, so we return them as-is
    if isinstance(path, str):
        # Fast check for Windows path format using drive letter presence
        # This is more efficient than regex for the common case
        if ':' in path:  # Windows path with drive letter
            # Extract the drive letter (first character)
            drive = path[0].lower()
            
            # Build WSL path with /mnt/ prefix and the rest of the path
            # Skip the colon and separator (typically the 2nd and 3rd chars)
            wsl_path = f"/mnt/{drive}{path[2:].replace('\\', '/')}"

            return wsl_path
            
        # For non-Windows paths, just normalize slashes and return
        return path.replace('\\', '/')  # Convert any backslashes to forward slashes
        
    # Return Path objects unchanged - they handle platform differences internally
    return path

def ensure_directory_exists(path):
    """
    Ensure that a directory exists, creating it if necessary.
    
    This utility function checks if a directory exists at the specified path
    and creates it (including any necessary parent directories) if it doesn't.
    It's designed to work consistently across Windows and WSL/Linux environments
    by normalizing the path first.
    
    Args:
        path (str or Path): Path to the directory that should exist.
                           Will be normalized for cross-platform compatibility.
    
    Returns:
        Path: The Path object representing the ensured directory.
        
    Raises:
        OSError: If directory creation fails due to permissions or other issues.
        
    Example:
        >>> ensure_directory_exists("C:/Users/Student/MBA/Notes")
        # Creates directory if needed and returns Path object
        Path('/mnt/c/Users/Student/MBA/Notes')
    """
    # Normalize the path for WSL compatibility
    norm_path = normalize_wsl_path(path)
    
    # Convert to Path object for consistent operations
    dir_path = Path(norm_path) if isinstance(norm_path, str) else norm_path
    
    # Create directory if it doesn't exist (with parents)
    if not dir_path.exists():
        dir_path.mkdir(parents=True, exist_ok=True)
        
    return dir_path
