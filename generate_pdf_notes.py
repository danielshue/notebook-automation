#!/usr/bin/env python3
"""
PDF Note Generator for Course Materials with OneDrive Shared Links

This script scans PDFs from OneDrive MBA-Resources folder, creates shareable links in OneDrive,
and generates corresponding reference notes (markdown file) in the Obsidian vault. The script 
maintains the same folder structure from OneDrive in your Obsidian Vault.

Features:
- Authenticates with Microsoft Graph API for secure access to OneDrive
- Creates shareable links for PDFs stored in OneDrive
- Generates markdown notes with both local file:// links and shareable OneDrive links
- Extracts text from PDFs and generates AI-powered summaries with OpenAI
- Automatically generates relevant tags using content analysis via OpenAI
- Infers course and program information from file paths
- Maintains consistent folder structure between OneDrive and Obsidian vault
- Robust error handling with categorization and retry mechanism
- Integration with Microsoft Graph API using Azure best practices
- Preserves user modifications to notes (respects auto-generated-state flag)
- Secure token cache management with encryption

Generated Markdown Structure:
The script generates comprehensive markdown notes with the following structure:
1. YAML Frontmatter - Rich metadata including:
   - auto-generated-state: Tracks if note can be auto-updated (default: writable)
   - template-type: Type of template used (pdf-reference)
   - title: PDF title derived from filename
   - pdf-path: Relative path to the PDF in OneDrive
   - onedrive-pdf-path: Direct file:// URL to open PDF locally
   - onedrive-sharing-link: Shareable OneDrive link (when available)
   - date-created: Creation timestamp
   - tags: Auto-generated tags using OpenAI analysis
   - program/course: Inferred from file path structure
   - pdf-uploaded: PDF creation date
   - pdf-size: File size in MB
   - status: Reading status tracking (unread/in-progress/complete)
   - completion-date: When reading was finished (user filled)
   - review-date: When content was reviewed (user filled)
   - comprehension: Self-assessed understanding level (user filled)

2. Content Sections - AI-generated content including:
   - PDF Reference with links to local and OneDrive shared versions
   - Topics Covered: Key topics in bullet point format
   - Key Concepts Explained: Detailed explanations of main concepts
   - Important Takeaways: Practical applications and insights
   - Summary: Concise overview of PDF content
   - Notable Quotes/Insights: Important quotes from the document
   - Questions: Reflection prompts to connect content to broader learning
   - Notes: Section for user's personal notes

Usage:
    wsl python3 generate_pdf_notes_updated.py                       # Process all PDFs in OneDrive
    wsl python3 generate_pdf_notes_updated.py -f "path/to/file.pdf" # Process a single PDF file (relative to OneDrive)
    wsl python3 generate_pdf_notes_updated.py --folder "folder"     # Process PDFs in a specific OneDrive subfolder
    wsl python3 generate_pdf_notes_updated.py --dry-run             # Test without making changes
    wsl python3 generate_pdf_notes_updated.py --no-share-links      # Skip OneDrive shared links (faster)
    wsl python3 generate_pdf_notes_updated.py --debug               # Enable debug logging
    wsl python3 generate_pdf_notes_updated.py --retry-failed        # Only retry previously failed files
    wsl python3 generate_pdf_notes_updated.py --force               # Force overwrite of existing notes
    wsl python3 generate_pdf_notes_updated.py --timeout 15          # Set custom API request timeout (seconds)

Environment Variables:
    OPENAI_API_KEY: API key for OpenAI (required for AI summary and tag generation)

Requirements:
    - requests: For HTTP communication with Microsoft Graph API
    - msal: Microsoft Authentication Library for secure Azure AD authentication
    - pdfplumber: For extracting text from PDFs
    - openai: For AI-powered summary and tag generation
    - pyyaml: For parsing YAML templates and frontmatter
    - python-dotenv: For loading environment variables
    - cryptography: For secure token cache encryption
    - urllib3: For retry strategies and connection pooling

Author: Daniel Shue
Created: April 2025
Last Updated: April 21, 2025
"""
import os
import json
import logging
from urllib.parse import urljoin, quote
from msal import PublicClientApplication
import requests
import yaml
from datetime import datetime, timedelta
from dateutil import parser
from dotenv import load_dotenv
from cryptography.fernet import Fernet
import base64
import hashlib
import re
from pathlib import Path
import argparse
from requests.adapters import HTTPAdapter
from urllib3.util import Retry
from functools import partial, wraps
import pdfplumber
import time
import sys

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

# Load environment variables from .env file
load_dotenv()

# Constants
PDF_EXTENSIONS = {'.pdf'}
RESULTS_FILE = 'pdf_notes_results.json'
FAILED_FILES_JSON = 'failed_files.json'
VAULT_ROOT = Path(normalize_wsl_path(r'/mnt/d/Vault/01_Projects/MBA'))  # Your Obsidian vault root
ONEDRIVE_ROOT = Path(normalize_wsl_path(r'/mnt/c/Users/danielshue/OneDrive/Education/MBA-Resources'))  # PDF source location
ONEDRIVE_BASE = '/Education/MBA-Resources'  # OneDrive path to your MBA Resources (URL path)

# Microsoft Graph API configuration
CLIENT_ID = "489ad055-e4b0-4898-af27-53506ce83db7"  # Using the same app registration as generate_video_links.py
AUTHORITY = "https://login.microsoftonline.com/common"
SCOPES = ["Files.ReadWrite.All", "Sites.Read.All"]
GRAPH_BASE_URL = "https://graph.microsoft.com/v1.0"
TOKEN_CACHE_FILE = "token_cache.bin"

# Configure logging to write to both a file and the console
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler("pdf_notes_generator.log"),
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
    session.request = partial(session.request, timeout=timeout)
    
    return session

