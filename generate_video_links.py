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
RESOURCES_ROOT = Path(normalize_wsl_path(r'/mnt/c/Users/danielshue/OneDrive/Education/MBA Resources'))  # The folder containing your videos in OneDrive
VAULT_ROOT = Path(normalize_wsl_path(r'/mnt/d/MBA'))  # Your Obsidian vault root
ONEDRIVE_BASE = '/Education/MBA Resources'  # OneDrive path to your MBA Resources (this is a URL path, not a file system path)
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
                # Check if parsing produced a valid dictionary
                if yaml_content and isinstance(yaml_content, dict):
                    template_type = yaml_content.get('template-type')
                    if template_type:
                        templates[template_type] = yaml_content
                        successful_templates.append(template_type)
                        logger.debug(f"Successfully loaded template: {template_type}")
                    else:
                        logger.warning(f"Skipping YAML document without template-type")
                else:
                    logger.warning(f"Skipping invalid YAML content (not a dictionary)")
            except yaml.YAMLError as e:
                logger.warning(f"Error parsing YAML document: {e}")
                continue
        
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

def authenticate_graph_api():
    """
    Authenticate to Microsoft Graph API using interactive browser authentication,
    which is compatible with existing Azure app registrations.
    Stores and reuses refresh tokens to minimize authentication prompts.
    Implements Azure best practices for token security and resilience.
    """
    import os
    import time
    import json
    import subprocess
    import webbrowser
    from datetime import datetime, timedelta
    
    # Use WSL-compatible location for token cache
    token_cache_dir = os.path.expanduser("~/.config/mba-notebook")
    token_cache_file = os.path.join(token_cache_dir, "onedrive_token_cache.dat")
    
    # Create the persistent token cache
    # Following Azure best practice for persistent token caches
    from msal.token_cache import SerializableTokenCache
    token_cache = SerializableTokenCache()
    
    # Check if we have a cached token that's still valid
    if os.path.exists(token_cache_file):
        try:
            with open(token_cache_file, 'r') as f:
                encrypted_cache = f.read()
                
            # Decrypt the token cache
            cached_data = decrypt_token_data(encrypted_cache)
            
            # Check if token is still valid (with 5 minute buffer)
            if cached_data.get('expires_at', 0) > time.time() + 300:
                logger.info("Using cached access token")
                return cached_data['access_token']
                
            # If we have a refresh token, we'll use it with the app below
            access_token = None
            refresh_token = cached_data.get('refresh_token')
            
            # Deserialize token cache from cache file if it exists
            if 'token_cache' in cached_data:
                token_cache.deserialize(cached_data['token_cache'])
        except Exception as e:
            logger.warning(f"Error reading token cache: {e}")
            refresh_token = None
    else:
        refresh_token = None
    
    # Initialize MSAL application with proper cache
    app = PublicClientApplication(
        CLIENT_ID, 
        authority=AUTHORITY,
        token_cache=token_cache  # Use the proper serializable token cache for persistence
    )
    
    # Try silent authentication first if we have accounts in the cache
    accounts = app.get_accounts()
    if accounts:
        logger.info(f"Found {len(accounts)} cached account(s)")
        # Try to silently acquire token with the first account
        try:
            result = app.acquire_token_silent(SCOPES, account=accounts[0])
            if result and 'access_token' in result:
                logger.info("Successfully acquired token silently")
                
                # Create directory if it doesn't exist
                os.makedirs(token_cache_dir, exist_ok=True)
                
                # Prepare data for caching
                token_data = {
                    'access_token': result['access_token'],
                    'refresh_token': result.get('refresh_token', refresh_token),
                    'expires_at': time.time() + result.get('expires_in', 3600),
                    'token_cache': token_cache.serialize()  # Save serialized token cache
                }
                
                # Encrypt and save the token cache
                with open(token_cache_file, 'w') as f:
                    f.write(encrypt_token_data(token_data))
                
                # Set proper permissions
                try:
                    import stat
                    os.chmod(token_cache_file, stat.S_IRUSR | stat.S_IWUSR)
                except Exception:
                    pass
                
                return result['access_token']
        except Exception as e:
            logger.warning(f"Silent authentication failed: {e}")
    
    # If silent auth fails, use interactive browser authentication
    # This is most compatible with various Azure app registrations
    logger.info("Starting interactive browser authentication...")
    
    # Check if running in WSL
    is_wsl = "microsoft" in os.uname().release.lower() if hasattr(os, 'uname') else False
    
    if is_wsl:
        # In WSL, we may need to open the default Windows browser
        try:
            # Use powershell.exe to open the default browser
            logger.info("WSL environment detected. Will try to use Windows browser.")
            
            # Prepare for browser authentication
            print("\n" + "="*80)
            print("🔐 Microsoft Graph Authentication Required")
            print("="*80)
            print("A browser window will open for authentication.")
            print("If no browser opens automatically, you may need to use the Windows")
            print("browser manually. Sign in with your Microsoft account.")
            print("="*80 + "\n")
            
            # Interactive auth with browser
            result = app.acquire_token_interactive(
                scopes=SCOPES,
                prompt="select_account"  # Force account selection to avoid account confusion
            )
        except Exception as e:
            logger.error(f"Interactive authentication failed: {e}")
            raise Exception(f"Failed to authenticate: {e}")
    else:
        # On regular systems, just use the normal browser flow
        try:
            # Prepare for browser authentication
            print("\n" + "="*80)
            print("🔐 Microsoft Graph Authentication Required")
            print("="*80)
            print("A browser window will open for authentication.")
            print("Please sign in with your Microsoft account.")
            print("="*80 + "\n")
            
            # Interactive auth with browser
            result = app.acquire_token_interactive(
                scopes=SCOPES,
                prompt="select_account"  # Force account selection to avoid account confusion
            )
        except Exception as e:
            logger.error(f"Interactive authentication failed: {e}")
            raise Exception(f"Failed to authenticate: {e}")
    
    if "access_token" not in result:
        logger.error(f"Authentication failed: {result.get('error_description', 'Unknown error')}")
        raise Exception("Failed to authenticate with Microsoft Graph")
    
    # Get token components
    access_token = result['access_token']
    refresh_token = result.get('refresh_token', refresh_token)
    
    # Store the token for future use
    token_data = {
        'access_token': access_token,
        'refresh_token': refresh_token,
        'expires_at': time.time() + result.get('expires_in', 3600),
        'scope': result.get('scope', ' '.join(SCOPES)),
        'token_cache': token_cache.serialize()  # Save serialized token cache
    }
    
    # Create directory if it doesn't exist
    os.makedirs(token_cache_dir, exist_ok=True)
    
    # Encrypt and save the token cache
    with open(token_cache_file, 'w') as f:
        f.write(encrypt_token_data(token_data))
    
    # Set appropriate permissions on token cache file (Unix only)
    try:
        import stat
        os.chmod(token_cache_file, stat.S_IRUSR | stat.S_IWUSR)  # Read and write permissions for owner only
    except Exception as e:
        logger.warning(f"Could not set restrictive permissions on token cache: {e}")
    
    print("\nAuthentication successful! ✓")
    logger.info("Authentication successful")
    return access_token

