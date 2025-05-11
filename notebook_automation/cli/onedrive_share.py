
#!/usr/bin/env python3
"""
OneDrive Personal File Sharing Script (CLI Version)

This script connects to OneDrive Personal using interactive authentication and
generates a shareable link for a specified file, or lists files/folders.

Usage:
    vault-onedrive-share --file <file_path_in_onedrive>
    vault-onedrive-share --list <folder_path_in_onedrive>
    vault-onedrive-share --notebook-resource <file_path_in_resources_root>
    vault-onedrive-share --notebook-resource-list <folder_path_in_resources_root>

Example:
    vault-onedrive-share --file "Documents/resume.pdf"
    vault-onedrive-share --list "Documents"
    vault-onedrive-share --notebook-resource "Books/textbook.pdf"
    vault-onedrive-share --notebook-resource-list "Value Chain Management"

"""

import os
import sys
import argparse
import requests
import msal
from pathlib import Path
from notebook_automation.tools.utils.config import (
    MICROSOFT_GRAPH_API_CLIENT_ID,
    AUTHORITY,
    SCOPES,
    TOKEN_CACHE_FILE,
    GRAPH_API_ENDPOINT,
    ONEDRIVE_LOCAL_RESOURCES_ROOT,
    logger,
)
from notebook_automation.cli.utils import OKGREEN, FAIL, WARNING, ENDC

def authenticate_interactive() -> str | None:
    """Authenticate with Microsoft Graph API using interactive authentication.

    Initiates an interactive authentication flow with Microsoft Graph API using MSAL.
    Loads and saves a token cache to avoid repeated logins. If a valid token is found in the cache,
    it is used; otherwise, the user is prompted to authenticate in a browser window.

    Returns:
        str | None: Access token if successful, None otherwise.

    Raises:
        Exception: If authentication fails or the token cannot be acquired.

    Example:
        >>> token = authenticate_interactive()
        >>> if token:
        ...     print("Authenticated!")
        ... else:
        ...     print("Authentication failed.")
    """
    logger.info("Starting interactive authentication flow...")
    cache = msal.SerializableTokenCache()
    if os.path.exists(TOKEN_CACHE_FILE):
        try:
            with open(TOKEN_CACHE_FILE, "r") as f:
                cache_data = f.read()
                if cache_data:
                    cache.deserialize(cache_data)
                    logger.info("Token cache loaded successfully")
        except Exception as e:
            logger.warning(f"Could not load token cache: {e}")
    app = msal.PublicClientApplication(
        MICROSOFT_GRAPH_API_CLIENT_ID,
        authority=AUTHORITY,
        token_cache=cache
    )
    accounts = app.get_accounts()
    result = None
    if accounts:
        logger.info(f"Account found in token cache, attempting to use existing token...")
        print(f"{OKGREEN}Account found in cache: {accounts[0]['username']}{ENDC}")
        result = app.acquire_token_silent(SCOPES, account=accounts[0])
    if not result or "access_token" not in result:
        logger.info("No valid token in cache, initiating interactive authentication...")
        print(f"\n{OKGREEN}Initiating interactive authentication...\nA browser window will open for you to sign in.{ENDC}")
        try:
            result = app.acquire_token_interactive(
                scopes=SCOPES,
                prompt="select_account"
            )
            if "access_token" in result:
                logger.info("Interactive authentication successful!")
                print(f"{OKGREEN}✓ Interactive authentication successful!{ENDC}")
                with open(TOKEN_CACHE_FILE, "w") as f:
                    f.write(cache.serialize())
                logger.info(f"Token cache saved to {TOKEN_CACHE_FILE}")
            else:
                error = result.get("error")
                error_desc = result.get("error_description")
                logger.error(f"Interactive authentication failed: {error} - {error_desc}")
                print(f"{FAIL}× Interactive authentication failed: {error} - {error_desc}{ENDC}")
                return None
        except Exception as e:
            logger.error(f"Exception during interactive authentication: {type(e).__name__}: {e}")
            print(f"{FAIL}× Exception during authentication: {str(e)}{ENDC}")
            return None
    return result.get("access_token") if result and "access_token" in result else None

def check_if_file_exists(access_token: str, file_path: str) -> dict | None:
    """Check if a file exists in OneDrive and return its metadata.

    Args:
        access_token (str): Valid access token for Microsoft Graph API.
        file_path (str): Path to the file in OneDrive.

    Returns:
        dict | None: File metadata if found, None otherwise.

    Raises:
        requests.exceptions.HTTPError: If the API call fails with an HTTP error.

    Example:
        >>> meta = check_if_file_exists(token, "Documents/report.pdf")
        >>> if meta:
        ...     print(meta["name"])
    """
    file_path = file_path.replace("\\", "/")
    if file_path.startswith("/"):
        file_path = file_path[1:]
    api_endpoint = f"{GRAPH_API_ENDPOINT}/me/drive/root:/{file_path}"
    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }
    try:
        response = requests.get(api_endpoint, headers=headers)
        response.raise_for_status()
        return response.json()
    except:
        return None

