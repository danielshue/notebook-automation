"""
Path utilities for MBA Notebook Automation.

Provides helpers for normalizing and manipulating file paths, including WSL compatibility.
"""
from pathlib import Path
import os

def normalize_path(path: str | Path) -> Path:
    """Normalize a path string or Path object to a Path object.

    Args:
        path (str | Path): The path to normalize.

    Returns:
        Path: Normalized Path object.
    """
    return Path(path).expanduser().resolve()

def normalize_wsl_path(path: str | Path) -> str:
    """Normalize a path for WSL compatibility (if needed).

    Args:
        path (str | Path): The path to normalize.

    Returns:
        str: Normalized path as a string, with WSL-style separators if on WSL.
    """
    p = Path(path)
    # If running on Windows, just return as string
    if os.name == 'nt':
        return str(p)
    # If running on WSL, convert to WSL path
    return str(p).replace('\\', '/')
