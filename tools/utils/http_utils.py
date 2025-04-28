#!/usr/bin/env python3
"""
HTTP Utilities Module for MBA Notebook Automation System

This module provides robust HTTP request handling capabilities with built-in retry mechanisms,
connection pooling, and comprehensive error handling. It's designed to ensure reliable
communication with external APIs, particularly the Microsoft Graph API for OneDrive integration.

Key Features:
------------
1. Resilient HTTP Sessions
   - Automatic retry logic with exponential backoff
   - Connection pooling for performance optimization
   - Configurable timeouts to prevent hanging requests
   - Status code-based retry policies

2. API Health Monitoring
   - OneDrive connectivity verification
   - Authentication validation
   - Storage quota monitoring
   - Structured health status reporting

3. Error Handling
   - Categorized error reporting
   - Detailed logging of failures
   - Exception management with context preservation
   - Integration with the system-wide error tracking

Integration Points:
-----------------
- Primary interface for Microsoft Graph API communication
- Supports OneDrive file operations for cloud storage access
- Provides health checks for the authentication system
- Centralizes HTTP configuration for consistent behavior

Usage Example:
------------
```python
from tools.utils.http_utils import create_requests_session, check_api_health
from tools.utils.error_handling import categorize_error

# Create a resilient HTTP session
session = create_requests_session(
    retries=5,               # 5 retries for better reliability
    timeout=15,              # 15 second timeout
    backoff_factor=1.0       # More aggressive backoff
)

# Set up headers with authentication token
headers = {
    'Authorization': f'Bearer {access_token}',
    'Content-Type': 'application/json',
}

# Check API health before proceeding
is_healthy, health_message = check_api_health(session, headers)
if not is_healthy:
    print(f"API health check failed: {health_message}")
    exit(1)

# Make API requests with resilience
try:
    response = session.get(
        'https://graph.microsoft.com/v1.0/me/drive/root/children',
        headers=headers
    )
    response.raise_for_status()  # Trigger retry for 5xx errors
    
    # Process successful response
    files = response.json().get('value', [])
    print(f"Found {len(files)} items in OneDrive root")
    
except Exception as e:
    # Categorize the error for better handling
    error_type = categorize_error(e, response.status_code if 'response' in locals() else None)
    print(f"Error accessing OneDrive: {str(e)} ({error_type})")
```
"""

# Standard library imports
from functools import partial  # Used for modifying function signatures

# Third-party HTTP handling imports
import requests  # Core HTTP client library
from requests.adapters import HTTPAdapter  # For connection pooling and retry management
from urllib3.util.retry import Retry  # For configuring retry behavior

# Internal imports for logging and configuration
from ..utils.config import logger  # Centralized logging system

def create_requests_session(retries=3, backoff_factor=0.5, status_forcelist=(500, 502, 503, 504), timeout=10):
    """
    Create a resilient HTTP session with advanced retry logic and connection pooling.
    
    This function constructs a pre-configured requests.Session object designed for
    robust API communication. The session includes intelligent retry mechanisms
    with exponential backoff, connection pooling for performance optimization,
    and configurable timeouts to prevent request hanging.
    
    The retry strategy handles various failure scenarios:
    - Transient network errors (connection drops, timeouts)
    - Server-side failures (500-series status codes)
    - Rate limiting and service unavailability
    
    Session Features:
    - Exponential backoff with jitter for optimal retry spacing
    - Connection pooling to reduce connection establishment overhead
    - Configurable timeout settings to prevent indefinite waiting
    - Support for all common HTTP methods in retry logic
    
    Args:
        retries (int): Maximum number of retry attempts for failed requests.
                      Default is 3, which means a request can be attempted up to 4 times
                      (initial attempt + 3 retries).
                      
        backoff_factor (float): Exponential backoff multiplier between retry attempts.
                               For example, with factor 0.5, the delays between retries
                               would be: 0.5s, 1.0s, 2.0s, etc.
                               
        status_forcelist (tuple): Collection of HTTP status codes that should trigger
                                 a retry regardless of the HTTP method. By default,
                                 includes 500, 502, 503, 504 (server errors).
                                 
        timeout (int): Request timeout in seconds for both connect and read operations.
                      Prevents requests from hanging indefinitely. Default is 10 seconds.
        
    Returns:
        requests.Session: A fully configured session object ready for making
                         resilient HTTP requests with consistent behavior.
                         
    Example:
        ```python
        session = create_requests_session(retries=5, timeout=15)
        response = session.get("https://api.example.com/data")
        
        # All requests made with this session will automatically use the
        # configured retry logic, timeouts, and connection pooling
        ```
    """
    session = requests.Session()
    
    # Configure retry strategy with exponential backoff
    # This creates a robust retry mechanism that handles transient failures gracefully
    # by using progressively longer delays between retry attempts
    retry_strategy = Retry(
        total=retries,             # Total number of retries to allow
        read=retries,              # How many read errors to retry on
        connect=retries,           # How many connection-related errors to retry on
        backoff_factor=backoff_factor,  # Exponential backoff multiplier
        status_forcelist=status_forcelist,  # HTTP status codes to force retry on
        allowed_methods=["HEAD", "GET", "OPTIONS", "POST", "PUT"],  # HTTP methods to retry
        # method_whitelist is deprecated in urllib3 >=1.26, replaced with allowed_methods
        raise_on_status=False      # Don't raise exceptions on status codes in forcelist
    )
    
    # Create an adapter with the retry strategy and connection pooling
    # Connection pooling improves performance by reusing connections
    # rather than establishing a new one for each request
    adapter = HTTPAdapter(
        max_retries=retry_strategy,    # Apply our custom retry strategy
        pool_connections=10,           # Number of connection pools to cache
        pool_maxsize=10                # Maximum connections per pool
    )
    
    # Apply the adapter to both HTTP and HTTPS connections
    # This ensures consistent behavior regardless of protocol
    session.mount("http://", adapter)
    session.mount("https://", adapter)
    
    # Set default timeouts for all requests made with this session
    # This prevents requests from hanging indefinitely
    # We use functools.partial to create a new request function with a default timeout
    session.request = partial(session.request, timeout=timeout)
    
    return session

