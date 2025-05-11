#!/usr/bin/env python3
""""
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
"""

import os
import logging
import inspect
import colorlog
import json
import os.path
from pathlib import Path
from typing import Dict, List, Optional, Tuple, Union, Any
from dotenv import load_dotenv
from .paths import normalize_wsl_path

# Get the default logger
logger = logging.getLogger(__name__)

# Load environment variables from .env file
load_dotenv()

# Error categories for standardized error classification across the system
class ErrorCategories:
    """
    Standardized error categories for consistent error handling and reporting.
    
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
        ...     logger.error(f\"{error_type}: {str(e)}\")
    """
    # Network and connectivity related errors
    NETWORK: str = "network_error"          # Network connectivity issues
    AUTHENTICATION: str = "authentication_error"  # Login/token problems
    PERMISSION: str = "permission_error"    # Access rights issues 
    TIMEOUT: str = "timeout_error"          # Operation timeout
    RATE_LIMIT: str = "rate_limit_error"    # API throttling or quota limits
    SERVER: str = "server_error"            # Remote server errors (5xx)
    INVALID_REQUEST: str = "invalid_request_error"  # Client errors (4xx)
    FILE_NOT_FOUND: str = "file_not_found_error"  # Missing files
    DATA_ERROR: str = "data_error"          # Data parsing or validation errors
    UNKNOWN: str = "unknown_error"          # Default catch-all category
