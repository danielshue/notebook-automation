#!/usr/bin/env python3
"""
Main orchestration module for processing educational content.

This module coordinates the overall workflow for:
- Processing individual videos and PDFs
- Batch processing multiple course materials
- Retrying failed operations
- Command-line interface
- PDF and video content summarization
"""

import os
import json
import logging
import argparse
from pathlib import Path
from datetime import datetime

from tools.utils.config import setup_logging, RESOURCES_ROOT, VAULT_ROOT, VIDEO_EXTENSIONS
from tools.utils.http_utils import create_requests_session, check_api_health
from tools.utils.error_handling import update_failed_files, update_results_file, categorize_error, ErrorCategories
from tools.auth.microsoft_auth import authenticate_graph_api
from tools.onedrive.file_operations import (
    get_file_in_onedrive, create_share_link, get_onedrive_items, 
    find_file_in_onedrive, check_if_file_exists
)
from tools.transcript.processor import find_transcript_file, get_transcript_content
from tools.notes.video_markdown_generator import create_markdown_note_for_video, extract_metadata_from_path
from tools.ai.summarizer import generate_summary_with_openai
from tools.pdf.processor import extract_pdf_text, infer_course_and_program
from tools.pdf.note_generator import create_markdown_note_for_pdf
from tools.ai.summarizer import generate_summary_with_openai

logger = logging.getLogger(__name__)
failed_logger = logging.getLogger("failed_files")

def process_single_video(item, headers, templates, results_file='video_links_results.json', args=None):
    """
    Process a single video through its complete lifecycle.
    
    Args:
        item (dict): Video item metadata from OneDrive
        headers (dict): Authentication headers
        templates (dict): Templates for note generation
        results_file (str): Path to the results file
        args (Namespace): Command line arguments
        
    Returns:
        dict: Result of processing
    """
    try:
        item_path = item.get('parentReference', {}).get('path', '') + '/' + item['name']
        
        # Extract relative path from OneDrive path
        if '/root:/' in item_path:
            rel_path = item_path.split('/root:/')[1]
        else:
            rel_path = item['name']  # Fall back to just the filename
        
        logger.info(f"Processing video: {rel_path}")
        
        # Create session for connection pooling
        session = create_requests_session()
        
        # Determine the local path to the video file
        # Fix: Strip 'Education/MBA-Resources/' prefix from rel_path if present
        rel_path_clean = rel_path
        if rel_path_clean.startswith('Education/MBA-Resources/'):
            rel_path_clean = rel_path_clean[len('Education/MBA-Resources/'):]
        rel_path_clean = rel_path_clean.lstrip('/')
        local_path = RESOURCES_ROOT / rel_path_clean.replace('/', os.path.sep)
        # Calculate note path early to check if it exists
        video_name = local_path.stem
        note_name = f"{video_name}-video.md"
        try:
            rel_path_for_vault = local_path.relative_to(RESOURCES_ROOT)
        except ValueError:
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

