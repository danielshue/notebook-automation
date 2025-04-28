#!/usr/bin/env python3
"""
Authentication Troubleshooter for Microsoft Graph API

This script helps diagnose Microsoft Graph API authentication issues by:
1. Testing network connectivity
2. Validating authentication flows (interactive and device)
3. Providing detailed error logging

Usage:
    python3 test_authenticate_troubleshoot.py
"""

import os
import sys
import json
import requests
import msal
import webbrowser
from datetime import datetime

import sys
import os

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from tools.utils.config import setup_logging
from tools.utils.config import MICROSOFT_GRAPH_API_CLIENT_ID, AUTHORITY, SCOPES, TOKEN_CACHE_FILE

# Setup logging and get the logger instance
logger = setup_logging(True, "auth_troubleshoot.log", "auth_troubleshoot_failed.log")
if isinstance(logger, tuple):
    # If setup_logging returns a tuple, extract the logger
    logger = logger[0]  # Assuming the first element is the logger

def test_network_connectivity():
    """Test network connectivity to Microsoft authentication endpoints."""
    logger.info("Testing network connectivity...")
    endpoints = [
        "https://login.microsoftonline.com",
        "https://graph.microsoft.com/v1.0/",
    ]
    
    for endpoint in endpoints:
        try:
            logger.info(f"Testing connection to {endpoint}")
            response = requests.get(endpoint, timeout=10)
            logger.info(f"Response status: {response.status_code}")
            
            if response.status_code < 400 or response.status_code == 401:
                # 401 is actually expected for unauthenticated requests
                logger.info(f"✅ Connection to {endpoint} successful")
                print(f"✅ Connection to {endpoint} successful")
            else:
                logger.error(f"❌ Connection to {endpoint} failed with status {response.status_code}")
                print(f"❌ Connection to {endpoint} failed with status {response.status_code}")
        except Exception as e:
            logger.error(f"❌ Connection to {endpoint} failed: {e}")
            print(f"❌ Connection to {endpoint} failed: {e}")
            return False
    
    return True

def test_msal_imports():
    """Test that MSAL is properly installed and imported."""
    logger.info("Testing MSAL imports...")
    
    try:
        import msal
        logger.info(f"✅ MSAL version: {msal.__version__}")
        print(f"✅ MSAL version: {msal.__version__}")
        return True
    except ImportError as e:
        logger.error(f"❌ MSAL import error: {e}")
        print(f"❌ MSAL import error: {e}")
        print("Please install MSAL with: pip install msal")
        return False

def verify_token_cache():
    """Check if token cache exists and is readable."""
    if os.path.exists(TOKEN_CACHE_FILE):
        try:
            with open(TOKEN_CACHE_FILE, "r") as f:
                cache_content = f.read()
                if cache_content:
                    logger.info(f"✅ Token cache exists and is readable ({len(cache_content)} bytes)")
                    print(f"✅ Token cache exists and is readable ({len(cache_content)} bytes)")
                else:
                    logger.warning("⚠️ Token cache exists but is empty")
                    print("⚠️ Token cache exists but is empty")
        except Exception as e:
            logger.error(f"❌ Error reading token cache: {e}")
            print(f"❌ Error reading token cache: {e}")
            return False
    else:
        logger.info("ℹ️ Token cache does not exist (will be created during authentication)")
        print("ℹ️ Token cache does not exist (will be created during authentication)")
    
    return True

def test_interactive_auth():
    """Test interactive authentication flow with PKCE (recommended for desktop applications)."""
    logger.info("Testing interactive authentication flow...")
    
    # Set up the token cache
    cache = msal.SerializableTokenCache()
    
    # Create MSAL app
    app = msal.PublicClientApplication(
        MICROSOFT_GRAPH_API_CLIENT_ID,
        authority=AUTHORITY,
        token_cache=cache
    )
    
    # Check if we have accounts in the cache
    accounts = app.get_accounts()
    if accounts:
        logger.info("Account found in token cache, attempting to use existing token...")
        print("Account found in token cache, attempting to use existing token...")
        result = app.acquire_token_silent(SCOPES, account=accounts[0])
        if result and "access_token" in result:
            logger.info("✅ Token acquired from cache successfully!")
            print("✅ Token acquired from cache successfully!")
            return True
    
    # No suitable token in cache, initiate interactive authentication
    logger.info("Initiating interactive authentication flow...")
    print("\nInitiating interactive authentication...\nA browser window will open for you to sign in.")
    
    try:
        # This will automatically open the default web browser for authentication
        result = app.acquire_token_interactive(
            scopes=SCOPES,
            prompt="select_account"  # Force prompt to select account
        )
        
        if "access_token" in result:
            logger.info("✅ Interactive authentication successful!")
            print("✅ Interactive authentication successful!")
            
            # Save token cache for future use
            with open(TOKEN_CACHE_FILE, "w") as f:
                f.write(cache.serialize())
            logger.info(f"✅ Token cache saved to {TOKEN_CACHE_FILE}")
            print(f"✅ Token cache saved to {TOKEN_CACHE_FILE}")
            
            return True
        else:
            error = result.get("error")
            error_desc = result.get("error_description") 
            logger.error(f"❌ Interactive authentication failed: {error} - {error_desc}")
            print(f"❌ Interactive authentication failed: {error} - {error_desc}")
            return False
    
    except Exception as e:
        logger.error(f"❌ Exception during interactive authentication: {type(e).__name__}: {e}")
        print(f"❌ Exception during interactive authentication: {type(e).__name__}: {e}")
        return False

