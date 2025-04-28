#!/usr/bin/env python3
"""
OneDrive File Operations Module for Microsoft Graph API Integration

This module provides a comprehensive set of functions for interacting with OneDrive
through Microsoft Graph API, supporting the Notebook Generator system's file operations.
It handles sophisticated file access patterns with robust error handling and 
multi-strategy fallbacks for maximum reliability.

Key Features:
------------
1. Intelligent File Lookup
   - Multi-strategy file discovery with fallbacks
   - Path normalization and cleaning
   - Filename-based search when path-based lookup fails
   - Recursive folder traversal

2. Sharing Link Management
   - Creation of anonymous viewing links
   - Support for both path-based and ID-based sharing
   - Retry logic with exponential backoff
   - Rate limiting compliance

3. Robust Error Handling
   - Comprehensive categorization of API errors
   - Detailed logging with error context
   - Automatic retries with exponential backoff
   - Graceful degradation for different failure types

Integration Points:
-----------------
- Built on Microsoft Graph API for OneDrive access
- Works with the auth module for authentication
- Provides file access services to PDF and note generators
- Supports the overall notebook automation workflow
"""

import os
import re
import time
import urllib.parse
import requests
from pathlib import Path

from ..utils.config import GRAPH_API_ENDPOINT, ONEDRIVE_BASE
from ..utils.http_utils import create_requests_session
from ..utils.error_handling import categorize_error
from ..utils.config import logger

# Import list_folder_contents if available, otherwise define a placeholder
try:
    from ...list_folder_contents import list_folder_contents
except ImportError:
    # Define placeholder function to avoid runtime errors
    def list_folder_contents(parent_path, headers, filename):
        """Placeholder for list_folder_contents when not available"""
        logger.debug(f"list_folder_contents not available for debugging: {parent_path}, {filename}")

def check_if_file_exists(file_path, access_token, session=None):
    """
    Check if a file exists in OneDrive and return its metadata.
    
    This function validates file existence in OneDrive by making a direct API call
    to the Microsoft Graph API. It handles path normalization, ensures proper
    formatting of the OneDrive path, and provides detailed error handling.
    
    The function performs several preprocessing steps:
    1. Path normalization (converting backslashes, removing extra spaces)
    2. Ensuring the path has the correct OneDrive base path
    3. Removing duplicate slashes and other path cleanup
    
    Args:
        file_path (str): Path to the file in OneDrive (can be relative or absolute)
        access_token (str): Valid access token for Microsoft Graph API authorization
        session (requests.Session, optional): Optional requests session object for 
                                             connection pooling and configured retries.
                                             Creates a new session if None provided.
        
    Returns:
        dict: Complete file metadata from OneDrive API if found, including:
              - id: The unique OneDrive identifier for the file
              - name: The filename
              - size: File size
              - webUrl: Direct access URL
              - Other OneDrive metadata
              Returns None if file not found or error occurs.
              
    Error Handling:
        - Returns None for 404 (file not found)
        - Logs warnings/errors for other HTTP status codes
        - Categorizes errors for easier troubleshooting
    """
    # Create headers with the access token
    headers = {"Authorization": f"Bearer {access_token}"}
    
    # Use the provided session or create a new requests object
    requester = session if session else requests
    
    # Normalize path by replacing backslashes with forward slashes
    file_path = file_path.replace("\\", "/")
    
    # Clean up the path - remove trailing/leading spaces and slashes
    file_path = file_path.strip(" /\\")
    
    # Make sure the path has the correct base
    if not file_path.lower().startswith(ONEDRIVE_BASE.lower()):
        file_path = f"{ONEDRIVE_BASE}/{file_path}"
    
    # Make sure there are no duplicate slashes in the path
    file_path = re.sub(r'/{2,}', '/', file_path)
    
    api_endpoint = f"{GRAPH_API_ENDPOINT}/me/drive/root:/{file_path}"
    
    logger.debug(f"Checking if file exists: {file_path}")
    logger.debug(f"API endpoint: {api_endpoint}")
    
    try:
        response = requester.get(api_endpoint, headers=headers, timeout=15)
        if response.status_code == 200:
            logger.debug(f"File exists: {file_path}")
            return response.json()
        elif response.status_code == 404:
            logger.debug(f"File not found: {file_path}")
            return None
        else:
            error_category = categorize_error(f"HTTP {response.status_code}", response.status_code)
            logger.warning(f"Error checking if file exists: HTTP {response.status_code} ({error_category})")
            return None
    except Exception as e:
        error_category = categorize_error(e)
        logger.error(f"Exception checking if file exists: {e} ({error_category})")
        return None

