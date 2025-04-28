#!/usr/bin/env python
"""
Index Generator - Part of the MBA Notebook Automation toolkit

This script focuses on index generation functionality:
- Creates a hierarchical structure of index files for easy navigation
- Supports a 6-level hierarchy: Main → Program → Course → Class → Module → Lesson
- Generates Obsidian-compatible wiki-links between index levels
- Creates back-navigation links for seamless browsing
- Respects "readonly" marked files to prevent overwriting customized content
- Automatically categorizes content into readings, videos, transcripts, etc.
- Adds appropriate icons for different content types
- Implements a tagging system for enhanced content discovery

Directory Structure:
-------------------
- Root (main-index)
  - Program Folders (program-index)
    - Course Folders (course-index)
      - Case Study Folders (case-studies-index)
      - Class Folders (class-index)
        - Module Folders (module-index)
          - Live Session Folder (live-session-index)
          - Lesson Folders (lesson-index)
            - Content Files (readings, videos, transcripts, etc.)

pip install pyyaml

Usage:
-------
  python generate-index.py --generate-index --source <path>  # Generate all indexes
  python generate-index.py --generate-index --source <path> --index-type lesson-index  # Generate only lesson indexes
  python generate-index.py --generate-index --source <path> --debug  # Generate indexes with debugging
"""

import re
import os
import sys
import yaml  # For metadata parsing
import argparse  # For command line arguments
from pathlib import Path
from datetime import datetime
import urllib.parse

