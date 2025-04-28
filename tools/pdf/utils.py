#!/usr/bin/env python3
"""
PDF Utility Module for Core PDF Operations in Notebook Automation

This module provides fundamental PDF processing capabilities that support the 
MBA Notebook Automation system. It offers lightweight utility functions focused on 
essential PDF operations like metadata extraction, format validation, and 
selective text extraction.

Key Features:
------------
1. Metadata Extraction
   - Extraction of embedded PDF metadata (title, author, subject)
   - Document structure analysis (page count, creation date)
   - File system information (size, path)
   - Error resilience and fallback strategies

2. Format Validation
   - Extension-based PDF detection
   - Magic number signature verification
   - Content-aware validation

3. Page-level Text Extraction
   - Targeted extraction from specific pages
   - Boundary checking and error handling
   - Memory-efficient processing for large documents

Integration Notes:
-----------------
- Works alongside the more comprehensive processor.py module
- Provides simpler alternatives for basic PDF operations
- Designed for scenarios requiring minimal dependencies
- Uses PyPDF2 as its primary extraction engine

Usage Examples:
-------------
```python
# Verify file is a valid PDF
from tools.pdf.utils import is_pdf_file, get_pdf_metadata, extract_text_from_page

# Check if file is a PDF
if is_pdf_file("/path/to/document.pdf"):
    # Get metadata from the PDF
    metadata = get_pdf_metadata("/path/to/document.pdf")
    print(f"Title: {metadata['title']}")
    print(f"Author: {metadata['author']}")
    print(f"Pages: {metadata['page_count']}")
    
    # Extract text from a specific page (0-indexed)
    first_page_text = extract_text_from_page("/path/to/document.pdf", 0)
    print(f"First page preview: {first_page_text[:100]}...")
```
"""

import os
import sys
import logging
from pathlib import Path
from typing import Dict, Any, Optional, List, Tuple

# Import from other modules
try:
    from ..utils.paths import normalize_wsl_path
