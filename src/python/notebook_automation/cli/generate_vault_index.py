#!/usr/bin/env python
"""Index Generator - Part of the MBA Notebook Automation toolkit.

This script focuses on index generation functionality for the MBA notebook project,
creating a hierarchical structure of index files for easy navigation.

Features:
    - Creates a hierarchical structure of index files for easy navigation
    - Supports a 6-level hierarchy: Main → Program → Course → Class → Module → Lesson
    - Generates Obsidian-compatible wiki-links between index levels
    - Creates back-navigation links for seamless browsing
    - Respects "readonly" marked files to prevent overwriting customized content
    - Automatically categorizes content into readings, videos, transcripts, etc.
    - Adds appropriate icons for different content types
    - Implements a tagging system for enhanced content discovery

Directory Structure:
    - Root (main-index)
      - Program Folders (program-index)
        - Course Folders (course-index)
          - Case Study Folders (case-studies-index)
          - Class Folders (class-index)
            - Module Folders (module-index)
              - Live Session Folder (live-session-index)
              - Lesson Folders (lesson-index)
                - Content Files (readings, videos, transcripts, etc.)

Example:
    Generate all indexes:
        $ python generate-index_in_vault.py --generate-index --source /path/to/vault

    Generate only lesson indexes:
        $ python generate-index_in_vault.py --generate-index --source /path/to/vault --index-type lesson-index

    Generate indexes with debugging:
        $ python generate-index_in_vault.py --generate-index --source /path/to/vault --debug

Returns:
    int: Return code indicating success (0) or failure (1)
"""

import re
import os
import sys
import yaml
import shutil
import logging
import argparse
import importlib.metadata
from pathlib import Path
from datetime import datetime
import urllib.parse
from typing import Dict, List, Optional, Union, Callable

# Import shared utilities
from notebook_automation.utils.converters import process_file as convert_to_markdown
from notebook_automation.tools.utils.config import setup_logging
import urllib.parse
from typing import Dict, List, Optional, Union, Callable

# Import shared utilities
from notebook_automation.utils.converters import process_file as convert_to_markdown

# Try importing optional dependencies
try:
    import html2text
    HTML2TEXT_AVAILABLE = True
except ImportError:
    HTML2TEXT_AVAILABLE = False
    logging.warning("html2text package not found. HTML conversion will be disabled.")

# Package version
try:
    __version__ = importlib.metadata.version("notebook_automation")
except importlib.metadata.PackageNotFoundError:
    __version__ = "0.0.0"

# Constants
ICONS = {
    "readings": "📄",
    "videos": "🎬",
    "transcripts": "📓",
    "notes": "🗋",
    "quizzes": "🎓",
    "assignments": "📋",
    "folder": "📁"
}

ORDER = ["readings", "videos", "transcripts", "notes", "quizzes", "assignments"]

# Global variables
METADATA_TEMPLATES = {}
_cached_metadata_templates = {}



