#!/usr/bin/env python3
"""
PDF Page Extractor CLI for MBA Notebook Automation

This module provides a command-line interface for extracting specific pages from
PDF files. It supports single files or directories, and various page range formats.

Examples:
    vault-extract-pdf-pages input.pdf 1-5             # Extract to auto-named output
    vault-extract-pdf-pages input.pdf 1-5 output.pdf  # Extract to specific output
    vault-extract-pdf-pages dir/ 1,3,5                # Process PDFs in directory
"""

import os
import sys
import re
import glob
import argparse
from pathlib import Path
from typing import List, Set
from PyPDF2 import PdfReader, PdfWriter

from ..tools.pdf.utils import is_pdf_file
from notebook_automation.cli.utils import OKCYAN, ENDC


def parse_page_range(page_range_str: str, max_page: int) -> List[int]:
    """Parse a page range string into a list of page numbers.
    
    Converts a human-readable page range string into a list of actual page indices
    for PDF extraction. Handles comma-separated values and hyphenated ranges.
    
    Args:
        page_range_str (str): String specifying page ranges like "1-5,7,9-12"
        max_page (int): Maximum page number in the document (for validation)
        
    Returns:
        List[int]: List of page numbers (0-based for PyPDF2)
        
    Raises:
        ValueError: If the page range format is invalid or exceeds document bounds
        
    Example:
        >>> parse_page_range("1-3,5", 10)
        [0, 1, 2, 4]
    """
    pages = set()
    
    # Split into comma-separated parts
    parts = page_range_str.split(",")
    
    for part in parts:
        # Check if this part is a range (contains "-")
        if "-" in part:
            try:
                start, end = map(int, part.split("-"))
                # Adjust for 0-based indexing in PyPDF2
                start = max(1, start)  # Ensure minimum of 1
                start_idx = start - 1  # Convert to 0-based
                
                end_idx = min(end, max_page) - 1  # Convert to 0-based and cap at max
                
                # Validate range
                if start > end:
                    raise ValueError(f"Invalid range: {part} (start > end)")
                if start < 1:
                    raise ValueError(f"Invalid range: {part} (pages start at 1)")
                if end > max_page:
                    print(f"Warning: Range {part} exceeds document length. Capping at page {max_page}.")
                
                # Add all pages in range
                pages.update(range(start_idx, end_idx + 1))
            except ValueError:
                raise ValueError(f"Invalid page range format: {part}")
        else:
            # Handle single page
            try:
                page = int(part)
                
                # Validate page
                if page < 1:
                    raise ValueError(f"Invalid page number: {page} (pages start at 1)")
                if page > max_page:
                    raise ValueError(f"Page number {page} exceeds document length ({max_page} pages)")
                
                # Adjust for 0-based indexing in PyPDF2
                pages.add(page - 1)
            except ValueError:
                raise ValueError(f"Invalid page number: {part}")
    
    # Sort pages in ascending order
    return sorted(pages)


def find_pdf_files(path: str) -> List[str]:
    """Find PDF files in a given path.
    
    Locates PDF files from a path that could be a single file, directory,
    or glob pattern. Returns a sorted list of all matching PDF file paths.
    
    Args:
        path (str): Path to a file, directory, or glob pattern
        
    Returns:
        List[str]: Sorted list of paths to PDF files found
        
    Example:
        >>> find_pdf_files("./documents/")
        ['/documents/doc1.pdf', '/documents/doc2.pdf']
        >>> find_pdf_files("./documents/report.pdf")
        ['/documents/report.pdf']
    """
    if os.path.isfile(path) and path.lower().endswith('.pdf'):
        return [path]
    
    if os.path.isdir(path):
        # Find all PDFs in directory
        pdf_files = glob.glob(os.path.join(path, "*.pdf"))
        return sorted(pdf_files)
    
    # Try to find files with glob pattern
    pdf_files = glob.glob(path)
    if pdf_files:
        return [f for f in pdf_files if f.lower().endswith('.pdf')]
    
    # Try to find files in different locations by adding .pdf extension
    if not path.lower().endswith('.pdf'):
        pdf_path = f"{path}.pdf"
        if os.path.isfile(pdf_path):
            return [pdf_path]
            
    return []


