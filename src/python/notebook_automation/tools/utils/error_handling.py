#!/usr/bin/env python3
"""
Error Handling Module for MBA Notebook Automation System

This module provides a comprehensive error management system for the MBA Notebook
Automation platform, with sophisticated error categorization, tracking, and reporting
capabilities. It ensures consistent error handling across the entire system and
enables effective troubleshooting of automated processes.

Key Features:
------------
1. Intelligent Error Categorization
   - Pattern-based error classification
   - HTTP status code interpretation
   - Semantic analysis of error messages
   - Standardized categories for consistent reporting

2. Structured Error Tracking
   - JSON-based error recording and persistence
   - Timestamping and categorization of failures
   - Automatic deduplication of repeated errors
   - Support for retry status tracking

3. Results Management
   - Consolidated processing results tracking
   - Success/failure statistics maintenance
   - Historical record of processed files
   - Error correlation with specific files and operations

Integration Points:
-----------------
- Used by all tool modules for consistent error handling
- Works with the logging system for error reporting
- Maintains persistent error records for post-mortem analysis
- Supports the retry mechanism for transient failures

Usage Example:
------------
```python
# Import error handling components
from notebook_automation.tools.utils.error_handling import categorize_error, update_failed_files, update_results_file
from notebook_automation.tools.utils.config import ErrorCategories

# Process files with comprehensive error handling
def process_video_file(video_path):
    try:
        # Attempt to process the video file
        result = video_processing_logic(video_path)
        
        # Record successful result
        update_results_file('video_results.json', {
            'file': os.path.basename(video_path),
            'path': video_path,
            'success': True,
            'output': 'notes_output.md',
            'timestamp': datetime.now().isoformat()
        })
        
        return result
        
    except Exception as e:
        # Categorize the error type
        error_category = categorize_error(e)
        
        # Update failed files tracking
        update_failed_files(
            {'file': os.path.basename(video_path), 'path': video_path},
            str(e),
            error_category
        )
        
        # Record failure in results
        update_results_file('video_results.json', {
            'file': os.path.basename(video_path),
            'path': video_path,
            'success': False,
            'error': str(e),
            'category': error_category,
            'timestamp': datetime.now().isoformat()
        })
        
        # Re-raise or return error indication based on needs
        return None
"""

import os
import json
import logging
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional, Tuple, Union, Any
from .config import ErrorCategories, ensure_logger_configured

# Initialize module loggers with safe configuration
logger = ensure_logger_configured(__name__)
failed_logger = logging.getLogger("failed_files")

