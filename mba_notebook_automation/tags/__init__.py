"""
Tags Package

Contains scripts for managing and manipulating tags in Obsidian notes.

This package provides tools for:
- Adding nested tags automatically (add_nested_tags)
- Consolidating tags across multiple notes (consolidate_tags)
- Restructuring tags to follow proper hierarchy (restructure_tags)
- General tag management functionality (tag_manager)
"""

# Import key modules for easier access
from mba_notebook_automation.tags.tag_manager import TagManager
from mba_notebook_automation.tags.add_nested_tags import add_nested_tags, main as add_nested_tags_main
from mba_notebook_automation.tags.consolidate_tags import consolidate_tags
from mba_notebook_automation.tags.restructure_tags import restructure_tags
