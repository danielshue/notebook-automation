#!/usr/bin/env python3
"""
OneDrive Folder Listing Tool

This script helps diagnose issues with OneDrive path handling by listing 
the contents of a specified folder in OneDrive. It's useful for troubleshooting
when files can't be found despite existing in OneDrive.

Usage:
    python list_folder_contents.py [folder_path] [--search filename]

Example:
    python list_folder_contents.py "Education/MBA Resources/Value Chain Management" --search lecture1.mp4
    
This will authenticate with Microsoft Graph API and list all files in the specified folder.
"""

import os
import sys
import json
import urllib.parse
import time
import requests
import msal
import logging
import argparse

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
)
logger = logging.getLogger(__name__)

# Microsoft Graph API Configuration
CLIENT_ID = "489ad055-e4b0-4898-af27-53506ce83db7"
AUTHORITY = "https://login.microsoftonline.com/common"
SCOPES = ["Files.ReadWrite.All", "Sites.Read.All"]
TOKEN_CACHE_FILE = "token_cache.bin"

def authenticate_graph_api(force_refresh=False):
    """
    Authenticate with Microsoft Graph API using cached token or interactive flow.
    """
    # Set up the token cache
    cache = msal.SerializableTokenCache()
    if os.path.exists(TOKEN_CACHE_FILE) and not force_refresh:
        try:
            with open(TOKEN_CACHE_FILE, "r") as f:
                cache.deserialize(f.read())
                logger.info("Token cache loaded")
        except Exception as e:
            logger.warning(f"Error loading token cache: {e}")
    
    app = msal.PublicClientApplication(
        CLIENT_ID,
        authority=AUTHORITY,
        token_cache=cache
    )

    # Try to get token from cache
    accounts = app.get_accounts()
    if accounts and not force_refresh:
        result = app.acquire_token_silent(SCOPES, account=accounts[0])
        if result and "access_token" in result:
            logger.info("Token acquired from cache")
            return result["access_token"]

    # Interactive authentication if silent acquisition fails
    try:
        print("No valid token found in cache. Starting interactive authentication...")
        result = app.acquire_token_interactive(scopes=SCOPES)
        
        if "access_token" in result:
            # Save token cache
            with open(TOKEN_CACHE_FILE, "w") as f:
                f.write(cache.serialize())
            logger.info("New token acquired and saved to cache")
            return result["access_token"]
        else:
            # Fall back to device code flow if interactive fails
            print("Interactive authentication failed. Using device code flow instead...")
            flow = app.initiate_device_flow(scopes=SCOPES)
            
            if "user_code" not in flow:
                raise Exception("Failed to create device flow")
                
            print(f"To sign in, use a web browser to open the page {flow['verification_uri']} and enter the code {flow['user_code']} to authenticate.")
            
            result = app.acquire_token_by_device_flow(flow)
            if "access_token" in result:
                # Save token cache
                with open(TOKEN_CACHE_FILE, "w") as f:
                    f.write(cache.serialize())
                logger.info("New token acquired via device flow and saved to cache")
                return result["access_token"]
    except Exception as e:
        logger.error(f"Authentication error: {e}")
        sys.exit(1)
    
    logger.error("Failed to acquire token after all authentication methods")
    sys.exit(1)

def list_drives(headers):
    """
    List all available OneDrive drives for the authenticated user.
    This helps verify which OneDrive account we're accessing.
    """
    url = "https://graph.microsoft.com/v1.0/me/drives"
    
    try:
        print("Checking available OneDrive drives...")
        resp = requests.get(url, headers=headers, timeout=15)
        
        if resp.status_code == 200:
            drives = resp.json().get('value', [])
            print(f"Found {len(drives)} available drives:\n")
            
            for i, drive in enumerate(drives):
                drive_id = drive.get('id', 'Unknown')
                drive_name = drive.get('name', 'Unknown')
                drive_type = drive.get('driveType', 'Unknown')
                owner = drive.get('owner', {}).get('user', {}).get('displayName', 'Unknown')
                
                print(f"{i+1}. Drive: {drive_name}")
                print(f"   ID: {drive_id}")
                print(f"   Type: {drive_type}")
                print(f"   Owner: {owner}\n")
            
            return drives
        else:
            print(f"Error listing drives: HTTP {resp.status_code}")
            print(f"Response: {resp.text}")
            return []
    except Exception as e:
        print(f"Exception listing drives: {str(e)}")
        return []

