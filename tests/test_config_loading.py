#!/usr/bin/env python3
"""
Test script for config loading functionality

This script tests the config file loading logic to ensure our changes work properly.
It simulates different ways of loading the configuration:
1. Default loading (using auto-discovery)
2. Environment variable override
3. Command-line parameter
"""

import os
import sys
from pathlib import Path

# Add the project root to the path
sys.path.append(str(Path(__file__).parent.parent))

def test_config_loading():
    """Test various methods of loading the config file."""
    print("\n=== Testing Config File Loading ===")

    # Test 1: Default config loading
    print("\n1. Default config loading:")
    from notebook_automation.tools.utils.config import find_config_path
    default_path = find_config_path()
    print(f"   Default config path: {default_path}")

    # Test 2: Environment variable override
    print("\n2. Environment variable override:")
    test_config_path = str(Path(__file__).parent / "test_config.json")
    # Create a simple test config file
    with open(test_config_path, "w") as f:
        f.write('{"test": "environment_variable_config"}')
    
    os.environ["NOTEBOOK_CONFIG_PATH"] = test_config_path
    from importlib import reload
    import notebook_automation.tools.utils.config
    reload(notebook_automation.tools.utils.config)
    
    env_path = notebook_automation.tools.utils.config.find_config_path()
    print(f"   Environment config path: {env_path}")
    
    # Test 3: Command-line parameter (simulated)
    print("\n3. Command-line parameter:")
    cli_config_path = str(Path(__file__).parent / "cli_test_config.json")
    # Create another simple test config file
    with open(cli_config_path, "w") as f:
        f.write('{"test": "command_line_config"}')
    
    # Remove environment variable to test direct path
    del os.environ["NOTEBOOK_CONFIG_PATH"]
    reload(notebook_automation.tools.utils.config)
    
    direct_path = notebook_automation.tools.utils.config.find_config_path(cli_config_path)
    print(f"   CLI config path: {direct_path}")

    # Clean up test files
    Path(test_config_path).unlink()
    Path(cli_config_path).unlink()
    
    print("\n=== Tests Complete ===\n")

if __name__ == "__main__":
    test_config_loading()
