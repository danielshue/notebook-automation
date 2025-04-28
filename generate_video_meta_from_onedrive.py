#!/usr/bin/env python3
"""
OneDrive Video Link Generator for MBA Course Materials

This script generates shareable OneDrive links for video files stored in OneDrive
and creates corresponding reference notes in an Obsidian vault. The script follows
the folder structure from OneDrive to maintain consistent organization. Generated
notes include '-video' suffix for custom icon support in Obsidian.

Features:
- Authenticates with Microsoft Graph API using secure token handling
- Recursively processes video files in OneDrive
- Creates shareable links for videos
- Generates markdown notes in Obsidian vault with links to videos
- Appends '-video' suffix to generated files for custom Obsidian icons
- Automatically extracts course/program metadata from file paths
- Finds associated transcript files and generates AI summaries (with OpenAI)
- Supports single file processing for testing
- Incremental processing with results tracking
- Enhanced retry logic with connection pooling and timeouts
- Failed files management with dedicated tracking and logging
- Robust error handling with error categorization
- API health checks before processing
- Preserves user-modified notes and progress tracking fields
- Supports top-level video metadata in human-readable format

Usage:
    wsl python3 generate_video_links.py                       # Process all videos
    wsl python3 generate_video_links.py -f "path/to/file.mp4" # Process single file
    wsl python3 generate_video_links.py --dry-run             # Test without making changes
    wsl python3 generate_video_links.py --no-summary          # Skip OpenAI summary generation
    wsl python3 generate_video_links.py --debug               # Enable debug logging
    wsl python3 generate_video_links.py --retry-failed        # Only retry previously failed files
    wsl python3 generate_video_links.py --timeout 15          # Set custom API request timeout (seconds)

Environment Variables:
    OPENAI_API_KEY: API key for OpenAI (optional, for summary generation)

Requirements:
    - msal: Microsoft Authentication Library for Python
    - requests: For HTTP communication with Microsoft Graph API
    - pyyaml: For YAML parsing of metadata templates
    - openai: For AI summary generation (optional)
    - python-dotenv: For loading environment variables from .env file
    - cryptography (optional): For enhanced token security
    - urllib3: For retry strategies and connection pooling

Azure Best Practices:
    - Secure token storage with encryption
    - Progressive token acquisition (silent -> interactive)
    - Proper error handling and retry logic for all Microsoft Graph API calls
    - Cross-platform compatibility for WSL environments
    - Exponential backoff for API rate limiting
    - Connection pooling for optimized performance
    - API health checks and graceful degradation
    - Categorized error handling and reporting
    - Request timeouts to prevent hanging connections

File Organization:
    - Authentication: Microsoft Graph API authentication using Azure best practices
    - OneDrive Access: Functions for accessing and querying OneDrive
    - Metadata Extraction: Functions to infer course/program from file paths
    - Note Generation: Functions to create markdown notes in Obsidian
    - Transcript Processing: Functions to find and process transcript files
    - OpenAI Integration: Functions to generate AI summaries from transcripts
    - Command-line Interface: Argument parsing and script execution flow
    - Error Handling: Error categorization and failed files management
    - Logging: Separate logging for general operations and failed files

User Notes Preservation:
    - User notes sections are always preserved during updates
    - Progress tracking fields (status, completion-date, review-date, comprehension) 
      are preserved if they contain user-defined values
    - Notes section always remains at the bottom of the document

Video Metadata Format:
    - video-duration: Human-readable format (e.g., "4 minutes 3 seconds")
    - video-uploaded: Date in YYYY-MM-DD format
    - video-size: Size in MB with 2 decimal precision (e.g., "16.57 MB")

Author: Daniel Shue
Created: April 2025
Last Updated: April 19, 2025
"""

import os
import requests
from msal import PublicClientApplication
from pathlib import Path
from datetime import datetime
import json
import logging
import yaml
import re
import openai
import argparse
import time
from dotenv import load_dotenv
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry
from functools import wraps, partial
import sys
from tools.notes.video_markdown_generator import create_or_update_markdown_note_for_video

# Load environment variables from .env file
load_dotenv()

# Setup logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler("video_links_generator.log"),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

# Create a separate logger for failed files
failed_logger = logging.getLogger("failed_files")
failed_logger.setLevel(logging.INFO)
failed_handler = logging.FileHandler("failed_files.log")
failed_handler.setFormatter(logging.Formatter('%(asctime)s - %(levelname)s - %(message)s'))
failed_logger.addHandler(failed_handler)
failed_logger.propagate = False  # Don't propagate to root logger

# Error categories for better tracking
class ErrorCategories:
    NETWORK = "network_error"
    AUTHENTICATION = "authentication_error"
    PERMISSION = "permission_error"
    RATE_LIMIT = "rate_limit_error"
    SERVER = "server_error"
    TIMEOUT = "timeout_error"
    UNKNOWN = "unknown_error"

def create_requests_session(retries=3, backoff_factor=0.5, status_forcelist=(500, 502, 503, 504), timeout=10):
    """
    Create a requests session with retry capabilities and connection pooling.
    Implements Azure best practice for HTTP client resilience with exponential backoff.
    
    Args:
        retries: Maximum number of retries
        backoff_factor: Backoff factor for retry delay calculation
        status_forcelist: HTTP status codes that should trigger a retry
        timeout: Request timeout in seconds (default: 10)
        
    Returns:
        A configured requests.Session object
    """
    session = requests.Session()
    
    # Configure the retry strategy with exponential backoff
    retry_strategy = Retry(
        total=retries,
        backoff_factor=backoff_factor,
        status_forcelist=status_forcelist,
        allowed_methods=frozenset(['HEAD', 'GET', 'PUT', 'POST', 'DELETE', 'OPTIONS', 'TRACE'])
    )
    
    # Mount the retry adapter to both HTTP and HTTPS
    adapter = HTTPAdapter(max_retries=retry_strategy, 
                          pool_connections=10,  # Connection pool size
                          pool_maxsize=10)      # Maximum number of connections
    session.mount("http://", adapter)
    session.mount("https://", adapter)
    
    # Set default timeouts to prevent hanging requests
    session.request = partial(session.request, timeout=timeout)  # Use the provided timeout value
    
    return session

def categorize_error(error, status_code=None):
    """
    Categorize errors for better tracking and reporting.
    
    Args:
        error: The exception or error message
        status_code: Optional HTTP status code
        
    Returns:
        Error category string
    """
    error_str = str(error).lower()
    
    if status_code:
        if status_code == 401 or status_code == 403:
            return ErrorCategories.AUTHENTICATION
        elif status_code == 429:
            return ErrorCategories.RATE_LIMIT
        elif status_code >= 500:
            return ErrorCategories.SERVER
        
    if isinstance(error, requests.exceptions.Timeout) or "timeout" in error_str:
        return ErrorCategories.TIMEOUT
    elif isinstance(error, requests.exceptions.ConnectionError) or "connection" in error_str:
        return ErrorCategories.NETWORK
    elif "permission" in error_str or "access" in error_str or "forbidden" in error_str:
        return ErrorCategories.PERMISSION
    elif "rate" in error_str or "limit" in error_str or "throttl" in error_str:
        return ErrorCategories.RATE_LIMIT
    
    return ErrorCategories.UNKNOWN

# Configuration
CLIENT_ID = "489ad055-e4b0-4898-af27-53506ce83db7"
AUTHORITY = "https://login.microsoftonline.com/common"
SCOPES = ["Files.ReadWrite.All", "Sites.Read.All"]
GRAPH_API_ENDPOINT = "https://graph.microsoft.com/v1.0"  # Microsoft Graph API endpoint

# WSL path handling - normalize paths for WSL environment
def normalize_wsl_path(path):
    """Convert Windows or WSL path to proper WSL format."""
    if isinstance(path, str):
        # Convert Windows-style path to WSL path
        if ':' in path:  # Windows path with drive letter
            drive = path[0].lower()
            wsl_path = f"/mnt/{drive}{path[2:].replace('\\', '/')}"
            return wsl_path
        return path.replace('\\', '/')  # Just convert backslashes to forward slashes
    return path  # Return as is if not a string

# Define paths with proper WSL normalization
RESOURCES_ROOT = Path(normalize_wsl_path(r'/mnt/c/Users/danielshue/OneDrive/Education/MBA-Resources'))  # The folder containing your videos in OneDrive
VAULT_ROOT = Path(normalize_wsl_path(r'/mnt/d/Vault/01_Projects/MBA'))  # Your Obsidian vault root
ONEDRIVE_BASE = '/Education/MBA-Resources'  # OneDrive path to your MBA Resources (this is a URL path, not a file system path)
METADATA_FILE = Path(normalize_wsl_path(r'/mnt/d/repos/mba-notebook-automation/metadata.yaml'))  # Path to metadata templates

# Print paths to verify correct path normalization
logger.info(f"Using resources path: {RESOURCES_ROOT}")
logger.info(f"Using vault path: {VAULT_ROOT}")
logger.info(f"Using metadata file path: {METADATA_FILE}")

# OpenAI configuration - get API key from environment variable for security
OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
# Initialize OpenAI client if API key is available
if OPENAI_API_KEY:
    openai.api_key = OPENAI_API_KEY
else:
    logger.warning("OpenAI API key not found in environment variables. Summary generation will be disabled.")

