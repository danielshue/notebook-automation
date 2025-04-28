#!/usr/bin/env python
"""
Generate Obsidian-compatible links to video and PDF files stored in OneDrive.

This script scans your OneDrive Education/MBA Resources folder and generates
markdown files with links to videos and PDFs that can be used in Obsidian.

Usage:
    wsl python3 generate_obsidian_links.py
"""

import os
import re
import json
import logging
from pathlib import Path
from datetime import datetime
from urllib.parse import quote

# Configure paths
ONEDRIVE_ROOT = Path("/mnt/c/Users/danielshue/OneDrive/Education/MBA Resources")
LINKS_OUTPUT_DIR = Path("/mnt/d/Vault/01_Projects/MBA/Resources")
LOG_FILE = "obsidian_links_generator.log"

# File extension filters
VIDEO_EXTENSIONS = ['.mp4', '.mov', '.avi', '.wmv', '.mkv', '.m4v', '.webm', '.flv']
DOCUMENT_EXTENSIONS = ['.pdf', '.epub', '.mobi', '.azw', '.azw3', '.djvu']
ALL_EXTENSIONS = VIDEO_EXTENSIONS + DOCUMENT_EXTENSIONS

# Set up logging
logging.basicConfig(
    filename=LOG_FILE,
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)

console = logging.StreamHandler()
console.setLevel(logging.INFO)
formatter = logging.Formatter('%(message)s')
console.setFormatter(formatter)
logging.getLogger('').addHandler(console)

def clean_filename(name):
    """Clean a filename to be used as a markdown file name."""
    # Remove non-alphanumeric characters and replace with spaces
    cleaned = re.sub(r'[^\w\s-]', ' ', name)
    # Replace multiple spaces with single space
    cleaned = re.sub(r'\s+', ' ', cleaned).strip()
    # Replace spaces with hyphens
    cleaned = cleaned.replace(' ', '-').lower()
    return cleaned

def get_course_name(path):
    """Extract the course name from file path."""
    rel_path = Path(path).relative_to(ONEDRIVE_ROOT)
    parts = rel_path.parts
    
    if len(parts) >= 1:
        if parts[0] == "Electives" and len(parts) >= 2:
            return f"Electives/{parts[1]}"
        return parts[0]
    return "Unknown"

def get_windows_path(unix_path):
    """Convert Unix path to Windows path for Obsidian compatibility."""
    # Remove the /mnt/c prefix and replace with C:
    win_path = str(unix_path).replace('/mnt/c', 'C:')
    # Replace forward slashes with backslashes for Windows
    win_path = win_path.replace('/', '\\')
    # URL encode the path for special characters
    return f"file:///{quote(win_path)}"

def scan_files():
    """Scan OneDrive for media files and generate an organized structure."""
    logging.info(f"Scanning OneDrive folder: {ONEDRIVE_ROOT}")
    
    courses = {}
    total_videos = 0
    total_documents = 0
    
    # Scan for files
    for root, _, files in os.walk(ONEDRIVE_ROOT):
        for file in files:
            file_path = os.path.join(root, file)
            file_ext = os.path.splitext(file)[1].lower()
            
            # Skip if not a media file we're interested in
            if file_ext not in ALL_EXTENSIONS:
                continue
                
            # Get the course name
            course_name = get_course_name(file_path)
            
            # Create course entry if it doesn't exist
            if course_name not in courses:
                courses[course_name] = {"videos": [], "documents": []}
            
            # Get file details
            rel_path = os.path.relpath(file_path, ONEDRIVE_ROOT)
            file_size_mb = os.path.getsize(file_path) / (1024 * 1024)
            windows_path = get_windows_path(file_path)
            
            # Add to appropriate list
            if file_ext in VIDEO_EXTENSIONS:
                courses[course_name]["videos"].append({
                    "name": file,
                    "path": rel_path,
                    "size_mb": file_size_mb,
                    "windows_path": windows_path
                })
                total_videos += 1
            elif file_ext in DOCUMENT_EXTENSIONS:
                courses[course_name]["documents"].append({
                    "name": file,
                    "path": rel_path,
                    "size_mb": file_size_mb,
                    "windows_path": windows_path
                })
                total_documents += 1
    
    logging.info(f"Found {total_videos} video files and {total_documents} document files across {len(courses)} courses")
    return courses, total_videos, total_documents

