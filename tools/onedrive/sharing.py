#!/usr/bin/env python3
"""
OneDrive Sharing Module for MBA Notebook Automation System

This module provides comprehensive OneDrive sharing functionality for the MBA 
Notebook Automation system. It enables the creation, management, and retrieval 
of OneDrive sharing links, supporting the system's needs for distributing 
educational resources.

Key Features:
------------
1. Sharing Link Generation
   - Creation of anonymous viewing links for various file types
   - Support for both path-based and ID-based sharing
   - Customizable permission levels and link types
   - Path normalization and resolution

2. Batch Processing
   - Efficient handling of multiple sharing operations
   - Progress tracking and reporting
   - Consistent error handling across operations

Integration Points:
-----------------
- Uses Microsoft Graph API for OneDrive access
- Works with the auth module for authentication
- Supports the notebook and resource generation pipelines
- Integrates with the file operations module for lookups
"""

import os
import time
import json
import requests
import urllib.parse
from pathlib import Path

from ..utils.config import GRAPH_API_ENDPOINT, ONEDRIVE_BASE, setup_logging
from ..auth.graph_auth import authenticate_graph_api
from ..utils.paths import normalize_path

# Set up logging
logger, failed_logger = setup_logging()

def convert_local_path_to_cloud_path(local_path):
    """
    Convert a local file system path to a OneDrive cloud path.
    
    This function handles different local path formats:
    - Windows paths like C:\\Users\\username\\OneDrive\\...
    - WSL paths like /mnt/c/Users/username/OneDrive/...
    
    Args:
        local_path (str): Local file system path to a file in OneDrive
        
    Returns:
        str: Relative path that can be used with OneDrive API or None if conversion failed
    """
    try:
        # Normalize path separators
        norm_path = local_path.replace('\\', '/')
        
        # Handle WSL paths like /mnt/c/Users/username/OneDrive/...
        if norm_path.startswith('/mnt/'):
            # Extract the drive letter from the path
            parts = norm_path.split('/')
            if len(parts) >= 3:
                # Turn /mnt/c/... into C:/...
                drive_letter = parts[2].upper()
                new_path = f"{drive_letter}:/{'/'.join(parts[3:])}"
                norm_path = new_path
        
        # Try to find the "OneDrive" folder in the path
        onedrive_marker = "OneDrive"
        onedrive_index = norm_path.find(onedrive_marker)
        
        if onedrive_index >= 0:
            # Get everything after "OneDrive/" in the path
            cloud_path = norm_path[onedrive_index + len(onedrive_marker) + 1:]
            return cloud_path
        
        # If we can't find "OneDrive" in the path, it might already be a relative path
        # Try to determine if this is already a OneDrive relative path
        if not norm_path.startswith('/') and not norm_path.startswith('http'):
            return norm_path
            
        logger.warning(f"Could not find OneDrive folder in path: {local_path}")
        return None
    
    except Exception as e:
        logger.error(f"Error converting local path to cloud path: {e}")
        return None

def get_item_id_by_path(access_token, file_path):
    """
    Get the OneDrive item ID for a given file path.
    
    Args:
        access_token (str): Microsoft Graph API access token
        file_path (str or Path): Path to the file in OneDrive
        
    Returns:
        str: The OneDrive item ID or None if not found
    """
    try:
        # Convert Path object to string if needed
        if isinstance(file_path, Path):
            file_path = str(file_path)
        
        # Convert local file system path to OneDrive cloud path
        # Handle paths like /mnt/c/Users/username/OneDrive/...
        # or C:\Users\username\OneDrive\...
        onedrive_cloud_path = convert_local_path_to_cloud_path(file_path)
        
        if not onedrive_cloud_path:
            logger.error(f"Could not convert local path to OneDrive cloud path: {file_path}")
            return None
            
        logger.debug(f"Converted local path to OneDrive cloud path: {file_path} -> {onedrive_cloud_path}")
        
        # URL encode the path
        encoded_path = urllib.parse.quote(onedrive_cloud_path)
        
        # URL encode the path
        encoded_path = urllib.parse.quote(onedrive_cloud_path)
        
        # Make the API call to get the item
        url = f"{GRAPH_API_ENDPOINT}/me/drive/root:/{encoded_path}"
        headers = {
            'Authorization': f'Bearer {access_token}',
            'Content-Type': 'application/json'
        }
        
        logger.debug(f"Getting item ID for path: {onedrive_cloud_path}")
        logger.debug(f"API URL: {url}")
        
        response = requests.get(url, headers=headers)
        response.raise_for_status()
        
        data = response.json()
        item_id = data.get('id')
        
        if item_id:
            logger.debug(f"Successfully got item ID: {item_id}")
        else:
            logger.error("Item ID not found in API response")
            
        return item_id
    
    except Exception as e:
        logger.error(f"Failed to get item ID for {file_path}: {e}")
        return None

