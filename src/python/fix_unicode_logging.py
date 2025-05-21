#!/usr/bin/env python3
"""
Fix Unicode logging in ensure_logger_configured function.

This script modifies the ensure_logger_configured function in config.py 
to properly handle Unicode characters in log messages.
"""

import os
import io
import sys
import re
from pathlib import Path

# Path to config.py
config_path = Path("d:/repos/mba-notebook-automation/notebook_automation/tools/utils/config.py")

if not config_path.exists():
    print(f"Error: Could not find {config_path}")
    sys.exit(1)

# Read the file content
with open(config_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Find the ensure_logger_configured function
pattern = r'def ensure_logger_configured\([^)]*\)[^:]*:[^`]*?return logger'
match = re.search(pattern, content, re.DOTALL)

if not match:
    print("Error: Could not find ensure_logger_configured function in config.py")
    sys.exit(1)

# Original function code
original_func = match.group(0)

# Update function to use io.TextIOWrapper with UTF-8 encoding
updated_func = original_func.replace(
    '    # If we reach here, this logger needs configuration\n    # Configure a basic setup for this specific logger\n    handler = logging.StreamHandler()',
    '    # If we reach here, this logger needs configuration\n    # Configure a basic setup for this specific logger\n    # Use io.TextIOWrapper with UTF-8 encoding to ensure Unicode support\n    stream = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8")\n    handler = logging.StreamHandler(stream)'
)

# Update docstring to mention Unicode support
updated_func = updated_func.replace(
    '    Key features:\n    1. Checks if logger already has handlers before configuring\n    2. Uses a simplified configuration for modules used as libraries\n    3. Preserves logger hierarchy and propagation\n    4. Prevents duplicate log messages',
    '    Key features:\n    1. Checks if logger already has handlers before configuring\n    2. Uses a simplified configuration for modules used as libraries\n    3. Preserves logger hierarchy and propagation\n    4. Prevents duplicate log messages\n    5. Ensures proper Unicode character support with UTF-8 encoding'
)

# Replace the function in the content
new_content = content.replace(original_func, updated_func)

# Add the missing import for io
if 'import io' not in new_content:
    new_content = new_content.replace('import os', 'import os\nimport io')

# Write the updated file
with open(config_path, 'w', encoding='utf-8') as f:
    f.write(new_content)

print(f"Successfully updated {config_path}")
print("The ensure_logger_configured function now properly handles Unicode characters.")