def get_file_in_onedrive(relative_path, headers):
    """Get a file's metadata in OneDrive by its relative path."""
    encoded_path = relative_path.replace('/', '%2F')
    url = f"https://graph.microsoft.com/v1.0/me/drive/root:{ONEDRIVE_BASE}/{encoded_path}"
    resp = requests.get(url, headers=headers)
    if resp.status_code == 200:
        return resp.json()
    return None

def create_share_link(file_id, headers):
    """Create a shareable link for the file with specified ID."""
    url = f"https://graph.microsoft.com/v1.0/me/drive/items/{file_id}/createLink"
    
    # Add retry logic - Microsoft Graph API sometimes has intermittent issues
    max_retries = 3
    retry_delay = 2  # seconds
    
    for attempt in range(max_retries):
        try:
            logger.debug(f"Creating share link for file ID: {file_id}, attempt {attempt+1}/{max_retries}")
            resp = requests.post(url, headers=headers, json={"type": "view", "scope": "anonymous"})
            
            # Log the response for debugging
            logger.debug(f"Share link response status code: {resp.status_code}")
            
            if resp.status_code == 200:
                share_link = resp.json()['link']['webUrl']
                logger.debug(f"Successfully created share link: {share_link}")
                return share_link
            elif resp.status_code == 429:  # Rate limiting
                retry_after = int(resp.headers.get('Retry-After', retry_delay))
                logger.warning(f"Rate limited. Retrying after {retry_after} seconds.")
                time.sleep(retry_after)
                continue
            else:
                # Log the error details
                try:
                    error_details = resp.json()
                    error_msg = error_details.get('error', {}).get('message', 'Unknown error')
                    logger.error(f"Failed to create share link. Status: {resp.status_code}, Error: {error_msg}")
                except Exception:
                    logger.error(f"Failed to create share link. Status: {resp.status_code}, Response: {resp.text}")
                
                if attempt < max_retries - 1:
                    logger.info(f"Retrying in {retry_delay} seconds...")
                    time.sleep(retry_delay)
                    retry_delay *= 2  # Exponential backoff
                    continue
        except Exception as e:
            logger.error(f"Exception during share link creation: {str(e)}")
            if attempt < max_retries - 1:
                logger.info(f"Retrying in {retry_delay} seconds...")
                time.sleep(retry_delay)
                retry_delay *= 2  # Exponential backoff
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
                info['lesson'] = lesson_name      # Look for class identifiers - try multiple detection methods
    # Method 1: Direct "class X" pattern
    class_match = re.search(r'class[\s_-]*(\w+)', path_str, re.IGNORECASE)
    if class_match:
        info['class'] = f"Class {class_match.group(1)}"
    else:
        # Method 2: Look for patterns like "01_class" or "class_01" in directory names
        for part in path_parts:
            # Check for common class patterns in folder names
            class_pattern_match = re.search(r'(?:^|\D)((?:\d+[\s_-]*class)|(?:class[\s_-]*\d+))(?:$|\D)', part.lower(), re.IGNORECASE)
            if class_pattern_match:
                class_str = class_pattern_match.group(1).replace('_', ' ').replace('-', ' ').title()
                info['class'] = class_str
                break
        
        # Method 3: If we found a lesson but no class yet, check if any directory after the lesson looks like a class
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

