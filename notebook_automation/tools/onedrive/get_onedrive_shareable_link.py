#!/usr/bin/env python3
"""
OneDrive Personal File Sharing Script

This script connects to OneDrive Personal using interactive authentication and
generates a shareable link for a specified file.

Usage:
    python get_onedrive_shareable_link.py --file <file_path_in_onedrive>
    python get_onedrive_shareable_link.py --list <folder_path_in_onedrive>

Example:
    python get_onedrive_shareable_link.py --file "Documents/resume.pdf"
    python get_onedrive_shareable_link.py --list "Documents"
"""

import os
import sys
import logging
import json
import argparse
import requests
import msal
import webbrowser
from datetime import datetime

# Setup logging
logs_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'logs')
if not os.path.exists(logs_dir):
    os.makedirs(logs_dir)

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler(os.path.join(logs_dir, "onedrive_sharing.log")),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

# Microsoft Graph API Configuration
CLIENT_ID = "489ad055-e4b0-4898-af27-53506ce83db7"  # Using the same client ID from the troubleshooter
AUTHORITY = "https://login.microsoftonline.com/common"
SCOPES = ["Files.ReadWrite.All", "Sites.Read.All"]
TOKEN_CACHE_FILE = "token_cache.bin"
GRAPH_API_ENDPOINT = "https://graph.microsoft.com/v1.0"
MBA_RESOURCES_PATH = "/c/Users/danielshue/OneDrive/Education/MBA-Resources"
ONEDRIVE_MBA_PATH = "Education/MBA-Resources"

def authenticate_interactive():
    """
    Authenticate with Microsoft Graph API using interactive authentication.
    Returns an access token if successful, None otherwise.
    """
    logger.info("Starting interactive authentication flow...")
    
    # Set up the token cache
    cache = msal.SerializableTokenCache()
    
    # Load the token cache if it exists
    if os.path.exists(TOKEN_CACHE_FILE):
        try:
            with open(TOKEN_CACHE_FILE, "r") as f:
                cache_data = f.read()
                if cache_data:
                    cache.deserialize(cache_data)
                    logger.info("Token cache loaded successfully")
        except Exception as e:
            logger.warning(f"Could not load token cache: {e}")
    
    # Create MSAL app
    app = msal.PublicClientApplication(
        CLIENT_ID,
        authority=AUTHORITY,
        token_cache=cache
    )
    
    # Check if we have accounts in the cache
    accounts = app.get_accounts()
    result = None
    
    if accounts:
        logger.info(f"Account found in token cache, attempting to use existing token...")
        print(f"Account found in cache: {accounts[0]['username']}")
        result = app.acquire_token_silent(SCOPES, account=accounts[0])
        
    # No suitable token in cache, initiate interactive authentication
    if not result or "access_token" not in result:
        logger.info("No valid token in cache, initiating interactive authentication...")
        print("\nInitiating interactive authentication...\nA browser window will open for you to sign in.")
        
        try:
            # This will automatically open the default web browser for authentication
            result = app.acquire_token_interactive(
                scopes=SCOPES,
                prompt="select_account"  # Force prompt to select account
            )
            
            if "access_token" in result:
                logger.info("Interactive authentication successful!")
                print("✓ Interactive authentication successful!")
                
                # Save token cache for future use
                with open(TOKEN_CACHE_FILE, "w") as f:
                    f.write(cache.serialize())
                logger.info(f"Token cache saved to {TOKEN_CACHE_FILE}")
            else:
                error = result.get("error")
                error_desc = result.get("error_description") 
                logger.error(f"Interactive authentication failed: {error} - {error_desc}")
                print(f"× Interactive authentication failed: {error} - {error_desc}")
                return None
        
        except Exception as e:
            logger.error(f"Exception during interactive authentication: {type(e).__name__}: {e}")
            print(f"× Exception during authentication: {str(e)}")
            return None
    
    return result.get("access_token") if result and "access_token" in result else None

def list_items_in_folder(access_token, folder_path=""):
    """
    List items in a OneDrive folder.
    
    Args:
        access_token: Valid access token for Microsoft Graph API
        folder_path: Path to the folder in OneDrive (default: root)
        
    Returns:
        List of items in the folder
    """
    # Normalize path by replacing backslashes with forward slashes
    folder_path = folder_path.replace("\\", "/")
    
    if folder_path and folder_path.startswith("/"):
        folder_path = folder_path[1:]  # Remove leading slash
        
    api_endpoint = f"{GRAPH_API_ENDPOINT}/me/drive/root"
    if folder_path:
        # Handle path with colon notation
        api_endpoint = f"{api_endpoint}:/{folder_path}:"
        
    api_endpoint = f"{api_endpoint}/children"
    
    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }
    logger.info(f"Listing items in folder: {folder_path if folder_path else 'root'}")
    try:
        response = requests.get(api_endpoint, headers=headers)
        
        # Check if we get an error because the path is a file, not a folder
        if response.status_code == 404:
            # Try to get item metadata instead to see if it's a file
            file_metadata_endpoint = f"{GRAPH_API_ENDPOINT}/me/drive/root:/{folder_path}"
            file_response = requests.get(file_metadata_endpoint, headers=headers)
            
            if file_response.status_code == 200:
                file_data = file_response.json()
                if not file_data.get("folder"):  # It's a file, not a folder
                    print(f"\nNote: '{folder_path}' appears to be a file, not a folder.")
                    print("If you want to create a shareable link, use --mba instead of --mba-list")
                    print(f"Try: --mba \"{folder_path.split('/')[-1]}\"")
                    return []
        
        response.raise_for_status()
        data = response.json()
        
        items = data.get("value", [])
        print(f"\nFound {len(items)} items in {folder_path if folder_path else 'root'}:")
        
        for i, item in enumerate(items, 1):
            item_name = item.get("name", "Unknown")
            item_type = "📁 Folder" if item.get("folder") else "📄 File"
            print(f"{i}. {item_type}: {item_name}")
            
        return items
    
    except requests.exceptions.HTTPError as e:
        status_code = e.response.status_code
        error_msg = e.response.json().get("error", {}).get("message", str(e))
        logger.error(f"HTTP Error {status_code}: {error_msg}")
        print(f"Error listing items: {error_msg}")
    except Exception as e:
        logger.error(f"Error listing items: {str(e)}")
        print(f"Error listing items: {str(e)}")
    
    return []

