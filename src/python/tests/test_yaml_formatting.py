#!/usr/bin/env python3
"""
Test for YAML formatting in the PDF note generator.
"""

import os
import sys
import datetime
from pathlib import Path

# Add parent directory to the path to import from tools
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from tools.pdf.note_generator import yaml_to_string

def test_yaml_formatting():
    """Test the yaml_to_string function with various data types."""
    
    # Create a test YAML dictionary with various data types
    yaml_dict = {
        "template-type": "pdf-reference",
        "auto-generated-state": "writable",
        "title": "Sample PDF Document",
        "pdf-path": "resources/course/document.pdf",
        "date-created": "2025-04-26",  # Date in YYYY-MM-DD format
        "date-modified": datetime.datetime.now().strftime("%Y-%m-%d"),
        "tags": ["finance", "economics", "MBA"],
        "program": "MBA",
        "course": "Finance 101",
        "status": "unread",
        "pdf-size": "2.5 MB",
        "completion-date": "",
        "review-date": None,
        "page-count": 42,
        "special-chars": "Test: with, special; characters!",
        "quoted-already": "'quoted value'",
    }
    
    # Convert to YAML string
    yaml_str = yaml_to_string(yaml_dict)
    
    # Print the result
    print("\nFormatted YAML string:")
    print("---------------------")
    print(yaml_str)
    print("---------------------")
    
    # Check if dates are not quoted
    assert '"2025-04-26"' not in yaml_str, "Date should not be quoted"
    
    # Check if regular strings are quoted
    assert 'title: "Sample PDF Document"' in yaml_str, "Title should be quoted"
    assert 'program: "MBA"' in yaml_str, "Program should be quoted"
    
    # Check if special characters are properly handled
    assert 'special-chars: "Test: with, special; characters!"' in yaml_str, "Special characters should be handled properly"
    
    # Check if tags are quoted
    assert '  - "finance"' in yaml_str, "Tags should be quoted"
    
    # Check if numbers are not quoted
    assert 'page-count: 42' in yaml_str, "Numbers should not be quoted"
    
    # Check if file sizes are not quoted
    assert 'pdf-size: 2.5 MB' in yaml_str, "File sizes should not be quoted"
    
    # Check if empty values are handled correctly
    assert 'completion-date: ' in yaml_str, "Empty values should be handled correctly"
    
    print("\n✅ All tests passed!")

if __name__ == "__main__":
    test_yaml_formatting()
