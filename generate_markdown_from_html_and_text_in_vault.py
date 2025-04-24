#!/usr/bin/env python
"""
Markdown Generator from HTML and TXT - Part of the MBA Notebook Automation toolkit

This script is a copy of generate-index.py and contains all original functionality (index generation and file conversion).
You can now remove index generation from this script and remove file conversion from generate-index.py as needed.
"""

import re
import os
import sys
import yaml  # For metadata parsing
import argparse  # For command line arguments
from pathlib import Path
from datetime import datetime
import urllib.parse
import shutil
import html2text

# --- Configurable settings ---

# Source directories for HTML and TXT files
SRC_DIRS = ['.', './notebooks']

# Destination directory for Markdown files
DEST_DIR = './markdown'

# Pattern to match notebook files
NOTEBOOK_PATTERN = r'.*\.(ipynb|html|txt)$'

# --- End of configurable settings ---

# --- Functions ---

def parse_args():
    parser = argparse.ArgumentParser(description='Generate Markdown files from HTML and TXT sources.')
    parser.add_argument('--src_dirs', nargs='+', default=SRC_DIRS,
                        help='List of source directories to scan for HTML and TXT files.')
    parser.add_argument('--dest_dir', default=DEST_DIR,
                        help='Destination directory for Markdown files.')
    return parser.parse_args()

def convert_html_to_markdown(html_content):
    return html2text.html2text(html_content)

def process_file(src_file, dest_dir):
    # Determine the relative path and create the corresponding destination path
    rel_path = os.path.relpath(src_file, start=SRC_DIRS[0])
    dest_file = os.path.join(dest_dir, rel_path)
    dest_file = os.path.splitext(dest_file)[0] + '.md'  # Change the file extension to .md

    # Create the destination directory if it doesn't exist
    os.makedirs(os.path.dirname(dest_file), exist_ok=True)

    # Read the source file
    with open(src_file, 'r', encoding='utf-8') as f:
        if src_file.endswith('.html'):
            html_content = f.read()
            markdown_content = convert_html_to_markdown(html_content)
            # Write the Markdown content to the destination file
            with open(dest_file, 'w', encoding='utf-8') as md_file:
                md_file.write(markdown_content)
            print(f'Converted HTML to Markdown: {src_file} -> {dest_file}')
        elif src_file.endswith('.txt'):
            txt_content = f.read()
            # Write the TXT content directly to the destination file with .md extension
            with open(dest_file, 'w', encoding='utf-8') as md_file:
                md_file.write(txt_content)
            print(f'Converted TXT to Markdown: {src_file} -> {dest_file}')

def generate_markdown(src_dirs, dest_dir):
    for src_dir in src_dirs:
        for root, dirs, files in os.walk(src_dir):
            for file in files:
                if re.match(NOTEBOOK_PATTERN, file):
                    src_file = os.path.join(root, file)
                    process_file(src_file, dest_dir)

# --- Main script ---

if __name__ == "__main__":
    args = parse_args()
    generate_markdown(args.src_dirs, args.dest_dir)