def load_metadata_templates() -> Dict:
    """Load metadata templates from the metadata.yaml file.
    
    Returns:
        Dict: Dictionary containing metadata templates.
        
    Raises:
        FileNotFoundError: If metadata.yaml is not found.
        yaml.YAMLError: If metadata.yaml is invalid.
    """
    # First declare the global variable
    global _cached_metadata_templates
    
    # Then use it - check if cached templates are available
    if _cached_metadata_templates:
        logging.debug("Using cached metadata templates")
        return _cached_metadata_templates
    
    # First try to get metadata_file path from config.json
    try:
        from notebook_automation.tools.utils.config import _get_config_data
        config_data = _get_config_data()
        metadata_file_path = Path(config_data.get("paths", {}).get("metadata_file", ""))
        if metadata_file_path.exists():
            yaml_path = metadata_file_path
            logging.debug(f"Using metadata file from config: {yaml_path}")
        else:
            # Fallback to script directory
            script_dir = Path(os.path.dirname(os.path.abspath(sys.argv[0]))).resolve()
            yaml_path = script_dir / "metadata.yaml"
            logging.debug(f"Metadata file from config not found, falling back to: {yaml_path}")
    except Exception as e:
        # If there's any error getting the config, fallback to script directory
        script_dir = Path(os.path.dirname(os.path.abspath(sys.argv[0]))).resolve()
        yaml_path = script_dir / "metadata.yaml"
        logging.debug(f"Error getting config, falling back to: {yaml_path} - {str(e)}")
    
    if not yaml_path.exists():
        logging.warning(f"metadata.yaml not found at {yaml_path}")
        return {}
    
    logging.debug(f"Found metadata.yaml at {yaml_path}")
    
    templates = {}
    
    try:
        # Read the entire file
        with open(yaml_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Make sure the content starts with ---
        if not content.startswith('---'):
            content = '---\n' + content
            
        # Split content into YAML documents
        # Each document should be delimited by --- lines
        yaml_docs = []
        documents = content.split('---\n')
        
        # Skip the first empty element if the file started with ---
        if documents and not documents[0].strip():
            documents = documents[1:]
            
        for doc in documents:
            if not doc.strip():
                continue
                
            try:
                # Parse each document individually
                metadata = yaml.safe_load(doc)
                if isinstance(metadata, dict) and 'template-type' in metadata:
                    template_type = metadata['template-type']
                    templates[template_type] = metadata
                    logging.debug(f"Loaded template: {template_type}")
            except yaml.YAMLError as e:
                logging.warning(f"Skipping invalid YAML document: {e}")
                continue
        logging.info(f"Successfully loaded {len(templates)} templates")
        # Cache the templates globally
        _cached_metadata_templates = templates
        return templates
    except yaml.YAMLError as e:
        logging.error(f"Error parsing metadata.yaml: {e}")
        return {}  # Return empty dict instead of raising to allow script to continue
    except Exception as e:
        logging.error(f"Unexpected error loading metadata templates: {e}")
        return {}  # Return empty dict instead of raising to allow script to continue

def make_friendly_link_title(name: str) -> str:
    """Make a friendly link title by cleaning up the text.
    
    Args:
        name (str): Original name to clean up.
        
    Returns:
        str: Cleaned up title string.
    """
    # Remove leading numbers and separators
    name = re.sub(r'^[0-9]+([_\-\s]+)', '', name, flags=re.IGNORECASE)
    
    # Replace dashes/underscores with spaces
    name = re.sub(r'[_\-]+', ' ', name)
    
    # Remove common structural words and numbers
    words_to_remove = [
        r'\blesson\b', r'\blessons\b', r'\bmodule\b', r'\bmodules\b', 
        r'\bcourse\b', r'\bcourses\b', 
        r'\band\b', r'\bto\b', r'\bof\b',
        r'\b\d+\s*\d*\b'
    ]
    pattern = '|'.join(words_to_remove)
    name = re.sub(pattern, ' ', name, flags=re.IGNORECASE)
    
    # Clean up whitespace
    name = re.sub(r'\s+', ' ', name).strip()
    
    # Use fallback for short titles
    if len(name.strip()) < 3:
        return "Content"
    
    # Title case and fix special cases
    title_cased = name.title()
    
    # Fix Roman numerals
    title_cased = re.sub(r'\bIi\b', 'II', title_cased)
    
    # Fix known acronyms
    known_acronyms = ['CVP', 'ROI', 'KPI', 'MBA', 'CEO', 'CFO', 'COO', 'CTO', 'CIO', 'CMO']
    for acronym in known_acronyms:
        pattern = re.compile(r'\b' + acronym + r'\b', re.IGNORECASE)
        title_cased = pattern.sub(acronym, title_cased)
    
    # Handle Roman numeral "II"
    title_cased = re.sub(r'\b([A-Za-z]+) Ii\b', r'\1 II', title_cased)
    
    return title_cased

def check_dependencies() -> bool:
    """Check if all required dependencies are installed.
    
    Returns:
        bool: True if all dependencies are available, False otherwise.
    """
    if not HTML2TEXT_AVAILABLE:
        logging.error("Required package 'html2text' is not installed.")
        logging.error("Install it using: pip install html2text")
        return False
    return True

def parse_arguments() -> argparse.Namespace:
    """Parse and validate command line arguments.
    
    Returns:
        argparse.Namespace: Parsed command line arguments.
    """
    parser = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter
    )
    
    parser.add_argument(
        '--version',
        action='version',
        version=f'%(prog)s {__version__}'
    )
    
    parser.add_argument(
        '--source',
        required=False,
        type=Path,
        help='Path to the Obsidian vault (defaults to VAULT_LOCAL_ROOT if not provided)'
    )
    
    parser.add_argument(
        '--generate-index',
        action='store_true',
        help='Generate index files'
    )
    
    parser.add_argument(
        '--convert',
        action='store_true',
        help='Convert HTML files to markdown'
    )
    
    parser.add_argument(
        '--all',
        action='store_true',
        help='Perform all operations (convert and generate index)'
    )
    
    parser.add_argument(
        '--index-type',
        choices=['main-index', 'program-index', 'course-index', 
                'class-index', 'module-index', 'lesson-index'],
        help='Specific type of index to generate'
    )
    
    parser.add_argument(
        '--debug',
        action='store_true',
        help='Enable debug logging'
    )
    
    parser.add_argument(
        '--dry-run',
        action='store_true',
        help='Show what would be done without making changes'
    )
    

    parser.add_argument(
        '--delete-indexes',
        action='store_true',
        help='Recursively delete all index files (directory-named .md files) under the given --source folder.'
    )

    args = parser.parse_args()

    # Always get VAULT_LOCAL_ROOT for path logic
    try:
        from notebook_automation.tools.utils.config import VAULT_LOCAL_ROOT
        vault_root = Path(VAULT_LOCAL_ROOT)
    except ImportError:
        vault_root = None

    # If source is not provided, use VAULT_LOCAL_ROOT
    if args.source is None:
        if vault_root is not None:
            args.source = vault_root
            logging.info(f"No --source provided. Using VAULT_LOCAL_ROOT: {args.source}")
        else:
            parser.error("Could not import VAULT_LOCAL_ROOT from config. Please provide --source explicitly.")
    else:
        # If source is provided and is not absolute, treat as relative to VAULT_LOCAL_ROOT
        if not args.source.is_absolute():
            if vault_root is not None:
                args.source = vault_root / args.source
                logging.info(f"--source provided as relative path. Using: {args.source}")
            else:
                parser.error("Could not import VAULT_LOCAL_ROOT from config to resolve relative --source path.")

    # Validate source path
    if not args.source.exists():
        parser.error(f"Source path does not exist: {args.source}")

    return args