def categorize_error(error, status_code=None):
    """
    Categorize errors for better tracking and reporting.
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

# Token cache encryption
def encrypt_cache(data, key=None):
    """Encrypt token cache data using Fernet symmetric encryption."""
    if key is None:
        # Generate a key derived from machine-specific attributes for added security
        machine_id = hashlib.sha256(f"{os.getlogin()}:{os.name}".encode()).digest()
        key = base64.urlsafe_b64encode(machine_id[:32])
    
    try:
        cipher = Fernet(key)
        return cipher.encrypt(json.dumps(data).encode())
    except Exception as e:
        logger.warning(f"Encryption failed: {e}. Falling back to plaintext cache.")
        return json.dumps(data).encode()

def decrypt_cache(data, key=None):
    """Decrypt token cache data using Fernet symmetric encryption."""
    if key is None:
        # Use the same key derivation as in encrypt_cache
        machine_id = hashlib.sha256(f"{os.getlogin()}:{os.name}".encode()).digest()
        key = base64.urlsafe_b64encode(machine_id[:32])
    
    try:
        cipher = Fernet(key)
        return json.loads(cipher.decrypt(data).decode())
    except Exception as e:
        logger.warning(f"Decryption failed: {e}. Trying to load as plaintext.")
        try:
            return json.loads(data.decode())
        except Exception:
            logger.warning("Failed to parse cache as plaintext. Creating new cache.")
            return {}

# Microsoft Graph authentication class
class GraphAPIAuth:
    """
    Handles Microsoft Graph API authentication using MSAL.
    Implements Azure best practices for authentication.
    """
    def __init__(self, client_id, authority, scopes, cache_file=TOKEN_CACHE_FILE):
        self.client_id = client_id
        self.authority = authority
        self.scopes = scopes
        self.cache_file = cache_file
        self.app = None
        self.token = None
        self.session = create_requests_session()
          # Initialize the MSAL application
        self._initialize_app()
        
    def _initialize_app(self):
        """Initialize the MSAL application with the token cache."""
        token_cache = self._load_cache()
        self.app = PublicClientApplication(
            client_id=self.client_id,
            authority=self.authority,
            token_cache=token_cache
        )
    
    def _load_cache(self):
        """Load the token cache from file."""
        from msal.token_cache import SerializableTokenCache
        cache = SerializableTokenCache()
        
        if os.path.exists(self.cache_file):
            try:
                with open(self.cache_file, 'rb') as f:
                    cache_data = f.read()
                    cache.deserialize(decrypt_cache(cache_data))
                logger.debug("Token cache loaded successfully")
            except Exception as e:
                logger.warning(f"Failed to load token cache: {e}")
        
        return cache
    
    def _save_cache(self):
        """Save the token cache to file."""
        if self.app.get_accounts():
            cache_data = encrypt_cache(json.loads(self.app.token_cache.serialize()))
            os.makedirs(os.path.dirname(os.path.abspath(self.cache_file)), exist_ok=True)
            with open(self.cache_file, 'wb') as f:
                f.write(cache_data)
            logger.debug("Token cache saved successfully")
    
    def get_token_silent(self):
        """
        Attempt to get a token silently using the token cache.
        Returns the token or None if silent acquisition fails.
        """
        accounts = self.app.get_accounts()
        if accounts:
            try:
                # Try to get token silently from the cache
                result = self.app.acquire_token_silent(self.scopes, account=accounts[0])
                if result:
                    logger.debug("Token acquired silently from cache")
                    self.token = result
                    return result
            except Exception as e:
                logger.warning(f"Silent token acquisition failed: {e}")
        logger.debug("No token available in cache")
        return None
    
    def get_token_interactive(self):
        """
        Get a token using interactive browser authentication.
        This is compatible with confidential and public client applications.
        Returns the token or raises an exception if authentication fails.
        """
        try:
            import webbrowser
            import platform
            
            # Detect if we're running in WSL
            is_wsl = "microsoft-standard" in platform.uname().release.lower() if hasattr(platform, "uname") else False
            
            # Configure system browser authentication
            if is_wsl:
                # For WSL, we need to tell the user to copy the URL manually
                print("\n⚠️ Running in WSL environment. You'll need to copy the authentication URL manually.")
                print("When prompted, copy the URL from the terminal and paste it into your browser.\n")
                
            # Use the interactive browser auth flow
            result = self.app.acquire_token_interactive(
                scopes=self.scopes,
                prompt="select_account"  # Force prompt to select account
            )
            
            if "access_token" not in result:
                logger.error(f"Authentication failed: {result.get('error')}")
                raise Exception(f"Authentication failed: {result.get('error_description')}")
            
            self.token = result
            self._save_cache()
            return result
        
        except Exception as e:
            logger.error(f"Interactive authentication failed: {e}")
            raise
    
    def get_token(self):
        """
        Get a valid access token using a progressive token acquisition strategy.
        First tries to get a token silently, then falls back to interactive auth.
        Returns the token or raises an exception if all methods fail.
        """
        token = self.get_token_silent()
        if not token:
            logger.info("Silent token acquisition failed, trying interactive auth...")
            token = self.get_token_interactive()
        
        if not token or "access_token" not in token:
            raise Exception("Failed to acquire token through all available methods")
        
        return token
    
    def get_headers(self):
        """
        Get the authorization headers for Microsoft Graph API requests.
        """
        if not self.token or "access_token" not in self.token:
            self.token = self.get_token()
        
        return {
            "Authorization": f"Bearer {self.token['access_token']}",
            "Content-Type": "application/json"
        }

# OneDrive API functions
class OneDriveAPI:
    """
    Handles interactions with Microsoft OneDrive via Graph API.
    """
    def __init__(self, auth, base_url=GRAPH_BASE_URL):
        self.auth = auth
        self.base_url = base_url
        self.session = create_requests_session()

    def get_item_by_path(self, path):
        # Ensure path starts with '/' and ends with ':' for Graph API
        if not path.startswith('/'):
            path = '/' + path
        if not path.endswith(':'):
            path = path + ':'
        url = f"{self.base_url}/me/drive/root:{path}"
        try:
            headers = self.auth.get_headers()
            response = self.session.get(url, headers=headers)
            response.raise_for_status()
            return response.json()
        except Exception as e:
            error_category = categorize_error(e, response.status_code if 'response' in locals() else None)
            logger.error(f"Failed to get item by path '{path}': {e} (Category: {error_category})")
            raise

    def create_sharing_and_embed_link(self, path):
        # Ensure path starts with '/' and ends with ':' for Graph API
        if not path.startswith('/'):
            path = '/' + path
        if not path.endswith(':'):
            path = path + ':'
        result = {
            'sharing_link': None,
            'embed_html': None,
            'success': False
        }
        try:
            logger.debug(f"Creating sharing and embed links for: {path}")
            if path.startswith('/'):
                normalized_path = path[1:]
                logger.debug(f"Removed leading slash from path: {normalized_path}")
            else:
                normalized_path = path
            try:
                item = self.get_item_by_path(normalized_path)
                logger.debug(f"Successfully got item by path: {normalized_path}")
            except Exception as first_err:
                logger.debug(f"Path format issue. Trying alternative format: {first_err}")
                alt_path = '/' + path if not path.startswith('/') else path[1:]
                item = self.get_item_by_path(alt_path)
                logger.debug(f"Successfully got item by alternative path format")
            item_id = item["id"]
            url = f"{self.base_url}/me/drive/items/{item_id}/createLink"
            data = {
                "type": "embed",
                "scope": "anonymous"
            }
            headers = self.auth.get_headers()
            response = self.session.post(url, json=data, headers=headers)
            response.raise_for_status()
            api_response = response.json()
            if "link" in api_response:
                if "webUrl" in api_response["link"]:
                    result['sharing_link'] = api_response["link"]["webUrl"]
                    result['success'] = True
                    logger.debug(f"Created sharing link: {result['sharing_link']}")
                if "webHtml" in api_response["link"]:
                    result['embed_html'] = api_response["link"]["webHtml"]
                    logger.info("Created embed HTML from webHtml property")
                elif result['sharing_link']:
                    embed_url = result['sharing_link']
                    result['embed_html'] = f'<iframe src="{embed_url}" width="800" height="600" frameborder="0" scrolling="no"></iframe>'
                    logger.info("Created fallback embed HTML from sharing URL")
            return result
        except Exception as e:
            logger.warning(f"Failed to create sharing and embed links: {e}")
            return result

# Load templates from YAML file
def load_templates():
    try:
        with open("metadata.yaml", 'r') as f:
            # Load all YAML documents as a list
            templates_list = list(yaml.safe_load_all(f))
        # Only keep dicts (ignore None or other types)
        templates = [doc for doc in templates_list if isinstance(doc, dict)]
        return templates
    except Exception as e:
        logger.error(f"Error loading templates: {e}")
        return None

# Recursively find all PDFs under ONEDRIVE_ROOT
def find_all_pdfs(root):
    return [p for p in root.rglob("*.pdf") if p.is_file()]

# Get the corresponding vault path for a OneDrive PDF path
def get_vault_path_for_pdf(onedrive_pdf_path):
    """
    Calculate where in the Vault to place a note for a PDF from OneDrive.
    Preserves the folder structure from OneDrive in the Vault.
    """
    try:
        # Get the relative path from OneDrive root
        rel_path = onedrive_pdf_path.relative_to(ONEDRIVE_ROOT)
        # Create the same path structure in the Vault
        vault_path = VAULT_ROOT / rel_path
        # Return the directory where the note should be placed (same directory as the PDF would be)
        return vault_path.parent
    except ValueError:
        # If the path is not within OneDrive root, just use its parent directory
        logger.warning(f"Path {onedrive_pdf_path} is not within OneDrive root")
        return onedrive_pdf_path.parent

# Helper: Generate OneDrive sharing and embed links
def get_sharing_and_embed_links(pdf_path, rel_path, onedrive_api, args):
    if not onedrive_api or (args and getattr(args, 'no_share_links', False)):
        return None, None
    try:
        onedrive_path = f"{ONEDRIVE_BASE}/{str(rel_path).replace('\\', '/')}"
        print("  ├─ Generating OneDrive sharing and embed links...")
        link_result = onedrive_api.create_sharing_and_embed_link(onedrive_path)
        sharing_link = link_result.get('sharing_link')
        embed_html = link_result.get('embed_html')
        if sharing_link:
            print(f"  │  └─ Shared link created ✓")
        else:
            print(f"  │  └─ Failed to create shared link ✗")
        return sharing_link, embed_html
    except Exception as e:
        logger.warning(f"Failed to generate sharing/embed link: {e}")
        print(f"  │  └─ Error generating sharing/embed link: {str(e)}")
        return None, None

# Helper: Extract PDF text
def get_pdf_text(pdf_path):
    print(f"  ├─ Extracting PDF text...")
    return extract_pdf_text(pdf_path)

# Helper: Infer course/program metadata
def get_course_program_metadata(pdf_path):
    print(f"  ├─ Inferring course/program metadata...")
    return infer_course_and_program(pdf_path)

# Helper: Generate summary with OpenAI
def get_openai_summary(pdf_text, pdf_stem, sharing_link, pdf_path, course_program_metadata):
    print(f"  ├─ Generating summary and tags with OpenAI...")
    return generate_summary_with_openai(
        pdf_text,
        pdf_name=pdf_stem,
        sharing_link=sharing_link,
        pdf_path=pdf_path,
        course_program_metadata=course_program_metadata
    )

# Helper: Create markdown note
def write_markdown_note(pdf_path, vault_dir, pdf_stem, summary, include_embed, embed_html, dry_run, sharing_link, course_program_metadata):
    print("  ├─ Creating final markdown note...")
    logger.debug(f"Calling create_markdown_note_for_pdf with embed_html: {embed_html}")
    return create_markdown_note_for_pdf(
        pdf_path=pdf_path,
        vault_dir=vault_dir,
        pdf_stem=pdf_stem,
        markdown=summary,
        include_embed=include_embed,
        embed_html=embed_html,
        dry_run=dry_run,
        sharing_link=sharing_link,
        course_program_metadata=course_program_metadata
    )

def process_single_pdf(pdf_path, vault_dir, results_file='pdf_notes_results.json', args=None, onedrive_api=None, dry_run=False, include_embed=True, embed_html=None):
    try:
        pdf_stem = pdf_path.stem
        rel_path = pdf_path.relative_to(ONEDRIVE_ROOT)
        note_name = f"{pdf_stem}-Notes.md"
        os.makedirs(vault_dir, exist_ok=True)
        note_path = vault_dir / note_name

        # Early exit if note exists and not forced
        if note_path.exists() and not (args and getattr(args, 'force', False)):
            logger.info(f"Skipping {pdf_stem}: note already exists and --force not set.")
            print(f"Skipping: {pdf_stem} (note exists, use --force to overwrite)")
            return {
                'file': str(rel_path),
                'note_path': str(note_path),
                'success': True,
                'skipped': True,
                'reason': 'already_exists',
                'modified_date': datetime.now().isoformat()
            }

        # Step 1: Generate sharing and embed links
        sharing_link, embed_html_final = get_sharing_and_embed_links(pdf_path, rel_path, onedrive_api, args)

        # Step 2: Extract PDF text
        pdf_text = get_pdf_text(pdf_path)

        # Step 3: Infer course/program metadata
        course_program_metadata = get_course_program_metadata(pdf_path)

        # Step 4: Generate summary with OpenAI
        summary = get_openai_summary(pdf_text, pdf_stem, sharing_link, pdf_path, course_program_metadata)

        # Step 5: Create markdown note
        note_path = write_markdown_note(
            pdf_path=pdf_path,
            vault_dir=vault_dir,
            pdf_stem=pdf_stem,
            summary=summary,
            include_embed=include_embed,
            embed_html=embed_html_final,
            dry_run=dry_run,
            sharing_link=sharing_link,
            course_program_metadata=course_program_metadata
        )
        logger.info(f"Created markdown note: {note_path}")
        print("  │  └─ Note created at: " + str(note_path).replace(str(VAULT_ROOT), "").lstrip('/\\'))
        result = {
            'file': str(rel_path),
            'onedrive_path': str(pdf_path),
            'note_path': str(note_path),
            'success': True,
            'modified_date': datetime.now().isoformat()
        }
        if sharing_link:
            result['sharing_link'] = sharing_link
        update_results_file(results_file, result)
        print("  └─ Results saved ✓")
        return result
    except Exception as e:
        error_msg = str(e)
        error_category = categorize_error(e)
        logger.error(f"Error processing {pdf_path.name}: {error_msg} (Category: {error_category})")
        print(f"  └─ Error: {error_msg}")
        failed_data = {
            'file': str(pdf_path),
            'relative_path': str(rel_path) if 'rel_path' in locals() else None,
            'error': error_msg,
            'category': error_category,
            'timestamp': datetime.now().isoformat()
        }
        record_failed_file(failed_data)
        return {
            'file': str(pdf_path),
            'success': False,
            'error': error_msg,
            'error_category': error_category,
            'timestamp': datetime.now().isoformat()
        }


# Main function to process PDFs from ONEDRIVE_ROOT
def process_pdfs_for_notes(args=None, folder_path=None, onedrive_api=None, dry_run=False):
    templates = load_templates()
    if not templates:
        logger.warning("Could not load templates from metadata.yaml. Using default templates.")
    else:
        logger.info(f"Successfully loaded {len(templates)} templates from metadata.yaml")

    # Step 1: Determine scan root
    scan_root = get_scan_root(folder_path)
    if scan_root is None:
        return [], []

    print(f"\n🔍 Scanning for PDFs under OneDrive at {scan_root} ...")
    pdf_paths = find_all_pdfs(scan_root)
    total_pdfs = len(pdf_paths)
    logger.info(f"Found {total_pdfs} PDF files in OneDrive at {scan_root}")
    print(f"📄 Found {total_pdfs} PDF files to process\n")

    if total_pdfs == 0:
        print("❗ No PDF files found. Nothing to process.")
        return [], []

    processed_pdfs = []
    errors = []

    # Step 2: Process each PDF
    for index, pdf_path in enumerate(pdf_paths):
        pdf_name = pdf_path.name
        print(f"[{index+1}/{total_pdfs}] Processing: {pdf_name}")
        vault_dir = get_vault_path_for_pdf(pdf_path)
        result = process_single_pdf(pdf_path, vault_dir, RESULTS_FILE, args, onedrive_api, dry_run)
        if result.get('success', False):
            processed_pdfs.append(result)
        else:
            errors.append(result)
        print("")

    # Step 3: Count all processed PDFs (from results file)
    try:
        with open(RESULTS_FILE, 'r') as f:
            final_results = json.load(f)
            all_pdfs_count = len(final_results.get('processed_pdfs', []))
    except Exception:
        all_pdfs_count = len(processed_pdfs)

    # Step 4: Print summary
    print_pdf_processing_summary(processed_pdfs, errors, all_pdfs_count)

    return processed_pdfs, errors

# Helper: Print summary of results
def print_pdf_processing_summary(processed_pdfs, errors, all_pdfs_count):
    print("\n" + "="*60)
    print(f"📊 SUMMARY: PDF Note Generation")
    print("="*60)
    print(f"✅ Processed in this run: {len(processed_pdfs)} PDFs")
    print(f"❌ Errors in this run: {len(errors)}")
    print(f"📝 Total PDFs with notes: {all_pdfs_count}")
    print(f"📄 Full results saved to: {RESULTS_FILE}")
    print("="*60 + "\n")
    logger.info("\n===== Summary =====")
    logger.info(f"Processed {len(processed_pdfs)} PDFs")
    logger.info(f"Total PDFs with notes: {all_pdfs_count}")
    if errors:
        logger.info(f"Encountered {len(errors)} errors:")
        for error in errors:
            logger.info(f"  - {error.get('file')}: {error.get('error')}")

# Helper: Determine scan root folder
def get_scan_root(folder_path):
    scan_root = ONEDRIVE_ROOT
    if folder_path:
        scan_root = Path(folder_path)
        if not scan_root.is_absolute():
            scan_root = ONEDRIVE_ROOT / scan_root
        scan_root = scan_root.resolve()
        if not scan_root.exists() or not scan_root.is_dir():
            print(f"❗ Specified folder does not exist: {scan_root}")
            return None
    return scan_root

def update_results_file(results_file, result):
    """Update the results file with the processing result."""
    try:
        with open(results_file, 'r') as f:
            results = json.load(f)
    except Exception:
        results = {"processed_pdfs": []}

    results["processed_pdfs"].append(result)

    try:
        with open(results_file, 'w') as f:
            json.dump(results, f, indent=4)
    except Exception as e:
        logger.error(f"Error updating results file {results_file}: {e}")

def record_failed_file(failed_data):
    """Record a failed file to the failed_files.json and log."""
    try:
        # Log to the failed files logger
        failed_logger.error(f"Failed: {failed_data['file']} - {failed_data['error']}")
        
        # Load existing failed files
        try:
            with open(FAILED_FILES_JSON, 'r') as f:
                failed_files = json.load(f)
        except Exception:
            failed_files = {"failed_pdfs": []}
        
        # Append the new failure
        failed_files["failed_pdfs"].append(failed_data)
        
        # Save the updated list
        with open(FAILED_FILES_JSON, 'w') as f:
            json.dump(failed_files, f, indent=4)
            
    except Exception as e:
        logger.error(f"Error recording failed file: {e}")

def build_pdf_yaml_frontmatter(pdf_stem, pdf_path, sharing_link=None, program_and_course_metadata=None):
    """
    Build YAML frontmatter for a PDF note using the pdf-reference template.
    Ensures all template fields are present, values are quoted as needed, and dynamic fields are set.
    """
    logger.info(f"Building YAML frontmatter for {pdf_stem}...")
    # Load all templates and select the pdf-reference template
    templates = load_templates()
    template = None
    if templates:
        for t in templates:
            if isinstance(t, dict) and t.get('template-type') == 'pdf-reference':
                template = t.copy()
                break
    if not template:
        template = {}
    logger.debug(f"build_pdf_yaml_frontmatter: using template\n{template}")

    # Start with a copy of the template
    yaml_dict = template.copy()
    # Set dynamic and required fields
    yaml_dict['title'] = pdf_stem
    # Compute OneDrive path for the PDF
    try:
        rel_path = pdf_path.relative_to(ONEDRIVE_ROOT)
        pdf_path_onedrive = f"{ONEDRIVE_BASE}/{rel_path.as_posix()}"
    except Exception:
        pdf_path_onedrive = str(pdf_path)
    yaml_dict['pdf-path'] = pdf_path_onedrive
    # Set or clear sharing-link
    if sharing_link is not None:
        yaml_dict['sharing-link'] = sharing_link
    elif 'sharing-link' in yaml_dict:
        yaml_dict['sharing-link'] = ''
    # Add program and course metadata if provided
    if program_and_course_metadata:
        for k, v in program_and_course_metadata.items():
            if v is not None:
                yaml_dict[k] = v
    # Set standard dynamic fields
    today_str = datetime.now().strftime('%Y-%m-%d')
    yaml_dict['date-created'] = today_str
    yaml_dict['auto-generated-state'] = 'writable'
    yaml_dict['template-type'] = 'pdf-reference'
    yaml_dict['pdf-uploaded'] = today_str
    # Set file size in MB
    try:
        size_bytes = pdf_path.stat().st_size
        size_mb = size_bytes / (1024 * 1024)
        yaml_dict['pdf-size'] = f"{size_mb:.2f} MB"
    except Exception as e:
        logger.warning(f"Could not determine file size for {pdf_path}: {e}")
        yaml_dict['pdf-size'] = ''

    # YAML formatting: quote all scalars except for certain keys
    no_quote_keys = {'date-created', 'auto-generated-state', 'template-type'}
    yaml_lines = ['---']
    # Always include all keys from the template, even if value is None or empty
    for k in template.keys():
        v = yaml_dict.get(k, None)
        if isinstance(v, list):
            yaml_lines.append(f"{k}:")
            for item in v:
                yaml_lines.append(f"  - \"{item}\"")
        elif k in no_quote_keys:
            yaml_lines.append(f"{k}: {v if v is not None else ''}")
        else:
            yaml_lines.append(f"{k}: \"{v if v is not None else ''}\"")
    # Also include any new keys added to yaml_dict that weren't in the template
    for k, v in yaml_dict.items():
        if k not in template:
            if isinstance(v, list):
                yaml_lines.append(f"{k}:")
                for item in v:
                    yaml_lines.append(f"  - \"{item}\"")
            elif k in no_quote_keys:
                yaml_lines.append(f"{k}: {v if v is not None else ''}")
            else:
                yaml_lines.append(f"{k}: \"{v if v is not None else ''}\"")
    yaml_lines.append('---\n')
    final_yaml = '\n'.join(yaml_lines)
    logger.debug(f"build_pdf_yaml_frontmatter: final yaml\n{final_yaml}")
    return final_yaml


def create_markdown_note_for_pdf(
    pdf_path,
    vault_dir,
    pdf_stem,
    markdown=None,
    include_embed=True,
    embed_html=None,
    dry_run=False,
    sharing_link=None,
    course_program_metadata=None
):
    """
    Create and write a markdown note for a PDF, including YAML frontmatter, optional embed, and notes section.
    - If markdown is not provided, generate YAML frontmatter only.
    - Optionally appends an embed section.
    - Preserves or adds a notes section at the end.
    """
    # Step 1: Generate YAML frontmatter if no markdown is provided
    if not markdown:
        markdown = build_pdf_yaml_frontmatter(
            pdf_stem,
            pdf_path,
            sharing_link=sharing_link,
            program_and_course_metadata=course_program_metadata
        )

    # Step 2: Prepare embed section if requested
    embed_section = ""
    if include_embed:
        if embed_html:
            embed_html_str = str(embed_html)
            # Fix width and height attributes to always be 700x500
            embed_html_str = re.sub(r'width="[0-9]+"', 'width="700"', embed_html_str)
            embed_html_str = re.sub(r'height="[0-9]+"', 'height="500"', embed_html_str)
            # Also handle possible single quotes (rare, but robust)
            embed_html_str = re.sub(r"width='[0-9]+'", 'width="700"', embed_html_str)
            embed_html_str = re.sub(r"height='[0-9]+'", 'height="500"', embed_html_str)
            embed_section = "\n\n### PDF Preview\n" + embed_html_str + "\n\n---\n"
        else:
            embed_section = "\n> PDF embed requested, but no embed HTML was generated.\n"

    # Step 3: Prepare notes section (preserve existing notes if present)
    note_path = vault_dir / f"{pdf_stem}-Notes.md"
    notes_header = "## 📝 Notes"
    notes_prompt = "Add your notes about the PDF here."
    notes_section = f"\n{notes_header}\n{notes_prompt}\n"
    existing_notes = ""
    if note_path.exists():
        with open(note_path, 'r', encoding='utf-8') as f:
            existing_content = f.read()
        notes_index = existing_content.find(notes_header)
        if notes_index != -1:
            existing_notes = existing_content[notes_index:]

    # Step 4: Assemble final content
    if notes_header in markdown:
        # If the markdown already contains a notes section, don't append another
        final_content = markdown + embed_section
    else:
        notes_to_add = existing_notes.strip() if existing_notes else notes_section
        final_content = markdown + embed_section + "\n" + notes_to_add

    # Step 5: Write to file unless dry run
    if not dry_run:
        vault_dir.mkdir(parents=True, exist_ok=True)
        with open(note_path, 'w', encoding='utf-8') as f:
            f.write(final_content)
    return note_path

# Extract text from a PDF using pdfplumber
def extract_pdf_text(pdf_path):
    try:
        with pdfplumber.open(pdf_path) as pdf:
            text = "\n".join(page.extract_text() or '' for page in pdf.pages)
        return text.strip()
    except Exception as e:
        logger.error(f"Error extracting text from {pdf_path}: {e}")
        return ""

def chunk_text(text, chunk_size=3000, overlap=200):
    """Split text into overlapping chunks for OpenAI summarization."""
    chunks = []
    start = 0
    text_length = len(text)
    while start < text_length:
        end = min(start + chunk_size, text_length)
        chunk = text[start:end]
        chunks.append(chunk)
        start += chunk_size - overlap
    return chunks

def load_prompt_template(path):
    with open(path, 'r', encoding='utf-8') as f:
        return f.read()

# Generate summary with OpenAI using extracted PDF text
def generate_summary_with_openai(pdf_text, pdf_name="", sharing_link=None, pdf_path=None, course_program_metadata=None):
    """
    Generate a comprehensive markdown summary for a PDF using OpenAI, including YAML frontmatter and all required sections.
    Steps:
    1. Prepare YAML frontmatter and metadata.
    2. Chunk the PDF text for summarization.
    3. Summarize each chunk with OpenAI.
    4. Synthesize a final summary from all chunks.
    5. Return the final markdown summary.
    """
    # Step 1: Prepare YAML frontmatter and metadata
    if pdf_path is None:
        from pathlib import Path
        pdf_path = Path(pdf_name)
    yaml_frontmatter = build_pdf_yaml_frontmatter(
        pdf_name,
        pdf_path=pdf_path,
        sharing_link=sharing_link,
        program_and_course_metadata=course_program_metadata
    )
    logger.debug(f"Using YAML frontmatter:\n{yaml_frontmatter}")
    course_name = course_program_metadata.get('course', 'Unknown Course') if course_program_metadata else 'Unknown Course'

    # Step 2: Validate OpenAI API key and input text
    OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
    if not OPENAI_API_KEY:
        logger.warning("OpenAI API key not set. Cannot generate summary.")
        return None
    if not pdf_text or len(pdf_text) < 100:
        logger.warning("PDF text too short or empty. Cannot generate summary.")
        return None

    try:
        import openai
        client = openai.OpenAI(api_key=OPENAI_API_KEY)
        # Step 3: Chunk the PDF text for summarization
        chunk_size = 3000
        overlap = 200
        chunks = chunk_text(pdf_text, chunk_size=chunk_size, overlap=overlap)
        logger.debug(f"Chunked PDF text into {len(chunks)} chunks.")
        chunk_summaries = []

        # Step 4: Summarize each chunk with OpenAI
        chunk_prompt_path = os.path.join("prompts", "chunk_summary_prompt.md")
        if os.path.exists(chunk_prompt_path):
            chunk_template = load_prompt_template(chunk_prompt_path)
        else:
            chunk_template = (
                "You are an educational content summarizer for MBA course materials.\n"
                "Create a comprehensive summary of the following PDF document chunk from '{pdf_name}' in the course '{course_name}'.\n"
                "Structure your response in markdown format with these exact sections:\n"
                "# 📝 PDF Summary (AI Generated)\n"
                "## 🧩 Topics Covered\n- List 3-5 main topics covered in the chunk\n- Be specific and use bullet points\n"
                "## 📝 Key Concepts Explained\n- Explain the key concepts in 1-2 paragraphs\n- Focus on the most important ideas\n"
                "## ⭐ Important Takeaways\n- List 2-3 important takeaways as bullet points\n- Focus on practical applications and insights\n"
                "---\n\n{chunk}\n"
            )
        for i, chunk in enumerate(chunks):
            system_prompt = chunk_template.format(pdf_name=pdf_name, course_name=course_name, chunk=chunk)
            user_prompt = f"Summarize chunk {i+1} of the extracted text from a PDF titled '{pdf_name}' from the course '{course_name}'."
            logger.debug(f"[Chunk {i+1}] System prompt:\n{system_prompt}")
            logger.debug(f"[Chunk {i+1}] User prompt:\n{user_prompt}")
            response = client.chat.completions.create(
                model="gpt-4.1",
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt}
                ],
                max_tokens=900,
                temperature=0.5
            )
            summary = response.choices[0].message.content
            logger.info(f"Chunk {i+1}/{len(chunks)} summarized.")
            chunk_summaries.append(summary)
        combined_summary_text = "\n\n".join(chunk_summaries)
        logger.debug(f"Combined summary text from all chunks. Length: {len(combined_summary_text)} characters.")

        # Step 5: Synthesize a final summary from all chunks
        final_prompt_path = os.path.join("prompts", "final_summary_prompt.md")
        if os.path.exists(final_prompt_path):
            final_system_prompt = load_prompt_template(final_prompt_path)
            if "{{yaml-frontmatter}}" in final_system_prompt:
                final_system_prompt = final_system_prompt.replace("{{yaml-frontmatter}}", yaml_frontmatter)
        else:
            final_system_prompt = (
                "You are an educational content summarizer for MBA course materials.\n"
                "Your task is to synthesize multiple AI-generated summaries of individual PDF chunks into a single, cohesive summary.\n"
                "The output should be in markdown format and adhere to the structure below. Additionally, extract and populate metadata\n"
                "fields, including a dynamically generated array of relevant tags based on the content.\n"
                "The summary in markdown format with these sections:\n"
                "# 📝 PDF Summary (AI Generated)\n"
                "## 🧩 Topics Covered\n- List 3-5 main topics covered in the PDF\n- Be specific and use bullet points\n"
                "## 📝 Key Concepts Explained\n- Explain the key concepts in 3-5 paragraphs\n- Focus on the most important ideas\n"
                "## ⭐ Important Takeaways\n- List 3-5 important takeaways as bullet points\n- Focus on practical applications and insights\n"
                "## 🧠 Summary\n- Write a concise 1-paragraph summary of the overall PDF content\n"
                "## 💬 Notable Quotes / Insights\n- Include 1-2 significant quotes or key insights from the PDF\n- Format as proper markdown blockquotes using '>' symbol\n"
                "## ❓ Questions\n- What did I learn from this PDF?\n- What's still unclear or needs further exploration?\n- How does this material relate to the broader course or MBA program?\n"
            )
        final_user_prompt = (
            f"These are the summaries of all chunks from the PDF '{pdf_name}' in the course '{course_name}'. "
            f"The following YAML frontmatter contains metadata for the PDF. Please create a single, cohesive summary as instructed.\n\n"
            f"{yaml_frontmatter}\n\n{combined_summary_text}"
        )
        logger.debug(f"[Final] System Prompt for OpenAI:\n{final_system_prompt}")
        logger.debug(f"[Final] User Prompt for OpenAI:\n{final_user_prompt}")
        final_response = client.chat.completions.create(
            model="gpt-4.1",
            messages=[
                {"role": "system", "content": final_system_prompt},
                {"role": "user", "content": final_user_prompt}
            ],
            max_tokens=2000,
            temperature=0.5
        )
        final_summary = final_response.choices[0].message.content
        logger.info("Successfully generated final summary with OpenAI")
        logger.debug(f"Successfully generated final summary with OpenAI:\n\n{final_summary}")
        return final_summary
    except Exception as e:
        logger.error(f"Error generating summary with OpenAI: {e}")
        return None

def infer_course_and_program(path):
    """Attempt to infer course and program from file path."""
    # Use pathlib for all path manipulations in class/course inference
    path = Path(path)
    path_parts = path.parts
    info = {
        'program': 'MBA Program',
        'course': 'Unknown Course',
        'class': 'Unknown Class'
    }
    program_folders = [
        'Value Chain Management',
        'Financial Management',
        'Focus Area Specialization',
        'Strategic Leadership and Management',
        'Managerial Economics and Business Analysis'
    ]
    path_str = str(path).lower()
    # Find program
    for program in program_folders:
        if program.lower() in path_str:
            info['program'] = program
            break
    # Find course: second part after program in path_parts
    for i, part in enumerate(path_parts):
        if part in program_folders and i+1 < len(path_parts):
            info['course'] = path_parts[i+1]
            break
    # Fallback: if course is still unknown, use the second part of the path
    if info['course'] == 'Unknown Course' and len(path_parts) > 1:
        info['course'] = path_parts[1]
    # Class inference logic
    program_found = False
    for i, part in enumerate(path_parts):
        if part in program_folders:
            program_found = True
            if i+1 < len(path_parts):
                info['class'] = path_parts[i+1]
                break
    if info['class'] == "Unknown Class" and info['course'] != "Unknown Course":
        info['class'] = info['course']
    if info['class'] == "Unknown Class":
        class_match = re.search(r'class[\s_-]*(\w+)', path_str, re.IGNORECASE)
        if class_match:
            info['class'] = f"Class {class_match.group(1)}"
    if info['class'] == "Unknown Class":
        for part in path_parts:
            if "class" in part.lower() or "course" in part.lower():
                potential_class = part.replace('-', ' ').replace('_', ' ').title()
                info['class'] = potential_class
                break
    if info['class'] == "Unknown Class" and info['course'] != "Unknown Course":
        info['class'] = info['course']
    if info['course'] == 'Unknown Course':
        for part in path_parts:
            clean_part = part.replace('_', ' ').replace('-', ' ').title()
            if any(keyword in clean_part.lower() for keyword in ['accounting', 'economics', 'finance', 'marketing', 'operations', 'management']):
                info['course'] = clean_part
                break
    return info

# Keep your existing parse_arguments function
def parse_arguments():
    parser = argparse.ArgumentParser(
        description="Generate Obsidian notes for MBA PDFs in OneDrive with shareable links."
    )
    group = parser.add_mutually_exclusive_group()
    group.add_argument(
        "-f", "--single-file",
        help="Process only a single file (path relative to OneDrive base path)"
    )
    group.add_argument(
        "--folder",
        help="Process all PDFs in a specific subfolder (relative to OneDrive root or absolute path)"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Perform a dry run without creating actual notes"
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
        help="Force processing of PDFs that already have notes (overwrite existing)"
    )
    parser.add_argument(
        "--timeout",
        type=int,
        default=10,
        help="Set API request timeout in seconds (default: 10)"
    )      
    parser.add_argument(
        "--no-share-links",
        action="store_true",
        help="Skip creating OneDrive shared links (faster processing)"
    )
    parser.add_argument(
        "--include-embed",
        action="store_true",
        help="Include OneDrive embed iframe in the generated markdown"
    )
    return parser.parse_args()

# Main function to be added to generate_pdf_notes_updated.py
if __name__ == "__main__":
    print("Starting PDF note generation process...")
    args = parse_arguments()
    
    if args.debug:
        logger.setLevel(logging.DEBUG)
        logger.debug("Debug logging enabled")
      # Initialize OneDrive API client (skip if in dry-run mode or no-share-links)
    onedrive_api = None
    if args.dry_run:
        print("🔍 DRY RUN MODE: Skipping OneDrive API initialization")
        logger.info("DRY RUN: Skipping OneDrive API initialization")
    elif not args.no_share_links:
        try:
            print("Initializing Microsoft Graph API client...")
            # Create authentication manager
            auth = GraphAPIAuth(CLIENT_ID, AUTHORITY, SCOPES)
            # Create OneDrive API client
            onedrive_api = OneDriveAPI(auth)
            print("✅ Microsoft Graph API client initialized")
        except Exception as e:
            logger.error(f"Failed to initialize Microsoft Graph API client: {e}")
            print(f"❌ Failed to initialize Microsoft Graph API client: {e}")
            print("Will continue without generating sharing links.")
            onedrive_api = None
    else:
        print("🔧 Skipping OneDrive shared links generation as requested (--no-share-links)")
    
    if args.dry_run:
        print("🔍 DRY RUN MODE: No actual notes will be created")
        logger.info("Running in DRY RUN mode - no actual files will be modified")
    
    if args.retry_failed:
        print("🔄 Retrying previously failed files...")
        try:
            with open(FAILED_FILES_JSON, 'r') as f:
                failed_files = json.load(f).get("failed_pdfs", [])
                
            if not failed_files:
                print("No failed files found to retry.")
            else:
                print(f"Found {len(failed_files)} failed files to retry.")
                for failed_file in failed_files:
                    file_path = failed_file.get('file')
                    if file_path:
                        pdf_path = Path(file_path)
                        if pdf_path.exists() and pdf_path.suffix.lower() == '.pdf':
                            vault_dir = get_vault_path_for_pdf(pdf_path)
                            print(f"Processing previously failed file: {pdf_path.name}")
                            process_single_pdf(pdf_path, vault_dir, RESULTS_FILE, args, onedrive_api)
                        else:
                            print(f"Failed file not found or not a PDF: {file_path}")
        except Exception as e:
            print(f"Error reading or processing failed files: {e}")
    
    elif args.single_file:
        print(f"Processing single file: {args.single_file}")
        # Use OneDrive path for single file
        pdf_path = ONEDRIVE_ROOT / args.single_file
        if pdf_path.exists() and pdf_path.suffix.lower() == '.pdf':
            # Get the correct vault directory for this PDF
            vault_dir = get_vault_path_for_pdf(pdf_path)
            process_single_pdf(pdf_path, vault_dir, RESULTS_FILE, args, onedrive_api)
        else:
            print(f"File not found or not a PDF: {pdf_path}")
    
    elif args.folder:
        print(f"Processing all PDFs in folder: {args.folder}")
        processed_pdfs, errors = process_pdfs_for_notes(args, folder_path=args.folder, onedrive_api=onedrive_api, dry_run=args.dry_run)
        if errors:
            print("\nSome files failed processing. To retry them later, run:")
            print(f"python3 {os.path.basename(__file__)} --retry-failed")
    
    else:
        processed_pdfs, errors = process_pdfs_for_notes(args, onedrive_api=onedrive_api,dry_run=args.dry_run)
        if errors:
            print("\nSome files failed processing. To retry them later, run:")
            print(f"python3 {os.path.basename(__file__)} --retry-failed")
    
    print("Process completed.")