# Function to load templates from metadata.yaml
def load_templates():
    """Load YAML templates from the metadata file."""
    if not os.path.exists(METADATA_FILE):
        logger.error(f"Metadata file not found: {METADATA_FILE}")
        return {}
    
    try:
        # Read the metadata file content
        with open(METADATA_FILE, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Remove comments at the start of the file that aren't part of YAML structure
        content = re.sub(r'^#.*?\n', '', content, flags=re.MULTILINE)
        
        # Split the file by the YAML document separator
        yaml_docs = content.split('---\n')
          # Filter out empty documents and parse each YAML document
        templates = {}
        successful_templates = []        
        for doc in yaml_docs:
            # Skip empty documents or those that only contain comments
            doc = doc.strip()
            if not doc or doc.startswith('#'):
                continue
                
            try:
                # Add proper YAML document start marker if not present
                if not doc.startswith('---'):
                    doc = '---\n' + doc
                
                yaml_content = yaml.safe_load(doc)
                # Debug output to see what's being loaded
                logger.debug(f"Loaded YAML content: {yaml_content}")
                
                # Check if parsing produced a valid dictionary
                if yaml_content and isinstance(yaml_content, dict):
                    template_type = yaml_content.get('template-type')
                    if template_type:
                        templates[template_type] = yaml_content
                        successful_templates.append(template_type)
                        logger.debug(f"Successfully loaded template: {template_type}")
                    else:
                        logger.warning(f"Skipping YAML document without template-type: {yaml_content}")
                else:
                    logger.warning(f"Skipping invalid YAML content (not a dictionary)")
            except yaml.YAMLError as e:
                logger.warning(f"Error parsing YAML document: {e}")
                return
        
        logger.info(f"Loaded {len(templates)} templates from metadata file: {', '.join(successful_templates)}")
        return templates
    
    except Exception as e:
        logger.error(f"Error loading templates: {e}")
        return {}

# Video file extensions to process
VIDEO_EXTENSIONS = {'.mp4', '.mov', '.avi', '.mkv', '.webm'}

def encrypt_token_data(data, key=None):
    """
    Encrypt token data with a key (uses simple obfuscation if cryptography package not available).
    Following Azure security best practice to never store tokens in plain text.
    """
    try:
        # Try to use strong encryption if cryptography package is available
        from cryptography.fernet import Fernet
        from cryptography.hazmat.primitives import hashes
        from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
        import base64
        
        if key is None:
            # Use machine-specific information as salt
            import socket
            salt = socket.gethostname().encode()
            # Derive a key from the CLIENT_ID and salt
            kdf = PBKDF2HMAC(
                algorithm=hashes.SHA256(),
                length=32,
                salt=salt,
                iterations=100000,
            )
            key = base64.urlsafe_b64encode(kdf.derive(CLIENT_ID.encode()))
        
        f = Fernet(key)
        return f.encrypt(json.dumps(data).encode()).decode()
    
    except ImportError:
        # Fall back to simple obfuscation if cryptography package not available
        logger.warning("Cryptography package not found. Using simple obfuscation for token cache.")
        import base64
        return base64.b64encode(json.dumps(data).encode()).decode()

def decrypt_token_data(encrypted_data, key=None):
    """
    Decrypt token data (handles both strong encryption and simple obfuscation).
    """
    try:
        # Try to use strong decryption if cryptography package is available
        from cryptography.fernet import Fernet, InvalidToken
        from cryptography.hazmat.primitives import hashes
        from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
        import base64
        
        if key is None:
            # Use machine-specific information as salt
            import socket
            salt = socket.gethostname().encode()
            # Derive a key from the CLIENT_ID and salt
            kdf = PBKDF2HMAC(
                algorithm=hashes.SHA256(),
                length=32,
                salt=salt,
                iterations=100000,
            )
            key = base64.urlsafe_b64encode(kdf.derive(CLIENT_ID.encode()))
        
        try:
            f = Fernet(key)
            return json.loads(f.decrypt(encrypted_data.encode()).decode())
        except InvalidToken:
            # If the token wasn't encrypted with Fernet, try the fallback method
            return json.loads(base64.b64decode(encrypted_data).decode())
            
    except ImportError:
        # Fall back to simple deobfuscation
        import base64
        return json.loads(base64.b64decode(encrypted_data).decode())

def authenticate_graph_api(force_refresh=False):
    """
    Authenticate with Microsoft Graph API using interactive authentication with robust fallback.
    Improved based on the implementation in onedrive_share.py.
    
    Args:
        force_refresh: If True, ignore cached tokens and force new authentication
    
    Returns:
        str: Access token for Microsoft Graph API
        
    Raises:
        Exception: If authentication fails after trying all methods
    """
    import msal
    import os
    import webbrowser
    import json
    
    # Microsoft Graph API Configuration
    CLIENT_ID = "489ad055-e4b0-4898-af27-53506ce83db7"
    AUTHORITY = "https://login.microsoftonline.com/common"
    SCOPES = ["Files.ReadWrite.All", "Sites.Read.All"]
    TOKEN_CACHE_FILE = "token_cache.bin"

    # Remove the token cache if force_refresh is requested
    if force_refresh and os.path.exists(TOKEN_CACHE_FILE):
        os.remove(TOKEN_CACHE_FILE)
        logger.info("Token cache file removed for forced refresh.")
        print("🔄 Force refreshing authentication token...")

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
            logger.warning(f"Could not load token cache: {e}. Will authenticate from scratch.")

    # Create the MSAL application
    app = msal.PublicClientApplication(
        CLIENT_ID,
        authority=AUTHORITY,
        token_cache=cache
    )

    # Try to get a token from the cache first
    accounts = app.get_accounts()
    result = None
    
    if accounts and not force_refresh:
        logger.info("Found account in cache, attempting to use existing token...")
        print(f"Account found in cache: {accounts[0]['username']}")
        result = app.acquire_token_silent(SCOPES, account=accounts[0])
        
        if result and "access_token" in result:
            logger.info("Token acquired from cache successfully!")
            print("✓ Using cached authentication token")
            
            # Save the cache (refreshes expiry)
            with open(TOKEN_CACHE_FILE, "w") as f:
                f.write(cache.serialize())
            
            return result["access_token"]
        else:
            logger.info("Token not found in cache or expired")
            print("Token expired or not found in cache. Initiating new authentication.")

    # Method 1: Try interactive authentication first (preferred for desktop apps)
    logger.info("Initiating interactive authentication flow...")
    print("\n🔑 Initiating interactive authentication...")
    print("A browser window will open for you to sign in.")
    print("If it doesn't open automatically, check your browser or manually open the URL that will be displayed.")
    
    try:
        # This will automatically open the default web browser for authentication
        result = app.acquire_token_interactive(
            scopes=SCOPES,
            prompt="select_account"  # Force prompt to select account
        )
        
        if result and "access_token" in result:
            logger.info("Interactive authentication successful!")
            print("✓ Interactive authentication successful!")
            
            # Save token cache for future use
            with open(TOKEN_CACHE_FILE, "w") as f:
                f.write(cache.serialize())
            logger.info(f"Token cache saved to {TOKEN_CACHE_FILE}")
            
            return result["access_token"]
        else:
            error = result.get("error", "unknown")
            error_desc = result.get("error_description", "No details provided") 
            logger.warning(f"Interactive authentication failed: {error} - {error_desc}")
            print("× Interactive authentication failed. Trying alternative method...")
    except Exception as e:
        logger.warning(f"Exception during interactive authentication: {type(e).__name__}: {e}")
        print(f"× Exception during authentication: {str(e)}")
        print("Trying alternative method...")

    # Method 2: Fall back to device flow authentication
    logger.info("Falling back to device flow authentication...")
    print("\n🔐 Using device code authentication instead:")
    
    try:
        flow = app.initiate_device_flow(scopes=SCOPES)
        
        if "user_code" not in flow:
            error_details = json.dumps(flow) if flow else "No response details"
            logger.error(f"Failed to create device flow: {error_details}")
            raise Exception("Failed to create device flow for authentication.")
        
        print("\nTo authenticate, use a browser to visit:")
        print(flow["verification_uri"])
        print("and enter the code:")
        print(flow["user_code"])
        print("")
        
        logger.info("Waiting for user to complete device flow authentication...")
        result = app.acquire_token_by_device_flow(flow)
        
        if "access_token" in result:
            logger.info("Device flow authentication successful!")
            print("✓ Authentication successful!")
            
            # Save token cache for future use
            with open(TOKEN_CACHE_FILE, "w") as f:
                f.write(cache.serialize())
            logger.info(f"Token cache saved to {TOKEN_CACHE_FILE}")
            
            return result["access_token"]
        else:
            error = result.get("error", "unknown")
            error_desc = result.get("error_description", "No details provided")
            error_message = f"Device flow authentication failed: {error} - {error_desc}"
            logger.error(error_message)
            raise Exception(error_message)
    except Exception as e:
        logger.error(f"Authentication failed after trying all methods: {e}")
        raise Exception(f"Authentication failed: {e}")
    if "access_token" in result:
        with open(TOKEN_CACHE_FILE, "w") as f:
            f.write(cache.serialize())
        return result["access_token"]
    else:
        raise Exception(f"Authentication failed: {result.get('error_description', 'Unknown error')}")

def get_file_in_onedrive(relative_path, headers, session=None):
    """
    Get a file's metadata in OneDrive by its relative path.
    Improved based on the implementation in onedrive_share.py.
    
    Args:
        relative_path: The path to the file relative to ONEDRIVE_BASE
        headers: Headers including authentication token
        session: Optional requests session for connection pooling and retries
        
    Returns:
        File metadata dict if found, None if not found
    """
    # Use a session if provided, otherwise create a new requests object
    requester = session if session else requests
    
    # Normalize path by replacing backslashes with forward slashes
    import urllib.parse
    
    # First clean the path to ensure consistent formatting
    clean_path = relative_path.strip('/\\').replace('\\', '/')
    
    # Build the full path
    full_path = f"{ONEDRIVE_BASE}/{clean_path}" if clean_path else ONEDRIVE_BASE
    full_path = full_path.rstrip('/')  # Remove trailing slash if present
    
    # URL encode the path for API call
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
    Create a shareable link for the file with specified ID.
    
    Args:
        file_id: The ID of the file to create a share link for
        headers: Authentication headers
        session: Optional requests session for connection pooling and retries
        
    Returns:
        str: The shareable link URL if successful, None otherwise
    """
    # Use a session if provided, otherwise create a new requests object
    requester = session if session else requests
    
    url = f"{GRAPH_API_ENDPOINT}/me/drive/items/{file_id}/createLink"
    
    # Create a sharing link that provides view-only access and doesn't expire
    body = {
        "type": "view",
        "scope": "anonymous"
    }
    
    # Add retry logic - Microsoft Graph API sometimes has intermittent issues
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

def create_share_link(file_id_or_path, headers, session=None):
    """
    Create a shareable link for a file in OneDrive by ID or path.
    Improved based on the implementation in onedrive_share.py.
    
    This function can be called in two ways:
    1. With a file ID (legacy mode)
    2. With a file path (new mode, using the approach from onedrive_share.py)
    
    Args:
        file_id_or_path: The ID or path of the file to create a share link for
        headers: Authentication headers
        session: Optional requests session for connection pooling and retries
        
    Returns:
        str: The shareable link URL if successful, None otherwise
    """
    # Check if this is a path or ID
    if isinstance(file_id_or_path, str) and ('/' in file_id_or_path or '\\' in file_id_or_path):
        # This is a file path - use the path-based approach
        return create_share_link_by_path(file_id_or_path, headers, session)
    else:
        # This is a file ID - use the ID-based approach
        return create_share_link_by_id(file_id_or_path, headers, session)

def create_share_link_by_path(file_path, headers, session=None):
    """
    Create a shareable link for a file in OneDrive by path.
    Implementation based on onedrive_share.py.
    
    Args:
        file_path: Path to the file in OneDrive
        headers: Authentication headers
        session: Optional requests session for connection pooling and retries
        
    Returns:
        Shareable link URL if successful, None otherwise
    """
    # Use a session if provided, otherwise create a new requests object
    requester = session if session else requests
    
    # Extract access token from headers
    access_token = headers.get('Authorization', '').replace('Bearer ', '')
    if not access_token:
        logger.error("No valid access token provided in headers")
        return None
    
    # Normalize path by replacing backslashes with forward slashes
    file_path = file_path.replace("\\", "/")
    
    if file_path.startswith("/"):
        file_path = file_path[1:]  # Remove leading slash
    
    # First check if file exists
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

def infer_course_and_program(path):
    """Attempt to infer course and program from file path."""
    path_parts = Path(path).parts
    
    # MBA Program structure:
    # - Program folders (Value Chain Management, Financial Management, etc.)
    # - Course folders within programs
    # - Module folders (01_xxx, 02_xxx)
    # - Lesson folders within modules
    # - Class folders (sometimes)
      # Initialize with default values
    info = {
        'program': 'MBA Program',
        'course': 'Unknown Course',
        'module': "Unknown Module",
        'lesson': "Unknown Lesson",
        'class': "Unknown Class"
    }
    
    # Known program folders from your MBA structure
    program_folders = [
        'Value Chain Management',
        'Financial Management',
        'Focus Area Specialization',
        'Strategic Leadership and Management',
        'Managerial Economics and Business Analysis'
    ]
    
    # Try to identify components from path
    path_str = str(path).lower()
    
    # First, identify the program
    for program in program_folders:
        if program.lower() in path_str:
            info['program'] = program
            break
    
    # Find course by examining path parts
    for i, part in enumerate(path_parts):
        # Look for program folder, and the next folder is usually the course
        if part.lower() in [p.lower() for p in program_folders] and i+1 < len(path_parts):
            info['course'] = path_parts[i+1]
            # Once we find the course, break to avoid further matches
            break
            
    # Extract module info - typically follows pattern like 01_module-name
    module_match = re.search(r'(\d+[_-][\w-]+)', path_str)
    if module_match:
        module_part = module_match.group(1)
        # Clean up the module name
        module_name = module_part.replace('_', ' ').replace('-', ' ').title()
        # If it starts with a number, format as "Module X"
        if re.match(r'^\d+\s', module_name):
            num = module_name.split()[0]
            module_name = f"Module {num}"
        info['module'] = module_name
    
    # Extract lesson info - usually after module in path
    for i, part in enumerate(path_parts):
        if module_match and module_match.group(1).lower() in part.lower() and i+1 < len(path_parts):
            potential_lesson = path_parts[i+1]
            # Clean up lesson name
            lesson_name = potential_lesson.replace('_', ' ').replace('-', ' ').title()
            # Remove numbers at start if present
            lesson_name = re.sub(r'^\d+[\s_-]*', '', lesson_name)
            if lesson_name:
                info['lesson'] = lesson_name    # Look for class identifiers - try multiple detection methods
    
    # Method 1: In MBA structure, the class is typically the second part of the path
    # Example: "Value Chain Management/Managerial Accounting Business Decisions/accounting-for-managers/..."
    # Here "Managerial Accounting Business Decisions" is the class
    program_found = False
    for i, part in enumerate(path_parts):
        # First identify if this part is a program
        if part in program_folders:
            program_found = True
            # The next part after the program should be the class
            if i+1 < len(path_parts):
                info['class'] = path_parts[i+1]
                logger.debug(f"Found class from path structure: {info['class']}")
                break
    
    # Method 2: If we couldn't determine class from path structure, try other approaches
    if info['class'] == "Unknown Class" and info['course'] != "Unknown Course":
        # Use the course as the class (since course and class are often the same in MBA structures)
        info['class'] = info['course']
        logger.debug(f"Using course as class: {info['class']}")
    
    # Method 3: Direct "class X" pattern as fallback
    if info['class'] == "Unknown Class":
        class_match = re.search(r'class[\s_-]*(\w+)', path_str, re.IGNORECASE)
        if class_match:
            info['class'] = f"Class {class_match.group(1)}"
            logger.debug(f"Found class from explicit pattern: {info['class']}")
    
    # Method 4: Extract from structured folder names
    if info['class'] == "Unknown Class":
        for part in path_parts:
            # Look for parts with common class naming patterns
            if "class" in part.lower() or "course" in part.lower():
                potential_class = part.replace('-', ' ').replace('_', ' ').title()
                info['class'] = potential_class
                logger.debug(f"Found class from folder name: {info['class']}")
                break
    
    # Method 4: If we found a lesson but no class yet, check if any directory after the lesson looks like a class
    if info['class'] == "Unknown Class" and info['lesson'] != "Unknown Lesson":
        lesson_found = False
        for i, part in enumerate(path_parts):
                if lesson_found and i+1 < len(path_parts):
                    next_part = path_parts[i+1].lower()
                    # Check if the next folder after a lesson might be a class
                    if re.search(r'^\d+', next_part) or 'lecture' in next_part or 'session' in next_part or 'class' in next_part:
                        class_name = path_parts[i+1].replace('_', ' ').replace('-', ' ').title()
                        info['class'] = class_name
                        break
                
                # Mark when we've found the lesson folder to check what comes after it
                if info['lesson'].lower() in part.lower().replace('_', ' ').replace('-', ' '):
                    lesson_found = True
    
    # If we couldn't identify specific components, try to extract meaningful names
    # from the path parts directly
    if info['course'] == 'Unknown Course':
        for part in path_parts:
            clean_part = part.replace('_', ' ').replace('-', ' ').title()
            if any(keyword in clean_part.lower() for keyword in ['accounting', 'economics', 'finance', 'marketing', 'operations', 'management']):
                info['course'] = clean_part
                break
    
    logger.debug(f"Extracted metadata from path: {info}")
    return info

def get_onedrive_items(access_token, folder_path='/Education/MBA Resources'):
    """Get all items within a OneDrive folder recursively."""
    headers = {"Authorization": f"Bearer {access_token}"}
    all_items = []
    
    def fetch_folder_items(folder_path):
        encoded_path = folder_path.replace('/', '%2F')
        url = f"https://graph.microsoft.com/v1.0/me/drive/root:{encoded_path}:/children"
        items = []
        
        while url:
            resp = requests.get(url, headers=headers)
            if resp.status_code != 200:
                logger.error(f"Error fetching items from {folder_path}: {resp.text}")
                break
                
            data = resp.json()
            items.extend(data.get('value', []))
            
            # Check for next page
            url = data.get('@odata.nextLink', None)
            
        return items

    def process_folder(folder_path):
        logger.info(f"Processing OneDrive folder: {folder_path}")
        items = fetch_folder_items(folder_path)
        
        for item in items:
            if item.get('file', None):
                # It's a file
                all_items.append(item)
            elif item.get('folder', None):
                # It's a folder, process recursively
                subfolder_path = f"{folder_path}/{item['name']}"
                process_folder(subfolder_path)
    
    # Start recursive processing
    process_folder(folder_path)
    return all_items

def process_single_video(item, headers, templates, results_file='video_links_results.json', args=None):
    """
    Process a single video file through its complete lifecycle.
    Improved with better session handling and error management.
    """
    # Create a session for all API requests
    session = create_requests_session(timeout=getattr(args, 'timeout', 15) if args else 15)
    
    try:
        # Extract video path information
        item_path = item.get('parentReference', {}).get('path', '') + '/' + item['name']
        
        # Extract the path relative to ONEDRIVE_BASE
        if ONEDRIVE_BASE in item_path:
            rel_path = item_path[item_path.find(ONEDRIVE_BASE) + len(ONEDRIVE_BASE):].lstrip('/')
        else:
            rel_path = item['name']
        
        video_name = item.get('name', 'Unknown Video')
        
        logger.info(f"Processing video: {rel_path}")
        print(f"Processing video: {video_name}")
        
        # Create local file path reference
        local_path = RESOURCES_ROOT / rel_path.replace('/', os.path.sep)
        
        # Calculate note path early to check if it exists
        video_name = local_path.stem
        note_name = f"{video_name}-video.md"
        
        # Determine the corresponding vault path
        try:
            rel_path_for_vault = os.path.relpath(local_path, RESOURCES_ROOT)
        except ValueError:
            # Handle case where paths are on different drives
            rel_path_for_vault = Path(local_path.parts[-2]) / local_path.name
            
        vault_path = VAULT_ROOT / rel_path_for_vault
        vault_dir = vault_path.parent
        note_path = vault_dir / note_name
        
        # Check if note exists and --force is not set
        if note_path.exists() and not (args and getattr(args, 'force', False)):
            logger.info(f"Skipping {video_name}: note already exists and --force not set.")
            print(f"Skipping: {video_name} (note exists, use --force to overwrite)")
            return {
                'file': rel_path,
                'note_path': str(note_path),
                'success': True,
                'skipped': True,
                'reason': 'already_exists',
                'modified_date': datetime.now().isoformat()
            }
        
        # Verify the file exists in OneDrive before proceeding
        access_token = headers['Authorization'].replace('Bearer ', '')
        file_data = check_if_file_exists(item_path, access_token, session)
        
        if not file_data and item.get('id'):
            logger.info(f"File not found by path, but we have an ID. Proceeding with share link creation.")
        elif not file_data:
            logger.error(f"File not found in OneDrive: {item_path}")
            print(f"  └─ Error: File not found in OneDrive")
            return {
                'file': rel_path,
                'success': False,
                'error': "File not found in OneDrive",
                'timestamp': datetime.now().isoformat()
            }
          # Step 1: Create a shareable link
        print("  ├─ Creating shareable link...")
        # Try creating share link with both path and id for better reliability
        share_link = None
        
        # First try using the item_path (more reliable but might not work for items just moved)
        if item_path and '/' in item_path:
            logger.info(f"Attempting to create share link using path: {item_path}")
            share_link = create_share_link(item_path, headers, session)
            if share_link:
                logger.info("Successfully created share link using path")
        
        # If path-based sharing failed, fall back to ID-based sharing
        if not share_link and item.get('id'):
            logger.info(f"Attempting to create share link using ID: {item.get('id')}")
            share_link = create_share_link(item['id'], headers, session)
            
        if not share_link:
            logger.error(f"Could not create share link for: {rel_path}")
            print("  │  └─ Failed to create shareable link!")
            
            # Add to failed files tracking
            update_failed_files(
                {'file': rel_path, 'path': item_path}, 
                "Failed to create share link", 
                ErrorCategories.PERMISSION
            )
            
            return {
                'file': rel_path,
                'success': False,
                'error': "Could not create share link",
                'timestamp': datetime.now().isoformat()
            }
        
        logger.info(f"Created shareable link: {share_link}")
        print("  │  └─ Link created successfully ✓")
        
        # Step 2: Create a markdown note in the vault
        print("  ├─ Creating markdown note...")
        note_path = create_or_update_markdown_note_for_video(local_path, share_link, VAULT_ROOT, item, args=args, dry_run=getattr(args, 'dry_run', False))
        logger.info(f"Created markdown note: {note_path}")
        print("  │  └─ Note created at: " + str(note_path).replace(str(VAULT_ROOT), "").lstrip('/\\'))
        
        # Create result record
        result = {
            'file': rel_path,
            'share_link': share_link,
            'note_path': str(note_path),
            'success': True,
            'modified_date': datetime.now().isoformat()
        }
        
        # Step 3: Update the results file with this individual result
        update_results_file(results_file, result)
        print("  └─ Results saved ✓")
        
        return result
    
    except Exception as e:
        error_msg = str(e)
        error_category = categorize_error(e)
        logger.error(f"Error processing {item.get('name', 'unknown')}: {error_msg} ({error_category})")
        print(f"  └─ Error: {error_msg}")
        
        # Add to failed files tracking
        update_failed_files(
            {'file': item.get('name', 'unknown'), 'path': item.get('parentReference', {}).get('path', '')}, 
            error_msg, 
            error_category
        )
        
        return {
            'file': item.get('name', 'unknown'),
            'success': False,
            'error': error_msg,
            'timestamp': datetime.now().isoformat()
        }

def update_failed_files(file_info, error, category=ErrorCategories.UNKNOWN):
    """
    Update the failed_files.json with information about a failed file.
    
    Args:
        file_info: Dictionary with file information
        error: Error message or exception
        category: Error category from ErrorCategories
    """
    failed_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'failed_files.json')
    failed_list = []
    
    # Try to load existing failed files
    if os.path.exists(failed_file):
        try:
            with open(failed_file, 'r') as f:
                failed_list = json.load(f)
        except Exception as e:
            logger.error(f"Error reading failed files: {e}")
            failed_list = []
    
    # Prepare the record
    failed_record = {
        'file': file_info.get('file') or file_info.get('name', 'Unknown'),
        'path': file_info.get('path') or file_info.get('parentReference', {}).get('path', 'Unknown'),
        'error': str(error),
        'category': category,
        'timestamp': datetime.now().isoformat(),
        'retried': False
    }
    
    # Check if this file is already in the list
    found = False
    for i, item in enumerate(failed_list):
        if item.get('file') == failed_record['file']:
            # Update existing record
            failed_list[i] = failed_record
            found = True
            break
    
    # Add new record if not found
    if not found:
        failed_list.append(failed_record)
    
    # Log to failed files logger
    failed_logger.error(f"Failed file: {failed_record['file']}, Error: {failed_record['error']}, Category: {failed_record['category']}")
    
    # Write back to file
    try:
        with open(failed_file, 'w') as f:
            json.dump(failed_list, f, indent=2)
    except Exception as e:
        logger.error(f"Error writing failed files: {e}")

def check_api_health(session, headers=None):
    """
    Check Microsoft Graph API health by making a series of test API requests.
    
    Args:
        session: The requests session to use
        headers: Authentication headers to include in requests
        
    Returns:
        tuple: (is_healthy, message)
    """
    # If no headers provided, we can only check basic connectivity
    if not headers:
        url = "https://graph.microsoft.com/v1.0/"
        try:
            resp = session.get(url, timeout=5)
            if resp.status_code == 200:
                logger.info("Basic API connectivity check passed")
                return True, "API connectivity is healthy"
            else:
                logger.warning(f"API connectivity check returned status code {resp.status_code}")
                return False, f"API returned status code {resp.status_code}"
        except Exception as e:
            logger.error(f"API connectivity check failed: {e}")
            return False, f"API connectivity error: {str(e)}"
    
    # With headers, we can perform more comprehensive checks
    # 1. Check user profile access
    try:
        url = f"{GRAPH_API_ENDPOINT}/me"
        resp = session.get(url, headers=headers, timeout=5)
        if resp.status_code != 200:
            logger.warning(f"API user profile check failed with status code {resp.status_code}")
            return False, f"User profile access failed with status code {resp.status_code}"
        
        # Get the user's display name for the health message
        display_name = resp.json().get('displayName', 'Unknown User')
        
        # 2. Check OneDrive drive access
        url = f"{GRAPH_API_ENDPOINT}/me/drive"
        resp = session.get(url, headers=headers, timeout=5)
        if resp.status_code != 200:
            logger.warning(f"API drive check failed with status code {resp.status_code}")
            return False, f"OneDrive access failed with status code {resp.status_code}"
        
        # 3. Check OneDrive quota info
        quota = resp.json().get('quota', {})
        total = quota.get('total', 0)
        used = quota.get('used', 0)

        if total > 0:
            used_percent = (used / total) * 100
            if used_percent > 90:
                logger.warning(f"OneDrive is nearly full ({used_percent:.1f}% used)")
                return True, f"API healthy. Connected as {display_name}. OneDrive is {used_percent:.1f}% full (WARNING: low space)"
        
        # All checks passed
        logger.info("API health check passed successfully")
        return True, f"API healthy. Connected as {display_name}."
        
    except Exception as e:
        logger.error(f"API health check failed: {e}")
        return False, f"API health check error: {str(e)}"

def update_results_file(results_file, new_result):
    """Update the results JSON file with a new processed video result."""
    # Ensure the file exists
    if not os.path.exists(results_file):
        with open(results_file, 'w') as f:
            json.dump({
                'processed_videos': [],
                'errors': [],
                'timestamp': datetime.now().isoformat()
            }, f, indent=2)
    
    try:
        # Read existing results
        with open(results_file, 'r') as f:
            existing_results = json.load(f)
        
        # Find if this file was previously processed
        file_path = new_result.get('file')
        
        if 'processed_videos' not in existing_results:
            existing_results['processed_videos'] = []
        
        # Check if this file exists in the processed videos
        found = False
        for i, video in enumerate(existing_results['processed_videos']):
            if video.get('file') == file_path:
                # Update existing entry
                existing_results['processed_videos'][i] = new_result
                found = True
                break
        
        # If not found, add it
        if not found:
            existing_results['processed_videos'].append(new_result)
        
        # If there was an error, add it to the errors list
        if not new_result.get('success', True):
            if 'errors' not in existing_results:
                existing_results['errors'] = []
            
            existing_results['errors'].append({
                'file': file_path,
                'error': new_result.get('error', 'Unknown error'),
                'timestamp': datetime.now().isoformat()
            })
        
        # Update last run timestamp
        existing_results['last_run'] = datetime.now().isoformat()
        
        # Write updated results back to file
        with open(results_file, 'w') as f:
            json.dump(existing_results, f, indent=2)
        
    except Exception as e:
        logger.error(f"Error updating results file: {e}")

def process_videos_for_sharing(args=None):
    """
    Main function to find videos in OneDrive, generate links, and create notes.
    Uses improved authentication and session handling.
    """
    # Load templates first
    templates = load_templates()
    if not templates:
        logger.warning("Could not load templates from metadata.yaml. Using default templates.")
    else:
        logger.info(f"Successfully loaded {len(templates)} templates from metadata.yaml")
    
    # Authenticate using the enhanced authentication function 
    print("\n🔑 Authenticating with Microsoft Graph API...")
    try:
        # Check if we should force refresh the authentication
        force_refresh = args and getattr(args, 'refresh_auth', False)
        
        access_token = authenticate_graph_api(force_refresh=force_refresh)
        headers = {"Authorization": f"Bearer {access_token}"}
        logger.info("Authenticated to Microsoft Graph API")
        print("✅ Authentication successful\n")
        
        # Create a session for consistent API access
        session = create_requests_session(timeout=getattr(args, 'timeout', 15) if args else 15)
        
        # Perform health check to verify API access
        is_healthy, health_message = check_api_health(session, headers)
        if not is_healthy:
            logger.warning(f"API health check warning: {health_message}")
            print(f"⚠️ API health check warning: {health_message}")
            # Continue anyway as it might still work
    except Exception as e:
        error_category = categorize_error(e)
        logger.error(f"Authentication failed: {e} ({error_category})")
        print(f"\n❌ Authentication failed: {e}\n")
        return [], [f"Authentication error: {str(e)}"]
      # Get all items from OneDrive
    print("📂 Fetching files from OneDrive...")
    try:
        onedrive_items = get_onedrive_items(access_token, ONEDRIVE_BASE)
        logger.info(f"Found {len(onedrive_items)} items in OneDrive")
        print(f"✅ Found {len(onedrive_items)} files in OneDrive\n")
        
        # Debug: Print some sample items to check what's being returned
        if len(onedrive_items) > 0:
            logger.debug(f"First 5 items from OneDrive:")
            for i, item in enumerate(onedrive_items[:5]):
                logger.debug(f"  Item {i+1}: {item.get('name')} - Type: {item.get('file', {}).get('mimeType', 'folder')}") 
        else:
            logger.warning("No items found in OneDrive - check path configuration")
    except Exception as e:
        logger.error(f"Failed to fetch OneDrive items: {e}")
        print(f"❌ Failed to fetch files from OneDrive: {e}\n")
        return [], [f"OneDrive API error: {str(e)}"]
      # Filter for video files
    video_extensions = {'.mp4', '.mov', '.avi', '.mkv', '.webm', '.wmv', '.mpg', '.mpeg', '.m4v'}
    video_items = [
        item for item in onedrive_items if 
        item.get('file', {}).get('mimeType', '').startswith('video/') or
        (item.get('name', '').lower().endswith(tuple(video_extensions)))
    ]
    total_videos = len(video_items)
    logger.info(f"Found {total_videos} video files in OneDrive")
    print(f"🎬 Found {total_videos} video files to process\n")
    
    if total_videos == 0:
        print("❗ No video files found. Nothing to process.")
        return [], []
    
    # Results file
    results_file = 'video_links_results.json'
    
    # Process each video individually through its complete lifecycle
    processed_videos = []
    errors = []
    for index, item in enumerate(video_items):
        video_name = item.get('name', f"Video {index+1}")
        # Determine the expected note path
        item_path = item.get('parentReference', {}).get('path', '') + '/' + item['name']
        if ONEDRIVE_BASE in item_path:
            rel_path = item_path[item_path.find(ONEDRIVE_BASE) + len(ONEDRIVE_BASE):].lstrip('/')
        else:
            rel_path = item['name']
        local_path = RESOURCES_ROOT / rel_path.replace('/', os.path.sep)
        note_name = f"{local_path.stem}-video.md"
        try:
            rel_path_for_vault = os.path.relpath(local_path, RESOURCES_ROOT)
        except ValueError:
            rel_path_for_vault = Path(local_path.parts[-2]) / local_path.name
        vault_path = VAULT_ROOT / rel_path_for_vault
        vault_dir = vault_path.parent
        note_path = vault_dir / note_name
        # Check if note exists and --force is not set
        if note_path.exists() and not (args and getattr(args, 'force', False)):
            logger.info(f"Skipping {video_name}: note already exists and --force not set.")
            print(f"Skipping: {video_name} (note exists, use --force to overwrite)")
            errors.append({
                'file': rel_path,
                'note_path': str(note_path),
                'success': True,
                'skipped': True,
                'reason': 'already_exists',
                'modified_date': datetime.now().isoformat()
            })
            continue
        
        print(f"[{index+1}/{total_videos}] Processing: {video_name}")
            
            # Check for next page
            url = data.get('@odata.nextLink', None)
            
        return items

    def process_folder(folder_path):
        logger.info(f"Processing OneDrive folder: {folder_path}")
        items = fetch_folder_items(folder_path)
        
        for item in items:
            if item.get('file', None):
                # It's a file
                all_items.append(item)
            elif item.get('folder', None):
                # It's a folder, process recursively
                subfolder_path = f"{folder_path}/{item['name']}"
                process_folder(subfolder_path)
    
    # Start recursive processing
    process_folder(folder_path)
    return all_items

def process_single_video(item, headers, templates, results_file='video_links_results.json', args=None):
    """
    Process a single video file through its complete lifecycle.
    Improved with better session handling and error management.
    """
    # Create a session for all API requests
    session = create_requests_session(timeout=getattr(args, 'timeout', 15) if args else 15)
    
    try:
        # Extract video path information
        item_path = item.get('parentReference', {}).get('path', '') + '/' + item['name']
        
        # Extract the path relative to ONEDRIVE_BASE
        if ONEDRIVE_BASE in item_path:
            rel_path = item_path[item_path.find(ONEDRIVE_BASE) + len(ONEDRIVE_BASE):].lstrip('/')
        else:
            rel_path = item['name']
        
        video_name = item.get('name', 'Unknown Video')
        
        logger.info(f"Processing video: {rel_path}")
        print(f"Processing video: {video_name}")
        
        # Create local file path reference
        local_path = RESOURCES_ROOT / rel_path.replace('/', os.path.sep)
        
        # Calculate note path early to check if it exists
        video_name = local_path.stem
        note_name = f"{video_name}-video.md"
        
        # Determine the corresponding vault path
        try:
            rel_path_for_vault = os.path.relpath(local_path, RESOURCES_ROOT)
        except ValueError:
            # Handle case where paths are on different drives
            rel_path_for_vault = Path(local_path.parts[-2]) / local_path.name
            
        vault_path = VAULT_ROOT / rel_path_for_vault
        vault_dir = vault_path.parent
        note_path = vault_dir / note_name
        
        # Check if note exists and --force is not set
        if note_path.exists() and not (args and getattr(args, 'force', False)):
            logger.info(f"Skipping {video_name}: note already exists and --force not set.")
            print(f"Skipping: {video_name} (note exists, use --force to overwrite)")
            return {
                'file': rel_path,
                'note_path': str(note_path),
                'success': True,
                'skipped': True,
                'reason': 'already_exists',
                'modified_date': datetime.now().isoformat()
            }
        
        # Verify the file exists in OneDrive before proceeding
        access_token = headers['Authorization'].replace('Bearer ', '')
        file_data = check_if_file_exists(item_path, access_token, session)
        
        if not file_data and item.get('id'):
            logger.info(f"File not found by path, but we have an ID. Proceeding with share link creation.")
        elif not file_data:
            logger.error(f"File not found in OneDrive: {item_path}")
            print(f"  └─ Error: File not found in OneDrive")
            return {
                'file': rel_path,
                'success': False,
                'error': "File not found in OneDrive",
                'timestamp': datetime.now().isoformat()
            }
          # Step 1: Create a shareable link
        print("  ├─ Creating shareable link...")
        # Try creating share link with both path and id for better reliability
        share_link = None
        
        # First try using the item_path (more reliable but might not work for items just moved)
        if item_path and '/' in item_path:
            logger.info(f"Attempting to create share link using path: {item_path}")
            share_link = create_share_link(item_path, headers, session)
            if share_link:
                logger.info("Successfully created share link using path")
        
        # If path-based sharing failed, fall back to ID-based sharing
        if not share_link and item.get('id'):
            logger.info(f"Attempting to create share link using ID: {item.get('id')}")
            share_link = create_share_link(item['id'], headers, session)
            
        if not share_link:
            logger.error(f"Could not create share link for: {rel_path}")
            print("  │  └─ Failed to create shareable link!")
            
            # Add to failed files tracking
            update_failed_files(
                {'file': rel_path, 'path': item_path}, 
                "Failed to create share link", 
                ErrorCategories.PERMISSION
            )
            
            return {
                'file': rel_path,
                'success': False,
                'error': "Could not create share link",
                'timestamp': datetime.now().isoformat()
            }
        
        logger.info(f"Created shareable link: {share_link}")
        print("  │  └─ Link created successfully ✓")
        
        # Step 2: Create a markdown note in the vault
        print("  ├─ Creating markdown note...")
        note_path = create_markdown_note_for_video(local_path, share_link, VAULT_ROOT, item)
        logger.info(f"Created markdown note: {note_path}")
        print("  │  └─ Note created at: " + str(note_path).replace(str(VAULT_ROOT), "").lstrip('/\\'))
        
        # Create result record
        result = {
            'file': rel_path,
            'share_link': share_link,
            'note_path': str(note_path),
            'success': True,
            'modified_date': datetime.now().isoformat()
        }
        
        # Step 3: Update the results file with this individual result
        update_results_file(results_file, result)
        print("  └─ Results saved ✓")
        
        return result
    
    except Exception as e:
        error_msg = str(e)
        error_category = categorize_error(e)
        logger.error(f"Error processing {item.get('name', 'unknown')}: {error_msg} ({error_category})")
        print(f"  └─ Error: {error_msg}")
        
        # Add to failed files tracking
        update_failed_files(
            {'file': item.get('name', 'unknown'), 'path': item.get('parentReference', {}).get('path', '')}, 
            error_msg, 
            error_category
        )
        
        return {
            'file': item.get('name', 'unknown'),
            'success': False,
            'error': error_msg,
            'timestamp': datetime.now().isoformat()
        }

def update_failed_files(file_info, error, category=ErrorCategories.UNKNOWN):
    """
    Update the failed_files.json with information about a failed file.
    
    Args:
        file_info: Dictionary with file information
        error: Error message or exception
        category: Error category from ErrorCategories
    """
    failed_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'failed_files.json')
    failed_list = []
    
    # Try to load existing failed files
    if os.path.exists(failed_file):
        try:
            with open(failed_file, 'r') as f:
                failed_list = json.load(f)
        except Exception as e:
            logger.error(f"Error reading failed files: {e}")
            failed_list = []
    
    # Prepare the record
    failed_record = {
        'file': file_info.get('file') or file_info.get('name', 'Unknown'),
        'path': file_info.get('path') or file_info.get('parentReference', {}).get('path', 'Unknown'),
        'error': str(error),
        'category': category,
        'timestamp': datetime.now().isoformat(),
        'retried': False
    }
    
    # Check if this file is already in the list
    found = False
    for i, item in enumerate(failed_list):
        if item.get('file') == failed_record['file']:
            # Update existing record
            failed_list[i] = failed_record
            found = True
            break
    
    # Add new record if not found
    if not found:
        failed_list.append(failed_record)
    
    # Log to failed files logger
    failed_logger.error(f"Failed file: {failed_record['file']}, Error: {failed_record['error']}, Category: {failed_record['category']}")
    
    # Write back to file
    try:
        with open(failed_file, 'w') as f:
            json.dump(failed_list, f, indent=2)
    except Exception as e:
        logger.error(f"Error writing failed files: {e}")

def check_api_health(session, headers=None):
    """
    Check Microsoft Graph API health by making a series of test API requests.
    
    Args:
        session: The requests session to use
        headers: Authentication headers to include in requests
        
    Returns:
        tuple: (is_healthy, message)
    """
    # If no headers provided, we can only check basic connectivity
    if not headers:
        url = "https://graph.microsoft.com/v1.0/"
        try:
            resp = session.get(url, timeout=5)
            if resp.status_code == 200:
                logger.info("Basic API connectivity check passed")
                return True, "API connectivity is healthy"
            else:
                logger.warning(f"API connectivity check returned status code {resp.status_code}")
                return False, f"API returned status code {resp.status_code}"
        except Exception as e:
            logger.error(f"API connectivity check failed: {e}")
            return False, f"API connectivity error: {str(e)}"
    
    # With headers, we can perform more comprehensive checks
    # 1. Check user profile access
    try:
        url = f"{GRAPH_API_ENDPOINT}/me"
        resp = session.get(url, headers=headers, timeout=5)
        if resp.status_code != 200:
            logger.warning(f"API user profile check failed with status code {resp.status_code}")
            return False, f"User profile access failed with status code {resp.status_code}"
        
        # Get the user's display name for the health message
        display_name = resp.json().get('displayName', 'Unknown User')
        
        # 2. Check OneDrive drive access
        url = f"{GRAPH_API_ENDPOINT}/me/drive"
        resp = session.get(url, headers=headers, timeout=5)
        if resp.status_code != 200:
            logger.warning(f"API drive check failed with status code {resp.status_code}")
            return False, f"OneDrive access failed with status code {resp.status_code}"
        
        # 3. Check OneDrive quota info
        quota = resp.json().get('quota', {})
        total = quota.get('total', 0)
        used = quota.get('used', 0)

        if total > 0:
            used_percent = (used / total) * 100
            if used_percent > 90:
                logger.warning(f"OneDrive is nearly full ({used_percent:.1f}% used)")
                return True, f"API healthy. Connected as {display_name}. OneDrive is {used_percent:.1f}% full (WARNING: low space)"
        
        # All checks passed
        logger.info("API health check passed successfully")
        return True, f"API healthy. Connected as {display_name}."
        
    except Exception as e:
        error_category = categorize_error(e)
        logger.error(f"API health check failed: {e} ({error_category})")
        return False, f"API health check error: {str(e)}"

def update_results_file(results_file, new_result):
    """Update the results JSON file with a new processed video result."""
    # Ensure the file exists
    if not os.path.exists(results_file):
        with open(results_file, 'w') as f:
            json.dump({
                'processed_videos': [],
                'errors': [],
                'timestamp': datetime.now().isoformat()
            }, f, indent=2)
    
    try:
        # Read existing results
        with open(results_file, 'r') as f:
            existing_results = json.load(f)
        
        # Find if this file was previously processed
        file_path = new_result.get('file')
        
        if 'processed_videos' not in existing_results:
            existing_results['processed_videos'] = []
        
        # Check if this file exists in the processed videos
        found = False
        for i, video in enumerate(existing_results['processed_videos']):
            if video.get('file') == file_path:
                # Update existing entry
                existing_results['processed_videos'][i] = new_result
                found = True
                break
        
        # If not found, add it
        if not found:
            existing_results['processed_videos'].append(new_result)
        
        # If there was an error, add it to the errors list
        if not new_result.get('success', True):
            if 'errors' not in existing_results:
                existing_results['errors'] = []
            
            existing_results['errors'].append({
                'file': file_path,
                'error': new_result.get('error', 'Unknown error'),
                'timestamp': datetime.now().isoformat()
            })
        
        # Update last run timestamp
        existing_results['last_run'] = datetime.now().isoformat()
        
        # Write updated results back to file
        with open(results_file, 'w') as f:
            json.dump(existing_results, f, indent=2)
        
    except Exception as e:
        logger.error(f"Error updating results file: {e}")

def process_videos_for_sharing(args=None):
    """
    Main function to find videos in OneDrive, generate links, and create notes.
    Uses improved authentication and session handling.
    """
    # Load templates first
    templates = load_templates()
    if not templates:
        logger.warning("Could not load templates from metadata.yaml. Using default templates.")
    else:
        logger.info(f"Successfully loaded {len(templates)} templates from metadata.yaml")
    
    # Authenticate using the enhanced authentication function 
    print("\n🔑 Authenticating with Microsoft Graph API...")
    try:
        # Check if we should force refresh the authentication
        force_refresh = args and getattr(args, 'refresh_auth', False)
        
        access_token = authenticate_graph_api(force_refresh=force_refresh)
        headers = {"Authorization": f"Bearer {access_token}"}
        logger.info("Authenticated to Microsoft Graph API")
        print("✅ Authentication successful\n")
        
        # Create a session for consistent API access
        session = create_requests_session(timeout=getattr(args, 'timeout', 15) if args else 15)
        
        # Perform health check to verify API access
        is_healthy, health_message = check_api_health(session, headers)
        if not is_healthy:
            logger.warning(f"API health check warning: {health_message}")
            print(f"⚠️ API health check warning: {health_message}")
            # Continue anyway as it might still work
    except Exception as e:
        error_category = categorize_error(e)
        logger.error(f"Authentication failed: {e} ({error_category})")
        print(f"\n❌ Authentication failed: {e}\n")
        return [], [f"Authentication error: {str(e)}"]
      # Get all items from OneDrive
    print("📂 Fetching files from OneDrive...")
    try:
        onedrive_items = get_onedrive_items(access_token, ONEDRIVE_BASE)
        logger.info(f"Found {len(onedrive_items)} items in OneDrive")
        print(f"✅ Found {len(onedrive_items)} files in OneDrive\n")
        
        # Debug: Print some sample items to check what's being returned
        if len(onedrive_items) > 0:
            logger.debug(f"First 5 items from OneDrive:")
            for i, item in enumerate(onedrive_items[:5]):
                logger.debug(f"  Item {i+1}: {item.get('name')} - Type: {item.get('file', {}).get('mimeType', 'folder')}") 
        else:
            logger.warning("No items found in OneDrive - check path configuration")
    except Exception as e:
        logger.error(f"Failed to fetch OneDrive items: {e}")
        print(f"❌ Failed to fetch files from OneDrive: {e}\n")
        return [], [f"OneDrive API error: {str(e)}"]
      # Filter for video files
    video_extensions = {'.mp4', '.mov', '.avi', '.mkv', '.webm', '.wmv', '.mpg', '.mpeg', '.m4v'}
    video_items = [
        item for item in onedrive_items if 
        item.get('file', {}).get('mimeType', '').startswith('video/') or
        (item.get('name', '').lower().endswith(tuple(video_extensions)))
    ]
    total_videos = len(video_items)
    logger.info(f"Found {total_videos} video files in OneDrive")
    print(f"🎬 Found {total_videos} video files to process\n")
    
    if total_videos == 0:
        print("❗ No video files found. Nothing to process.")
        return [], []
    
    # Results file
    results_file = 'video_links_results.json'
    
    # Process each video individually through its complete lifecycle
    processed_videos = []
    errors = []
    for index, item in enumerate(video_items):
        video_name = item.get('name', f"Video {index+1}")
        # Determine the expected note path
        item_path = item.get('parentReference', {}).get('path', '') + '/' + item['name']
        if ONEDRIVE_BASE in item_path:
            rel_path = item_path[item_path.find(ONEDRIVE_BASE) + len(ONEDRIVE_BASE):].lstrip('/')
        else:
            rel_path = item['name']
        local_path = RESOURCES_ROOT / rel_path.replace('/', os.path.sep)
        note_name = f"{local_path.stem}-video.md"
        try:
            rel_path_for_vault = os.path.relpath(local_path, RESOURCES_ROOT)
        except ValueError:
            rel_path_for_vault = Path(local_path.parts[-2]) / local_path.name
        vault_path = VAULT_ROOT / rel_path_for_vault
        vault_dir = vault_path.parent
        note_path = vault_dir / note_name
        # Check if note exists and --force is not set
        if note_path.exists() and not (args and getattr(args, 'force', False)):
            logger.info(f"Skipping {video_name}: note already exists and --force not set.")
            print(f"Skipping: {video_name} (note exists, use --force to overwrite)")
            errors.append({
                'file': rel_path,
                'note_path': str(note_path),
                'success': True,
                'skipped': True,
                'reason': 'already_exists',
                'modified_date': datetime.now().isoformat()
            })
            continue
        
        print(f"[{index+1}/{total_videos}] Processing: {video_name}")
        # Process this video through its full lifecycle - pass args to respect --force flag
        result = process_single_video(item, headers, templates, results_file, args)
        
        if result.get('success', False):
            processed_videos.append(result)
        else:
            errors.append(result)
        
        print("") # Add blank line between videos
    
    # Load the final results to get accurate counts
    try:
        with open(results_file, 'r') as f:
            final_results = json.load(f)
            all_videos_count = len(final_results.get('processed_videos', []))
    except Exception:
        all_videos_count = len(processed_videos)
    
    # Print summary
    print("\n" + "="*60)
    print(f"📊 SUMMARY: Video Link Generation")
    print("="*60)
    print(f"✅ Processed in this run: {len(processed_videos)} videos")
    print(f"❌ Errors in this run: {len(errors)}")
    print(f"📝 Total videos with links: {all_videos_count}")
    print(f"📄 Full results saved to: {results_file}")
    print("="*60 + "\n")
    
    logger.info("\n===== Summary =====")
    logger.info(f"Processed {len(processed_videos)} videos")
    logger.info(f"Total videos with links: {all_videos_count}")
    if errors:
        logger.info(f"Encountered {len(errors)} errors:")
        for error in errors:
            logger.info(f"  - {error.get('file')}: {error.get('error')}")

    return processed_videos, errors

def find_transcript_file(video_path, vault_root):
    """
    Find a transcript file that corresponds to a video file.
    
    This function searches for transcript files in multiple locations:
    1. In the vault directory corresponding to the video
    2. In the parent directory of that vault directory
    3. In the OneDrive directory where the original video is located
    4. In commonly used transcript subdirectories
    
    The function also supports multiple naming patterns for transcript files.
    
    Args:
        video_path: Path to the video file
        vault_root: Root directory of the Obsidian vault
        
    Returns:
        Path object for the transcript file if found, None otherwise
    """
    if isinstance(video_path, str):
        video_path = Path(video_path)
    
    # Print video path info for debugging
    logger.info(f"Searching for transcript file for video: {video_path}")
    logger.info(f"Video parent directory: {video_path.parent}")
    
    # First check: Direct exact match in same directory (same name, different extension)
    direct_txt_match = video_path.with_suffix('.txt')
    if direct_txt_match.exists():
        logger.info(f"Found exact matching transcript file: {direct_txt_match}")
        return direct_txt_match
    
    # Generate possible transcript file names with a wider range of patterns
    video_name = video_path.stem
    
    # Log the base video name
    logger.info(f"Video base name: {video_name}")
    
    # Add more patterns to make transcript detection more robust
    transcript_name_patterns = [
        # Standard formats - most common first
        f"{video_name}.txt",
        f"{video_name}.md",
        # Additional formats
        f"{video_name} Transcript.md",
        f"{video_name}-Transcript.md",
        f"{video_name} transcript.md",
        f"{video_name}-transcript.md",
        f"{video_name}_Transcript.md",
        f"{video_name}_transcript.md",
        
        # Common variations
        f"{video_name} Transcript.txt",
        f"{video_name}-Transcript.txt",
        f"{video_name} transcript.txt",
        f"{video_name}-transcript.txt",
        f"{video_name}_Transcript.txt",
        f"{video_name}_transcript.txt",
        
        # Special case for files with -Live-Session in name
        video_name.replace('-Live-Session', ' Live Session Transcript') + ".md",
        video_name.replace('-Live-Session', ' Live Session Transcript') + ".txt",
        
        # Files that might have different naming conventions
        f"{video_name} - Transcript.md",
        f"{video_name} - Transcript.txt"
    ]
    
    logger.debug(f"Looking for transcript for video: {video_name}")
    logger.debug(f"Searching with patterns: {', '.join(transcript_name_patterns[:5])}...")
    
    # 1. First, check if transcript exists in the same directory as where the video reference would be in the vault
    try:
        rel_path = os.path.relpath(video_path, RESOURCES_ROOT)
        vault_dir = vault_root / Path(os.path.dirname(rel_path))
        logger.debug(f"Searching for transcript in vault directory: {vault_dir}")
        
        # Check each possible transcript filename pattern
        for pattern in transcript_name_patterns:
            transcript_path = vault_dir / pattern
            if transcript_path.exists():
                logger.info(f"Found transcript file in vault directory: {transcript_path}")
                return transcript_path
    except ValueError:
        # Handle case where paths are on different drives
        logger.debug("Path error when getting relative path to vault, using fallback approach")
        rel_path = Path(video_path.name)
        vault_dir = vault_root
    
    # 2. Check parent directory in vault
    parent_dir = vault_dir.parent
    if parent_dir.exists():
        logger.debug(f"Searching parent directory: {parent_dir}")
        for pattern in transcript_name_patterns:
            transcript_path = parent_dir / pattern
            if transcript_path.exists():
                logger.info(f"Found transcript file in parent directory: {transcript_path}")
                return transcript_path
      # 3. Check in the OneDrive source directory where the video is located
    try:
        onedrive_dir = video_path.parent
        if onedrive_dir.exists():
            logger.debug(f"Searching OneDrive source directory: {onedrive_dir}")
            
            # First list all txt files in the directory for debugging
            txt_files = list(onedrive_dir.glob("*.txt"))
            logger.info(f"Available TXT files in directory ({len(txt_files)}): {', '.join([f.name for f in txt_files])}")
            
            # Check each pattern systematically
            for pattern in transcript_name_patterns:
                transcript_path = onedrive_dir / pattern
                logger.debug(f"Checking for: {transcript_path}")
                if transcript_path.exists():
                    logger.info(f"Found transcript file in OneDrive directory: {transcript_path}")
                    return transcript_path
            
            # If no match found with standard patterns, try a more direct approach
            # If there's exactly one .txt file in the folder, it's likely the transcript
            if len(txt_files) == 1:
                logger.info(f"Found single TXT file in directory, using as transcript: {txt_files[0]}")
                return txt_files[0]
    except Exception as e:
        logger.debug(f"Error checking OneDrive directory: {e}")
    
    # 4. Check in common transcript subdirectories
    common_transcript_dirs = ["Transcripts", "Transcript", "transcripts", "transcript"]
    
    # Check in vault directory's potential transcript subdirectories
    for subdir in common_transcript_dirs:
        transcript_dir = vault_dir / subdir
        if transcript_dir.exists():
            logger.debug(f"Searching transcript subdirectory: {transcript_dir}")
            for pattern in transcript_name_patterns:
                transcript_path = transcript_dir / pattern
                if transcript_path.exists():
                    logger.info(f"Found transcript file in transcript subdirectory: {transcript_path}")
                    return transcript_path
    
    # Check in OneDrive directory's potential transcript subdirectories
    try:
        for subdir in common_transcript_dirs:
            transcript_dir = video_path.parent / subdir
            if transcript_dir.exists():
                logger.debug(f"Searching OneDrive transcript subdirectory: {transcript_dir}")
                for pattern in transcript_name_patterns:
                    transcript_path = transcript_dir / pattern
                    if transcript_path.exists():
                        logger.info(f"Found transcript file in OneDrive transcript subdirectory: {transcript_path}")
                        return transcript_path
    except Exception as e:
        logger.debug(f"Error checking OneDrive transcript directories: {e}")
      # 5. More sophisticated partial name matching 
    try:
        onedrive_dir = video_path.parent
        if onedrive_dir.exists():
            logger.debug(f"Trying advanced partial name matches in: {onedrive_dir}")
            
            # Get all txt files in the directory
            txt_files = list(onedrive_dir.glob("*.txt"))
            
            # Try different matching strategies
            matched_file = None
            
            # Strategy 1: Direct match without special characters
            clean_video_name = re.sub(r'[^a-zA-Z0-9]', '', video_name.lower())
            for file in txt_files:
                clean_file_name = re.sub(r'[^a-zA-Z0-9]', '', file.stem.lower())
                if clean_video_name == clean_file_name:
                    logger.info(f"Found transcript via normalized name match: {file}")
                    return file
            
            # Strategy 2: Substring match
            for file in txt_files:
                # If the file name contains the video name or vice versa
                if video_name.lower() in file.stem.lower() or file.stem.lower() in video_name.lower():
                    logger.info(f"Found potential transcript via substring match: {file}")
                    matched_file = file
                    break
            
            # Strategy 3: Check if there are numeric suffixes
            if not matched_file:
                # Extract base name without numeric suffix
                base_name_match = re.match(r'(.+?)[-_\s]*\d+$', video_name.lower())
                if base_name_match:
                    base_name = base_name_match.group(1)
                    logger.debug(f"Extracted base name without numeric suffix: {base_name}")
                    
                    for file in txt_files:
                        if base_name in file.stem.lower():
                            logger.info(f"Found potential transcript via base name match: {file}")
                            matched_file = file
                            break
            
            # Return the matched file if found
            if matched_file:
                return matched_file
            
    except Exception as e:
        logger.debug(f"Error during advanced partial name matching: {e}")
    
    # 6. Final attempt - if all else fails and there's just one text file, use it
    try:
        onedrive_dir = video_path.parent
        if onedrive_dir.exists():
            txt_files = list(onedrive_dir.glob("*.txt"))
            if len(txt_files) == 1:
                logger.info(f"Last resort: Using the only text file in directory: {txt_files[0]}")
                return txt_files[0]
    except Exception as e:
        logger.debug(f"Error in final attempt: {e}")
    
    # If no transcript file found after all these attempts
    logger.info(f"No transcript file found for {video_name} after exhaustive search")
    return None

def get_transcript_content(transcript_path):
    """
    Extract and clean content from a transcript file for optimal summarization.
    
    Handles various transcript formats:
    - Markdown files with frontmatter
    - Plain text files
    - Files with timestamps and speaker annotations
    - Auto-generated transcripts with repetitions and formatting issues
    
    Args:
        transcript_path: Path to the transcript file
        
    Returns:
        str: Clean transcript text content if successful, None otherwise
    """
    try:
        # Try different encodings in case of encoding issues
        encodings_to_try = ['utf-8', 'latin-1', 'windows-1252']
        content = None
        
        for encoding in encodings_to_try:
            try:
                with open(transcript_path, 'r', encoding=encoding) as f:
                    content = f.read()
                logger.debug(f"Successfully read transcript with {encoding} encoding")
                break
            except UnicodeDecodeError:
                logger.debug(f"Failed to read with {encoding} encoding, trying next encoding")
                continue
            except Exception as e:
                logger.error(f"Error reading transcript file with {encoding}: {e}")
                raise
        
        if not content:
            logger.error(f"Could not read transcript file with any encoding: {transcript_path}")
            return None
            
        # Log original content size
        original_size = len(content)
        logger.debug(f"Original transcript content size: {original_size} characters")
        
        # STEP 1: Remove structural elements
        
        # Remove YAML frontmatter if it exists
        content = re.sub(r'^---\s.*?---\s', '', content, flags=re.DOTALL)
        
        # Remove common transcript headers/footers
        content = re.sub(r'^(Transcript|TRANSCRIPT|Video Transcript|AUTO-GENERATED TRANSCRIPT).*?\n+', '', content, flags=re.IGNORECASE)
        content = re.sub(r'\n+(End of|END OF|End)\s+(Transcript|TRANSCRIPT|Video).*?$', '', content, flags=re.IGNORECASE)
        
        # STEP 2: Clean up timestamps and speaker annotations
        
        # Remove common timestamp patterns (e.g., [00:01:23] or 00:01:23 -> or (00:01:23))
        content = re.sub(r'\[?\d{1,2}:\d{2}(:\d{2})?\]?(\s*->)?\s*', '', content)
        content = re.sub(r'\(\d{1,2}:\d{2}(:\d{2})?\)(\s*->)?\s*', '', content)
        content = re.sub(r'^(\d{1,2}:\d{2}(:\d{2})?)(\s|$)', '', content, flags=re.MULTILINE)  # Timestamps at line start
        
        # Remove speaker annotations (e.g., "Speaker 1:" or "John:")
        content = re.sub(r'^\s*(Speaker\s*\d+|Instructor|Student|Moderator|Interviewer|Interviewee|Host|Guest|Professor):\s*', '', content, flags=re.MULTILINE)
          # Also remove names followed by colons at the beginning of lines (more generic speaker labels)
        content = re.sub(r'^\s*[A-Z][a-zA-Z\s\.\-]*:\s*', '', content, flags=re.MULTILINE)
        
        # STEP 3: Clean up markdown and formatting
        
        # Remove any markdown headings and formatting
        content = re.sub(r'#+\s.*?\n', '', content)
        content = re.sub(r'\*\*(.*?)\*\*', r'\1', content)
        content = re.sub(r'\*(.*?)\*', r'\1', content)
        content = re.sub(r'_{1,2}(.*?)_{1,2}', r'\1', content)  # Underscores for emphasis
        
        # STEP 4: Handle auto-generated transcript issues
        
        # Fix common transcript errors
        content = re.sub(r'(?:um|uh|er|ah|like)\s', ' ', content, flags=re.IGNORECASE)  # Remove filler words
        
        # Remove lines that are likely transcript errors or non-informative
        lines = content.split('\n')
        filtered_lines = []
        for line in lines:
            # Skip very short lines (often just noise in transcripts)
            if len(line.strip()) < 3:
                continue
            # Skip lines that are just punctuation or single words (often errors)
            if len(line.strip().split()) < 2 and not re.search(r'[a-zA-Z]{3,}', line):
                continue
            filtered_lines.append(line)
        
        content = '\n'.join(filtered_lines)
        
        # STEP 5: Final cleanup and whitespace handling
        
        # Fix spacing issues
        content = re.sub(r'\s{2,}', ' ', content)  # Multiple spaces to single space
        content = re.sub(r'\n{3,}', '\n\n', content)  # Multiple newlines to double newline
        
        # Fix common punctuation issues
        content = re.sub(r'([.!?])\s*([a-zA-Z])', r'\1 \2', content)  # Ensure space after sentence ending
        content = re.sub(r'\s([.,;:!?])', r'\1', content)  # Remove space before punctuation
        
        # Log cleaned content size for comparison
        cleaned_content = content.strip()
        cleaned_size = len(cleaned_content)
        logger.info(f"Cleaned transcript: {cleaned_size} characters ({cleaned_size/original_size:.1%} of original)")
        
        # If the content is extremely short after cleaning, it might indicate a problematic transcript
        if cleaned_size < original_size * 0.3 and original_size > 1000:
            logger.warning(f"Cleaning removed over 70% of transcript content. Check transcript quality.")
        
        return cleaned_content
    except Exception as e:
        logger.error(f"Error processing transcript file {transcript_path}: {e}")
        return None

def generate_tags_with_openai(transcript_text, video_name="", course_name="", program_name=""):
    """
    Generate relevant tags for the video based on its content using OpenAI.
    
    For long transcripts, extracts key portions for better tag generation instead
    of truncating arbitrarily.
    
    Args:
        transcript_text (str): The transcript text
        video_name (str): Name of the video
        course_name (str): Name of the course
        program_name (str): Name of the program
        
    Returns:
        list: Generated tags
    """
    if not OPENAI_API_KEY:
        logger.warning("OpenAI API key not set. Cannot generate tags.")
        return ['video', 'onedrive', 'course-materials']
    
    if not transcript_text or len(transcript_text) < 100:
        logger.warning("Transcript text too short or empty. Using default tags.")
        return ['video', 'onedrive', 'course-materials']
    
    try:
        # For lengthy transcripts, use a smarter approach than just truncating
        max_tokens = 6000  # More context for better tag generation
        if len(transcript_text) > max_tokens * 4:  # If transcript is very large
            logger.info(f"Transcript too large for tag generation ({len(transcript_text)} chars), extracting key portions")
            
            # Extract key portions of the text for better tag generation instead of simple truncation
            # 1. Get the beginning (first 20%)
            beginning_size = int(max_tokens * 1.5)
            beginning = transcript_text[:beginning_size]
            
            # 2. Get the end (last 20%)
            end_size = int(max_tokens * 1)
            end = transcript_text[-end_size:] if len(transcript_text) > end_size else ""
            
            # 3. Extract some portions from the middle (30%)
            middle_size = int(max_tokens * 1.5)
            middle_start = (len(transcript_text) - middle_size) // 2
            middle = transcript_text[middle_start:middle_start+middle_size] if len(transcript_text) > middle_start + middle_size else ""
            
            # Combine with clear markers
            extracted_text = f"{beginning}\n\n[...MIDDLE PORTION OF TRANSCRIPT...]\n\n{middle}\n\n[...REMAINING PORTION OF TRANSCRIPT...]\n\n{end}"
            transcript_text = extracted_text
            logger.info(f"Extracted key portions for tag generation: {len(transcript_text)} chars")
        else:
            logger.info(f"Using full transcript for tag generation: {len(transcript_text)} chars")        # Create a prompt focused on tag generation
        system_prompt = """You are an expert at categorizing MBA educational content. 
        Generate 5-10 relevant tags for the transcript provided. 
        
        Follow these guidelines:
        - Include key subject areas (e.g., finance, accounting, marketing)
        - Include specific topics within those areas (e.g., valuation, costing, brand-management)
        - Include relevant methodologies or frameworks mentioned (e.g., SWOT, DCF, porter-five-forces)
        - Include cognitive tags that describe the thinking or analytical process (e.g., critical-thinking, quantitative-analysis)
        - Use kebab-case for multi-word tags (e.g., 'cash-flow' not 'cash flow')
        - Do not include generic tags like 'MBA', 'video', 'course'
        - Be specific to the actual content
        
        Return ONLY a JSON array of string tags without explanation or commentary.
        Example: ["accounting", "fixed-costs", "profit-margin", "decision-making", "cost-allocation", "quantitative-analysis"]
        """
          # Include metadata in the prompt to improve tag relevance
        context = ""
        if course_name:
            context += f"Course: {course_name}\n"
        if program_name:
            context += f"Program: {program_name}\n"
        if video_name:
            context += f"Video Title: {video_name}\n"
            
        user_prompt = f"{context}\nGenerate tags for this educational video based on the following transcript:\n\n{transcript_text}"
        
        # Try with GPT-4.1 first for better tag generation
        try:
            response = openai.chat.completions.create(
                model="gpt-4.1",  # Using GPT-4.1 for precise tag generation
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt}
                ],
                max_tokens=250,  # Tags should be concise
                temperature=0.3  # Lower temperature for more consistent results
            )
            
            # Parse the response as JSON array
            tags_text = response.choices[0].message.content.strip()
            logger.debug(f"Raw tag response: {tags_text}")
        except Exception as e:
            # If GPT-4.1 fails, fall back to another model
            logger.warning(f"Error using GPT-4.1 for tag generation: {e}. Falling back to GPT-3.5-turbo.")
            try:
                response = openai.chat.completions.create(
                    model="gpt-3.5-turbo",  # Fallback model
                    messages=[
                        {"role": "system", "content": system_prompt},
                        {"role": "user", "content": user_prompt}
                    ],
                    max_tokens=250,
                    temperature=0.3
                )
                tags_text = response.choices[0].message.content.strip()
                logger.debug(f"Raw tag response from fallback model: {tags_text}")
            except Exception as inner_e:
                logger.error(f"Error in fallback tag generation: {inner_e}")
                return ['video', 'onedrive', 'course-materials']
        
        # Clean up and parse the response
        try:
            # Clean up the response in case there's any explanation text
            if '[' in tags_text and ']' in tags_text:
                tags_json = tags_text[tags_text.find('['):tags_text.rfind(']')+1]
                custom_tags = json.loads(tags_json)
                
                # Clean up tags - ensure they are proper kebab-case and no duplicates
                cleaned_tags = []
                for tag in custom_tags:
                    # Convert to lowercase and strip any whitespace
                    clean_tag = tag.lower().strip()
                    
                    # Replace spaces with hyphens and remove any other non-alphanumeric chars
                    clean_tag = re.sub(r'\s+', '-', clean_tag)
                    clean_tag = re.sub(r'[^\w\-]', '', clean_tag)
                    
                    # Add only if it's a valid tag (not empty, not too long)
                    if clean_tag and len(clean_tag) < 50:
                        cleaned_tags.append(clean_tag)
                
                # Remove duplicates while preserving order
                seen = set()
                unique_tags = [x for x in cleaned_tags if not (x in seen or seen.add(x))]
                
                logger.info(f"Successfully generated {len(unique_tags)} custom tags: {', '.join(unique_tags)}")
                
                # Add course-specific tag if course name is provided
                if course_name:
                    course_tag = course_name.lower().replace(' ', '-').replace('_', '-')
                    course_tag = re.sub(r'[^\w\-]', '', course_tag)
                    if course_tag and course_tag not in unique_tags:
                        unique_tags.append(course_tag)
                
                # Always include the default tags
                default_tags = ['video', 'onedrive', 'course-materials']
                for tag in default_tags:
                    if tag not in unique_tags:
                        unique_tags.append(tag)
                
                return unique_tags
            else:
                logger.warning("No valid JSON array found in tag response. Using default tags.")
                return ['video', 'onedrive', 'course-materials']
        except json.JSONDecodeError as e:
            logger.warning(f"Could not parse tags JSON ({e}). Using default tags.")
            return ['video', 'onedrive', 'course-materials']
            
    except Exception as e:
        logger.error(f"Error generating tags with OpenAI: {e}")
        return ['video', 'onedrive', 'course-materials']

def chunk_text(text, max_chunk_size=8000, overlap=500):
    """
    Split a long text into overlapping chunks for processing.
    
    Args:
        text (str): The text to split
        max_chunk_size (int): Maximum characters per chunk
        overlap (int): Number of characters to overlap between chunks
        
    Returns:
        list: List of text chunks
    """
    if len(text) <= max_chunk_size:
        return [text]
        
    chunks = []
    start = 0
    
    while start < len(text):
        # Define the end of this chunk
        end = min(start + max_chunk_size, len(text))
        
        # If we're not at the end, find a good breakpoint (newline or period)
        if end < len(text) and end - start == max_chunk_size:
            # Look for a newline or period in the last 100 chars of the chunk
            breakpoint_search_area = text[end-100:end]
            
            # Try to find a newline first
            newline_pos = breakpoint_search_area.rfind('\n')
            if newline_pos != -1:
                end = end - (100 - newline_pos)
            else:
                # If no newline, try to find a period followed by space
                period_pos = breakpoint_search_area.rfind('. ')
                if period_pos != -1:
                    end = end - (100 - period_pos - 2)  # -2 to include the period and space
        
        # Extract the chunk
        chunk = text[start:end]
        chunks.append(chunk)
        
        # Move the start position for the next chunk, with overlap
        start = end - overlap
        
        # Ensure we're making progress even if no good breakpoint was found
        if start < end - overlap:
            start = end - overlap
            
    logger.info(f"Split text into {len(chunks)} chunks (avg {len(text)/len(chunks):.0f} chars per chunk)")
    return chunks

def summarize_chunk(chunk, video_name, course_name, is_first_chunk=False, is_final_chunk=False, chunk_num=1, total_chunks=1):
    """
    Summarize a single chunk of transcript text using OpenAI API.
    
    Args:
        chunk (str): The transcript text chunk
        video_name (str): Name of the video
        course_name (str): Name of the course
        is_first_chunk (bool): Whether this is the first chunk
        is_final_chunk (bool): Whether this is the final chunk
        chunk_num (int): Current chunk number
        total_chunks (int): Total number of chunks
        
    Returns:
        str: The generated summary
    """
    # Create a prompt that indicates whether this is part of a larger transcript
    chunk_context = ""
    if total_chunks > 1:
        chunk_context = f"This is part {chunk_num} of {total_chunks} from the transcript. "
        
        if is_first_chunk:
            chunk_context += "This is the beginning of the transcript. "
        elif is_final_chunk:
            chunk_context += "This is the end of the transcript. "
        else:
            chunk_context += "This is a middle section of the transcript. "

    # Create a more focused prompt for chunk processing
    system_prompt = f"""You are an educational content summarizer for MBA course materials.
    {chunk_context}Analyze this transcript chunk and extract the key information.
    
    For each chunk, identify:
    1. Main topics discussed
    2. Key concepts explained
    3. Important takeaways or insights
    4. Notable quotes
    5. Questions that might arise from this content
    
    Format your response as simple bullet points under each category.
    Keep your response concise but informative.
    """
    
    user_prompt = f"This is {chunk_context.lower()}from a video titled '{video_name}' from the course '{course_name}':\n\n{chunk}"
    
    try:
        response = openai.chat.completions.create(
            model="gpt-4.1",
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt}
            ],
            max_tokens=1000,
            temperature=0.3
        )
        chunk_summary = response.choices[0].message.content
        logger.debug(f"Generated chunk summary ({chunk_num}/{total_chunks}): {len(chunk_summary)} chars")
        return chunk_summary
    except Exception as e:
        logger.error(f"Error summarizing chunk {chunk_num}: {e}")
        return f"Error processing chunk {chunk_num}: {str(e)}"

def generate_summary_with_openai(transcript_text, video_name="", course_name=""):
    """
    Generate a comprehensive summary of the transcript using OpenAI API.
    
    For long transcripts, text is split into chunks, each chunk is summarized 
    separately, and then a final consolidated summary is generated from all 
    chunk summaries.
    
    The summary includes:
    - Topics Covered
    - Key Concepts Explained
    - Important Takeaways
    - Summary
    - Notable Quotes/Insights
    - Questions for reflection
    """
    if not OPENAI_API_KEY:
        logger.warning("OpenAI API key not set. Cannot generate summary.")
        return None
    
    if not transcript_text or len(transcript_text) < 100:
        logger.warning("Transcript text too short or empty. Cannot generate summary.")
        return None
    
    try:
        # Log original text size
        logger.info(f"Processing transcript of {len(transcript_text)} characters")
        
        # Define chunk size based on GPT model limits
        # GPT-4.1 has higher token limits, but we still need to be conservative
        max_chunk_chars = 8000  # ~2000 tokens
        
        # Determine if we need to chunk the text
        if len(transcript_text) > max_chunk_chars * 1.5:
            logger.info(f"Transcript too large ({len(transcript_text)} chars), splitting into chunks")
            chunks = chunk_text(transcript_text, max_chunk_chars)
            total_chunks = len(chunks)
            
            # Process each chunk
            chunk_summaries = []
            for i, chunk in enumerate(chunks):
                logger.info(f"Processing chunk {i+1}/{total_chunks} ({len(chunk)} chars)")
                is_first = (i == 0)
                is_last = (i == total_chunks - 1)
                chunk_summary = summarize_chunk(
                    chunk, 
                    video_name, 
                    course_name, 
                    is_first_chunk=is_first,
                    is_final_chunk=is_last,
                    chunk_num=i+1, 
                    total_chunks=total_chunks
                )
                chunk_summaries.append(chunk_summary)
                
            # Combine the chunk summaries into a single document for final summarization
            combined_summary = "\n\n".join([
                f"--- CHUNK {i+1}/{total_chunks} SUMMARY ---\n{summary}" 
                for i, summary in enumerate(chunk_summaries)
            ])
            
            logger.info(f"Generated {len(chunk_summaries)} chunk summaries, now creating final summary")
            
            # Create the final consolidated summary from all chunk summaries
            system_prompt = """You are an educational content summarizer for MBA course materials. 
            Create a comprehensive final summary based on the provided chunk summaries.
            
            Structure your response in markdown format with these exact sections:
            
            # 🎓 Educational Video Summary (AI Generated)
            
            ## 🧩 Topics Covered
            - List 3-5 main topics covered in the video
            - Be specific and use bullet points
            
            ## 📝 Key Concepts Explained
            - Explain the key concepts in 3-5 paragraphs
            - Focus on the most important ideas
            
            ## ⭐ Important Takeaways
            - List 3-5 important takeaways as bullet points
            - Focus on practical applications and insights
            
            ## 🧠 Summary
            - Write a concise 1-paragraph summary of the overall video content
            
            ## 💬 Notable Quotes / Insights
            - Include 1-2 significant quotes or key insights from the video
            - Format as proper markdown blockquotes using '>' symbol
            
            ## ❓ Questions
            - What did I learn from this video?
            - What's still unclear or needs further exploration?
            - How does this material relate to the broader course or MBA program?
            """
            
            user_prompt = f"These are the chunk summaries from a video titled '{video_name}' from the course '{course_name}'. Create a comprehensive final summary following the format in your instructions:\n\n{combined_summary}"
            
            response = openai.chat.completions.create(
                model="gpt-4.1",  # Using GPT-4.1 for best quality summaries
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt}
                ],
                max_tokens=2000,  # Increased token limit for comprehensive summaries
                temperature=0.5
            )
            
            summary = response.choices[0].message.content
            logger.info("Successfully generated consolidated summary from all chunks with OpenAI")
            return summary
        else:
            # For shorter transcripts, process directly without chunking
            logger.info("Transcript length is appropriate for direct processing")
            
            # Create a comprehensive prompt for better summaries
            system_prompt = """You are an educational content summarizer for MBA course materials. 
            Create a comprehensive summary of the following transcript from an educational video.
            
            Structure your response in markdown format with these exact sections:
            
            # 🎓 Educational Video Summary (AI Generated)
            
            ## 🧩 Topics Covered
            - List 3-5 main topics covered in the video
            - Be specific and use bullet points
            
            ## 📝 Key Concepts Explained
            - Explain the key concepts in 3-5 paragraphs
            - Focus on the most important ideas
            
            ## ⭐ Important Takeaways
            - List 3-5 important takeaways as bullet points
            - Focus on practical applications and insights
            
            ## 🧠 Summary
            - Write a concise 1-paragraph summary of the overall video content
            
            ## 💬 Notable Quotes / Insights
            - Include 1-2 significant quotes or key insights from the video
            - Format as proper markdown blockquotes using '>' symbol
            
            ## ❓ Questions
            - What did I learn from this video?
            - What's still unclear or needs further exploration?
            - How does this material relate to the broader course or MBA program?
            """
            
            user_prompt = f"This is a transcript from a video titled '{video_name}' from the course '{course_name}'. Please summarize it following the format in your instructions:\n\n{transcript_text}"
            
            response = openai.chat.completions.create(
                model="gpt-4.1",  # Using GPT-4.1 for best quality summaries
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt}
                ],
                max_tokens=2000,  # Increased token limit for comprehensive summaries
                temperature=0.5
            )
            
            summary = response.choices[0].message.content
            logger.info("Successfully generated comprehensive summary with OpenAI")
            return summary
        
    except Exception as e:
        logger.error(f"Error generating summary with OpenAI: {e}", exc_info=True)
        return None

def retry_failed_files(headers, templates, results_file='video_links_results.json'):
    """
    Retry processing files that previously failed.
    
    Args:
        headers: Auth headers for API calls
        templates: Loaded templates for note generation
        results_file: Path to results file
        
    Returns:
        Tuple of (processed_count, error_count)
    """
    failed_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'failed_files.json')
    
    if not os.path.exists(failed_file):
        logger.info("No failed files found to retry")
        print("No failed files found to retry")
        return 0, 0
    
    try:
        # Load the failed files
        with open(failed_file, 'r') as f:
            failed_list = json.load(f)
    except Exception as e:
    
        logger.error(f"Error reading failed files: {e}")
        print(f"Error reading failed files: {e}")
        return 0, 0
    
    if not failed_list:
        logger.info("Failed files list is empty")
        print("No failed files found to retry")
        return 0, 0
    
    # Get access token
    access_token = headers['Authorization'].replace('Bearer ', '')
    processed = 0
    errors = 0
    
    print(f"Retrying {len(failed_list)} previously failed files...")
    
    for i, failed_item in enumerate(failed_list):
        file_path = failed_item.get('path')
        file_name = failed_item.get('file')
        
        if not file_path or not file_name:
            logger.warning(f"Incomplete failed file record: {failed_item}")
            continue
        
        print(f"[{i+1}/{len(failed_list)}] Retrying: {file_name}")
        
        # Find the file in OneDrive
        item = find_file_in_onedrive(file_path, access_token)
        
        if not item:
            logger.error(f"File not found: {file_path}")
            errors += 1
            continue
        
        # Mark as retried
        failed_item['retried'] = True
        
        # Process the file
        result = process_single_video(item, headers, templates, results_file)
        
        if result.get('success', False):
            processed += 1
            # Remove from failed list
            failed_list.remove(failed_item)
        else:
            errors += 1
            
        # Update the failed files list after each iteration
        try:
            with open(failed_file, 'w') as f:
                json.dump(failed_list, f, indent=2)
        except Exception as e:
            logger.error(f"Error updating failed files: {e}")
    
    # Ensure this is inside a function or replace with appropriate logic
    print(f"Processed: {processed}, Errors: {errors}")

def check_if_file_exists(file_path, access_token, session=None):
    """
    Check if a file exists in OneDrive and return its metadata.
    Implemented based on the onedrive_share.py approach.
    
    Args:
        file_path: Path to the file in OneDrive
        access_token: Valid access token for Microsoft Graph API
        session: Optional requests session for connection pooling and retries
        
    Returns:
        File metadata if found, None otherwise
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
    Find a file in OneDrive by its path relative to ONEDRIVE_BASE.
    Improved with comprehensive search strategies.
    
    Args:
        file_path: Path to the file in OneDrive (relative or absolute)
        access_token: Valid access token for Microsoft Graph API
        
    Returns:
        File metadata if found, None otherwise
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
    import urllib.parse
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

def process_single_file_by_path(file_path, headers, templates, results_file='video_links_results.json'):
    """Process a single video file by its path within OneDrive."""
    # Get the file metadata from OneDrive
    item = find_file_in_onedrive(file_path, headers.get('Authorization').split(' ')[1])
    
    if not item:
        error_msg = f"File not found in OneDrive: {file_path}"
        logger.error(error_msg)
        print(f"❌ {error_msg}")
        return {
            'file': file_path,
            'success': False,
            'error': error_msg,
            'timestamp': datetime.now().isoformat()
        }
    
    # Check if it's a video file
    if not item.get('file', {}).get('mimeType', '').startswith('video/'):
        error_msg = f"File is not a video: {file_path} (MIME type: {item.get('file', {}).get('mimeType', 'unknown')})"
        logger.error(error_msg)
        print(f"❌ {error_msg}")
        return {
            'file': file_path,
            'success': False,
            'error': error_msg,
            'timestamp': datetime.now().isoformat()
        }
    
    # Process the file
    print(f"🎬 Processing single video: {item.get('name')}")
    return process_single_video(item, headers, templates, results_file)

def process_path(path, headers, templates, results_file='video_links_results.json', args=None):
    """
    Process a single file or all video files in a directory (recursively).
    Accepts both absolute and relative paths, and normalizes them to the correct base.
    
    The path can be:
    1. A path relative to the OneDrive base path
    2. An absolute path in the local filesystem
    3. A full path including the OneDrive base path
    """
    # The path might be passed as a string with a leading slash from the command line
    # We need to handle this case specially for OneDrive paths
    if isinstance(path, str) and path.startswith('/'):
        # This is likely a path relative to OneDrive base
        # First, try to see if this is an OneDrive path that needs to be processed directly
        logger.info(f"Processing path that starts with slash: {path}")
        result = process_single_file_by_onedrive_path(path.lstrip('/'), headers, templates, results_file)
        if result:
            return [result] if result.get('success', False) else [], [result] if not result.get('success', False) else []

    # Standard path handling for local files
    path = Path(path)
    if not path.is_absolute():
        abs_path = (RESOURCES_ROOT / path).resolve()
    else:
        abs_path = path.resolve()
    
    video_extensions = {'.mp4', '.mov', '.avi', '.mkv', '.webm', '.wmv', '.mpg', '.mpeg', '.m4v'}
    processed = []
    errors = []
    
    if abs_path.is_file():
        result = process_single_file_by_path(str(abs_path), headers, templates, results_file)
        if result and result.get('success', False):
            processed.append(result)
        else:
            errors.append(result)
    elif abs_path.is_dir():
        for file in abs_path.rglob('*'):
            if file.suffix.lower() in video_extensions:
                result = process_single_file_by_path(str(file), headers, templates, results_file)
                if result and result.get('success', False):
                    processed.append(result)
                else:
                    errors.append(result)
    else:
        logger.error(f"Path not found: {path}")
        print(f"Path not found: {path}")
    
    return processed, errors

def process_single_file_by_onedrive_path(file_path, headers, templates, results_file='video_links_results.json'):
    """
    Process a single video file by its path within OneDrive, without requiring the file to exist locally.
    This is useful when processing files that exist in OneDrive but may not be synced locally.
    
    Args:
        file_path: Path to the file relative to the OneDrive base path
        headers: Authentication headers
        templates: Loaded templates for note generation
        results_file: Path to results file
        
    Returns:
        dict: Result of processing
    """
    logger.info(f"Looking for file directly in OneDrive: {file_path}")
    
    # Extract access token
    access_token = headers['Authorization'].replace('Bearer ', '')
    
    # First try to find the file in OneDrive
    item = find_file_in_onedrive(file_path, access_token)
    
    if not item:
        error_msg = f"File not found in OneDrive: {file_path}"
        logger.error(error_msg)
        print(f"❌ {error_msg}")
        return {
            'file': file_path,
            'success': False,
            'error': error_msg,
            'timestamp': datetime.now().isoformat()
        }
    
    # Check if it's a video file
    if not item.get('file', {}).get('mimeType', '').startswith('video/'):
        # Check file extension as fallback
        file_name = item.get('name', '')
        if not any(file_name.lower().endswith(ext) for ext in ('.mp4', '.mov', '.avi', '.mkv', '.webm', '.wmv', '.mpg', '.mpeg', '.m4v')):
            error_msg = f"File is not a video: {file_path} (MIME type: {item.get('file', {}).get('mimeType', 'unknown')})"
            logger.error(error_msg)
            print(f"❌ {error_msg}")
            return {
                'file': file_path,
                'success': False,
                'error': error_msg,
                'timestamp': datetime.now().isoformat()
            }
    
    # Process the file
    print(f"🎬 Processing video from OneDrive: {item.get('name')}")
    return process_single_video(item, headers, templates, results_file)

def parse_arguments():
    """Parse command-line arguments."""
    parser = argparse.ArgumentParser(
        description="Generate shareable OneDrive links for MBA videos and create reference notes in Obsidian vault."
    )
    
    # File or folder selection options (mutually exclusive)
    file_group = parser.add_mutually_exclusive_group()
    file_group.add_argument(
        "-f", "--single-file", 
        help="Process only a single file (path relative to OneDrive base path)"
    )
    
    file_group.add_argument(
        "--folder", 
        help="Process all video files in a directory (path relative to OneDrive base path)"
    )
    
    parser.add_argument(
        "--no-summary", 
        action="store_true",
        help="Disable OpenAI summary generation for transcripts"
    )
    
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Perform a dry run without creating actual links or notes"
    )
    
    parser.add_argument(
        "--debug",
        action="store_true",
        help="Enable debug logging"
    )
    
    parser.add_argument(
        "--retry-failed",
        action="store_true",
        help="Retry only previously failed files from failed_files.json"
    )
    
    parser.add_argument(
        "--force",
        action="store_true",
        help="Force processing of videos that already have notes (overwrite existing)"
    )
    
    parser.add_argument(
        "--timeout",
        type=int,
        default=15,
        help="Set API request timeout in seconds (default: 15)"
    )
    
    parser.add_argument(
        "--refresh-auth",
        action="store_true",
        help="Force refresh the Microsoft Graph API authentication cache (ignore cached tokens)"
    )
    
    parser.add_argument(
        "--no-share-links",
        action="store_true",
        help="Skip creating OneDrive shared links (faster for testing)"
    )
    
    return parser.parse_args()

