#!/usr/bin/env python3
"""
Test for PDF processor modules.
"""
import os
import sys
import logging

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(message)s')
logger = logging.getLogger(__name__)

def test_pdf_module_imports():
    """Test importing PDF modules."""
    logger.info("=== Testing PDF Module Imports ===")
    
    try:
        import tools.pdf
        logger.info("✅ Successfully imported tools.pdf package")
    except Exception as e:
        logger.info(f"❌ Error importing tools.pdf package: {e}")
        return False
    
    try:
        from tools.pdf.processor import extract_pdf_text, infer_course_and_program
        logger.info("✅ Successfully imported PDF processor functions")
    except Exception as e:
        logger.info(f"❌ Error importing PDF processor functions: {e}")
        return False
    
    try:
        from tools.pdf.note_generator import create_markdown_note_for_pdf
        logger.info("✅ Successfully imported PDF note generator functions")
    except Exception as e:
        logger.info(f"❌ Error importing PDF note generator functions: {e}")
        return False
        
    try:
        from tools.ai.summarizer import generate_summary_with_openai
        logger.info("✅ Successfully imported PDF AI summarizer functions")
    except Exception as e:
        logger.info(f"❌ Error importing PDF AI summarizer functions: {e}")
        return False
    
    return True

def test_config():
    """Test configuration access."""
    logger.info("\n=== Testing Configuration Access ===")
    
    try:
        from tools.utils.config import ONEDRIVE_LOCAL_RESOURCES_ROOT, VAULT_LOCAL_ROOT, ONEDRIVE_BASE
        logger.info(f"✅ Config values:")
        logger.info(f"  - RESOURCES_ROOT: {ONEDRIVE_LOCAL_RESOURCES_ROOT}")
        logger.info(f"  - VAULT_ROOT: {VAULT_LOCAL_ROOT}")
        logger.info(f"  - ONEDRIVE_BASE: {ONEDRIVE_BASE}")
    except Exception as e:
        logger.info(f"❌ Error accessing config values: {e}")
        return False
        
    return True
    
def test_refactored_script():
    """Test that the refactored script loads without errors."""
    logger.info("\n=== Testing Refactored Script Import ===")
    
    script_path = "generate_pdf_notes_from_onedrive_refactored.py"
    if not os.path.exists(script_path):
        logger.info(f"❌ Refactored script not found at: {script_path}")
        return False
        
    try:
        with open(script_path, 'r') as f:
            script_contents = f.read()
            
        # Use compile to check for syntax errors
        compile(script_contents, script_path, 'exec')
        logger.info(f"✅ Refactored script compiles without syntax errors")
    except Exception as e:
        logger.info(f"❌ Error compiling refactored script: {e}")
        return False
        
    return True

if __name__ == "__main__":
    logger.info("🔍 Testing PDF Modules and Refactored Code")
    logger.info("-----------------------------------------")
    
    all_passed = True
    all_passed = test_pdf_module_imports() and all_passed
    all_passed = test_config() and all_passed
    all_passed = test_refactored_script() and all_passed
    
    if all_passed:
        logger.info("\n✅ All tests passed! The refactoring appears to be working correctly.")
    else:
        logger.info("\n❌ Some tests failed. Please check the output above for details.")
