#!/usr/bin/env python3
"""
Test Tag Restructuring on Sample Files

This script runs the tag restructuring on a small sample of files
to verify it works correctly before applying to the entire vault.

Usage:
    python test_restructure_tags.py [vault_path] [--sample-size N] [--specific-types type1,type2]
"""

import os
import sys
import random
import argparse
import re
from pathlib import Path
from restructure_tags import update_file_tags, identify_document_type

def parse_args():
    """
    Parse command line arguments.
    
    Returns:
        argparse.Namespace: Parsed arguments
    """
    parser = argparse.ArgumentParser(description="Test tag restructuring on sample files")
    
    # Default vault path
    default_vault = "D:\\Vault\\01_Projects\\MBA" if os.name == 'nt' else "/mnt/d/Vault/01_Projects/MBA"
    
    parser.add_argument("vault_path", nargs="?", default=default_vault,
                      help=f"Path to the Obsidian vault (default: {default_vault})")
    parser.add_argument("--sample-size", type=int, default=3,
                      help="Number of files to sample per directory (default: 3)")
    parser.add_argument("--specific-types", type=str, default="",
                      help="Comma-separated list of specific document types to focus on (e.g., lecture,case-study)")
    parser.add_argument("--specific-folder", type=str, default="",
                      help="Specific folder to test (relative to vault path)")
    
    return parser.parse_args()

def select_sample_files(vault_path, sample_size=3, specific_types=None, specific_folder=""):
    """
    Select a sample of markdown files from different directories.
    
    Args:
        vault_path (str): Path to the Obsidian vault
        sample_size (int): Number of files to sample per directory
        specific_types (list): List of document types to focus on
        specific_folder (str): Specific folder to test
        
    Returns:
        dict: Dictionary with folder paths as keys and lists of files as values
    """
    folder_files = {}
    
    # Determine the root path to start from
    root_path = os.path.join(vault_path, specific_folder) if specific_folder else vault_path
    
    # Traverse the vault and collect files by folder
    for root, _, files in os.walk(root_path):
        md_files = [os.path.join(root, f) for f in files if f.endswith('.md')]
        
        # Filter for specific document types if requested
        if specific_types and md_files:
            filtered_files = []
            for file_path in md_files:
                try:
                    with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                        content = f.read(1000)  # Read just enough to identify type
                    
                    # Look for type indicators in content
                    matches_type = False
                    for doc_type in specific_types:
                        if doc_type in content.lower() or doc_type in file_path.lower():
                            matches_type = True
                            break
                    
                    if matches_type:
                        filtered_files.append(file_path)
                except Exception:
                    continue  # Skip problematic files
            
            md_files = filtered_files
        
        if md_files:
            # Get relative path for pretty printing
            rel_path = os.path.relpath(root, vault_path)
            # Take up to sample_size random files from each directory
            sampled = random.sample(md_files, min(sample_size, len(md_files)))
            folder_files[rel_path] = sampled
    
    return folder_files

def test_restructuring(folder_files, vault_path):
    """
    Test tag restructuring on sample files without making changes.
    
    Args:
        folder_files (dict): Dictionary of folder paths and their sample files
        vault_path (str): Path to the vault root
        
    Returns:
        tuple: (successful tests, total tests)
    """
    successful = 0
    total = 0
    
    print("\nTEST RESULTS BY FOLDER:")
    print("="*60)
    
    for folder, files in folder_files.items():
        print(f"\nFolder: {folder}")
        print("-"*60)
        
        for file_path in files:
            total += 1
            file_name = os.path.basename(file_path)
            rel_path = os.path.relpath(file_path, vault_path)
            
            print(f"\n{total}. {file_name}")
            print(f"   Path: {rel_path}")
            
            try:
                # Get content for analysis
                with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read(2000)  # First 2000 chars for analysis
                
                # Extract existing tags first (for comparison)
                current_tags = []
                yaml_match = re.search(r'^---\s+(.*?)\s+---', content, re.DOTALL)
                if yaml_match:
                    yaml_text = yaml_match.group(1)
                    if 'tags:' in yaml_text:
                        tag_lines = re.findall(r'tags:\s*\n((?:\s*-.*\n)*)|tags:\s*\[(.*)\]', yaml_text)
                        if tag_lines:
                            if tag_lines[0][0]:  # List format
                                current_tags = re.findall(r'\s*- (.*)\n', tag_lines[0][0])
                            elif tag_lines[0][1]:  # Inline format
                                current_tags = [t.strip() for t in tag_lines[0][1].split(',')]
                
                # Identify document type
                tag_dict = identify_document_type(file_path, content)
                
                print(f"   Identified Document Properties:")
                for category, values in tag_dict.items():
                    print(f"     - {category}: {values}")
                
                # Test tag update in simulation mode
                success, old_tags, new_tags = update_file_tags(file_path, simulate=True)
                
                if success:
                    print(f"   Current tags: {old_tags}")
                    print(f"   New tags: {new_tags}")
                    
                    # Highlight differences
                    added = set(new_tags) - set(old_tags)
                    removed = set(old_tags) - set(new_tags)
                    
                    if added:
                        print(f"   Tags to add: {sorted(added)}")
                    if removed:
                        print(f"   Tags to remove: {sorted(removed)}")
                    
                    successful += 1
                else:
                    print(f"   Failed to process file")
                    
            except Exception as e:
                print(f"   Error processing file: {e}")
    
    return successful, total

def main():
    """Main function."""
    args = parse_args()
    
    # Check if vault path exists
    if not os.path.exists(args.vault_path):
        print(f"Error: Vault path does not exist: {args.vault_path}")
        sys.exit(1)
    
    print(f"Testing tag restructuring in vault: {args.vault_path}")
    print(f"Sample size per folder: {args.sample_size}")
    
    # Process specific types argument
    specific_types = []
    if args.specific_types:
        specific_types = [t.strip() for t in args.specific_types.split(',')]
        print(f"Focusing on document types: {specific_types}")
    
    # Select sample files
    folder_files = select_sample_files(
        args.vault_path, 
        args.sample_size, 
        specific_types, 
        args.specific_folder
    )
    
    # Count total files
    total_files = sum(len(files) for files in folder_files.values())
    total_folders = len(folder_files)
    
    print(f"\nSelected {total_files} files from {total_folders} folders for testing")
    
    # Test restructuring
    successful, total = test_restructuring(folder_files, args.vault_path)
    
    # Print summary
    print("\nTEST SUMMARY:")
    print("="*60)
    print(f"Total files tested: {total}")
    print(f"Successfully processed: {successful}")
    print(f"Success rate: {successful/total*100:.1f}%")
    
    if successful < total:
        print("\nWarning: Some files could not be processed. Review errors above.")
        print("You may want to fix these issues before applying to your entire vault.")
    else:
        print("\nAll tests successful! You can now run restructure_tags.py with --apply")

if __name__ == "__main__":
    main()
