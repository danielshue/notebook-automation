#!/usr/bin/env python
"""
MBA Vault Metadata Enhancer

This script adds or updates metadata in Markdown files throughout your MBA vault.
It extracts context from the file path and content to populate YAML frontmatter
with relevant metadata fields, making it easier to organize and connect concepts.
"""

import os
import re
import yaml
from pathlib import Path
from datetime import datetime
import argparse

# Define patterns for extracting key information from filenames and content
COURSE_PATTERN = r'(.+?)(?:/|$)'
MODULE_PATTERN = r'(\d+)_(.+?)(?:/|$)'
NOTE_TYPE_PATTERNS = {
    'lecture': r'lecture|session',
    'reading': r'reading|article|paper|book|chapter',
    'assignment': r'assignment|homework|exercise|quiz|case study',
    'transcript': r'transcript',
    'summary': r'summary|overview|conclusion',
    'note': r'note'
}

def extract_topics_from_content(content):
    """Extract potential topics from headings in the content"""
    # Extract all headings (## Heading) from content
    headings = re.findall(r'^#{1,3}\s+(.+?)$', content, re.MULTILINE)
    topics = []
    
    # Clean up headings and add as potential topics
    for heading in headings[:5]:  # Limit to top 5 headings
        # Remove formatting, numbers, etc.
        clean_heading = re.sub(r'[^a-zA-Z\s]', '', heading).strip()
        if clean_heading and len(clean_heading) > 3 and clean_heading.lower() not in ['introduction', 'summary', 'conclusion', 'overview']:
            topics.append(clean_heading)
            
    return topics

def extract_concepts_from_content(content):
    """Extract potential concepts from bold and italic text in the content"""
    concepts = []
    
    # Extract bold text
    bold = re.findall(r'\*\*(.+?)\*\*', content)
    # Extract italic text
    italic = re.findall(r'\*([^*]+)\*', content)
    
    # Combine and clean up
    for text in bold + italic:
        if len(text) > 3 and len(text) < 40:  # Reasonable length for a concept
            concepts.append(text)
            
    return list(set(concepts))[:10]  # Return up to 10 unique concepts

def extract_metadata_from_path(file_path):
    """Extract metadata from the file path"""
    metadata = {}
    path_parts = file_path.parts
    
    # Extract course name from path
    for i, part in enumerate(path_parts):
        if part == "MBA" and i+1 < len(path_parts):
            course = path_parts[i+1]
            metadata['course'] = course.replace('-', ' ').title()
            break
    
    # Extract module name if it exists in path
    for part in path_parts:
        module_match = re.match(MODULE_PATTERN, part)
        if module_match:
            module_num, module_name = module_match.groups()
            metadata['module'] = f"Module {module_num}: {module_name.replace('-', ' ').title()}"
            break
    
    # Determine note type from filename
    filename = file_path.name.lower()
    for note_type, pattern in NOTE_TYPE_PATTERNS.items():
        if re.search(pattern, filename):
            metadata['type'] = note_type
            break
    
    if 'type' not in metadata:
        metadata['type'] = 'note'  # Default type
        
    # Set current date
    metadata['date'] = datetime.now().strftime('%Y-%m-%d')
    
    return metadata

def find_related_notes(root_dir, file_path, content):
    """Find potentially related notes based on content similarity"""
    related = []
    
    # Extract significant terms from content
    words = re.findall(r'\b[A-Za-z]{4,}\b', content)
    word_count = {}
    for word in words:
        word = word.lower()
        if word not in ['this', 'that', 'with', 'from', 'have', 'there']:
            word_count[word] = word_count.get(word, 0) + 1
    
    # Get top 5 significant terms
    significant_terms = sorted(word_count.items(), key=lambda x: x[1], reverse=True)[:5]
    significant_terms = [term[0] for term in significant_terms]
    
    if not significant_terms:
        return related
    
    # Find other files containing these terms
    # This is a simplified approach - in reality you'd want to use more advanced NLP
    target_dir = file_path.parent
    for i in range(2):  # Look in current directory and parent directory
        if target_dir.name == "MBA":
            break
            
        for other_file in target_dir.glob("**/*.md"):
            if other_file == file_path:
                continue
                
            try:
                with other_file.open('r', encoding='utf-8', errors='ignore') as f:
                    other_content = f.read()
                    
                match_count = 0
                for term in significant_terms:
                    if term in other_content.lower():
                        match_count += 1
                
                if match_count >= 2:  # If at least 2 significant terms match
                    rel_path = os.path.relpath(other_file, file_path.parent).replace("\\", "/")
                    related.append(rel_path)
                    
                    # Limit to 5 related files
                    if len(related) >= 5:
                        return related
                        
            except Exception:
                pass
                
        target_dir = target_dir.parent
    
    return related