def find_file_in_onedrive(file_path, access_token):
    """
    Find a file in OneDrive using a multi-strategy intelligent lookup system.
    
    This function implements a sophisticated file discovery process with multiple
    fallback strategies to maximize the chances of finding a file even with
    imperfect path information. It's designed to be resilient against common
    issues like path formatting differences and base path variations.
    
    Search Strategies (in order):
    1. Direct path lookup - Fastest when path is accurate
    2. Normalized path lookup - Handles slash/backslash differences and spacing
    3. Base path manipulation - Tries adding/removing the OneDrive base path
    4. Filename-based search - Last resort using Microsoft Graph search API
    
    Args:
        file_path (str): Path to the file in OneDrive (relative or absolute)
                        Can include or exclude the OneDrive base path
        access_token (str): Valid access token for Microsoft Graph API
        
    Returns:
        dict: Complete file metadata from OneDrive if found, including:
              - id: The OneDrive file identifier (needed for sharing)
              - name: The filename
              - size: File size information
              - webUrl: Direct access URL
              - parentReference: Information about the parent folder
              Returns None if the file cannot be found after all strategies
    
    Performance Note:
        The function creates a persistent requests session for connection
        pooling across multiple API calls, significantly improving performance
        when multiple search strategies are needed.
    """
    # Create a requests session for connection pooling and retry
    session = create_requests_session()
    headers = {"Authorization": f"Bearer {access_token}"}
    
    # STRATEGY 1: Direct lookup using the path
    logger.info(f"Looking for file at path: {file_path}")
    file_data = check_if_file_exists(file_path, access_token, session)
    
    if file_data:
        logger.info(f"Found file using direct path lookup")
        return file_data
    
    # STRATEGY 2: Clean and normalize the path and try again
    cleaned_path = file_path.strip('/\\').replace('\\', '/')
    if cleaned_path != file_path:
        logger.info(f"Trying with normalized path: {cleaned_path}")
        file_data = check_if_file_exists(cleaned_path, access_token, session)
        if file_data:
            logger.info(f"Found file using normalized path")
            return file_data
    
    # STRATEGY 3: Try with and without ONEDRIVE_BASE prefix
    if not cleaned_path.startswith(ONEDRIVE_BASE):
        prefixed_path = f"{ONEDRIVE_BASE}/{cleaned_path}"
        logger.info(f"Trying with base path added: {prefixed_path}")
        file_data = check_if_file_exists(prefixed_path, access_token, session)
        if file_data:
            logger.info(f"Found file by adding base path")
            return file_data
    elif cleaned_path.startswith(ONEDRIVE_BASE):
        # Try without the base prefix
        unprefixed_path = cleaned_path[len(ONEDRIVE_BASE):].lstrip('/')
        if unprefixed_path:  # Only if we have something left after removing prefix
            logger.info(f"Trying without base path: {unprefixed_path}")
            file_data = check_if_file_exists(unprefixed_path, access_token, session)
            if file_data:
                logger.info(f"Found file by removing base path")
                return file_data
    
    # STRATEGY 4: Search by filename
    file_name = os.path.basename(file_path)
    logger.info(f"Path lookups failed. Searching by filename: {file_name}")
    
    # Encode the filename for search query
    encoded_name = urllib.parse.quote(file_name)
    search_url = f"{GRAPH_API_ENDPOINT}/me/drive/root/search(q='{encoded_name}')"
    
    try:
        resp = session.get(search_url, headers=headers, timeout=15)
        
        if resp.status_code == 200:
            results = resp.json().get('value', [])
            logger.info(f"Search returned {len(results)} results for '{file_name}'")
            
            # Filter for exact name match
            for item in results:
                if item.get('name') == file_name:
                    logger.info(f"Found exact match for file: {file_name}")
                    return item
                    
            # If no exact match, look for close matches
            for item in results:
                if file_name.lower() in item.get('name', '').lower():
                    logger.info(f"Found partial match: {item.get('name')}")
                    return item
        else:
            # Log the error properly
            category = categorize_error(f"HTTP {resp.status_code}", resp.status_code)
            logger.error(f"Search failed with status {resp.status_code} ({category})")
    except Exception as e:
        category = categorize_error(e)
        logger.error(f"Error during search: {e} ({category})")
    
    logger.error(f"File not found: {file_path}")
    return None

