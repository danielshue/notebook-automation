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


def list_drives(headers: Dict[str, str]) -> None:
    """List available OneDrive drives.
    
    Args:
        headers: Request headers with auth token
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
    
    Args:
        headers: Request headers with auth token
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
    
    Args:
        file_id: OneDrive file ID
        headers: Request headers with auth token
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
    
    Args:
        filename: Name of file to search for
        headers: Request headers with auth token
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
    
    Args:
        relative_path: Path relative to onedrive_base
        headers: Request headers with auth token
        search_filename: Optional filename to search for
        onedrive_base: Base path in OneDrive
        
    Returns:
        List of items in the folder
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
    
    args = parser.parse_args()
    
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
