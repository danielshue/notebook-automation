#!/usr/bin/env python3
"""
Test the yaml_to_string function specifically for handling file paths with spaces.
"""
import sys
import os

# Add the parent directory to the path so we can import the tools modules
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

from tools.metadata.yaml_metadata_helper import yaml_to_string

# Set up logging to file
import logging
logging.basicConfig(
    filename="/mnt/d/repos/mba-notebook-automation/tests/yaml_test.log",
    level=logging.INFO,
    format="%(message)s"
)

def test_yaml_to_string_with_file_paths():
    """Test that file paths with spaces are properly quoted."""
    test_dict = {
        "template-type": "pdf-reference",
        "auto-generated-state": "writable",
        "title": "Gibson Agency Case",
        "date-created": "2025-04-27",
        "onedrive-pdf-path": "Value Chain Management/Managerial Accounting Business Decisions/Case Studies/Gibson Agency Case.pdf",
        "onedrive-sharing-link": "https://1drv.ms/b/c/8e6caaf990d03558/Ea9d40-HAchLoQm3za1Yo8UBY2uC81OY6uv8LjT75UZ_hA",
        "pdf-size": "0.39 MB",
        "pdf-uploaded": "2025-04-27",
        "program": "Value Chain Management",
        "course": "Managerial Accounting Business Decisions",
        "class": "Managerial Accounting Business Decisions",
        "status": "unread",
        "tags": []
    }
    
    yaml_str = yaml_to_string(test_dict)
    logging.info("Generated YAML:\n%s", yaml_str)
    
    # Check that the onedrive-pdf-path is properly quoted and on a single line
    path_line_found = False
    for line in yaml_str.splitlines():
        if "onedrive-pdf-path" in line:
            path_line = line
            logging.info("\nPath line: %s", path_line)
            if '"Value Chain Management/Managerial Accounting Business Decisions/Case Studies/Gibson Agency Case.pdf"' in path_line:
                logging.info("✅ Path value is correctly quoted")
            else:
                logging.info("❌ Path value is NOT correctly quoted: %s", path_line)
            
            if "\n" not in path_line:
                logging.info("✅ Path line does not contain newlines")
            else:
                logging.info("❌ Path line contains newlines")
                
            path_line_found = True
            break
            
    if not path_line_found:
        logging.info("❌ onedrive-pdf-path not found in YAML output")
        
    # Check that no values with spaces are split across multiple lines
    all_properly_quoted = True
    for line in yaml_str.splitlines():
        if ": " in line and not line.strip().endswith(":") and not line.strip().startswith("- "):
            key, value = line.split(": ", 1)
            if " " in value and not (value.startswith('"') and value.endswith('"')) and not (value.startswith("'") and value.endswith("'")):
                logging.info(f"❌ Line with spaces not properly quoted: {line}")
                all_properly_quoted = False
    
    if all_properly_quoted:
        logging.info("✅ All values with spaces are properly quoted")
    
    return yaml_str

if __name__ == "__main__":
    yaml_string = test_yaml_to_string_with_file_paths()
    logging.info("\nFull YAML Output:\n%s", yaml_string)
    
    # Write the YAML string directly to a file
    with open("/mnt/d/repos/mba-notebook-automation/yaml_test_result.txt", "w") as f:
        f.write(yaml_string)
