#!/usr/bin/env python3
"""
Test for PDF processor modules using simple print statements.
"""
import os
import sys

def test_pdf_module_imports():
    """Test importing PDF modules."""
    print("=== Testing PDF Module Imports ===")
    
    try:
        import tools.pdf
        print("✅ Successfully imported tools.pdf package")
    except Exception as e:
        print(f"❌ Error importing tools.pdf package: {e}")
        return False
    
    try:
        from tools.pdf.processor import extract_pdf_text, infer_course_and_program
        print("✅ Successfully imported PDF processor functions")
    except Exception as e:
        print(f"❌ Error importing PDF processor functions: {e}")
        return False
    
    try:
        from tools.pdf.note_generator import create_markdown_note_for_pdf
        print("✅ Successfully imported PDF note generator functions")
    except Exception as e:
        print(f"❌ Error importing PDF note generator functions: {e}")
        return False
        
    try:
        from tools.ai.summarizer import generate_summary_with_openai
        print("✅ Successfully imported PDF AI summarizer functions")
    except Exception as e:
        print(f"❌ Error importing PDF AI summarizer functions: {e}")
        return False
    
    return True

def test_config():
    """Test configuration access."""
    print("\n=== Testing Configuration Access ===")
    
    try:
        from tools.utils.config import RESOURCES_ROOT, VAULT_ROOT, ONEDRIVE_BASE
        print(f"✅ Config values:")
        print(f"  - RESOURCES_ROOT: {RESOURCES_ROOT}")
        print(f"  - VAULT_ROOT: {VAULT_ROOT}")
        print(f"  - ONEDRIVE_BASE: {ONEDRIVE_BASE}")
    except Exception as e:
        print(f"❌ Error accessing config values: {e}")
        return False
        
    return True
    
def test_refactored_script():
    """Test that the refactored script loads without errors."""
    print("\n=== Testing Refactored Script Import ===")
    
    script_path = "generate_pdf_notes_from_onedrive_refactored.py"
    if not os.path.exists(script_path):
        print(f"❌ Refactored script not found at: {script_path}")
        return False
        
    try:
        with open(script_path, 'r') as f:
            script_contents = f.read()
            
        # Use compile to check for syntax errors
        compile(script_contents, script_path, 'exec')
        print(f"✅ Refactored script compiles without syntax errors")
    except Exception as e:
        print(f"❌ Error compiling refactored script: {e}")
        return False
        
    return True

if __name__ == "__main__":
    print("🔍 Testing PDF Modules and Refactored Code")
    print("-----------------------------------------")
    
    all_passed = True
    all_passed = test_pdf_module_imports() and all_passed
    all_passed = test_config() and all_passed
    all_passed = test_refactored_script() and all_passed
    
    if all_passed:
        print("\n✅ All tests passed! The refactoring appears to be working correctly.")
    else:
        print("\n❌ Some tests failed. Please check the output above for details.")