if __name__ == "__main__":
    args = parse_arguments()
    
    # Configure logging level if debug flag is set
    if args.debug:
        logger.setLevel(logging.DEBUG)
        for handler in logger.handlers:
            handler.setLevel(logging.DEBUG)
        print("🐛 Debug logging enabled")
    
    # Load templates
    templates = load_templates()
    
    # Use the --refresh-auth flag to force refresh the token cache
    access_token = authenticate_graph_api(force_refresh=getattr(args, 'refresh_auth', False))
    headers = {"Authorization": f"Bearer {access_token}"}
    results_file = 'video_links_results.json'

    # Process specified files or directories
    if args.retry_failed:
        print("🔄 Retrying previously failed files...")
        retry_failed_files(headers, templates, results_file)
    else:
        # Determine if the input is a file or directory
        input_path = getattr(args, 'single_file', None) or getattr(args, 'folder', None)
        
        if input_path:
            print(f"🔍 Processing specified path: {input_path}")
            # If path is an OneDrive path that starts with slash, handle it directly
            processed, errors = process_path(input_path, headers, templates, results_file, args)
            print(f"✅ Processed {len(processed)} videos, ❌ {len(errors)} errors.")        
        else:
            # Fallback to original behavior if no path is provided
            print("📂 Processing all videos in OneDrive...")
            process_videos_for_sharing(args)


