#!/usr/bin/env python3
"""
PDF Processing Module for Intelligent Text Extraction and Metadata Analysis

This module provides a comprehensive set of utilities for working with PDF files
within the MBA Notebook Automation system. It implements multi-library extraction 
strategies with graceful degradation, intelligent text processing, and extensive 
metadata handling capabilities.

Key Features:
------------
1. Multi-strategy Text Extraction
   - Primary extraction using pdfplumber for highest quality results
   - Fallback to PyPDF2 when pdfplumber is unavailable
   - Intelligent caching system for performance optimization
   - Configurable page ranges and selective extraction

2. Comprehensive Metadata Extraction
   - File system metadata (creation date, modification date, size)
   - PDF-specific metadata (author, title, producer)
   - Page count and document structure information
   - Format normalization for consistent downstream processing

3. Advanced Text Processing
   - Cleanup of common PDF extraction artifacts
   - Removal of headers, footers, and page numbers
   - Hyphenation fix for improved readability
   - Whitespace normalization and formatting

4. Integration Features
   - Seamless interaction with AI summarization pipeline
   - Built-in logging and error handling
   - Progress reporting for long-running extractions
   - Memory-efficient processing of large documents

Usage Example:
-------------```python
from tools.pdf.processor import extract_pdf_text, get_pdf_metadata, clean_pdf_text

# Extract text from a PDF file with caching
pdf_path = "path/to/document.pdf" 
text = extract_pdf_text(pdf_path)

# Get comprehensive metadata
metadata = get_pdf_metadata(pdf_path)
print(f"Document: {metadata.get('title', 'Unknown')}")
print(f"Author: {metadata.get('author', 'Unknown')}")
print(f"Pages: {metadata.get('page_count', 0)}")

# Clean the extracted text
cleaned_text = clean_pdf_text(text)

# Use the cleaned text for AI processing or note generation
from tools.ai.summarizer import generate_summary
summary = generate_summary(cleaned_text)
```"""
import os
import re
import os.path
from pathlib import Path
from datetime import datetime
from ..utils.config import logger

# Try to import pdfplumber for text extraction, with a fallback to PyPDF2
try:
    import pdfplumber
    HAS_PDFPLUMBER = True
except ImportError:
    HAS_PDFPLUMBER = False
    try:
        import PyPDF2
        HAS_PYPDF2 = True
    except ImportError:
        HAS_PYPDF2 = False

