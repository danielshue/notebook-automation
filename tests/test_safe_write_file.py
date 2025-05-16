#!/usr/bin/env python3
"""
Test script for the safe_write_file function.

This script tests the safe_write_file function under different conditions 
and with various path formats to ensure it can properly handle WSL/Windows
path issues.
"""

import os
import sys
import logging
import tempfile
from pathlib import Path

# Configure logging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Add the project root to the Python path to import our modules
project_root = str(Path(__file__).parent.parent.absolute())
if project_root not in sys.path:
    sys.path.append(project_root)

from notebook_automation.tools.notes.note_markdown_generator import safe_write_file, is_wsl

def test_writing_to_various_paths():
    """Test writing files to different paths and handle potential issues."""
    running_in_wsl = is_wsl()
    logger.info(f"Running in WSL: {running_in_wsl}")
    
    # Create a test content
    content = "# Test Content\n\nThis is a test file created by the test_safe_write_file.py script.\n"
    
    # Test with a temporary local file path
    with tempfile.NamedTemporaryFile(mode='w', delete=False, suffix='.md') as temp:
        temp_path = temp.name
    
    logger.info(f"Testing with temporary file: {temp_path}")
    result1 = safe_write_file(temp_path, content)
    logger.info(f"Result with local temp path: {result1}")
    
    # Read back the content to verify
    try:
        with open(temp_path, 'r', encoding='utf-8') as f:
            read_content = f.read()
        logger.info(f"Content verification: {'SUCCESS' if content == read_content else 'FAILED'}")
    except Exception as e:
        logger.error(f"Error reading back temporary file: {e}")
    
    # Clean up
    try:
        os.unlink(temp_path)
    except:
        pass
    
    # If running in WSL, test with a Windows path
    if running_in_wsl:
        # Create test paths to test with
        windows_paths = [
            "/mnt/c/temp/test_safe_write_wsl.md",
            "/mnt/c/Users/Public/Documents/test_safe_write_wsl.md"
        ]
        
        for path in windows_paths:
            logger.info(f"Testing with Windows path: {path}")
            result2 = safe_write_file(path, content)
            logger.info(f"Result with Windows path: {result2}")
            
            # Read back the content to verify
            try:
                with open(path, 'r', encoding='utf-8') as f:
                    read_content = f.read()
                logger.info(f"Content verification for {path}: {'SUCCESS' if content == read_content else 'FAILED'}")
            except Exception as e:
                logger.error(f"Error reading back Windows path file: {e}")
                
            # Clean up
            try:
                os.unlink(path)
                logger.info(f"Deleted test file: {path}")
            except Exception as e:
                logger.error(f"Could not delete test file {path}: {e}")
    
    logger.info("Test completed.")

if __name__ == "__main__":
    test_writing_to_various_paths()