def test_device_flow():
    """Test device flow authentication."""
    logger.info("Testing device flow authentication...")
      # Set up the token cache
    cache = msal.SerializableTokenCache()
      # Create MSAL app
    app = msal.PublicClientApplication(
        MICROSOFT_GRAPH_API_CLIENT_ID,
        authority=AUTHORITY,
        token_cache=cache
    )
    
    # Test device flow
    logger.info("Initiating device flow...")
    try:
        flow = app.initiate_device_flow(scopes=SCOPES)
        
        if "user_code" in flow:
            logger.info("✅ Device flow created successfully!")
            print("\n✅ Device flow created successfully!")
            print("\nTo authenticate, use a browser to visit:")
            print(flow["verification_uri"])
            print("and enter the code:")
            print(flow["user_code"])
            print("")
            
            # Attempt to acquire token
            logger.info("Waiting for user to complete authentication...")
            result = app.acquire_token_by_device_flow(flow)
            
            if "access_token" in result:
                logger.info("✅ Token acquired successfully!")
                print("✅ Token acquired successfully!")
                
                # Save token cache for future use
                with open(TOKEN_CACHE_FILE, "w") as f:
                    f.write(cache.serialize())
                logger.info(f"✅ Token cache saved to {TOKEN_CACHE_FILE}")
                print(f"✅ Token cache saved to {TOKEN_CACHE_FILE}")
                
                return True
            else:
                error = result.get("error")
                error_desc = result.get("error_description") 
                logger.error(f"❌ Token acquisition failed: {error} - {error_desc}")
                print(f"❌ Token acquisition failed: {error} - {error_desc}")
                return False
        else:
            error_details = json.dumps(flow) if flow else "No response details"
            logger.error(f"❌ Device flow creation failed. Details: {error_details}")
            print(f"❌ Device flow creation failed.")
            print(f"Details: {error_details}")
            return False
            
    except Exception as e:
        logger.error(f"❌ Exception during device flow test: {type(e).__name__}: {e}")
        print(f"❌ Exception during device flow test: {type(e).__name__}: {e}")
        return False

def run_diagnostic():
    """Run all diagnostic tests."""
    print("\n==== Microsoft Graph API Authentication Diagnostic ====\n")
    logger.info("Starting authentication diagnostic")
    
    # Test network connectivity
    if not test_network_connectivity():
        print("\n❌ Network connectivity issues detected. Please check your internet connection.")
        return False
    
    # Test MSAL imports
    if not test_msal_imports():
        print("\n❌ MSAL library issues detected. Please check your Python environment.")
        return False
    
    # Verify token cache
    verify_token_cache()
    
    print("\nChoose authentication method:")
    print("1. Interactive authentication (recommended for desktop apps)")
    print("2. Device code flow (requires app to be registered as mobile)")
    
    choice = input("\nEnter your choice (1 or 2): ")
    
    if choice == "1":
        # Try interactive authentication first (recommended for desktop apps)
        if test_interactive_auth():
            print("\n✅ All tests passed! Authentication is working correctly.")
            logger.info("All diagnostic tests passed with interactive auth")
            return True
        else:
            print("\n❌ Interactive authentication failed. Please check the logs.")
            logger.error("Interactive authentication diagnostic failed")
            return False
    else:
        # Fall back to device flow authentication
        if test_device_flow():
            print("\n✅ All tests passed! Authentication is working correctly.")
            logger.info("All diagnostic tests passed with device flow")
            return True
        else:
            print("\n❌ Device flow authentication failed. See error details above.")
            logger.error("Device flow authentication diagnostic failed")
            return False

if __name__ == "__main__":
    run_diagnostic()