def categorize_error(error: Union[Exception, str], status_code: Optional[int] = None) -> str:
    """Intelligently categorize an error based on its type, message pattern, or HTTP status code.
    
    This function analyzes errors through multiple approaches to determine the most
    appropriate error category from ErrorCategories. It performs semantic analysis
    of error messages, interprets HTTP status codes, and recognizes common error patterns
    to provide consistent categorization across the entire automation system.
    
    Args:
        error (Union[Exception, str]): The error object or error message to analyze
        status_code (Optional[int], optional): HTTP status code if applicable. 
            Defaults to None.
    
    Returns:
        str: The error category string from ErrorCategories constants
        
    Example:
        >>> try:
        ...     response = requests.get("https://api.example.com")
        ...     response.raise_for_status()
        ... except Exception as e:
        ...     category = categorize_error(e, response.status_code)
        ...     print(f"Error category: {category}")
        Error category: NETWORK_ERROR
        
    Notes:
        Categorization Strategy:
        1. First checks for network connectivity issues (timeouts, connection errors)
        2. Then examines authentication-related problems (tokens, credentials)
        3. Looks for permission and access issues
        4. Interprets HTTP status codes when available
        5. Detects rate limiting and throttling indicators
        6. Falls back to UNKNOWN category if no patterns match
                                 Can be any exception type or string representation.
        status_code (int, optional): HTTP status code if the error occurred
                                    during an API call. Provides more precise
                                    categorization for HTTP-based errors.
        
    Returns:
        str: A standard error category from the ErrorCategories class constants.
             Categories include NETWORK, AUTHENTICATION, PERMISSION, 
             TIMEOUT, RATE_LIMIT, SERVER, and UNKNOWN.
    
    Example:
        ```python
        try:
            response = requests.get('https://api.example.com/data')
            response.raise_for_status()
        except requests.exceptions.RequestException as e:
            error_category = categorize_error(e, response.status_code if response else None)
            logger.error(f"{error_category}: Failed to fetch data: {str(e)}")
        ```
    """
    # Convert any error object to lowercase string for consistent pattern matching
    error_str = str(error).lower()
    
    # PHASE 1: Check for network-related errors
    # Network issues are common in distributed systems and often transient,
    # so identifying them specifically helps with retry strategies
    if any(net_err in error_str for net_err in ['timeout', 'connecttimeout', 'connect timeout', 'connection', 'network']):
        # Further differentiate timeout issues from general network problems
        # as they may require different handling strategies
        return ErrorCategories.TIMEOUT if 'timeout' in error_str else ErrorCategories.NETWORK
        
    # PHASE 2: Check for authentication errors
    # Auth issues typically require user intervention or token refresh
    if any(auth_err in error_str for auth_err in ['auth', 'token', 'credential', 'unauthorized', 'permission']):
        return ErrorCategories.AUTHENTICATION
        
    # PHASE 3: Check for permission errors
    # Permission issues are distinct from authentication and typically
    # indicate a lack of access rights rather than invalid credentials
    if any(perm_err in error_str for perm_err in ['permission', 'access denied', 'forbidden']):
        return ErrorCategories.PERMISSION
        
    # PHASE 4: Check HTTP status codes when available
    # HTTP status codes provide more precise categorization for web API errors
    if status_code:
        if status_code == 401:
            return ErrorCategories.AUTHENTICATION
        elif status_code == 403:
            return ErrorCategories.PERMISSION
        elif status_code == 429:
            return ErrorCategories.RATE_LIMIT
        elif status_code >= 500:
            return ErrorCategories.SERVER
    
    # PHASE 5: Check for rate limit errors
    # Rate limiting requires special handling, often with exponential backoff
    # This checks for various ways APIs might indicate rate limiting
    if any(rate_err in error_str for rate_err in ['rate', 'limit', 'throttl', 'too many', '429']):
        return ErrorCategories.RATE_LIMIT
    
    # PHASE 6: Fall back to unknown category if no patterns matched
    # This ensures every error gets categorized, even if we can't determine specifics
    return ErrorCategories.UNKNOWN

