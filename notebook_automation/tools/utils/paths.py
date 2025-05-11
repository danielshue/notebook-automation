"""
Path utilities for Notebook Automation.

This module provides helper functions for normalizing and manipulating file paths
across different operating systems, with special handling for Windows Subsystem for Linux
(WSL) compatibility. These utilities ensure consistent path handling throughout the
application regardless of the operating environment.

Usage:
    from notebook_automation.tools.utils.paths import normalize_path, normalize_wsl_path
    
    # Get a normalized Path object
    config_path = normalize_path("~/notebook_automation/config.json")
    
    # Get a string path that works in WSL contexts
    wsl_compatible_path = normalize_wsl_path("/mnt/d/notebooks")
"""
from pathlib import Path
import os
from typing import Union, Optional

def normalize_path(path: Union[str, Path]) -> Path:
    """Normalize a path string or Path object to a fully resolved Path object.

    Converts a path string or Path object to an absolute Path with user directory 
    expansion (~) and symbolic links resolved. This ensures consistent path 
    representation throughout the application.

    Args:
        path (Union[str, Path]): The path to normalize, either as a string or Path object.

    Returns:
        Path: Fully normalized and resolved Path object.
        
    Example:
        >>> normalize_path("~/Documents")
        PosixPath('/home/user/Documents')
        >>> normalize_path("../relative/path")
        PosixPath('/absolute/path/to/relative/path')
    """
    return Path(path).expanduser().resolve()

def normalize_wsl_path(path: Union[str, Path]) -> str:
    """Normalize a path for WSL compatibility.

    Ensures paths work correctly when using Windows Subsystem for Linux by
    converting path separators appropriately. This is particularly useful when
    passing paths between Windows and Linux contexts.

    Args:
        path (Union[str, Path]): The path to normalize.

    Returns:
        str: Normalized path as a string, with appropriate separators for the
            current operating system or WSL context.
            
    Example:
        >>> normalize_wsl_path(r"C:\Users\name\Documents")
        'C:/Users/name/Documents'  # When on WSL
        >>> normalize_wsl_path("/home/user/docs")
        '/home/user/docs'  # When on Windows
    """
    p = Path(path)
    # If running on Windows, just return as string
    if os.name == 'nt':
        return str(p)
    # If running on WSL, convert to WSL path
    return str(p).replace('\\', '/')
