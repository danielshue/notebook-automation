"""
Path Metadata Module for Intelligent Metadata Extraction from Filepaths

This module provides intelligent metadata extraction capabilities for the Notebook Generator
system, enabling automatic identification of course structure, hierarchy, and context from
file paths and directory structures without requiring explicit tagging.

Key Features:
------------
1. Template Management
   - Loading and parsing of YAML-based metadata templates
   - Template validation and error handling
   - Hierarchical template organization by type

2. Path-based Metadata Extraction
   - Extraction of program, course, and class information from file paths
   - Normalization of extracted metadata for consistency
   - Heuristic-based inference for incomplete or ambiguous paths

3. Educational Context Recognition
   - Recognition of common educational program indicators
   - Intelligent handling of course naming conventions
   - Support for standard MBA curriculum structures

Integration Notes:
----------------
- Works with the utils.paths module for path normalization
- Provides metadata to notes and PDF processing pipelines
- Supports the hierarchical organization of educational content
- Enables automatic context-aware tagging and categorization
"""
import os
import yaml
from pathlib import Path
from ..utils.paths import normalize_path
from ..utils.config import logger

import re

def load_metadata_templates():
    """
    Load YAML templates from the metadata.yaml file in the script directory.
    
    This function locates and loads the metadata templates used throughout the
    Notebook Generator system. These templates define the structure, default values,
    and formatting rules for various types of content (videos, PDFs, references, etc.).
    
    The function handles:
    1. Template file discovery based on script location
    2. Multi-document YAML parsing (each template is a separate YAML document)
    3. Organization of templates by template-type for easy lookup
    4. Error handling with detailed logging
    
    Returns:
        dict: Dictionary of templates indexed by template-type.
              Empty dict if templates cannot be loaded.
    
    Note:
        Templates are expected to be in YAML format with a 'template-type' field
        that identifies the type of content they apply to (e.g., 'video', 'pdf-reference').
    """
    import sys
    # Locate the script directory to find metadata.yaml relative to the executing script
    script_dir = Path(os.path.dirname(os.path.abspath(sys.argv[0]))).resolve()
    yaml_path = script_dir / "metadata.yaml"

    if not yaml_path.exists():
        logger.error(f"metadata.yaml not found at {yaml_path}")
        return {}

    try:
        # Parse all YAML documents in the file (using safe_load_all for security)
        with open(yaml_path, 'r', encoding='utf-8') as f:
            templates_list = list(yaml.safe_load_all(f))
        
        # Organize templates by their template-type for easy lookup
        templates = {}
        for doc in templates_list:
            if isinstance(doc, dict) and 'template-type' in doc:
                templates[doc['template-type']] = doc
                
        logger.info(f"Loaded {len(templates)} templates from metadata.yaml: {', '.join(templates.keys())}")
        return templates
    except Exception as e:
        logger.error(f"Error loading templates: {e}")
        return {}

def extract_metadata_from_path(file_path):
    """
    Extract course and program information from file path using intelligent heuristics.
    
    This function analyzes a file path to automatically determine educational context
    information such as program and course names. It uses a combination of pattern
    recognition, directory structure analysis, and educational domain knowledge to
    make these determinations without requiring explicit tagging.
    
    The extraction process includes:
    1. Path normalization to handle different operating systems
    2. Pattern matching for common program identifiers
    3. Intelligent directory hierarchy analysis
    4. Cleanup and formatting of extracted metadata
    5. Fallback mechanisms when primary extraction methods fail
    
    Args:
        file_path (Path or str): Path to the file for metadata extraction
        
    Returns:
        dict: Extracted metadata dictionary containing keys such as:
              - 'program': The academic program (e.g., 'MBA', 'Executive MBA')
              - 'course': The specific course (e.g., 'Financial Management')
              May be empty if no metadata could be reliably extracted.
    
    Note:
        This function uses heuristics and may not be 100% accurate in all cases,
        especially with non-standard directory structures or naming conventions.
    """
    # Normalize the path first
    file_path = normalize_path(file_path)
    if isinstance(file_path, str):
        file_path = Path(file_path)
    
    # Convert to string and normalize slashes
    path_str = str(file_path).replace('\\', '/')
    
    # Extract course and program metadata from the path
    metadata = {}
    
    # Extract program name - usually the first directory after MBA-Resources
    parts = path_str.split('/')
    
    # Debug logging
    logger.debug(f"extract_metadata_from_path: Path parts: {parts}")
    
    # Look for common program names in the path
    program_indicators = ['MBA', 'EMBA', 'MsBA', 'Executive MBA', 'Data Analytics', 
                         'Marketing', 'Finance', 'Accounting', 'Leadership', 'Strategy']
    
    # Try to find course name first (usually in depth position -2)
    try:
        # Course is often the parent directory
        course_name = file_path.parent.name
        if course_name and course_name not in ['video', 'videos', 'recordings', 'transcripts', 'materials']:
            # Clean up the course name
            course_name = course_name.replace('_', ' ').replace('-', ' ').strip()
            
            # Sanity check: If it's likely not a course name (too short or just a number),
            # try the grandparent directory
            if len(course_name) <= 3 or course_name.isdigit():
                course_name = file_path.parent.parent.name.replace('_', ' ').replace('-', ' ').strip()
                
            metadata['course'] = course_name
    except Exception as e:
        logger.debug(f"Error extracting course name: {e}")
      # Try to find program name (usually higher up in the directory structure)
    try:
        # Find the deepest directory that matches a program indicator
        # This prioritizes more specific program information that appears deeper in the path
        for part in parts:
            clean_part = part.replace('_', ' ').replace('-', ' ').strip()
            for indicator in program_indicators:
                if indicator.lower() in clean_part.lower() and len(clean_part) > 2:
                    metadata['program'] = clean_part
                    logger.debug(f"Found program indicator '{indicator}' in '{clean_part}'")
                    break
                    
        # If no program found in directory names, try to infer from structure
        # This is a fallback method based on typical directory hierarchies
        if 'program' not in metadata and 'course' in metadata:
            # Look at the parts above the course (typically program is parent of course)
            if len(parts) >= 4:
                potential_program = parts[-3]
                if potential_program not in ['videos', 'recordings', 'transcripts', 'materials']:
                    metadata['program'] = potential_program.replace('_', ' ').replace('-', ' ')
                    logger.debug(f"Inferred program '{potential_program}' from directory structure")
    except Exception as e:
        logger.debug(f"Error extracting program name: {e}")
    
    return metadata