def should_generate_index_of_type(index_type: str, filter_type: Optional[str] = None) -> bool:
    """Determine if an index of the given type should be generated.
    
    Args:
        index_type (str): The type of index being considered.
        filter_type (Optional[str]): The type specified by --index-type.
        
    Returns:
        bool: True if the index should be generated, False otherwise.
    """
    if filter_type is None:
        return True
    return index_type == filter_type

# ======= FILE CONVERSION FUNCTIONS ========

def clean_filename(name: str) -> str:
    """Clean up filenames by removing numbering and improving readability.
    
    Args:
        name (str): Original filename to clean.
        
    Returns:
        str: Cleaned filename.
    """
    name = re.sub(r'^[0-9]+([_\-\s]+)', '', name)
    name = re.sub(r'^([0-9]+[_\-\s]+)+', '', name)
    name = re.sub(r'[_\-]+', ' ', name)
    name = re.sub(r'\s+', ' ', name).strip()
    return name

def check_file_writable(file_path: Path) -> bool:
    """Check if a file exists and if it's writable or readonly.
    
    Args:
        file_path (Path): Path to the file to check.
        
    Returns:
        bool: True if file is writable, False if readonly.
    """
    if not file_path.exists():
        return True
        
    try:
        # Check frontmatter for readonly state
        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
            
        frontmatter_match = re.search(r'---\s(.*?)\s---', content, re.DOTALL)
        if frontmatter_match:
            frontmatter = frontmatter_match.group(1)
            if re.search(r'auto-generated-state\s*:\s*readonly', frontmatter, re.IGNORECASE):
                logging.info(f"Skipping readonly file: {file_path}")
                return False
                
        return True
        
    except Exception as e:
        logging.warning(f"Error checking file writability for {file_path}: {e}")
        return False

def add_frontmatter(content: str, template_type: str = "reading", filename: str = "") -> str:
    """Add frontmatter to markdown content.
    
    Args:
        content (str): Original markdown content.
        template_type (str): Type of template to use.
        filename (str): Name of the file for title generation.
        
    Returns:
        str: Content with frontmatter added.
    """
    title = os.path.splitext(filename)[0].replace("-", " ").title() if filename else ""
    frontmatter = [
        "---",
        "auto-generated-state: writable",
        f"template-type: {template_type}",
        f"title: {title}",
        "---",
        ""
    ]
    
    content_with_frontmatter = "\n".join(frontmatter)
    
    if title:
        content_with_frontmatter += f"# {title}\n\n"
    
    content_with_frontmatter += content
    return content_with_frontmatter

def convert_html_to_md(html_path: Path, md_path: Path) -> bool:
    """Convert HTML files to Markdown with frontmatter.
    
    Args:
        html_path (Path): Path to the HTML file to convert.
        md_path (Path): Path where to save the markdown file.
        
    Returns:
        bool: True if conversion was successful, False otherwise.
    """
    if not HTML2TEXT_AVAILABLE:
        logging.error("html2text package is required for HTML conversion")
        return False
        
    if not check_file_writable(md_path):
        return False
        
    try:
        with open(html_path, 'r', encoding='utf-8') as html_file:
            html_content = html_file.read()
        
        # Configure html2text
        h = html2text.HTML2Text()
        h.body_width = 0  # Don't wrap lines
        h.ignore_images = False
        h.ignore_links = False
        h.ignore_emphasis = False
        h.ignore_tables = False
        
        markdown = h.handle(html_content)
        
        # Add frontmatter
        markdown_with_frontmatter = add_frontmatter(
            content=markdown,
            template_type="reading",
            filename=md_path.name
        )
        
        with open(md_path, 'w', encoding='utf-8') as md_file:
            md_file.write(markdown_with_frontmatter)
            
        logging.debug(f"Converted {html_path} to {md_path}")
        return True
        
    except Exception as e:
        logging.error(f"Error converting {html_path} to markdown: {e}")
        return False

def convert_txt_to_md(txt_path, md_path):
    """Convert TXT files to Markdown with frontmatter"""
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
    """Process all files in the directory for conversion"""
    for root, dirs, files in os.walk(course_path):
        # Skip the Templater folder and folders that begin with a dot
        dirs[:] = [d for d in dirs if d != "Templater" and not d.startswith('.')]
        
        for file in files:
            path = Path(root) / file

            # ✅ Delete all .en.srt files
            if path.name.endswith(".en.srt"):
                logging.info(f"🗑️ Deleting SRT file: {path}")
                path.unlink()
                continue

            ext = path.suffix.lower()

            if ext not in ['.html', '.txt', '.pdf', '.mp4']:
                continue

            original_name = path.stem
            cleaned_name = clean_filename(original_name)

            if ext == ".txt" and path.name.endswith(".en.txt"):
                cleaned_name = clean_filename(path.stem.replace('.en', '')) + " Transcript"
                md_path = path.with_name(cleaned_name + ".md")
                if convert_txt_to_md(path, md_path):
                    path.unlink()
                    logging.info(f"✅ Converted TXT → {md_path}")
                continue

            if ext == ".html":
                md_path = path.with_name(cleaned_name + ".md")
                if convert_html_to_md(path, md_path):
                    path.unlink()
                    logging.info(f"✅ Converted HTML → {md_path}")
                continue

            if ext in ['.mp4', '.pdf']:
                new_path = path.with_name(cleaned_name + path.suffix)
                if new_path != path and path.exists():
                    try:
                        shutil.move(str(path), str(new_path))
                        logging.info(f"📁 Renamed: {path.name} → {new_path.name}")
                    except Exception as e:
                        logging.warning(f"❌ Error renaming {path}: {e}")

