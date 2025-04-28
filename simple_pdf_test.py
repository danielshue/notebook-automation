#!/usr/bin/env python3
"""
Simple Test for PDF Module Loading

This script checks if all required modules for the PDF processing can be loaded correctly.
It will output a simple success or error message to the console.
"""

import sys

def run_test():
    """Run the test and return True if successful, False otherwise"""
    try:
        # Try to import all required modules
        from tools.utils.config import VAULT_ROOT, RESOURCES_ROOT
        from tools.auth.microsoft_auth import authenticate_graph_api
        from tools.onedrive.file_operations import create_share_link
        from tools.pdf.processor import extract_pdf_text, infer_course_and_program
        from tools.pdf.note_generator import create_markdown_note_for_pdf
        from tools.ai.summarizer import generate_summary_with_openai
        
        # If we get here, all imports succeeded
        return True
    except ImportError as e:
        print(f"Import Error: {e}")
        return False
    except Exception as e:
        print(f"Unexpected Error: {e}")
        return False

if __name__ == "__main__":
    result = run_test()
    if result:
        print("SUCCESS: All PDF modules loaded correctly!")
        sys.exit(0)
    else:
        print("FAILURE: Some modules could not be loaded. See errors above.")
        sys.exit(1)
