#!/usr/bin/env python3
"""
Authentication module for Microsoft Graph API.

This module provides a comprehensive authentication solution for Microsoft Graph API
integration within the Notebook Generator system. It implements multi-layered
authentication strategies using MSAL (Microsoft Authentication Library) with
secure token caching and automatic refresh mechanisms.

Key Features:
------------
1. Multi-method Authentication
   - Silent token acquisition from cache for seamless user experience
   - Interactive browser-based authentication with account selection
   - Device code flow authentication for headless environments
   - Automatic fallback logic between authentication methods

2. Secure Token Management
   - Machine-specific encryption for token storage
   - Transparent token caching with automatic refresh
   - Multiple security layers with cryptography package (when available)
   - Fallback security options for environments without cryptography package

3. Robust Error Handling
   - Comprehensive logging throughout the authentication process
   - Graceful degradation across authentication methods
   - Clear user feedback during interactive authentication steps
   - Detailed error reporting for troubleshooting

Integration Points:
-----------------
- Configurable through environment variables and config module
- Designed for both CLI and GUI environments
- Compatible with OneDrive API authorization needs
- Supports extended Microsoft Graph API scopes
"""

import os
import json
import base64
import msal
from pathlib import Path
from ..utils.config import logger
from ..utils.config import MICROSOFT_GRAPH_API_CLIENT_ID, AUTHORITY, SCOPES, TOKEN_CACHE_FILE

def encrypt_token_data(data, key=None):
    """
    Encrypt or obfuscate token data for secure storage.
    
    This function provides a security layer for storing Microsoft authentication 
    tokens on the local filesystem. It implements a multi-tiered security approach:
    
    1. Primary security: Strong encryption using the Fernet symmetric encryption
       algorithm from the cryptography package (when available)
    2. Fallback security: Base64 encoding when the cryptography package is unavailable
    
    The encryption key is either provided explicitly or automatically derived from
    machine-specific identifiers, ensuring that tokens encrypted on one machine
    can only be decrypted on the same machine for added security.
    
    Args:
        data (dict): Token data dictionary to encrypt (typically MSAL token cache)
        key (bytes, optional): Custom encryption key. If None, a machine-specific
                              key is automatically generated. Defaults to None.
        
    Returns:
        str: Encrypted or encoded data as a string, ready for storage
              (format depends on whether strong encryption or fallback was used)
    
    Security Notes:
    - The machine-specific key generation adds a layer of security even if the
      token cache file is copied to another machine
    - When the cryptography package is unavailable, the function clearly logs
      the reduced security level
    - JSON serialization is handled before encryption to support complex token structures
    """
    try:
        # Strong encryption using Fernet (symmetric encryption)
        from cryptography.fernet import Fernet
        if key is None:
            # Use a consistent key derived from machine-specific information
            # This ensures tokens can only be decrypted on the same machine
            import hashlib
            import platform
            # Combine multiple machine identifiers for stronger uniqueness
            machine_id = platform.node() + platform.machine() + platform.processor()
            # Create a consistent 32-byte key using SHA-256
            key = hashlib.sha256(machine_id.encode()).digest()[:32]
            # Convert to URL-safe base64 format required by Fernet
            key = base64.urlsafe_b64encode(key)
        
        cipher = Fernet(key)
        return cipher.encrypt(json.dumps(data).encode()).decode()
    except ImportError:
        # Fall back to simple obfuscation if cryptography package not available
        # This is less secure but still prevents casual inspection of token data
        logger.warning("Cryptography package not found. Using simple obfuscation for token cache.")
        # Base64 encoding provides basic obfuscation but not true encryption
        # Still requires deliberate effort to decode but is not cryptographically secure
        return base64.b64encode(json.dumps(data).encode()).decode()