def load_prompt_from_file(file_path):
    """
    Load a prompt template from a file.
    
    Args:
        file_path (str): Path to the prompt file
        
    Returns:
        str: Content of the prompt file or None if the file doesn't exist
    """
    try:
        prompt_path = Path(file_path)
        if not prompt_path.exists():
            logger.warning(f"Prompt file not found: {file_path}")
            return None
            
        with open(prompt_path, 'r', encoding='utf-8') as f:
            content = f.read()
            
        logger.info(f"Loaded prompt template from {file_path}: {len(content)} chars")
        return content
    except Exception as e:
        logger.error(f"Error loading prompt from {file_path}: {e}")
        return None

def format_prompt(template, **kwargs):
    """
    Format a prompt template with the given variables.
    
    Args:
        template (str): The prompt template with placeholders
        **kwargs: Variables to insert into the template
        
    Returns:
        str: Formatted prompt
    """
    if not template:
        return ""
        
    # Replace placeholders in the format {variable_name}
    formatted = template
    for key, value in kwargs.items():
        placeholder = f"{{{key}}}"
        formatted = formatted.replace(placeholder, str(value))
        
    return formatted

def chunk_text(text, max_chunk_size=8000, overlap=500):
    """
    Split a long text into overlapping chunks for processing.
    
    Args:
        text (str): The text to split
        max_chunk_size (int): Maximum characters per chunk
        overlap (int): Number of characters to overlap between chunks
        
    Returns:
        list: List of text chunks
    """
    if len(text) <= max_chunk_size:
        return [text]
        
    chunks = []
    start = 0
    
    while start < len(text):
        # Define the end of this chunk
        end = min(start + max_chunk_size, len(text))
        
        # If we're not at the end, find a good breakpoint (newline or period)
        if end < len(text) and end - start == max_chunk_size:
            # Look for a newline or period in the last 100 chars of the chunk
            breakpoint_search_area = text[end-100:end]
            
            # Try to find a newline first
            newline_pos = breakpoint_search_area.rfind('\n')
            if newline_pos != -1:
                end = end - (100 - newline_pos)
            else:
                # If no newline, try to find a period followed by space
                period_pos = breakpoint_search_area.rfind('. ')
                if period_pos != -1:
                    end = end - (100 - period_pos - 2)  # -2 to include the period and space
        
        # Extract the chunk
        chunk = text[start:end]
        chunks.append(chunk)
        
        # Move the start position for the next chunk, with overlap
        start = end - overlap
        
        # Ensure we're making progress even if no good breakpoint was found
        if start < end - overlap:
            start = end - overlap
            
    logger.info(f"Split text into {len(chunks)} chunks (avg {len(text)/len(chunks):.0f} chars per chunk)")
    return chunks