def update_failed_files(failed_file: str, file_info: Union[str, Dict[str, Any], List[str]], 
                     error: Union[Exception, str] = "Unknown error", 
                     category: str = ErrorCategories.UNKNOWN) -> None:
    """Update the centralized tracking system for failed file operations.
    
    This function maintains a comprehensive error tracking system that records
    which files failed processing, details about the error, error categorization,
    and timestamps. It supports adding both individual files and batches of files,
    handles deduplication, and maintains a persistent JSON record for later analysis
    and retry operations.
    
    Args:
        failed_file (str): Path to the JSON file where failures are tracked.
            Will be created if it doesn't exist.
        file_info (Union[str, Dict[str, Any], List[str]]): Information about the failed file(s).
            Can be a single file path string, a dictionary with file metadata, 
            or a list of file paths for batch updates.
        error (Union[Exception, str], optional): The error that occurred during processing.
            Can be an exception object or a string description. Defaults to "Unknown error".
        category (str, optional): The error category for classification.
            Should be one of the constants from ErrorCategories. 
            Defaults to ErrorCategories.UNKNOWN.
    
    Returns:
        None
    
    Example:
        >>> try:
        ...     process_video_file('/path/to/video.mp4')
        ... except Exception as e:
        ...     category = categorize_error(e)
        ...     update_failed_files(
        ...         'failed_files.json',
        ...         {'file': 'video.mp4', 'path': '/path/to/video.mp4'},
        ...         e,
        ...         category
        ...     )
        
    Notes:
        Side Effects:
        - Updates the specified failed_files.json file
        - Logs the failure to the dedicated failed_logger
        - Creates the tracking file if it doesn't exist
    """
    # Use the provided failed_file path directly (don't override it)
    failed_file_path = failed_file
    
    # Load existing failed files or create a new list
    # This ensures we maintain a comprehensive history and don't lose previous failures
    try:
        if os.path.exists(failed_file_path):
            with open(failed_file_path, 'r') as f:
                failed_data = json.load(f)
                # Handle both formats: direct list or {'failed_files': [...]} format
                if isinstance(failed_data, dict) and 'failed_files' in failed_data:
                    failed_list = failed_data.get('failed_files', [])
                else:
                    failed_list = failed_data if isinstance(failed_data, list) else []
        else:
            # Initialize a new list if this is the first recorded failure
            failed_list = []
    except Exception as e:
        # Error handling for the error handler - use empty list as fallback
        # This prevents errors in the error handling system itself from causing crashes
        logger.error(f"Error loading failed files: {e}")
        failed_list = []
    
    # Handle different input types for file_info
    if isinstance(file_info, list):
        # Handle case when file_info is a list of filenames/paths
        files_to_add = []
        for file_path in file_info:
            # Handle each item in the list properly
            if isinstance(file_path, str):
                files_to_add.append({
                    'file': os.path.basename(file_path),
                    'path': str(file_path),
                    'error': str(error),
                    'category': category,
                    'timestamp': datetime.now().isoformat(),
                    'retried': False
                })
            elif isinstance(file_path, dict):
                # Handle dictionaries in the list
                files_to_add.append({
                    'file': file_path.get('file', os.path.basename(str(file_path.get('path', 'unknown')))),
                    'path': str(file_path.get('path', '')),
                    'error': str(error),
                    'category': category,
                    'timestamp': datetime.now().isoformat(),
                    'retried': False
                })
            else:
                # Handle any other type
                files_to_add.append({
                    'file': 'unknown',
                    'path': str(file_path),
                    'error': str(error),
                    'category': category,
                    'timestamp': datetime.now().isoformat(),
                    'retried': False
                })
                logger.warning(f"Unexpected type for file_path in list: {type(file_path)}. Using string conversion.")
            
        # Add all files to the failed list, avoiding duplicates
        for new_record in files_to_add:
            found = False
            for i, item in enumerate(failed_list):
                if isinstance(item, dict) and item.get('file') == new_record['file']:
                    failed_list[i] = new_record  # Update existing record
                    found = True
                    break
            if not found:
                failed_list.append(new_record)  # Add new record
            
            # Log each failed file
            failed_logger.error(f"Failed file: {new_record['file']}, Error: {new_record['error']}, Category: {new_record['category']}")
    else:
        # Create a structured failure record with consistent fields
        # This standardized format enables better analysis and reporting
        failed_record = {}
        
        if isinstance(file_info, str):
            # Handle case when file_info is just a string (filename/path)
            failed_record = {
                'file': os.path.basename(file_info),
                'path': file_info,
                'error': str(error),
                'category': category,
                'timestamp': datetime.now().isoformat(),
                'retried': False
            }
        elif isinstance(file_info, dict):
            # Handle case when file_info is a dictionary
            failed_record = {
                'file': file_info.get('file', os.path.basename(str(file_info.get('path', 'unknown')))),
                'path': str(file_info.get('path', '')),
                'error': str(error),
                'category': category,
                'timestamp': datetime.now().isoformat(),
                'retried': False
            }
        else:
            # Handle unexpected type by creating a minimal record
            failed_record = {
                'file': 'unknown',
                'path': str(file_info) if file_info is not None else '',
                'error': str(error),
                'category': category,
                'timestamp': datetime.now().isoformat(),
                'retried': False
            }
            logger.warning(f"Unexpected type for file_info: {type(file_info)}. Using minimal record.")
        
        # Check if this file is already in the list to prevent duplicates
        # This deduplication logic ensures we track the latest error for each file
        found = False
        for i, item in enumerate(failed_list):
            if isinstance(item, dict) and item.get('file') == failed_record['file']:
                # Update existing record with new error information
                failed_list[i] = failed_record
                found = True
                break
        
        # Add new record if not found
        # This ensures all failed files are tracked, not just updated ones
        if not found:
            failed_list.append(failed_record)
        
        # Log to specialized failed files logger
        # This creates a dedicated log stream just for failures that's easier to monitor
        failed_logger.error(f"Failed file: {failed_record['file']}, Error: {failed_record['error']}, Category: {failed_record['category']}")
    
    # Write updated list back to the JSON file
    # This persists the failure information across process restarts
    try:
        with open(failed_file_path, 'w') as f:
            # Store as a container object with failed_files key for better structure
            json.dump({'failed_files': failed_list}, f, indent=2)  # Pretty-print for human readability
    except Exception as e:
        # Handle errors in writing the failure record
        # Log but don't raise to prevent cascading failures
        logger.error(f"Error writing failed files: {e}")

