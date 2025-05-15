#!/usr/bin/env python3
"""
OneDrive Folder Listing CLI Tool

This module provides a command-line interface for listing contents of OneDrive folders
and searching for files. It handles authentication and provides various listing options.

Examples:
    vault-list-folder "path/to/folder"           # List folder contents
    vault-list-folder --search "filename.pdf"    # Search for a file
    vault-list-folder --show-drives              # Show available drives
"""

import os
import sys
import json
import requests
import argparse
import msal
from pathlib import Path
from typing import Dict, List, Optional, Any

from ..tools.auth.microsoft_auth import authenticate_graph_api
from notebook_automation.cli.utils import OKCYAN, ENDC


def list_drives(headers: Dict[str, str]) -> None:
    """List available OneDrive drives.
    
    Queries the Microsoft Graph API to retrieve all available drives
    for the authenticated user and displays them with their types and IDs.
    
    Args:
        headers (Dict[str, str]): Request headers containing the authentication token
        
    Returns:
        None: This function prints results to stdout but doesn't return a value
        
    Example:
        >>> list_drives({"Authorization": "Bearer token123"})
        Found 2 drives:
        Drive: OneDrive
          Type: personal
          ID: drive123456
    """
    resp = requests.get("https://graph.microsoft.com/v1.0/me/drives", 
                       headers=headers, timeout=15)
    if resp.status_code == 200:
        drives = resp.json().get('value', [])
        print(f"\nFound {len(drives)} drives:")
        for drive in drives:
            print(f"Drive: {drive.get('name', 'Unknown')}")
            print(f"  Type: {drive.get('driveType', 'Unknown')}")
            print(f"  ID: {drive.get('id', 'Unknown')}")
    else:
        print(f"Error listing drives: HTTP {resp.status_code}")
        print(resp.text)


def get_root_items(headers: Dict[str, str]) -> None:
    """List items in the OneDrive root.
    
    Queries Microsoft Graph API to retrieve all files and folders in the
    root directory of the user's OneDrive and displays them with their
    type (folder/file) and creation date.
    
    Args:
        headers (Dict[str, str]): Request headers containing the authentication token
        
    Returns:
        None: This function prints results to stdout but doesn't return a value
        
    Example:
        >>> get_root_items({"Authorization": "Bearer token123"})
        Files and folders in OneDrive root:
        📁 Documents (folder)
        📄 Resume.pdf (file, created: 2023-05-10)
    """
    resp = requests.get("https://graph.microsoft.com/v1.0/me/drive/root/children", 
                       headers=headers, timeout=15)
    if resp.status_code == 200:
        items = resp.json().get('value', [])
        print(f"\nFound {len(items)} items in root:")
        for item in items:
            name = item.get('name', 'Unknown')
            item_type = 'Folder' if 'folder' in item else 'File'
            print(f"{item_type}: {name}")
    else:
        print(f"Error listing root items: HTTP {resp.status_code}")
        print(resp.text)


def direct_file_access(file_id: str, headers: Dict[str, str]) -> None:
    """Access a file directly by its ID.
    
    Retrieves and displays detailed metadata for a specific OneDrive file
    using its unique identifier. This is useful when you already know the
    file ID and need to examine its properties.
    
    Args:
        file_id (str): OneDrive file ID to look up
        headers (Dict[str, str]): Request headers containing the authentication token
        
    Returns:
        None: This function prints results to stdout but doesn't return a value
        
    Example:
        >>> direct_file_access("ABC123fileID", {"Authorization": "Bearer token123"})
        File details:
          Name: Important Document.docx
          Size: 245KB
          Created: 2023-05-11
    """
    url = f"https://graph.microsoft.com/v1.0/me/drive/items/{file_id}"
    resp = requests.get(url, headers=headers, timeout=15)
    if resp.status_code == 200:
        item = resp.json()
        print(f"\nFile details for ID {file_id}:")
        print(f"Name: {item.get('name', 'Unknown')}")
        print(f"Web URL: {item.get('webUrl', 'Not available')}")
        print(f"Created: {item.get('createdDateTime', 'Unknown')}")
        print(f"Modified: {item.get('lastModifiedDateTime', 'Unknown')}")
        if 'file' in item:
            print(f"Size: {item.get('size', 0) / (1024*1024):.2f} MB")
            print(f"Type: {item.get('file', {}).get('mimeType', 'Unknown')}")
    else:
        print(f"Error accessing file: HTTP {resp.status_code}")
        print(resp.text)