def summarize_chunk(chunk, video_name, course_name, is_first_chunk=False, is_final_chunk=False, chunk_num=1, total_chunks=1):
    """
    Summarize a single chunk of transcript text using OpenAI API.
    
    Args:
        chunk (str): The transcript text chunk
        video_name (str): Name of the video
        course_name (str): Name of the course
        is_first_chunk (bool): Whether this is the first chunk
        is_final_chunk (bool): Whether this is the final chunk
        chunk_num (int): Current chunk number
        total_chunks (int): Total number of chunks
        
    Returns:
        str: The generated summary
    """
    # Create a prompt that indicates whether this is part of a larger transcript
    chunk_context = ""
    if total_chunks > 1:
        chunk_context = f"This is part {chunk_num} of {total_chunks} from the transcript. "
        
        if is_first_chunk:
            chunk_context += "This is the beginning of the transcript. "
        elif is_final_chunk:
            chunk_context += "This is the end of the transcript. "
        else:
            chunk_context += "This is a middle section of the transcript. "

    # Create a more focused prompt for chunk processing
    system_prompt = f"""You are an educational content summarizer for MBA course materials.
    {chunk_context}Analyze this transcript chunk and extract the key information.
    
    For each chunk, identify:
    1. Main topics discussed
    2. Key concepts explained
    3. Important takeaways or insights
    4. Notable quotes
    5. Questions that might arise from this content
    
    Format your response as simple bullet points under each category.
    Keep your response concise but informative.
    """
    
    user_prompt = f"This is {chunk_context.lower()}from a video titled '{video_name}' from the course '{course_name}':\n\n{chunk}"
    
    try:
        response = openai.chat.completions.create(
            model="gpt-4.1",
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt}
            ],
            max_tokens=1000,
            temperature=0.3
        )
        chunk_summary = response.choices[0].message.content
        logger.debug(f"Generated chunk summary ({chunk_num}/{total_chunks}): {len(chunk_summary)} chars")
        return chunk_summary
    except Exception as e:
        logger.error(f"Error summarizing chunk {chunk_num}: {e}")
        return f"Error processing chunk {chunk_num}: {str(e)}"