def process_videos_for_sharing(args=None):
    """
    Process all video files in the OneDrive collection.
    
    Args:
        args (Namespace): Command line arguments
        
    Returns:
        tuple: (processed_videos, errors)
    """
    # Configure timeout from args if provided
    timeout = getattr(args, 'timeout', 15) if args else 15
    session = create_requests_session(timeout=timeout)
    
    # Step 0: Authenticate with Microsoft Graph API
    try:
        print("🔑 Authenticating with Microsoft Graph API...")
        # Use the --refresh-auth flag to force refresh the token cache
        access_token = authenticate_graph_api(force_refresh=getattr(args, 'refresh_auth', False) if args else False)
        headers = {"Authorization": f"Bearer {access_token}"}
        logger.info("Successfully authenticated with Microsoft Graph API")
        print("✅ Authentication successful\n")
        
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
        from tools.utils.config import ONEDRIVE_BASE
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
    video_items = [
        item for item in onedrive_items if 
        item.get('file', {}).get('mimeType', '').startswith('video/') or
        (item.get('name', '').lower().endswith(tuple(VIDEO_EXTENSIONS)))
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
        # Extract relative path from OneDrive path
        if item_path and '/root:/' in item_path:
            rel_path = item_path.split('/root:/')[1]
        else:
            rel_path = item['name']  # Fall back to just the filename
        local_path = RESOURCES_ROOT / rel_path.replace('/', os.path.sep)
        note_name = f"{local_path.stem}-video.md"
        try:
            rel_path_for_vault = local_path.relative_to(RESOURCES_ROOT)
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
        result = process_single_video(item, headers, {}, results_file, args)
        
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

def process_single_file_by_path(file_path, headers, templates, results_file='video_links_results.json', args=None):
    """
    Process a single video file by its path within OneDrive.
    
    Args:
        file_path (str): Path to the file
        headers (dict): Authentication headers
        templates (dict): Templates for note generation
        results_file (str): Path to results file
        
    Returns:
        dict: Result of processing
    """
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
    return process_single_video(item, headers, templates, results_file, args)


def process_single_file_by_onedrive_path(file_path, headers, templates, results_file='video_links_results.json', args=None):
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
        if not any(file_name.lower().endswith(ext) for ext in VIDEO_EXTENSIONS):
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
    return process_single_video(item, headers, templates, results_file, args)


def process_path(path, headers, templates, results_file='video_links_results.json', args=None):
    """
    Process a single file or all video files in a directory (recursively).
    Accepts both absolute and relative paths, and normalizes them to the correct base.
    
    The path can be:
    1. A path relative to the OneDrive base path
    2. An absolute path in the local filesystem
    3. A full path including the OneDrive base path
    
    Args:
        path: Path to process
        headers: Authentication headers
        templates: Loaded templates for note generation
        results_file: Path to results file
        args: Command line arguments
        
    Returns:
        tuple: (processed, errors)
    """
    # The path might be passed as a string with a leading slash from the command line
    # We need to handle this case specially for OneDrive paths
    if isinstance(path, str) and path.startswith('/'):
        # This is likely a path relative to OneDrive base
        # First, try to see if this is an OneDrive path that needs to be processed directly
        logger.info(f"Processing path that starts with slash: {path}")
        result = process_single_file_by_onedrive_path(path.lstrip('/'), headers, templates, results_file, args)
        if result:
            return [result] if result.get('success', False) else [], [result] if not result.get('success', False) else []

    # Standard path handling for local files
    path = Path(path)
    if not path.is_absolute():
        abs_path = (RESOURCES_ROOT / path).resolve()
    else:
        abs_path = path.resolve()
    
    processed = []
    errors = []
    
    if abs_path.is_file():
        result = process_single_file_by_path(str(abs_path), headers, templates, results_file, args)
        if result and result.get('success', False):
            processed.append(result)
        else:
            errors.append(result)
    elif abs_path.is_dir():
        for file in abs_path.rglob('*'):
            if file.suffix.lower() in VIDEO_EXTENSIONS:
                result = process_single_file_by_path(str(file), headers, templates, results_file, args)
                if result and result.get('success', False):
                    processed.append(result)
                else:
                    errors.append(result)
    else:
        logger.error(f"Path not found: {path}")
        print(f"Path not found: {path}")
    
    return processed, errors

def retry_failed_files(headers, templates, results_file='video_links_results.json'):
    """
    Retry processing files that previously failed.
    
    Args:
        headers: Auth headers for API calls
        templates: Loaded templates for note generation
        results_file: Path to results file
        
    Returns:
        tuple: (processed_count, error_count)
    """
    failed_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "failed_files.json")
    
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
    
    print(f"Processed: {processed}, Errors: {errors}")
    return processed, errors

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Notebook Automation Tool - Video Note Generation")
    parser.add_argument('--convert', action='store_true', help='Convert files (HTML/TXT to Markdown)')
    parser.add_argument('--generate-index', action='store_true', help='Generate index files')
    parser.add_argument('--all', action='store_true', help='Run all operations')
    parser.add_argument('--source', type=str, required=False, help='Source directory for processing')
    parser.add_argument('--debug', action='store_true', help='Enable debug logging')
    parser.add_argument('--force', action='store_true', help='Force overwrite of existing notes')
    parser.add_argument('--retry-failed', action='store_true', help='Retry only previously failed files')
    parser.add_argument('--timeout', type=int, default=15, help='Set API request timeout (seconds)')
    args = parser.parse_args()

    # Setup logging with debug flag
    logger, failed_logger = setup_logging(debug=args.debug)
    if args.debug:
        logger.debug(f"Debug logging enabled via --debug flag")
        print("🐛 Debug logging enabled (logger level: DEBUG)")

    # Example: Run video processing for sharing (expand as needed)
    if args.retry_failed:
        print("Retrying failed files...")
        retry_failed_files({}, {}, results_file='video_links_results.json')
    elif args.convert or args.all:
        print("Processing videos for sharing...")
        process_videos_for_sharing(args)
    else:
        print("No operation specified. Use --convert, --all, or --retry-failed.")
