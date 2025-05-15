#!/usr/bin/env python3
"""Test script to verify config display for all updated CLI tools."""

import os
import sys
from pathlib import Path

def test_config_display():
    print("\n=== Testing All CLI Tools Config Display ===")
    
    # Get the absolute path to the config.json file for testing
    config_path = Path(__file__).parent / "config.json"
    print(f"Setting config path environment variable to: {config_path.absolute()}")
    
    # Set environment variable
    os.environ["NOTEBOOK_CONFIG_PATH"] = str(config_path.absolute())
    
    # Verify that all tools can find the config correctly
    from notebook_automation.tools.utils.config import find_config_path
    found_path = find_config_path()
    print(f"Found config path: {found_path}")
    
    assert str(config_path.absolute()) == found_path, "Config path not matching!"
    print("Config path verification successful!")
    
    return True

if __name__ == "__main__":
    try:
        success = test_config_display()
        print("\n✅ All tests passed!" if success else "\n❌ Tests failed!")
    except Exception as e:
        print(f"\n❌ Error during testing: {e}")
        sys.exit(1)