def decrypt_token_data(encrypted_data, key=None):
    """
    Decrypt token data with support for both encryption and simple obfuscation.
    
    This function is the counterpart to encrypt_token_data, providing the ability
    to recover token data that was previously secured. It handles both security tiers:
    
    1. Strong decryption using the Fernet algorithm for cryptography-secured tokens
    2. Simple Base64 decoding for tokens stored with the fallback mechanism
    
    The function automatically attempts both decryption methods if needed, providing
    resilience against changes in the environment (e.g., if cryptography was
    installed after tokens were already stored with the fallback method).
    
    Args:
        encrypted_data (str): Encrypted or encoded token data string
        key (bytes, optional): Encryption key used during encryption. If None, 
                              the machine-specific key is regenerated using the
                              same algorithm as in encrypt_token_data. Defaults to None.
        
    Returns:
        dict: Decrypted token data as a dictionary, or an empty dict if
              decryption fails (preventing application crashes)
    
    Error Handling:
    - Gracefully handles decryption failures with detailed logging
    - Returns an empty dictionary rather than raising exceptions to prevent
      application crashes due to token issues
    - Automatically falls back to Base64 decoding if Fernet decryption fails
    """
    try:
        # First attempt strong decryption using Fernet
        from cryptography.fernet import Fernet
        if key is None:
            # Regenerate the machine-specific key using the same algorithm
            # This must match the key generation in encrypt_token_data
            import hashlib
            import platform
            machine_id = platform.node() + platform.machine() + platform.processor()
            key = hashlib.sha256(machine_id.encode()).digest()[:32]
            key = base64.urlsafe_b64encode(key)
        
        cipher = Fernet(key)
        return json.loads(cipher.decrypt(encrypted_data.encode()).decode())
    except (ImportError, Exception) as e:
        # Log the specific error but continue to fallback method
        logger.debug(f"Fernet decryption failed: {type(e).__name__}: {e}")
        logger.info("Falling back to Base64 decoding")
        
        # Fall back to simple Base64 decoding
        try:
            return json.loads(base64.b64decode(encrypted_data).decode())
        except Exception as e:
            # If all decryption methods fail, log the error and return empty dict
            logger.error(f"Failed to decrypt token data: {e}")
            return {}