def search_for_file(filename: str, headers: Dict[str, str]) -> None:
    """Search for a file across all of OneDrive.
    
    Performs a search across the entire OneDrive account for files matching 
    the specified filename or pattern. Displays detailed results including
    file paths, sizes, and IDs.
    
    Args:
        filename (str): Name or partial name of file to search for
        headers (Dict[str, str]): Request headers containing the authentication token
        
    Returns:
        None: This function prints results to stdout but doesn't return a value
        
    Example:
        >>> search_for_file("report", {"Authorization": "Bearer token123"})
        Found 3 matching items:
        
        Name: quarterly_report.pdf
        Path: /Documents/Finance
        ID: 12345ABC
        Size: 1.25 MB
    """
    search_url = "https://graph.microsoft.com/v1.0/me/drive/root/search(q=?)"
    resp = requests.get(search_url, 
                       params={'q': filename},
                       headers=headers, 
                       timeout=15)
    
    if resp.status_code == 200:
        items = resp.json().get('value', [])
        print(f"\nFound {len(items)} matching items:")
        for item in items:
            name = item.get('name', 'Unknown')
            parent_path = item.get('parentReference', {}).get('path', '').split('root:')[-1]
            print(f"\nName: {name}")
            print(f"Path: {parent_path}")
            print(f"ID: {item.get('id', 'Unknown')}")
            if 'file' in item:
                size_mb = item.get('size', 0) / (1024 * 1024)
                print(f"Size: {size_mb:.2f} MB")
    else:
        print(f"Error searching: HTTP {resp.status_code}")
        print(resp.text)


def try_alternative_path_formats(path: str, headers: Dict[str, str]) -> Optional[Dict[str, Any]]:
    """Try different path formats to locate a folder.
    
    Args:
        path: Folder path to try
        headers: Request headers with auth token
        
    Returns:
        Dict with folder details if found, None otherwise
    """
    # Try variations of the path
    variations = [
        f"/drive/root:{path}",
        f"/drive/root:/{path}",
        path.rstrip('/'),
        path.rstrip('/') + '/',
        path.replace(' ', '%20')
    ]
    
    for var in variations:
        url = f"https://graph.microsoft.com/v1.0/me{var}"
        try:
            resp = requests.get(url, headers=headers, timeout=15)
            if resp.status_code == 200:
                return resp.json()
        except Exception:
            continue
    
    return None


def list_folder_contents(relative_path: str, 
                        headers: Dict[str, str], 
                        search_filename: Optional[str] = None, 
                        onedrive_base: str = '') -> List[Dict[str, Any]]:
    """List contents of a OneDrive folder.
    
    Retrieves and displays the files and folders in a specified OneDrive directory.
    The function supports various path formats and handles error conditions
    gracefully, with multiple API approaches as fallbacks.
    
    Args:
        relative_path (str): Path relative to onedrive_base to list contents from
        headers (Dict[str, str]): Request headers containing the authentication token
        search_filename (Optional[str]): If provided, only show items matching this name pattern.
            Defaults to None (show all items).
        onedrive_base (str): Base path in OneDrive to prepend to the relative path.
            Defaults to empty string (use root as base).
        
    Returns:
        List[Dict[str, Any]]: List of item objects from the OneDrive API, each containing
            metadata about a file or folder such as name, id, size, and web URL.
            
    Example:
        >>> items = list_folder_contents("Documents/MBA", {"Authorization": "Bearer token123"})
        Listing contents of folder: /Documents/MBA
        Found 15 items:
        📁 Finance (folder)
        📄 Schedule.xlsx (2.1 MB)
    """
    # Clean up the path
    clean_path = relative_path.strip('/')
    if clean_path:
        clean_path = f"/{clean_path}"
    
    # Build the API URL
    if not clean_path:
        url = f"https://graph.microsoft.com/v1.0/me/drive/root:{onedrive_base}:/children"
    else:
        url = f"https://graph.microsoft.com/v1.0/me/drive/root:{onedrive_base}{clean_path}:/children"
        
    print(f"Listing contents of folder: {onedrive_base}{clean_path}")
    print(f"API URL (approach 1): {url}\n")
    
    try:
        # Try Approach 1: Using root:{path} format
        resp = requests.get(url, headers=headers, timeout=15)
        
        # If first approach failed, try Approach 2
        if resp.status_code != 200:
            print(f"Approach 1 failed with status {resp.status_code}")
            print("Trying Approach 2: Using folder path lookup...")
            
            # First, try to get the folder ID
            parent_url = f"https://graph.microsoft.com/v1.0/me/drive/root:{onedrive_base}{clean_path}"
            try:
                folder_resp = requests.get(parent_url, headers=headers, timeout=15)
                if folder_resp.status_code == 200:
                    folder_id = folder_resp.json().get('id')
                    print(f"Found folder with ID: {folder_id}")
                    
                    # Now get its children
                    children_url = f"https://graph.microsoft.com/v1.0/me/drive/items/{folder_id}/children"
                    print(f"API URL (approach 2): {children_url}")
                    resp = requests.get(children_url, headers=headers, timeout=15)
                else:
                    print(f"Failed to find folder: HTTP {folder_resp.status_code}")
                    
                    # Try alternative path formats as a last resort
                    print("Trying alternative path formats...")
                    if onedrive_base:
                        alt_path = f"{onedrive_base}{clean_path}"
                    else:
                        alt_path = clean_path
                    
                    result = try_alternative_path_formats(alt_path, headers)
                    if result and 'id' in result:
                        folder_id = result['id']
                        children_url = f"https://graph.microsoft.com/v1.0/me/drive/items/{folder_id}/children"
                        print(f"API URL (alternative): {children_url}")
                        resp = requests.get(children_url, headers=headers, timeout=15)
                    else:
                        print("All path format attempts failed.")
                        
                        # Last resort: List drives and root folders to help diagnose
                        list_drives(headers)
                        get_root_items(headers)
            except Exception as path_error:
                print(f"Error with path-based lookup: {path_error}")
        
        # Process the response, regardless of which approach succeeded
        if resp.status_code == 200:
            items = resp.json().get('value', [])
            print(f"\n✅ Success! Found {len(items)} items in folder\n")
            
            # List all items
            for i, item in enumerate(items):
                item_name = item.get('name', 'Unknown')
                item_type = 'Folder' if 'folder' in item else 'File'
                item_id = item.get('id', 'Unknown')
                last_modified = item.get('lastModifiedDateTime', 'Unknown')
                
                print(f"{i+1:3}. {item_type}: {item_name}")
                print(f"    ID: {item_id}")
                print(f"    Last modified: {last_modified}")
                
                if 'file' in item:
                    mime_type = item.get('file', {}).get('mimeType', 'Unknown')
                    size = item.get('size', 0)
                    size_mb = size / (1024 * 1024)
                    print(f"    Type: {mime_type}")
                    print(f"    Size: {size_mb:.2f} MB")
                
                if search_filename and item_name.lower() == search_filename.lower():
                    print(f"\n✅ FOUND EXACT MATCH: '{item_name}' - matches search for '{search_filename}'")
                    print(f"    File ID: {item_id}")
                    print(f"    File webUrl: {item.get('webUrl', 'Not available')}")
                    print(f"    API path: https://graph.microsoft.com/v1.0/me/drive/items/{item_id}")
                    print("\nFor direct access, use this file ID in your scripts instead of path lookup.")
                
                print("")
            
            # Additional search for similar filenames if not found
            if search_filename:
                exact_match_found = any(item.get('name', '').lower() == search_filename.lower() 
                                      for item in items)
                
                if not exact_match_found:
                    print(f"\nSearching for similar filenames to '{search_filename}':")
                    close_matches = [item for item in items 
                                   if search_filename.lower() in item.get('name', '').lower()]
                    
                    if close_matches:
                        print(f"Found {len(close_matches)} similar filenames:")
                        for match in close_matches:
                            print(f"  - {match.get('name')} (ID: {match.get('id')})")
                    else:
                        print("No similar filenames found.")
            
            return items
            
        print(f"Error listing folder contents (HTTP {resp.status_code})")
        print(f"Response: {resp.text}")
        return []
        
    except Exception as e:
        print(f"Exception listing folder contents: {str(e)}")
        return []