# ======= INDEX GENERATION FUNCTIONS ========

def get_index_type(depth, folder_name=None):
    """Determine the type of index based on directory depth and folder name"""
    index_types = ["main-index", "program-index", "course-index", "class-index", "module-index", "lesson-index"]
    
    # Special case for Live Session folders
    if folder_name and "live session" in folder_name.lower():
        return "live-session-index"
    
    # Special case for depth 4 (module level) - check if it's a case study
    if depth == 4 and folder_name and ("case" in folder_name.lower() or "case-study" in folder_name.lower()):
        return "case-study-index"
    
    return index_types[depth] if depth < len(index_types) else "lesson-index"

def is_hidden(p):
    """Check if a path is hidden (starts with .) or is in Templater folder"""
    return any(part.startswith(".") for part in p.parts) or any(part == "Templater" for part in p.parts)

def should_include(path):
    """Determine if a path should be included in the index"""
    return not path.name.startswith(".") and path.name != "_index.md" and path.name != "Templater"

def classify_file(file):
    """Classify a file into a content category"""
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
    Infer program and course names from the folder path and index type.
    Returns (program, course) as strings.
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
    """Generate appropriate tags for a file based on its type and context"""
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
    """Build combined navigation with back link and general navigation links."""
    # Start with a navigation section header
    nav = "## "
    
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
    """Generate breadcrumbs YAML with up and down keys, all values double-quoted, using original folder/file names."""
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
    global METADATA_TEMPLATES
    program, course = infer_course_and_program(folder, index_type)
    # Use course name as title if available, else fallback to folder name
    if course:
        title = make_friendly_link_title(course)
    else:
        title = make_friendly_link_title(folder.name)
    now = datetime.now()
    def _format_with_ordinal(dt: datetime) -> str:
        day = dt.day
        if 10 <= day % 100 <= 20:
            suffix = 'th'
        else:
            suffix = {1: 'st', 2: 'nd', 3: 'rd'}.get(day % 10, 'th')
        # Use strftime for all but the day, then insert day+suffix manually
        # Handle Windows: use %#I for hour (no leading zero), else %-I (Linux/Mac)
        try:
            hour_fmt = '%#I' if os.name == 'nt' else '%-I'
        except Exception:
            hour_fmt = '%I'
        base = dt.strftime(f'%A, %B {{}}{suffix} %Y, {hour_fmt}:%M:%S %p')
        base = base.replace('AM', 'am').replace('PM', 'pm')
        return base.format(day)
    date_created = _format_with_ordinal(now)
    date_modified = _format_with_ordinal(now)
    # Only apply rich template for class-index
    if index_type == "class-index":
        # Aliases: use the title as the only alias
        aliases = [title]
        # Breadcrumbs YAML
        breadcrumbs_yaml = generate_breadcrumbs_yaml(folder, depth)
        # Permalink: build from folder path (lowercase, replace spaces with dashes)
        permalink = folder.as_posix().replace(" ", "-").lower()
        # Class: slugified class name
        class_slug = folder.name.replace(" ", "-").lower()
        # Tags: program and course tags
        tags = [f"course/{program.replace(' ', '-')}", f"program/Mba"] if program else []
        # YAML frontmatter as dict
        yaml_dict = {
            "title": title,
            "aliases": [title],
            "auto-generated-state": "writable",
            "banner": "[[gies-banner.png]]",
            "class": class_slug,
            "course": course,
            "date-created": date_created,
            "date-modified": date_modified,
            "linter-yaml-title-alias": title,
            "permalink": permalink,
            "program": program,
            "tags": tags,
            "template-type": "class-index"
        }
        # Merge in breadcrumbs YAML as dict
        # Parse breadcrumbs_yaml to dict
        import yaml as _yaml
        breadcrumbs_dict = _yaml.safe_load(breadcrumbs_yaml)
        if breadcrumbs_dict:
            yaml_dict["breadcrumbs"] = breadcrumbs_dict.get("breadcrumbs", {})
        yaml_text = yaml.safe_dump(yaml_dict, sort_keys=False, default_flow_style=False, allow_unicode=True)
        # Markdown body
        nav = build_backlink(depth, folder) + "\n\n"
        title_section = f"# {title}\n\n"
        modules_section = "# Modules\n\n"
        # List subfolders as modules
        subfolders = [item for item in sorted(folder.iterdir()) if item.is_dir() and should_include(item)]
        for sub in subfolders:
            display = make_friendly_link_title(sub.name)
            modules_section += "- 📁 [[{}/{}.md|{}]]\n\n".format(sub.name, sub.name, display)
        modules_section += "---\n"
        return f"---\n{yaml_text}---\n\n{nav}{title_section}{modules_section}"
    # Fallback to previous logic for other index types
    add_program_course = index_type not in ("main-index", "program-index")
    add_completion_fields = index_type == "lesson-index"
    if index_type not in METADATA_TEMPLATES:
        breadcrumbs_yaml = generate_breadcrumbs_yaml(folder, depth)
        # Build YAML dict
        yaml_dict = {
            "auto-generated-state": "writable",
            "template-type": index_type,
            "title": title,
        }
        if add_program_course:
            yaml_dict["program"] = program
            yaml_dict["course"] = course
        if add_completion_fields:
            yaml_dict["completion-date"] = ""
            yaml_dict["review-date"] = ""
            yaml_dict["comprehension"] = ""
        # Add breadcrumbs and banner
        import yaml as _yaml
        breadcrumbs_dict = _yaml.safe_load(breadcrumbs_yaml)
        if breadcrumbs_dict:
            yaml_dict["breadcrumbs"] = breadcrumbs_dict.get("breadcrumbs", {})
        yaml_dict["banner"] = "[[gies-banner.png]]"
        yaml_text = yaml.safe_dump(yaml_dict, sort_keys=False, default_flow_style=False, allow_unicode=True)
        nav = build_backlink(depth, folder) + "\n\n"
        title_section = f"# {title}\n\n"
        return f"---\n{yaml_text}---\n\n{nav}{title_section}"
    template = METADATA_TEMPLATES[index_type].copy()
    template['date-created'] = datetime.now().strftime('%Y-%m-%d')
    # Use course name as title if available, else fallback to folder name
    if course:
        template['title'] = make_friendly_link_title(course)
    else:
        template['title'] = make_friendly_link_title(folder.name)
    if add_program_course:
        if 'program' not in template or not template['program']:
            template['program'] = program
        if 'course' not in template or not template['course']:
            template['course'] = course
    else:
        template.pop('program', None)
        template.pop('course', None)
    if add_completion_fields:
        if 'completion-date' not in template:
            template['completion-date'] = ""
        if 'review-date' not in template:
            template['review-date'] = ""
        if 'comprehension' not in template:
            template['comprehension'] = ""
    else:
        template.pop('completion-date', None)
        template.pop('review-date', None)
        template.pop('comprehension', None)
    # Add breadcrumbs and banner
    breadcrumbs_yaml = generate_breadcrumbs_yaml(folder, depth)
    import yaml as _yaml
    breadcrumbs_dict = _yaml.safe_load(breadcrumbs_yaml)
    if breadcrumbs_dict:
        template["breadcrumbs"] = breadcrumbs_dict.get("breadcrumbs", {})
    template["banner"] = "[[gies-banner.png]]"
    yaml_text = yaml.safe_dump(template, sort_keys=False, default_flow_style=False, allow_unicode=True)
    title = template['title']
    nav = build_backlink(depth, folder) + "\n\n"
    title_section = f"# {title}\n\n"
    return f"---\n{yaml_text}---\n\n{nav}{title_section}"

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
        "case-studies-index": "Case Studies"
    }
    return headings.get(index_type, "Subfolders")

