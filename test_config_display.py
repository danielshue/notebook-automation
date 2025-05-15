#!/usr/bin/env python3
"""Test script to verify config display for CLI tools."""

import os
import sys
from pathlib import Path

# Get the absolute path to the config.json file for testing
config_path = Path(__file__).parent / "config.json"
print(f"Testing with config path: {config_path}")

# Set environment variable
os.environ["NOTEBOOK_CONFIG_PATH"] = str(config_path.absolute())

# Simple run
from notebook_automation.tools.utils.config import find_config_path
print(f"Found config path: {find_config_path()}")

print("Done!")