def get_file_in_onedrive(relative_path, headers, session=None):
    """
    Gets a file from OneDrive with enterprise-grade reliability features.
    
    This function provides a robust implementation for retrieving file metadata
    from OneDrive with comprehensive error handling and recovery strategies:
    
    1. Path normalization and URL-safe encoding
    2. Multiple retry attempts with exponential backoff
    3. Rate limiting detection and compliance
    4. Intelligent error categorization for troubleshooting
    5. Fallback to filename search when path lookup fails
    
    This function is particularly suited for mission-critical file access
    where reliability is essential, even in environments with network
    instability or API rate limiting.
    
    Args:
        relative_path (str): Path to the file, relative to the OneDrive base.
                            Will be properly formatted and encoded.
        headers (dict): Headers with authentication token for API access
        session (requests.Session, optional): Requests session for connection pooling
                                             and configured retries
        
    Returns:
        dict: Complete file metadata if found, including:
              - id: The unique identifier for the file
              - name: The filename
              - size: File size information
              - webUrl: Direct access URL
              - parentReference: Information about parent folder
              Returns None if the file cannot be found or accessed
              
    Error Handling:
        - Retries with exponential backoff for transient failures
        - Special handling for rate limiting (HTTP 429)
        - Detailed error categorization and logging
        - Fallback strategies when direct file access fails
    """
    # Use the provided session or create a new requests object
    requester = session if session else requests
    
    # Clean the path and make it URL-safe
    clean_path = relative_path.strip(" /\\").replace("\\", "/")
    
    # Check if the path already has the base path
    if not clean_path.lower().startswith(ONEDRIVE_BASE.lower()):
        # Add the base path
        full_path = f"{ONEDRIVE_BASE}/{clean_path}"
    else:
        full_path = clean_path
    
    # Make sure there are no duplicate slashes in the path
    full_path = re.sub(r'/{2,}', '/', full_path)
    
    # URL encode the path
    encoded_path = urllib.parse.quote(full_path)
    
    # Build the URL with proper formatting
    url = f"https://graph.microsoft.com/v1.0/me/drive/root:{encoded_path}"
    
    logger.debug(f"Looking for file at: {full_path}")
    logger.debug(f"Using Graph API URL: {url}")
    
    # Add retry logic
    max_retries = 3
    retry_delay = 1  # seconds
    
    for attempt in range(max_retries):
        try:
            resp = requester.get(url, headers=headers, timeout=15)
            
            # Handle different status codes
            if resp.status_code == 200:
                logger.debug(f"File found successfully in OneDrive: {clean_path}")
                return resp.json()
            elif resp.status_code == 404:
                logger.warning(f"File not found in OneDrive: {clean_path}")
                logger.debug(f"Full URL that returned 404: {url}")
                
                # Try alternative approach - search by filename
                if '/' in clean_path:
                    parent_path = '/'.join(clean_path.split('/')[:-1])
                    filename = clean_path.split('/')[-1]
                    logger.info(f"Attempting to list parent folder contents to locate the file: {filename}")
                    
                    # If list_folder_contents exists, use it - otherwise we'll skip this
                    if 'list_folder_contents' in globals():
                        list_folder_contents(parent_path, headers, filename)
                    
                    # Try searching by filename as fallback
                    logger.info(f"Searching by filename: {filename}")
                    search_url = f"https://graph.microsoft.com/v1.0/me/drive/root/search(q='{urllib.parse.quote(filename)}')"
                    
                    try:
                        search_resp = requester.get(search_url, headers=headers, timeout=15)
                        if search_resp.status_code == 200:
                            results = search_resp.json().get('value', [])
                            if results:
                                logger.info(f"Found {len(results)} possible matches by filename")
                                # Look for exact filename match
                                for item in results:
                                    if item.get('name') == filename:
                                        logger.info(f"Found exact match for {filename}")
                                        return item
                    except Exception as e:
                        logger.warning(f"Error during filename search: {e}")
                
                return None
            elif resp.status_code == 401:
                logger.error(f"Authentication error (401) accessing OneDrive. Token may have expired.")
                return None
            elif resp.status_code == 429:
                # Rate limiting - get retry-after header
                retry_after = int(resp.headers.get('Retry-After', retry_delay))
                logger.warning(f"Rate limited (429). Waiting for {retry_after} seconds before retry.")
                time.sleep(retry_after)
                continue
            else:
                error_category = categorize_error(f"HTTP {resp.status_code}", resp.status_code)
                logger.error(f"OneDrive API error: HTTP {resp.status_code} ({error_category})")
                try:
                    error_details = resp.json()
                    error_msg = error_details.get('error', {}).get('message', 'Unknown error')
                    logger.error(f"Error details: {error_msg}")
                except Exception:
                    logger.error(f"Error response: {resp.text}")
                
                if attempt < max_retries - 1:
                    logger.info(f"Retrying in {retry_delay} seconds... (Attempt {attempt+1}/{max_retries})")
                    time.sleep(retry_delay)
                    retry_delay *= 2  # Exponential backoff
                    continue
                return None
                
        except requests.exceptions.Timeout:
            logger.warning(f"Timeout accessing OneDrive API. Retrying... (Attempt {attempt+1}/{max_retries})")
            if attempt < max_retries - 1:
                time.sleep(retry_delay)
                retry_delay *= 2
                continue
            else:
                logger.error("Max retries reached. Could not access OneDrive API due to timeout.")
                return None
        except Exception as e:
            error_category = categorize_error(e)
            logger.error(f"Unexpected error accessing OneDrive API: {str(e)} ({error_category})")
            if attempt < max_retries - 1:
                time.sleep(retry_delay)
                retry_delay *= 2
                continue
            return None
    
    return None

