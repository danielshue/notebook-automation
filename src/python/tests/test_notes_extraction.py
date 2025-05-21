#!/usr/bin/env python3
"""
Simplified test for note preservation in PDF note generator.
This script directly tests the logic for preserving notes content when updating markdown files.
"""

import os
import re
from pathlib import Path

def extract_preserve_notes_test():
    """Test the logic for extracting and preserving notes from an existing markdown file."""
    
    # Create test directory
    test_dir = Path("./tests/notes_test")
    test_dir.mkdir(parents=True, exist_ok=True)
    
    # Create a test markdown file with notes section
    test_file = test_dir / "test-Notes.md"
    
    # Initial content with notes
    initial_content = """---
template-type: "pdf-reference"
auto-generated-state: "writable"
title: "Test PDF"
date-created: 2025-04-27
onedrive-pdf-path: "Path/To/Test.pdf"
---

# Test PDF

This is some AI-generated summary content.

## Key Concepts
- Point 1
- Point 2

## 📝 Notes

These are my custom notes about the PDF.

1. First important point
2. Second important insight
3. Questions for later follow-up
"""

    # Write the initial file
    with open(test_file, 'w', encoding='utf-8') as f:
        f.write(initial_content)
    
    print(f"Created initial test file: {test_file}")
    
    # Now let's simulate generating new content that should preserve the notes
    new_content = """---
template-type: "pdf-reference"
auto-generated-state: "writable"
title: "Test PDF"
date-created: 2025-04-27
onedrive-pdf-path: "Path/To/Test.pdf"
---

# Test PDF

This is updated AI-generated summary content with new insights.

## Key Concepts
- Updated point 1
- Updated point 2
- New point 3

"""  # Note: No notes section in the new content
    
    # Read the existing file to find the notes section
    with open(test_file, 'r', encoding='utf-8') as f:
        existing_content = f.read()
    
    # Extract the notes section using regex
    notes_match = re.search(r'(## 📝 Notes[\s\S]*?)$', existing_content)
    existing_notes = ""
    if notes_match:
        existing_notes = notes_match.group(1)
        print("Found existing notes:")
        print(existing_notes)
    
    # Combine new content with existing notes
    final_content = new_content
    if existing_notes:
        final_content += f"\n\n{existing_notes}"
    else:
        notes_header = "## 📝 Notes\n\n\nAdd your notes about the PDF here."
        final_content += f"\n\n{notes_header}"
    
    # Write the updated file
    with open(test_file, 'w', encoding='utf-8') as f:
        f.write(final_content)
    
    print(f"\nUpdated file with preserved notes: {test_file}")
    
    # Verify the notes were preserved
    with open(test_file, 'r', encoding='utf-8') as f:
        updated_content = f.read()
    
    if "These are my custom notes about the PDF." in updated_content:
        print("\n✅ Success: Custom notes were preserved!")
    else:
        print("\n❌ Error: Custom notes were not preserved!")
    
    # Print the updated content for verification
    print("\nFinal content:")
    print(updated_content)

if __name__ == "__main__":
    extract_preserve_notes_test()
