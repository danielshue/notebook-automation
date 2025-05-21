#!/usr/bin/env python3
"""
Tools Package Loading Test

This script tests that all modules in the tools package structure
can be imported correctly. It systematically checks each module and
reports any import errors encountered.

Usage:
    python3 test_tools_packages.py
"""

import os
import sys
import importlib
import traceback
import time
from pathlib import Path

# Add the parent directory to the path to ensure tools can be imported
parent_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, parent_dir)
sys.path.insert(0, os.path.abspath('.'))

# Print path info for debugging
print(f"Current working directory: {os.getcwd()}")
print(f"Script location: {os.path.abspath(__file__)}")
print(f"Parent directory added to path: {parent_dir}")
print(f"Python path: {sys.path}")

# ANSI colors for formatting terminal output
class Colors:
    GREEN = '\033[92m'
    YELLOW = '\033[93m'
    RED = '\033[91m'
    BLUE = '\033[94m'
    BOLD = '\033[1m'
    ENDC = '\033[0m'

def print_color(color, text):
    """Print text with color in the terminal"""
    print(f"{color}{text}{Colors.ENDC}")

def print_heading(text):
    """Print a heading with decoration"""
    print("\n" + "="*70)
    print_color(Colors.BOLD + Colors.BLUE, f"  {text}")
    print("="*70)

def print_success(text):
    """Print a success message"""
    print_color(Colors.GREEN, f"✓ {text}")

def print_warning(text):
    """Print a warning message"""
    print_color(Colors.YELLOW, f"⚠ {text}")

def print_error(text):
    """Print an error message"""
    print_color(Colors.RED, f"✗ {text}")

def test_import(module_name):
    """
    Test importing a specific module
    
    Args:
        module_name (str): Name of the module to import
        
    Returns:
        tuple: (bool, float) - Success status and import time in seconds
    """
    start_time = time.time()
    try:
        importlib.import_module(module_name)
        end_time = time.time()
        elapsed = end_time - start_time
        print_success(f"Successfully imported '{module_name}' (took {elapsed:.4f} seconds)")
        return True, elapsed
    except Exception as e:
        end_time = time.time()
        elapsed = end_time - start_time
        print_error(f"Failed to import '{module_name}' (took {elapsed:.4f} seconds)")
        print_error(f"  Error: {str(e)}")
        return False, elapsed

def find_package_modules(base_dir):
    """
    Find all Python modules in the directory structure
    
    Args:
        base_dir (str): Base directory to start search from
        
    Returns:
        list: List of discovered module paths
    """
    modules = []
    
    # First check if base_dir is a relative path within the workspace
    if not os.path.isabs(base_dir):
        # Check if it's in the current directory
        if os.path.exists(base_dir):
            base_path = Path(base_dir)
        # Check if it's in the parent directory
        elif os.path.exists(os.path.join("..", base_dir)):
            base_path = Path(os.path.join("..", base_dir))
        # Check if it's in the parent's parent directory
        elif os.path.exists(os.path.join("..", "..", base_dir)):
            base_path = Path(os.path.join("..", "..", base_dir))
        else:
            print_error(f"Directory not found anywhere in the path: {base_dir}")
            return modules
    else:
        base_path = Path(base_dir)
    
    if not base_path.exists() or not base_path.is_dir():
        print_error(f"Directory not found: {base_dir}")
        print_error(f"Absolute path tried: {base_path.absolute()}")
        return modules
    
    print_success(f"Found base directory: {base_path.absolute()}")
    
    # First check if the directory itself is a package
    if (base_path / "__init__.py").exists():
        try:
            rel_path = base_path.relative_to(os.getcwd())
            module_name = str(rel_path).replace(os.sep, ".")
        except ValueError:
            # If we can't get a relative path, use the directory name
            module_name = base_path.name
            
        if module_name.startswith("."):
            module_name = module_name[1:]
        print(f"Adding base package: {module_name}")
        modules.append(module_name)
      # Find all Python files and packages
    for item in base_path.glob("**/*.py"):
        # Skip test files and private modules
        if "test_" in item.name or (item.name.startswith("_") and item.name != "__init__.py"):
            continue
        
        # Skip files that can't be imported properly
        if "." in str(item).replace(".py", ""):
            continue# Convert path to module name
        try:
            rel_path = item.relative_to(os.getcwd())
        except ValueError:
            # If the path is not relative to the current directory, try using the absolute path
            rel_path = item.parts[-len(base_path.parts)-1:] if len(item.parts) > len(base_path.parts) else item.parts
            rel_path = Path(os.path.join(*rel_path))
        
        module_path = str(rel_path).replace(os.sep, ".")[:-3]  # Remove .py extension
        
        # Fix for relative imports - remove any leading dots
        if module_path.startswith("."):
            module_path = module_path[1:]
        
        # Skip if it's an __init__.py file and we already added the package
        if item.name == "__init__.py" and ".".join(module_path.split(".")[:-1]) in modules:
            continue
            
        modules.append(module_path)
    
    return sorted(modules)