def create_share_link_by_id(file_id, headers, session=None):
    """
    Create a shareable link for a file in OneDrive using its unique ID.
    
    This function generates an anonymous, view-only sharing link for a OneDrive
    file identified by its ID (the most reliable identifier). This implementation
    includes enterprise-grade reliability features:
    
    1. Robust retry logic with exponential backoff
    2. Rate limiting detection and compliance
    3. Comprehensive error categorization and logging
    4. Full response validation and error extraction
    
    The sharing links generated are:
    - Anonymous (no authentication required to view)
    - View-only (recipients cannot edit)
    - Non-expiring (links remain valid indefinitely)
    
    Args:
        file_id (str): Unique ID of the file in OneDrive (preferred over path-based methods)
        headers (dict): Headers with authentication token for API access
        session (requests.Session, optional): Requests session for connection pooling
                                             and configured retries
        
    Returns:
        str: The sharing URL that can be distributed to provide access to the file.
             Returns None if sharing link creation fails after all retries.
             
    Error Handling:
        - Auto-retries on transient failures and timeouts
        - Special handling for rate limiting with dynamic backoff
        - Detailed error logging with categorization
        - Escalating wait periods between retry attempts
    """
    # Use the provided session or create a new requests object
    requester = session if session else requests
    
    # API endpoint for creating a sharing link
    url = f"{GRAPH_API_ENDPOINT}/me/drive/items/{file_id}/createLink"
    
    # Create a sharing link that provides view-only access and doesn't expire
    body = {
        "type": "view",
        "scope": "anonymous"
    }
    
    logger.info(f"Creating sharing link for file ID: {file_id}")
    
    # Add retry logic
    max_retries = 3
    retry_delay = 2  # seconds
    
    for attempt in range(max_retries):
        try:
            logger.debug(f"Creating share link for file ID: {file_id}, attempt {attempt+1}/{max_retries}")
            resp = requester.post(url, headers=headers, json=body, timeout=15)
            
            # Log the response for debugging
            logger.debug(f"Share link response status code: {resp.status_code}")
            
            if resp.status_code == 200:
                # Parse the response to get the sharing URL
                data = resp.json()
                sharing_link = data.get('link', {}).get('webUrl')
                if sharing_link:
                    logger.info(f"Successfully created share link")
                    logger.debug(f"Share link: {sharing_link}")
                    return sharing_link
                else:
                    logger.error("Sharing link not found in response")
                    if attempt < max_retries - 1:
                        logger.info(f"Retrying in {retry_delay} seconds...")
                        time.sleep(retry_delay)
                        retry_delay *= 2  # Exponential backoff
                        continue
                    else:
                        return None
            elif resp.status_code == 429:  # Rate limiting
                retry_after = int(resp.headers.get('Retry-After', retry_delay))
                logger.warning(f"Rate limited (429). Waiting for {retry_after} seconds before retry.")
                time.sleep(retry_after)
                continue  # Continue to next retry attempt
            else:
                # Log the error details
                try:
                    error_details = resp.json()
                    error_msg = error_details.get('error', {}).get('message', 'Unknown error')
                    error_category = categorize_error(error_msg, resp.status_code)
                    logger.error(f"Failed to create share link. Status: {resp.status_code}, Error: {error_msg}, Category: {error_category}")
                except Exception:
                    logger.error(f"Failed to create share link. Status: {resp.status_code}, Response: {resp.text}")
                
                if attempt < max_retries - 1:
                    logger.info(f"Retrying in {retry_delay} seconds... (Attempt {attempt+1}/{max_retries})")
                    time.sleep(retry_delay)
                    retry_delay *= 2  # Exponential backoff
                    continue
        except requests.exceptions.Timeout:
            logger.warning(f"Timeout creating share link. Retrying... (Attempt {attempt+1}/{max_retries})")
            if attempt < max_retries - 1:
                time.sleep(retry_delay)
                retry_delay *= 2  # Exponential backoff
                continue
            else:
                logger.error("Max retries reached. Could not create share link due to timeout.")
                return None
        except Exception as e:
            error_category = categorize_error(e)
            logger.error(f"Exception during share link creation: {str(e)}, Category: {error_category}")
            if attempt < max_retries - 1:
                logger.info(f"Retrying in {retry_delay} seconds...")
                time.sleep(retry_delay)
                retry_delay *= 2  # Exponential backoff
                continue
    
    # All retries failed
    return None

