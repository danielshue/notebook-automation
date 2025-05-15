"""
DEPRECATED: This script has been superseded by a new CLI version.
Please use the updated CLI in mba_notebook_automation/cli/tag_manager.py for future work.

General Tag Management Tool
--------------------------
This utility helps manage and organize nested tags in a directory of markdown files.
It provides functionality to analyze, validate, suggest, and refactor tags across your notes.

Features:
- Analyze tag usage and generate reports
- Suggest tags for individual notes
- Refactor tags across multiple files

Usage Examples:
  python tag_manager.py analyze --vault /path/to/notes
  python tag_manager.py refactor --vault /path/to/notes --old "tag/old" --new "tag/new"
  python tag_manager.py suggest --vault /path/to/notes --file "path/to/note.md"
  python tag_manager.py --help
"""

import os
import re
import yaml
import argparse
import logging
from collections import defaultdict, Counter
from typing import Dict, List, Set, Tuple, Optional, Any

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger("ObsidianTagManager")

class ObsidianTagManager:
    """
    Manager for Obsidian vault tags with special focus on MBA content organization.
    
    This class provides methods to analyze tag usage, validate tag structure,
    suggest tags based on content, and refactor tags across the vault.
    """
    
    def __init__(self, vault_path: str):
        """
        Initialize the tag manager.
        
        Args:
            vault_path (str): Path to the Obsidian vault
        """
        self.vault_path = os.path.expanduser(vault_path)
        self.tag_counts = Counter()
        self.tag_hierarchy = defaultdict(dict)
        self.files_by_tag = defaultdict(list)
        
        # MBA course-specific tag patterns
        self.mba_courses = {
            'finance': ['corporate finance', 'investment', 'valuation', 'risk'],
            'marketing': ['digital', 'strategy', 'consumer', 'brand', 'market research'],
            'accounting': ['financial', 'managerial', 'audit', 'tax', 'statement'],
            'strategy': ['competitive', 'global', 'innovation', 'business model'],
            'operations': ['supply chain', 'process', 'project management', 'quality'],
            'leadership': ['organizational behavior', 'change management', 'teams']
        }

    def analyze_vault(self) -> Dict[str, Any]:
        """
        Analyze the tag structure and usage in the vault.
        
        Scans all markdown files in the vault, extracts tags, and builds
        statistics about tag usage.
        
        Returns:
            Dict[str, Any]: Analysis results including counts, hierarchies, and files
        """
        logger.info(f"Analyzing vault at {self.vault_path}")
        file_count = 0
        
        for root, _, files in os.walk(self.vault_path):
            for file in files:
                if file.endswith('.md'):
                    file_path = os.path.join(root, file)
                    rel_path = os.path.relpath(file_path, self.vault_path)
                    
                    try:
                        tags = self._extract_tags_from_file(file_path)
                        if tags:
                            file_count += 1
                            for tag in tags:
                                self.tag_counts[tag] += 1
                                self.files_by_tag[tag].append(rel_path)
                                self._add_to_hierarchy(tag)
                    except Exception as e:
                        logger.error(f"Error processing {file_path}: {e}")
        
        logger.info(f"Analyzed {file_count} files with tags")
        return {
            'tag_counts': self.tag_counts,
            'tag_hierarchy': self.tag_hierarchy,
            'files_by_tag': self.files_by_tag
        }
    
    def _extract_tags_from_file(self, file_path: str) -> List[str]:
        """
        Extract tags from an Obsidian markdown file.
        
        Looks for tags in both YAML frontmatter and inline Markdown tags.
        
        Args:
            file_path (str): Path to the markdown file
            
        Returns:
            List[str]: List of tags found in the file
            
        Raises:
            IOError: If the file cannot be read
            yaml.YAMLError: If the frontmatter is malformed
        """
        tags = []
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
            
        # Extract YAML frontmatter tags
        yaml_match = re.search(r'^---\s+(.*?)\s+---', content, re.DOTALL)
        if yaml_match:
            try:
                frontmatter = yaml.safe_load(yaml_match.group(1))
                if frontmatter and 'tags' in frontmatter:
                    if isinstance(frontmatter['tags'], list):
                        tags.extend(frontmatter['tags'])
                    else:
                        tags.append(frontmatter['tags'])
            except yaml.YAMLError as e:
                logger.warning(f"YAML error in {file_path}: {e}")
                
        # Extract inline tags
        inline_tags = re.findall(r'#([a-zA-Z0-9_/\-]+)', content)
        tags.extend(inline_tags)
        
        return tags
    
    def _add_to_hierarchy(self, tag: str) -> None:
        """
        Add a tag to the hierarchy structure.
        
        Args:
            tag (str): The tag to add
        """
        parts = tag.split('/')
        current = self.tag_hierarchy
        
        for i, part in enumerate(parts):
            if i == len(parts) - 1:
                if part not in current:
                    current[part] = {}
            else:
                if part not in current:
                    current[part] = {}
                current = current[part]
    
    def generate_markdown_report(self, output_file: str = "tag-analysis.md") -> str:
        """
        Generate a markdown report of tag analysis.
        
        Args:
            output_file (str): Filename for the report
            
        Returns:
            str: Path to the generated report file
        """
        output_path = os.path.join(self.vault_path, output_file)
        
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write("# Tag Analysis Report\n\n")
            
            # Overall statistics
            f.write("## Overall Statistics\n\n")
            f.write(f"- **Total unique tags**: {len(self.tag_counts)}\n")
            f.write(f"- **Top-level categories**: {len(self.tag_hierarchy)}\n\n")
            
            # Most used tags
            f.write("## Most Used Tags\n\n")
            f.write("| Tag | Count |\n|-----|-------|\n")
            for tag, count in self.tag_counts.most_common(20):
                f.write(f"| {tag} | {count} |\n")
            f.write("\n")
            
            # Tag hierarchy
            f.write("## Tag Hierarchy\n\n")
            f.write(self._hierarchy_to_markdown(self.tag_hierarchy))
            
        logger.info(f"Report generated at {output_path}")
        return output_path
    
    def _hierarchy_to_markdown(self, hierarchy: Dict, indent: int = 0) -> str:
        """
        Convert tag hierarchy to markdown format.
        
        Args:
            hierarchy (Dict): Nested dictionary representing tag hierarchy
            indent (int): Current indentation level
            
        Returns:
            str: Markdown formatted representation of tag hierarchy
        """
        result = []
        for key, value in sorted(hierarchy.items()):
            count = self.tag_counts.get('/'.join([key]), 0)
            count_str = f" ({count})" if count > 0 else ""
            result.append(f"{' ' * indent}- {key}{count_str}")
            if value:
                result.append(self._hierarchy_to_markdown(value, indent + 2))
        return '\n'.join(result)
    
    def suggest_tags(self, file_path: str) -> List[str]:
        """
        Suggest relevant tags for a note based on its content.
        
        Args:
            file_path (str): Path to the note file
            
        Returns:
            List[str]: List of suggested tags
        """
        suggested_tags = set()
        
        try:
            with open(os.path.join(self.vault_path, file_path), 'r', encoding='utf-8') as f:
                content = f.read().lower()
                
            # Determine the type of note
            if re.search(r'lecture|class|course', content):
                suggested_tags.add('type/note/lecture')
            elif re.search(r'case study|case analysis', content):
                suggested_tags.add('type/note/case-study')
            elif re.search(r'literature review|book|article|paper', content):
                suggested_tags.add('type/note/literature')
            elif re.search(r'meeting|discussion', content):
                suggested_tags.add('type/note/meeting')
                
            # Determine MBA course
            for course, keywords in self.mba_courses.items():
                if any(keyword in content for keyword in keywords):
                    suggested_tags.add(f'mba/course/{course}')
                    
            # Check for specific tools mentioned
            tools = {'excel': r'excel|spreadsheet', 
                    'python': r'python|pandas|numpy',
                    'r': r'\br\b|rstudio|ggplot',
                    'powerbi': r'power\s*bi',
                    'tableau': r'tableau'}
                    
            for tool, pattern in tools.items():
                if re.search(pattern, content):
                    suggested_tags.add(f'mba/tool/{tool}')
                    
        except Exception as e:
            logger.error(f"Error suggesting tags for {file_path}: {e}")
            
        return list(suggested_tags)

    def refactor_tags(self, old_tag: str, new_tag: str, dry_run: bool = True) -> Dict[str, Any]:
        """
        Refactor tags across the vault, replacing old tags with new ones.
        
        Args:
            old_tag (str): The tag to replace
            new_tag (str): The replacement tag
            dry_run (bool): If True, only simulates the changes
            
        Returns:
            Dict[str, Any]: Statistics about the changes made
        """
        if old_tag not in self.files_by_tag:
            logger.warning(f"Tag '{old_tag}' not found in vault")
            return {"files_changed": 0, "occurrences": 0}
            
        files_to_change = self.files_by_tag[old_tag]
        logger.info(f"Found {len(files_to_change)} files with tag '{old_tag}'")
        
        changed_count = 0
        occurrence_count = 0
        
        for rel_path in files_to_change:
            file_path = os.path.join(self.vault_path, rel_path)
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Replace in YAML frontmatter
                yaml_match = re.search(r'^---\s+(.*?)\s+---', content, re.DOTALL)
                if yaml_match:
                    frontmatter_text = yaml_match.group(1)
                    try:
                        frontmatter = yaml.safe_load(frontmatter_text)
                        if frontmatter and 'tags' in frontmatter:
                            modified = False
                            if isinstance(frontmatter['tags'], list):
                                if old_tag in frontmatter['tags']:
                                    frontmatter['tags'] = [new_tag if t == old_tag else t 
                                                          for t in frontmatter['tags']]
                                    modified = True
                                    occurrence_count += 1
                            else:
                                if frontmatter['tags'] == old_tag:
                                    frontmatter['tags'] = new_tag
                                    modified = True
                                    occurrence_count += 1
                                    
                            if modified and not dry_run:
                                new_frontmatter = yaml.dump(frontmatter, default_flow_style=False)
                                content = content.replace(yaml_match.group(0), 
                                                        f"---\n{new_frontmatter}---")
                                changed_count += 1
                    except yaml.YAMLError:
                        logger.warning(f"YAML error in {file_path}, skipping frontmatter")
                
                # Replace inline tags
                inline_pattern = f'#({re.escape(old_tag)}\\b)'
                inline_matches = re.findall(inline_pattern, content)
                if inline_matches:
                    occurrence_count += len(inline_matches)
                    if not dry_run:
                        content = re.sub(inline_pattern, f'#{new_tag}', content)
                        changed_count += 1
                
                if not dry_run and (inline_matches or changed_count > 0):
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write(content)
                    logger.info(f"Updated {file_path}")
                
            except Exception as e:
                logger.error(f"Error processing {file_path}: {e}")
                
        action = "Would change" if dry_run else "Changed"
        logger.info(f"{action} {occurrence_count} occurrences in {changed_count} files")
        
        return {
            "files_changed": changed_count,
            "occurrences": occurrence_count
        }

