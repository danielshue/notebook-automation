#!/usr/bin/env python3
"""
Configuration Module for Notebook Automation System

This module provides a centralized configuration system for the Notebook
Automation platform. It handles environment detection, configuration loading,
path normalization, logging setup, and error categorization for the entire system.

Key Features:
------------
1. Dynamic Configuration Management
   - JSON-based configuration loading with fallback mechanisms
   - Environment variable integration through dotenv
   - WSL path normalization for cross-platform compatibility 
   - Centralized constant definitions for system-wide access

2. Advanced Logging System
   - Colored console output for improved readability
   - Multi-level logging with DEBUG/INFO controls
   - Separate logging streams for failed operations
   - Automatic log file naming based on caller module
   - Environment variable controlled debug mode

3. Error Handling Framework
   - Standardized error categories for consistent reporting
   - Centralized error tracking and classification
   - Support for structured error logging and analysis

4. API Integration Configuration
   - Microsoft Graph API settings and authentication
   - OpenAI API integration settings
   - Token caching and management

Integration Points:
-----------------
- Used by all tools modules for configuration access
- Supports both command-line tools and library functions
- Provides the logging foundation for the entire system
- Centralizes path management across Windows/Linux environments

Usage Example:
------------
```python
# Import configuration module
from notebook_automation.tools.utils.config import setup_logging, ErrorCategories, VAULT_ROOT

# Set up logging for the module
logger, failed_logger = setup_logging(debug=True)

# Use configuration constants
logger.info(f"Using vault at: {VAULT_ROOT}")

# Process files with standardized error handling
try:
    # Attempt to process files
    process_files_in_directory(VAULT_ROOT)
except ConnectionError as e:
    # Use standardized error categorization
    logger.error(f"{ErrorCategories.NETWORK}: {str(e)}")
    failed_logger.error(f"Failed to process files: {str(e)}")
```
"""

import os
import logging
import inspect
import colorlog
import json
import os.path
from pathlib import Path
from typing import Dict, List, Optional, Tuple, Union, Any, Set
from dotenv import load_dotenv
from .paths import normalize_wsl_path

# Get the default logger
logger = logging.getLogger(__name__)

# Load environment variables from .env file
load_dotenv()

# Error categories for standardized error classification across the system
class ErrorCategories:
    """Standardized error categories for consistent error handling and reporting.
    
    This class defines string constants for various error types encountered
    throughout the Notebook Automation system. Using these standardized
    categories enables consistent error handling, improved error reporting,
    and makes it possible to aggregate error statistics across different
    modules and components.
    
    Attributes:
        NETWORK (str): Network connectivity issues like DNS failures or timeouts.
        AUTHENTICATION (str): Authentication failures with external services.
        PERMISSION (str): Permission-denied errors for files or APIs.
        TIMEOUT (str): Timeout errors when operations take too long.
        RATE_LIMIT (str): Rate limiting or throttling errors from external APIs.
        SERVER (str): Remote server errors (5xx HTTP codes).
        INVALID_REQUEST (str): Client-side errors in request formation (4xx HTTP codes).
        FILE_NOT_FOUND (str): File not found errors when accessing local or remote files.
        DATA_ERROR (str): Data validation or parsing errors.
        UNKNOWN (str): Unclassified errors that don't match other categories.
    
    Example:
        >>> try:
        ...     # Operation that might fail
        ...     response = requests.get('https://api.example.com/data')
        ...     response.raise_for_status()
        ... except requests.exceptions.RequestException as e:
        ...     error_type = ErrorCategories.NETWORK
        ...     logger.error(f"{error_type}: {str(e)}")
    """
    NETWORK = "network_error"          # Network connectivity issues
    AUTHENTICATION = "authentication_error"  # Login/token problems
    PERMISSION = "permission_error"    # Access rights issues 
    RATE_LIMIT = "rate_limit_error"    # API throttling or quota limits
    SERVER = "server_error"            # Remote server errors (5xx)
    TIMEOUT = "timeout_error"          # Operation timed out
    VALIDATION = "validation_error"    # Data validation failures
    FILE_ERROR = "file_error"          # File access/processing issues
    UNKNOWN = "unknown_error"          # Unclassified errors

# Load settings from config.json file
import json
import os.path

# Get the absolute path to the config file in the project root
# This construction ensures the config is always found at the project root
config_file_path = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..', 'config.json'))

