#!/usr/bin/env python3
"""
Graph API Authentication Module for MBA Notebook Automation System

This module provides specialized authentication for Microsoft Graph API
to support OneDrive and SharePoint integration within the MBA Notebook
Automation system. It extends the core Microsoft authentication with
Graph API-specific functionality.

Key Features:
------------
1. Simplified Graph API Authentication
   - Streamlined authentication with minimal user interaction
   - Automatic token refresh and caching for persistent sessions
   - Support for both interactive and non-interactive environments
   
2. API-Specific Token Management
   - Graph API scopes customization
   - OneDrive-specific permission handling
   - API version compatibility management
   
Integration Points:
-----------------
- Builds on core Microsoft authentication
- Used by OneDrive sharing and file operations modules
- Supports the video and resource processing pipelines
"""

import os
import json
import msal
import requests
from pathlib import Path

from ..utils.config import setup_logging, MICROSOFT_GRAPH_API_CLIENT_ID, AUTHORITY, SCOPES, TOKEN_CACHE_FILE

# Set up logging
logger, failed_logger = setup_logging()

def authenticate_graph_api():
    """
    Authenticate with Microsoft Graph API and return an access token.
    
    This function handles the complete authentication flow for Microsoft Graph API:
    1. Try to get a token silently from the token cache
    2. If that fails, try interactive authentication
    3. Return the access token or None if authentication failed
    
    Returns:
        str: Access token for Microsoft Graph API or None if authentication failed
    """
    try:
        logger.debug("Starting Graph API authentication")
        
        # Initialize token cache
        cache = msal.SerializableTokenCache()
        
        # Load the token cache from file if it exists
        if os.path.exists(TOKEN_CACHE_FILE):
            with open(TOKEN_CACHE_FILE, 'r') as f:
                cache.deserialize(f.read())
                logger.debug("Token cache loaded from file")
        
        # Create the MSAL app
        app = msal.PublicClientApplication(
            MICROSOFT_GRAPH_API_CLIENT_ID,
            authority=AUTHORITY,
            token_cache=cache
        )
        
        # Try to get a token silently
        logger.debug("Attempting silent token acquisition")
        accounts = app.get_accounts()
        if accounts:
            logger.debug(f"Found {len(accounts)} accounts in cache")
            result = app.acquire_token_silent(SCOPES, account=accounts[0])
            if result:
                logger.info("Successfully acquired token silently")
                
                # Save the token cache
                with open(TOKEN_CACHE_FILE, 'w') as f:
                    f.write(cache.serialize())
                    logger.debug("Token cache saved to file")
                
                return result['access_token']
        
        # If silent acquisition failed, try interactive authentication
        logger.debug("Silent token acquisition failed, attempting interactive authentication")
        result = app.acquire_token_interactive(SCOPES)
        
        if result:
            logger.info("Successfully acquired token interactively")
            
            # Save the token cache
            with open(TOKEN_CACHE_FILE, 'w') as f:
                f.write(cache.serialize())
                logger.debug("Token cache saved to file")
            
            return result['access_token']
        else:
            logger.error("Failed to acquire token interactively")
            return None
    
    except Exception as e:
        logger.error(f"Error in Graph API authentication: {e}")
        return None

def validate_token(access_token):
    """
    Validate an access token by making a simple Graph API call.
    
    Args:
        access_token (str): Access token to validate
        
    Returns:
        bool: True if the token is valid, False otherwise
    """
    try:
        # Make a simple call to the Graph API
        url = "https://graph.microsoft.com/v1.0/me"
        headers = {
            'Authorization': f'Bearer {access_token}'
        }
        
        response = requests.get(url, headers=headers)
        return response.status_code == 200
    
    except Exception as e:
        logger.error(f"Error validating access token: {e}")
        return False