def create_markdown_note_for_video(video_path, share_link, vault_root, item=None):
    """Create a markdown note in the Obsidian vault for the video using metadata templates."""
    # Load templates if not already loaded
    templates = load_templates()
    
    # If no templates found, use a default template
    if not templates:
        logger.warning("No templates found in metadata file. Using default template.")
        
    # If video_path is a string, convert it to Path
    if isinstance(video_path, str):
        video_path = Path(video_path)
    
    # Extract the relative path from the resources folder
    try:
        rel_path = os.path.relpath(video_path, RESOURCES_ROOT)
    except ValueError:
        # Handle case where paths are on different drives
        # Just use the filename and parent folder
        rel_path = Path(video_path.parts[-2]) / video_path.name
        
    # Determine the corresponding vault path
    vault_path = vault_root / rel_path
    vault_dir = vault_path.parent
    
    # Create the directories if they don't exist
    vault_dir.mkdir(parents=True, exist_ok=True)
      # Prepare the markdown file name - same name as video but with -video.md extension
    video_name = video_path.stem
    note_name = f"{video_name}-video.md"
    note_path = vault_dir / note_name
      # Infer course and program information from the file path
    path_info = infer_course_and_program(rel_path)
    
    # Check if a transcript file exists and generate a summary if it does
    transcript_path = find_transcript_file(video_path, vault_root)
    summary_text = None
    custom_tags = ['video', 'onedrive', 'course-materials']
    if transcript_path:
        logger.info(f"Found transcript for {video_name} at: {transcript_path}")
        transcript_content = get_transcript_content(transcript_path)
        
        if transcript_content and OPENAI_API_KEY:
            # Generate AI summary from transcript
            logger.info(f"Generating summary for {video_name} using OpenAI...")
            summary_text = generate_summary_with_openai(transcript_content, video_name, path_info['course'])
            logger.info(f"Summary generated: {len(summary_text) if summary_text else 0} characters")
            
            # Generate AI tags based on the transcript content
            logger.info(f"Generating tags for {video_name} using OpenAI...")
            try:
                custom_tags = generate_tags_with_openai(
                    transcript_content, 
                    video_name, 
                    path_info['course'], 
                    path_info['program']
                )
                logger.info(f"Generated tags: {', '.join(custom_tags)}")
            except Exception as e:
                logger.error(f"Error generating tags: {e}")
                # Ensure we have default tags if tag generation fails
                custom_tags = ['video', 'onedrive', 'course-materials']
    
    # Get the video-reference template if available
    video_template = templates.get('video-reference', {})    # Build the frontmatter based on the template and path information
    # Extract duration from item metadata
    duration = item.get('video', {}).get('duration', 'Unknown')
    if duration and duration != 'Unknown':
        # Try to format duration in minutes and seconds if it's numeric
        try:
            duration_seconds = int(duration)
            minutes = duration_seconds // 60
            seconds = duration_seconds % 60
            formatted_duration = f"{minutes} minutes {seconds} seconds"
        except (ValueError, TypeError):
            formatted_duration = str(duration)
    else:
        formatted_duration = "Unknown"
    
    # Format the upload date to be more user-friendly
    uploaded_date = item.get('createdDateTime', 'Unknown')
    if uploaded_date and uploaded_date != 'Unknown':
        # Try to parse ISO date and format it to YYYY-MM-DD
        try:
            # Extract just the date part if it's a full ISO datetime
            if 'T' in uploaded_date:
                uploaded_date = uploaded_date.split('T')[0]
        except Exception:
            # Keep original if parsing fails
            pass
    
    # Calculate size in MB with 2 decimal precision
    size_mb = f"{item.get('size', 0) / (1024 * 1024):.2f} MB" if item.get('size') else 'Unknown'
    
    frontmatter = {
        'auto-generated-state': 'writable',
        'template-type': 'video-reference',
        'template-description': 'Links to a video file stored in OneDrive for improved sharing and accessibility.',
        'type': 'reference',  # Match the template type
        'title': video_name,
        'video-link': share_link,
        'onedrive-path': str(video_path),
        'date-created': datetime.now().strftime('%Y-%m-%d'),
        'tags': custom_tags,
        'program': path_info['program'],
        'course': path_info['course'],
        # Video metadata as top-level properties instead of nested object
        'video-duration': formatted_duration,
        'video-uploaded': uploaded_date,
        'video-size': size_mb,
        # Progress tracking fields
        'status': 'unwatched',  # Options: unwatched, in-progress, completed
        'completion-date': '',  # To be filled manually
        'review-date': '',      # For scheduling review sessions
        'comprehension': ''     # 1-5 rating
    }
    
    # Add module, lesson, and class if available
    if path_info['module']:
        frontmatter['module'] = path_info['module']
    if path_info['lesson']:
        frontmatter['lesson'] = path_info['lesson']
    if path_info['class']:
        frontmatter['class'] = path_info['class']
    
    # Add transcript reference if found
    if transcript_path:
        frontmatter['transcript'] = str(transcript_path.relative_to(vault_root)) if vault_root in transcript_path.parents else str(transcript_path)
    
    # For any keys in the template, use those values as defaults if not already set
    if video_template:
        for key, value in video_template.items():
            if key not in frontmatter and key != 'title' and key != 'template-type':
                frontmatter[key] = value
      # Format the frontmatter as YAML
    frontmatter_text = "---\n"
    for key, value in frontmatter.items():
        if key == 'tags' and isinstance(value, list):
            # Special handling for tags to ensure proper formatting
            frontmatter_text += f"{key}:\n"
            for tag in value:
                # Clean tag: remove quotes, spaces, etc.
                clean_tag = str(tag).strip().strip('"\'')
                if clean_tag:  # Only add non-empty tags
                    frontmatter_text += f" - {clean_tag}\n"
        elif isinstance(value, list):
            frontmatter_text += f"{key}:\n"
            for item in value:
                frontmatter_text += f" - {item}\n"
        else:
            frontmatter_text += f"{key}: {value}\n"
    frontmatter_text += "---\n\n"
      # Create the main content based on the Templater template format
    content = f"""# 📼 {video_name}

## 🔗 Video Source
- 📺 Link: [Watch here]({share_link})
- 🏫 Source: OneDrive
  
"""    # Add transcript reference if available
    if transcript_path:
        rel_transcript_path = transcript_path.relative_to(vault_root) if vault_root in transcript_path.parents else transcript_path
        content += f"## 📄 Transcript\nA transcript for this video is available at: [[{rel_transcript_path.stem}]]\n"# Add summary if available
    if summary_text:
        # Don't add the heading as it's already in the summary_text
        content += f"\n{summary_text}\n"
    else:
        # If no summary, provide the template structure
        content += f"""

## 🧩 Topics Covered
-
-
-

## 🧠 Summary
- _Write a brief summary of the video content here._

## 📝 Key Concepts Explained 
-
-
-

## ⭐ Important Takeaways
-
-
-

## 💬 Notable Quotes / Insights
> "Quote or key idea from the video"

## ❓ Questions
- What did I learn?
- What's still unclear?
- How does this relate to the course or class?
"""
    
    content += f"\n# 📝 Notes\nAdd your notes about the video here.\n"
    
    full_content = frontmatter_text + content      # Check if the file already exists
    if note_path.exists():
        logger.info(f"Note already exists at {note_path}. Checking if updates are allowed.")
        # Read existing content
        with open(note_path, 'r', encoding='utf-8') as f:
            existing_content = f.read()
        
        # Parse the existing YAML frontmatter if it exists
        existing_frontmatter = {}
        content_without_frontmatter = existing_content
        
        # Check if the content has YAML frontmatter
        if existing_content.startswith('---'):
            try:
                # Find the end of frontmatter
                end_frontmatter = existing_content.find('---', 3)
                if end_frontmatter > 0:
                    frontmatter_text = existing_content[3:end_frontmatter].strip()
                    existing_frontmatter = yaml.safe_load(frontmatter_text) or {}
                    content_without_frontmatter = existing_content[end_frontmatter + 3:].strip()
            except Exception as e:
                logger.warning(f"Error parsing existing frontmatter: {e}")
        
        # Check if auto-generated-state is not writable
        auto_gen_state = existing_frontmatter.get('auto-generated-state', 'writable')
        if auto_gen_state != 'writable':
            logger.info(f"Note has auto-generated-state={auto_gen_state}, skipping updates")
            print(f"  │  └─ Note marked as {auto_gen_state}, skipping updates")
            return note_path
            
        logger.info(f"Note is writable, updating content and metadata.")
        
        # Extract Notes section to preserve it if it exists
        notes_section = ""
        notes_pattern = re.compile(r'(#\s*📝\s*Notes[\s\S]*?)(?=^#|$)', re.MULTILINE)
        match = notes_pattern.search(content_without_frontmatter)
        if match:
            notes_section = match.group(1)
            logger.info(f"Found existing notes section to preserve ({len(notes_section)} chars)")
      # Update frontmatter with new tags and other metadata
        # Keep existing values for keys we don't want to overwrite
        for key, value in frontmatter.items():
            # For tags, merge existing and new tags with duplicates removed
            if key == 'tags' and key in existing_frontmatter and isinstance(existing_frontmatter[key], list):
                existing_tags = existing_frontmatter[key]
                # Use a set to eliminate duplicates, then convert back to list
                merged_tags = list(set(existing_tags + custom_tags))
                existing_frontmatter[key] = merged_tags
            # Special handling for video-metadata object - ensure it's completely replaced
            elif key == 'video-metadata':
                # Completely replace the video-metadata with our updated values
                existing_frontmatter[key] = value
                logger.debug(f"Updated video-metadata: {value}")
            # Preserve existing values for progress tracking fields if they've been set
            elif key in ['status', 'completion-date', 'review-date', 'comprehension'] and key in existing_frontmatter:
                # Only keep non-empty values to preserve user modifications
                if existing_frontmatter[key]:
                    logger.debug(f"Preserving user-set value for {key}: {existing_frontmatter[key]}")
                    # Don't overwrite with default value
                    continue
                else:
                    # Use the default value if the field is empty
                    existing_frontmatter[key] = value
            else:
                # For other keys, update with new value if it doesn't exist or we want to overwrite
                existing_frontmatter[key] = value
        
        # Format updated frontmatter as YAML
        updated_frontmatter_text = "---\n"
        for key, value in existing_frontmatter.items():
            if key == 'tags' and isinstance(value, list):
                # Special handling for tags to ensure proper formatting
                updated_frontmatter_text += f"{key}:\n"
                for tag in value:
                    # Clean tag: remove quotes, spaces, etc.
                    clean_tag = str(tag).strip().strip('"\'')
                    if clean_tag:  # Only add non-empty tags
                        updated_frontmatter_text += f" - {clean_tag}\n"
            elif isinstance(value, list):
                updated_frontmatter_text += f"{key}:\n"
                for item in value:
                    updated_frontmatter_text += f" - {item}\n"
            else:
                updated_frontmatter_text += f"{key}: {value}\n"
        updated_frontmatter_text += "---\n\n"
        
        # Check if we need to update the content body with the link
        if share_link not in content_without_frontmatter:
            # Find where to insert the link
            if "## Video" in content_without_frontmatter or "## 🔗 Video Source" in content_without_frontmatter:
                # Insert after the Video heading if it exists
                if "## Video" in content_without_frontmatter:
                    content_without_frontmatter = content_without_frontmatter.replace(
                        "## Video", 
                        f"## Video\n\n[Watch Video]({share_link})"
                    )
                elif "## 🔗 Video Source" in content_without_frontmatter:
                    # Don't modify if it already has our formatted video source section
                    pass
                else:
                    # Append to the end
                    content_without_frontmatter += f"\n\n## Video\n\n[Watch Video]({share_link})\n"
            else:
                # Append to the end
                content_without_frontmatter += f"\n\n## Video\n\n[Watch Video]({share_link})\n"        # If we have a summary but the current file doesn't, add it
        # Don't add the heading as it's already in the summary_text
        if summary_text and "# 🎓 Educational Video Summary (AI Generated)" not in content_without_frontmatter:
            content_without_frontmatter += f"\n{summary_text}\n"        # Create new content based on the template but preserve notes section
        if notes_section:
            # First ensure that the notes section is removed from the content we're updating
            content_without_notes = re.sub(r'#\s*📝\s*Notes[\s\S]*?(?=^#|$)', '', content_without_frontmatter, flags=re.MULTILINE)
            
            # Extract the user-added content sections
            # We need to identify where the notes section was located in the original content
            parts = content_without_frontmatter.split('# 📝 Notes')
            
            if len(parts) > 1:
                # Content before the notes section
                content_before_notes = parts[0].strip()
                
                # Any content after the notes section marker should be part of the notes
                # (this would handle the case where user has added sub-sections under Notes)
                # But we already have the full notes section from our earlier extraction
                
                # Use the content before notes and append the preserved notes section
                modified_content_body = f"{content_before_notes}\n\n{notes_section}"
            else:
                # If we can't clearly identify where the notes section was,
                # just add it at the end as before
                modified_content_body = f"{content_without_notes.strip()}\n\n{notes_section}"
            
            # Combine updated frontmatter and content with preserved notes
            modified_content = f"{updated_frontmatter_text}{modified_content_body}"
        else:
            # No notes section found, use standard update
            modified_content = updated_frontmatter_text + content_without_frontmatter
        
        # Write the modified content
        with open(note_path, 'w', encoding='utf-8') as f:
            f.write(modified_content)
        
        logger.info(f"Updated note with new metadata including tags: {', '.join(custom_tags)}")
        return note_path
    
    # Write the file
    with open(note_path, 'w', encoding='utf-8') as f:
        f.write(full_content)
    
    return note_path

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