def get_additional_sections(index_type):
    """Return any additional section headings for the given index type (e.g., Readings for class-index)."""
    if index_type == "class-index":
        return ["Readings"]
    if index_type == "lesson-index":
        return ["Readings", "Videos & Transcripts", "Notes"]
    return []

def generate_navigation_section():
    """This function now returns empty string as navigation is handled by build_backlink."""
    return ""

def get_index_template(index_type: str) -> Dict:
    """Get the index template from METADATA_TEMPLATES.
    
    Args:
        index_type (str): Type of index to get template for.
        
    Returns:
        Dict: Template data for the index type.
    """
    global METADATA_TEMPLATES
    if index_type not in METADATA_TEMPLATES:
        logging.warning(f"No template found for index type: {index_type}")
        return {}
    return METADATA_TEMPLATES[index_type]

def generate_index_content(path: Path, content_type: str, items: List[Dict]) -> str:
    """Generate content for an index file.
    
    Args:
        path (Path): Path to the directory being indexed.
        content_type (str): Type of content being indexed.
        items (List[Dict]): List of items to include in the index.
        
    Returns:
        str: Generated index content.
    """
    template = get_index_template(content_type)
    # Use course name as title if available, else fallback to folder name
    program, course = infer_course_and_program(path, content_type)
    if course:
        title = make_friendly_link_title(course)
    else:
        title = make_friendly_link_title(path.name)

    # Compute depth for navigation (relative to vault root)
    def find_vault_root(p: Path) -> Path:
        for parent in [p] + list(p.parents):
            if (parent / '01_Projects').exists() or (parent / 'MBA').exists() or (parent / 'MBA-Classes-Assignments.md').exists():
                return parent
        return p
    vault_root = find_vault_root(path.resolve())
    try:
        relative_path = path.resolve().relative_to(vault_root)
        depth = len(relative_path.parts)
    except ValueError:
        depth = 0

    # Navigation section
    nav = build_backlink(depth, path) + "\n"

    lines = [
        "---",
        "auto-generated-state: writable",
        f"template-type: {content_type}",
        f"title: {title}",
        "---",
        "",
        nav,
        f"# {title}",
        ""
    ]

    # Add template-specific content if available
    if template.get('header'):
        lines.extend([template['header'], ""])

    # Group items by type
    grouped_items = {}
    for item in items:
        item_type = item.get('type', 'other')
        if item_type not in grouped_items:
            grouped_items[item_type] = []
        grouped_items[item_type].append(item)

    # Add items in specified order
    for category in ORDER:
        if category in grouped_items:
            items = grouped_items[category]
            icon = ICONS.get(category, '')

            lines.extend([f"## {icon} {category.title()}", ""])

            for item in sorted(items, key=lambda x: x.get('title', '')):
                link = item.get('link', '')
                title = item.get('title', '')
                lines.append(f"- [[{link}|{title}]]")

            lines.append("")

    # Add remaining uncategorized items
    other_items = [
        item for item in items
        if item.get('type', 'other') not in ORDER
    ]

    if other_items:
        lines.extend(["## Other", ""])
        for item in sorted(other_items, key=lambda x: x.get('title', '')):
            link = item.get('link', '')
            title = item.get('title', '')
            lines.append(f"- [[{link}|{title}]]")
        lines.append("")

    # Add template-specific footer if available
    if template.get('footer'):
        lines.extend(["", template['footer']])

    return "\n".join(lines)

