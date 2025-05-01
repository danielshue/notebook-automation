#!/usr/bin/env python3
"""
Test script to verify that the PDF note generator preserves existing notes
when updating a markdown file.
"""
import sys
import os
import tempfile
import re
import unittest
from unittest import mock
from pathlib import Path

# Add the parent directory to the path so we can import the tools modules
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

# We'll mock these functions to avoid API calls and external dependencies
from tools.notes.note_markdown_generator import create_or_update_markdown_note_for_pdf

def test_notes_preservation():
    """Test that existing notes are preserved when updating a markdown file."""
    
    # Create temporary directory for test files
    with tempfile.TemporaryDirectory() as temp_dir:
        # Create a dummy PDF file
        pdf_path = Path(temp_dir) / "test.pdf"
        with open(pdf_path, 'wb') as f:
            # Write a minimal valid PDF file (just some header)
            f.write(b'%PDF-1.0\nTest PDF content')
        
        vault_dir = Path(temp_dir)
        pdf_stem = "test"
        
        # First, create an initial note
        create_or_update_markdown_note_for_pdf(
            pdf_path=pdf_path,
            vault_dir=vault_dir,
            pdf_stem=pdf_stem,
            dry_run=False
        )
        
        note_path = vault_dir / f"{pdf_stem}-Notes.md"
        
        # Now add some custom notes to the file
        with open(note_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Find notes section and replace with custom notes
        custom_notes = "## 📝 Notes\n\n\nThese are my custom notes about the PDF.\n\n1. First important point\n2. Second important insight\n3. Questions for later follow-up"
        content = re.sub(r'## 📝 Notes.*', custom_notes, content, flags=re.DOTALL)
        
        with open(note_path, 'w', encoding='utf-8') as f:
            f.write(content)
        
        print(f"Created note with custom notes: {note_path}")
        
        # Now update the note and verify that custom notes are preserved
        create_or_update_markdown_note_for_pdf(
            pdf_path=pdf_path,
            vault_dir=vault_dir,
            pdf_stem=pdf_stem,
            dry_run=False
        )
        
        # Read the updated note
        with open(note_path, 'r', encoding='utf-8') as f:
            updated_content = f.read()
        
        # Check if our custom notes are preserved
        if custom_notes in updated_content:
            print("✅ Success: Custom notes were preserved!")
        else:
            print("❌ Error: Custom notes were not preserved!")
            print("\nOriginal custom notes:")
            print(custom_notes)
            print("\nUpdated content notes section:")
            notes_match = re.search(r'(## 📝 Notes[\s\S]*?)$', updated_content)
            if notes_match:
                print(notes_match.group(1))
            else:
                print("No notes section found in updated content!")

if __name__ == "__main__":
    test_notes_preservation()