def process_single_video(item, headers, templates, results_file='video_links_results.json'):
    """Process a single video file through its complete lifecycle."""
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
        
        # Early check if the file exists and has non-writable auto-generated-state
        if note_path.exists():
            try:
                with open(note_path, 'r', encoding='utf-8') as f:
                    existing_content = f.read()
                
                # Check if the content has YAML frontmatter
                if existing_content.startswith('---'):
                    # Find the end of frontmatter
                    end_frontmatter = existing_content.find('---', 3)
                    if end_frontmatter > 0:
                        frontmatter_text = existing_content[3:end_frontmatter].strip()
                        existing_frontmatter = yaml.safe_load(frontmatter_text) or {}
                          # Check auto-generated-state
                        auto_gen_state = existing_frontmatter.get('auto-generated-state', 'writable')
                        if auto_gen_state != 'writable':
                            logger.info(f"Note {note_path} has auto-generated-state={auto_gen_state}, skipping all processing")
                            print(f"  └─ Note exists with auto-generated-state={auto_gen_state}, skipping processing")
                            
                            # Get existing share link from frontmatter if available
                            share_link = existing_frontmatter.get('video-link', None)
                            
                            # Still create a result record but mark it as skipped
                            result = {
                                'file': rel_path,
                                'note_path': str(note_path),
                                'share_link': share_link,
                                'success': True,
                                'skipped': True,
                                'reason': f"auto-generated-state={auto_gen_state}",
                                'modified_date': datetime.now().isoformat()
                            }
                            
                            # Update the results file
                            update_results_file(results_file, result)
                            return result
            except Exception as e:
                logger.warning(f"Error checking existing note frontmatter: {e}, will continue processing")
        
        # Step 1: Create a shareable link
        print("  ├─ Creating shareable link...")
        share_link = create_share_link(item['id'], headers)
        if not share_link:
            logger.error(f"Could not create share link for: {rel_path}")
            print("  │  └─ Failed to create shareable link!")
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
        logger.error(f"Error processing {item.get('name', 'unknown')}: {error_msg}")
        print(f"  └─ Error: {error_msg}")
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

