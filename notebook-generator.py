#!/usr/bin/env python
"""
MBA Notebook Automation System - Comprehensive Knowledge Management Platform

This script serves as the central engine for the MBA Notebook Automation system,
providing sophisticated organization, transformation, and management of academic
materials in an Obsidian knowledge base. It employs intelligent categorization,
hierarchical indexing, and metadata enhancement to create a highly structured
learning environment.

Key Features:
----------------
1. Intelligent File Processing
   - Converts diverse file formats (HTML, TXT, PDF) to standardized Markdown
   - Implements context-aware filename cleaning with academic terminology preservation
   - Adds rich YAML frontmatter with metadata templates appropriate to content type
   - Preserves custom content through readonly state detection

2. Hierarchical Knowledge Organization
   - Creates a 6-level hierarchy: Main → Program → Course → Class → Module → Lesson
   - Generates specialized index types: case studies, live sessions, course materials
   - Builds bidirectional navigation with breadcrumbs and backlinks
   - Intelligently infers relationships between academic materials

3. Enhanced Content Discovery
   - Automatically categorizes resources by type and purpose
   - Implements a multi-dimensional tagging system (structural, cognitive, workflow)
   - Adds visual indicators with context-appropriate icons
   - Creates consistent cross-linking between related materials

4. Content Adaptation
   - Transforms raw materials into knowledge-optimized formats
   - Intelligently removes numbering schemes while preserving meaningful identifiers
   - Standardizes link formatting for maximum compatibility
   - Handles special cases like live sessions and case studies with customized formatting

Knowledge Base Structure:
-----------------------
- Root (main-index)
  - Program Folders (program-index)
    - Course Folders (course-index)
      - Case Study Folders (case-studies-index)
      - Required Readings (readings-index)
      - Resources (resources-index)
      - Class Folders (class-index)
        - Module Folders (module-index)
          - Live Session Folder (live-session-index)
          - Lesson Folders (lesson-index)
            - Content Files (readings, videos, transcripts, notes, etc.)

Dependencies:
------------
- PyYAML: For metadata template loading and YAML frontmatter generation
- html2text: For HTML to Markdown conversion
- Standard libraries: re, os, sys, pathlib, urllib.parse, argparse, datetime

Usage Examples:
--------------
  # Generate indexes for your entire knowledge base
  python notebook-generator.py --generate-index --source /path/to/vault
  
  # Convert source files to markdown with proper formatting
  python notebook-generator.py --convert --source /path/to/source/files
  
  # Do both operations in one command
  python notebook-generator.py --all --source /path/to/vault
  
  # Generate only specific index types
  python notebook-generator.py --generate-index --source /path/to/vault --index-type module,lesson
  
  # Run in debug mode for verbose output
  python notebook-generator.py --all --source /path/to/vault --debug
"""

import re
import os
import sys
import yaml  # For metadata parsing
import argparse  # For command line arguments
import html2text  # For HTML to Markdown conversion
import shutil  # For file operations
import os
import sys
import re
import yaml
import shutil
import argparse
import time  # For tracking execution time
from pathlib import Path
from datetime import datetime
import urllib.parse
import html2text

# Import the logging setup from tools.utils.config
try:
    from tools.utils.config import setup_logging, VAULT_LOCAL_ROOT
    # Setup logging for this module
    logger, failed_logger = setup_logging(debug=False)
except ImportError:
    # Fallback if the module can't be imported
    import logging
    logger = logging.getLogger(__name__)
    failed_logger = logging.getLogger('failed_files')
    logging.basicConfig(level=logging.INFO)

