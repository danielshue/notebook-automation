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
    parent_index_name = f"{parent_folder.name.replace('-', ' ').title().replace(' ', '-')} Index"
    
    if depth == 3:
        return f"[[../{parent_index_name}|← Back to Module Index]]"
    elif depth == 2:
        return f"[[../{parent_index_name}|← Back to Course Index]]"
    elif depth == 1:
        return f"[[../{parent_index_name}|← Back to Main Index]]"
    return ""

def write_index(folder: Path, depth: int):
    """Write an index file for the given folder"""
    index_type = get_index_type(depth)
      # The index filename uses the folder name with proper capitalization
    folder_name_formatted = folder.name.replace("-", " ").title().replace(" ", "-")
    index_file = folder / f"{folder_name_formatted} Index.md"
    if index_file.exists():
        # Check the auto-generated-state property in the frontmatter
        with index_file.open("r", encoding="utf-8") as f:
            content = f.read()
            
            # More robust check for the auto-generated-state property
            frontmatter_match = re.search(r'---\s(.*?)\s---', content, re.DOTALL)
            if frontmatter_match:
                frontmatter = frontmatter_match.group(1)
                # Check specifically for readonly state with proper regex
                if re.search(r'auto-generated-state\s*:\s*readonly', frontmatter, re.IGNORECASE):
                    print(f"Skipping readonly index: {index_file}")
                    return
                    
    backlink = build_backlink(depth, folder)
    
    # Create a nice title for the index
    if index_type == "main-index":
        title = "Main Index"
    elif index_type == "course-index":
        title = f"{folder.name.replace('-', ' ').title()} Course"
    elif index_type == "module-index":
        title = f"{folder.name.replace('-', ' ').title()} Module"
    else:
        title = f"{folder.name.replace('-', ' ').title()} Lesson"
        
    metadata = f"---\nauto-generated-state: writable\ntemplate-type: {index_type}\ntitle: {title}\n---"
    lines = [metadata]
    
    # Add title as a heading
    lines.append(f"\n# {title}")
    
    if backlink:
        lines.append(backlink)
    
    # Main index only shows course indexes
    if index_type == "main-index":
        lines.append("\n## Courses\n")
        for item in sorted(folder.iterdir()):
            if item.is_dir() and should_include(item):
                name = item.name.replace("-", " ").title()
                # Create proper index filename for course link
                item_index_filename = f"{item.name} Index"
                lines.append(f"- {ICONS['folder']} [[{item.name}/{item_index_filename}|{name}]]")
    
    # Course index only shows module indexes
    elif index_type == "course-index":
        lines.append("\n## Modules\n")
        for item in sorted(folder.iterdir()):
            if item.is_dir() and should_include(item):
                name = item.name.replace("-", " ").title()
                # Create proper index filename for module link
                item_index_filename = f"{item.name} Index"
                lines.append(f"- {ICONS['folder']} [[{item.name}/{item_index_filename}|{name}]]")
    
    # Module index only shows lesson indexes
    elif index_type == "module-index":
        lines.append("\n## Lessons\n")
        for item in sorted(folder.iterdir()):
            if item.is_dir() and should_include(item):
                name = item.name.replace("-", " ").title()
                # Create proper index filename for lesson link
                item_index_filename = f"{item.name} Index"
                lines.append(f"- {ICONS['folder']} [[{item.name}/{item_index_filename}|{name}]]")
    
    # Lesson index shows categorized files
    else:
        # Handle detailed categories for lesson indexes
        categorized = {k: [] for k in ORDER}

        for item in sorted(folder.iterdir()):
            if not should_include(item):
                continue
            if item.is_dir():
                continue  # Only list files for now
            ctype = classify_file(item)
            if ctype:
                display = item.stem.replace("-", " ").title()
                # Get appropriate tags for this file
                tags = get_tags_for_file(item, ctype)
                tags_str = " ".join(tags) if tags else ""
                # Use Obsidian style linking format with tags
                categorized[ctype].append(f"- {ICONS[ctype]} [[{item.name}|{display}]] {tags_str}")

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
        # If the paths don't have a parent-child relationship, just use the absolute path
        relative_path = index_file
    print(f"Wrote index: {relative_path}")

def walk_directory(root: Path, depth=0):
    """Walk the directory tree and generate indexes at each level"""
    if is_hidden(root):
        return
        
    # Generate the appropriate index for this folder based on depth
    write_index(root, depth)
    
    # Only process child directories if at appropriate levels
    # depth 0 = main index - proceed to courses
    # depth 1 = course index - proceed to modules
    # depth 2 = module index - proceed to lessons
    # depth 3 = lesson index - don't go deeper (stop recursion)
    if depth < 3:  # Only process children for main, course, and module levels
        for child in sorted(root.iterdir()):
            if child.is_dir() and not is_hidden(child):
                walk_directory(child, depth + 1)

def process_directory_index(source_path):
    """Process the directory tree to generate indexes"""
    root = Path(source_path).resolve()
    walk_directory(root)

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
        process_directory_index(source_path)    
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
