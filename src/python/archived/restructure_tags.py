"""
DEPRECATED: This script has been superseded by a new CLI version.
Please use the updated CLI in mba_notebook_automation/cli/restructure_tags.py for future work.

General Tag Restructuring Tool
-----------------------------
This script analyzes markdown documents in a directory, identifies their types, and applies structured hierarchical tags based on:
- Document location in the folder structure
- Content patterns and format
- Document type identification

Features:
- Analyze and restructure tags in markdown files
- Identify document types and apply hierarchical tags
- Supports batch processing of directories

Usage Examples:
  python restructure_tags.py /path/to/notes
  python restructure_tags.py --help
"""

import os
import re
import sys
import yaml
from pathlib import Path
from collections import defaultdict

def identify_document_type(file_path, content=None):
    """
    Identify document type based on content patterns and location.
    
    Args:
        file_path (str): Path to the markdown file
        content (str, optional): Content if already loaded
        
    Returns:
        dict: Dictionary of tag categories and values to apply
    """
    if content is None:
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
        except:
            return {"type": ["note"]}  # Default fallback
    
    path = Path(file_path)
    rel_path = path.parts  # Get path parts for analysis
    filename = path.name
    tags = {}
    
    # Default type is "note"
    tags["type"] = ["note"]
    
    # Program identification
    tags["program"] = ["imba"]  # Default to iMBA program
    
    # Identify term/semester
    # Look for terms in path or content like "Fall 2023", "Spring 2024", etc.
    term_patterns = [
        # Pattern for standard term formats in content
        (r"(Spring|Summer|Fall|Winter)\s+(20\d{2})", "\\2-\\1"),
        # Pattern for numeric terms like "2023-1" (Spring), "2023-2" (Summer), "2023-3" (Fall)
        (r"(20\d{2})[/-]([123])", lambda m: f"{m.group(1)}-{['spring', 'summer', 'fall'][int(m.group(2))-1]}")
    ]
    
    for pattern, replacement in term_patterns:
        term_match = re.search(pattern, content[:2000], re.IGNORECASE) or re.search(pattern, " ".join(rel_path), re.IGNORECASE)
        if term_match:
            if callable(replacement):
                term_value = replacement(term_match).lower()
            else:
                term_value = re.sub(pattern, replacement, term_match.group(0), flags=re.IGNORECASE).lower()
            tags["term"] = [term_value]
            break
    
    # Identify course codes
    course_code_pattern = r"\b([A-Z]{2,5})\s*[-_]?\s*(\d{3}[A-Z]?)\b"
    course_match = re.search(course_code_pattern, content[:1000]) or re.search(course_code_pattern, " ".join(rel_path))
    if course_match:
        course_code = f"{course_match.group(1)}{course_match.group(2)}"
        tags["course"] = [course_code]
      # Identify MBA content by path
    if "MBA" in rel_path:
        tags["mba"] = []  # MBA root tag
        
        # Course code to subject mappings
        course_code_to_subject = {
            # Finance courses
            "FIN501": ("finance", "corporate-finance"),
            "FIN571": ("finance", "investments"),
            "FIN580": ("finance", "valuation"),
            
            # Accounting courses
            "ACCY501": ("accounting", "financial"),
            "ACCY502": ("accounting", "managerial"),
            "ACCY503": ("accounting", "audit"),
            "ACCY504": ("accounting", "tax"),
            
            # Marketing courses
            "MKTG571": ("marketing", "strategy"),
            "MKTG572": ("marketing", "digital"),
            "MKTG573": ("marketing", "consumer-behavior"),
            "MKTG578": ("marketing", "analytics"),
            
            # Strategy courses
            "BADM508": ("leadership", "teams"),
            "BADM509": ("strategy", "corporate"),
            "BADM520": ("strategy", "competitive"),
            "BADM544": ("strategy", "global"),
            
            # Operations courses
            "BADM567": ("operations", "project-management"),
            "BADM566": ("operations", "supply-chain"),
            "BADM589": ("operations", "quality-management"),
            
            # Economics courses
            "ECON528": ("economics", "statistics"),
            "ECON540": ("economics", "managerial")
        }
        
        # Try to match course code from path or content
        if "course" in tags and tags["course"]:
            course_code = tags["course"][0]
            if course_code in course_code_to_subject:
                subject, subcategory = course_code_to_subject[course_code]
                tags["mba"].append("course")
                tags["mba/course"] = [subject]
                tags[f"mba/course/{subject}"] = [subcategory]
        
        # Fallback to folder name identification
        if "mba/course" not in tags:
            # Course identification by folder
            for course_type in ["Finance", "Accounting", "Marketing", "Operations", "Strategy", "Leadership", "Economics"]:
                if course_type in rel_path or course_type.lower() in rel_path:
                    tags["mba"].append("course")
                    tags["mba/course"] = [course_type.lower()]
                    break
        
        # Try to identify course subcategories
        if "mba/course" in tags:
            course = tags["mba/course"][0]
            subcategories = {
                "finance": ["corporate-finance", "investments", "valuation"],
                "accounting": ["financial", "managerial", "audit", "tax"],
                "marketing": ["digital", "strategy", "consumer-behavior"],
                "operations": ["supply-chain", "project-management", "quality-management"],
                "strategy": ["corporate", "competitive", "global"],
                "leadership": ["organizational-behavior", "change-management", "teams"]
            }
            
            # Check for subcategory keywords in path or content
            if course in subcategories:
                for subcategory in subcategories[course]:
                    # Check if subcategory term appears in path or beginning of content
                    if subcategory.replace("-", " ") in " ".join(rel_path).lower() or \
                       subcategory.replace("-", " ") in content[:1000].lower():
                        tags[f"mba/course/{course}"] = [subcategory]
                        break
    
    # Document type identification
    if "# " in content[:500]:  # Heading near top indicates a note
        if "lecture" in content.lower()[:1000] or "lecture" in " ".join(rel_path).lower():
            tags["type"] = ["note", "lecture"]
        elif "case study" in content.lower()[:1000] or "case" in " ".join(rel_path).lower():
            tags["type"] = ["note", "case-study"]
        elif any(term in content.lower()[:1000] for term in ["assignment", "homework", "exercise"]):
            tags["type"] = ["assignment"]
            if "group" in content.lower()[:1000]:
                tags["type/assignment"] = ["group"]
            else:
                tags["type/assignment"] = ["individual"]
        elif "project" in " ".join(rel_path).lower() or "project" in content.lower()[:1000]:
            tags["type"] = ["project"]
            if "complete" in content.lower()[:1000] or "finished" in content.lower()[:1000]:
                tags["type/project"] = ["completed"]
            else:
                tags["type/project"] = ["active"]
    
    # Add status tags
    if "draft" in content.lower()[:1000] or "draft" in filename.lower():
        tags["status"] = ["draft"]
    elif "archive" in " ".join(rel_path).lower() or "archive" in content.lower()[:1000]:
        tags["status"] = ["archived"]
    else:
        tags["status"] = ["active"]
    
    # Check for tool references
    for tool in ["excel", "python", "r", "powerbi", "tableau"]:
        if tool in content.lower()[:2000] or tool in " ".join(rel_path).lower():
            if "mba" in tags:
                if "tool" not in tags["mba"]:
                    tags["mba"].append("tool")
                tags["mba/tool"] = [tool]
                break
    
    # Check for skill references
    skill_keywords = {
        "quantitative": ["statistics", "calculation", "formula", "equation", "math"],
        "qualitative": ["interview", "observation", "focus group", "case study"],
        "presentation": ["slide", "present", "powerpoint", "presentation"],
        "negotiation": ["negotiation", "bargain", "deal", "contract"],
        "leadership": ["leadership", "team", "manage", "direct"]
    }
    
    for skill, keywords in skill_keywords.items():
        if any(keyword in content.lower()[:2000] for keyword in keywords):
            if "mba" in tags:
                if "skill" not in tags["mba"]:
                    tags["mba"].append("skill")
                tags["mba/skill"] = [skill]
                break
    
    return tags