def main():
    """
    Main function to parse arguments and execute tag manager operations.
    """
    parser = argparse.ArgumentParser(
        description="""
DEPRECATED: Use the new CLI version if available.

General tag management for markdown notes. Analyze, suggest, and refactor tags in a directory.

Features:
  - Analyze tag usage and generate reports
  - Suggest tags for individual notes
  - Refactor tags across multiple files

Examples:
  python tag_manager.py analyze --vault /path/to/notes
  python tag_manager.py refactor --vault /path/to/notes --old "tag/old" --new "tag/new"
  python tag_manager.py suggest --vault /path/to/notes --file "path/to/note.md"
        """
    )
    parser.add_argument('--vault', required=True, help="Path to the root directory of markdown notes")

    subparsers = parser.add_subparsers(dest='command')

    # Analyze command
    analyze_parser = subparsers.add_parser('analyze', help="Analyze tag usage and generate a report")
    analyze_parser.add_argument('--output', default="tag-analysis.md",
                             help="Output file for the tag analysis report")

    # Suggest command
    suggest_parser = subparsers.add_parser('suggest', help="Suggest tags for a specific note")
    suggest_parser.add_argument('--file', required=True,
                              help="Path to the note file (relative to the vault root)")

    # Refactor command
    refactor_parser = subparsers.add_parser('refactor', help="Refactor tags across notes")
    refactor_parser.add_argument('--old', required=True, help="Old tag to replace")
    refactor_parser.add_argument('--new', required=True, help="New tag to use")
    refactor_parser.add_argument('--dry-run', action='store_true',
                               help="Simulate changes without modifying files")

    args = parser.parse_args()
    
    tag_manager = ObsidianTagManager(args.vault)
    
    if args.command == 'analyze':
        tag_manager.analyze_vault()
        tag_manager.generate_markdown_report(args.output)
    elif args.command == 'suggest':
        tag_manager.analyze_vault()  # Need to analyze first to build context
        suggestions = tag_manager.suggest_tags(args.file)
        print(f"Suggested tags for {args.file}:")
        for tag in suggestions:
            print(f"- {tag}")
    elif args.command == 'refactor':
        tag_manager.analyze_vault()  # Need to analyze first to know which files to change
        tag_manager.refactor_tags(args.old, args.new, args.dry_run)
    else:
        parser.print_help()

if __name__ == "__main__":
    main()