def create_share_link_by_path(file_path, headers, session=None):
    """
    Create a shareable link for a file in OneDrive using its path.
    
    This function provides path-based sharing link generation with a two-step process:
    1. First validate that the file exists and get its metadata
    2. Then use either ID-based or path-based sharing depending on what's available
    
    The function includes additional validations:
    - Confirms the path points to a file (not a folder)
    - Uses check_if_file_exists to normalize and validate the path
    - Falls back to ID-based sharing when possible (more reliable)
    
    The sharing links generated are:
    - Anonymous (no authentication required to view)
    - View-only (recipients cannot edit)
    - Non-expiring (links remain valid indefinitely)
    
    Args:
        file_path (str): Path to the file in OneDrive
        headers (dict): Headers with authentication token for API access
        session (requests.Session, optional): Requests session for connection pooling
                                             and retry configuration
        
    Returns:
        str: The sharing URL that can be distributed to provide access to the file.
             Returns None if the file doesn't exist or sharing creation fails.
             
    Implementation Note:
        This function prefers to use ID-based sharing when possible because it's
        more reliable. Path-based sharing is only used as a fallback when an ID
        cannot be obtained.
    """
    # Use the provided session or create a new requests object
    requester = session if session else requests
    
    # Get access token from headers
    access_token = headers['Authorization'].split(' ')[1]
    
    # First check if the file exists and get its metadata
    file_data = check_if_file_exists(file_path, access_token, requester)
    if not file_data:
        logger.error(f"File not found: {file_path}")
        return None
    
    if file_data.get("folder"):
        logger.error(f"The path '{file_path}' is a folder, not a file.")
        return None
    
    # If we have an ID, use the more efficient ID-based method
    if file_data.get('id'):
        return create_share_link_by_id(file_data['id'], headers, requester)
    
    # URL encode the file path for special characters
    api_endpoint = f"{GRAPH_API_ENDPOINT}/me/drive/root:/{file_path}:/createLink"
    
    # Create a sharing link that provides view-only access and doesn't expire
    body = {
        "type": "view",
        "scope": "anonymous"
    }
    
    logger.info(f"Creating sharing link for file: {file_path}")
    
    # Add retry logic
    max_retries = 3
    retry_delay = 2  # seconds
    
    for attempt in range(max_retries):
        try:
            logger.debug(f"Creating share link for file path: {file_path}, attempt {attempt+1}/{max_retries}")
            response = requester.post(api_endpoint, headers=headers, json=body, timeout=15)
            
            if response.status_code == 200:
                data = response.json()
                sharing_link = data.get("link", {}).get("webUrl")
                
                if sharing_link:
                    logger.info(f"Sharing link created successfully")
                    logger.debug(f"Share link: {sharing_link}")
                    return sharing_link
                else:
                    logger.error("Sharing link not found in response")
                    if attempt < max_retries - 1:
                        time.sleep(retry_delay)
                        retry_delay *= 2
                        continue
                    return None
            elif response.status_code == 429:  # Rate limiting
                retry_after = int(response.headers.get('Retry-After', retry_delay))
                logger.warning(f"Rate limited (429). Waiting for {retry_after} seconds before retry.")
                time.sleep(retry_after)
                continue
            else:
                try:
                    error_details = response.json()
                    error_msg = error_details.get('error', {}).get('message', 'Unknown error')
                    error_category = categorize_error(error_msg, response.status_code)
                    logger.error(f"Failed to create share link. Status: {response.status_code}, Error: {error_msg}, Category: {error_category}")
                except Exception:
                    logger.error(f"Failed to create share link. Status: {response.status_code}")
                
                if attempt < max_retries - 1:
                    logger.info(f"Retrying in {retry_delay} seconds... (Attempt {attempt+1}/{max_retries})")
                    time.sleep(retry_delay)
                    retry_delay *= 2
                    continue
        except requests.exceptions.Timeout:
            logger.warning(f"Timeout creating share link. Retrying... (Attempt {attempt+1}/{max_retries})")
            if attempt < max_retries - 1:
                time.sleep(retry_delay)
                retry_delay *= 2
                continue
            else:
                logger.error("Max retries reached. Could not create share link due to timeout.")
                return None
        except Exception as e:
            error_category = categorize_error(e)
            logger.error(f"Exception during share link creation: {str(e)}, Category: {error_category}")
            if attempt < max_retries - 1:
                time.sleep(retry_delay)
                retry_delay *= 2
                continue
    
    # All retries failed
    return None

