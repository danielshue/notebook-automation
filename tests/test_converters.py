#!/usr/bin/env python
"""
Tests for the shared markdown conversion utilities.
"""

import os
import pytest
from pathlib import Path
from notebook_automation.utils.converters import (
    convert_html_to_markdown,
    convert_txt_to_markdown,
    process_file
)

@pytest.fixture
def sample_html():
    return """
    <html>
        <body>
            <h1>Test Title</h1>
            <p>This is a test paragraph.</p>
            <ul>
                <li>Item 1</li>
                <li>Item 2</li>
            </ul>
        </body>
    </html>
    """

@pytest.fixture
def sample_txt():
    return "This is a test text file.\nIt has multiple lines.\n"

def test_html_to_markdown_conversion(sample_html):
    """Test conversion of HTML to markdown."""
    markdown = convert_html_to_markdown(sample_html)
    assert "# Test Title" in markdown
    assert "This is a test paragraph" in markdown
    assert "* Item 1" in markdown
    assert "* Item 2" in markdown

def test_txt_to_markdown_conversion(sample_txt):
    """Test conversion of text to markdown."""
    markdown = convert_txt_to_markdown(sample_txt, "test.txt")
    assert "# Test" in markdown
    assert "This is a test text file." in markdown
    assert "It has multiple lines." in markdown

def test_process_file(tmp_path):
    """Test processing of files with the shared utility."""
    # Create test files
    html_file = tmp_path / "test.html"
    txt_file = tmp_path / "test.txt"
    md_file_html = tmp_path / "test_html.md"
    md_file_txt = tmp_path / "test_txt.md"
    
    # Write test content
    html_file.write_text("<h1>Test</h1><p>Content</p>")
    txt_file.write_text("Test\nContent")
    
    # Test HTML conversion
    success, error = process_file(str(html_file), str(md_file_html))
    assert success
    assert error is None
    assert md_file_html.exists()
    content = md_file_html.read_text()
    assert "# Test" in content
    assert "Content" in content
    
    # Test TXT conversion
    success, error = process_file(str(txt_file), str(md_file_txt))
    assert success
    assert error is None
    assert md_file_txt.exists()
    content = md_file_txt.read_text()
    assert "Test" in content
    assert "Content" in content

def test_process_file_dry_run(tmp_path):
    """Test dry run mode of file processing."""
    # Create test file
    html_file = tmp_path / "test.html"
    md_file = tmp_path / "test.md"
    
    html_file.write_text("<h1>Test</h1>")
    
    # Process in dry run mode
    success, error = process_file(str(html_file), str(md_file), dry_run=True)
    assert success
    assert error is None
    assert not md_file.exists()

def test_process_file_error_handling(tmp_path):
    """Test error handling in file processing."""
    # Test with non-existent file
    success, error = process_file(
        str(tmp_path / "nonexistent.html"),
        str(tmp_path / "output.md")
    )
    assert not success
    assert error is not None
    
    # Test with invalid file type
    invalid_file = tmp_path / "test.invalid"
    invalid_file.write_text("test")
    success, error = process_file(
        str(invalid_file),
        str(tmp_path / "output.md")
    )
    assert not success
    assert "Unsupported file type" in error
