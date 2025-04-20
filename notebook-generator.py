#!/usr/bin/env python
"""
MBA Notebook Tool - A unified tool for managing MBA course notes

This script provides comprehensive automation for organizing MBA course materials in an Obsidian vault.
It combines multiple workflows into a single unified tool to streamline course note management.

Key features:
----------------
1. File Conversion
   - Converts HTML files to Markdown with proper formatting
   - Converts transcript TXT files to Markdown
   - Cleans up filenames by removing numbering prefixes and improving readability
   - Adds YAML frontmatter with template type and auto-generated state properties

2. Index Generation
   - Creates a hierarchical structure of index files for easy navigation
   - Supports a 6-level hierarchy: Main → Program → Course → Class → Module → Lesson
   - Generates Obsidian-compatible wiki-links between index levels
   - Creates back-navigation links for seamless browsing
   - Respects "readonly" marked files to prevent overwriting customized content

3. Content Organization
   - Automatically categorizes content into readings, videos, transcripts, etc.
   - Adds appropriate icons for different content types
   - Implements a tagging system for enhanced content discovery
   - Supports structural, cognitive, and workflow tags

Directory Structure:
-------------------
- Root (main-index)
  - Program Folders (program-index)
    - Course Folders (course-index)
      - Class Folders (class-index)
        - Case Study Folders (case-study-index)
        - Module Folders (module-index)
          - Live Session Folder (live-session-index)
          - Lesson Folders (lesson-index)
            - Content Files (readings, videos, transcripts, etc.)

pip install pyyaml html2text

Usage:
-------
  python mba_notebook_tool.py --convert --source <path>  # Convert files
  python mba_notebook_tool.py --generate-index --source <path>  # Generate indexes
  python mba_notebook_tool.py --all --source <path>  # Do both operations
"""

import re
import os
import sys
import yaml  # For metadata parsing
import argparse  # For command line arguments
import html2text  # For HTML to Markdown conversion
import shutil  # For file operations
from pathlib import Path
from datetime import datetime

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
    """Build backlink for navigation between index levels"""
    if depth == 0:
        return ""

    parent_folder = folder.parent
    parent_name = parent_folder.name.replace("-", " ").title()
    # Use the same logic as index file creation for the filename
    parent_index_filename = parent_folder.name.replace("-", " ").title().replace(" ", "-") + "-Index.md"
    encoded_folder_name = parent_folder.name.replace(" ", "%20")

    if depth == 5:  # Lesson level - link back to module
        return f"[\u2190 Back to Module Index](../{parent_index_filename})"
    elif depth == 4:  # Module level - link back to class
        return f"[\u2190 Back to Class Index](../{parent_index_filename})"
    elif depth == 3:  # Class level - link back to course
        return f"[\u2190 Back to Course Index](../{parent_index_filename})"
    elif depth == 2:  # Course level - link back to program
        return f"[\u2190 Back to Program Index](../{parent_index_filename})"
    elif depth == 1:  # Program level - link back to main
        return f"[\u2190 Back to Main Index](../{parent_index_filename})"
    return ""

def generate_metadata_from_template(index_type, folder, depth):
    """Generate metadata using templates from metadata.yaml with dynamic values filled in"""
    # Default if template not found
    if index_type not in METADATA_TEMPLATES:
        return f"---\nauto-generated-state: writable\ntemplate-type: {index_type}\ntitle: {folder.name.replace('-', ' ').title()}\n---"
    
    # Get the template and create a copy to modify
    template = METADATA_TEMPLATES[index_type].copy()
    
    # Always update these dynamic values
    template['date-created'] = datetime.now().strftime('%Y-%m-%d')
    template['title'] = folder.name.replace('-', ' ').title()
    # Do NOT add backlinks to YAML frontmatter; backlinks only go in the body
    
    # Convert to YAML and return as a string
    yaml_text = yaml.dump(template, sort_keys=False)
    return f"---\n{yaml_text}---"