def create_index_file(
    path: Path, 
    index_type: str, 
    items: list[dict], 
    dry_run: bool = False
) -> bool:
    """Create an index file with the given items.
    
    Args:
        path (Path): The directory path where the index will be created.
        index_type (str): Type of index to create.
        items (list[dict]): Items to include in the index.
        dry_run (bool): If True, only show what would be done without creating files.
        
    Returns:
        bool: True if index was created or would be created (in dry_run mode).
    """
    if not items:
        return False

    # Use the global METADATA_TEMPLATES
    global METADATA_TEMPLATES
    # Compute depth for navigation
    # Depth is the number of parts in the path relative to the vault root
    # We'll use the same logic as in generate_indexes
    def find_vault_root(path: Path) -> Path:
        for parent in [path] + list(path.parents):
            if (parent / '01_Projects').exists() or (parent / 'MBA').exists() or (parent / 'MBA-Classes-Assignments.md').exists():
                return parent
        return path
    vault_root = find_vault_root(path.resolve())
    try:
        relative_path = path.resolve().relative_to(vault_root)
        depth = len(relative_path.parts)
    except ValueError:
        depth = 0

    # Use directory name for the index file instead of a fixed "index.md"
    index_file = path / f"{path.name}.md"

    # Generate the header, navigation, and title section using generate_metadata_from_template
    header_section = generate_metadata_from_template(index_type, path, depth).rstrip()

    # Handle items
    files = [item for item in items if item['type'] == 'file']
    folders = [item for item in items if item['type'] == 'folder']

    # Start with the header (which already includes YAML, navigation, and title)
    content = [header_section]

    # Add folders section if any folders exist
    if folders:
        content.append("## Folders")
        for folder in sorted(folders, key=lambda x: x.get('title', '').lower()):
            # Link to the directory-named index file
            link_path = f"{folder['link']}/{folder['path'].name}"
            content.append(f"- [[{link_path}|{folder['title']}]]")
        content.append("")

    # Add files section if any files exist
    if files:
        content.append("## Files")
        for file in sorted(files, key=lambda x: x.get('title', '').lower()):
            content.append(f"- [[{file['link']}|{file['title']}]]")
        content.append("")

    # In dry run mode, just log what would happen
    if dry_run:
        logging.info(f"Would create index at {index_file}")
        return True

    # Write the content to the file
    try:
        with open(index_file, 'w', encoding='utf-8') as file:
            file.write('\n'.join(content))
        return True
    except Exception as e:
        logging.error(f"Failed to create index at {index_file}: {e}")
        return False

def get_content_type(path: Path) -> str:
    """Determine the content type based on file path and name.
    
    Args:
        path (Path): Path to analyze.
        
    Returns:
        str: Detected content type.
    """
    name = path.name.lower()
    parent = path.parent.name.lower()
    
    if "video" in name or "video" in parent:
        return "videos"
    elif "reading" in name or "reading" in parent:
        return "readings"
    elif "transcript" in name or "transcript" in parent:
        return "transcripts"
    elif "quiz" in name or "quiz" in parent:
        return "quizzes"
    elif "assignment" in name or "assignment" in parent:
        return "assignments"
    elif "note" in name or "note" in parent:
        return "notes"
    else:
        return "other"

def gather_index_items(path: Path) -> list[dict]:
    """Gather all items to include in an index file.
    
    Args:
        path (Path): The directory path to gather items from.
        
    Returns:
        list[dict]: List of items with their metadata.
    """
    items = []
    
    for child in path.iterdir():
        if child.name.startswith('.'):
            continue
        
        # Skip automatically generated index files (both old "index.md" and new directory-named indices)
        if child.name == "index.md" or (child.is_file() and child.stem == path.name and child.suffix.lower() == ".md"):
            continue
        
        # Get relative path for links
        relative_path = child.relative_to(path).as_posix()
        
        # Get title from the filename or directory name
        title = child.stem if child.is_file() else child.name
        
        if child.is_file():
            if child.suffix.lower() == '.md':
                items.append({
                    'type': "file",
                    'link': relative_path,
                    'title': title,
                    'path': child
                })
        elif child.is_dir():
            # For directories, link to their directory-named index
            items.append({
                'type': "folder",
                'link': relative_path,
                'title': title,
                'path': child
            })
    
    return items

