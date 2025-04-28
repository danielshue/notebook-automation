#!/usr/bin/env python3
"""
Test the PDF template loading functions to ensure they properly handle both dictionary and list templates.
"""
import os
import sys
import logging

# Add the parent directory to the path so we can import tools modules
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

# Set up logging
logging.basicConfig(level=logging.INFO, format='%(levelname)s: %(message)s')
logger = logging.getLogger(__name__)

# Import the functions to test
from tools.notes.pdf_note_generator import _get_pdf_reference_template
from tools.metadata.path_metadata import load_metadata_templates

def test_pdf_template_dict():
    """Test that _get_pdf_reference_template can handle dictionary templates."""
    # Test with a dictionary of templates
    templates_dict = {
        "pdf-reference": {
            "template-type": "pdf-reference",
            "auto-generated-state": "writable",
            "test-field": "dict-version"
        },
        "another-template": {
            "template-type": "another-template",
            "test-field": "not-this-one"
        }
    }
    
    template = _get_pdf_reference_template(templates_dict)
    
    print("Test 1: Dictionary of templates")
    if template and template.get("template-type") == "pdf-reference" and template.get("test-field") == "dict-version":
        print("✅ Success: Found correct template from dictionary")
    else:
        print("❌ Error: Did not find correct template from dictionary")
        print(f"Template found: {template}")

def test_pdf_template_list():
    """Test that _get_pdf_reference_template can handle list templates."""
    # Test with a list of templates
    templates_list = [
        {
            "template-type": "another-template",
            "test-field": "not-this-one"
        },
        {
            "template-type": "pdf-reference",
            "auto-generated-state": "writable",
            "test-field": "list-version"
        }
    ]
    
    template = _get_pdf_reference_template(templates_list)
    
    print("Test 2: List of templates")
    if template and template.get("template-type") == "pdf-reference" and template.get("test-field") == "list-version":
        print("✅ Success: Found correct template from list")
    else:
        print("❌ Error: Did not find correct template from list")
        print(f"Template found: {template}")

def test_actual_templates():
    """Test with the actual templates loaded from metadata.yaml."""
    print("Test 3: Actual templates from metadata.yaml")
    
    # Load the actual templates
    templates = load_metadata_templates()
    
    print(f"Loaded templates of type: {type(templates)}")
    if isinstance(templates, dict):
        print(f"Templates contains keys: {', '.join(templates.keys())}")
    elif isinstance(templates, list):
        print(f"Templates list contains {len(templates)} items")
    else:
        print(f"Templates is of unexpected type: {type(templates)}")
    
    # Try to get the PDF template
    template = _get_pdf_reference_template(templates)
    
    if template and template.get("template-type") == "pdf-reference":
        print("✅ Success: Found PDF template from actual templates")
        print(f"Template found: {template}")
    else:
        print("❌ Error: Did not find PDF template from actual templates")
        print(f"Template found: {template}")

if __name__ == "__main__":
    print("\n=== Testing PDF Template Functions ===\n")
    
    test_pdf_template_dict()
    print("\n" + "-"*50 + "\n")
    
    test_pdf_template_list()
    print("\n" + "-"*50 + "\n")
    
    test_actual_templates()
    print("\n" + "-"*50 + "\n")
