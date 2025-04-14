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
   - Supports a 4-level hierarchy: Main → Course → Module → Lesson
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
  - Course Folders (course-index)
    - Module Folders (module-index)
      - Lesson Folders (lesson-index)
        - Content Files (readings, videos, transcripts, etc.)

Usage:
-------
  python mba_notebook_tool.py --convert --source <path>  # Convert files
  python mba_notebook_tool.py --generate-index --source <path>  # Generate indexes
  python mba_notebook_tool.py --all --source <path>  # Do both operations
"""

import os
import argparse
import re
from pathlib import Path
import html2text
import shutil
import sys
from datetime import datetime

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

def get_index_type(depth):
    """Determine the type of index based on directory depth"""
    return ["main-index", "course-index", "module-index", "lesson-index"][depth] if depth < 4 else "lesson-index"

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
    
    # Get parent folder
    parent_folder = folder.parent
    parent_name = parent_folder.name.replace("-", " ").title()
    parent_index_filename = f"{parent_folder.name}-Index.md"
    
    if depth == 3:  # Lesson level - link back to module
        return f"[← Back to Module Index](../)"
    elif depth == 2:  # Module level - link back to course
        return f"[← Back to Course Index](../)"
    elif depth == 1:  # Course level - link back to main
        return f"[← Back to Main Index](../)"
    return ""

def write_index(folder: Path, depth: int):
    """Write an index file for the given folder"""
    index_type = get_index_type(depth)
    folder_name_formatted = folder.name.replace("-", " ").title().replace(" ", "-")
    index_file = folder / f"{folder_name_formatted}-Index.md"
    if index_file.exists():
        with index_file.open("r", encoding="utf-8") as f:
            content = f.read()
            frontmatter_match = re.search(r'---\s(.*?)\s---', content, re.DOTALL)
            if frontmatter_match:
                frontmatter = frontmatter_match.group(1)
                if re.search(r'auto-generated-state\s*:\s*readonly', frontmatter, re.IGNORECASE):
                    print(f"Skipping readonly index: {index_file}")
                    return

    backlink = build_backlink(depth, folder)

    if index_type == "main-index":
        metadata = f"---\nauto-generated-state: writable\ntemplate-type: main-index\ntemplate-description: Top-level folder for the entire MBA program.\ntitle: MBA Program Index\ntype: index\nindex-type: main\ntags: [index, mba]\ndate: {datetime.now().strftime('%Y-%m-%d')}\n---"
    elif index_type == "course-index":
        metadata = f"---\nauto-generated-state: writable\ntemplate-type: course-index\ntemplate-description: Top-level folder for a single course.\ntitle: {title}\ntype: index\nindex-type: course\ntags: [index, course]\ndate: {datetime.now().strftime('%Y-%m-%d')}\nbacklinks: [MBA Program Index](../Mba-Index.md)\ncourse: {title}\n---"
    elif index_type == "module-index":
        metadata = f"---\nauto-generated-state: writable\ntemplate-type: module-index\ntemplate-description: Groups together lessons or topics within a course.\ntitle: {title}\ntype: index\nindex-type: module\ntags: [index, module]\ndate: {datetime.now().strftime('%Y-%m-%d')}\nbacklinks: [{course_title} Index](../{course_title.replace(' ', '-')}-Index.md)\ncourse: {course_title}\nmodule: {title}\ncontains:\n  assignments: 0\n  notes: 0\n  quizzes: 0\n  readings: 0\n  transcripts: 0\n  videos: 0\n---"
    elif index_type == "lesson-index":
        metadata = f"---\nauto-generated-state: writable\ntemplate-type: lesson-index\ntemplate-description: A single lesson's landing page.\ntitle: {title}\ntype: index\nindex-type: lesson\ntags: [lesson, course-materials]\ndate: {datetime.now().strftime('%Y-%m-%d')}\nbacklinks: [{module_title} Index](../{module_title.replace(' ', '-')}-Index.md)\ncourse: {course_title}\nlesson: {title}\nstatus: complete\nconcepts:\n  - \ntopics:\n  - \n---"
    elif index_type == "live-session-note":
        metadata = f"---\nauto-generated-state: writable\ntemplate-type: live-session-note\ntemplate-description: Notes for a live session.\ntitle: {title}\ntype: live-session\ntags: [live-session, notes, interactive]\ndate: {datetime.now().strftime('%Y-%m-%d')}\nconcepts:\n  - \nrelated:\n  - \nstatus: complete\n---"
    else:
        metadata = f"---\nauto-generated-state: writable\ntemplate-type: {index_type}\ntitle: {title}\n---"

    lines = [metadata]

    lines.append(f"\n# {title}")

    if backlink:
        lines.append(backlink)

    if index_type == "main-index":
        lines.append("\n## Courses\n")
        for item in sorted(folder.iterdir()):
            if item.is_dir() and should_include(item):
                name = item.name.replace("-", " ").title()
                item_index_filename = f"{item.name}-Index"
                # URL-encode the folder name for proper linking
                encoded_folder_name = item.name.replace(" ", "%20")
                lines.append(f"- {ICONS['folder']} [{name}]({encoded_folder_name}/{item_index_filename}.md)")

    elif index_type == "course-index":
        lines.append("\n## Modules\n")
        for item in sorted(folder.iterdir()):
            if item.is_dir() and should_include(item):
                name = item.name.replace("-", " ").title()
                item_index_filename = f"{item.name}-Index"
                # URL-encode the folder name for proper linking
                encoded_folder_name = item.name.replace(" ", "%20")
                lines.append(f"- {ICONS['folder']} [{name}]({encoded_folder_name}/{item_index_filename}.md)")

    elif index_type == "module-index":
        live_session_dir = folder / "Live Session"
        if live_session_dir.exists() and should_include(live_session_dir):
            lines.append("\n## Live Session\n")
            lines.append(f"- {ICONS['folder']} [Live Session](Live%20Session/Live-Session-Index.md)")

        lines.append("\n## Lessons\n")
        for item in sorted(folder.iterdir()):
            if item.is_dir() and should_include(item) and item.name != "Live Session":
                name = item.name.replace("-", " ").title()
                item_index_filename = f"{item.name}-Index"
                # URL-encode the folder name for proper linking
                encoded_folder_name = item.name.replace(" ", "%20")
                lines.append(f"- {ICONS['folder']} [{name}]({encoded_folder_name}/{item_index_filename}.md)")

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
    if root.name == "Live Session" and depth == 3:
        return
        
    # Generate the appropriate index for this folder based on depth
    write_index(root, depth)# If at module level (depth 2) and create_live_session flag is True, create a "Live Session" directory
    if depth == 2 and create_live_session:
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
    # depth 0 = main index - proceed to courses
    # depth 1 = course index - proceed to modules
    # depth 2 = module index - proceed to lessons
    # depth 3 = lesson index - don't go deeper (stop recursion)
    if depth < 3:  # Only process children for main, course, and module levels
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
    parser.add_argument("--all", action="store_true", help="Perform both conversion and index generation")
    args = parser.parse_args()
      # Validate arguments
    if not (args.convert or args.generate_indexes or args.all or args.single_index):
        parser.print_help()
        print("\nError: At least one action (--convert, --generate-indexes, --single-index, or --all) must be specified.")
        return
    
    source_path = Path(args.source).resolve()
    
    if not source_path.exists() or not source_path.is_dir():
        print(f"Error: Source directory '{source_path}' does not exist or is not a directory.")
        return    # Process single index if specified
    if args.single_index:
        # Just use the source path directly
        print(f"\n== Regenerating single index for {source_path} ==\n")
        process_single_index(source_path)
        print("\n== Single index regeneration complete ==")
        
        # Since single-index is now a standalone operation, return after processing
        return
      # Execute requested actions for full directory or individual operations
    if args.all:
        # When using --all flag, copy Templater folder, then convert files, then generate indexes
        print(f"\n== Copying Templater folder to {source_path} ==\n")
        copy_templater_folder(source_path)
        print(f"\n== Converting files in {source_path} ==\n")
        process_directory_conversion(source_path)
        
        print(f"\n== Generating indexes for {source_path} ==\n")
        # Pass True for create_live_session when using --all flag
        process_directory_index(source_path, create_live_session=True)    
    else:
        # Handle individual operations
        if args.convert:
            print(f"\n== Converting files in {source_path} ==\n")
            process_directory_conversion(source_path)
        
        if args.generate_indexes:
            print(f"\n== Generating indexes for {source_path} ==\n")
            process_directory_index(source_path)
    
    print("\n== Processing complete ==")

if __name__ == "__main__":
    main()