try:
    # Attempt to load and parse the JSON configuration file
    # This is the primary source of configuration for the system
    with open(config_file_path, 'r') as config_file:
        config_data = json.load(config_file)
      # Load and normalize paths from the configuration
    # WSL path normalization ensures proper path handling in Windows Subsystem for Linux
    # Converting string paths to Path objects provides better path manipulation capabilities
    ONEDRIVE_LOCAL_RESOURCES_ROOT: Path = Path(normalize_wsl_path(config_data['paths']['resources_root']))
    VAULT_LOCAL_ROOT: Path = Path(normalize_wsl_path(config_data['paths']['vault_root']))
    METADATA_FILE: Path = Path(normalize_wsl_path(config_data['paths']['metadata_file']))
    
    # Other configuration settings
    ONEDRIVE_BASE: str = config_data['onedrive']['base_path']    # Base path for OneDrive operations
    VIDEO_EXTENSIONS: Set[str] = set(config_data['video_extensions']) # Set for O(1) extension lookups
    
    # Microsoft Graph API configuration for OneDrive integration
    # These settings control authentication and API access
    MICROSOFT_GRAPH_API_CLIENT_ID: str = config_data['microsoft_graph']['client_id']
    GRAPH_API_ENDPOINT: str = config_data['microsoft_graph']['api_endpoint']
    AUTHORITY: str = config_data['microsoft_graph']['authority']
    SCOPES: List[str] = config_data['microsoft_graph']['scopes'] # API permissions requested
      # Static configuration (not from JSON)
    TOKEN_CACHE_FILE: str = "token_cache.bin"
    
    logger.debug(f"Loaded configuration from {config_file_path}")
except Exception as e:
    # Robust error handling with fallback mechanism
    # This ensures the system can still operate with defaults if the config is missing or invalid
    logger.warning(f"Could not load config from {config_file_path}: {e}")
    
    # Fallback defaults could be defined here if needed
    # These would provide minimum required functionality in absence of config.json
    
# Token cache location for Microsoft Graph API authentication
# This is not moved to config.json as it's a derived path based on the module location
# The token cache enables persistent authentication between sessions
TOKEN_CACHE_FILE: str = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "token_cache.bin")

# OpenAI API integration configuration
# The API key is loaded from environment variables for security
# This approach keeps sensitive credentials out of the code and config files
OPENAI_API_KEY: Optional[str] = os.getenv("OPENAI_API_KEY")

# Additional OpenAI configuration could be added here in the future
# For example: model selection, temperature settings, etc.

