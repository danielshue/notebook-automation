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
    wsl python3 generate_video_meta_refactored.py                       # Process all videos
    wsl python3 generate_video_meta_refactored.py -f "path/to/file.mp4" # Process single file
    wsl python3 generate_video_meta_refactored.py --dry-run             # Test without making changes
    wsl python3 generate_video_meta_refactored.py --no-summary          # Skip OpenAI summary generation
"""

import os
import sys
import json
import argparse
from pathlib import Path
from datetime import datetime
import traceback
import re
import time

# Import from tools package
from notebook_automation.tools.utils.config import (setup_logging, VAULT_LOCAL_ROOT, ONEDRIVE_LOCAL_RESOURCES_ROOT, 
                               GRAPH_API_ENDPOINT, VIDEO_EXTENSIONS, ONEDRIVE_BASE)
from notebook_automation.tools.utils.error_handling import update_failed_files, update_results_file, categorize_error, ErrorCategories
from notebook_automation.tools.auth.microsoft_auth import authenticate_graph_api
from notebook_automation.tools.onedrive.file_operations import create_share_link
from notebook_automation.tools.notes.note_markdown_generator import create_or_update_markdown_note
from notebook_automation.tools.transcript.processor import find_transcript_file, get_transcript_content
from notebook_automation.tools.metadata.path_metadata import extract_metadata_from_path, load_metadata_templates, infer_course_and_program
from notebook_automation.tools.utils.file_operations import find_files_by_extension

# Constants
RESULTS_FILE = 'generate_video_notes_results.json'
FAILED_FILES_JSON = 'generate_video_notes_failed_files.json'

# Initialize loggers as global variables to be populated in main()
logger = None
failed_logger = None

def _record_failed_file(failed_data):
    """Record a failed file to the generate_video_notes_failed_files.json and log."""
    # Ensure failed_data is a dictionary
    if isinstance(failed_data, str):
        failed_data = {
            'file': failed_data,
            'success': False,
            'error': 'Unknown error',
            'error_category': 'unknown_error',
            'timestamp': datetime.now().isoformat()
        }
        
    if failed_logger:
        # Use dictionary access with get() for safe access to dictionary values
        if isinstance(failed_data, dict):
            failed_logger.error(
                f"Failed: {failed_data.get('file', 'unknown_file')} - Error: {failed_data.get('error', 'Unknown error')} - "
                f"Category: {failed_data.get('error_category', 'unknown_error')}"
            )
        else:
            # Fallback for unexpected types
            failed_logger.error(f"Failed: {failed_data}")
    
    # Update the failed files record
    # Extract relevant information from failed_data dictionary
    if isinstance(failed_data, dict):
        file_info = {
            'file': failed_data.get('file', 'unknown'),
            'path': failed_data.get('path', '')
        }
        error = failed_data.get('error', 'Unknown error')
        error_category = failed_data.get('error_category', ErrorCategories.UNKNOWN)
        update_failed_files(file_info, error, error_category)
    else:
        # Handle string case
        update_failed_files({'file': str(failed_data)}, 'Unknown error', ErrorCategories.UNKNOWN)
    
    return failed_data

def find_file_in_onedrive(file_path, access_token):
    """
    Find a file in the local OneDrive folder structure by its path.
    Uses the local file system instead of making API calls to OneDrive.
    
    Args:
        file_path: Path to the file in OneDrive (relative or absolute)
        access_token: Valid access token for Microsoft Graph API (not used for searching,
                     only included for compatibility with the function signature)
        
    Returns:
        Dict with file metadata if found, None otherwise
    """
    logger.info(f"Looking for file locally at path: {file_path}")
    
    # Normalize path - replace backslashes with forward slashes
    file_path = Path(file_path.replace("\\", "/").strip(" /\\"))
    
    # If the path is already absolute, use it directly
    if file_path.is_absolute():
        local_file_path = file_path
    else:
        # If path is relative to ONEDRIVE_BASE, combine with ONEDRIVE_LOCAL_RESOURCES_ROOT
        if not str(file_path).lower().startswith(ONEDRIVE_BASE.lower()):
            # Need to add the base path
            local_file_path = ONEDRIVE_LOCAL_RESOURCES_ROOT / file_path
        else:
            # The path already includes the base, extract the relative part
            relative_path = str(file_path)
            if relative_path.lower().startswith(ONEDRIVE_BASE.lower()):
                relative_path = relative_path[len(ONEDRIVE_BASE):].lstrip('/')
            local_file_path = ONEDRIVE_LOCAL_RESOURCES_ROOT / relative_path
    
    logger.info(f"Local file path: {local_file_path}")
    
    # Check if the file exists
    if local_file_path.exists() and local_file_path.is_file():
        logger.debug(f"File exists locally: {local_file_path}")
        
        # Get basic file metadata
        file_name = local_file_path.name
        file_size = local_file_path.stat().st_size
        last_modified = datetime.fromtimestamp(local_file_path.stat().st_mtime)
        
        # Determine MIME type based on file extension
        mime_type = "video/mp4"  # Default for MP4
        if file_name.lower().endswith('.mov'):
            mime_type = "video/quicktime"
        elif file_name.lower().endswith('.avi'):
            mime_type = "video/x-msvideo"
        elif file_name.lower().endswith('.wmv'):
            mime_type = "video/x-ms-wmv"
        elif file_name.lower().endswith('.webm'):
            mime_type = "video/webm"
        
        # Construct a metadata dictionary similar to what the Graph API would return
        metadata = {
            'name': file_name,
            'size': file_size,
            'lastModifiedDateTime': last_modified.isoformat(),
            'id': str(local_file_path),  # Use path as ID (it's only used for share link creation)
            'file': {
                'mimeType': mime_type
            },
            'parentReference': {
                'path': str(local_file_path.parent).replace(str(ONEDRIVE_LOCAL_RESOURCES_ROOT), ONEDRIVE_BASE)
            },
            'localPath': str(local_file_path)  # Additional field for local path
        }
        
        return metadata
    
    # If file not found at the given path, try searching by filename
    file_name = os.path.basename(str(file_path))
    logger.info(f"File not found at expected path. Searching by filename: {file_name}")
    
    # Search for the file in the OneDrive local directory
    matching_files = []
    for root, _, files in os.walk(ONEDRIVE_LOCAL_RESOURCES_ROOT):
        for name in files:
            if name == file_name:
                found_path = Path(os.path.join(root, name))
                matching_files.append(found_path)
    
    if matching_files:
        # Use the first match
        match_path = matching_files[0]
        logger.info(f"Found file locally: {match_path}")
        
        # Get file metadata
        file_size = match_path.stat().st_size
        last_modified = datetime.fromtimestamp(match_path.stat().st_mtime)
        
        # Determine MIME type
        mime_type = "video/mp4"  # Default for MP4
        if file_name.lower().endswith('.mov'):
            mime_type = "video/quicktime"
        elif file_name.lower().endswith('.avi'):
            mime_type = "video/x-msvideo"
        elif file_name.lower().endswith('.wmv'):
            mime_type = "video/x-ms-wmv"
        elif file_name.lower().endswith('.webm'):
            mime_type = "video/webm"
        
        # Construct metadata
        metadata = {
            'name': file_name,
            'size': file_size,
            'lastModifiedDateTime': last_modified.isoformat(),
            'id': str(match_path),  # Use path as ID
            'file': {
                'mimeType': mime_type
            },
            'parentReference': {
                'path': str(match_path.parent).replace(str(ONEDRIVE_LOCAL_RESOURCES_ROOT), ONEDRIVE_BASE)
            },
            'localPath': str(match_path)  # Additional field for local path
        }
        
        return metadata
    
    logger.error(f"File not found locally: {file_path}")
    return None

def process_single_file_by_path(file_path, access_token, templates, results_file=RESULTS_FILE, args=None):
    """Process a single video file by its local path."""
    # Get the file metadata from the local file system
    item = find_file_in_onedrive(file_path, access_token)
    
    if not item:
        error_msg = f"File not found locally: {file_path}"
        logger.error(error_msg)
        print(f"❌ {error_msg}")
        return {
            'file': file_path,
            'success': False,
            'error': error_msg,
            'error_category': ErrorCategories.NOTFOUND,
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
                'error_category': ErrorCategories.INVALID_FILE,
                'timestamp': datetime.now().isoformat()
            }
    
    # Process the file
    print(f"🎬 Processing video: {item.get('name')}")
    return process_single_video(item, access_token, templates, results_file, args)

def process_single_video(item, access_token, templates, results_file=RESULTS_FILE, args=None):
    """
    Process a single video file from OneDrive.
    This function:
    1. Creates a shareable link for the video if needed
    2. Determines where to create the note in the Obsidian vault
    3. Finds an associated transcript if available
    4. Generates a summary with OpenAI if a transcript is found and not disabled
    5. Creates/updates the markdown note in the Obsidian vault
    
    Args:
        item: OneDrive item metadata
        access_token: Microsoft Graph API access token
        templates: Templates for note generation
        results_file: File to store processing results
        args: Command line arguments
        
    Returns:
        dict: Result of the processing operation
    """
    video_name = item.get('name', '')
    file_path = item.get('parentReference', {}).get('path', '').replace('/drive/root:/', '')
    
    if file_path:
        file_path = f"{file_path}/{video_name}"
    else:
        file_path = video_name
    
    logger.info(f"Processing video: {file_path}")
    
    share_result = None

    try:
        # Create header for API calls
        headers = {"Authorization": f"Bearer {access_token}"}
        
        # Skip share link creation if option is set
        share_link = None
        if not (args and getattr(args, 'no_share_links', False)):
            try:
                logger.info(f"Creating shareable link for {video_name}")
                
                # Make sure the path includes ONEDRIVE_BASE for OneDrive API
                # The path needs to be relative to the root of OneDrive
                if isinstance(file_path, str):
                    # Clean up the path and remove any leading/trailing slashes
                    onedrive_path = str(file_path).replace('\\', '/').strip('/')
                    
                    # Parse ONEDRIVE_BASE into components for comparison
                    onedrive_base_clean = ONEDRIVE_BASE.strip('/')
                    base_parts = onedrive_base_clean.split('/')
                    
                    # We need at least one component to work with
                    if len(base_parts) > 0:
                        # Get the last component of the base path (e.g., "MBA-Resources" from "Education/MBA-Resources")
                        last_base_component = base_parts[-1]
                        
                        # Check if path already contains the last component to avoid duplication
                        if last_base_component in onedrive_path:
                            # If the path already has the last component, make sure it has the full base path
                            if not any(onedrive_path.startswith(prefix) for prefix in [onedrive_base_clean, onedrive_base_clean.lstrip('/')]):
                                # Extract everything from the last component onwards
                                component_index = onedrive_path.find(last_base_component)
                                if component_index >= 0:
                                    # Replace with the proper base path
                                    onedrive_path = f"{onedrive_base_clean}/{onedrive_path[component_index + len(last_base_component):].lstrip('/')}"
                        else:
                            # Path doesn't have the base component at all, simply prepend the base path
                            onedrive_path = f"{onedrive_base_clean}/{onedrive_path}"
                    
                    logger.debug(f"Using OneDrive path for sharing: {onedrive_path}")
                    logger.debug(f"ONEDRIVE_BASE is: {ONEDRIVE_BASE}")
                else:
                    onedrive_path = item.get('id')
                    logger.debug(f"Using item ID for sharing: {onedrive_path}")

                share_link = create_share_link(onedrive_path, headers)
                    
                logger.info(f"Shareable link created: {share_link[:60] if share_link else 'None'}...")
                
            except Exception as e:
                error_category = categorize_error(e)
                logger.warning(f"Could not create shareable link: {e} ({error_category})")
                # Continue without the shareable link
                pass
        else:
            logger.info("Skipping share link creation as requested")
        
        # Extract metadata from the file path
        path_parts = file_path.split('/')
        logger.info(f"Extracting metadata from path parts: {path_parts}")
        
        # Pass the full path string, not the path_parts array
        metadata = extract_metadata_from_path(file_path)
        
        # Further enhance metadata extraction for modules and lessons from file paths
        # Example path: Value Chain Management/Managerial Accounting Business Decisions/accounting-for-managers/01_course-overview-and-introduction-to-managerial-accounting/01_about-the-course-and-your-classmates/learn-on-your-terms.mp4
        try:
            # Make sure path_parts is properly populated
            if isinstance(file_path, str):
                path_parts = file_path.split('/')
                # Remove any empty parts
                path_parts = [part for part in path_parts if part]
            
            # Try to extract more detailed course structure information
            if len(path_parts) >= 5:
                # Parse ONEDRIVE_BASE to identify the structure to look for
                onedrive_base_clean = ONEDRIVE_BASE.strip('/')
                base_parts = onedrive_base_clean.split('/')
                
                # Check if the path matches our expected OneDrive structure
                if len(base_parts) >= 2 and all(part in path_parts for part in base_parts):
                    # Find the position of the last base component
                    last_base_component = base_parts[-1]
                    if last_base_component in path_parts:
                        resources_index = path_parts.index(last_base_component)
                        if len(path_parts) > resources_index + 2:
                            # Program is typically right after the base path
                            if 'program' not in metadata or not metadata['program']:
                                metadata['program'] = path_parts[resources_index + 1]
                            
                            # Course is typically two directories after the base path
                            if 'course' not in metadata or not metadata['course']:
                                metadata['course'] = path_parts[resources_index + 2]
                else:
                    # Fallback to original method for non-standard paths
                    if 'program' not in metadata or not metadata['program']:
                        metadata['program'] = path_parts[0]
                        
                    if 'course' not in metadata or not metadata['course']:
                        metadata['course'] = path_parts[1]
                
                # Extract module (folder with numbering like 01_module-name)
                module_part = path_parts[-3] if len(path_parts) >= 3 else None
                if module_part:
                    # Clean up module name (remove numbering prefix, replace hyphens with spaces)
                    clean_module = re.sub(r'^\d+[_-]', '', module_part).replace('-', ' ').replace('_', ' ').title()
                    metadata['module'] = clean_module
                
                # Extract lesson (folder with numbering like 01_lesson-name)
                lesson_part = path_parts[-2] if len(path_parts) >= 2 else None
                if lesson_part:
                    # Clean up lesson name (remove numbering prefix, replace hyphens with spaces)
                    clean_lesson = re.sub(r'^\d+[_-]', '', lesson_part).replace('-', ' ').replace('_', ' ').title()
                    metadata['lesson'] = clean_lesson
        except Exception as e:
            logger.warning(f"Error during enhanced metadata extraction: {e}")
        
        logger.info(f"Extracted metadata: {metadata}")
        
        program_name = metadata.get('program', '')
        course_name = metadata.get('course', '')
        
        # Check for a matching transcript file
        transcript_content = None
        transcript_path = None
        
        try:
            # Get the complete file path to use for finding the transcript
            local_file_path = Path(item.get('localPath', ''))
            
            # If we have a local file path that exists, use it to find the transcript
            if local_file_path and local_file_path.exists():
                logger.info(f"Searching for transcript using full video path: {local_file_path}")
                transcript_path = find_transcript_file(local_file_path, ONEDRIVE_LOCAL_RESOURCES_ROOT)
            else:
                # Try using the file path constructed from parent reference instead
                try:
                    complete_path = Path(ONEDRIVE_LOCAL_RESOURCES_ROOT) / file_path.lstrip('/')
                    logger.info(f"Trying alternative path for transcript search: {complete_path}")
                    transcript_path = find_transcript_file(complete_path, ONEDRIVE_LOCAL_RESOURCES_ROOT)
                except Exception as path_error:
                    logger.warning(f"Error with alternative path: {path_error}")
                    # Fall back to just using the video name as a last resort
                    logger.info(f"Falling back to video name only for transcript search: {video_name}")
                    transcript_path = find_transcript_file(video_name, ONEDRIVE_LOCAL_RESOURCES_ROOT)
            
            if transcript_path:
                logger.info(f"Found transcript at: {transcript_path}")
                transcript_content = get_transcript_content(transcript_path)
                
                if transcript_content:
                    logger.info(f"Transcript content extracted: {len(transcript_content)} chars: {transcript_content[:200]}...")
                else:
                    logger.warning(f"Transcript found but content extraction failed")
            else:
                logger.info(f"No transcript found for {video_name}")
        except Exception as e:
            logger.warning(f"Error finding transcript: {e}")
            transcript_path = None
            transcript_content = None
        
        
        # Create or update the markdown note
        dry_run = args and getattr(args, 'dry_run', False)
        force = args and getattr(args, 'force', False)

        # Generate summary if transcript is available and not disabled
        summary = None
        video_path = None
        rel_path = None        
        try:
            # Get the local file path
            local_file_path = Path(item.get('localPath', file_path))
            
            # Calculate the vault directory path that mirrors the OneDrive structure
            # This creates a directory structure in the vault that matches the OneDrive structure
            try:
                # Get the relative path from OneDrive root
                if str(local_file_path).startswith(str(ONEDRIVE_LOCAL_RESOURCES_ROOT)):
                    rel_path = local_file_path.relative_to(ONEDRIVE_LOCAL_RESOURCES_ROOT)
                    logger.info(f"Video path is relative to ONEDRIVE_LOCAL_RESOURCES_ROOT: {rel_path}")
                else:
                    # If it's not a direct file path, try to extract from the relative path
                    # Convert string path to a Path object
                    path_parts = file_path.split('/')
                    # Remove any empty parts and reconstruct
                    clean_path = '/'.join([part for part in path_parts if part])
                    rel_path = Path(clean_path)
                    logger.info(f"Using relative path from file_path: {rel_path}")
                
                # Create the vault directory path mirroring the OneDrive structure
                vault_dir = VAULT_LOCAL_ROOT
                if rel_path and rel_path.parent:
                    vault_dir = VAULT_LOCAL_ROOT / rel_path.parent
                    logger.info(f"Created vault dir path that mirrors OneDrive: {vault_dir}")
            except Exception as e:
                logger.warning(f"Error determining vault directory path: {e}, using VAULT_LOCAL_ROOT instead")
                vault_dir = VAULT_LOCAL_ROOT
                        
                        
            # Create the vault directory if it doesn't exist
            # Add the transcript link to metadata (convert Path to string if it exists)
            if transcript_path:
                metadata["transcript_link"] = str(transcript_path)
                logger.info(f"Added transcript_link to metadata: {str(transcript_path)}")
            else:
                metadata["transcript_link"] = None
                logger.info("No transcript_link available to add to metadata")
            
            metadata["onedrive-sharing-link"] = share_link
            
            # Use the video_markdown_generator to create the note
            note_path = create_or_update_markdown_note(text_to_summarize=transcript_content,
                file_path=local_file_path,
                friendly_filename=video_name,
                sharing_link=share_link,
                vault_dir=vault_dir,
                template_type="video-reference",
                course_program_metadata=metadata,
                dry_run=dry_run
            )
            if note_path:
                logger.info(f"Note created/updated at: {note_path}")
                result = {
                    'file': file_path,
                    'success': True,
                    'note_path': str(note_path),
                    'has_transcript': bool(transcript_content),
                    'has_summary': bool(summary),
                    'has_share_link': bool(share_link),
                    'timestamp': datetime.now().isoformat()
                }
                update_results_file(results_file, result)
                print(f"✅ {video_name} -> {note_path}")
                return result
            else:
                error_msg = "Failed to create/update note (no path returned)"
                logger.error(error_msg)
                result = {
                    'file': file_path,
                    'success': False,
                    'error': error_msg,
                    'error_category': ErrorCategories.WRITE_ERROR,
                    'timestamp': datetime.now().isoformat()
                }
                return _record_failed_file(result)
        except Exception as e:
            error_category = categorize_error(e)
            error_msg = f"Error creating/updating note: {e}"
            logger.error(f"{error_msg}\n{traceback.format_exc()}")
            result = {
                'file': file_path,
                'success': False,
                'error': error_msg,
                'error_category': error_category,
                'timestamp': datetime.now().isoformat()
            }
            return _record_failed_file(result)
            
    except Exception as e:
        error_category = categorize_error(e)
        error_msg = f"Error processing video: {e}"
        logger.error(f"{error_msg}\n{traceback.format_exc()}")
        result = {
            'file': file_path,
            'success': False,
            'error': error_msg,
            'error_category': error_category,
            'timestamp': datetime.now().isoformat()
        }
        return _record_failed_file(result)

def process_path(path, access_token, templates, results_file=RESULTS_FILE, args=None):
    """
    Process a single file or all video files in a directory (recursively).
    Accepts both absolute and relative paths, and normalizes them to the correct base.
    Works with the local OneDrive folder structure.
    
    Args:
        path: File or directory path (string or Path object)
        access_token: Microsoft Graph API access token
        templates: Templates for note generation
        results_file: File to store processing results
        args: Command line arguments
        
    Returns:
        tuple: (processed files list, error files list)
    """
    # Handle cases where path is a string with a leading slash
    if isinstance(path, str) and path.startswith('/'):
        path = path.lstrip('/')
    
    # Convert to Path object if it's not already
    path = Path(path) if not isinstance(path, Path) else path
    
    # Resolve path relative to OneDrive local resources root if it's not absolute
    if not path.is_absolute():
        abs_path = (ONEDRIVE_LOCAL_RESOURCES_ROOT / path).resolve()
    else:
        abs_path = path.resolve()
        
    processed = []
    errors = []
    
    logger.info(f"Processing path: {abs_path}")
    
    # Check if the path exists
    if not abs_path.exists():
        # Try finding the file by name in the OneDrive directory if path doesn't exist
        file_name = abs_path.name
        possible_matches = []
        
        for ext in VIDEO_EXTENSIONS:
            if file_name.lower().endswith(ext.lower()):
                # This is likely a video file path that doesn't exist
                # Search for it by name
                logger.info(f"Path not found, searching for file by name: {file_name}")
                for root, _, files in os.walk(ONEDRIVE_LOCAL_RESOURCES_ROOT):
                    if file_name in files:
                        match_path = Path(os.path.join(root, file_name))
                        possible_matches.append(match_path)
        
        if possible_matches:
            logger.info(f"Found {len(possible_matches)} possible matches for {file_name}")
            # Use the first match
            abs_path = possible_matches[0]
            logger.info(f"Using match: {abs_path}")
        else:
            logger.error(f"Path not found: {path}")
            print(f"❌ Path not found: {path}")
            return [], []
    
    if abs_path.is_file():
        # Process a single file
        result = process_single_file_by_path(str(abs_path), access_token, templates, results_file, args)
        if result and result.get('success', False):
            processed.append(result)
        else:
            errors.append(result)
    elif abs_path.is_dir():
        # Process all video files in the directory recursively
        # Use find_files_by_extension for each video extension
        video_files = []
        for ext in VIDEO_EXTENSIONS:
            found_files = find_files_by_extension(abs_path, extension=ext)
            video_files.extend(found_files)
        
        logger.info(f"Found {len(video_files)} video files in {abs_path}")
        print(f"Found {len(video_files)} video files in {abs_path}")
        
        for i, file in enumerate(video_files):
            print(f"[{i+1}/{len(video_files)}] Processing: {file.name}")
            result = process_single_file_by_path(str(file), access_token, templates, results_file, args)
            if result and result.get('success', False):
                processed.append(result)
            else:
                errors.append(result)
    
    return processed, errors

def process_single_file_by_onedrive_path(file_path, access_token, templates, results_file=RESULTS_FILE):
    """
    Process a single video file by its path within the local OneDrive folder.
    
    Args:
        file_path: Path to the file relative to the OneDrive base path
        access_token: Authentication token
        templates: Templates for note generation
        results_file: Path to results file
        
    Returns:
        dict: Result of processing
    """
    logger.info(f"Looking for file in local OneDrive folder: {file_path}")
    
    # Try to find the file in the local OneDrive folder
    item = find_file_in_onedrive(file_path, access_token)
    
    if not item:
        error_msg = f"File not found locally: {file_path}"
        logger.error(error_msg)
        print(f"❌ {error_msg}")
        return {
            'file': file_path,
            'success': False,
            'error': error_msg,
            'error_category': ErrorCategories.NOTFOUND,
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
                'error_category': ErrorCategories.INVALID_FILE,
                'timestamp': datetime.now().isoformat()
            }
    
    # Process the file
    print(f"🎬 Processing local video: {item.get('name')}")
    return process_single_video(item, access_token, templates, results_file)

def retry_failed_files(access_token, templates, results_file=RESULTS_FILE):
    """
    Retry processing files that previously failed.
    
    Args:
        access_token: Microsoft Graph API access token
        templates: Templates for note generation
        results_file: File to store processing results
        
    Returns:
        tuple: (processed_count, error_count)
    """
    failed_file = FAILED_FILES_JSON
    
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
    
    processed = 0
    errors = 0
    
    print(f"Retrying {len(failed_list)} previously failed files...")
    
    for i, failed_item in enumerate(failed_list):
        file_path = failed_item.get('path', failed_item.get('file'))
        file_name = os.path.basename(file_path) if file_path else None
        
        if not file_path:
            logger.warning(f"Incomplete failed file record: {failed_item}")
            continue
        
        print(f"[{i+1}/{len(failed_list)}] Retrying: {file_name if file_name else file_path}")
        
        # Find the file in local OneDrive folder
        item = find_file_in_onedrive(file_path, access_token)
        
        if not item:
            logger.error(f"File not found locally: {file_path}")
            errors += 1
            continue
        
        # Mark as retried
        failed_item['retried'] = True
        
        # Process the file
        result = process_single_video(item, access_token, templates, results_file)
        
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

def process_videos_for_sharing(args=None):
    """
    Main processing function to handle all videos in OneDrive.
    This is used when no specific file or folder is specified.
    
    Args:
        args: Command line arguments
    """
    # Get access token
    access_token = authenticate_graph_api(force_refresh=getattr(args, 'refresh_auth', False) if args else False)
    if not access_token:
        logger.error("Failed to authenticate with Microsoft Graph API")
        print("❌ Failed to authenticate with Microsoft Graph API")
        return
    
    # Load templates
    templates = load_metadata_templates()
    
    # Process all video files in the OneDrive local resources root directory
    logger.info(f"Finding video files in local OneDrive folder: {ONEDRIVE_LOCAL_RESOURCES_ROOT}")
    print(f"Finding video files in local OneDrive folder...")
    
    try:
        # Use the file_operations.find_files_by_extension function
        video_files = []
        for ext in VIDEO_EXTENSIONS:
            found_files = find_files_by_extension(ONEDRIVE_LOCAL_RESOURCES_ROOT, extension=ext)
            video_files.extend(found_files)
            
        if not video_files:
            logger.info("No video files found in OneDrive directory")
            print("No video files found in OneDrive directory")
            return
            
        logger.info(f"Found {len(video_files)} video files in OneDrive directory")
        print(f"Found {len(video_files)} video files in OneDrive directory")
        
        # Process each file
        processed = 0
        errors = 0
        
        for i, file_path in enumerate(video_files):
            video_name = file_path.name
            print(f"[{i+1}/{len(video_files)}] Processing: {video_name}")
            
            # Get metadata about the file from the local filesystem
            item = find_file_in_onedrive(str(file_path), access_token)
            if not item:
                logger.error(f"Could not get metadata for {file_path}")
                errors += 1
                continue
                
            result = process_single_video(item, access_token, templates, RESULTS_FILE, args)
            
            if result and result.get('success', False):
                processed += 1
            else:
                errors += 1
        
        print(f"\nProcessing complete: {processed} videos processed, {errors} errors")
        logger.info(f"Processing complete: {processed} videos processed, {errors} errors")
        
    except Exception as e:
        error_category = categorize_error(e)
        logger.error(f"Error processing videos: {e} ({error_category})")
        print(f"❌ Error processing videos: {e}")

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
        help="Retry only previously failed files from generate_video_notes_failed_files.json"
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

def main():
    """Main entry point for the script."""
    args = parse_arguments()
    
    # Set up logging using the config module's setup_logging function
    global logger, failed_logger
    
       # Set up and configure logging and get logger instances with specific log file
    global logger, failed_logger
    logger, failed_logger = setup_logging(
        debug=args.debug,
        log_file="generate_video_notes.log",
        failed_log_file="generate_video_notes_failed_files.log"
    )
   
    start_time = time.time()
    logger.info(f"Starting video meta generator script")
    
    # Load templates
    templates = load_metadata_templates()
    
    # Get access token
    access_token = authenticate_graph_api(force_refresh=args.refresh_auth)
    if not access_token:
        logger.error("Failed to authenticate with Microsoft Graph API")
        print("❌ Failed to authenticate with Microsoft Graph API")
        return
        
    # Process videos based on arguments
    if args.retry_failed:
        print("🔄 Retrying previously failed files...")
        processed, errors = retry_failed_files(access_token, templates)
        print(f"✅ Processed {processed} videos, ❌ {errors} errors.")
    elif args.single_file or args.folder:
        input_path = args.single_file or args.folder
        print(f"🔍 Processing specified path: {input_path}")
        processed, errors = process_path(input_path, access_token, templates, RESULTS_FILE, args)
        print(f"✅ Processed {len(processed)} videos, ❌ {len(errors)} errors.")
    else:
        print("📂 Processing all videos in OneDrive...")
        process_videos_for_sharing(args)
    
    elapsed_time = time.time() - start_time
    logger.info(f"Script completed in {elapsed_time:.2f} seconds")
    print(f"🏁 Script completed in {elapsed_time:.2f} seconds")

if __name__ == "__main__":
    main()