def list_items_in_folder(access_token: str, folder_path: str = "") -> list[dict]:
    """List items in a OneDrive folder.

    Lists the files and folders in a specified OneDrive folder using the Microsoft Graph API.
    If the path is a file, not a folder, prints a message and returns an empty list.

    Args:
        access_token (str): Valid access token for Microsoft Graph API.
        folder_path (str, optional): Path to the folder in OneDrive (default: root).

    Returns:
        list[dict]: List of items (files/folders) in the folder, or an empty list on error.

    Raises:
        requests.exceptions.HTTPError: If the API call fails with an HTTP error.

    Example:
        >>> items = list_items_in_folder(token, "Documents")
        >>> for item in items:
        ...     print(item["name"])
    """
    folder_path = folder_path.replace("\\", "/")
    if folder_path and folder_path.startswith("/"):
        folder_path = folder_path[1:]
    if folder_path:
        file_data = check_if_file_exists(access_token, folder_path)
        if file_data and "folder" not in file_data:
            print(f"\n{WARNING}Note: '{folder_path}' appears to be a file, not a folder.{ENDC}")
            print(f"{WARNING}If you want to create a shareable link, use --notebook-resource instead of --notebook-resource-list{ENDC}")
            print(f"{WARNING}Try: --notebook-resource \"{folder_path}\"{ENDC}")
            choice = input(f"\n{OKGREEN}Would you like to get a shareable link for this file instead? (y/n): {ENDC}")
            if choice.lower() == 'y':
                return create_sharing_link(access_token, folder_path)
            else:
                return []
    api_endpoint = f"{GRAPH_API_ENDPOINT}/me/drive/root"
    if folder_path:
        api_endpoint = f"{api_endpoint}:/{folder_path}:"
    api_endpoint = f"{api_endpoint}/children"
    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }
    logger.info(f"Listing items in folder: {folder_path if folder_path else 'root'}")
    try:
        response = requests.get(api_endpoint, headers=headers)
        response.raise_for_status()
        data = response.json()
        items = data.get("value", [])
        print(f"\n{OKGREEN}Found {len(items)} items in {folder_path if folder_path else 'root'}:{ENDC}")
        for i, item in enumerate(items, 1):
            item_name = item.get("name", "Unknown")
            item_type = "📁 Folder" if item.get("folder") else "📄 File"
            print(f"{OKGREEN}{i}. {item_type}: {item_name}{ENDC}")
        return items
    except requests.exceptions.HTTPError as e:
        status_code = e.response.status_code
        error_msg = e.response.json().get("error", {}).get("message", str(e))
        logger.error(f"HTTP Error {status_code}: {error_msg}")
        print(f"{FAIL}Error listing items: {error_msg}{ENDC}")
        if status_code == 404 and folder_path:
            print(f"\n{WARNING}The path '{folder_path}' was not found.{ENDC}")
            print(f"{WARNING}Double-check the path or try using --list with a parent folder to navigate.{ENDC}")
    except Exception as e:
        logger.error(f"Error listing items: {str(e)}")
        print(f"{FAIL}Error listing items: {str(e)}{ENDC}")
    return []