def build_nested_tags(tag_dict):
    """
    Convert tag dictionary to flat list of nested tags.
    
    Args:
        tag_dict (dict): Dictionary of tag categories and values
        
    Returns:
        list: List of nested tag strings (e.g., ["type/note/lecture", "mba/course/finance"])
    """
    flat_tags = []
    
    # Process each tag category
    for category, values in tag_dict.items():
        if "/" in category:  # Already a nested path
            parent, child = category.rsplit("/", 1)
            for value in values:
                flat_tags.append(f"{parent}/{child}/{value}")
        else:  # Top level category
            for value in values:
                flat_tags.append(f"{category}/{value}")
    
    return flat_tags

def update_file_tags(file_path, simulate=False):
    """
    Update tags in a file based on its type and location.
    
    Args:
        file_path (str): Path to the markdown file to update
        simulate (bool): If True, doesn't modify files, just reports changes
        
    Returns:
        tuple: (success, old_tags, new_tags)
    """
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Extract existing frontmatter and tags
        yaml_match = re.search(r'^---\s+(.*?)\s+---', content, re.DOTALL)
        frontmatter = {}
        old_tags = []
        
        if yaml_match:
            try:
                frontmatter_text = yaml_match.group(1)
                frontmatter = yaml.safe_load(frontmatter_text) or {}
                
                if isinstance(frontmatter, dict) and 'tags' in frontmatter:
                    if isinstance(frontmatter['tags'], list):
                        old_tags = frontmatter['tags']
                    elif frontmatter['tags']:
                        old_tags = [frontmatter['tags']]
            except yaml.YAMLError:
                pass
        
        # Identify document type and generate new tags
        tag_dict = identify_document_type(file_path, content)
        new_tags = build_nested_tags(tag_dict)
        
        # Don't modify if simulate mode
        if simulate:
            return True, old_tags, new_tags
        
        # Create or update frontmatter
        if yaml_match:
            # Update existing frontmatter
            frontmatter['tags'] = new_tags
            new_frontmatter = yaml.dump(frontmatter, default_flow_style=False)
            new_content = content.replace(yaml_match.group(0), f"---\n{new_frontmatter}---")
        else:
            # Create new frontmatter
            new_frontmatter = yaml.dump({'tags': new_tags}, default_flow_style=False)
            new_content = f"---\n{new_frontmatter}---\n\n{content}"
        
        # Write changes back to file
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        
        return True, old_tags, new_tags
        
    except Exception as e:
        print(f"Error updating {file_path}: {e}")
        return False, [], []