def generate_index_file(courses, total_videos, total_documents, force=False):
    """Generate a master index markdown file, respecting --force flag."""
    os.makedirs(LINKS_OUTPUT_DIR, exist_ok=True)
    index_path = LINKS_OUTPUT_DIR / "MBA Resources Index.md"
    content = "# MBA Resources Index\n\n"
    content += f"*Generated on {datetime.now().strftime('%Y-%m-%d at %H:%M')}*\n\n"
    content += f"This index contains links to {total_videos} video files and {total_documents} document files across {len(courses)} courses in your OneDrive MBA Resources folder.\n\n"
    content += "## Table of Contents\n\n"
    for course in sorted(courses.keys()):
        safe_course = course.replace('/', '-')
        content += f"- [{course}](#course-{clean_filename(safe_course)})\n"
    content += "\n"
    for course in sorted(courses.keys()):
        safe_course = course.replace('/', '-')
        content += f"## Course: {course} {{{{{{'#course-{clean_filename(safe_course)}'}}}}}}\n\n"
        if courses[course]["videos"]:
            content += "### Videos\n\n"
            for video in sorted(courses[course]["videos"], key=lambda x: x["name"]):
                content += f"- [{video['name']}]({video['windows_path']}) - *{video['size_mb']:.2f} MB*\n"
            content += "\n"
        if courses[course]["documents"]:
            content += "### Documents\n\n"
            for doc in sorted(courses[course]["documents"], key=lambda x: x["name"]):
                content += f"- [{doc['name']}]({doc['windows_path']}) - *{doc['size_mb']:.2f} MB*\n"
            content += "\n"
    safe_write_file(index_path, content, force=force)
    logging.info(f"Index generated at: {index_path}")
    return index_path

def generate_course_files(courses, force=False):
    """Generate individual markdown files for each course, respecting --force flag."""
    course_files = []
    for course in courses:
        safe_course = clean_filename(course.replace('/', '-'))
        course_path = LINKS_OUTPUT_DIR / f"{safe_course}_resources.md"
        content = f"# {course} Resources\n\n"
        content += f"*Generated on {datetime.now().strftime('%Y-%m-%d at %H:%M')}*\n\n"
        if courses[course]["videos"]:
            content += "## Videos\n\n"
            for video in sorted(courses[course]["videos"], key=lambda x: x["name"]):
                content += f"- [{video['name']}]({video['windows_path']}) - *{video['size_mb']:.2f} MB*\n"
            content += "\n"
        if courses[course]["documents"]:
            content += "## Documents\n\n"
            for doc in sorted(courses[course]["documents"], key=lambda x: x["name"]):
                content += f"- [{doc['name']}]({doc['windows_path']}) - *{doc['size_mb']:.2f} MB*\n"
            content += "\n"
        safe_write_file(course_path, content, force=force)
        course_files.append(course_path)
        logging.info(f"Created course resource file: {course_path}")
    return course_files

def safe_write_file(path, content, force=False):
    """
    Write content to a file only if it doesn't exist, or if force is True.
    Returns True if file was written, False if skipped.
    """
    if path.exists() and not force:
        logging.info(f"File already exists and --force not set, skipping: {path}")
        return False
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
    logging.info(f"File written: {path}")
    return True

def main():
    import argparse
    parser = argparse.ArgumentParser(description="Generate Obsidian-compatible links to video and PDF files stored in OneDrive.")
    parser.add_argument('--force', action='store_true', help='Overwrite existing files if they exist')
    args = parser.parse_args()
    force = args.force

    logging.info("Starting Obsidian links generator")
    
    # Scan files
    courses, total_videos, total_documents = scan_files()
    
    # Generate the main index file
    index_path = generate_index_file(courses, total_videos, total_documents, force=force)
    
    # Generate individual course files
    course_files = generate_course_files(courses, force=force)
    
    # Generate results summary
    results = {
        "timestamp": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
        "total_courses": len(courses),
        "total_videos": total_videos,
        "total_documents": total_documents,
        "index_file": str(index_path),
        "course_files": [str(path) for path in course_files]
    }
    
    # Save results to JSON
    with open("obsidian_links_results.json", 'w', encoding='utf-8') as f:
        json.dump(results, f, indent=2)
    
    logging.info("\n" + "="*50)
    logging.info("SUMMARY:")
    logging.info(f"Total courses: {len(courses)}")
    logging.info(f"Total videos: {total_videos}")
    logging.info(f"Total documents: {total_documents}")
    logging.info(f"Main index created: {index_path}")
    logging.info(f"Course files created: {len(course_files)}")
    logging.info("="*50)
    
    logging.info("\nNext Steps:")
    logging.info("1. Open your Vault in Obsidian")
    logging.info(f"2. Navigate to the generated index file: {index_path.name}")
    logging.info("3. Use the links to access your videos and PDFs directly from Obsidian")

if __name__ == "__main__":
    main()
