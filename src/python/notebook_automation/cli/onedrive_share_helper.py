"""
OneDrive Share Helper Module.

This module provides helper functions for generating OneDrive share links
without duplicating functionality across different CLI tools.
"""

import os
import logging
from pathlib import Path
from typing import Dict, Optional, Union, Any

from notebook_automation.cli.onedrive_share import create_sharing_link, authenticate_interactive
from notebook_automation.tools.utils.config import ONEDRIVE_BASE, setup_logging, ensure_logger_configured

# Configure module logger with safe initialization
logger = ensure_logger_configured(__name__)

def create_share_link_once(file_path: Path, access_token: str = None, timeout: int = 15) -> Union[Dict[str, str], str, None]:
    """Get a shareable link for the given file using the imported function from onedrive_share.py.
    
    This function is designed to only authenticate once if no token is provided,
    and to handle proper path resolution for OneDrive sharing.
    
    Args:
        file_path: Path to the file to share
        access_token: Optional pre-existing access token
        timeout: Request timeout in seconds
        
    Returns:
        Share link information (dict with webUrl and embedHtml) or string URL
    """
    logger.debug(f"Creating share link for: {file_path}")
      # Use provided access token or authenticate if none provided
    if not access_token:
        logger.info("No access token provided, authenticating with Microsoft Graph API...")
        # Call the authentication function but let it handle its own logging
        access_token = authenticate_interactive()
        if not access_token:
            logger.error("Failed to authenticate for OneDrive sharing")
            return None
    
    # file_path should be relative to OneDrive root, e.g. /Education/MBA-Resources/...
    file_path_str = str(file_path).replace("\\", "/")
    logger.debug(f"Normalized file path: {file_path_str}")
    
    # Try to find the OneDrive base path within the file path
    marker = ONEDRIVE_BASE if ONEDRIVE_BASE.startswith("/") else "/" + ONEDRIVE_BASE
    idx = file_path_str.find(marker)
    
    if idx == -1:
        # Try without leading slash
        marker = ONEDRIVE_BASE.lstrip("/")
        idx = file_path_str.find(marker)
        
        if idx == -1:
            # Fallback: use the filename only (will likely fail, but avoids crash)
            rel_path = os.path.basename(file_path_str)
            logger.warning(f"Could not determine relative path for {file_path_str}, using filename only: {rel_path}")
        else:
            rel_path = "/" + file_path_str[idx:]
    else:
        rel_path = file_path_str[idx:]
        if not rel_path.startswith("/"):
            rel_path = "/" + rel_path
    
    logger.debug(f"Resolved OneDrive relative path: {rel_path}")
    
    # Generate the share link
    try:
        result = create_sharing_link(access_token, rel_path)
        if result:
            logger.debug(f"Successfully created share link: {result}")
        else:
            logger.warning(f"Failed to create share link for {rel_path}")
        return result
    except Exception as e:
        logger.error(f"Error creating share link: {str(e)}")
        return None