def get_root_items(headers):
    """
    List items at the root of the OneDrive to help diagnose path issues.
    """
    url = "https://graph.microsoft.com/v1.0/me/drive/root/children"
    
    try:
        print("Checking root OneDrive folder contents...")
        resp = requests.get(url, headers=headers, timeout=15)
        
        if resp.status_code == 200:
            items = resp.json().get('value', [])
            print(f"Found {len(items)} items in OneDrive root:\n")
            
            for i, item in enumerate(items):
                item_name = item.get('name', 'Unknown')
                item_type = 'Folder' if 'folder' in item else 'File'
                
                print(f"{i+1}. {item_type}: {item_name}")
            
            print("\n")
            return items
        else:
            print(f"Error listing root items: HTTP {resp.status_code}")
            print(f"Response: {resp.text}")
            return []
    except Exception as e:
        print(f"Exception listing root items: {str(e)}")
        return []

def try_alternative_path_formats(path, headers):
    """Try different path formats to locate a folder in OneDrive."""
    path_formats = [
        path,
        path.lstrip('/'),
        path.rstrip('/'),
        path.lstrip('/').rstrip('/'),
        urllib.parse.quote(path),
        urllib.parse.quote(path.lstrip('/'))
    ]
    
    print(f"Trying alternative path formats for: {path}")
    
    for i, format_path in enumerate(path_formats):
        url = f"https://graph.microsoft.com/v1.0/me/drive/root:/{format_path}"
        try:
            print(f"Attempt {i+1}: {url}")
            resp = requests.get(url, headers=headers, timeout=10)
            if resp.status_code == 200:
                print(f"✅ Success with format: {format_path}")
                return resp.json()
            else:
                print(f"❌ Failed: HTTP {resp.status_code}")
        except Exception as e:
            print(f"❌ Error: {str(e)}")
    
    return None

def list_folder_contents(relative_path, headers, search_filename=None, onedrive_base='/Education/MBA Resources'):
    """
    List contents of a folder in OneDrive to help with troubleshooting.
    
    Args:
        relative_path: Path to the folder relative to ONEDRIVE_BASE
        headers: Headers including authentication token
        search_filename: Optional filename to look for in the results
        onedrive_base: Base OneDrive path
        
    Returns:
        List of items in the folder
    """
    import urllib.parse
    clean_path = relative_path.strip('/\\').replace('\\', '/')
    encoded_path = urllib.parse.quote(clean_path)
    
    # For consumer OneDrive, we'll try multiple path approaches
    # Approach 1: Using root:{path}
    if clean_path:
        full_path = f"{onedrive_base}/{clean_path}".rstrip('/')
        url = f"https://graph.microsoft.com/v1.0/me/drive/root:{full_path}:/children"
    else:
        # If trying to list the base folder itself
        url = f"https://graph.microsoft.com/v1.0/me/drive/root:{onedrive_base}:/children"
    print(f"Listing contents of folder: {onedrive_base}/{clean_path}")
    print(f"API URL (approach 1): {url}\n")
    
    try:
        # Try Approach 1: Using root:{path} format
        resp = requests.get(url, headers=headers, timeout=15)
        
        # If first approach failed, try Approach 2: Using /drive/items/{parent-id}/children
        if resp.status_code != 200:
            print(f"Approach 1 failed with status {resp.status_code}")
            print("Trying Approach 2: Using folder path lookup...")
            
            # First, try to get the folder ID
            parent_url = f"https://graph.microsoft.com/v1.0/me/drive/root:{onedrive_base}/{clean_path}"
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
                        alt_path = f"{onedrive_base}/{clean_path}"
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
                
                # Format the output
                print(f"{i+1:3}. {item_type}: {item_name}")
                print(f"    ID: {item_id}")
                print(f"    Last modified: {last_modified}")
                
                # If it's a file, show more details
                if 'file' in item:
                    mime_type = item.get('file', {}).get('mimeType', 'Unknown')
                    size = item.get('size', 0)
                    size_mb = size / (1024 * 1024)
                    print(f"    Type: {mime_type}")
                    print(f"    Size: {size_mb:.2f} MB")
                
                # If we're searching for a specific file
                if search_filename and item_name.lower() == search_filename.lower():
                    print(f"\n✅ FOUND EXACT MATCH: '{item_name}' - matches search for '{search_filename}'")
                    print(f"    File ID: {item_id}")
                    print(f"    File webUrl: {item.get('webUrl', 'Not available')}")
                    print(f"    API path: https://graph.microsoft.com/v1.0/me/drive/items/{item_id}")
                    print("\nFor direct access, use this file ID in your scripts instead of path lookup.")
                
                print("")
            
            # Additional search for similar filenames if not found
            exact_match_found = False
            if search_filename:
                exact_match_found = any(item.get('name', '').lower() == search_filename.lower() for item in items)
                
                if not exact_match_found:
                    print(f"\nSearching for similar filenames to '{search_filename}':")
                    close_matches = [item for item in items if search_filename.lower() in item.get('name', '').lower()]
                    
                    if close_matches:
                        print(f"Found {len(close_matches)} similar filenames:")
                        for match in close_matches:
                            print(f"  - {match.get('name')} (ID: {match.get('id')})")
                    else:
                        print("No similar filenames found.")
            
            return items
        else:
            print(f"Error listing folder contents (HTTP {resp.status_code})")
            print(f"Response: {resp.text}")
            return []
            
    except Exception as e:
        print(f"Exception listing folder contents: {str(e)}")
        return []

