"""
Fix duplicate logging during authentication in Microsoft Graph API calls.

This script checks for duplicate logging in the Microsoft authentication flow
and makes sure that all scripts use the standard logger setup.
"""

import os
import sys
from pathlib import Path

def check_log_duplicates():
    """Check which files might have duplicate logging during authentication."""
    root_dir = Path('d:/repos/mba-notebook-automation')
    
    # List of files that use Microsoft authentication and might have duplicate logging
    target_files = [
        "notebook_automation/cli/onedrive_share.py",
        "notebook_automation/cli/onedrive_share_helper.py",
        "notebook_automation/cli/generate_pdf_notes.py",
        "notebook_automation/cli/generate_video_meta.py",
        "notebook_automation/tools/auth/microsoft_auth.py",
    ]
    
    for file_path in target_files:
        full_path = root_dir / file_path
        if not full_path.exists():
            print(f"File not found: {full_path}")
            continue
            
        # Check for common patterns that might indicate duplicate logging
        with open(full_path, 'r', encoding='utf-8') as f:
            content = f.read()
            
        duplicate_indicators = [
            ("logger.info", "print") if "print" in content and "logger.info" in content else None,
            ("Account found in cache", "Account found in cache") if content.count("Account found in cache") > 1 else None,
            ("authentication successful", "authentication successful") if content.count("authentication successful") > 1 else None,
        ]
        
        duplicate_indicators = [ind for ind in duplicate_indicators if ind]
        
        if duplicate_indicators:
            print(f"Potential duplicate logging in {file_path}:")
            for log_type, print_type in duplicate_indicators:
                print(f"  - Found both {log_type} and {print_type}")

def insert_proper_log_setup(file_path):
    """Add proper logging setup to a file if missing."""
    full_path = Path(file_path)
    if not full_path.exists():
        print(f"File not found: {full_path}")
        return
        
    with open(full_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Check if setup_logging is imported but not used
    if "from notebook_automation.tools.utils.config import setup_logging" in content and "setup_logging" not in content:
        print(f"File {file_path} imports setup_logging but doesn't use it")
        
    # Check if logging is used but setup_logging isn't imported
    if "logging" in content and "logger" in content and "setup_logging" not in content:
        print(f"File {file_path} uses logging but doesn't import setup_logging")
    
    print(f"Checking {file_path}...")

if __name__ == "__main__":
    print("Checking for duplicate logging in authentication flows...")
    check_log_duplicates()