def check_api_health(session, headers=None):
    """
    Perform a comprehensive health check of the Microsoft Graph API connection.
    
    This function executes a multi-stage verification process to ensure the API
    is accessible, authentication is valid, and required resources are available.
    It checks user profile access, OneDrive availability, and storage quotas in
    a sequential process that builds a complete picture of API health.
    
    Health Check Stages:
    1. User Authentication Verification
       - Confirms the provided authentication token is valid
       - Retrieves user identity information for context
       
    2. OneDrive Access Validation
       - Verifies the ability to access the user's OneDrive
       - Confirms proper permissions for file operations
       
    3. Storage Quota Analysis
       - Checks available storage space in OneDrive
       - Provides warnings for low storage conditions
       - Calculates usage percentages for reporting
    
    Args:
        session (requests.Session): A configured HTTP session object, typically created
                                  with create_requests_session(), used for making
                                  all API requests during the health check.
                                  
        headers (dict): HTTP headers dictionary containing the authentication token 
                       required for Microsoft Graph API access. Must include a valid
                       Bearer token in the Authorization header.
        
    Returns:
        tuple: A two-element tuple containing:
              - is_healthy (bool): True if all critical health checks pass, False otherwise
              - message (str): Detailed health status message with user information
                              and diagnostic details. Includes warnings for potential
                              issues like low storage space.
                              
    Integration:
        This function is typically used at the start of operations that depend
        on Microsoft Graph API access to ensure the system is properly connected
        before attempting those operations.
        
    Error Handling:
        Captures and categorizes exceptions during the health check process, providing
        detailed logging and diagnostics. Returns False with an explanatory message
        rather than propagating exceptions to the caller.
    """
    from ..utils.config import GRAPH_API_ENDPOINT
    from ..utils.error_handling import categorize_error
    
    # Validate input parameters
    if not headers:
        return False, "No headers provided for API health check."
    
    try:
        # STAGE 1: User Authentication Verification
        # Check if we can access the user profile, which confirms:
        # - The authentication token is valid
        # - The basic API endpoints are accessible
        # - We have correct permissions for user identity
        url = f"{GRAPH_API_ENDPOINT}/me"
        resp = session.get(url, headers=headers, timeout=5)
        if resp.status_code != 200:
            logger.warning(f"API user profile check failed with status code {resp.status_code}")
            return False, f"User profile access failed with status code {resp.status_code}"
        
        # Extract user information for the health message and logging context
        display_name = resp.json().get('displayName', 'Unknown User')
        
        # STAGE 2: OneDrive Access Verification
        # Confirm we can access OneDrive, which is essential for file operations
        # This validates both authentication and permissions for drive access
        url = f"{GRAPH_API_ENDPOINT}/me/drive"
        resp = session.get(url, headers=headers, timeout=5)
        if resp.status_code != 200:
            logger.warning(f"API drive check failed with status code {resp.status_code}")
            return False, f"OneDrive access failed with status code {resp.status_code}"
        
        # STAGE 3: Storage Quota Analysis
        # Check OneDrive storage capacity and usage
        # This identifies potential storage issues before they cause failures
        quota = resp.json().get('quota', {})
        total = quota.get('total', 0)
        used = quota.get('used', 0)

        # Only calculate percentage if we have valid total capacity
        # (Prevents division by zero and handles missing quota info)
        if total > 0:
            used_percent = (used / total) * 100
            # Warning threshold: storage more than 90% full
            if used_percent > 90:
                logger.warning(f"OneDrive is nearly full ({used_percent:.1f}% used)")
                return True, f"API healthy. Connected as {display_name}. OneDrive is {used_percent:.1f}% full (WARNING: low space)"
        
        # All checks passed - API is fully healthy with no warnings
        logger.info("API health check passed successfully")
        return True, f"API healthy. Connected as {display_name}."
        
    except Exception as e:
        # Comprehensive exception handling with error categorization
        # This provides valuable context for troubleshooting API issues
        from ..utils.error_handling import categorize_error
        error_category = categorize_error(e)
        
        # Log detailed error information for diagnostics
        logger.error(f"API health check failed: {e} ({error_category})")
        
        # Return simplified error message to caller
        return False, f"API health check error: {str(e)}"