def check_api_health(session):
    """
    Perform a health check against Microsoft Graph API.
    
    Args:
        session: Requests session to use
        
    Returns:
        Tuple of (is_healthy, message)
    """
    try:
        # Make a simple request to check API availability
        response = session.get(
            "https://graph.microsoft.com/v1.0/",
            timeout=5  # Short timeout for quick health check
        )
        
        if response.status_code == 401:
            # This is actually expected when no auth token is provided
            return True, "API is available"
        elif response.status_code >= 500:
            return False, f"API server error: {response.status_code}"
        else:
            return True, "API is available"
    except requests.exceptions.ConnectionError:
        return False, "Cannot connect to API"
    except requests.exceptions.Timeout:
        return False, "API timeout"
    except Exception as e:
        return False, f"API check failed: {str(e)}"

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

def process_videos_for_sharing():
    """Main function to find videos in OneDrive, generate links, and create notes."""
    # Load templates first
    templates = load_templates()
    if not templates:
        logger.warning("Could not load templates from metadata.yaml. Using default templates.")
    else:
        logger.info(f"Successfully loaded {len(templates)} templates from metadata.yaml")
    
    # Authenticate - using the improved interactive browser authentication
    print("\n🔑 Authenticating with Microsoft Graph API...")
    try:
        access_token = authenticate_graph_api()
        headers = {"Authorization": f"Bearer {access_token}"}
        logger.info("Authenticated to Microsoft Graph API")
        print("✅ Authentication successful\n")
    except Exception as e:
        logger.error(f"Authentication failed: {e}")
        print(f"\n❌ Authentication failed: {e}\n")
        return [], [f"Authentication error: {str(e)}"]
    
    # Get all items from OneDrive
    print("📂 Fetching files from OneDrive...")
    try:
        onedrive_items = get_onedrive_items(access_token, ONEDRIVE_BASE)
        logger.info(f"Found {len(onedrive_items)} items in OneDrive")
        print(f"✅ Found {len(onedrive_items)} files in OneDrive\n")
    except Exception as e:
        logger.error(f"Failed to fetch OneDrive items: {e}")
        print(f"❌ Failed to fetch files from OneDrive: {e}\n")
        return [], [f"OneDrive API error: {str(e)}"]
    
    # Filter for video files
    video_items = [item for item in onedrive_items if item.get('file', {}).get('mimeType', '').startswith('video/')]
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
        print(f"[{index+1}/{total_videos}] Processing: {video_name}")
        
        # Process this video through its full lifecycle
        result = process_single_video(item, headers, templates, results_file)
        
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
    """Find a transcript file that corresponds to a video file."""
    if isinstance(video_path, str):
        video_path = Path(video_path)
    
    # Generate possible transcript file names
    video_name = video_path.stem
    transcript_name_patterns = [
        f"{video_name} Transcript.md",
        f"{video_name}-Transcript.md",
        f"{video_name} transcript.md",
        f"{video_name}-transcript.md",
        f"{video_name}_Transcript.md",
        f"{video_name}_transcript.md"
    ]
    
    # First, check if transcript exists in the same directory as where the video reference would be
    try:
        rel_path = os.path.relpath(video_path, RESOURCES_ROOT)
        vault_dir = vault_root / Path(os.path.dirname(rel_path))
    except ValueError:
        # Handle case where paths are on different drives
        rel_path = Path(video_path.name)
        vault_dir = vault_root
    
    # Check each possible transcript filename pattern
    for pattern in transcript_name_patterns:
        transcript_path = vault_dir / pattern
        if transcript_path.exists():
            logger.info(f"Found transcript file: {transcript_path}")
            return transcript_path
    
    # Check parent directory as well
    parent_dir = vault_dir.parent
    if parent_dir.exists():
        for pattern in transcript_name_patterns:
            transcript_path = parent_dir / pattern
            if transcript_path.exists():
                logger.info(f"Found transcript file in parent directory: {transcript_path}")
                return transcript_path
    
    # If no transcript file found in local filesystem, return None
    logger.info(f"No transcript file found for {video_name}")
    return None