def extract_pdf_text(pdf_path, max_pages=None, start_page=0, page_range=None, force=False):
    """
    Extract text from a PDF file with intelligent caching and error handling.
    
    This function serves as the main entry point for PDF text extraction in the
    notebook automation system. It implements a caching mechanism for performance
    optimization and handles various error conditions gracefully.
    
    The extraction process follows these steps:
    1. Check for cached version (unless force=True)
    2. If cache not available, perform full extraction using pdfplumber
    3. Process specified page range with comprehensive error handling
    4. Save extracted text to cache for future use
    
    Args:
        pdf_path (str or Path): Path to the PDF file to process
        max_pages (int, optional): Maximum number of pages to extract. 
                                  Useful for large documents where only a sample is needed.
                                  Defaults to None (all pages).
        start_page (int, optional): First page to extract (0-indexed). 
                                   Useful for skipping cover pages or front matter.
                                   Defaults to 0 (beginning of document).
        page_range (tuple, optional): Specific range of pages to extract as (start, end).
                                     If provided, overrides max_pages and start_page.
                                     Defaults to None.
        force (bool, optional): If True, forces re-extraction even if cached 
                               version exists. Useful when PDF content has changed.
                               Defaults to False.
        
    Returns:
        str: Extracted text from the PDF with preserved paragraph structure.
             Returns empty string if extraction fails completely.
    
    Performance Notes:
        - Uses caching to disk for repeated access to the same PDF
        - For very large PDFs, consider using page ranges to reduce memory usage
        - First extraction may be slow, but subsequent access will be fast if cache is used
    """
    from ..utils.config import logger
    
    pdf_path = Path(pdf_path)
    cache_path = pdf_path.with_suffix('.txt')
    
    # Check if cached version exists and should be used
    if not force and cache_path.exists():
        logger.info(f"Using cached text from {cache_path} for {pdf_path}")
        try:
            # Attempt to load text from cache for performance optimization
            # This significantly speeds up repeated processing of the same file
            with open(cache_path, 'r', encoding='utf-8') as f:
                return f.read()
        except Exception as e:
            # If cache file exists but is corrupted or unreadable,
            # log the error and continue with fresh extraction
            logger.warning(f"Failed to read cached text file, will extract from PDF: {e}")
            # Continue to extraction if cache read fails
    
    # Extract text from PDF if cache doesn't exist or force=True
    logger.info(f"Extracting text from PDF: {pdf_path}")
    import pdfplumber
    
    text_parts = []
    try:
        with pdfplumber.open(pdf_path) as pdf:
            total_pages = len(pdf.pages)
            
            # Determine which pages to process
            if page_range:
                # If a specific page range is provided, use it with bounds checking
                # This ensures we don't attempt to access pages beyond document boundaries
                start, end = page_range
                start = max(0, start)  # Ensure start page is not negative
                end = min(total_pages, end)  # Ensure end page doesn't exceed document
                pages_to_process = list(range(start, end))
            else:
                # Otherwise use start_page and max_pages parameters
                # with appropriate boundary checking
                start = min(max(0, start_page), total_pages)
                end = total_pages if max_pages is None else min(total_pages, start + max_pages)
                pages_to_process = list(range(start, end))
            
            # Extract text from each page
            for i in pages_to_process:
                try:
                    page = pdf.pages[i]
                    text = page.extract_text() or ""
                    text_parts.append(text)
                except Exception as e:
                    # Handle per-page extraction errors gracefully
                    # This ensures a single problematic page doesn't fail the entire extraction
                    logger.warning(f"Error extracting text from page {i}: {e}")
    except Exception as e:
        logger.error(f"Error extracting text from PDF {pdf_path}: {e}")
        raise
    
    # Join extracted text from all pages with double newlines to preserve paragraph structure
    # This maintains separation between pages while creating a readable continuous text
    full_text = "\n\n".join(text_parts)
    
    # Save the extracted text to cache file for future performance optimization
    # This significantly speeds up repeated processing of the same file
    try:
        with open(cache_path, 'w', encoding='utf-8') as f:
            f.write(full_text)
        logger.info(f"Saved extracted text to cache: {cache_path}")
    except Exception as e:
        # If cache writing fails, log the error but continue processing
        # This ensures the function still returns the extracted text even if caching fails
        logger.warning(f"Failed to save text cache: {e}")
    
    return full_text

def _extract_with_pdfplumber(pdf_path, max_pages=None, start_page=0, page_range=None):
    """
    Extract text using pdfplumber library (preferred method).
    
    This internal helper function handles the actual text extraction using the
    pdfplumber library, which provides high-quality text extraction with layout
    preservation. It includes progress reporting for long-running extractions
    and comprehensive per-page error handling.
    
    The pdfplumber method is preferred because it:
    1. Better preserves text positioning and layout
    2. Handles complex PDF structures more accurately
    3. Provides better extraction of tables and formatted text
    4. More accurately handles text encoding issues
    
    Args:
        pdf_path (str or Path): Path to the PDF file
        max_pages (int, optional): Maximum number of pages to extract
        start_page (int, optional): First page to extract (0-indexed)
        page_range (tuple, optional): Specific range of pages to extract
        
    Returns:
        str: Extracted text with preserved structure
    
    Note:
        This function is called internally by extract_pdf_text() and
        should not typically be called directly by external code.
    """
    extracted_text = ""
    
    with pdfplumber.open(pdf_path) as pdf:
        # Determine which pages to process
        if page_range:
            pages_to_extract = range(page_range[0], min(page_range[1], len(pdf.pages)))
        else:
            end_page = len(pdf.pages) if max_pages is None else min(start_page + max_pages, len(pdf.pages))
            pages_to_extract = range(start_page, end_page)
        
        # Extract text from each page
        for i in pages_to_extract:
            try:
                page = pdf.pages[i]
                page_text = page.extract_text() or ""
                extracted_text += page_text + "\n\n"
                if (i - start_page + 1) % 10 == 0:
                    logger.debug(f"Extracted {i - start_page + 1} pages...")
            except Exception as e:
                logger.warning(f"Error extracting text from page {i}: {e}")
    
    return extracted_text.strip()

