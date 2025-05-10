#!/usr/bin/env python3
"""
PDF Page Extractor for Notebook Automation

This script provides a utility for extracting a range of pages from a PDF file and
saving them as a new PDF document. It leverages the existing PDF utilities in the
Notebook Automation system while adding specific functionality for page extraction.

Key Features:
------------
1. Page Range Extraction
   - Extract specific page ranges (e.g., "1-5", "1,3,5-10")
   - Support for multiple discontinuous ranges
   - Input validation and error handling

2. Command-line Interface
   - Simple argument-based operation
   - Informative help and error messages
   - Progress reporting during extraction

3. Integration with Existing Tools
   - Uses PDF validation from the tools.pdf.utils module
   - Leverages existing error handling mechanisms
   - Maintains consistent logging approach

Usage:
------
python extract_pdf_pages.py input.pdf 1-5,7,9-12 output.pdf
"""

import os
import sys
import re
import argparse
import glob
from pathlib import Path

def convert_windows_to_wsl_path(path):
    """
    Convert a Windows path to a WSL path.
    
    Args:
        path (str): Windows path (e.g., 'C:\\Users\\name\\file.pdf')
        
    Returns:
        str: WSL path (e.g., '/mnt/c/Users/name/file.pdf')
    """
    # Check if it looks like a Windows path
    if not path or ('\\' not in path and ':' not in path):
        return path
    
    # Replace backslashes with forward slashes
    path = path.replace('\\', '/')
    
    # Handle drive letter (e.g., C: -> /mnt/c)
    if ':' in path:
        drive_letter = path[0].lower()
        path = f'/mnt/{drive_letter}/{path[3:]}'
    
    return path

# Add the repository root to the Python path to import the tools module
script_dir = Path(os.path.dirname(os.path.abspath(__file__)))
repo_root = script_dir
sys.path.append(str(repo_root))

try:
    # Import existing PDF tools modules to reuse functionality
    from notebook_automation.tools.pdf.utils import is_pdf_file
    
    # Import PyPDF2 directly for page extraction since it's not exposed in the tools
    from PyPDF2 import PdfReader, PdfWriter
except ImportError as e:
    print(f"Error importing required modules: {e}")
    print("Please make sure PyPDF2 is installed: pip install PyPDF2")
    sys.exit(1)

def parse_page_range(page_range_str, max_page):
    """
    Parse a page range string and convert it to a list of page numbers.
    
    Handles various formats:
    - Single pages: "1,3,5"
    - Ranges: "1-5" (inclusive)
    - Mixed: "1,3-5,7-9"
    
    Args:
        page_range_str (str): The page range string to parse
        max_page (int): The maximum page number in the PDF (for validation)
        
    Returns:
        list: A sorted list of page numbers to extract
        
    Raises:
        ValueError: If the range format is invalid or contains out-of-bounds pages
    """
    pages = set()
    
    # Remove all whitespace and split by commas
    parts = page_range_str.replace(" ", "").split(",")
    
    for part in parts:
        if "-" in part:
            # Handle range (e.g., "1-5")
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

def extract_pdf_pages(input_pdf, output_pdf, page_range_str):
    """
    Extract specified pages from a PDF file and save them to a new PDF file.
    
    Args:
        input_pdf (str): Path to the input PDF file
        output_pdf (str): Path to save the extracted pages
        page_range_str (str): Range of pages to extract, e.g., "1-5,7,9-12"
        
    Returns:
        bool: True if extraction was successful, False otherwise
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

def find_pdf_files(path):
    """
    Find PDF files in a given path.
    
    If path is a directory, returns all PDFs in that directory.
    If path is a file that exists, returns that file.
    If path doesn't exist, tries to find matching PDFs using glob patterns.
    
    Args:
        path (str): Path to a file or directory
        
    Returns:
        list: List of paths to PDF files found
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

def generate_output_filename(input_path, page_range):
    """
    Generate a default output filename based on the input filename and page range.
    
    Args:
        input_path (str): Path to the input PDF file
        page_range (str): Range of pages to extract
        
    Returns:
        str: Generated output filename with full path
    """
    # Get the directory of the input file
    input_dir = os.path.dirname(input_path)
    
    # Get the base filename without extension
    base_name = os.path.splitext(os.path.basename(input_path))[0]
    
    # Clean up the page range for use in a filename
    clean_range = page_range.replace(' ', '').replace(',', '-')
    if '-' not in clean_range:
        clean_range = clean_range.replace('_', '-')
    
    # Create the output filename in the same directory as the input file
    output_filename = f"{base_name} - Pages {clean_range}.pdf"
    
    # Combine with the directory path
    return os.path.join(input_dir, output_filename)

def main():
    """
    Main entry point for the PDF page extraction tool.
    
    This function:
    1. Parses command line arguments
    2. Validates input parameters and resolves file paths
    3. Extracts the specified pages
    4. Reports success or failure
    """
    # Create the argument parser
    parser = argparse.ArgumentParser(
        description="Extract pages from a PDF file and save them to a new PDF file",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python extract_pdf_pages.py input.pdf 1-5                # Extract pages 1-5 to auto-named output file
  python extract_pdf_pages.py input.pdf 1-5 output.pdf     # Extract pages 1-5 to specified output
  python extract_pdf_pages.py directory/ 1,3,5             # Find PDFs in directory and extract pages 1,3,5
  python extract_pdf_pages.py "path with spaces" 1-5       # Handle paths with spaces correctly
  python extract_pdf_pages.py "C:\\Users\\name\\file.pdf" 1-5  # Windows paths are automatically converted
"""
    )
    
    # Add arguments
    parser.add_argument("input_path", help="Path to the input PDF file or directory containing PDFs")
    parser.add_argument("page_range", help="Range of pages to extract (e.g., 1-5,7,9-12)")
    parser.add_argument("output_pdf", nargs='?', help="Path to save the extracted PDF (optional, auto-generated if not provided)")
    
    # Parse arguments
    args = parser.parse_args()
    
    # Convert Windows paths to WSL paths if needed
    input_path = convert_windows_to_wsl_path(args.input_path)
    if input_path != args.input_path:
        print(f"Converted Windows input path to WSL path:\n  From: {args.input_path}\n  To:   {input_path}")
    
    if args.output_pdf:
        output_pdf = convert_windows_to_wsl_path(args.output_pdf)
        if output_pdf != args.output_pdf:
            print(f"Converted Windows output path to WSL path:\n  From: {args.output_pdf}\n  To:   {output_pdf}")
        
        # Ensure output file has .pdf extension
        if not output_pdf.lower().endswith('.pdf'):
            output_pdf = f"{output_pdf}.pdf"
            print(f"Added .pdf extension to output path: {output_pdf}")
    else:
        output_pdf = None
    
    # Find PDF files based on the converted input path
    pdf_files = find_pdf_files(input_path)
    
    if not pdf_files:
        print(f"Error: No PDF files found at path '{args.input_path}'")
        sys.exit(1)
    
    # If multiple PDFs were found, list them and ask user to specify
    if len(pdf_files) > 1:
        print(f"Found {len(pdf_files)} PDF files:")
        for i, pdf in enumerate(pdf_files, 1):
            print(f"{i}: {pdf}")
        
        # For now, just use the first file
        input_pdf = pdf_files[0]
        print(f"Using first PDF: {input_pdf}")
    else:
        input_pdf = pdf_files[0]
    
    # Check if output directory exists
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