def test_specific_imports():
    """
    Test specific import statements that are critical for functionality
    
    Returns:
        tuple: (bool, list) - Success status and list of import timings
    """
    import_statements = [
        "from tools.utils.config import VAULT_ROOT, RESOURCES_ROOT, MICROSOFT_GRAPH_API_CLIENT_ID",
        "from tools.utils.paths import normalize_wsl_path",
        "from tools.utils.error_handling import categorize_error, ErrorCategories",
        "from tools.auth.microsoft_auth import authenticate_graph_api",
        "from tools.onedrive.file_operations import create_share_link, get_onedrive_items",
        "from tools.transcript.processor import find_transcript_file, get_transcript_content",
        "from tools.notes.markdown_generator import create_markdown_note_for_video",
        "from tools.ai.summarizer import generate_summary_with_openai",
    ]
    
    # Add the new PDF-related imports
    pdf_imports = [
        "from tools.pdf.processor import extract_pdf_text, infer_course_and_program",
        "from tools.pdf.note_generator import create_markdown_note_for_pdf",
        "from tools.pdf.utils import get_pdf_metadata",  # Added utils module
    ]
    
    import_statements.extend(pdf_imports)
    all_successful = True
    import_timings = []
    
    for stmt in import_statements:
        start_time = time.time()
        try:
            exec(stmt)
            end_time = time.time()
            elapsed = end_time - start_time
            print_success(f"Successfully executed: {stmt} (took {elapsed:.4f} seconds)")
            import_timings.append((stmt, True, elapsed))
        except Exception as e:
            end_time = time.time()
            elapsed = end_time - start_time
            print_error(f"Failed: {stmt} (took {elapsed:.4f} seconds)")
            print_error(f"  Error: {str(e)}")
            all_successful = False
            import_timings.append((stmt, False, elapsed))
    
    return all_successful, import_timings

def main():
    """Main test function"""
    overall_start_time = time.time()
    print_heading("TOOLS PACKAGE LOADING TEST")
    
    # First test base tools package
    print("\nTesting base 'tools' package:")
    base_success, base_time = test_import("tools")
    if not base_success:
        print_error("Base 'tools' package failed to import. Cannot continue.")
        return 1
    
    # Test all discovered modules
    print("\nTesting individual modules:")
    modules = find_package_modules("tools")
    
    all_modules_ok = True
    failed_modules = []
    module_timings = []
    total_module_time = 0.0
    
    for module in modules:
        success, elapsed = test_import(module)
        module_timings.append((module, success, elapsed))
        total_module_time += elapsed
        if not success:
            all_modules_ok = False
            failed_modules.append(module)
    
    # Test specific critical imports
    print("\nTesting specific import statements:")
    specific_imports_ok, import_timings = test_specific_imports()
    
    # Calculate overall time
    overall_end_time = time.time()
    overall_elapsed = overall_end_time - overall_start_time
    
    # Print summary
    print_heading("TEST RESULTS")
    
    print(f"Total modules tested: {len(modules)}")
    print(f"Total import time: {total_module_time:.4f} seconds")
    print(f"Average import time: {total_module_time / len(modules):.4f} seconds")
    print(f"Overall test execution time: {overall_elapsed:.4f} seconds")
    
    # Print the slowest modules
    print_heading("TOP 5 SLOWEST MODULES")
    sorted_timings = sorted(module_timings, key=lambda x: x[2], reverse=True)
    for i, (module, success, timing) in enumerate(sorted_timings[:5]):
        status = "✓" if success else "✗"
        print(f"{i+1}. {status} {module}: {timing:.4f} seconds")
    
    # Print success/failure summary
    if all_modules_ok:
        print_success("\nAll modules imported successfully!")
    else:
        print_error(f"\nFailed modules: {len(failed_modules)}/{len(modules)}")
        for module in failed_modules:
            print_error(f"  - {module}")
    
    if specific_imports_ok:
        print_success("All specific import statements succeeded!")
    else:
        print_error("Some specific import statements failed. See details above.")
    
    # Final result
    if all_modules_ok and specific_imports_ok:
        print_color(Colors.GREEN, "\nALL TESTS PASSED! The tools package structure is working correctly.")
        return 0
    else:
        print_color(Colors.RED, "\nSOME TESTS FAILED! See details above.")
        return 1

if __name__ == "__main__":
    sys.exit(main())