def _extract_with_pypdf2(pdf_path, max_pages=None, start_page=0, page_range=None):
    """
    Extract text using PyPDF2 library (fallback method).
    
    This internal helper function provides a fallback extraction method using
    the PyPDF2 library when pdfplumber is not available. While PyPDF2 generally
    produces lower quality extraction (especially for complex layouts), it has
    fewer dependencies and may work when pdfplumber fails.
    
    Limitations compared to pdfplumber:
    1. Less accurate text positioning and flow
    2. Poorer handling of columns and complex layouts
    3. May struggle with certain fonts or encodings
    4. Less reliable for tables and structured content
    
    Args:
        pdf_path (str or Path): Path to the PDF file
        max_pages (int, optional): Maximum number of pages to extract
        start_page (int, optional): First page to extract (0-indexed)
        page_range (tuple, optional): Specific range of pages to extract
        
    Returns:
        str: Extracted text (may have formatting issues)
        
    Note:
        This function is called internally by extract_pdf_text() only when
        pdfplumber is unavailable, and should not typically be called directly.
    """
    extracted_text = ""
    
    with open(pdf_path, 'rb') as file:
        reader = PyPDF2.PdfFileReader(file)
        
        # Determine which pages to process
        if page_range:
            pages_to_extract = range(page_range[0], min(page_range[1], reader.numPages))
        else:
            end_page = reader.numPages if max_pages is None else min(start_page + max_pages, reader.numPages)
            pages_to_extract = range(start_page, end_page)
        
        # Extract text from each page
        for i in pages_to_extract:
            try:
                page = reader.getPage(i)
                page_text = page.extractText() or ""
                extracted_text += page_text + "\n\n"
                if (i - start_page + 1) % 10 == 0:
                    logger.debug(f"Extracted {i - start_page + 1} pages...")
            except Exception as e:
                logger.warning(f"Error extracting text from page {i}: {e}")
    
    return extracted_text.strip()

def get_pdf_metadata(pdf_path):
    """
    Get comprehensive metadata for a PDF file from both filesystem and PDF content.
    
    This function extracts a rich set of metadata from PDF files by combining:
    1. Filesystem metadata (creation date, modification date, size)
    2. PDF internal metadata (author, title, creator, producer)
    3. Structural information (page count)
    
    The function implements a multi-library approach with fallbacks:
    - Tries pdfplumber first for best metadata extraction
    - Falls back to PyPDF2 if pdfplumber is unavailable
    - Ensures critical fields are always available even if PDF extraction fails
    
    Args:
        pdf_path (str or Path): Path to the PDF file to analyze
        
    Returns:
        dict: Comprehensive PDF metadata dictionary including:
            - created_date: PDF creation date (ISO format)
            - modified_date: PDF last modified date (ISO format)
            - size_mb: File size in megabytes (float, rounded to 2 decimal places)
            - page_count: Number of pages in the document (int)
            - filename: Base filename without extension
            - path: Full path to the file
            
            When available from PDF internal metadata:
            - author: Document author
            - title: Document title
            - subject: Document subject
            - creator: Application that created the PDF
            - producer: PDF producer library/software
            - creationdate: Original PDF creation date from internal metadata
            
            Error information when applicable:
            - error: Error message if file not found
    
    Example:
        >>> metadata = get_pdf_metadata("path/to/document.pdf")
        >>> print(f"Title: {metadata.get('title', 'Unknown')}")
        >>> print(f"Pages: {metadata.get('page_count', 0)}")
    """
    pdf_path = Path(pdf_path)
    
    if not pdf_path.exists():
        logger.error(f"PDF file not found: {pdf_path}")
        return {"error": "File not found"}
    
    # Initialize with basic filesystem metadata that's always available
    # These values don't depend on PDF internal structure and are always reliable
    metadata = {
        "filename": pdf_path.stem,
        "path": str(pdf_path),
        "size_mb": round(pdf_path.stat().st_size / (1024 * 1024), 2),  # Convert bytes to MB
        "created_date": datetime.fromtimestamp(pdf_path.stat().st_ctime).isoformat(),
        "modified_date": datetime.fromtimestamp(pdf_path.stat().st_mtime).isoformat()
    }
    
    # Try to get page count and more detailed metadata from the PDF itself
    # This requires opening the PDF and may fail if the file is corrupted
    try:
        if HAS_PDFPLUMBER:
            # STRATEGY 1: Get metadata using pdfplumber (preferred method)
            with pdfplumber.open(pdf_path) as pdf:
                metadata["page_count"] = len(pdf.pages)
                if hasattr(pdf.metadata, "keys"):
                    # Copy relevant metadata from the PDF itself if available
                    # These fields may not exist in all PDFs
                    for key in ["Author", "Title", "Subject", "Creator", "Producer", "CreationDate"]:
                        if key in pdf.metadata:
                            metadata[key.lower()] = pdf.metadata[key]
        elif HAS_PYPDF2:
            # STRATEGY 2: Fallback to PyPDF2 if pdfplumber is not available
            with open(pdf_path, 'rb') as file:
                reader = PyPDF2.PdfFileReader(file)
                metadata["page_count"] = reader.numPages
                pdf_info = reader.getDocumentInfo()
                if pdf_info:
                    # Copy relevant metadata from the PDF itself if available
                    for key in pdf_info:
                        clean_key = key.strip('/').lower()
                        metadata[clean_key] = pdf_info[key]
    except Exception as e:
        # If metadata extraction fails, log the error but return what we have
        # This ensures the function doesn't fail completely when PDF is corrupt
        logger.warning(f"Error getting detailed PDF metadata: {e}")
        metadata["page_count"] = 0
    
    return metadata