def create_sharing_link_by_id(access_token, item_id, link_type="view", scope="anonymous"):
    """
    Create a sharing link for a OneDrive item based on its ID.
    
    Args:
        access_token (str): Microsoft Graph API access token
        item_id (str): OneDrive item ID
        link_type (str): Type of link - "view", "edit", or "embed"
        scope (str): Scope of access - "anonymous" or "organization"
        
    Returns:
        str: The sharing link URL or None if creation failed
    """
    try:
        # Make the API call to create sharing link
        url = f"{GRAPH_API_ENDPOINT}/me/drive/items/{item_id}/createLink"
        headers = {
            'Authorization': f'Bearer {access_token}',
            'Content-Type': 'application/json'
        }
        
        body = {
            "type": link_type,
            "scope": scope
        }
        
        response = requests.post(url, headers=headers, json=body)
        response.raise_for_status()
        
        data = response.json()
        return data.get('link', {}).get('webUrl')
    
    except Exception as e:
        logger.error(f"Failed to create sharing link for item {item_id}: {e}")
        return None

def create_sharing_link(file_path, link_type="view", scope="anonymous"):
    """
    Create a sharing link for a OneDrive file or folder.
    
    This function handles the entire process of creating a sharing link:
    1. Authenticate with the Microsoft Graph API
    2. Get the item ID for the given file path
    3. Create and return the sharing link
    
    Args:
        file_path (str or Path): Path to the file in OneDrive 
                                (can be local file system path or relative to OneDrive root)
        link_type (str): Type of link - "view", "edit", or "embed"
        scope (str): Scope of access - "anonymous" or "organization"
        
    Returns:
        str: The sharing link URL or None if creation failed
    """
    try:
        # If file_path is already an actual URL, return it directly
        if isinstance(file_path, str) and file_path.startswith(('http://', 'https://')):
            logger.info(f"File path is already a URL: {file_path}")
            return file_path
        
        # Handle local file system paths
        cloud_path = convert_local_path_to_cloud_path(file_path)
        if cloud_path:
            logger.debug(f"Converted local path to cloud path: {file_path} -> {cloud_path}")
        else:
            logger.warning(f"Using original path, could not determine cloud path: {file_path}")
            cloud_path = file_path
        
        # First authenticate with Graph API
        logger.debug(f"Authenticating with Graph API")
        access_token = authenticate_graph_api()
        if not access_token:
            logger.error("Failed to authenticate with Graph API")
            return None
        
        # Get the item ID
        logger.debug(f"Getting item ID for cloud path: {cloud_path}")
        item_id = get_item_id_by_path(access_token, cloud_path)
        if not item_id:
            logger.error(f"Could not find OneDrive item at path: {cloud_path}")
            logger.error(f"Original local path: {file_path}")
            return None
        
        # Create the sharing link
        logger.debug(f"Creating {link_type} sharing link for item {item_id}")
        link = create_sharing_link_by_id(access_token, item_id, link_type, scope)
        
        if link:
            logger.info(f"Created sharing link for {file_path}: {link}")
            return link
        else:
            logger.error(f"Failed to create sharing link for {file_path}")
            return None
    
    except Exception as e:
        logger.error(f"Uncaught exception in create_sharing_link: {e}")
        return None

def batch_create_sharing_links(file_paths, link_type="view", scope="anonymous"):
    """
    Create sharing links for multiple OneDrive files or folders.
    
    Args:
        file_paths (list): List of file paths in OneDrive
        link_type (str): Type of link - "view", "edit", or "embed"
        scope (str): Scope of access - "anonymous" or "organization"
        
    Returns:
        dict: Dictionary mapping file paths to their sharing links
    """
    results = {}
    
    # First authenticate with Graph API
    access_token = authenticate_graph_api()
    if not access_token:
        logger.error("Failed to authenticate with Graph API")
        return results
    
    total = len(file_paths)
    logger.info(f"Creating sharing links for {total} files")
    
    for i, file_path in enumerate(file_paths, 1):
        try:
            # Get the item ID
            item_id = get_item_id_by_path(access_token, file_path)
            if not item_id:
                logger.error(f"Could not find OneDrive item at path: {file_path}")
                continue
            
            # Create the sharing link
            link = create_sharing_link_by_id(access_token, item_id, link_type, scope)
            
            if link:
                results[file_path] = link
                logger.info(f"[{i}/{total}] Created sharing link for {file_path}")
            else:
                logger.error(f"[{i}/{total}] Failed to create sharing link for {file_path}")
            
            # Brief pause to avoid rate limiting
            time.sleep(0.5)
            
        except Exception as e:
            logger.error(f"Error processing {file_path}: {e}")
    
    logger.info(f"Created {len(results)} sharing links out of {total} files")
    return results
