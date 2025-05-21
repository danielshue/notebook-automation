#!/usr/bin/env python3
"""
Fix Unicode box characters in generate_pdf_notes.py

This script replaces all Unicode box characters with ASCII alternatives in log messages.
"""

import sys
import re
from pathlib import Path

# Path to generate_pdf_notes.py
pdf_notes_path = Path("d:/repos/mba-notebook-automation/notebook_automation/cli/generate_pdf_notes.py")

if not pdf_notes_path.exists():
    print(f"Error: Could not find {pdf_notes_path}")
    sys.exit(1)

# Read the file content
with open(pdf_notes_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Define replacements for problematic Unicode box characters
replacements = [
    # Box drawing characters used for tree structure
    (r'logger\.info\([\'"](\s*)(├─|└─|│)(.*?)[\'"]\)', r'logger.info(\'\1|- \3\')'),
    (r'logger\.debug\([\'"](\s*)(├─|└─|│)(.*?)[\'"]\)', r'logger.debug(\'\1|- \3\')'),
    (r'logger\.error\([\'"](\s*)(├─|└─|│)(.*?)[\'"]\)', r'logger.error(\'\1|- \3\')'),
    (r'logger\.warning\([\'"](\s*)(├─|└─|│)(.*?)[\'"]\)', r'logger.warning(\'\1|- \3\')'),
    
    # f-strings with box characters
    (r'logger\.info\(f[\'"](\s*)(├─|└─|│)(.*?)[\'"]\)', r'logger.info(f\'\1|- \3\')'),
    (r'logger\.debug\(f[\'"](\s*)(├─|└─|│)(.*?)[\'"]\)', r'logger.debug(f\'\1|- \3\')'),
    (r'logger\.error\(f[\'"](\s*)(├─|└─|│)(.*?)[\'"]\)', r'logger.error(f\'\1|- \3\')'),
    (r'logger\.warning\(f[\'"](\s*)(├─|└─|│)(.*?)[\'"]\)', r'logger.warning(f\'\1|- \3\')'),
    
    # Replace checkmark
    (r'✓', r'*')
]

# Apply all replacements
new_content = content
for pattern, replacement in replacements:
    new_content = re.sub(pattern, replacement, new_content)

# Check if changes were made
if new_content == content:
    print("No changes needed - no Unicode box characters found in log messages.")
    sys.exit(0)

# Write the updated file
with open(pdf_notes_path, 'w', encoding='utf-8') as f:
    f.write(new_content)

print(f"Successfully updated {pdf_notes_path}")
print("Replaced Unicode box characters with ASCII alternatives in log messages.")