def extract_pdf_pages(input_pdf: str, output_pdf: str, page_range_str: str) -> bool:
    """Extract specified pages from a PDF file into a new PDF document.
    
    This function processes a source PDF file, extracts the pages specified in the
    page range string, and saves them to a new PDF file. It provides detailed
    validation and progress information during processing.
    
    Args:
        input_pdf (str): Path to the input PDF file to extract pages from
        output_pdf (str): Path where the extracted pages will be saved as a new PDF
        page_range_str (str): Range of pages to extract in the format "1-5,7,9-12"
            where ranges are specified with hyphens and individual pages with commas
        
    Returns:
        bool: True if the extraction completed successfully, False on any error
        
    Raises:
        No exceptions are raised as all errors are handled internally and
        result in returning False with an error message printed
        
    Example:
        >>> extract_pdf_pages("lecture.pdf", "summary.pdf", "1-3,10")
        Processing: lecture.pdf
        PDF has 20 pages
        Extracting pages: 1, 2, 3, 10
        Successfully created summary.pdf with 4 pages
        True
    """
    try:
        print(f"Processing: {input_pdf}")
        
        # Validate input file
        if not is_pdf_file(input_pdf):
            print(f"Error: {input_pdf} is not a valid PDF file")
            return False
        
        # Open the input PDF
        reader = PdfReader(input_pdf)
        writer = PdfWriter()
        
        # Get the total number of pages
        total_pages = len(reader.pages)
        print(f"PDF has {total_pages} pages")
        
        # Parse the page range string
        try:
            pages_to_extract = parse_page_range(page_range_str, total_pages)
        except ValueError as e:
            print(f"Error parsing page range: {str(e)}")
            print(f"Valid range would be 1-{total_pages}")
            return False
        
        if not pages_to_extract:
            print("No valid pages to extract")
            return False
            
        print(f"Extracting {len(pages_to_extract)} pages...")
        
        # Add each specified page to the output PDF
        for i, page_idx in enumerate(pages_to_extract):
            try:
                writer.add_page(reader.pages[page_idx])
                if i % 10 == 0 or i == len(pages_to_extract) - 1:
                    print(f"Added page {page_idx + 1} ({i+1}/{len(pages_to_extract)})")
            except IndexError:
                print(f"Warning: Page {page_idx + 1} does not exist, skipping")
        
        # Check if we actually extracted any pages
        if len(writer.pages) == 0:
            print("No pages were successfully extracted")
            return False
        
        # Write the output PDF
        with open(output_pdf, "wb") as output_file:
            writer.write(output_file)
            
        print(f"Successfully extracted {len(writer.pages)} pages to {output_pdf}")
        return True
        
    except Exception as e:
        print(f"Error extracting PDF pages: {str(e)}")
        return False


def generate_output_filename(input_path: str, page_range: str) -> str:
    """Generate an output filename based on the input path and page range.
    
    Args:
        input_path: Path to input PDF file
        page_range: Page range string
        
    Returns:
        str: Generated output filename
    """
    base = os.path.splitext(input_path)[0]
    return f"{base}_pages_{page_range.replace(',', '_')}.pdf"


def main() -> None:
    """Main entry point for the PDF page extraction command-line tool.
    
    Parses command-line arguments, finds PDF files matching the input path,
    extracts the specified pages, and saves the result to the output file.
    Handles various error conditions and provides user feedback during processing.
    
    Usage:
        When run directly: python -m notebook_automation.cli.extract_pdf_pages input.pdf 1-5
        When installed: vault-extract-pdf-pages input.pdf 1-5
        
    Example:
        $ vault-extract-pdf-pages lectures/week1.pdf 5-10
        Processing: lectures/week1.pdf
        PDF has 30 pages
        Extracting pages: 5, 6, 7, 8, 9, 10
        Successfully created lectures/week1_pages_5-10.pdf with 6 pages
    """
    parser = argparse.ArgumentParser(
        description="Extract pages from a PDF file",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  vault-extract-pdf-pages input.pdf 1-5                # Extract to auto-named output
  vault-extract-pdf-pages input.pdf 1-5 output.pdf     # Extract to specific output
  vault-extract-pdf-pages directory/ 1,3,5             # Find PDFs in directory
  vault-extract-pdf-pages "path with spaces" 1-5       # Handle paths with spaces
  vault-extract-pdf-pages "C:\\path\\file.pdf" 1-5     # Windows paths work too
"""
    )
    
    parser.add_argument("input_path", 
                       help="Path to input PDF file or directory")
    parser.add_argument("page_range", 
                       help="Page range (e.g., 1-5,7,9-12)")
    parser.add_argument("output_pdf", nargs='?',
                       help="Output PDF path (optional)")
    parser.add_argument("-c", "--config", type=str, default=None,
                       help="Path to config.json file (optional)")
    
    args = parser.parse_args()
    
    # Set config path if provided
    if args.config:
        # Use absolute path to ensure consistency
        config_path = str(Path(args.config).absolute())
        os.environ["NOTEBOOK_CONFIG_PATH"] = config_path
        
    # Display which config.json file is being used
    try:
        from notebook_automation.tools.utils.config import find_config_path
        config_path = os.environ.get("NOTEBOOK_CONFIG_PATH") or find_config_path()
        print(f"{OKCYAN}Using configuration file: {config_path}{ENDC}")
    except Exception as e:
        print(f"Could not determine config file path: {e}")
    
    # Find PDF files based on the input path
    pdf_files = find_pdf_files(args.input_path)
    
    if not pdf_files:
        print(f"Error: No PDF files found at path '{args.input_path}'")
        sys.exit(1)
    
    # Handle multiple PDFs if found
    if len(pdf_files) > 1:
        print(f"Found {len(pdf_files)} PDF files:")
        for i, pdf in enumerate(pdf_files, 1):
            print(f"{i}: {pdf}")
        input_pdf = pdf_files[0]
        print(f"\nUsing first PDF: {input_pdf}")
    else:
        input_pdf = pdf_files[0]
    
    # Generate output filename if not provided
    output_pdf = args.output_pdf or generate_output_filename(input_pdf, args.page_range)
    
    # Ensure output directory exists
    output_dir = os.path.dirname(output_pdf)
    if output_dir and not os.path.exists(output_dir):
        print(f"Creating output directory: {output_dir}")
        try:
            os.makedirs(output_dir, exist_ok=True)
        except OSError as e:
            print(f"Error creating output directory: {e}")
            sys.exit(1)
    
    # Extract the pages
    success = extract_pdf_pages(input_pdf, output_pdf, args.page_range)
    
    # Exit with appropriate status code
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