def search_for_file(filename, headers):
    """
    Search for a file by name across OneDrive.
    This is more reliable than path-based access.
    """
    print(f"\n🔎 Searching for file '{filename}' across all of OneDrive...")
    search_url = f"https://graph.microsoft.com/v1.0/me/drive/root/search(q='{filename}')"
    
    try:
        resp = requests.get(search_url, headers=headers, timeout=15)
        
        if resp.status_code == 200:
            items = resp.json().get('value', [])
            print(f"Found {len(items)} matches for '{filename}'")
            
            if items:
                # Show all items found
                for i, item in enumerate(items):
                    item_name = item.get('name', 'Unknown')
                    item_id = item.get('id', 'Unknown')
                    parent_path = item.get('parentReference', {}).get('path', 'Unknown')
                    
                    print(f"\n{i+1}. {item_name}")
                    print(f"   ID: {item_id}")
                    print(f"   Path: {parent_path}")
                    print(f"   webUrl: {item.get('webUrl', 'Not available')}")
                    
                print("\nTo access these files in your script, use direct file ID access instead of path-based access:")
                print("Example: https://graph.microsoft.com/v1.0/me/drive/items/{file_id}")
                return items
            else:
                print("No matching files found.")
                return []
        else:
            print(f"Error searching for file: HTTP {resp.status_code}")
            print(f"Response: {resp.text}")
            return []
    except Exception as e:
        print(f"Exception during search: {str(e)}")
        return []

def direct_file_access(file_id, headers):
    """
    Directly access a file by its ID.
    This is the most reliable method for accessing files in OneDrive.
    """
    print(f"Attempting direct access to file with ID: {file_id}")
    url = f"https://graph.microsoft.com/v1.0/me/drive/items/{file_id}"
    
    try:
        resp = requests.get(url, headers=headers, timeout=15)
        
        if resp.status_code == 200:
            item = resp.json()
            print(f"\n✅ Successfully accessed file:")
            print(f"   Name: {item.get('name', 'Unknown')}")
            print(f"   ID: {item.get('id', 'Unknown')}")
            print(f"   webUrl: {item.get('webUrl', 'Not available')}")
            print(f"   parentPath: {item.get('parentReference', {}).get('path', 'Unknown')}")
            
            if 'file' in item:
                mime_type = item.get('file', {}).get('mimeType', 'Unknown')
                size = item.get('size', 0)
                size_mb = size / (1024 * 1024)
                print(f"   Type: {mime_type}")
                print(f"   Size: {size_mb:.2f} MB")
            
            return item
        else:
            print(f"Error accessing file: HTTP {resp.status_code}")
            print(f"Response: {resp.text}")
            return None
    except Exception as e:
        print(f"Exception accessing file: {str(e)}")
        return None

def main():
    parser = argparse.ArgumentParser(description='List contents of a OneDrive folder')
    parser.add_argument('folder_path', nargs='?', default='', help='The path in OneDrive to list')
    parser.add_argument('--search', help='Search for a specific filename')
    parser.add_argument('--file-id', help='Directly access a file by its ID')
    parser.add_argument('--base', default='', 
                        help='The OneDrive base path (default: empty for root access)')
    parser.add_argument('--refresh', action='store_true', help='Force refresh the authentication token')
    parser.add_argument('--show-drives', action='store_true', help='Show available OneDrive drives')
    parser.add_argument('--show-root', action='store_true', help='Show contents of OneDrive root')
    
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