def create_share_link(file_id_or_path, headers, session=None):
    """
    Create a shareable link for a file in OneDrive using either ID or path.
    
    This is a smart dispatcher function that determines whether the provided
    parameter is a file ID or path and routes to the appropriate specialized
    function. It serves as the primary entry point for link generation in the
    Notebook Generator system.
    
    Identification Logic:
    - If the parameter has no slashes, it's treated as a file ID
    - If the parameter contains slashes, it's treated as a file path
    
    Benefits:
    - Simplifies the API for callers who don't need to know if they have an ID or path
    - Automatically uses the most reliable method available based on input format
    - Maintains consistent retry logic and error handling regardless of method used
    
    Args:
        file_id_or_path (str): Either a file ID (no slashes) or path (with slashes)
        headers (dict): Headers with authentication token for API access
        session (requests.Session, optional): Requests session for connection pooling
                                             and retry configuration
        
    Returns:
        str: The sharing URL that can be distributed to provide access to the file.
             Returns None if the file doesn't exist or sharing creation fails.
    
    Usage Examples:
        # With file ID:
        link = create_share_link("01ABC123DEFG456HIJKL", headers)
        
        # With file path:
        link = create_share_link("Education/MBA Resources/Marketing/slides.pdf", headers)
    """
    # Determine if this is a file ID (no slashes) or path (contains slashes)
    if isinstance(file_id_or_path, str) and '/' not in file_id_or_path and '\\' not in file_id_or_path:
        # This is a file ID - use the ID-based approach
        return create_share_link_by_id(file_id_or_path, headers, session)
    else:
        # This is a file path - use the path-based approach
        return create_share_link_by_path(file_id_or_path, headers, session)