# Function to load metadata templates from YAML file
def load_metadata_templates():
    """Load metadata templates from the metadata.yaml file"""
    script_dir = Path(os.path.dirname(os.path.abspath(sys.argv[0]))).resolve()
    yaml_path = script_dir / "metadata.yaml"
    
    if not yaml_path.exists():
        print(f"Warning: metadata.yaml not found at {yaml_path}")
        return {}
    else:
        print(f"Found metadata.yaml at {yaml_path}")
    
    # Read the YAML file content
    with open(yaml_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Split the content into documents (separated by ---)
    yaml_docs = content.split('---\n')
    print(f"Found {len(yaml_docs)} YAML documents in metadata.yaml")
    
    # Parse each document
    templates = {}
    for doc in yaml_docs:
        if not doc.strip():
            continue
        
        try:
            # Parse the YAML document
            metadata = yaml.safe_load(doc)
            
            # Skip if not a dictionary or doesn't have template-type
            if not isinstance(metadata, dict) or 'template-type' not in metadata:
                continue
            
            template_type = metadata['template-type']
            templates[template_type] = metadata
            print(f"Loaded template: {template_type}")
        except Exception as e:
            print(f"Error parsing YAML document: {e}")
    
    print(f"Total templates loaded: {len(templates)}")
    return templates

# Load the metadata templates at module import time
METADATA_TEMPLATES = load_metadata_templates()

# Icons for different content types
ICONS = {
    "readings": "📄",
    "videos": "🎬",
    "transcripts": "📓",
    "notes": "🗋",
    "quizzes": "🎓",
    "assignments": "📋",
    "folder": "📁"
}

# Order for displaying content categories
ORDER = ["readings", "videos", "transcripts", "notes", "quizzes", "assignments"]

def make_friendly_link_title(name):
    """
    Make a friendly link title by removing numbers, dashes, underscores, and words like 'lesson', 'module', 'course', 
    'overview', and 'introduction' and capitalizing properly.
    """
    # Remove leading numbers and separators
    name = re.sub(r'^[0-9]+([_\-\s]+)', '', name, flags=re.IGNORECASE)
    
    # Replace dashes/underscores with spaces
    name = re.sub(r'[_\-]+', ' ', name)
    
    # Remove the common structural words, course numbers, and lesson numbers
    words_to_remove = [
        r'\blesson\b', r'\blessons\b', r'\bmodule\b', r'\bmodules\b', 
        r'\bcourse\b', r'\bcourses\b', 
        r'\band\b', r'\bto\b', r'\bof\b',  # Common connecting words
        r'\b\d+\s*\d*\b'  # Numbers like "1" or "1 1" anywhere in the text
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

# ======= FILE CONVERSION FUNCTIONS ========

def clean_filename(name):
    """Clean up filenames by removing numbering and improving readability"""
    name = re.sub(r'^[0-9]+([_\-\s]+)', '', name)
    name = re.sub(r'^([0-9]+[_\-\s]+)+', '', name)
    name = re.sub(r'[_\-]+', ' ', name)
    name = re.sub(r'\s+', ' ', name).strip()
    return name

def check_file_writable(file_path):
    """Check if a file exists and if it's writable or readonly"""
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
            print(f"Skipping readonly file: {file_path}")
            return False
    
    return True  # File is writable or doesn't have the readonly property

def add_frontmatter(content, template_type="reading", filename=""):
    """Add frontmatter to markdown content"""
    # Extract title from filename, removing extension and formatting nicely
    title = os.path.splitext(filename)[0].replace("-", " ").title() if filename else ""
    frontmatter = f"---\nauto-generated-state: writable\ntemplate-type: {template_type}\ntitle: {title}\n---\n\n"
    # Add a title heading at the top of the content if we have a title
    if title:
        return frontmatter + f"# {title}\n\n" + content
    return frontmatter + content

def convert_html_to_md(html_path, md_path):
    """Convert HTML files to Markdown with frontmatter"""
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
                print(f"🗑️ Deleting SRT file: {path}")
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
                    print(f"✅ Converted TXT → {md_path}")
                continue

            if ext == ".html":
                md_path = path.with_name(cleaned_name + ".md")
                if convert_html_to_md(path, md_path):
                    path.unlink()
                    print(f"✅ Converted HTML → {md_path}")
                continue

            if ext in ['.mp4', '.pdf']:
                new_path = path.with_name(cleaned_name + path.suffix)
                if new_path != path and path.exists():
                    try:
                        shutil.move(str(path), str(new_path))
                        print(f"📁 Renamed: {path.name} → {new_path.name}")
                    except Exception as e:
                        print(f"❌ Error renaming {path}: {e}")

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
    program, course = infer_course_and_program(folder, index_type)
    add_program_course = index_type not in ("main-index", "program-index")
    add_completion_fields = index_type == "lesson-index"
    if index_type not in METADATA_TEMPLATES:
        title = make_friendly_link_title(folder.name)
        breadcrumbs_yaml = generate_breadcrumbs_yaml(folder, depth)
        extra_fields = (
            (f"program: {program}\n" if add_program_course else "") +
            (f"course: {course}\n" if add_program_course else "") +
            (f"completion-date: \nreview-date: \ncomprehension: \n" if add_completion_fields else "")
        )
        # Add banner fields at the end of YAML
        banner_fields = "banner: \"![[gies-banner.png]]\"\nbanner_x: 0.25\n"
        return (
            f"---\n"
            f"auto-generated-state: writable\n"
            f"template-type: {index_type}\n"
            f"title: {title}\n"
            f"{extra_fields}"
            f"{breadcrumbs_yaml}\n"
            f"{banner_fields}---\n\n"
            f"# {title}\n\n")
    template = METADATA_TEMPLATES[index_type].copy()
    template['date-created'] = datetime.now().strftime('%Y-%m-%d')
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
    # Add banner fields at the end of YAML
    yaml_text = yaml.safe_dump(template, sort_keys=False, default_flow_style=False, allow_unicode=True)
    breadcrumbs_yaml = generate_breadcrumbs_yaml(folder, depth)
    banner_fields = "banner: \"![[gies-banner.png]]\"\nbanner_x: 0.25\n"
    title = template['title']
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

def generate_indexes(root_path, debug=False, index_type_filter=None):
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
            # Write file
            with open(index_path, "w", encoding="utf-8") as f:
                f.write(metadata)
                f.write("\n".join(content_sections))
                f.write(generate_navigation_section())
            if debug:
                print(f"[DEBUG] Wrote case-studies index: {index_path}")
            continue
        elif index_type == "course-index":
            # Separate out class folders, live session folders, and case study folders
            class_folders = [sub for sub in subfolders if not ("case" in sub.name.lower() or "case-study" in sub.name.lower() or "live session" in sub.name.lower())]
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
                content_sections.append("---\n")
            # Case Studies section (unchanged)
            case_study_folders = [sub for sub in subfolders if ("case" in sub.name.lower() or "case-study" in sub.name.lower())]
            if case_study_folders:
                content_sections.append("## Case Studies\n")
                for sub in case_study_folders:
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
    return generated_count, generated_types

# ======= MAIN SCRIPT ========

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Notebook Generator - MBA Course Notes Automation")
    parser.add_argument('--convert', action='store_true', help='Convert files (HTML/TXT to Markdown)')
    parser.add_argument('--generate-index', action='store_true', help='Generate index files')
    parser.add_argument('--all', action='store_true', help='Run all operations')
    parser.add_argument('--source', type=str, required=True, help='Source directory for processing')
    parser.add_argument('--debug', action='store_true', help='Enable debug logging')
    parser.add_argument('--index-type', action='append', help='Only generate indexes of the specified type(s) (e.g., lesson-index, live-session-index). Can be used multiple times.')
    args = parser.parse_args()

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
        generated_count, generated_types = generate_indexes(source_path, debug=debug, index_type_filter=should_generate_index_of_type)
        print("\nSummary:")
        print(f"Total indexes generated: {generated_count}")
        for t, c in generated_types.items():
            print(f"  {t}: {c}")