def main():
    """Main entry point for the CLI tool."""
    parser = argparse.ArgumentParser(
        description='List contents of a OneDrive folder',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
    vault-list-folder "path/to/folder"           # List folder contents
    vault-list-folder --search filename.pdf      # Search for specific file
    vault-list-folder --file-id {id}            # Access file by ID
    vault-list-folder --show-drives             # Show available drives
    vault-list-folder --show-root               # Show root contents
    vault-list-folder --base "alt/base" ...     # Use alternate base path
"""
    )
    
    parser.add_argument('folder_path', nargs='?', default='',
                       help='The path in OneDrive to list')
    parser.add_argument('--search',
                       help='Search for a specific filename')
    parser.add_argument('--file-id',
                       help='Directly access a file by its ID')
    parser.add_argument('--base', default='', 
                       help='The OneDrive base path (default: empty for root access)')
    parser.add_argument('--refresh', action='store_true',
                       help='Force refresh the authentication token')
    parser.add_argument('--show-drives', action='store_true',
                       help='Show available OneDrive drives')
    parser.add_argument('--show-root', action='store_true',
                       help='Show contents of OneDrive root')
    parser.add_argument('-c', '--config', type=str, default=None,
                       help='Path to config.json file (optional)')
    
    args = parser.parse_args()
    
    # Set config path if provided
    if args.config:
        # Use absolute path to ensure consistency
        config_path = str(Path(args.config).absolute())
        os.environ["NOTEBOOK_CONFIG_PATH"] = config_path
        
    # Display which config.json file is being used
    try:
        from notebook_automation.tools.utils.config import find_config_path
        config_path = os.environ.get("NOTEBOOK_CONFIG_PATH") or find_config_path()
        print(f"{OKCYAN}Using configuration file: {config_path}{ENDC}")
    except Exception as e:
        print(f"Could not determine config file path: {e}")
    
    # Authenticate
    print("Authenticating with Microsoft Graph API...")
    access_token = authenticate_graph_api(args.refresh)
    headers = {"Authorization": f"Bearer {access_token}"}
    
    # Show drives if requested
    if args.show_drives:
        list_drives(headers)
    
    # Show root contents if requested
    if args.show_root:
        get_root_items(headers)
    
    # Direct file access if ID provided
    if args.file_id:
        direct_file_access(args.file_id, headers)
        return
    
    # Search for file by name across all of OneDrive
    if args.search and not args.folder_path:
        search_for_file(args.search, headers)
        return
    
    # List folder contents
    list_folder_contents(args.folder_path, headers, args.search, args.base)


if __name__ == "__main__":
    main()
