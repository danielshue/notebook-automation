#!/usr/bin/env python3
"""
Test script to verify that the configuration is properly loaded.
"""
import sys
import os

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from tools.utils.config import setup_logging
from tools.utils.config import ONEDRIVE_LOCAL_RESOURCES_ROOT, VAULT_LOCAL_ROOT, METADATA_FILE, ONEDRIVE_BASE, VIDEO_EXTENSIONS, logger, setup_logging

def main():
    # Set up logging
    main_logger, _ = setup_logging(debug=True)
    
    # Print the loaded configuration values
    main_logger.info("=== Configuration Test ===")
    main_logger.info(f"RESOURCES_ROOT: {ONEDRIVE_LOCAL_RESOURCES_ROOT}")
    main_logger.info(f"VAULT_ROOT: {VAULT_LOCAL_ROOT}")
    main_logger.info(f"METADATA_FILE: {METADATA_FILE}")
    main_logger.info(f"ONEDRIVE_BASE: {ONEDRIVE_BASE}")
    main_logger.info(f"VIDEO_EXTENSIONS: {VIDEO_EXTENSIONS}")
    main_logger.info("=== Test Complete ===")

if __name__ == "__main__":
    main()