def convert_file_to_markdown(src_file: Path, dest_file: Path, dry_run: bool = False, verbose: bool = False) -> bool:
    """Convert a file to markdown using shared utilities.
    
    Args:
        src_file (Path): Source file to convert
        dest_file (Path): Destination file path
        dry_run (bool): If True, don't write any files
        verbose (bool): If True, print detailed information
        
    Returns:
        bool: True if conversion was successful
    """
    if verbose:
        logging.info(f"Converting file: {src_file} -> {dest_file}")
        
    success, error = convert_to_markdown(str(src_file), str(dest_file), dry_run)
    
    if not success and error:
        logging.error(f"Error converting {src_file}: {error}")
    elif verbose:
        logging.info(f"{'[DRY RUN] Would convert' if dry_run else 'Converted'} file: {src_file}")
        
    return success

def convert_files(source_path: Path, dry_run: bool = False, verbose: bool = False) -> int:
    """Convert files in the source directory to markdown.
    
    Args:
        source_path (Path): Root path to process
        dry_run (bool): If True, don't write any files
        verbose (bool): If True, print detailed information
        
    Returns:
        int: Number of files successfully converted
    """
    converted_count = 0
    
    try:
        for root, _, files in os.walk(source_path):
            root_path = Path(root)
            
            for file in files:
                if not (file.endswith('.html') or file.endswith('.txt')):
                    continue
                    
                file_path = root_path / file
                md_path = file_path.with_suffix('.md')
                
                if verbose:
                    logging.info(f"Processing file: {file_path}")
                
                # Skip conversion if target exists
                if md_path.exists() and not dry_run:
                    if verbose:
                        logging.info(f"Skipping existing file: {md_path}")
                    continue
                
                # Use shared conversion utility
                if convert_file_to_markdown(file_path, md_path, dry_run, verbose):
                    converted_count += 1
                    
                    # On successful conversion, handle the original file
                    if not dry_run:
                        try:
                            backup_path = file_path.with_suffix(file_path.suffix + '.bak')
                            shutil.move(str(file_path), str(backup_path))
                            if verbose:
                                logging.info(f"Backed up original to: {backup_path}")
                        except Exception as e:
                            logging.warning(f"Failed to backup {file_path}: {e}")
        
        return converted_count
        
    except Exception as e:
        logging.error(f"Error during file conversion: {e}")
        raise

def generate_indexes(
    source_path: Path,
    debug: bool = False,
    index_type_filter: Callable[[str], bool] = lambda _: True,
    dry_run: bool = False
) -> tuple[int, set]:
    """Generate index files in the directory structure.
    
    Args:
        source_path (Path): Root path to generate indexes for.
        debug (bool): Enable debug logging.
        index_type_filter (Callable): Function to filter index types.
        dry_run (bool): If True, only show what would be done.
        
    Returns:
        tuple[int, set]: Count of generated files and set of types generated.
    """
    generated_count = 0
    generated_types = set()
    deleted_index_files = 0
    
    try:
        # Find the true vault root by walking up until we find a folder that matches known vault roots
        # or just use the parent of the source_path if not specified
        # For now, assume the vault root is the parent of the folder containing '01_Projects' or 'MBA' or 'MBA-Classes-Assignments.md'
        def find_vault_root(path: Path) -> Path:
            for parent in [path] + list(path.parents):
                if (parent / '01_Projects').exists() or (parent / 'MBA').exists() or (parent / 'MBA-Classes-Assignments.md').exists():
                    return parent
            return path

        vault_root = find_vault_root(source_path.resolve())
        logging.debug(f"Detected vault root: {vault_root}")

        # First, find and delete all "index.md" files
        if not dry_run:
            for root, dirs, files in os.walk(source_path):
                root_path = Path(root)
                index_md_path = root_path / "index.md"
                if index_md_path.exists():
                    try:
                        index_md_path.unlink()
                        deleted_index_files += 1
                        logging.debug(f"Deleted old index file: {index_md_path}")
                    except Exception as e:
                        logging.warning(f"Failed to delete index.md at {root_path}: {e}")
            if deleted_index_files > 0:
                logging.info(f"Deleted {deleted_index_files} old index.md files")

        # Now generate new directory-named index files
        for root, dirs, files in os.walk(source_path):
            root_path = Path(root)
            # Calculate depth relative to the vault root, not just the source_path
            try:
                relative_path = root_path.relative_to(vault_root)
                depth = len(relative_path.parts)
            except ValueError:
                # If root_path is not under vault_root, fallback to source_path
                relative_path = root_path.relative_to(source_path)
                depth = len(relative_path.parts)

            # Debug output for troubleshooting
            logging.debug(f"Indexing: {root_path} | Relative to vault: {relative_path} | Depth: {depth}")

            # Determine index type based on directory structure
            if depth == 0:
                index_type = "main-index"
            elif depth == 1:
                index_type = "program-index"
            elif depth == 2:
                index_type = "course-index"
            elif depth == 3:
                if "case-studies" in root_path.name.lower():
                    index_type = "case-studies-index"
                else:
                    index_type = "class-index"
            elif depth == 4:
                index_type = "module-index"
            elif depth == 5:
                if "live-session" in root_path.name.lower():
                    index_type = "live-session-index"
                else:
                    index_type = "lesson-index"
            else:
                continue  # Skip deeper directories

            logging.debug(f"Assigned index type: {index_type} for {root_path}")

            if not index_type_filter(index_type):
                continue

            # Skip hidden directories and special folders
            if any(part.startswith('.') for part in root_path.parts):
                continue

            items = gather_index_items(root_path)

            if items and create_index_file(root_path, index_type, items, dry_run):
                generated_count += 1
                generated_types.add(index_type)
                logging.debug(f"Generated {index_type} at {root_path}")

        return generated_count, generated_types

    except Exception as e:
        logging.error(f"Error during index generation: {e}")
        raise

