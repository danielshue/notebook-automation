"""
Notes Tools

This module provides tools for managing and generating notes:
- Markdown note generation from PDFs
- Note template management
- Note metadata handling
- Note formatting and structuring
"""

from notebook_automation.tools.notes.note_markdown_generator import (
    create_or_update_markdown_note_for_pdf,
    create_or_update_markdown_note_for_video,
    get_note_template
)