def get_transcript_content(transcript_path):
    """Extract the content from a transcript file."""
    try:
        with open(transcript_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Remove YAML frontmatter if it exists
        content = re.sub(r'^---\s.*?---\s', '', content, flags=re.DOTALL)
        
        # Remove any markdown headings and formatting
        content = re.sub(r'#+\s.*?\n', '', content)
        content = re.sub(r'\*\*(.*?)\*\*', r'\1', content)
        content = re.sub(r'\*(.*?)\*', r'\1', content)
        
        return content.strip()
    except Exception as e:
        logger.error(f"Error reading transcript file {transcript_path}: {e}")
        return None

def generate_tags_with_openai(transcript_text, video_name="", course_name="", program_name=""):
    """Generate relevant tags for the video based on its content using OpenAI."""
    if not OPENAI_API_KEY:
        logger.warning("OpenAI API key not set. Cannot generate tags.")
        return ['video', 'onedrive', 'course-materials']
    
    if not transcript_text or len(transcript_text) < 100:
        logger.warning("Transcript text too short or empty. Using default tags.")
        return ['video', 'onedrive', 'course-materials']
    
    try:
        # Limit input to avoid excessive token usage
        max_tokens = 4000  # We need less context for tag generation
        if len(transcript_text) > max_tokens * 4:
            logger.info(f"Truncating transcript text for tag generation")
            transcript_text = transcript_text[:max_tokens * 4] + "..."
        
        # Create a prompt focused on tag generation
        system_prompt = """You are an expert at categorizing MBA educational content. 
        Generate 5-10 relevant tags for the transcript provided. 
        
        Follow these guidelines:
        - Include key subject areas (e.g., finance, accounting, marketing)
        - Include specific topics within those areas (e.g., valuation, costing, brand-management)
        - Include relevant methodologies or frameworks mentioned (e.g., SWOT, DCF, porter-five-forces)
        - Use kebab-case for multi-word tags (e.g., 'cash-flow' not 'cash flow')
        - Do not include generic tags like 'MBA', 'video', 'course'
        - Be specific to the actual content
        
        Return ONLY a JSON array of string tags without explanation or commentary.
        Example: ["accounting", "fixed-costs", "profit-margin", "decision-making", "cost-allocation"]
        """
        
        user_prompt = f"Generate tags for a video titled '{video_name}' from the '{course_name}' course in the '{program_name}' program based on this transcript:\n\n{transcript_text}"
        
        response = openai.chat.completions.create(
            model="gpt-4",  # Using GPT-4 for precise tag generation
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt}
            ],
            max_tokens=200,  # Tags should be concise
            temperature=0.3  # Lower temperature for more consistent results
        )
        
        # Parse the response as JSON array
        tags_text = response.choices[0].message.content.strip()
        # Clean up the response in case there's any explanation text
        if '[' in tags_text and ']' in tags_text:
            tags_json = tags_text[tags_text.find('['):tags_text.rfind(']')+1]
            try:
                custom_tags = json.loads(tags_json)
                logger.info(f"Successfully generated {len(custom_tags)} custom tags")
                # Always include the default tags
                return ['video', 'onedrive', 'course-materials'] + custom_tags
            except json.JSONDecodeError:
                logger.warning("Could not parse tags JSON. Using default tags.")
                return ['video', 'onedrive', 'course-materials']
        else:
            logger.warning("No valid JSON array found in tag response. Using default tags.")
            return ['video', 'onedrive', 'course-materials']
            
    except Exception as e:
        logger.error(f"Error generating tags with OpenAI: {e}")
        return ['video', 'onedrive', 'course-materials']

