#!/usr/bin/env python3
"""
Helper script for fixing authentication and logging in onedrive_share.py
This script modifies the authenticate_interactive function to eliminate
duplicate logging and fix the syntax errors.
"""

import sys
from pathlib import Path

# Get path to onedrive_share.py
onedrive_share_path = Path("d:/repos/mba-notebook-automation/notebook_automation/cli/onedrive_share.py")

if not onedrive_share_path.exists():
    print(f"Error: Could not find {onedrive_share_path}")
    sys.exit(1)

# Define the improved authenticate_interactive function
new_function = '''
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
        # Use ANSI colors for console output but not duplicate the log message
        print(f"{OKGREEN}Account found in cache: {accounts[0]['username']}{ENDC}")
        result = app.acquire_token_silent(SCOPES, account=accounts[0])
    
    if not result or "access_token" not in result:
        logger.info("No valid token in cache, initiating interactive authentication...")
        print(f"\\n{OKGREEN}Initiating interactive authentication...\\nA browser window will open for you to sign in.{ENDC}")
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
'''

# Read the file content
with open(onedrive_share_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace the existing function
start_marker = "def authenticate_interactive() -> str | None:"
end_marker = "def check_if_file_exists"

if start_marker not in content or end_marker not in content:
    print("Error: Function markers not found in the file")
    sys.exit(1)

# Find the start and end positions of the function
start_pos = content.find(start_marker)
end_pos = content.find(end_marker)

# Replace the function
new_content = content[:start_pos] + new_function + content[end_pos:]

# Write the updated file
with open(onedrive_share_path, 'w', encoding='utf-8') as f:
    f.write(new_content)

print(f"Successfully updated {onedrive_share_path}")