def update_file_metadata(file_path, root_dir=None, dry_run=False):
    """Update the metadata for a single file"""
    if not file_path.name.endswith('.md'):
        return False
        
    if not root_dir:
        root_dir = file_path.parent
        
    try:
        with file_path.open('r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
    except Exception as e:
        print(f"Error reading {file_path}: {e}")
        return False
    
    # Extract existing frontmatter if any
    frontmatter_match = re.match(r'---\s(.*?)\s---', content, re.DOTALL)
    existing_metadata = {}
    remaining_content = content
    
    if frontmatter_match:
        # Extract existing frontmatter
        frontmatter_text = frontmatter_match.group(1)
        try:
            existing_metadata = yaml.safe_load(frontmatter_text) or {}
        except Exception:
            # If yaml parsing fails, keep the frontmatter as is
            pass
            
        # Remove existing frontmatter from content
        remaining_content = content[frontmatter_match.end():]
    
    # Extract metadata from path and content
    path_metadata = extract_metadata_from_path(file_path)
    
    # Extract title from content or filename
    title_match = re.search(r'^#\s+(.+?)$', remaining_content, re.MULTILINE)
    if title_match:
        path_metadata['title'] = title_match.group(1)
    else:
        # Use filename as title if no heading found
        path_metadata['title'] = file_path.stem.replace('-', ' ').title()
    
    # Extract topics and concepts from content
    path_metadata['topics'] = extract_topics_from_content(remaining_content)
    path_metadata['concepts'] = extract_concepts_from_content(remaining_content)
    
    # Find related notes
    path_metadata['related'] = find_related_notes(root_dir, file_path, remaining_content)
    
    # Set default status
    path_metadata['status'] = 'complete'  # Assume existing notes are complete
    
    # Merge with existing metadata, preserving user-added fields
    merged_metadata = {**path_metadata, **existing_metadata}
    
    # Generate the new frontmatter
    new_frontmatter = yaml.dump(merged_metadata, default_flow_style=False, sort_keys=False)
    new_file_content = f"---\n{new_frontmatter}---\n\n{remaining_content.lstrip()}"
    
    # Print what we're doing
    rel_path = os.path.relpath(file_path, root_dir)
    print(f"Processing: {rel_path}")
    
    if dry_run:
        print(f"Would update {rel_path} with new metadata:")
        print(f"---\n{new_frontmatter}---")
        return True
    
    # Write the updated file content
    try:
        with file_path.open('w', encoding='utf-8') as f:
            f.write(new_file_content)
        return True
    except Exception as e:
        print(f"Error writing {file_path}: {e}")
        return False

def process_directory(directory_path, dry_run=False):
    """Process all markdown files in the directory and its subdirectories"""
    success_count = 0
    total_count = 0
    
    directory_path = Path(directory_path).resolve()
    
    print(f"Processing directory: {directory_path}")
    print("This may take a while depending on the number of files...")
    
    for root, dirs, files in os.walk(directory_path):
        # Skip Templater folder
        if "Templater" in root:
            continue
            
        for file in files:
            if file.endswith('.md'):
                total_count += 1
                file_path = Path(root) / file
                
                if update_file_metadata(file_path, directory_path, dry_run):
                    success_count += 1
    
    print(f"\nProcessed {total_count} files. Successfully updated {success_count} files.")
    if dry_run:
        print("This was a dry run. No files were actually modified.")
    
    return success_count

def create_template_note(directory_path):
    """Create a template note with recommended metadata structure"""
    directory_path = Path(directory_path).resolve()
    template_file = directory_path / "metadata-template.md"
    
    metadata = {
        'title': "Note Title",
        'course': "Course Name",
        'module': "Module Name",
        'type': "lecture",
        'topics': ["topic1", "topic2", "topic3"],
        'concepts': ["concept1", "concept2", "concept3"],
        'related': ["link-to-related-note-1", "link-to-related-note-2"],
        'date': datetime.now().strftime('%Y-%m-%d'),
        'status': "in-progress"
    }
    
    content = f"""---
{yaml.dump(metadata, default_flow_style=False, sort_keys=False)}---

# Note Title

## Summary
Brief summary of the note goes here.

## Key Concepts
- First key concept
- Second key concept
- Third key concept

## Details
Main content goes here...

## Connections
How this relates to other concepts/courses...

"""
    
    with template_file.open('w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"Created template file at: {template_file}")
    return template_file

def main():
    """Main function to parse arguments and run the script"""
    parser = argparse.ArgumentParser(
        description="MBA Vault Metadata Enhancer - Add metadata to Markdown files"
    )
    parser.add_argument("--source", type=str, required=False, 
                       help="Directory to process - defaults to current MBA directory")
    parser.add_argument("--dry-run", action="store_true",
                       help="Show what would be changed without making changes")
    parser.add_argument("--create-template", action="store_true",
                       help="Create a template note with recommended metadata structure")
    args = parser.parse_args()
    
    # Default source directory
    source_path = args.source if args.source else "d:/MBA"
    
    if args.create_template:
        create_template_note(source_path)
        return
    
    process_directory(source_path, args.dry_run)

if __name__ == "__main__":
    main()
