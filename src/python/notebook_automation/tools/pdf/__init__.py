"""PDF Processing Tools for Notebook Automation.

This package provides comprehensive tools for working with PDF files within
the notebook automation system. It handles extraction, processing, and 
integration of PDF content into the knowledge management workflow.

Features:
    - Text extraction with intelligent caching
    - Metadata analysis and extraction
    - PDF content cleaning and formatting
    - Integration with note generation pipeline
    
Example:
    >>> from notebook_automation.tools.pdf import extract_pdf_text
    >>> text = extract_pdf_text('/path/to/document.pdf')
    >>> print(f"Extracted {len(text)} characters from PDF")
"""

from notebook_automation.tools.pdf.processor import extract_pdf_text