# ======= MAIN SCRIPT ========

def main() -> int:
    """Main entry point for the script.
    
    Returns:
        int: Return code (0 for success, 1 for failure)
    """    
    try:
        args = parse_arguments()
        setup_logging(args.debug, log_file="generate_vault_index.log")

        # Indicate which config.json is being used
        config_path = None
        try:
            from notebook_automation.tools.utils.config import CONFIG_PATH
            config_path = CONFIG_PATH if isinstance(CONFIG_PATH, str) else str(CONFIG_PATH)
        except ImportError:
            pass
        if not config_path:
            # Fallback: look for config.json in script dir or cwd
            script_dir = os.path.dirname(os.path.abspath(sys.argv[0]))
            fallback_path = os.path.join(script_dir, "config.json")
            if os.path.exists(fallback_path):
                config_path = fallback_path
            else:
                config_path = os.path.abspath("config.json")
        logging.info(f"Using config.json: {config_path}")
        print(f"Using config.json: {config_path}")

        logging.info(f"Starting index generation script v{__version__}")
        logging.debug(f"Arguments: {args}")

        # Check dependencies
        if args.convert and not check_dependencies():
            return 1        # Load metadata templates once at the beginning
        global METADATA_TEMPLATES
        logging.info("Loading metadata templates...")
        METADATA_TEMPLATES = load_metadata_templates()
        logging.info(f"Successfully loaded {len(METADATA_TEMPLATES)} metadata templates")

        if args.all:
            args.convert = True
            args.generate_index = True


        # If --delete-indexes is specified, perform deletion and exit
        if args.delete_indexes:
            try:
                deleted_count = delete_all_indexes(args.source, dry_run=args.dry_run)
                logging.info(f"Deleted {deleted_count} index files under {args.source}")
                print(f"Deleted {deleted_count} index files under {args.source}")
                return 0
            except Exception as e:
                logging.error(f"Error during index deletion: {e}")
                return 1

        if not (args.convert or args.generate_index):
            logging.error("No action specified. Use --convert, --generate-index, --delete-indexes, or --all")
            return 1

        if args.convert:
            try:
                converted_count = convert_files(args.source, dry_run=args.dry_run)
                logging.info(f"Converted {converted_count} files")
            except Exception as e:
                logging.error(f"Error during file conversion: {e}")
                return 1

        if args.generate_index:
            try:
                generated_count, generated_types = generate_indexes(
                    args.source, 
                    debug=args.debug,
                    index_type_filter=lambda t: should_generate_index_of_type(t, args.index_type),
                    dry_run=args.dry_run
                )
                logging.info(f"Generated {generated_count} index files of types: {', '.join(generated_types)}")
            except Exception as e:
                logging.error(f"Error during index generation: {e}")
                return 1
    except Exception as e:
        logging.error(f"Unhandled exception in main: {e}")
        return 1

def delete_all_indexes(folder_path: Path, dry_run: bool = False) -> int:
    """Recursively delete all directory-named index files (e.g., FOLDERNAME.md) under the given folder.
    
    Args:
        folder_path (Path): Root folder to search for index files.
        dry_run (bool): If True, only log what would be deleted.
    
    Returns:
        int: Number of index files deleted (or would be deleted in dry run).
    
    Raises:
        Exception: If an error occurs during deletion.
    
    Example:
        >>> delete_all_indexes(Path('/vault'), dry_run=True)
        42
    """
    from pathlib import Path
    import logging
    count = 0
    folder_path = Path(folder_path)
    for root, dirs, files in os.walk(folder_path):
        root_path = Path(root)
        index_file = root_path / f"{root_path.name}.md"
        if index_file.exists():
            if dry_run:
                logging.info(f"[DRY RUN] Would delete: {index_file}")
            else:
                try:
                    index_file.unlink()
                    logging.info(f"Deleted: {index_file}")
                except Exception as e:
                    logging.error(f"Failed to delete {index_file}: {e}")
                    continue
            count += 1
    return count

if __name__ == "__main__":
    sys.exit(main())