# Setup logging
def setup_logging(debug: bool = False, log_file: Optional[str] = None, 
               failed_log_file: str = "failed_files.log", console_output: bool = True) -> Tuple[logging.Logger, logging.Logger]:
    """Configure comprehensive logging for the Notebook Automation system.
    
    This function sets up a sophisticated logging system with multiple output streams,
    color-coded console output, and separate tracking for failed operations. It provides
    an intelligent default configuration while allowing customization through parameters.
    
    Key features:
    1. Automatic log file naming based on the caller module
    2. Color-coded console output with level-appropriate colors
    3. Separate logging stream for failed operations
    4. Environment variable override for debug mode
    5. Log level filtering based on debug setting
    6. Custom formatters for different output destinations
    
    Args:
        debug (bool): Enable debug logging level when True. When False, uses INFO level.
                     This can be overridden by the NOTEBOOK_DEBUG environment variable.
        
        log_file (str, optional): Custom log file name for the main logger.
                                 When not provided, automatically derives name from the 
                                 calling module (recommended).
                                 
        failed_log_file (str, optional): Log file name for recording failed operations.
                                        Defaults to "failed_files.log".
                                        
        console_output (bool, optional): When True, logs are output to both console
                                        and log files. When False, logs only go to files.
                                        Defaults to True.
        
    Returns:
        tuple: A tuple containing two configured loggers:
               (main_logger, failed_logger)
               
               - main_logger: The primary logger for the calling module
               - failed_logger: Special logger for recording failed operations
               
    Environment Variables:
        NOTEBOOK_DEBUG: When set to "1", forces debug logging regardless of
                       the debug parameter value.
                       
    Example:
        ```python
        from notebook_automation.tools.utils.config import setup_logging
        
        # Basic setup with automatic log file naming
        logger, failed_logger = setup_logging(debug=True)
        
        # Log at different levels
        logger.debug("Detailed debugging information")
        logger.info("Normal operation information")
        logger.warning("Warning condition")
        logger.error("Error condition")
        
        # Log failed operations separately
        failed_logger.error("Failed to process file.pdf: Permission denied")
        ```
    """
    # --- If DEBUG is set at the environment level, then set the logger to DEBUG level ---
    # Environment variable override for debug mode provides an external control mechanism
    # This allows enabling debug mode without code changes, useful for troubleshooting
    if os.environ.get("NOTEBOOK_DEBUG", "0") == "1":
        logger.setLevel(logging.DEBUG)
   
    # Base log level determined by function parameter or environment override
    log_level = logging.DEBUG if debug else logging.INFO
    
    # Add console handler if requested
    if console_output:
        # Create console handler with color formatting (no timestamp)
        console_handler = logging.StreamHandler()
        color_formatter = colorlog.ColoredFormatter(
            '%(log_color)s%(levelname)s: %(message)s',
            log_colors={
                'DEBUG': 'cyan',      # Cyan for detailed debug information
                'INFO': 'green',      # Green for normal operation messages
                'WARNING': 'yellow',  # Yellow for warning conditions
                'ERROR': 'red',       # Red for error conditions
                'CRITICAL': 'bold_red',  # Bold red for critical failures
            },
            secondary_log_colors={},
            style='%'
        )
        console_handler.setFormatter(color_formatter)
        console_handler.setLevel(log_level)
        # Add console handler to root logger for all modules
        logging.getLogger('').addHandler(console_handler)
        # Create logs directory if it doesn't exist
    logs_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), 'logs')
    if not os.path.exists(logs_dir):
        try:
            os.makedirs(logs_dir)
            logging.info(f"Created logs directory: {logs_dir}")
        except Exception as e:
            logging.warning(f"Failed to create logs directory: {e}")
    
    # Determine the appropriate log file name if not specified
    # This intelligent naming derives the log file name from the calling module
    # which provides better context than using a generic name for all logs
    if not log_file:
        # Use introspection to get the filename of the calling module
        caller_filename = inspect.stack()[1].filename
        # Extract just the base name without path or extension
        base_name = os.path.splitext(os.path.basename(caller_filename))[0]
        # Use the module name as the log file name
        log_file = os.path.join(logs_dir, f"{base_name}.log")
    elif not os.path.isabs(log_file):
        # If a relative path was provided, put it in the logs directory
        log_file = os.path.join(logs_dir, log_file)
    
    # Create a file handler for persistent logging to file
    # This ensures all log entries are recorded for later analysis
    file_handler = logging.FileHandler(log_file)
    file_handler.setLevel(log_level)
    # Standard timestamp-prefixed format for log files (keep timestamps in files)
    file_handler.setFormatter(logging.Formatter('%(asctime)s - %(levelname)s - %(message)s'))
    
    # Create an enhanced console handler with color support
    # Colors improve readability by making log levels visually distinct
    color_formatter = colorlog.ColoredFormatter(
        '%(log_color)s%(asctime)s - %(levelname)s - %(message)s',
        log_colors={
            'DEBUG': 'cyan',      # Cyan for detailed debug information
            'INFO': 'green',      # Green for normal operation messages
            'WARNING': 'yellow',  # Yellow for warning conditions
            'ERROR': 'red',       # Red for error conditions
            'CRITICAL': 'bold_red',  # Bold red for critical failures
        },
        secondary_log_colors={},  # No secondary colors needed currently
        style='%'                 # Use % style formatting for compatibility
    )
    console_handler.setFormatter(color_formatter)
      # Configure the root logger with our custom handlers
    # The root logger serves as the base configuration for all module loggers
    root_logger = logging.getLogger()
    root_logger.setLevel(log_level)
    
    # Remove any existing handlers to avoid duplicate log entries
    # This is essential when setup_logging is called multiple times
    for handler in root_logger.handlers[:]:
        root_logger.removeHandler(handler)
        
    # Add our custom handlers to the root logger
    # This ensures consistent formatting across all module loggers
    root_logger.addHandler(file_handler)
    root_logger.addHandler(console_handler)
    
    # Get a specialized logger for the caller module
    # This provides context about which module generated each log entry
    # and enables module-specific log filtering if needed
    caller_module = inspect.getmodule(inspect.stack()[1][0])
    logger = logging.getLogger(caller_module.__name__ if caller_module else __name__)
      # Create a specialized logger for tracking failed operations
    # This separate logging stream provides focused visibility into failures
    # and enables better error tracking and reporting
    failed_logger = logging.getLogger("failed_files")
    failed_logger.setLevel(logging.INFO)  # Always use INFO level for failures
    
    # Remove any existing handlers to prevent duplicate entries
    # This ensures clean behavior when setup_logging is called multiple times
    while failed_logger.handlers:
        failed_logger.removeHandler(failed_logger.handlers[0])
          # Add a dedicated file handler for failed operations
    # This creates a separate log file specifically for tracking failures
    if not os.path.isabs(failed_log_file):
        # If a relative path was provided, put it in the logs directory
        logs_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), 'logs')
        failed_log_file = os.path.join(logs_dir, failed_log_file)
    
    failed_file_handler = logging.FileHandler(failed_log_file)
    failed_file_handler.setFormatter(logging.Formatter('%(asctime)s - %(levelname)s - %(message)s'))
    failed_logger.addHandler(failed_file_handler)
    # Add a visually distinct colored console handler for failed operations (no timestamp)
    failed_console_handler = logging.StreamHandler()
    failed_console_handler.setFormatter(colorlog.ColoredFormatter(
        '%(log_color)sFAILED - %(message)s',
        log_colors={
            'DEBUG': 'cyan',
            'INFO': 'green',
            'WARNING': 'yellow',
            'ERROR': 'red,bg_white',     # Red text on white background for high visibility
            'CRITICAL': 'bold_red,bg_white',  # Bold red on white for critical failures
        }
    ))
    failed_logger.addHandler(failed_console_handler)
    failed_logger.propagate = False  # Don't propagate to root logger to avoid duplicate entries
    
    if debug:
        logger.debug("Debug logging enabled")
    
    return logger, failed_logger