def update_results_file(results_file: str, new_result: Dict[str, Any]) -> None:
    """Update the structured results tracking system with new processing outcomes.
    
    This function manages a comprehensive JSON-based tracking system for all processing
    results in the automation pipeline. It maintains both successful and failed operations,
    handles result deduplication, manages error lists, and ensures consistent timestamp
    tracking. The system provides a complete audit trail of all processing operations.
    
    Args:
        results_file (str): Path to the JSON results file to update.
            Will be created if it doesn't exist.
        new_result (Dict[str, Any]): A dictionary containing the new processing result.
            Must contain at least a 'file' key to identify the processed item. 
            Should include a 'success' boolean to indicate processing status.
    
    Returns:
        None
        
    Raises:
        None: Any internal exceptions are caught and logged but not propagated
        
    Example:
        >>> update_results_file('video_processing_results.json', {
        ...     'file': 'lecture1.mp4',
        ...     'path': '/videos/lecture1.mp4',
        ...     'success': True,
        ...     'processing_time': 45.2,
        ...     'timestamp': datetime.now().isoformat()
        ... })
    
    Notes:
        Data Structure:
        The results file maintains this structure:
        {
            "processed_videos": [
                {
                    "file": "video1.mp4",
                    "path": "/path/to/video1.mp4",
                    "success": true,
                    "output_file": "notes_video1.md",
                    "processing_time": "120.5",
                    ...other metadata...
                },
                ...
            ],
            "errors": [
                {
                    "file": "video2.mp4",
                    "error": "File not found",
                    "timestamp": "2023-09-01T14:30:22.123456"
                },
                ...
            ],
            "timestamp": "2023-09-01T14:30:25.123456",
            "last_run": "2023-09-01T14:30:25.123456"
        }
    
    Side Effects:
        - Creates or updates the specified results_file
        - Maintains the processing history for the automation system
        - Logs any errors that occur during the results file update
    
    Example:
        ```python
        result = {
            'file': 'lecture3.mp4',
            'path': '/videos/lecture3.mp4',
            'success': True,
            'processing_time': 45.2,
            'output_file': 'notes_lecture3.md'
        }
        update_results_file('video_processing_results.json', result)
    """
    # Ensure the results file exists with a valid initial structure
    # This provides a clean starting point for first-time runs
    if not os.path.exists(results_file):
        with open(results_file, 'w') as f:
            # Create standard structure with empty collections and initial timestamp
            json.dump({
                'processed_videos': [],  # List of all processed items
                'errors': [],            # Collection of error reports
                'timestamp': datetime.now().isoformat()  # Creation time
            }, f, indent=2)
    
    try:
        # Read existing results to maintain history
        # This ensures we don't lose previous processing records
        with open(results_file, 'r') as f:
            existing_results = json.load(f) or {}
            
            # Ensure existing_results is a dict
            if not isinstance(existing_results, dict):
                existing_results = {'processed_videos': []}
        
        # Get the file identifier from the new result
        file_path = new_result.get('file', 'unknown')
        
        # Ensure required structure exists in case of legacy or corrupted files
        # This backward compatibility check prevents errors with older result files
        if 'processed_videos' not in existing_results:
            existing_results['processed_videos'] = []
        
        # Check if this file exists in the processed videos to prevent duplicates
        # This ensures we maintain one record per file, with the latest result
        found = False
        for i, video in enumerate(existing_results['processed_videos']):
            video_file = video.get('file') if isinstance(video, dict) else None
            if video_file == file_path:
                # Update existing entry with new processing information
                # This maintains the processing history while ensuring current data
                existing_results['processed_videos'][i] = new_result
                found = True
                break
        
        # If not found, add it as a new entry
        # This ensures all newly processed files are properly recorded
        if not found:
            existing_results['processed_videos'].append(new_result)
        
        # Handle error tracking for failed operations
        # This maintains a separate list of errors for easier reporting and analysis
        if not new_result.get('success', True):
            # Initialize errors list if missing (backwards compatibility)
            if 'errors' not in existing_results:
                existing_results['errors'] = []
            
            # Add structured error record with timestamp
            # This provides a chronological history of all failures
            existing_results['errors'].append({
                'file': file_path,
                'error': new_result.get('error', 'Unknown error'),
                'timestamp': datetime.now().isoformat()
            })
        
        # Update last run timestamp for audit trails
        # This helps track when the system was last active
        existing_results['last_run'] = datetime.now().isoformat()
        
        # Write updated results back to file with pretty-printing
        # This ensures the file remains human-readable for debugging
        with open(results_file, 'w') as f:
            json.dump(existing_results, f, indent=2)
        
    except Exception as e:
        # Handle errors in the results tracking system
        # Log but don't raise to prevent cascading failures
        logger.error(f"Error updating results file: {e}")