def create_sharing_link(access_token: str, file_path: str) -> str | None:
    """Create a shareable link for a file in OneDrive.

    Uses the Microsoft Graph API to create a view-only, anonymous sharing link for a file.
    Prints the link and returns it if successful.

    Args:
        access_token (str): Valid access token for Microsoft Graph API.
        file_path (str): Path to the file in OneDrive.

    Returns:
        str | None: Shareable link URL if successful, None otherwise.

    Raises:
        requests.exceptions.HTTPError: If the API call fails with an HTTP error.

    Example:
        >>> link = create_sharing_link(token, "Documents/report.pdf")
        >>> print(link)
    """
    file_path = file_path.replace("\\", "/")
    if file_path.startswith("/"):
        file_path = file_path[1:]
    file_data = check_if_file_exists(access_token, file_path)
    if not file_data:
        print(f"\n{FAIL}× File not found: {file_path}{ENDC}")
        print(f"{FAIL}Use the --list option to browse available files.{ENDC}")
        return None
    if file_data.get("folder"):
        print(f"\n{WARNING}× The path '{file_path}' is a folder, not a file.{ENDC}")
        print(f"{WARNING}Use the --list option to list its contents instead.{ENDC}")
        print(f"{WARNING}Try: --list \"{file_path}\"{ENDC}")
        return None
    api_endpoint = f"{GRAPH_API_ENDPOINT}/me/drive/root:/{file_path}:/createLink"
    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }
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
            print(f"\n{OKGREEN}✓ Sharing link created successfully!{ENDC}")
            print(f"{OKGREEN}File: {file_path}{ENDC}")
            print(f"{OKGREEN}Shareable Link: {sharing_link}{ENDC}")
            return sharing_link
        else:
            logger.error("Sharing link not found in response")
            print(f"{FAIL}× Error: Sharing link not found in API response{ENDC}")
    except requests.exceptions.HTTPError as e:
        status_code = e.response.status_code
        error_msg = e.response.json().get("error", {}).get("message", str(e))
        logger.error(f"HTTP Error {status_code}: {error_msg}")
        print(f"{FAIL}× Error creating sharing link: {error_msg}{ENDC}")
        if status_code == 404:
            print(f"\n{WARNING}ℹ️ The file might not exist. Check the file path and try again.{ENDC}")
            print(f"{WARNING}Use the --list option to see files in a folder.{ENDC}")
    except Exception as e:
        logger.error(f"Error creating sharing link: {str(e)}")
        print(f"{FAIL}× Error creating sharing link: {str(e)}{ENDC}")
    return None

def main() -> None:
    """Main function to parse arguments and execute the appropriate action.

    Parses command-line arguments, authenticates with Microsoft Graph, and either lists
    folder contents or creates a shareable link for a file, depending on the arguments.

    Returns:
        None

    Example:
        When called from the command line:
            $ vault-onedrive-share --file "Documents/report.pdf"
    """
    parser = argparse.ArgumentParser(description="OneDrive Personal File Sharing Tool")
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--file", help="Path to the file in OneDrive to create a shareable link")
    group.add_argument("--list", help="Path to the folder in OneDrive to list contents", nargs="?", const="")
    group.add_argument("--notebook-resource", help="Path to file in notebook resources root to create a shareable link", metavar="RELATIVE_PATH")
    group.add_argument("--notebook-resource-list", help="List contents of a subfolder in notebook resources root", nargs="?", const="", metavar="SUBFOLDER")
    parser.add_argument("--verbose", action="store_true", help="Show detailed output and progress information")
    parser.add_argument("--debug", action="store_true", help="Enable debug logging output")

    args = parser.parse_args()

    # Set logger level for debug mode
    if args.debug:
        logger.setLevel(10)  # logging.DEBUG
        print(f"{OKGREEN}[DEBUG] Debug logging enabled{ENDC}")

    print(f"\n{OKGREEN}=== OneDrive Personal File Sharing Tool ==={ENDC}\n")
    access_token = authenticate_interactive()

    if not access_token:
        print(f"\n{FAIL}× Authentication failed. Cannot proceed.{ENDC}")
        sys.exit(1)

    # Process the request based on arguments
    if args.file:
        if args.verbose:
            print(f"{OKGREEN}Creating shareable link for file: {args.file}{ENDC}")
        create_sharing_link(access_token, args.file)
    elif args.list is not None:
        if args.verbose:
            print(f"{OKGREEN}Listing contents of folder: {args.list if args.list else 'root'}{ENDC}")
        list_items_in_folder(access_token, args.list)
    elif args.notebook_resource is not None:
        normalized_path = args.notebook_resource.replace("\\", "/")
        resource_file_path = str(Path(ONEDRIVE_LOCAL_RESOURCES_ROOT) / normalized_path).replace("\\", "/")
        if args.verbose:
            print(f"{OKGREEN}Looking for notebook resource file in OneDrive path: {resource_file_path}{ENDC}")
        create_sharing_link(access_token, resource_file_path)
    elif args.notebook_resource_list is not None:
        resource_folder_path = str(ONEDRIVE_LOCAL_RESOURCES_ROOT)
        if args.notebook_resource_list:
            normalized_path = args.notebook_resource_list.replace("\\", "/")
            resource_folder_path = str(Path(ONEDRIVE_LOCAL_RESOURCES_ROOT) / normalized_path).replace("\\", "/")
        if args.verbose:
            print(f"{OKGREEN}Listing contents of notebook resources folder: {resource_folder_path}{ENDC}")
        list_items_in_folder(access_token, resource_folder_path)

if __name__ == "__main__":
    main()