def get_onedrive_items(access_token, folder_path=ONEDRIVE_BASE):
    """
    Get all items (files and folders) from a folder in OneDrive with recursive support.
    
    This function provides a comprehensive directory listing capability with
    advanced features:
    
    1. Recursive Directory Traversal
       - Automatically explores all subfolders
       - Builds a complete file tree from the specified path
       
    2. Pagination Handling
       - Processes multiple result pages from the API
       - Combines results into a single comprehensive list
       
    3. Performance Optimization
       - Uses connection pooling for multiple requests
       - Implements safety limits for very large directories
       
    This function is particularly useful for:
    - Creating file indexes of educational content
    - Finding all resources within a course structure
    - Building comprehensive file catalogs for metadata processing
    
    Args:
        access_token (str): Valid access token for Microsoft Graph API
        folder_path (str, optional): Path to the folder to list. Defaults to
                                    the base OneDrive path in config.
        
    Returns:
        list: Comprehensive list of all items (files and folders) in the 
              specified folder and all of its subfolders. Each item includes:
              - name: The file or folder name
              - id: The unique ID for the item
              - folder/file: Type indicator
              - size: File size (for files)
              - webUrl: Direct access URL
              - other OneDrive metadata
              
    Performance Note:
        For very large directory structures, this function may take significant
        time to run due to recursive traversal and pagination handling. It 
        includes a safety limit of 50 pages per folder to prevent excessive
        API usage.
    """
    headers = {"Authorization": f"Bearer {access_token}"}
    session = create_requests_session()
    
    # Clean up path format
    folder_path = folder_path.strip(" /\\").replace("\\", "/")
    if folder_path:
        api_url = f"{GRAPH_API_ENDPOINT}/me/drive/root:/{folder_path}:/children"
    else:
        api_url = f"{GRAPH_API_ENDPOINT}/me/drive/root/children"
    
    logger.info(f"Getting OneDrive items from: {folder_path}")
    
    all_items = []
    next_link = api_url
    page = 1
    max_pages = 50  # Safety limit
    
    # Process paginated results
    while next_link and page <= max_pages:
        try:
            logger.debug(f"Fetching page {page} of items from: {next_link}")
            response = session.get(next_link, headers=headers, timeout=20)
            
            if response.status_code == 200:
                data = response.json()
                items = data.get('value', [])
                all_items.extend(items)
                logger.info(f"Retrieved {len(items)} items (page {page})")
                
                # Check for more pages
                next_link = data.get('@odata.nextLink')
                page += 1
            else:
                error_category = categorize_error(f"HTTP {response.status_code}", response.status_code)
                logger.error(f"Failed to get OneDrive items. Status: {response.status_code}, Category: {error_category}")
                try:
                    error_details = response.json()
                    error_msg = error_details.get('error', {}).get('message', 'Unknown error')
                    logger.error(f"Error details: {error_msg}")
                except Exception:
                    logger.error(f"Error response: {response.text}")
                break
        except Exception as e:
            error_category = categorize_error(e)
            logger.error(f"Error getting OneDrive items: {e}, Category: {error_category}")
            break
    
    # Process folders recursively
    expanded_items = list(all_items)  # Make a copy
    
    # Look for folders
    folders = [item for item in all_items if item.get('folder')]
    
    for folder in folders:
        folder_name = folder.get('name', '')
        folder_path_new = f"{folder_path}/{folder_name}" if folder_path else folder_name
        
        logger.info(f"Processing subfolder: {folder_path_new}")
        
        # Recursively get items from this folder
        folder_items = get_onedrive_items(access_token, folder_path_new)
        
        # Add these items to our expanded list
        expanded_items.extend(folder_items)
    
    return expanded_items