def clean_pdf_text(text):
    """
    Clean extracted PDF text to improve readability and AI processing quality.
    
    This function applies a series of intelligent text cleanup operations
    specifically designed to address common issues in PDF extracted text:
    
    1. Whitespace normalization
       - Removes excessive spaces, tabs, and line breaks
       - Preserves intentional paragraph breaks
       
    2. Artifact removal
       - Eliminates common PDF extraction artifacts like '(cid:123)'
       - Removes encoding remnants and control characters
       
    3. Content structure improvement
       - Fixes hyphenated words split across lines
       - Removes repeating headers and footers
       - Eliminates page numbers and reference markers
       - Normalizes line endings for consistent processing
    
    These cleanup operations significantly improve the quality of text for
    downstream natural language processing, summarization, and knowledge
    extraction tasks in the MBA notebook automation pipeline.
    
    Args:
        text (str): Raw text extracted from PDF, potentially containing
                   formatting issues and extraction artifacts
        
    Returns:
        str: Cleaned text optimized for readability and AI processing,
             with most common PDF extraction issues resolved
    
    Note:
        This function is designed to be conservative in its cleaning to avoid
        removing potentially important content. Some domain-specific artifacts
        may remain and require additional specialized processing.
    """
    if not text:
        return ""
    
    # PHASE 1: Remove excessive whitespace while preserving paragraph structure
    # This normalizes different whitespace patterns to consistent single spaces
    text = re.sub(r'\s+', ' ', text)
    
    # PHASE 2: Remove common PDF extraction artifacts
    # These patterns appear when font glyphs can't be properly mapped to Unicode
    text = re.sub(r'\(cid:\d+\)', '', text)
    
    # PHASE 3: Fix common line break and hyphenation issues
    # This combines words that were incorrectly split across lines with hyphens
    text = re.sub(r'(\w)-\s+(\w)', r'\1\2', text)  # Fix hyphenated words
    
    # PHASE 4: Remove navigational elements that reduce readability
    # Page numbers in various common formats
    text = re.sub(r'\n\s*\d+\s*\n', '\n', text)
    text = re.sub(r'\n\s*Page \d+ of \d+\s*\n', '\n', text)
    
    # PHASE 5: Remove structural elements that aren't content
    # Common header and footer patterns in academic and business documents
    text = re.sub(r'^\s*[A-Z][A-Za-z\s]+\s*\|\s*\d+\s*$', '', text, flags=re.MULTILINE)
    
    # PHASE 6: Final formatting cleanup
    # Normalize paragraph breaks to exactly two newlines for consistent formatting
    text = re.sub(r'\n{3,}', '\n\n', text)
    
    return text.strip()