def write_index(folder: Path, depth: int):
    """Write an index file for the given folder"""
    index_type = get_index_type(depth, folder.name)
    folder_name_formatted = folder.name.replace("-", " ").title().replace(" ", "-")
    index_file = folder / f"{folder_name_formatted}-Index.md"
    if index_file.exists():
        with index_file.open("r", encoding='utf-8') as f:
            content = f.read()
            frontmatter_match = re.search(r'---\s(.*?)\s---', content, re.DOTALL)
            if frontmatter_match:
                frontmatter = frontmatter_match.group(1)
                if re.search(r'auto-generated-state\s*:\s*readonly', frontmatter, re.IGNORECASE):
                    print(f"Skipping readonly index: {index_file}")
                    return

    backlink = build_backlink(depth, folder)
    title = folder.name.replace("-", " ").title()

    # Generate metadata using the template from metadata.yaml
    metadata = generate_metadata_from_template(index_type, folder, depth)
    
    lines = [metadata]
    lines.append(f"\n# {title}")

    if backlink:
        lines.append(backlink)

    if index_type == "main-index":
        lines.append("\n## Programs\n")
        for item in sorted(folder.iterdir()):
            if item.is_dir() and should_include(item):
                name = item.name.replace("-", " ").title()
                encoded_folder_name = item.name.replace(" ", "%20")
                # Use the same logic as index file creation for the filename
                item_index_filename = item.name.replace("-", " ").title().replace(" ", "-") + "-Index.md"
                lines.append(f"- {ICONS['folder']} [{name}]({encoded_folder_name}/{item_index_filename})")

    elif index_type == "program-index":
        lines.append("\n## Courses\n")
        for item in sorted(folder.iterdir()):
            if item.is_dir() and should_include(item):
                name = item.name.replace("-", " ").title()
                encoded_folder_name = item.name.replace(" ", "%20")
                item_index_filename = item.name.replace("-", " ").title().replace(" ", "-") + "-Index.md"
                lines.append(f"- {ICONS['folder']} [{name}]({encoded_folder_name}/{item_index_filename})")

    elif index_type == "course-index":
        lines.append("\n## Classes\n")
        for item in sorted(folder.iterdir()):
            if item.is_dir() and should_include(item):
                name = item.name.replace("-", " ").title()
                encoded_folder_name = item.name.replace(" ", "%20")
                item_index_filename = item.name.replace("-", " ").title().replace(" ", "-") + "-Index.md"
                lines.append(f"- {ICONS['folder']} [{name}]({encoded_folder_name}/{item_index_filename})")

    elif index_type == "class-index":
        lines.append("\n## Modules\n")
        for item in sorted(folder.iterdir()):
            if item.is_dir() and should_include(item):
                name = item.name.replace("-", " ").title()
                encoded_folder_name = item.name.replace(" ", "%20")
                item_index_filename = f"{item.name}-Index.md"
                lines.append(f"- {ICONS['folder']} [{name}]({encoded_folder_name}/{item_index_filename})")

    elif index_type == "module-index":
        live_session_dir = folder / "Live Session"
        if live_session_dir.exists() and should_include(live_session_dir):
            lines.append("\n## Live Session\n")
            lines.append(f"- {ICONS['folder']} [Live Session](Live%20Session/Live-Session-Index.md)")

        lines.append("\n## Lessons\n")
        for item in sorted(folder.iterdir()):
            if item.is_dir() and should_include(item) and item.name != "Live Session":
                name = item.name.replace("-", " ").title()
                encoded_folder_name = item.name.replace(" ", "%20")
                item_index_filename = f"{item.name}-Index.md"
                lines.append(f"- {ICONS['folder']} [{name}]({encoded_folder_name}/{item_index_filename})")

    elif index_type == "case-study-index":
        lines.append("\n## Case Study Materials\n")
        
        # Add sections for different case study materials
        categorized = {k: [] for k in ORDER}
        
        for item in sorted(folder.iterdir()):
            if not should_include(item):
                continue
            if item.is_dir():
                continue
            ctype = classify_file(item)
            if ctype:
                display = item.stem.replace("-", " ").title()
                tags = get_tags_for_file(item, ctype, "case-study")
                tags_str = " ".join(tags) if tags else ""
                # URL-encode the filename for proper linking
                encoded_filename = item.name.replace(" ", "%20")
                categorized[ctype].append(f"- {ICONS[ctype]} [{display}]({encoded_filename}) {tags_str}")

        for cat in ORDER:
            if cat in ["readings", "videos", "notes"]:  # Most common case study material types
                lines.append(f"\n### {cat.title()}\n")
                if categorized[cat]:
                    lines.extend(categorized[cat])
                else:
                    lines.append(f"- {ICONS[cat]} *(none)*")
    
    else:
        categorized = {k: [] for k in ORDER}

        for item in sorted(folder.iterdir()):
            if not should_include(item):
                continue
            if item.is_dir():
                continue
            ctype = classify_file(item)
            if ctype:
                display = item.stem.replace("-", " ").title()
                tags = get_tags_for_file(item, ctype)
                tags_str = " ".join(tags) if tags else ""
                # URL-encode the filename for proper linking
                encoded_filename = item.name.replace(" ", "%20")
                categorized[ctype].append(f"- {ICONS[ctype]} [{display}]({encoded_filename}) {tags_str}")

        for cat in ORDER:
            lines.append(f"\n### {cat.title()}\n")
            if categorized[cat]:
                lines.extend(categorized[cat])
            else:
                lines.append(f"- {ICONS[cat]} *(none)*")

    with index_file.open("w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    try:
        relative_path = index_file.relative_to(Path.cwd())
    except ValueError:
        relative_path = index_file
    print(f"Wrote index: {relative_path}")

def walk_directory(root: Path, depth=0, create_live_session=False):
    """Walk the directory tree and generate indexes at each level"""
    if is_hidden(root):
        return
        
    # Skip processing if this is a Live Session folder at lesson level
    if root.name == "Live Session" and depth == 5:  # Updated from depth == 3 for the new hierarchy
        return
          # Generate the appropriate index for this folder based on depth and folder name
    write_index(root, depth)
    
    # If at module level (depth 4) and create_live_session flag is True, create a "Live Session" directory
    # Updated from depth == 2 for the new hierarchy
    if depth == 4 and create_live_session:
        live_session_dir = root / "Live Session"
        if not live_session_dir.exists():
            live_session_dir.mkdir()
            print(f"Created 'Live Session' directory in module: {root.name}")
            # Get a formatted module name for the Live Session note title
            module_name = root.name.replace('-', ' ').title()
            # Create a Live Session note and index in the new directory
            create_live_session_note(live_session_dir, module_name)
            create_live_session_index(live_session_dir, module_name)
        else:
            # If the directory already exists but the index doesn't, create the index
            module_name = root.name.replace('-', ' ').title()
            index_path = live_session_dir / "Live-Session-Index.md"
            if not index_path.exists():
                create_live_session_index(live_session_dir, module_name)
    
    # Only process child directories if at appropriate levels
    # New hierarchy depth levels:
    # depth 0 = main index - proceed to programs
    # depth 1 = program index - proceed to courses
    # depth 2 = course index - proceed to classes
    # depth 3 = class index - proceed to modules
    # depth 4 = module index - proceed to lessons
    # depth 5 = lesson index - don't go deeper (stop recursion)
    if depth < 5:  # Updated from depth < 3 for the new hierarchy
        for child in sorted(root.iterdir()):
            if child.is_dir() and not is_hidden(child):
                walk_directory(child, depth + 1, create_live_session)

def process_directory_index(source_path, create_live_session=False):
    """Process the directory tree to generate indexes"""
    root = Path(source_path).resolve()
    walk_directory(root, create_live_session=create_live_session)

def process_single_index(index_path):
    """Process a single specific folder to regenerate its index"""
    folder = Path(index_path).resolve()
    
    if not folder.exists() or not folder.is_dir():
        print(f"Error: Path '{folder}' does not exist or is not a directory.")
        return False
        
    # Determine the depth based on the folder structure
    try:
        # This is a simple heuristic - we count the number of parent directories
        # from the source path to determine the appropriate depth level
        parts = len(folder.parts)
        # Assuming at least one parent folder (the source folder itself)
        depth = max(0, min(3, parts - 1))
        
        print(f"Regenerating index for {folder} (depth estimated as {depth})")
        write_index(folder, depth)
        return True
    except Exception as e:
        print(f"Error generating index: {e}")
        return False

# ======= UTILITY FUNCTIONS ========

def copy_templater_folder(source_path):
    """Copy the Templater folder to the target directory"""
    # Get the directory where this script is located
    script_dir = Path(os.path.dirname(os.path.abspath(sys.argv[0]))).resolve()
    templater_source = script_dir / "Templater"
    templater_target = source_path / "Templater"
    
    if not templater_source.exists():
        print(f"Warning: Templater folder not found at {templater_source}")
        return False
    
    if templater_target.exists():
        print(f"Templater folder already exists at {templater_target}")
        return True
    
    try:
        shutil.copytree(templater_source, templater_target)
        print(f"✅ Copied Templater folder to {templater_target}")
        return True
    except Exception as e:
        print(f"❌ Error copying Templater folder: {e}")
        return False

def create_live_session_note(live_session_dir, module_name):
    """Create a new Live Session note file in the given Live Session directory using the Templater template"""
    # Create a descriptive filename for the Live Session note
    current_date = datetime.now().strftime("%Y-%m-%d")
    note_filename = f"{current_date} Live-Session.md"
    note_path = live_session_dir / note_filename
    
    # If the note already exists, don't overwrite it
    if note_path.exists():
        return False
    
    # Get the script directory path
    script_dir = Path(os.path.dirname(os.path.abspath(sys.argv[0]))).resolve()
    
    # Path to the Live Session Note template in the Templater folder
    template_path = script_dir / "Templater" / "Live Session Note.md"
    
    if not template_path.exists():
        print(f"Warning: Live Session Note template not found at {template_path}")
        print("Using basic default template instead")
        # Create basic frontmatter and content as fallback
        content = f"""---
template-type: live-session-note
auto-generated-state: writable
date-created: {current_date}
title: {module_name} Live Session
tags: 
 - live-session 
 - notes
---

# {module_name} Live Session
"""
    else:
        # Read the template from the Templater folder
        with open(template_path, 'r', encoding='utf-8') as f:
            template_content = f.read()
        
        # Replace Templater variables with actual values
        # Replace <%* ... -%> blocks (Templater script) with empty string
        content = re.sub(r'<%\*.*?-%>', '', template_content, flags=re.DOTALL)
        # Replace <% creationDate %> with actual date
        content = content.replace('<% creationDate %>', current_date)
        # Replace <% fileName %> with the module name + Live Session
        content = content.replace('<% fileName %>', f"{module_name} Live Session")
    
    # Write the note file
    with open(note_path, 'w', encoding='utf-8') as f:
        f.write(content)
        
    print(f"Created Live Session note: {note_path} (using template)")
    return True

def create_live_session_index(live_session_dir, module_name):
    """Create an index file for the Live Session directory"""
    index_path = live_session_dir / "Live-Session Index.md"
    
    # If the index already exists, don't overwrite it
    if index_path.exists():
        return False
    
    # Create basic frontmatter and content for the Live Session index
    current_date = datetime.now().strftime("%Y-%m-%d")
    title = f"{module_name} Live Sessions"
    
    content = f"""---
auto-generated-state: writable
template-type: live-session-index
title: {title}
---

# {title}

This index contains notes from live sessions for the {module_name} module.

## Live Session Notes

"""
    
    # Look for any existing live session notes and add links to them
    for file in sorted(live_session_dir.iterdir()):
        if file.is_file() and file.suffix == ".md" and file.stem != "Live-Session Index":
            display_name = file.stem.replace("-", " ")
            content += f"- [[{file.name}|{display_name}]]\n"
    
    # Write the index file
    with open(index_path, 'w', encoding='utf-8') as f:
        f.write(content)
        
    print(f"Created Live Session index: {index_path}")
    return True

# ======= MAIN FUNCTION ========

def main():
    """Main function to parse arguments and run the tool"""    
    parser = argparse.ArgumentParser(
        description="Notebook Tool - Convert files and generate indexes for Obsidian"
    )    
    parser.add_argument("--source", type=str, required=True, help="Directory to process - can be any level in the hierarchy")
    parser.add_argument("--convert", action="store_true", help="Convert HTML and TXT files to Markdown")
    parser.add_argument("--generate-indexes", action="store_true", help="Generate indexes for the directory structure") 
    parser.add_argument("--single-index", action="store_true", help="Regenerate only the index for the directory specified by --source")
    parser.add_argument("--generate-video-links", action="store_true", help="Generate shareable links for videos in OneDrive and create reference notes")
    parser.add_argument("--resources-path", type=str, help="Path to the OneDrive resources folder (required for --generate-video-links)")
    parser.add_argument("--onedrive-path", type=str, default="/Education/MBA Resources", help="Path to videos in OneDrive (default: /Education/MBA Resources)")
    parser.add_argument("--all", action="store_true", help="Perform all operations (conversion, index generation, and video links)")
    args = parser.parse_args()
    
    # Validate arguments
    if not (args.convert or args.generate_indexes or args.generate_video_links or args.all or args.single_index):
        parser.print_help()
        print("\nError: At least one action (--convert, --generate-indexes, --single-index, --generate-video-links, or --all) must be specified.")
        return
        
    # Validate video link arguments
    if args.generate_video_links and not args.resources_path and not args.all:
        parser.print_help()
        print("\nError: --resources-path is required when using --generate-video-links")
        return
    
    source_path = Path(args.source).resolve()
    
    if not source_path.exists() or not source_path.is_dir():
        print(f"Error: Source directory '{source_path}' does not exist or is not a directory.")
        return
    
    # Process single index if specified
    if args.single_index:
        # Just use the source path directly
        print(f"\n== Regenerating single index for {source_path} ==\n")
        process_single_index(source_path)
        print("\n== Single index regeneration complete ==")
        
        # Since single-index is now a standalone operation, return after processing
        return
    
    # Execute requested actions for full directory or individual operations
    if args.all:
        # When using --all flag, perform all operations
        print(f"\n== Copying Templater folder to {source_path} ==\n")
        copy_templater_folder(source_path)
        
        print(f"\n== Converting files in {source_path} ==\n")
        process_directory_conversion(source_path)
        
        print(f"\n== Generating indexes for {source_path} ==\n")
        # Pass True for create_live_session when using --all flag
        process_directory_index(source_path, create_live_session=True)
        
        # Also generate video links if resources_path is provided
        if args.resources_path:
            resources_path = Path(args.resources_path).resolve()
            if resources_path.exists() and resources_path.is_dir():
                print(f"\n== Generating video links from OneDrive to {source_path} ==\n")
                generate_video_links(source_path, resources_path, args.onedrive_path)
            else:
                print(f"Warning: Resources directory '{resources_path}' does not exist or is not a directory. Skipping video link generation.")
        else:
            # Try to use a default resources path
            default_resources = Path("C:/Users/danielshue/OneDrive/Education/MBA Resources")
            if default_resources.exists() and default_resources.is_dir():
                print(f"\n== Generating video links from OneDrive to {source_path} (using default resources path) ==\n")
                generate_video_links(source_path, default_resources, args.onedrive_path)
            else:
                print("Warning: No resources path provided and default path not found. Skipping video link generation.")
    else:
        # Handle individual operations
        if args.convert:
            print(f"\n== Converting files in {source_path} ==\n")
            process_directory_conversion(source_path)
        
        if args.generate_video_links:
            if args.resources_path:
                resources_path = Path(args.resources_path).resolve()
                if resources_path.exists() and resources_path.is_dir():
                    print(f"\n== Generating video links from OneDrive to {source_path} ==\n")
                    generate_video_links(source_path, resources_path, args.onedrive_path)
                else:
                    print(f"Error: Resources directory '{resources_path}' does not exist or is not a directory.")
            else:
                print("Error: --resources-path is required when using --generate-video-links")
        
        if args.generate_indexes:
            print(f"\n== Generating indexes for {source_path} ==\n")
            process_directory_index(source_path)
    
    print("\n== Processing complete ==")

if __name__ == "__main__":
    main()

# ======= VIDEO LINKS GENERATION FUNCTIONS ========

def authenticate_graph_api(client_id, authority, scopes):
    """Authenticate to Microsoft Graph API and return access token."""
    try:
        from msal import PublicClientApplication
        app = PublicClientApplication(client_id, authority=authority)
        result = app.acquire_token_interactive(scopes)
        return result['access_token']
    except ImportError:
        print("MSAL library not found. Please install with: pip install msal")
        return None
    except Exception as e:
        print(f"Authentication error: {e}")
        return None

def get_onedrive_items(access_token, folder_path, base_url="https://graph.microsoft.com/v1.0"):
    """Get all items within a OneDrive folder recursively."""
    import requests
    headers = {"Authorization": f"Bearer {access_token}"}
    all_items = []
    
    def fetch_folder_items(folder_path):
        encoded_path = folder_path.replace('/', '%2F')
        url = f"{base_url}/me/drive/root:{encoded_path}:/children"
        items = []
        
        while url:
            resp = requests.get(url, headers=headers)
            if resp.status_code != 200:
                print(f"Error fetching items from {folder_path}: {resp.text}")
                break
                
            data = resp.json()
            items.extend(data.get('value', []))
            
            # Check for next page
            url = data.get('@odata.nextLink', None)
            
        return items

    def process_folder(folder_path):
        print(f"Processing OneDrive folder: {folder_path}")
        items = fetch_folder_items(folder_path)
        
        for item in items:
            if item.get('file', None):
                # It's a file
                all_items.append(item)
            elif item.get('folder', None):
                # It's a folder, process recursively
                subfolder_path = f"{folder_path}/{item['name']}"
                process_folder(subfolder_path)
    
    # Start recursive processing
    process_folder(folder_path)
    return all_items

def create_share_link(file_id, access_token, base_url="https://graph.microsoft.com/v1.0"):
    """Create a shareable link for the file with specified ID."""
    import requests
    headers = {"Authorization": f"Bearer {access_token}"}
    url = f"{base_url}/me/drive/items/{file_id}/createLink"
    resp = requests.post(url, headers=headers, json={"type": "view", "scope": "anonymous"})
    if resp.status_code == 200:
        return resp.json()['link']['webUrl']
    return None

def infer_course_and_program(path):
    """Attempt to infer course and program from file path."""
    from pathlib import Path
    path_parts = Path(path).parts
    
    # MBA Program structure:
    # - Program folders (Value Chain Management, Financial Management, etc.)
    # - Course folders within programs
    # - Module folders (01_xxx, 02_xxx)
    # - Lesson folders within modules
    # - Class folders (sometimes)
    
    # Initialize with default values
    info = {
        'program': 'MBA Program',
        'course': 'Unknown Course',
        'module': None,
        'lesson': None,
        'class': None
    }
    
    # Known program folders from your MBA structure
    program_folders = [
        'Value Chain Management',
        'Financial Management',
        'Focus Area Specialization',
        'Strategic Leadership and Management',
        'Managerial Economics and Business Analysis'
    ]
    
    # Try to identify components from path
    path_str = str(path).lower()
    
    # First, identify the program
    for program in program_folders:
        if program.lower() in path_str:
            info['program'] = program
            break
    
    # Find course by examining path parts
    for i, part in enumerate(path_parts):
        # Look for program folder, and the next folder is usually the course
        if part.lower() in [p.lower() for p in program_folders] and i+1 < len(path_parts):
            info['course'] = path_parts[i+1]
            # Once we find the course, break to avoid further matches
            break
            
    # Extract module info - typically follows pattern like 01_module-name
    module_match = re.search(r'(\d+[_\-][\w-]+)', path_str)
    if module_match:
        module_part = module_match.group(1)
        # Clean up the module name
        module_name = module_part.replace('_', ' ').replace('-', ' ').title()
        # If it starts with a number, format as "Module X"
        if re.match(r'^\d+\s', module_name):
            num = module_name.split()[0]
            module_name = f"Module {num}"
        info['module'] = module_name
    
    # Extract lesson info - usually after module in path
    for i, part in enumerate(path_parts):
        if module_match and module_match.group(1).lower() in part.lower() and i+1 < len(path_parts):
            potential_lesson = path_parts[i+1]
            # Clean up lesson name
            lesson_name = potential_lesson.replace('_', ' ').replace('-', ' ').title()
            # Remove numbers at start if present
            lesson_name = re.sub(r'^\d+[\s_-]*', '', lesson_name)
            if lesson_name:
                info['lesson'] = lesson_name
    
    # Look for class identifiers
    class_match = re.search(r'class[\s-_]*(\w+)', path_str, re.IGNORECASE)
    if class_match:
        info['class'] = f"Class {class_match.group(1)}"
    
    # If we couldn't identify specific components, try to extract meaningful names
    # from the path parts directly
    if info['course'] == 'Unknown Course':
        for part in path_parts:
            clean_part = part.replace('_', ' ').replace('-', ' ').title()
            if any(keyword in clean_part.lower() for keyword in ['accounting', 'economics', 'finance', 'marketing', 'operations', 'management']):
                info['course'] = clean_part
                break
    
    print(f"Extracted metadata from path: {info}")
    return info

def create_video_reference_note(video_path, share_link, vault_root, templates):
    """Create a markdown note in the Obsidian vault for the video using the Templater Video Reference Note template."""
    from pathlib import Path
    import os

    # If video_path is a string, convert it to Path
    if isinstance(video_path, str):
        video_path = Path(video_path)
    
    # Extract the relative path from the resources folder
    try:
        # Try to extract the common path components that match the MBA folder structure
        rel_parts = []
        for part in video_path.parts:
            if any(part.lower() in program.lower() for program in ["value chain", "financial", "strategic leadership", "managerial economics"]):
                rel_parts = video_path.parts[video_path.parts.index(part):]
                break
        
        if rel_parts:
            rel_path = Path(*rel_parts)
        else:
            # Fallback to just using the filename
            rel_path = Path(video_path.name)
            
    except Exception:
        # Just use the filename if we can't determine a proper path
        rel_path = Path(video_path.name)
        
    # Determine the corresponding vault path, preserving the folder structure
    vault_path = vault_root / rel_path
    vault_dir = vault_path.parent
    
    # Create the directories if they don't exist
    os.makedirs(vault_dir, exist_ok=True)
    
    # Prepare the markdown file name - same name as video but with .md extension
    video_name = video_path.stem
    note_name = f"{video_name}.md"
    note_path = vault_dir / note_name
    
    # Infer course and program information from the file path
    path_info = infer_course_and_program(rel_path)
    
    # Get the template file path
    template_path = vault_root / "Templater" / "MBA" / "Video Reference Note.md"
    
    # Check if the template exists
    if not os.path.exists(template_path):
        print(f"Video Reference Note template not found at {template_path}. Using default template.")
        # Use default template content if the Templater template isn't available
        
        # Build the frontmatter based on the template and path information
        frontmatter = {
            'auto-generated-state': 'writable',
            'template-type': 'video-reference',
            'template-description': 'Links to a video file stored in OneDrive for improved sharing and accessibility.',
            'type': 'reference',  # Match the template type
            'title': video_name,
            'video-link': share_link,
            'onedrive-path': str(video_path),
            'date-created': datetime.now().strftime('%Y-%m-%d'),
            'tags': ['video', 'onedrive', 'course-materials'],
            'program': path_info['program'],
            'course': path_info['course']
        }
        
        # Add module, lesson, and class if available
        if path_info['module']:
            frontmatter['module'] = path_info['module']
        if path_info['lesson']:
            frontmatter['lesson'] = path_info['lesson']
        if path_info['class']:
            frontmatter['class'] = path_info['class']
        
        # Format the frontmatter as YAML
        frontmatter_text = "---\n"
        for key, value in frontmatter.items():
            if isinstance(value, list):
                frontmatter_text += f"{key}: \n"
                for item in value:
                    frontmatter_text += f" - {item}\n"
            else:
                frontmatter_text += f"{key}: {value}\n"
        frontmatter_text += "---\n\n"
        
        # Create the main content
        content = f"""# {video_name}

This note links to a video file that is stored in OneDrive for improved sharing and accessibility.

## Video

[Watch Video]({share_link})

## Notes

Add your notes about the video here.

"""
        
        full_content = frontmatter_text + content
    else:
        # Read the template file
        with open(template_path, 'r', encoding='utf-8') as f:
            template_content = f.read()
        
        # Remove Templater syntax blocks - everything between <%* and %>
        template_content = re.sub(r'<%\*(.*?)%>', '', template_content, flags=re.DOTALL)
        
        # Current date in required format
        today = datetime.now().strftime('%Y-%m-%d')
        
        # Replace template variables with actual values
        # The template uses <%= variable %> syntax
        class_name = path_info.get('module', path_info.get('lesson', 'Unknown Class'))
        
        template_content = template_content.replace('<%= title %>', video_name)
        template_content = template_content.replace('<%= course %>', path_info['course'])
        template_content = template_content.replace('<%= program %>', path_info['program'])
        template_content = template_content.replace('<%= className %>', class_name)
        template_content = template_content.replace('<%= source %>', 'OneDrive')
        template_content = template_content.replace('<%= video_url %>', share_link)
        template_content = template_content.replace('<%= today %>', today)
        
        full_content = template_content
    
    # Check if the file already exists
    if os.path.exists(note_path):
        print(f"Note already exists at {note_path}. Appending OneDrive link.")
        # Read existing content
        with open(note_path, 'r', encoding='utf-8') as f:
            existing_content = f.read()
        
        # Check if the link is already in the file
        if share_link not in existing_content:
            # Find where to insert the link
            if "## Video" in existing_content:
                # Insert after the Video heading
                modified_content = existing_content.replace(
                    "## Video", 
                    f"## Video\n\n[Watch Video]({share_link})"
                )
            else:
                # Append to the end
                modified_content = existing_content + f"\n\n## Video\n\n[Watch Video]({share_link})\n"
                
            # Write the modified content
            with open(note_path, 'w', encoding='utf-8') as f:
                f.write(modified_content)
        return note_path
    
    # Write the file
    with open(note_path, 'w', encoding='utf-8') as f:
        f.write(full_content)
    
    return note_path

def generate_video_links(vault_root, resources_root, onedrive_base_path):
    """Generate shareable links for videos in OneDrive and create notes in the vault."""
    import json
    from pathlib import Path
    
    # Configuration for Microsoft Graph API
    CLIENT_ID = "489ad055-e4b0-4898-af27-53506ce83db7"
    AUTHORITY = "https://login.microsoftonline.com/common"
    SCOPES = ["Files.ReadWrite.All", "Sites.Read.All"]
    
    print("Starting video link generation process...")
    
    # Authenticate to Microsoft Graph API
    access_token = authenticate_graph_api(CLIENT_ID, AUTHORITY, SCOPES)
    if not access_token:
        print("Failed to authenticate with Microsoft Graph API. Cannot generate video links.")
        return False
    
    print("Authenticated to Microsoft Graph API")
    processed_videos = []
    errors = []
    
    # Get all items from OneDrive
    print(f"Fetching items from OneDrive path: {onedrive_base_path}")
    onedrive_items = get_onedrive_items(access_token, onedrive_base_path)
    print(f"Found {len(onedrive_items)} items in OneDrive")
    
    # Filter for video files
    video_extensions = {'.mp4', '.mov', '.avi', '.mkv', '.webm'}
    video_items = [
        item for item in onedrive_items if 
        item.get('file', {}).get('mimeType', '').startswith('video/') or
        Path(item.get('name', '')).suffix.lower() in video_extensions
    ]
    print(f"Found {len(video_items)} video files in OneDrive")
    
    # Process each video
    for item in video_items:
        try:
            item_path = item.get('parentReference', {}).get('path', '') + '/' + item['name']
            
            # Extract the path relative to onedrive_base_path
            if onedrive_base_path in item_path:
                rel_path = item_path[item_path.find(onedrive_base_path) + len(onedrive_base_path):].lstrip('/')
            else:
                rel_path = item['name']
                
            print(f"Processing video: {rel_path}")
            
            # Create local file path reference
            local_path = resources_root / rel_path.replace('/', os.path.sep)
            
            # Create a shareable link
            share_link = create_share_link(item['id'], access_token)
            if not share_link:
                print(f"Could not create share link for: {rel_path}")
                errors.append(f"Could not create share link: {rel_path}")
                continue
            
            print(f"Created shareable link: {share_link}")
            
            # Create a markdown note in the vault
            note_path = create_video_reference_note(local_path, share_link, vault_root, METADATA_TEMPLATES)
            print(f"Created/updated markdown note: {note_path}")
            
            processed_videos.append({
                'file': rel_path,
                'share_link': share_link,
                'note_path': str(note_path),
                'modified_date': datetime.now().strftime('%Y-%m-%d')
            })
            
        except Exception as e:
            print(f"Error processing {item.get('name', 'unknown')}: {str(e)}")
            errors.append(f"Error: {item.get('name', 'unknown')} - {str(e)}")
    
    # Save results to a JSON file for reference
    results_file = 'video_links_results.json'
    
    # Check if the file exists and update it if so
    existing_results = {}
    if os.path.exists(results_file):
        try:
            with open(results_file, 'r') as f:
                existing_results = json.load(f)
                
            # Merge the existing processed videos with the new ones
            if 'processed_videos' in existing_results:
                # Create a map of file paths to processed videos
                existing_map = {v['file']: v for v in existing_results['processed_videos']}
                
                # Update the map with new processed videos
                for video in processed_videos:
                    existing_map[video['file']] = video
                
                # Convert back to list
                all_videos = list(existing_map.values())
            else:
                all_videos = processed_videos
                
            # Update the results
            existing_results['processed_videos'] = all_videos
            existing_results['last_run'] = datetime.now().strftime('%Y-%m-%d')
            
        except Exception as e:
            print(f"Error updating existing results: {e}")
            existing_results = {
                'processed_videos': processed_videos,
                'errors': errors,
                'timestamp': datetime.now().strftime('%Y-%m-%d')
            }
    else:
        existing_results = {
            'processed_videos': processed_videos,
            'errors': errors,
            'timestamp': datetime.now().strftime('%Y-%m-%d')
        }
    
    # Save all results including any errors from this run
    existing_results['errors'] = errors
    
    # Write the updated results
    with open(results_file, 'w') as f:
        json.dump(existing_results, f, indent=2)
    
    # Print summary
    print("\n===== Video Link Generation Summary =====")
    print(f"Processed {len(processed_videos)} videos")
    print(f"Total videos with links: {len(existing_results.get('processed_videos', []))}")
    if errors:
        print(f"Encountered {len(errors)} errors:")
        for error in errors:
            print(f"  - {error}")
    
    return len(processed_videos) > 0