def generate_summary_with_openai(transcript_text, video_name="", course_name=""):
    """
    Generate a comprehensive summary of the transcript using OpenAI API.
    
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
        # Limit the input text to avoid excessive token usage
        max_tokens = 12000  # Limit input to ~12k tokens for gpt-4
        if len(transcript_text) > max_tokens * 4:  # Rough estimate of chars to tokens
            logger.info(f"Truncating transcript text from {len(transcript_text)} characters to ~{max_tokens * 4}")
            transcript_text = transcript_text[:max_tokens * 4] + "..."
          # Create a more comprehensive prompt for better summaries
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
            model="gpt-4",  # Using GPT-4 for best quality summaries
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt}
            ],
            max_tokens=1500,  # Increased token limit for comprehensive summaries
            temperature=0.5
        )
        
        summary = response.choices[0].message.content
        logger.info("Successfully generated comprehensive summary with OpenAI")
        return summary
        
    except Exception as e:
        logger.error(f"Error generating summary with OpenAI: {e}")
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
    
    return processed, errors

def find_file_in_onedrive(file_path, access_token):
    """Find a file in OneDrive by its path relative to ONEDRIVE_BASE."""
    headers = {"Authorization": f"Bearer {access_token}"}
    session = create_requests_session()
    
    # Try direct access by path first
    encoded_path = file_path.replace('/', '%2F')
    target_path = f"{ONEDRIVE_BASE}/{encoded_path}".rstrip('/')
    url = f"https://graph.microsoft.com/v1.0/me/drive/root:{target_path}"
    
    logger.info(f"Looking for file at path: {target_path}")
    
    try:
        resp = session.get(url, headers=headers)
        
        if resp.status_code == 200:
            return resp.json()
        elif resp.status_code >= 400:
            # Log the error properly
            category = categorize_error(f"HTTP {resp.status_code}", resp.status_code)
            logger.warning(f"Direct path lookup failed with status {resp.status_code} ({category})")
    except Exception as e:
        category = categorize_error(e)
        logger.warning(f"Error during direct path lookup: {e} ({category})")
    
    # If direct path fails, search by name as fallback
    file_name = os.path.basename(file_path)
    logger.info(f"Direct path lookup failed. Searching by filename: {file_name}")
    
    # Use search endpoint to find the file
    search_url = f"https://graph.microsoft.com/v1.0/me/drive/root/search(q='{file_name}')"
    
    try:
        resp = session.get(search_url, headers=headers)
        
        if resp.status_code == 200:
            results = resp.json().get('value', [])
            # Filter for exact name match
            for item in results:
                if item.get('name') == file_name:
                    logger.info(f"Found file by name: {file_name}")
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

def parse_arguments():
    """Parse command-line arguments."""
    parser = argparse.ArgumentParser(
        description="Generate shareable OneDrive links for MBA videos and create reference notes in Obsidian vault."
    )
    
    parser.add_argument(
        "-f", "--single-file", 
        help="Process only a single file (path relative to OneDrive base path)"
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
        "--timeout",
        type=int,
        default=10,
        help="Set API request timeout in seconds (default: 10)"
    )
    
    return parser.parse_args()

if __name__ == "__main__":
    print("Starting video link generation process...")
    
    # Parse command-line arguments
    args = parse_arguments()
      # Set up debug logging if requested
    if args.debug:
        logger.setLevel(logging.DEBUG)
        logger.debug("Debug logging enabled")
      # Set global timeout from arguments
    request_timeout = args.timeout if args.timeout and args.timeout > 0 else 10
    if args.timeout and args.timeout > 0:
        logger.info(f"Setting API timeout to {args.timeout} seconds")
    
    # Disable OpenAI summary if requested
    # global OPENAI_API_KEY  # Not needed in main module scope  # Declare global before using it
    if args.no_summary:
        logger.info("OpenAI summary generation disabled via command line")
        OPENAI_API_KEY = None
    
    # Load templates first - needed for all processing modes
    templates = load_templates()
    if not templates:
        logger.warning("Could not load templates from metadata.yaml. Using default templates.")
    else:
        logger.info(f"Successfully loaded {len(templates)} templates from metadata.yaml")
    
    # Authenticate
    print("\n🔑 Authenticating with Microsoft Graph API...")
    try:
        access_token = authenticate_graph_api()
        headers = {"Authorization": f"Bearer {access_token}"}
        logger.info("Authenticated to Microsoft Graph API")
        print("✅ Authentication successful\n")
    except Exception as e:
        category = categorize_error(e)
        logger.error(f"Authentication failed: {e} (Category: {category})")
        print(f"\n❌ Authentication failed: {e}\n")
        exit(1)
    
    # Create a session and check API health
    session = create_requests_session()
    is_healthy, health_message = check_api_health(session)
    if not is_healthy:
        logger.warning(f"API health check failed: {health_message}")
        print(f"⚠️ API health check failed: {health_message}")
        print("Proceeding with caution - this might lead to errors.")
    else:
        logger.info(f"API health check passed: {health_message}")
        print("✅ API health check passed\n")
    
    if args.dry_run:
        print("🔍 DRY RUN MODE: No actual changes will be made")
    
    # Check operating mode based on command line arguments
    if args.retry_failed:
        # Process only previously failed files
        print("🔄 Retrying previously failed files...")
        processed, errors = retry_failed_files(headers, templates)
        print(f"\n✅ Successfully processed {processed} previously failed files")
        if errors:
            print(f"❌ {errors} files still have errors. Check failed_files.json and failed_files.log for details.")
        
        # Show command to retry remaining failed files
        if errors:
            print(f"\nTo retry remaining failed files, run:")
            print(f"wsl python3 {os.path.basename(__file__)} --retry-failed")
    
    elif args.single_file:
        # Process single file mode
        print(f"Processing single file: {args.single_file}")
        
        if not args.dry_run:
            # Process the single file
            result = process_single_file_by_path(args.single_file, headers, templates)
            
            if result.get('success', False):
                print("\n✅ File processed successfully")
            else:
                error_msg = result.get('error', 'Unknown error')
                category = categorize_error(error_msg)
                print(f"\n❌ Failed to process file: {error_msg} (Category: {category})")
                # Update the failed files record
                update_failed_files({"file": args.single_file, "path": args.single_file}, error_msg, category)
        else:
            # In dry run mode, just find the file and report
            item = find_file_in_onedrive(args.single_file, access_token)
            if item:
                print(f"✅ File found: {item.get('name')}")
                print(f"   MIME Type: {item.get('file', {}).get('mimeType', 'Unknown')}")
                print(f"   Size: {item.get('size', 'Unknown')} bytes")
                print(f"   Would process this file in normal mode")
            else:
                print(f"❌ File not found: {args.single_file}")
    else:
        # Process all videos
        processed_videos, errors = process_videos_for_sharing()
        
        # If there were errors, suggest the retry option
        if errors:
            print("\nSome files failed processing. To retry them later, run:")
            print(f"wsl python3 {os.path.basename(__file__)} --retry-failed")
    
    print("Process completed.")