def infer_course_and_program(path):
    """
    Attempt to infer course and program from file path using structured analysis.
    
    This function provides a more sophisticated inference mechanism than extract_metadata_from_path,
    specifically designed to handle the hierarchical structure of MBA educational content.
    It uses a combination of known program structures, regex pattern matching, and
    hierarchical path analysis to build a comprehensive context model.
    
    The inference process includes:
    1. Recognition of standard MBA program structure and specializations
    2. Multi-level path analysis for hierarchical context determination
    3. Regex pattern matching for class identification
    4. Fallback mechanisms when primary inference methods fail
    5. Subject matter detection for course categorization
    
    Args:
        path (Path or str): Path object or string path to a file or directory
        
    Returns:
        dict: Dictionary containing inferred context with the following keys:
              - 'program': The academic program (e.g., 'MBA Program')
              - 'course': The specific course (e.g., 'Financial Management')
              - 'class': The specific class or module within the course
              
              All fields will contain default values if inference fails.
    
    Note:
        This function is specifically tailored for MBA education directory structures
        and may need adjustments for other educational contexts or organization schemes.
    """
    # Use pathlib for all path manipulations in class/course inference
    path = Path(path)
    path_parts = path.parts
    info = {
        'program': 'MBA Program',
        'course': 'Unknown Course',
        'class': 'Unknown Class'
    }
    program_folders = [
        'Value Chain Management',
        'Financial Management',
        'Focus Area Specialization',
        'Strategic Leadership and Management',
        'Managerial Economics and Business Analysis'
    ]
    path_str = str(path).lower()    # Find program by matching against known program folder names
    # This uses a predefined list of MBA program specializations
    for program in program_folders:
        if program.lower() in path_str:
            info['program'] = program
            logger.debug(f"Matched known program folder: {program}")
            break
    # Find course: typically the second part after program in path_parts
    # This looks for the directory immediately following a known program directory
    for i, part in enumerate(path_parts):
        if part in program_folders and i+1 < len(path_parts):
            info['course'] = path_parts[i+1]
            break
    # Fallback: if course is still unknown, use the second part of the path
    if info['course'] == 'Unknown Course' and len(path_parts) > 1:
        info['course'] = path_parts[1]    # Class inference logic - uses multiple strategies in priority order
    # Strategy 1: Look for class as the directory after a program folder
    program_found = False
    for i, part in enumerate(path_parts):
        if part in program_folders:
            program_found = True
            if i+1 < len(path_parts):
                info['class'] = path_parts[i+1]
                logger.debug(f"Inferred class as directory after program: {info['class']}")
                break
    
    # Strategy 2: If no class found but we have a course, use course as the class
    # This handles cases where course and class are the same or not separately specified
    if info['class'] == "Unknown Class" and info['course'] != "Unknown Course":
        info['class'] = info['course']
        logger.debug(f"Using course as class: {info['class']}")    # Strategy 3: Look for explicit class naming patterns using regex
    if info['class'] == "Unknown Class":
        class_match = re.search(r'class[\s_-]*(\w+)', path_str, re.IGNORECASE)
        if class_match:
            info['class'] = f"Class {class_match.group(1)}"
            logger.debug(f"Found class via regex pattern: {info['class']}")
    
    # Strategy 4: Check for directory names containing 'class' or 'course' keywords
    if info['class'] == "Unknown Class":
        for part in path_parts:
            if "class" in part.lower() or "course" in part.lower():
                potential_class = part.replace('-', ' ').replace('_', ' ').title()
                info['class'] = potential_class
                logger.debug(f"Found class from directory name: {info['class']}")
                break    # Strategy 5: Final fallback to course name for class if still unknown
    if info['class'] == "Unknown Class" and info['course'] != "Unknown Course":
        info['class'] = info['course']
        logger.debug("Using course as fallback for class (final strategy)")
    
    # Course fallback strategy: Look for common MBA subject keywords in path parts
    if info['course'] == 'Unknown Course':
        # Common MBA curriculum subjects that might appear in directory names
        mba_subjects = ['accounting', 'economics', 'finance', 'marketing', 
                        'operations', 'management', 'strategy', 'leadership']
        
        for part in path_parts:
            clean_part = part.replace('_', ' ').replace('-', ' ').title()
            if any(keyword in clean_part.lower() for keyword in mba_subjects):
                info['course'] = clean_part
                logger.debug(f"Found course from MBA subject keyword in: {clean_part}")
                break
    
    return info
