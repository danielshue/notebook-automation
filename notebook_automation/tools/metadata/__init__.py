"""
Metadata Tools

This module provides tools for managing metadata in notes and files:
- Path metadata extraction and inference
- Course and program information handling
- Metadata validation and consistency checking
- YAML frontmatter management
"""

from notebook_automation.tools.metadata.path_metadata import (
    infer_course_and_program,
    extract_metadata_from_path,
    validate_metadata
)