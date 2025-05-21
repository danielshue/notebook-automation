"""
Test suite for the update_glossary.py CLI tool.

This module contains tests to verify the functionality of the glossary updating tool,
ensuring that it correctly identifies and transforms definition entries into markdown callouts.
"""

import os
import tempfile
import unittest
from pathlib import Path

from notebook_automation.cli.update_glossary import process_glossary_file


class TestGlossaryUpdater(unittest.TestCase):
    """Tests for the glossary updater functionality."""

    def setUp(self):
        """Set up temporary test files for each test."""
        self.temp_dir = tempfile.TemporaryDirectory()
        self.temp_path = Path(self.temp_dir.name)

    def tearDown(self):
        """Clean up temporary files after each test."""
        self.temp_dir.cleanup()

    def test_basic_glossary_formatting(self):
        """Test basic glossary entry formatting with definitions."""
        # Create a test file with sample content
        test_content = """# Glossary

## A

**Apple** - A red fruit that grows on trees.
**Avocado** - A green fruit with a large seed.

## B

**Banana** - A yellow curved fruit.
"""
        
        expected_content = """# Glossary

## A

> [!definition]
> **Apple** - A red fruit that grows on trees.
> [!definition]
> **Avocado** - A green fruit with a large seed.

## B

> [!definition]
> **Banana** - A yellow curved fruit.
"""
        
        test_file = self.temp_path / "test_glossary.md"
        with open(test_file, 'w', encoding='utf-8') as f:
            f.write(test_content)
        
        # Process the file
        processed, modified = process_glossary_file(test_file)
        
        # Verify results
        self.assertEqual(processed, 3, "Should find 3 definitions")
        self.assertEqual(modified, 3, "Should modify 3 definitions")
        
        # Check file content
        with open(test_file, 'r', encoding='utf-8') as f:
            result_content = f.read()
        
        self.assertEqual(result_content, expected_content)

    def test_already_formatted_entries(self):
        """Test handling of already formatted entries."""
        # Create a test file with some already formatted entries
        test_content = """# Glossary

## A

> [!definition]
> **Apple** - A red fruit that grows on trees.
**Avocado** - A green fruit with a large seed.

## B

**Banana** - A yellow curved fruit.
"""
        
        expected_content = """# Glossary

## A

> [!definition]
> **Apple** - A red fruit that grows on trees.
> [!definition]
> **Avocado** - A green fruit with a large seed.

## B

> [!definition]
> **Banana** - A yellow curved fruit.
"""
        
        test_file = self.temp_path / "test_mixed_glossary.md"
        with open(test_file, 'w', encoding='utf-8') as f:
            f.write(test_content)
        
        # Process the file
        processed, modified = process_glossary_file(test_file)
        
        # Verify results
        self.assertEqual(processed, 2, "Should find 2 unformatted definitions")
        self.assertEqual(modified, 2, "Should modify 2 definitions")
        
        # Check file content
        with open(test_file, 'r', encoding='utf-8') as f:
            result_content = f.read()
        
        self.assertEqual(result_content, expected_content)

    def test_empty_file(self):
        """Test handling of a file with no definitions."""
        # Create an empty test file
        test_content = """# Glossary

This is just some text without any definitions.

## A

There are no definitions here.

## B

Still no definitions.
"""
        
        test_file = self.temp_path / "test_empty_glossary.md"
        with open(test_file, 'w', encoding='utf-8') as f:
            f.write(test_content)
        
        # Process the file
        processed, modified = process_glossary_file(test_file)
        
        # Verify results
        self.assertEqual(processed, 0, "Should find 0 definitions")
        self.assertEqual(modified, 0, "Should modify 0 definitions")
        
        # Check file content remains unchanged
        with open(test_file, 'r', encoding='utf-8') as f:
            result_content = f.read()
        
        self.assertEqual(result_content, test_content)

    def test_dry_run_mode(self):
        """Test that dry run mode doesn't modify files."""
        # Create a test file
        test_content = """# Glossary

## A

**Apple** - A red fruit that grows on trees.
"""
        
        test_file = self.temp_path / "test_dry_run.md"
        with open(test_file, 'w', encoding='utf-8') as f:
            f.write(test_content)
        
        # Process in dry run mode
        processed, modified = process_glossary_file(test_file, dry_run=True)
        
        # Verify results
        self.assertEqual(processed, 1, "Should find 1 definition")
        self.assertEqual(modified, 1, "Should report 1 modification")
        
        # Check file content should remain unchanged
        with open(test_file, 'r', encoding='utf-8') as f:
            result_content = f.read()
        
        self.assertEqual(result_content, test_content, "File should not be modified in dry run mode")


if __name__ == "__main__":
    unittest.main()