def process_vault(vault_path, simulate=False):
    """
    Process all markdown files in the vault and update their tags.
    
    Args:
        vault_path (str): Path to the Obsidian vault root
        simulate (bool): If True, doesn't modify files, just reports changes
        
    Returns:
        dict: Statistics on changes made
    """
    stats = {
        "total_files": 0,
        "processed_files": 0,
        "files_with_tags_changed": 0,
        "errors": 0
    }
    
    mode = "SIMULATION" if simulate else "UPDATE"
    print(f"\n{mode} MODE: Processing vault at {vault_path}")
    
    for root, _, files in os.walk(vault_path):
        for file in files:
            if file.endswith('.md'):
                stats["total_files"] += 1
                file_path = os.path.join(root, file)
                rel_path = os.path.relpath(file_path, vault_path)
                
                if stats["total_files"] % 50 == 0:
                    print(f"Processing file {stats['total_files']}: {rel_path}")
                
                success, old_tags, new_tags = update_file_tags(file_path, simulate)
                
                if success:
                    stats["processed_files"] += 1
                    if set(old_tags) != set(new_tags):
                        stats["files_with_tags_changed"] += 1
                        
                        # Print changes for important files or every 10th change
                        if "dashboard" in file.lower() or stats["files_with_tags_changed"] % 10 == 0:
                            print(f"\nChanged tags in: {rel_path}")
                            print(f"  Old: {old_tags}")
                            print(f"  New: {new_tags}")
                else:
                    stats["errors"] += 1
    
    print("\nProcessing Statistics:")
    print(f"  Total files: {stats['total_files']}")
    print(f"  Successfully processed: {stats['processed_files']}")
    print(f"  Files with tags changed: {stats['files_with_tags_changed']}")
    print(f"  Errors: {stats['errors']}")
    
    return stats

if __name__ == "__main__":
    # Set default vault path or get from command line
    if os.name == 'nt':  # Windows
        default_vault = "D:\\Vault\\01_Projects\\MBA"
    else:  # WSL or Linux
        default_vault = "/mnt/d/Vault/01_Projects/MBA"
    
    vault_path = default_vault
    simulate = True  # Default to simulation mode for safety
    
    # Parse command line arguments
    if len(sys.argv) > 1:
        if sys.argv[1] in ["-h", "--help"]:
            print("Usage: python restructure_tags.py [vault_path] [--apply]")
            print("  vault_path: Path to Obsidian vault (default: MBA folder)")
            print("  --apply: Apply changes (default: simulation mode)")
            sys.exit(0)
        elif sys.argv[1] == "--apply":
            simulate = False
        else:
            vault_path = sys.argv[1]
            
    if len(sys.argv) > 2 and sys.argv[2] == "--apply":
        simulate = False
    
    # Run tag restructuring
    if not os.path.exists(vault_path):
        print(f"Error: Vault path does not exist: {vault_path}")
        sys.exit(1)
        
    stats = process_vault(vault_path, simulate)
    
    if simulate:
        print("\nThis was a simulation. Run with --apply to make actual changes.")