# Function to load metadata templates from YAML file
def load_metadata_templates():
    """
    Load and parse metadata templates from the metadata.yaml configuration file.
    
    This function initializes the automation system's template engine by locating, 
    parsing, and validating the multi-document YAML configuration file containing
    metadata templates. These templates define the structure and properties for
    different content types in the knowledge base.
    
    The function performs the following steps:
    1. Locates the metadata.yaml file relative to the script location
    2. Validates file existence and reads content with UTF-8 encoding
    3. Splits the file into separate YAML documents (delimited by ---)
    4. Parses each document and validates required fields
    5. Organizes templates into a dictionary keyed by template-type
    
    Template Requirements:
    - Each template must be a valid YAML document
    - Each template must contain a 'template-type' field defining its purpose
    - Common template types: reading, transcript, video, note, quiz, assignment
    
    Returns:
        dict: A dictionary mapping template types to their complete template
             configuration. Returns an empty dict if no templates are found or
             if the metadata.yaml file doesn't exist.
             
    Example Template Structure:
    ```yaml
    template-type: reading
    tags: [reading, course-material]
    icon: 📄
    properties:
      status: unread
      importance: medium
    ---
    template-type: transcript
    tags: [transcript, video-related]
    icon: 📓
    properties:
      video-link: ""
      duration: 0
    ```
    """
    # Determine the script directory to find metadata.yaml relative to script location
    script_dir = Path(os.path.dirname(os.path.abspath(sys.argv[0]))).resolve()
    yaml_path = script_dir / "metadata.yaml"
      # Check if the metadata file exists
    if not yaml_path.exists():
        logger.warning(f"metadata.yaml not found at {yaml_path}")
        return {}
    else:
        logger.info(f"Found metadata.yaml at {yaml_path}")
    
    # Read the YAML file content with UTF-8 encoding
    # This ensures proper handling of special characters and symbols
    with open(yaml_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Split the content into separate YAML documents (delimited by ---)
    # This allows multiple templates to be defined in a single file
    yaml_docs = content.split('---\n')
    logger.info(f"Found {len(yaml_docs)} YAML documents in metadata.yaml")
      # Parse each document and build the templates dictionary
    templates = {}
    for doc in yaml_docs:
        # Skip empty documents (can occur due to leading/trailing delimiters)
        if not doc.strip():
            continue
        
        try:
            # Parse the YAML document using PyYAML's safe parser
            # This prevents code execution vulnerabilities
            metadata = yaml.safe_load(doc)
              # Validate document structure and required fields
            # Templates must be dictionaries and include the template-type field
            if not isinstance(metadata, dict):
                logger.warning(f"Skipping non-dictionary YAML document")
                continue
                
            if 'template-type' not in metadata:
                logger.warning(f"Skipping template without template-type field")
                continue
            
            # Extract template type and store the complete template
            template_type = metadata['template-type']
            templates[template_type] = metadata
            logger.debug(f"Loaded template: {template_type}")
            
        except yaml.YAMLError as e:
            # Handle YAML parsing errors with detailed diagnostics
            logger.error(f"Error parsing YAML document: {e}")
            failed_logger.error(f"YAML parsing failure in metadata file: {e}")
        except Exception as e:
            # Catch any other unexpected errors
            logger.error(f"Unexpected error processing template: {e}")
            failed_logger.error(f"Unexpected error processing template: {e}")
    
    # Report final template count
    logger.info(f"Total templates loaded: {len(templates)}")
    return templates

# Load the metadata templates at module import time
# This is done once when the module is imported to ensure templates
# are available throughout the system and avoid repeated file access
METADATA_TEMPLATES = load_metadata_templates()

# Icons for different content types
# These provide visual differentiation in the generated navigation
# and enhance the readability of index files
ICONS = {
    "readings": "📄",    # Document icon for readings
    "videos": "🎬",      # Film/movie icon for videos
    "transcripts": "📓",  # Notebook icon for transcripts
    "notes": "🗋",        # Note page icon for notes
    "quizzes": "🎓",      # Graduate cap for quizzes
    "assignments": "📋",  # Clipboard icon for assignments
    "folder": "📁"       # Folder icon for directories
}

# Order for displaying content categories
# This ensures consistent organization across all index files
# with the most important content types appearing first
ORDER = ["readings", "videos", "transcripts", "notes", "quizzes", "assignments"]

def make_friendly_link_title(name):
    """
    Transform filenames and folder names into clean, readable link titles.
      This function implements an intelligent title cleaning algorithm specifically designed 
    for academic content. It removes leading numeric prefixes, standardizes formatting,
    and properly capitalizes titles while preserving academic terminology integrity
    and any numbers that appear after words.
    
    The process follows these steps:
    1. Remove leading numeric prefixes commonly used in course materials
    2. Replace separators (dashes, underscores) with spaces
    3. Remove common structural terms that don't add meaning ('lesson', 'module', etc.)
    4. Clean up unnecessary spaces and standardize capitalization
    5. Apply special handling for academic acronyms and Roman numerals
    6. Preserve numbers that appear after words in the title
    
    Args:
        name (str): The original filename or folder name to transform
        
    Returns:
        str: A clean, human-readable title suitable for display in navigation
          Examples:
        >>> make_friendly_link_title("01_Introduction_to_Finance")
        "Introduction to Finance"
        
        >>> make_friendly_link_title("module-4-ROI-Analysis-part-ii")
        "ROI Analysis 4 Part II"
        
        >>> make_friendly_link_title("1 1 Introduction Concept Overview")
        "Introduction Concept Overview"
    """    # Remove leading numbers and separators (including multiple number groups at the beginning)
    name = re.sub(r'^(\d+\s*)+', '', name, flags=re.IGNORECASE)
    
    # Replace dashes/underscores with spaces
    name = re.sub(r'[_\-]+', ' ', name)
      # Remove the common structural words only
    words_to_remove = [
        r'\blesson\b', r'\blessons\b', r'\bmodule\b', r'\bmodules\b', 
        r'\bcourse\b', r'\bcourses\b', 
        r'\band\b', r'\bto\b', r'\bof\b'  # Common connecting words
        # Removed pattern that was removing all numbers in the text
    ]
    pattern = '|'.join(words_to_remove)
    name = re.sub(pattern, ' ', name, flags=re.IGNORECASE)
    
    # Remove any extra spaces
    name = re.sub(r'\s+', ' ', name).strip()
    
    # If title becomes too short (e.g., just "Overview" was removed), use a fallback
    if len(name.strip()) < 3:
        return "Content"
    
    # Convert to title case
    title_cased = name.title()
    
    # Fix Roman numerals: replace "Ii" with "II" (for cases like "part ii")
    title_cased = re.sub(r'\bIi\b', 'II', title_cased)
    
    # Fix known acronyms that should be all caps
    known_acronyms = ['CVP', 'ROI', 'KPI', 'MBA', 'CEO', 'CFO', 'COO', 'CTO', 'CIO', 'CMO']
    for acronym in known_acronyms:
        # Case-insensitive replacement to handle variations
        pattern = re.compile(r'\b' + acronym + r'\b', re.IGNORECASE)
        title_cased = pattern.sub(acronym, title_cased)
    
    # Handle special case for lowercase "ii" as Roman numeral "II"
    title_cased = re.sub(r'\b([A-Za-z]+) Ii\b', r'\1 II', title_cased)
    
    return title_cased

# ======= FILE CONVERSION SYSTEM ========
"""
The File Conversion System transforms various source materials into a standardized
knowledge base format. It handles cleaning filenames, converting file formats,
respecting user customizations, and organizing content into a cohesive structure.

Core Capabilities:
1. File Format Transformation
   - HTML → Markdown with semantic structure preservation
   - Plain text → Markdown with proper metadata
   - PDF/video → Cleaned filenames for consistency

2. Content Protection
   - Readonly detection for user-customized files
   - Non-destructive processing for critical content

3. Batch Processing
   - Recursive directory traversal
   - Multi-format handling in a single pass

Functions in this section work together as a pipeline, with process_directory_conversion
as the main entry point that orchestrates the entire conversion process.
"""

def clean_filename(name):
    """
    Clean up filenames by removing numbering prefixes and improving readability.
    
    This function standardizes filenames by removing common organizational prefixes
    like numbering schemes (e.g., "01_", "1-", "1."), and converting separators to spaces.
    The result is a more human-friendly filename that maintains the essential content
    while removing structural artifacts from the original naming scheme.
    
    Args:
        name (str): The original filename to clean
        
    Returns:
        str: The cleaned filename with improved readability
        
    Examples:
        >>> clean_filename("01_Introduction_to_Finance.pdf")
        "Introduction to Finance.pdf"
        
        >>> clean_filename("1-2-Advanced-Concepts.html")
        "Advanced Concepts.html"
    """
    name = re.sub(r'^[0-9]+([_\-\s]+)', '', name)
    name = re.sub(r'^([0-9]+[_\-\s]+)+', '', name)
    name = re.sub(r'[_\-]+', ' ', name)
    name = re.sub(r'\s+', ' ', name).strip()
    return name

def check_file_writable(file_path):
    """
    Determine if a file can be safely written to based on existence and metadata flags.
    
    This function implements a content-aware file protection system that respects
    user customizations. It checks if a file exists, and if so, examines its frontmatter
    for the 'auto-generated-state: readonly' flag which indicates user-modified content
    that should not be overwritten by automated processes.
    
    The function follows these steps:
    1. Check if the file exists (if not, it's writable)
    2. If it exists, read the file content
    3. Look for YAML frontmatter section
    4. Check for the readonly flag in the frontmatter
    
    Args:
        file_path (Path): The pathlib Path object pointing to the file to check
        
    Returns:
        bool: True if the file can be written to, False if it should be preserved
        
    Note:
        This function is crucial for preserving user customizations in generated files.
        Files marked as readonly will be skipped during index generation and conversion
        processes to prevent overwriting manual edits.
    """
    if not file_path.exists():
        return True  # File doesn't exist, so it's writable
        
    # File exists, check its auto-generated-state
    with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
          # Extract frontmatter if it exists
    frontmatter_match = re.search(r'---\s(.*?)\s---', content, re.DOTALL)
    if frontmatter_match:
        frontmatter = frontmatter_match.group(1)
        # Check if auto-generated-state is readonly
        if re.search(r'auto-generated-state\s*:\s*readonly', frontmatter, re.IGNORECASE):
            logger.info(f"Skipping readonly file: {file_path}")
            return False
    
    return True  # File is writable or doesn't have the readonly property

def add_frontmatter(content, template_type="reading", filename=""):
    """
    Add standardized YAML frontmatter to markdown content with intelligent title extraction.
    
    This function enhances markdown content by adding structured YAML frontmatter that
    includes template type classification, title derived from the filename, and default
    state management flags. It also inserts an H1 heading with the extracted title to
    ensure consistent document structure.
    
    Args:
        content (str): The original markdown content to enhance
        template_type (str): Classification of the content (reading, transcript, etc.)
                            Used for template-based formatting in Obsidian
        filename (str): Original filename to extract title from, if available
        
    Returns:
        str: The enhanced markdown content with frontmatter and title heading
        
    Note:
        The 'auto-generated-state: writable' flag indicates that the file can be
        updated by future runs of the generator. Users can change this to 'readonly'
        to protect their customizations.
    """
    # Extract title from filename, removing extension and formatting nicely
    title = os.path.splitext(filename)[0].replace("-", " ").title() if filename else ""
    frontmatter = f"---\nauto-generated-state: writable\ntemplate-type: {template_type}\ntitle: {title}\n---\n\n"
    # Add a title heading at the top of the content if we have a title
    if title:
        return frontmatter + f"# {title}\n\n" + content
    return frontmatter + content

def convert_html_to_md(html_path, md_path):
    """
    Convert HTML files to Markdown format with proper frontmatter and structure.
    
    This function transforms HTML content into well-formatted Markdown, preserving
    the essential content structure while making it compatible with the Obsidian
    knowledge base. It adds appropriate frontmatter, handles encoding issues, and
    respects readonly status of existing files.
    
    The process follows these steps:
    1. Check if the target file is writable (not marked readonly)
    2. Read the HTML content with UTF-8 encoding
    3. Convert HTML to Markdown using html2text
    4. Add standardized frontmatter and title heading
    5. Write the converted content to the destination file
    
    Args:
        html_path (Path): Path to the source HTML file
        md_path (Path): Path where the Markdown file should be written
        
    Returns:
        bool: True if conversion was successful, False if skipped (readonly)
    """
    # Check if we can write to the file first
    if not check_file_writable(md_path):
        return False
        
    with open(html_path, 'r', encoding='utf-8') as html_file:
        html_content = html_file.read()
    
    markdown = html2text.html2text(html_content)
    # Pass the filename to add_frontmatter for title generation
    markdown_with_frontmatter = add_frontmatter(markdown, "reading", md_path.name)
    
    with open(md_path, 'w', encoding='utf-8') as md_file:
        md_file.write(markdown_with_frontmatter)
    
    return True

def convert_txt_to_md(txt_path, md_path):
    """
    Convert plain text files to Markdown format with appropriate frontmatter.
    
    This function enhances plain text content (typically transcripts) by converting
    it to Markdown format with standardized frontmatter. It preserves the original
    content while adding the necessary structure to integrate it into the knowledge base.
    
    The process follows these steps:
    1. Check if the target file is writable (not marked readonly)
    2. Read the text content with UTF-8 encoding
    3. Add standardized frontmatter with transcript template type
    4. Add title heading derived from the filename
    5. Write the enhanced content to the destination file
    
    Args:
        txt_path (Path): Path to the source text file
        md_path (Path): Path where the Markdown file should be written
        
    Returns:
        bool: True if conversion was successful, False if skipped (readonly)
    """
    # Check if we can write to the file first
    if not check_file_writable(md_path):
        return False
        
    with open(txt_path, 'r', encoding='utf-8') as txt_file:
        text = txt_file.read()
    
    # Pass the filename to add_frontmatter for title generation
    text_with_frontmatter = add_frontmatter(text, "transcript", md_path.name)
    
    with open(md_path, 'w', encoding='utf-8') as md_file:
        md_file.write(text_with_frontmatter)
    
    return True

def process_directory_conversion(course_path):
    """
    Recursively process all files within a directory tree for format conversion and cleanup.
    
    This function implements a comprehensive file processing pipeline that transforms
    various source files into standardized formats suitable for the knowledge base.
    It performs the following operations in a single recursive pass:
    
    1. Format Conversion:
       - HTML files → Markdown with proper formatting
       - Text transcripts → Markdown with enhanced structure
       
    2. File Management:
       - Deletes auxiliary files (.en.srt subtitles)
       - Renames media files (PDF, MP4) to remove numbering prefixes
       - Intelligently generates better filenames
       
    3. Content Protection:
       - Respects readonly flags to preserve user customizations
       - Preserves special system directories
       
    The function recursively walks the directory tree, processing all compatible files
    while maintaining the original directory structure. Each file is processed according
    to its type, with appropriate conversions applied.
    
    Args:
        course_path (Path or str): The root path to begin recursive processing from.
                                  Can be a Path object or string path.
                                  
    Side Effects:
        - Deletes .en.srt files
        - Converts HTML and TXT files to Markdown (original files deleted after conversion)
        - Renames PDF and video files with cleaner names
        - Leaves system directories and hidden files untouched
        
    Returns:
        None: Results are reported via console output
    """
    for root, dirs, files in os.walk(course_path):
        # Skip the Templater folder and folders that begin with a dot
        # This preserves system directories and template files
        dirs[:] = [d for d in dirs if d != "Templater" and not d.startswith('.')]
        
        for file in files:
            # Create full path to the current file
            path = Path(root) / file

            # PHASE 1: SUBTITLE FILE CLEANUP
            # Delete subtitle files (.en.srt) as they're not needed in the knowledge base
            # These are typically auto-generated subtitle files from video platforms
            if path.name.endswith(".en.srt"):
                print(f"🗑️ Deleting SRT file: {path}")
                path.unlink()  # Delete the file
                continue  # Skip to the next file

            # Get the file extension in lowercase for consistent handling
            ext = path.suffix.lower()

            # Skip file types we don't need to process
            # We only handle HTML, TXT, PDF and MP4 files
            if ext not in ['.html', '.txt', '.pdf', '.mp4']:
                continue

            # Extract the original filename without extension
            original_name = path.stem
            
            # Clean the filename by removing numbering prefixes and improving readability
            cleaned_name = clean_filename(original_name)

            # PHASE 2: TRANSCRIPT CONVERSION
            # Special handling for transcript text files (ending with .en.txt)
            if ext == ".txt" and path.name.endswith(".en.txt"):
                # Create an improved filename by removing .en suffix and adding "Transcript"
                cleaned_name = clean_filename(path.stem.replace('.en', '')) + " Transcript"
                md_path = path.with_name(cleaned_name + ".md")
                
                # Convert the transcript text to markdown with proper formatting
                if convert_txt_to_md(path, md_path):
                    path.unlink()  # Delete the original after successful conversion
                    print(f"✅ Converted TXT → {md_path}")
                continue  # Skip to next file after handling transcript

            # PHASE 3: HTML CONVERSION
            # Convert HTML files to Markdown with proper structure
            if ext == ".html":
                md_path = path.with_name(cleaned_name + ".md")
                if convert_html_to_md(path, md_path):
                    path.unlink()  # Delete the original after successful conversion
                    print(f"✅ Converted HTML → {md_path}")
                continue  # Skip to next file after handling HTML

            # PHASE 4: MEDIA FILE RENAMING
            # Rename PDF and video files to have cleaner names (without renumbering)
            if ext in ['.mp4', '.pdf']:
                new_path = path.with_name(cleaned_name + path.suffix)
                # Only rename if the name would actually change and the file exists
                if new_path != path and path.exists():
                    try:
                        # Move (rename) the file to the new path
                        shutil.move(str(path), str(new_path))
                        print(f"📁 Renamed: {path.name} → {new_path.name}")
                    except Exception as e:
                        # Handle errors during renaming (e.g., permissions, file locks)
                        print(f"❌ Error renaming {path}: {e}")

# ======= INDEX GENERATION FUNCTIONS ========

def get_index_type(depth, folder_name=None):
    """
    Determine the appropriate index type based on directory depth and folder name.
    
    This function implements a context-aware classification system for index files
    that considers both the hierarchical depth and semantic folder naming. It maps
    directory depth to standard index types while providing special handling for
    semantically significant folders like live sessions, case studies, readings, and resources.
    
    The hierarchical mapping follows this pattern:
    - Depth 0: main-index (Root of the knowledge base)
    - Depth 1: program-index (MBA program level)
    - Depth 2: course-index (Individual courses)
    - Depth 3: class-index (Class sessions)
    - Depth 4: module-index (Module within classes)
    - Depth 5+: lesson-index (Specific lessons)
    
    Special semantic overrides:
    - "live session" in folder name: live-session-index
    - "case" or "case-study" in folder name: case-study-index
    - "reading" or "required reading" in folder name: readings-index
    - "resource" or "resources" in folder name: resources-index
    
    Args:
        depth (int): The hierarchical depth of the folder
        folder_name (str, optional): The name of the folder to check for special cases
        
    Returns:
        str: The appropriate index type identifier
    """
    index_types = ["main-index", "program-index", "course-index", "class-index", "module-index", "lesson-index"]
    
    # Check folder name for special cases (if folder_name is provided)
    if folder_name:
        folder_name_lower = folder_name.lower()
        
        # Special case for Live Session folders
        if "live session" in folder_name_lower:
            return "live-session-index"
        
        # Special case for Case Study folders
        if "case" in folder_name_lower or "case-study" in folder_name_lower:
            return "case-studies-index"
        
        # Special case for Required Readings folders
        if "reading" in folder_name_lower or "required reading" in folder_name_lower:
            return "readings-index"
        
        # Special case for Resources folders
        if "resource" in folder_name_lower or "resources" in folder_name_lower:
            return "resources-index"
    
    # Default to depth-based index type
    return index_types[depth] if depth < len(index_types) else "lesson-index"

def is_hidden(p):
    """
    Determine if a path should be excluded from processing based on visibility rules.
    
    This function identifies paths that should be excluded from the automation process
    by checking for hidden status (starting with a dot) or special system folders
    like the Templater folder used for Obsidian templates.
    
    Args:
        p (Path): The path to evaluate
        
    Returns:
        bool: True if the path should be excluded, False if it should be processed
        
    Example:
        >>> is_hidden(Path(".obsidian/config"))
        True
        >>> is_hidden(Path("Templater/class-template.md"))
        True
        >>> is_hidden(Path("Finance/Module 1/notes.md"))
        False
    """
    return any(part.startswith(".") for part in p.parts) or any(part == "Templater" for part in p.parts)

def should_include(path):
    """
    Determine if a path should be included in index generation based on inclusion rules.
    
    This function evaluates paths against inclusion criteria, filtering out system files,
    hidden files (starting with a dot), special index files, and template directories.
    Only paths that pass these criteria will be included in generated index files.
    
    Args:
        path (Path): The path to evaluate for inclusion
        
    Returns:
        bool: True if the path should be included in indexes, False if it should be excluded
        
    Note:
        This is different from is_hidden() as it specifically focuses on index inclusion
        rather than general visibility in the system. Some files may not be hidden
        but still should be excluded from indexes (like _index.md files).
    """
    return not path.name.startswith(".") and path.name != "_index.md" and path.name != "Templater"

def classify_file(file):
    """
    Classify a file into a content category based on extension and name patterns.
    
    This function implements an intelligent content classification system that 
    analyzes file attributes to determine the appropriate category. It uses both
    file extensions and filename patterns to make accurate classifications,
    supporting the automatic organization of diverse academic materials.
    
    Classification Logic:
    1. First checks for index files (excluded from classification)
    2. Identifies video files by common video extensions
    3. Categorizes markdown files by purpose (transcript, notes, readings)
    4. Classifies PDFs as readings
    5. Other file types may be excluded or given a default category
    
    Categories:
    - videos: Video content (mp4, mov, etc.)
    - transcripts: Text transcriptions of videos
    - notes: User-created notes and summaries
    - readings: Articles, papers, and reading materials
    
    Args:
        file (Path): The file to classify
        
    Returns:
        str or None: The content category, or None if the file should be excluded
    """
    fname = file.name.lower()
    
    # Skip any index files or files that should not be listed in indexes
    if "index" in fname.lower() or file.name == "Live-Session Index.md":
        return None
        
    # Check for video files first
    if file.suffix in ['.mp4', '.mov', '.avi', '.mkv', '.webm']:
        return "videos"
    # Then check markdown files
    elif file.suffix == ".md":
        if "transcript" in fname:
            return "transcripts"
        elif "note" in fname:
            return "notes"
        else:
            return "readings"
    # PDF files can be classified as readings
    elif file.suffix == '.pdf':
        return "readings"
    # For any other file types
    return None

def infer_course_and_program(folder, index_type):
    """
    Intelligently infer program and course names from folder path and hierarchy level.
    
    This function implements a sophisticated path analysis algorithm that uses the
    hierarchical structure and index type to determine the appropriate program and
    course names for a given folder. It uses positional heuristics based on the
    expected knowledge base structure to make these determinations.
    
    Inference Logic:
    - For lesson, module, class, case-study indexes: program is -4, course is -3
    - For course index: program is -3, course is -2
    - For program index: program is -2
    - For main index: both program and course are empty
    
    Args:
        folder (Path): The folder path to analyze
        index_type (str): The type of index being generated
        
    Returns:
        tuple: (program, course) - Inferred program and course names as strings
    """
    # Default values
    program = ""
    course = ""
    # Get all parts of the folder path
    parts = list(folder.parts)
    # Heuristic: 
    # - For lesson-index, module-index, class-index: program is -4, course is -3
    # - For course-index: program is -3, course is -2
    # - For program-index: program is -2
    # - For main-index: both empty
    if index_type in ("lesson-index", "module-index", "class-index", "case-study-index", "case-studies-index"):
        if len(parts) >= 4:
            program = parts[-4]
            course = parts[-3]
    elif index_type == "course-index":
        if len(parts) >= 3:
            program = parts[-3]
            course = parts[-2]
    elif index_type == "program-index":
        if len(parts) >= 2:
            program = parts[-2]
    # Clean up names
    program = program.replace('-', ' ').title() if program else ""
    course = course.replace('-', ' ').title() if course else ""
    return program, course

def get_tags_for_file(file, file_type, index_type=None):
    """
    Generate a comprehensive set of semantic tags for a file based on multiple dimensions.
    
    This function implements a multi-dimensional tagging system that categorizes content
    based on several factors:
    
    1. Structural Tags (🧩): Identify the fundamental type or purpose of content
       - #reading, #video, #transcript, #notes, #quiz, #assignment
       - For index files: #index, #course, #module, #lesson
       
    2. Cognitive Tags (🧠): Indicate the mental framework or learning approach
       - #lecture, #case-study, #summary, #exercise
       
    The tagging system creates a rich metadata layer that enables sophisticated
    filtering, searching, and organization within the knowledge base.
    
    Args:
        file (Path): The file to generate tags for
        file_type (str): The content category (readings, videos, etc.)
        index_type (str, optional): The index type if this is an index file
        
    Returns:
        list: A list of tag strings appropriate for the file
    """
    tags = []
    
    # 🧩 Structural Tags (by file type or purpose)
    if file_type == "readings":
        tags.append("#reading")
    elif file_type == "videos":
        tags.append("#video")
    elif file_type == "transcripts":
        tags.append("#transcript")
    elif file_type == "notes":
        tags.append("#notes")
    elif file_type == "quizzes":
        tags.append("#quiz")
    elif file_type == "assignments":
        tags.append("#assignment")
    
    # For index files
    if index_type:
        tags.append("#index")
        if "course" in index_type:
            tags.append("#course")
        elif "module" in index_type:
            tags.append("#module")  
        elif "lesson" in index_type:
            tags.append("#lesson")
      # 🧠 Cognitive Tags (inferred from filename)
    fname = file.name.lower()
    if "lecture" in fname:
        tags.append("#lecture")
    if "case" in fname or "case-study" in fname:
        tags.append("#case-study")
    if "summary" in fname:
        tags.append("#summary")
    if "exercise" in fname:
        tags.append("#exercise")
    
    # No workflow tags - removing #to-review tag entirely
    
    return tags

def build_backlink(depth, folder):
    """
    Build a sophisticated navigation header with contextual back links and global shortcuts.
    
    This function creates a comprehensive navigation header that provides both
    hierarchical navigation (back to parent) and global shortcuts to key destinations.
    The navigation adapts based on the hierarchical depth, providing appropriate
    back links with descriptive text that reflects the organizational structure.
    
    Navigation Components:
    1. Context-aware back link to parent index with appropriate label
    2. Global shortcut to Home (root of MBA knowledge base)
    3. Global shortcut to Dashboard (overview and analytics)
    4. Global shortcut to Assignments tracking
    
    The navigation is formatted as a level 6 header (######) to maintain visual
    consistency while minimizing impact on the document structure.
    
    Args:
        depth (int): The hierarchical depth of the current folder
        folder (Path): The current folder path
        
    Returns:
        str: A formatted navigation header with back link and global shortcuts
    """
    # Start with a navigation section header
    nav = "###### "
    
    # Add the back link based on depth
    if depth > 0:
        parent_folder = folder.parent
        parent_index_filename = f"{parent_folder.name}.md"
        parent_index_filename_escaped = urllib.parse.quote(parent_index_filename)
        
        back_text = ""
        if depth == 5:  # Lesson level - link back to module
            back_text = f"⬅️ [Back to Module Index](../{parent_index_filename_escaped})"
        elif depth == 4:  # Module level - link back to class
            back_text = f"⬅️ [Back to Class Index](../{parent_index_filename_escaped})"
        elif depth == 3:  # Class level - link back to course
            back_text = f"⬅️ [Back to Course Index](../{parent_index_filename_escaped})"
        elif depth == 2:  # Course level - link back to program
            back_text = f"⬅️ [Back to Program Index](../{parent_index_filename_escaped})"
        elif depth == 1:  # Program level - link back to main
            back_text = f"⬅️ [Back to Main Index](../{parent_index_filename_escaped})"
            
        nav += back_text + " | "
    else:
        nav += ""  # No back link for main index
    
    # Add the common notebook links
    nav += "🏫 [Home](/01_Projects/MBA/MBA.md) | "
    nav += "📈 [Dashboard](/01_Projects/MBA/MBA-Dashboard.md) | "
    nav += "📅 [Classes Assignments](/01_Projects/MBA/MBA-Classes-Assignments.md)\n"
    
    return nav

def generate_breadcrumbs_yaml(folder, depth):
    """
    Generate structured YAML breadcrumbs for bidirectional navigation.
    
    This function creates a YAML breadcrumbs structure that enables bidirectional
    navigation in the Obsidian knowledge base. It defines both upward (parent) and
    downward (children) relationships using properly formatted paths. The breadcrumbs
    use the original folder/file names to ensure consistent linking.
    
    The breadcrumbs follow this structure:
    ```yaml
    breadcrumbs:
      up: "../parent-folder.md"
      down:
      - "child-folder-1/child-folder-1.md"
      - "child-folder-2/child-folder-2.md"
    ```
    
    All path values are double-quoted to ensure proper YAML parsing, especially
    for paths containing special characters.
    
    Args:
        folder (Path): The current folder to generate breadcrumbs for
        depth (int): The hierarchical depth of the folder
        
    Returns:
        str: Formatted YAML breadcrumbs string ready to be included in frontmatter
    """
    up = None
    down = []
    # Up: one level up index
    if depth > 0:
        parent_folder = folder.parent
        parent_index = parent_folder.name + ".md"
        up = f"../{parent_index}"
    # Down: subfolder indexes (use original names)
    for item in sorted(folder.iterdir()):
        if item.is_dir() and should_include(item):
            child_index = item.name + ".md"
            down.append(f'{item.name}/{child_index}')
    # Format YAML with double quotes
    yaml_lines = ["breadcrumbs:"]
    if up:
        yaml_lines.append(f'  up: "{up}"')
    if down:
        yaml_lines.append("  down:")
        for c in down:
            yaml_lines.append(f'  - "{c}"')
    return "\n".join(yaml_lines)

def generate_metadata_from_template(index_type, folder, depth):
    """
    Generate comprehensive metadata frontmatter for index files with context awareness.
    
    This function creates the YAML frontmatter for index files by either using a predefined
    template from the metadata system or dynamically generating appropriate metadata
    when no template exists. It handles context-specific metadata fields including
    breadcrumbs, program/course relationships, completion tracking, and visual elements.
    
    The function performs several key operations:
    1. Infers program and course context from folder structure
    2. Determines which metadata fields are appropriate for the index type
    3. Applies templates from the METADATA_TEMPLATES system when available
    4. Dynamically generates frontmatter when no template exists
    5. Adds navigation breadcrumbs for bidirectional linking
    6. Includes visual elements like banners with proper positioning
    
    Args:
        index_type (str): The type of index being generated (e.g., "main-index",
                         "course-index", "lesson-index", etc.)
        folder (Path): The folder path for which the index is being generated
        depth (int): The hierarchical depth of the folder in the knowledge base
        
    Returns:
        str: A complete metadata block including YAML frontmatter and title heading,
             ready to be written to the index file
             
    Note:
        This function integrates with the template system defined in metadata.yaml
        but also provides fallback generation when templates are unavailable, ensuring
        consistent metadata even when templates are incomplete.
    """
    # Infer program and course names from folder structure and hierarchy
    program, course = infer_course_and_program(folder, index_type)
    
    # Determine which fields should be included based on index type
    # Program and course fields only make sense in deeper levels of hierarchy
    add_program_course = index_type not in ("main-index", "program-index")
    
    # Completion tracking fields only make sense at the lesson level
    add_completion_fields = index_type == "lesson-index"
    
    # FALLBACK TEMPLATE GENERATION
    # If no template exists for this index type, generate one dynamically
    if index_type not in METADATA_TEMPLATES:
        # Create a friendly title from the folder name
        title = make_friendly_link_title(folder.name)
        
        # Generate bidirectional navigation breadcrumbs
        breadcrumbs_yaml = generate_breadcrumbs_yaml(folder, depth)
        
        # Conditionally add program/course fields based on hierarchy position
        extra_fields = (
            (f"program: {program}\n" if add_program_course else "") +
            (f"course: {course}\n" if add_program_course else "") +
            (f"completion-date: \nreview-date: \ncomprehension: \n" if add_completion_fields else "")
        )
        
        # Add banner fields for visual enhancement
        banner_fields = "banner: \"![[gies-banner.png]]\"\nbanner_x: 0.25\n"
        
        # Construct the complete YAML frontmatter and title heading
        return (
            f"---\n"
            f"auto-generated-state: writable\n"
            f"template-type: {index_type}\n"
            f"title: {title}\n"
            f"{extra_fields}"
            f"{breadcrumbs_yaml}\n"
            f"{banner_fields}---\n\n"
            f"# {title}\n\n")
    
    # TEMPLATE-BASED GENERATION
    # When a matching template exists in the metadata system
    template = METADATA_TEMPLATES[index_type].copy()
    
    # Add creation date and title
    template['date-created'] = datetime.now().strftime('%Y-%m-%d')
    template['title'] = make_friendly_link_title(folder.name)
    
    # Handle program and course fields based on hierarchical context
    if add_program_course:
        # Only add program/course if not already set or empty in the template
        if 'program' not in template or not template['program']:
            template['program'] = program
        if 'course' not in template or not template['course']:
            template['course'] = course
    else:
        # Remove program/course fields for top-level indexes where they don't make sense
        template.pop('program', None)
        template.pop('course', None)
    
    # Handle completion tracking fields for lesson-level content
    if add_completion_fields:
        # Ensure completion tracking fields exist in the template
        if 'completion-date' not in template:
            template['completion-date'] = ""
        if 'review-date' not in template:
            template['review-date'] = ""
        if 'comprehension' not in template:
            template['comprehension'] = ""
    else:
        # Remove completion tracking fields for non-lesson indexes
        template.pop('completion-date', None)
        template.pop('review-date', None)
        template.pop('comprehension', None)
        
    # Convert template to YAML text with careful formatting options:
    # - sort_keys=False: preserve field order from template
    # - default_flow_style=False: use block style for readability
    # - allow_unicode=True: preserve special characters
    yaml_text = yaml.safe_dump(template, sort_keys=False, default_flow_style=False, allow_unicode=True)
    
    # Add navigation breadcrumbs for bidirectional linking
    breadcrumbs_yaml = generate_breadcrumbs_yaml(folder, depth)
    
    # Add visual elements (banner image with positioning)
    banner_fields = "banner: \"![[gies-banner.png]]\"\nbanner_x: 0.25\n"
    
    # Extract title for the markdown heading
    title = template['title']
    
    # Construct the complete frontmatter and title heading
    return (
        f"---\n"
        f"{yaml_text}{breadcrumbs_yaml}\n{banner_fields}---\n\n"
        f"# {title}\n\n")

def get_section_heading(index_type):
    """Return the appropriate section heading for the given index type."""
    headings = {
        "main-index": "Programs",
        "program-index": "Courses",
        "course-index": "Classes",
        "class-index": "Modules",
        "module-index": "Lessons",
        "lesson-index": "Content",
        "case-study-index": "Case Studies",
        "case-studies-index": "Case Studies",
        "readings-index": "Required Readings",
        "resources-index": "Resources"
    }
    return headings.get(index_type, "Subfolders")

def get_additional_sections(index_type):
    """
    Determine the appropriate content sections for different index types.
    
    This function implements a context-aware section generation system that adapts
    the content organization based on the hierarchical level. Different index types
    have different relevant content categories that should be displayed, and this
    function ensures consistent organization across the knowledge base.
    
    Section rules:
    - Class indexes include a "Readings" section for course materials
    - Lesson indexes use a comprehensive structure with "Readings", combined 
      "Videos & Transcripts" section, and "Notes"
    - Other index types don't include additional specialized content sections
    
    Args:
        index_type (str): The type of index being generated (e.g., "class-index", 
                          "lesson-index", "module-index", etc.)
        
    Returns:
        list: A list of section heading strings appropriate for the index type.
              Returns an empty list for index types that don't need additional sections.
              
    Example:
        >>> get_additional_sections("lesson-index")
        ["Readings", "Videos & Transcripts", "Notes"]
        
        >>> get_additional_sections("program-index")
        []
    """
    if index_type == "class-index":
        return ["Readings"]
    if index_type == "lesson-index":
        return ["Readings", "Videos & Transcripts", "Notes"]
    return []

def generate_navigation_section():
    """This function now returns empty string as navigation is handled by build_backlink."""
    return ""

def generate_indexes(root_path, debug=False, index_type_filter=None):
    """
    Generate a comprehensive hierarchical index structure for the knowledge base.
    
    This function is the core engine for creating the navigational structure of the
    MBA Notebook Automation system. It walks through the directory tree, analyzing
    the structure and content to create appropriate index files at each level. The
    indexes include proper navigation, categorized content listings, metadata, and
    formatting appropriate for the hierarchical level.
    
    The function handles special cases for different levels of the hierarchy:
    - main-index: Lists all program indexes
    - program-index: Lists all courses for a program
    - course-index: Organizes classes, live sessions, and case studies
    - class-index: Lists modules and content within the class
    - module-index: Organizes lessons and live sessions
    - lesson-index: Categorizes all content within a lesson
    - case-study-index: Special format for case study collections
    - live-session-index: Special format for live session materials
    
    Processing Logic:
    1. Recursively scans the directory tree starting from root_path
    2. For each folder, determines the appropriate index type based on depth and name
    3. Filters contents to include only relevant files and directories
    4. Groups content by type (readings, videos, folders, etc.)
    5. Generates appropriate YAML frontmatter with metadata templates
    6. Creates organized sections with proper headings and formatting
    7. Adds navigation links for hierarchical browsing
    8. Writes the complete index file unless it's marked as readonly
    
    Args:
        root_path (Path or str): The root path of the knowledge base to index
        debug (bool): Whether to print debug information during processing.
                    Useful for troubleshooting and development.
        index_type_filter (list or str, optional): Filter to generate only specific 
                                                 index types (comma-separated string or list)
                                              
    Returns:
        tuple: (generated_count, generated_types, elapsed_time)
               - generated_count: Total number of index files created
               - generated_types: Dictionary counting index files by type
               - elapsed_time: Time in seconds that it took to generate all indexes
               
    Example:
        ```python
        # Generate all index types
        count, types, elapsed = generate_indexes(Path("/path/to/vault"))
        print(f"Generated {count} indexes in {elapsed:.2f} seconds")
        
        # Generate only course and module indexes
        count, types, elapsed = generate_indexes(
            Path("/path/to/vault"), 
            index_type_filter="course-index,module-index"
        )
        ```
    """
    # Start timing the index generation process
    start_time = time.time()
    
    generated_count = 0
    generated_types = {}
    for dirpath, dirnames, filenames in os.walk(root_path):
        folder = Path(dirpath)
        if is_hidden(folder):
            continue
        try:
            depth = len(folder.relative_to(root_path).parts)
        except ValueError:
            depth = 0
        index_type = get_index_type(depth, folder.name)
        
        # Filter check - exit early if this index type should be skipped
        if index_type_filter and not index_type_filter(index_type):
            continue  # Skip this index type if not selected
            
        # Now we know this index passes the filter - debug output moved here
        # New index filename: folder name + .md
        index_filename = folder.name + ".md"
        index_path = folder / index_filename
            
        # Only write if writable
        if not check_file_writable(index_path):
            if debug:
                print(f"[DEBUG] Skipping readonly index: {index_path}")
            continue
        # Generate index content
        metadata = generate_metadata_from_template(index_type, folder, depth)
        content_sections = []
        subfolders = [f for f in sorted(folder.iterdir()) if f.is_dir() and should_include(f)]
        files = [f for f in sorted(folder.iterdir()) if f.is_file() and should_include(f)]
        # Section heading for subfolders
        section_heading = get_section_heading(index_type)
        # Special handling for main-index: only list Programs, title is 'MBA Program'
        if index_type == "main-index":
            metadata = generate_metadata_from_template(index_type, folder, depth)
            metadata = re.sub(r'(title: ).*', r'\1MBA Program', metadata)
            metadata = re.sub(r'# .*', r'# MBA Program', metadata, count=1)
            content_sections = []
            if subfolders:
                content_sections.append("## Programs\n")
                for sub in subfolders:
                    sub_index_filename = f'{sub.name}.md'
                    if sub_index_filename == index_filename:
                        continue
                    icon = ICONS.get("folder", "📁")
                    link_path = f'{urllib.parse.quote(sub.name)}.md'
                    friendly_title = make_friendly_link_title(sub.name)
                    content_sections.append(f'- {icon} [{friendly_title}]({link_path})\n')
            with open(index_path, "w", encoding="utf-8") as f:
                f.write(metadata)
                f.write("\n".join(content_sections))
                f.write(generate_navigation_section())
            if debug:
                print(f"[DEBUG] Wrote main index: {index_path}")
            continue
        # Special handling for module-index: group subfolders
        if index_type == "module-index":
            live_sessions = [sub for sub in subfolders if "live session" in sub.name.lower()]
            lessons = [sub for sub in subfolders if "live session" not in sub.name.lower()]
            if live_sessions:
                content_sections.append("## Live Session\n")
                for sub in live_sessions:
                    icon = ICONS.get("folder", "📁")
                    link_path = f'{urllib.parse.quote(sub.name)}.md'
                    friendly_title = make_friendly_link_title(sub.name)
                    content_sections.append(f'- {icon} [{friendly_title}]({link_path})\n')
            if lessons:
                content_sections.append("## Lessons\n")
                for sub in lessons:
                    icon = ICONS.get("folder", "📁")
                    link_path = f'{urllib.parse.quote(sub.name)}.md'
                    friendly_title = make_friendly_link_title(sub.name)
                    content_sections.append(f'- {icon} [{friendly_title}]({link_path})\n')        
            elif index_type == "case-study-index" or index_type == "case-studies-index":
                # YAML frontmatter: use template, add program, course, index-type
                template = METADATA_TEMPLATES.get("case-studies-index", {}).copy()
                template['date-created'] = datetime.now().strftime('%Y-%m-%d')
                template['title'] = "Case Studies"
                # Try to infer program and course from path
                program = folder.parent.parent.name if folder.parent.parent else ""
                course = folder.parent.name if folder.parent else ""
                template['program'] = program.replace('-', ' ').title()
                template['course'] = course.replace('-', ' ').title()
                template['index-type'] = "case-studies"
                yaml_text = yaml.safe_dump(template, sort_keys=False, default_flow_style=False, allow_unicode=True)
                metadata = f"---\n{yaml_text}---\n\n# {template['course']} - Case Studies\n\n"
            # Markdown body: list all .md files as case studies            
            case_study_files = [f for f in sorted(folder.iterdir()) if f.is_file() and f.suffix == '.md' and 'index' not in f.name.lower() and f.name != index_filename]
            if case_study_files:
                content_sections.append("## Case Studies\n")
                for f in case_study_files:
                    display = make_friendly_link_title(f.stem)
                    link = urllib.parse.quote(f.name)
                    icon = ICONS.get("readings", "📄")
                    content_sections.append(f'- {icon} [{display}]({link})\n')
                    
            # Add navigation backlink
            backlink = build_backlink(depth, folder)
            if backlink:
                content_sections.insert(0, backlink + "\n")
                
            # Write file
            with open(index_path, "w", encoding="utf-8") as f:
                f.write(metadata)
                f.write("\n".join(content_sections))
                f.write(generate_navigation_section())
            if debug:
                print(f"[DEBUG] Wrote case-studies index: {index_path}")
            continue
        elif index_type == "readings-index":
            # YAML frontmatter: use template, add program, course, index-type
            template = METADATA_TEMPLATES.get("readings-index", {}).copy()
            template['date-created'] = datetime.now().strftime('%Y-%m-%d')
            template['title'] = "Required Readings"
            # Try to infer program and course from path
            program = folder.parent.parent.name if folder.parent.parent else ""
            course = folder.parent.name if folder.parent else ""
            template['program'] = program.replace('-', ' ').title()
            template['course'] = course.replace('-', ' ').title()
            template['index-type'] = "readings"
            template['banner'] = "![[gies-banner.png]]"
            template['banner_x'] = 0.25
            # Generate breadcrumbs for navigation
            breadcrumbs_yaml = generate_breadcrumbs_yaml(folder, depth)
            yaml_text = yaml.safe_dump(template, sort_keys=False, default_flow_style=False, allow_unicode=True)
            metadata = f"---\n{yaml_text}{breadcrumbs_yaml}\n---\n\n# {template['course']} - Required Readings\n\n"
            # Markdown body: list all reading files
            reading_files = [f for f in sorted(folder.iterdir()) if f.is_file() and f.suffix == '.md' and 'index' not in f.name.lower() and f.name != index_filename]
            reading_files += [f for f in sorted(folder.iterdir()) if f.is_file() and f.suffix == '.pdf']
            if reading_files:
                content_sections.append("## Required Readings\n")
                for f in reading_files:
                    display = make_friendly_link_title(f.stem)
                    link = urllib.parse.quote(f.name)
                    icon = ICONS.get("readings", "📄")
                    content_sections.append(f'- {icon} [{display}]({link})\n')
            
            # Add navigation backlink
            backlink = build_backlink(depth, folder)
            if backlink:
                content_sections.insert(0, backlink + "\n")
                
            # Write file
            with open(index_path, "w", encoding="utf-8") as f:
                f.write(metadata)
                f.write("\n".join(content_sections))
                f.write(generate_navigation_section())
            if debug:
                print(f"[DEBUG] Wrote readings index: {index_path}")
            continue
        elif index_type == "resources-index":
            # YAML frontmatter: use template, add program, course, index-type
            template = METADATA_TEMPLATES.get("resources-index", {}).copy()
            template['date-created'] = datetime.now().strftime('%Y-%m-%d')
            template['title'] = "Resources"
            # Try to infer program and course from path
            program = folder.parent.parent.name if folder.parent.parent else ""
            course = folder.parent.name if folder.parent else ""
            template['program'] = program.replace('-', ' ').title()
            template['course'] = course.replace('-', ' ').title()
            template['index-type'] = "resources"
            template['banner'] = "![[gies-banner.png]]"
            template['banner_x'] = 0.25
            # Generate breadcrumbs for navigation
            breadcrumbs_yaml = generate_breadcrumbs_yaml(folder, depth)
            yaml_text = yaml.safe_dump(template, sort_keys=False, default_flow_style=False, allow_unicode=True)
            metadata = f"---\n{yaml_text}{breadcrumbs_yaml}\n---\n\n# {template['course']} - Resources\n\n"            # Markdown body: list all resource files
            resource_files = [f for f in sorted(folder.iterdir()) if f.is_file() and 'index' not in f.name.lower() and f.name != index_filename]
            if resource_files:
                content_sections.append("## Resources\n")
                for f in resource_files:
                    display = make_friendly_link_title(f.stem)
                    link = urllib.parse.quote(f.name)
                    # Choose an appropriate icon based on file type
                    if f.suffix == '.pdf':
                        icon = ICONS.get("readings", "📄")
                    elif f.suffix in ['.mp4', '.mov', '.avi']:
                        icon = ICONS.get("videos", "🎬")
                    else:
                        icon = "🔗"
                    content_sections.append(f'- {icon} [{display}]({link})\n')
                    
            # Add navigation backlink
            backlink = build_backlink(depth, folder)
            if backlink:
                content_sections.insert(0, backlink + "\n")
                
            # Write file
            with open(index_path, "w", encoding="utf-8") as f:
                f.write(metadata)
                f.write("\n".join(content_sections))
                f.write(generate_navigation_section())
            if debug:
                print(f"[DEBUG] Wrote resources index: {index_path}")
            continue        
        elif index_type == "course-index":
            # Separate out different folder types
            excluded_keywords = ["case", "case-study", "live session", "reading", "required reading", "resource", "resources"]
            class_folders = [sub for sub in subfolders if not any(keyword in sub.name.lower() for keyword in excluded_keywords)]
            live_session_folders = [sub for sub in subfolders if "live session" in sub.name.lower()]
            # Classes section
            if class_folders:
                content_sections.append("## Classes\n")
                for sub in class_folders:
                    icon = ICONS.get("folder", "📁")
                    link_path = f'{urllib.parse.quote(sub.name)}.md'
                    friendly_title = make_friendly_link_title(sub.name)
                    content_sections.append(f'- {icon} [{friendly_title}]({link_path})\n')
                content_sections.append("---\n")
            # Live Sessions section
            if live_session_folders:
                content_sections.append("## Live Sessions\n")
                for sub in live_session_folders:
                    icon = ICONS.get("folder", "📁")
                    link_path = f'{urllib.parse.quote(sub.name)}.md'
                    friendly_title = make_friendly_link_title(sub.name)
                    content_sections.append(f'- {icon} [{friendly_title}]({link_path})\n')
                content_sections.append("---\n")            # Case Studies section
            case_study_folders = [sub for sub in subfolders if ("case" in sub.name.lower() or "case-study" in sub.name.lower())]
            if case_study_folders:
                content_sections.append("## Case Studies\n")
                for sub in case_study_folders:
                    icon = ICONS.get("folder", "📁")
                    link_path = f'{urllib.parse.quote(sub.name)}.md'
                    friendly_title = make_friendly_link_title(sub.name)
                    content_sections.append(f'- {icon} [{friendly_title}]({link_path})\n')
                content_sections.append("---\n")
                
            # Required Readings section
            readings_folders = [sub for sub in subfolders if ("reading" in sub.name.lower() or "required reading" in sub.name.lower())]
            if readings_folders:
                content_sections.append("## Required Readings\n")
                for sub in readings_folders:
                    icon = ICONS.get("readings", "📄")
                    link_path = f'{urllib.parse.quote(sub.name)}.md'
                    friendly_title = make_friendly_link_title(sub.name)
                    content_sections.append(f'- {icon} [{friendly_title}]({link_path})\n')
                content_sections.append("---\n")
                
            # Resources section
            resources_folders = [sub for sub in subfolders if ("resource" in sub.name.lower() or "resources" in sub.name.lower())]
            if resources_folders:
                content_sections.append("## Resources\n")
                for sub in resources_folders:
                    icon = ICONS.get("folder", "📁") 
                    link_path = f'{urllib.parse.quote(sub.name)}.md'
                    friendly_title = make_friendly_link_title(sub.name)
                    content_sections.append(f'- {icon} [{friendly_title}]({link_path})\n')
                content_sections.append("---\n")
        # For all other index types, ensure all section headings use '##' and horizontal lines, and only show if they have files/folders
        else:
            if subfolders:
                content_sections.append(f"## {section_heading}\n")
                for sub in subfolders:
                    icon = ICONS.get("folder", "📁")
                    link_path = f'{urllib.parse.quote(sub.name)}.md'
                    sub_index_filename = f'{sub.name}.md'
                    if sub_index_filename == index_filename:
                        continue
                    friendly_title = make_friendly_link_title(sub.name)
                    content_sections.append(f'- {icon} [{friendly_title}]({link_path})\n')
                content_sections.append("---\n")
        
        # Don't add empty additional sections
        # for extra_section in get_additional_sections(index_type):
        #    content_sections.append(f"## {extra_section}\n")
            
        categorized = {k: [] for k in ORDER}
        for file in files:
            # Exclude the current index file from all categorized file lists
            if file.name == index_filename:
                continue
            file_type = classify_file(file)
            if not file_type:
                continue
            categorized[file_type].append(file)
            
        for cat in ORDER:
            # For lesson-index, merge videos and transcripts into 'Videos & Transcripts'
            if index_type == "lesson-index":
                if cat == "videos":
                    video_files = categorized["videos"]
                    transcript_files = categorized["transcripts"]
                    all_files = video_files + transcript_files
                    # Only add the section if there are files
                    if all_files:
                        content_sections.append(f"## Videos & Transcripts\n---\n")
                        for file in all_files:
                            tags = " ".join(get_tags_for_file(file, cat, index_type))
                            friendly_title = make_friendly_link_title(file.stem)
                            link = urllib.parse.quote(file.name)
                            content_sections.append(f'- {ICONS.get(cat, "")} [{friendly_title}]({link}) {tags}\n')
                        content_sections.append("---\n")
                elif cat == "transcripts":
                    continue  # Skip the separate transcripts section
                else:
                    # Only add the section if there are files
                    if categorized[cat]:
                        icon = ICONS.get(cat, "")
                        content_sections.append(f"## {cat.capitalize()}\n---\n")
                        for file in categorized[cat]:
                            tags = " ".join(get_tags_for_file(file, cat, index_type))
                            friendly_title = make_friendly_link_title(file.stem)
                            link = urllib.parse.quote(file.name)
                            content_sections.append(f'- {icon} [{friendly_title}]({link}) {tags}\n')
                        content_sections.append("---\n")
            else:
                # Only add the section if there are files
                if categorized[cat]:
                    icon = ICONS.get(cat, "")
                    content_sections.append(f"## {cat.capitalize()}\n")
                    for file in categorized[cat]:
                        tags = " ".join(get_tags_for_file(file, cat, index_type))
                        friendly_title = make_friendly_link_title(file.stem)
                        link = urllib.parse.quote(file.name)
                        content_sections.append(f'- {icon} [{friendly_title}]({link}) {tags}\n')
                    content_sections.append("---\n")
        backlink = build_backlink(depth, folder)
        if backlink:
            content_sections.insert(0, backlink + "\n")
        with open(index_path, "w", encoding="utf-8") as f:
            f.write(metadata)
            f.write("\n".join(content_sections))            
            f.write(generate_navigation_section())
        if debug:
            print(f"[DEBUG] Wrote {index_type}: {index_path}")
        generated_count += 1
        generated_types[index_type] = generated_types.get(index_type, 0) + 1
    
    # Calculate elapsed time
    elapsed_time = time.time() - start_time
    
    return generated_count, generated_types, elapsed_time

# ======= MAIN SCRIPT ========

def main():
    """
    Main entry point for the MBA Notebook Automation system.
    
    This function processes command-line arguments and executes the appropriate
    operations based on user input. It supports both file conversion operations
    and index generation, either individually or combined in a single run.
    
    Supported Operations:
    1. File Conversion: Transforms HTML, TXT, and other files to Markdown format
       - Cleans filenames and standardizes formatting
       - Adds appropriate frontmatter and structure
       - Preserves readonly files
       
    2. Index Generation: Creates hierarchical navigation structure
       - Builds index files at all levels of the hierarchy
       - Categorizes and organizes content
       - Adds navigation and metadata
       
    3. Combined Operation: Performs both conversion and index generation
       - First converts files to ensure latest content is available
       - Then generates indexes based on the updated content
       
    Command-line Arguments:
    - --convert: Enable file conversion
    - --generate-index: Enable index generation
    - --all: Enable both operations
    - --source: Path to the source directory (required)
    - --index-type: Optional filter for specific index types
    - --debug: Enable verbose debug output
    
    Returns:
        None: Results are output to the console and files are written to disk
    """    
    parser = argparse.ArgumentParser(description="Notebook Generator - MBA Course Notes Automation")
    parser.add_argument('--convert', action='store_true', help='Convert files (HTML/TXT to Markdown)')
    parser.add_argument('--generate-index', action='store_true', help='Generate index files')
    parser.add_argument('--all', action='store_true', help='Run all operations')
    parser.add_argument('--source', type=str, help='Source directory for processing. Defaults to VAULT_LOCAL_ROOT from config if not specified.')
    parser.add_argument('--debug', action='store_true', help='Enable debug logging')
    parser.add_argument('--index-type', action='append', help='Only generate indexes of the specified type(s) (e.g., lesson-index, live-session-index). Can be used multiple times.')
    args = parser.parse_args()
    
    # If no operation specified, default to generate-index
    if not (args.convert or args.generate_index or args.all):
        args.generate_index = True
        logger.info("No operation specified, defaulting to --generate-index")
    
    # If no source specified, default to VAULT_LOCAL_ROOT from config
    if not args.source:
        args.source = str(VAULT_LOCAL_ROOT)
        logger.info(f"No source directory specified, defaulting to VAULT_LOCAL_ROOT: {args.source}")

    debug = args.debug
    if debug:
        print("[DEBUG] Arguments:", args)
        print("[DEBUG] Loaded metadata templates:", METADATA_TEMPLATES.keys())

    if not METADATA_TEMPLATES:
        print("[ERROR] No metadata templates loaded. Exiting.")
        sys.exit(1)

    source_path = Path(args.source).resolve()
    if debug:
        print(f"[DEBUG] Source path: {source_path}")

    def should_generate_index_of_type(index_type):
        if not args.index_type:
            return True  # No filter, generate all
        # Handle both list and string cases, and normalize for comparison
        filter_types = [t.strip() for t in args.index_type]
        return index_type in filter_types    
    if args.convert or args.all:
        if debug:
            print("[DEBUG] Starting file conversion...")
        process_directory_conversion(source_path)
        if debug:
            print("[DEBUG] File conversion complete.")
            
    if args.generate_index or args.all:
        generated_count, generated_types, elapsed_time = generate_indexes(source_path, debug=debug, index_type_filter=should_generate_index_of_type)
        print("\nSummary:")
        print(f"Total indexes generated: {generated_count}")
        print(f"Total time: {elapsed_time:.2f} seconds")
        for t, c in generated_types.items():
            print(f"  {t}: {c}")

if __name__ == "__main__":
    main()