def generate_summary_with_openai(transcript_text, video_name="", course_name=""):
    """
    Generate a comprehensive summary of the transcript using OpenAI API.
    
    For long transcripts, text is split into chunks, each chunk is summarized 
    separately, and then a final consolidated summary is generated from all 
    chunk summaries.
    
    The summary includes:
    - Topics Covered
    - Key Concepts Explained
    - Important Takeaways
    - Summary
    - Notable Quotes/Insights
    - Questions for reflection
    """
    if not OPENAI_API_KEY:
        logger.warning("OpenAI API key not set. Cannot generate summary.")
        return None
    
    if not transcript_text or len(transcript_text) < 100:
        logger.warning("Transcript text too short or empty. Cannot generate summary.")
        return None
    
    try:
        # Log original text size
        logger.info(f"Processing transcript of {len(transcript_text)} characters")
        
        # Define chunk size based on GPT model limits
        # GPT-4.1 has higher token limits, but we still need to be conservative
        max_chunk_chars = 8000  # ~2000 tokens
        
        # Determine if we need to chunk the text
        if len(transcript_text) > max_chunk_chars * 1.5:
            logger.info(f"Transcript too large ({len(transcript_text)} chars), splitting into chunks")
            chunks = chunk_text(transcript_text, max_chunk_chars)
            total_chunks = len(chunks)
            
            # Process each chunk
            chunk_summaries = []
            for i, chunk in enumerate(chunks):
                logger.info(f"Processing chunk {i+1}/{total_chunks} ({len(chunk)} chars)")
                is_first = (i == 0)
                is_last = (i == total_chunks - 1)
                chunk_summary = summarize_chunk(
                    chunk, 
                    video_name, 
                    course_name, 
                    is_first_chunk=is_first,
                    is_final_chunk=is_last,
                    chunk_num=i+1, 
                    total_chunks=total_chunks
                )
                chunk_summaries.append(chunk_summary)
                
            # Combine the chunk summaries into a single document for final summarization
            combined_summary = "\n\n".join([
                f"--- CHUNK {i+1}/{total_chunks} SUMMARY ---\n{summary}" 
                for i, summary in enumerate(chunk_summaries)
            ])
            
            logger.info(f"Generated {len(chunk_summaries)} chunk summaries, now creating final summary")
            
            # Create the final consolidated summary from all chunk summaries
            system_prompt = """You are an educational content summarizer for MBA course materials. 
            Create a comprehensive final summary based on the provided chunk summaries.
            
            Structure your response in markdown format with these exact sections:
            
            # 🎓 Educational Video Summary (AI Generated)
            
            ## 🧩 Topics Covered
            - List 3-5 main topics covered in the video
            - Be specific and use bullet points
            
            ## 📝 Key Concepts Explained
            - Explain the key concepts in 3-5 paragraphs
            - Focus on the most important ideas
            
            ## ⭐ Important Takeaways
            - List 3-5 important takeaways as bullet points
            - Focus on practical applications and insights
            
            ## 🧠 Summary
            - Write a concise 1-paragraph summary of the overall video content
            
            ## 💬 Notable Quotes / Insights
            - Include 1-2 significant quotes or key insights from the video
            - Format as proper markdown blockquotes using '>' symbol
            
            ## ❓ Questions
            - What did I learn from this video?
            - What's still unclear or needs further exploration?
            - How does this material relate to the broader course or MBA program?
            """
            
            user_prompt = f"These are the chunk summaries from a video titled '{video_name}' from the course '{course_name}'. Create a comprehensive final summary following the format in your instructions:\n\n{combined_summary}"
            
            response = openai.chat.completions.create(
                model="gpt-4.1",  # Using GPT-4.1 for best quality summaries
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt}
                ],
                max_tokens=2000,  # Increased token limit for comprehensive summaries
                temperature=0.5
            )
            
            summary = response.choices[0].message.content
            logger.info("Successfully generated consolidated summary from all chunks with OpenAI")
            return summary
        else:
            # For shorter transcripts, process directly without chunking
            logger.info("Transcript length is appropriate for direct processing")
            
            # Create a comprehensive prompt for better summaries
            system_prompt = """You are an educational content summarizer for MBA course materials. 
            Create a comprehensive summary of the following transcript from an educational video.
            
            Structure your response in markdown format with these exact sections:
            
            # 🎓 Educational Video Summary (AI Generated)
            
            ## 🧩 Topics Covered
            - List 3-5 main topics covered in the video
            - Be specific and use bullet points
            
            ## 📝 Key Concepts Explained
            - Explain the key concepts in 3-5 paragraphs
            - Focus on the most important ideas
            
            ## ⭐ Important Takeaways
            - List 3-5 important takeaways as bullet points
            - Focus on practical applications and insights
            
            ## 🧠 Summary
            - Write a concise 1-paragraph summary of the overall video content
            
            ## 💬 Notable Quotes / Insights
            - Include 1-2 significant quotes or key insights from the video
            - Format as proper markdown blockquotes using '>' symbol
            
            ## ❓ Questions
            - What did I learn from this video?
            - What's still unclear or needs further exploration?
            - How does this material relate to the broader course or MBA program?
            """
            
            user_prompt = f"This is a transcript from a video titled '{video_name}' from the course '{course_name}'. Please summarize it following the format in your instructions:\n\n{transcript_text}"
            
            response = openai.chat.completions.create(
                model="gpt-4.1",  # Using GPT-4.1 for best quality summaries
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt}
                ],
                max_tokens=2000,  # Increased token limit for comprehensive summaries
                temperature=0.5)
            
            summary = response.choices[0].message.content
            logger.info("Successfully generated comprehensive summary with OpenAI")
            return summary
        
    except Exception as e:
        logger.error(f"Error generating summary with OpenAI: {e}", exc_info=True)
        return None

