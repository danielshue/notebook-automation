#!/usr/bin/env python3
"""
Script to update imports from mba_notebook_automation to notebook_automation.
"""

import os
import re
from pathlib import Path

def fix_imports_in_file(file_path):
    """Fix imports in a single file."""
    print(f"Processing {file_path}")
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Replace imports
    replacements = [
        ('from notebook_automation.mba_notebook_automation.', 'from notebook_automation.'),
        ('import notebook_automation.mba_notebook_automation.', 'import notebook_automation.'),
        ('from mba_notebook_automation.', 'from notebook_automation.'),
        ('import mba_notebook_automation.', 'import notebook_automation.'),
    ]
    
    new_content = content
    for old, new in replacements:
        new_content = new_content.replace(old, new)
    
    if new_content != content:
        print(f"Updating imports in {file_path}")
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)

def main():
    """Fix all imports in the project."""
    root = Path('d:/repos/mba-notebook-automation/notebook_automation')
    for file_path in root.rglob('*.py'):
        fix_imports_in_file(file_path)

if __name__ == '__main__':
    main()