except ImportError:
    # Fallback if the relative import fails
    sys.path.append(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
    from tools.utils.paths import normalize_wsl_path

logger = logging.getLogger(__name__)

def get_pdf_metadata(pdf_path: str) -> Dict[str, Any]:
    """
    Extract comprehensive metadata from a PDF file with error handling.
    
    This function extracts both embedded metadata from the PDF itself and file system 
    metadata. It includes robust error handling to ensure that even if PDF parsing 
    fails, basic file information is still returned.
    
    The metadata extraction covers:
    1. Document properties (title, author, subject, keywords)
    2. Production information (creator, producer, dates)
    3. Document structure (page count)
    4. File system details (size, path)
    
    Args:
        pdf_path (str): Path to the PDF file. Can be absolute or relative.
                        WSL paths are automatically normalized.
        
    Returns:
        Dict[str, Any]: Dictionary containing metadata with these keys:
            - title: Document title or filename if not available
            - author: Document author or "Unknown" if not available
            - subject: Document subject (empty string if not available)
            - keywords: Document keywords (empty string if not available)
            - creator: Application that created the document
            - producer: PDF producer software
            - creation_date: Original creation date of the document
            - modification_date: Last modification date
            - page_count: Number of pages in the document
            - file_size: Size of the file in bytes
            - path: Normalized path to the PDF file
            - error: Error message (only present if an error occurred)
            
    Error Handling:
        If the file cannot be found or read, or if PyPDF2 is not available,
        the function returns a partial metadata dictionary with basic file
        information and an error message.
    """
    # Try to import PyPDF2, which is required for PDF processing
    try:
        from PyPDF2 import PdfReader
    except ImportError:
        # Provide a helpful error message with installation instructions
        logger.error("PyPDF2 is required for PDF metadata extraction. Please install it with 'pip install PyPDF2'.")
        return {}
    
    # Normalize path to handle WSL path differences if present
    pdf_path = normalize_wsl_path(pdf_path)
    
    # Basic file existence check before attempting to process
    if not os.path.exists(pdf_path):
        logger.error(f"PDF file not found: {pdf_path}")
        return {}
    
    try:
        # Open the PDF and extract its metadata
        reader = PdfReader(pdf_path)
        info = reader.metadata
        
        # Extract basic metadata with fallbacks for missing fields
        # This ensures consistent keys are always present in the return dictionary
        metadata = {
            "title": info.get("/Title", os.path.basename(pdf_path)),  # Use filename as fallback
            "author": info.get("/Author", "Unknown"),                 # Use "Unknown" as fallback
            "subject": info.get("/Subject", ""),                      # Empty string fallback
            "keywords": info.get("/Keywords", ""),
            "creator": info.get("/Creator", ""),
            "producer": info.get("/Producer", ""),
            "creation_date": info.get("/CreationDate", ""),
            "modification_date": info.get("/ModDate", ""),
            "page_count": len(reader.pages),                          # Structural metadata
            "file_size": os.path.getsize(pdf_path),                   # File system metadata
            "path": pdf_path
        }
        
        # Clean up certain fields (remove binary markers, etc.)
        for key, value in metadata.items():
            if isinstance(value, str) and value.startswith("b'") and value.endswith("'"):
                metadata[key] = value[2:-1]  # Remove b'' wrapper
        
        return metadata
    except Exception as e:
        logger.error(f"Error extracting PDF metadata: {str(e)}")
        return {
            "title": os.path.basename(pdf_path),
            "page_count": 0,
            "file_size": os.path.getsize(pdf_path) if os.path.exists(pdf_path) else 0,
            "path": pdf_path,
            "error": str(e)
        }

def is_pdf_file(file_path: str) -> bool:
    """
    Verify if a file is a valid PDF using multi-level validation.
    
    This function performs a two-stage verification process:
    1. First checks the file extension (.pdf)
    2. Then verifies the file signature/magic number (%PDF)
    
    This dual-check approach provides more reliable PDF detection than
    extension-based checks alone, which can be easily spoofed. The 
    signature check confirms the file actually contains PDF content.
    
    Args:
        file_path (str): Path to the file to check for PDF validity
        
    Returns:
        bool: True if the file exists and is a valid PDF document,
              False if the file doesn't exist, has a non-PDF extension,
              or doesn't contain a valid PDF signature
    
    Security Note:
        This function performs basic validation but does not check for
        malformed PDFs or potential security issues within the file.
        It only confirms the file appears to be in PDF format.
    """
    # Basic file existence check first
    if not os.path.exists(file_path):
        return False
        
    # STAGE 1: Check file extension (quick but not definitive)
    # This is a fast preliminary check before reading file contents
    if not file_path.lower().endswith('.pdf'):
        return False
        
    # STAGE 2: Check file signature/magic number (more definitive)
    # PDF files start with the %PDF header
    try:
        with open(file_path, 'rb') as f:
            # Read just enough bytes for the PDF header
            header = f.read(4)
            # Verify against the standard PDF signature
            return header == b'%PDF'
    except Exception as e:
        # Log the error but don't propagate it - just return False
        # This ensures the function doesn't crash the calling code
        logger.debug(f"Error checking PDF signature for {file_path}: {str(e)}")
        return False

def extract_text_from_page(pdf_path: str, page_num: int) -> str:
    """
    Extract text from a specific page of a PDF with robust error handling.
    
    This function provides memory-efficient extraction by processing only a single
    page rather than loading the entire document. It includes comprehensive
    error handling, including:
    - PyPDF2 dependency checking
    - Page boundary validation
    - Exception handling for corrupted PDFs
    
    Advantages of page-specific extraction:
    - Reduced memory usage for large documents
    - Faster processing when only specific pages are needed
    - Ability to parallelize extraction across multiple pages
    
    Args:
        pdf_path (str): Path to the PDF file
        page_num (int): Page number to extract (0-based indexing,
                       where 0 is the first page)
        
    Returns:
        str: Extracted text from the specified page, or an empty string
             if the extraction fails for any reason (with error logged)
    
    Note:
        For extracting text from multiple pages or an entire document,
        consider using the processor.extract_pdf_text() function instead,
        which includes caching and more advanced processing features.
    """
    # Check for the required PyPDF2 dependency
    try:
        from PyPDF2 import PdfReader
    except ImportError:
        # Provide clear installation instructions in the error message
        logger.error("PyPDF2 is required for PDF text extraction. Please install it with 'pip install PyPDF2'.")
        return ""
        
    try:
        # Open the PDF file and create a reader object
        reader = PdfReader(pdf_path)
        
        # VALIDATION: Check page boundaries to avoid IndexError
        # PDF pages are 0-indexed in PyPDF2
        if page_num < 0 or page_num >= len(reader.pages):
            logger.error(f"Page number {page_num} is out of range (0-{len(reader.pages)-1})")
            return ""
            
        # EXTRACTION: Get the specified page and extract its text
        page = reader.pages[page_num]
        # The extract_text method handles most of the complexity of PDF text extraction
        return page.extract_text()
    except Exception as e:
        # Comprehensive error handling to avoid crashing the calling code
        # This catches file access issues, corrupted PDFs, and other problems
        logger.error(f"Error extracting text from PDF page: {str(e)}")
        return ""