def create_sharing_link(access_token, file_path):
    """
    Create a shareable link for a file in OneDrive.
    
    Args:
        access_token: Valid access token for Microsoft Graph API
        file_path: Path to the file in OneDrive
        
    Returns:
        Shareable link URL if successful, None otherwise
    """
    # Normalize path by replacing backslashes with forward slashes
    file_path = file_path.replace("\\", "/")
    
    if file_path.startswith("/"):
        file_path = file_path[1:]  # Remove leading slash
        
    # URL encode the file path for special characters
    api_endpoint = f"{GRAPH_API_ENDPOINT}/me/drive/root:/{file_path}:/createLink"
    
    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }
    
    # Create a sharing link that provides view-only access and doesn't expire
    body = {
        "type": "view",
        "scope": "anonymous"
    }
    
    logger.info(f"Creating sharing link for file: {file_path}")
    try:
        response = requests.post(api_endpoint, headers=headers, json=body)
        response.raise_for_status()
        data = response.json()
        
        sharing_link = data.get("link", {}).get("webUrl")
        if sharing_link:
            logger.info(f"Sharing link created successfully")
            print(f"\n✓ Sharing link created successfully!")
            print(f"\nFile: {file_path}")
            print(f"Shareable Link: {sharing_link}")
            return sharing_link
        else:
            logger.error("Sharing link not found in response")
            print("× Error: Sharing link not found in API response")
            
    except requests.exceptions.HTTPError as e:
        status_code = e.response.status_code
        error_msg = e.response.json().get("error", {}).get("message", str(e))
        logger.error(f"HTTP Error {status_code}: {error_msg}")
        print(f"× Error creating sharing link: {error_msg}")
        
        if status_code == 404:
            print("\nℹ️ The file might not exist. Check the file path and try again.")
            print("Use the --list option to see files in a folder.")
    
    except Exception as e:
        logger.error(f"Error creating sharing link: {str(e)}")
        print(f"× Error creating sharing link: {str(e)}")
    
    return None

def main():
    """Main function to parse arguments and execute the appropriate action."""
    parser = argparse.ArgumentParser(description="OneDrive Personal File Sharing Tool")
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--file", help="Path to the file in OneDrive to create a shareable link")
    group.add_argument("--list", help="Path to the folder in OneDrive to list contents", nargs="?", const="")
    group.add_argument("--mba", help="Path to file in MBA-Resources folder to create a shareable link", 
                      metavar="RELATIVE_PATH")
    group.add_argument("--mba-list", help="List contents of a subfolder in MBA-Resources", 
                      nargs="?", const="", metavar="SUBFOLDER")
    
    args = parser.parse_args()
    
    # Authenticate with Microsoft Graph API
    print("\n=== OneDrive Personal File Sharing Tool ===\n")
    access_token = authenticate_interactive()
    
    if not access_token:
        print("\n× Authentication failed. Cannot proceed.")
        sys.exit(1)
      # Process the request based on arguments
    if args.file:
        create_sharing_link(access_token, args.file)
    elif args.list is not None:  # Using is not None because args.list could be an empty string
        list_items_in_folder(access_token, args.list)
    elif args.mba is not None:
        # Normalize the path and combine with MBA-Resources path
        normalized_path = args.mba.replace("\\", "/")
        mba_file_path = f"{ONEDRIVE_MBA_PATH}/{normalized_path}"
        print(f"Looking for MBA file in OneDrive path: {mba_file_path}")
        create_sharing_link(access_token, mba_file_path)
    elif args.mba_list is not None:
        # List MBA-Resources folder or subfolder
        mba_folder_path = ONEDRIVE_MBA_PATH
        if args.mba_list:
            # Normalize the path
            normalized_path = args.mba_list.replace("\\", "/")
            mba_folder_path = f"{ONEDRIVE_MBA_PATH}/{normalized_path}"
        
        # Check if path ends with common file extensions - if so, suggest using --mba instead
        common_extensions = ['.mp4', '.pdf', '.txt', '.docx', '.pptx', '.xlsx']
        path_lower = mba_folder_path.lower()
        if any(path_lower.endswith(ext) for ext in common_extensions):
            print(f"\nNote: The path '{mba_folder_path}' appears to be a file, not a folder.")
            print("You may want to create a shareable link using --mba instead of listing its contents.")
            choice = input("\nWould you like to get a shareable link for this file instead? (y/n): ")
            if choice.lower() == 'y':
                create_sharing_link(access_token, mba_folder_path)
                return
                
        list_items_in_folder(access_token, mba_folder_path)

if __name__ == "__main__":
    main()