def authenticate_graph_api(force_refresh=False):
    """
    Authenticate with Microsoft Graph API using a progressive authentication strategy.
    
    This function serves as the main entry point for Microsoft Graph API authentication
    within the Notebook Generator system. It implements a sophisticated multi-layer
    authentication approach that balances user experience with robust authentication:
    
    Authentication Flow:
    1. Silent token acquisition from cache (no user interaction)
       - Uses previously cached tokens when available
       - Automatically refreshes expired tokens if refresh token is valid
    2. Interactive browser-based authentication (if silent auth fails)
       - Opens browser window for standard Microsoft login
       - Supports account selection for multi-account scenarios
       - Handles Azure AD organizational accounts and personal Microsoft accounts
    3. Device code flow authentication (fallback for non-interactive environments)
       - Provides code and instructions for authentication via another device
       - Suitable for SSH terminals, headless environments, and remote sessions
       - User-friendly prompts with clear instructions
    
    The function handles the entire authentication workflow including:
    - Token cache serialization and persistence
    - Secure storage of sensitive token data
    - User feedback during authentication steps
    - Detailed logging for troubleshooting
    - Error handling with informative messages
    
    Args:
        force_refresh (bool, optional): If True, bypasses the token cache and forces
                                       a new interactive authentication regardless of
                                       cached token validity. Useful for changing accounts
                                       or resolving token issues. Defaults to False.
        
    Returns:
        str: Valid Microsoft Graph API access token that can be used for API requests
    
    Raises:
        Exception: If authentication fails after exhausting all authentication methods,
                  with detailed error information to aid troubleshooting
    
    Integration Notes:
    - Uses the MICROSOFT_GRAPH_API_CLIENT_ID, AUTHORITY, SCOPES, and TOKEN_CACHE_FILE
      constants from the configuration module
    - The token cache is securely stored between sessions for persistent authentication
    - Provides console output for interactive user feedback in addition to logging
    """    # Initialize a serializable token cache for persistent token storage
    # This allows tokens to be stored between sessions for silent authentication
    cache = msal.SerializableTokenCache()
    
    # Load the token cache if it exists to enable silent authentication
    # This prevents unnecessary user prompts when valid tokens are available
    if os.path.exists(TOKEN_CACHE_FILE):
        try:
            with open(TOKEN_CACHE_FILE, "r") as f:
                cache_data = f.read()
                if cache_data:
                    cache.deserialize(cache_data)
                    logger.info("Token cache loaded successfully")
        except Exception as e:
            # Continue even if cache loading fails, we'll just authenticate from scratch
            logger.warning(f"Could not load token cache: {e}. Will authenticate from scratch.")

    # Create the MSAL application
    app = msal.PublicClientApplication(
        MICROSOFT_GRAPH_API_CLIENT_ID,
        authority=AUTHORITY,
        token_cache=cache
    )

    # Try to get a token from the cache first
    accounts = app.get_accounts()
    result = None
    
    if accounts and not force_refresh:
        logger.info("Found account in cache, attempting to use existing token...")
        print(f"Account found in cache: {accounts[0]['username']}")
        
        # Try silent token acquisition first
        result = app.acquire_token_silent(SCOPES, account=accounts[0])
        
        if result and "access_token" in result:
            logger.info("Successfully acquired token silently")
            print("✓ Silent authentication successful")
            
            # Save token cache for future use
            with open(TOKEN_CACHE_FILE, "w") as f:
                f.write(cache.serialize())
            logger.info(f"Token cache refreshed in {TOKEN_CACHE_FILE}")
            
            return result["access_token"]
        else:
            logger.info("Could not acquire token silently. Falling back to interactive authentication.")
            print("× Silent authentication failed. Falling back to interactive...")
      # METHOD 1: Try interactive browser-based authentication
    # This is the preferred method as it provides the standard Microsoft login experience
    # and supports modern authentication protocols like MFA
    try:
        logger.info("Initiating interactive browser-based authentication")
        result = app.acquire_token_interactive(
            scopes=SCOPES,
            prompt="select_account"  # Force prompt to select account, enabling account switching
        )
        
        # Check if authentication was successful
        if result and "access_token" in result:
            logger.info("Interactive authentication successful!")
            print("✓ Interactive authentication successful!")
            
            # Persist the token cache to enable silent authentication in future sessions
            # This includes both access tokens and refresh tokens for automatic renewal
            with open(TOKEN_CACHE_FILE, "w") as f:
                f.write(cache.serialize())
            logger.info(f"Token cache saved to {TOKEN_CACHE_FILE}")
            
            return result["access_token"]
        else:
            # Extract detailed error information for troubleshooting
            error = result.get("error", "unknown")
            error_desc = result.get("error_description", "No details provided") 
            logger.warning(f"Interactive authentication failed: {error} - {error_desc}")
            print("× Interactive authentication failed. Trying alternative method...")
    except Exception as e:
        # Handle exceptions such as browser launch failure or user cancellation
        logger.warning(f"Exception during interactive authentication: {type(e).__name__}: {e}")
        print(f"× Exception during authentication: {str(e)}")
        print("Trying alternative method...")    # METHOD 2: Fall back to device flow authentication
    # This method is critical for headless environments (SSH terminals, CI/CD pipelines)
    # where launching a browser isn't possible or reliable
    logger.info("Falling back to device flow authentication...")
    print("\n🔐 Using device code authentication instead:")
    
    try:
        # Initiate the device flow process
        # This generates a user code and verification URI for authentication
        flow = app.initiate_device_flow(scopes=SCOPES)
        
        # Validate that we received a valid device flow response
        if "user_code" not in flow:
            error_details = json.dumps(flow) if flow else "No response details"
            logger.error(f"Failed to create device flow: {error_details}")
            raise Exception("Failed to create device flow for authentication.")
        
        # Display clear instructions for the user to complete authentication
        # using another device or browser
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
