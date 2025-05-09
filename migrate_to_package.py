#!/usr/bin/env python3
"""
Migration Script for MBA Notebook Automation

This script helps users migrate from the old file structure to the new package-based structure.
It provides guidance on how to update imports and run the tools using the new package structure.

Usage:
------
python migrate_to_package.py
"""

import os
import sys
from pathlib import Path
import shutil

def print_header(text):
    """Print a formatted header."""
    print("\n" + "=" * 80)
    print(f"  {text}")
    print("=" * 80 + "\n")

def install_package():
    """Install the package in development mode."""
    print("Installing the package in development mode...")
    
    try:
        import subprocess
        result = subprocess.run(
            ["pip", "install", "-e", "."],
            capture_output=True,
            text=True,
            check=True
        )
        print("✓ Package installed successfully!")
        print(result.stdout)
        return True
    except Exception as e:
        print(f"! Error installing package: {e}")
        if hasattr(e, 'output'):
            print(e.output)
        return False

def main():
    """Main entry point for the migration script."""
    print_header("MBA Notebook Automation - Migration to Package Structure")
    
    # Check if the script is run from the project root
    if not os.path.exists("setup.py"):
        print("! This script must be run from the project root directory.")
        print("  Please navigate to the directory containing setup.py and try again.")
        return 1
    
    # Check if the package is already installed
    try:
        import mba_notebook_automation
        print("✓ The mba_notebook_automation package is already importable.")
    except ImportError:
        print("! The mba_notebook_automation package is not yet installable.")
        print("  Installing the package in development mode...")
        
        if not install_package():
            print("\n  If automatic installation failed, try manually:")
            print("  pip install -e .")
            return 1
      # Explain the changes
    print("\nThe project has been reorganized into a proper Python package structure.")
    print("This provides several benefits:")
    print("  - Reliable imports between modules")
    print("  - Easier installation and distribution")
    print("  - Better organization and maintainability")
    print("  - Command-line script entry points\n")
    
    print("Key changes:")
    print("  1. Scripts are now organized in the 'mba_notebook_automation' package")
    print("  2. Imports should use the package structure")
    print("  3. Scripts can be run as modules with 'python -m mba_notebook_automation.module_name'")
    print("  4. After installation, command-line tools are available system-wide\n")
    
    print("Import examples:")
    print("  Old: from tools.utils.config import ConfigDict")
    print("  New: from mba_notebook_automation.tools.utils.config import ConfigDict\n")
    
    print("  Old: import generate_pdf_notes_from_onedrive")
    print("  New: from mba_notebook_automation import generate_pdf_notes_from_onedrive\n")
    
    print("Running scripts:")
    print("  Old: python configure.py show")
    print("  New: python -m mba_notebook_automation.configure show")
    
    # Ask if the user wants to run the import updater
    print("\n" + "=" * 80)
    print("Automatic Import Statement Migration")
    print("=" * 80)
    print("\nWould you like to automatically update import statements in your custom scripts?")
    print("This will modify Python files to use the new package structure.")
    response = input("Run automatic import updater? (y/n) [y]: ").lower() or "y"
    
    if response.startswith('y'):
        print("\nRunning import updater...")
        try:
            import subprocess
            # Run the import updater script
            result = subprocess.run(
                ["python", "update_imports.py", ".", "--recursive"],
                capture_output=True,
                text=True,
                check=True
            )
            print(result.stdout)
            
            # Ask if they want to see a detailed report
            if "Updated " in result.stdout:
                print("\nFor more details, you can run:")
                print("  python update_imports.py . --recursive --dry-run")
        except Exception as e:
            print(f"Error running import updater: {e}")
            print("You can manually update imports using:")
            print("  python update_imports.py . --recursive")
    print("   Or: mba-configure show (after installation)\n")
    
    # Check if setup.py exists
    if not Path("setup.py").exists():
        print("! setup.py not found. This script should be run from the project root directory.")
        return 1
    
    # Provide next steps
    print_header("Next Steps")
    print("1. Install the package in development mode:")
    print("   pip install -e .\n")
    
    print("2. Update any custom scripts to use the new import structure:")
    print("   from mba_notebook_automation.tools.utils.config import ConfigDict\n")
    
    print("3. Run the commands using the new entry points:")
    print("   mba-configure show")
    print("   mba-add-nested-tags --dry-run --verbose\n")
    
    print("4. Or run modules directly:")
    print("   python -m mba_notebook_automation.configure show")
    print("   python -m mba_notebook_automation.tags.add_nested_tags --dry-run --verbose\n")
    
    print("If you encounter any issues, please refer to the documentation or report them.")
    
    return 0

if __name__ == "__main__":
    sys.exit(main())
