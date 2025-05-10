
"""
CLI for adding nested tags to notebook files.
Generalized for any program or course structure.

This script recursively scans a folder for markdown (.md) files, extracts fields
from the YAML frontmatter, and converts them to nested tags in the format
#field/value. These tags are then added to the existing tags field or a new tags
field is created if it doesn't exist.

If a file has 'index-type' in its frontmatter, any tags will be removed as index
files should not have tags.

Example:
    $ notebook-add-nested-tags /path/to/folder --verbose
    $ notebook-add-nested-tags /path/to/folder --dry-run
    $ notebook-add-nested-tags --verbose  # Uses default notebook vault path
"""

import argparse
import re
from pathlib import Path
from typing import Dict, Any

# Absolute imports for config and path utilities
from notebook_automation.tools.utils.config import setup_logging, VAULT_LOCAL_ROOT
from notebook_automation.tools.utils.paths import normalize_wsl_path
from notebook_automation.cli.utils import (
    HEADER, OKBLUE, OKCYAN, OKGREEN, WARNING, FAIL, ENDC, BOLD, BG_BLUE, remove_timestamps_from_logger
)

import logging

try:
    from ruamel.yaml import YAML
    USE_RUAMEL = True
    yaml_parser = YAML()
    yaml_parser.preserve_quotes = True
    yaml_parser.width = 4096
except ImportError:
    import yaml as pyyaml
    USE_RUAMEL = False


class MarkdownFrontmatterProcessor:
    """Process markdown files to extract and update YAML frontmatter tags."""

    def __init__(self, dry_run: bool = False, verbose: bool = False):
        """
        Initialize the markdown frontmatter processor.
        Args:
            dry_run (bool): If True, no files will be modified.
            verbose (bool): If True, additional information will be logged.
        """
        self.dry_run = dry_run
        self.verbose = verbose
        self.yaml_frontmatter_pattern = re.compile(r'^---\s*\n(.*?)\n---\s*\n', re.DOTALL)
        self.stats = {
            'files_processed': 0,
            'files_modified': 0,
            'tags_added': 0,
            'index_files_cleared': 0,
            'files_with_errors': 0
        }
        # Fields to process for tags (generalized)
        self.tag_fields = [
            'course', 'lecture', 'topic', 'subjects', 'professor', 'university',
            'program', 'assignment', 'type', 'author'
        ]

    def process_directory(self, directory: Path) -> Dict[str, int]:
        """
        Recursively process markdown files in a directory.
        Args:
            directory (Path): The directory to process.
        Returns:
            Dict[str, int]: Statistics about the processing.
        """
        for item in directory.glob('**/*.md'):
            if item.is_file():
                try:
                    self.process_file(item)
                except Exception as e:
                    logger.error(f"Error processing {item}: {str(e)}")
                    self.stats['files_with_errors'] += 1
        return self.stats

    def process_file(self, file_path: Path) -> None:
        """
        Process a single markdown file.
        Args:
            file_path (Path): Path to the markdown file.
        """
        from notebook_automation.cli.utils import OKGREEN, OKCYAN, WARNING, FAIL, ENDC
        self.stats['files_processed'] += 1
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
        except UnicodeDecodeError:
            with open(file_path, 'r', encoding='latin1') as f:
                content = f.read()
        match = self.yaml_frontmatter_pattern.search(content)
        if not match:
            if self.verbose:
                print(f"{WARNING}No YAML frontmatter found in: {file_path}{ENDC}")
            return
        yaml_content = match.group(1)
        try:
            if USE_RUAMEL:
                from io import StringIO
                yaml_stream = StringIO(yaml_content)
                frontmatter = yaml_parser.load(yaml_stream) or {}
            else:
                frontmatter = pyyaml.safe_load(yaml_content) or {}
        except Exception as e:
            print(f"{FAIL}Error parsing YAML in {file_path}: {str(e)}{ENDC}")
            self.stats['files_with_errors'] += 1
            return
        # Check if this is an index file
        if 'index-type' in frontmatter:
            if 'tags' in frontmatter and frontmatter['tags']:
                if self.verbose:
                    print(f"{OKCYAN}Clearing tags from index file: {file_path}{ENDC}")
                if not self.dry_run:
                    frontmatter['tags'] = []
                    self.update_file_frontmatter(file_path, content, frontmatter, match.start(1), match.end(1))
                    self.stats['index_files_cleared'] += 1
                    self.stats['files_modified'] += 1
            else:
                if self.verbose:
                    print(f"{OKCYAN}Index file (no tags to clear): {file_path}{ENDC}")
            return
        # Extract tags from frontmatter
        existing_tags = set()
        if 'tags' in frontmatter and frontmatter['tags']:
            if isinstance(frontmatter['tags'], list):
                existing_tags = set(frontmatter['tags'])
            elif isinstance(frontmatter['tags'], str):
                tags = frontmatter['tags'].split(',')
                existing_tags = {tag.strip() for tag in tags}
        # Generate new tags from frontmatter fields
        new_tags = set()
        for field in self.tag_fields:
            if field in frontmatter and frontmatter[field]:
                field_value = frontmatter[field]
                if isinstance(field_value, list):
                    for item in field_value:
                        if item:
                            tag = f"{field}/{str(item).strip()}"
                            new_tags.add(tag)
                else:
                    tag = f"{field}/{str(field_value).strip()}"
                    new_tags.add(tag)
        tags_to_add = new_tags - existing_tags
        if self.verbose:
            if tags_to_add:
                print(f"{OKGREEN}Tags to add in {file_path}: {tags_to_add}{ENDC}")
            else:
                print(f"{OKCYAN}No new tags to add in: {file_path}{ENDC}")
        if tags_to_add:
            if not self.dry_run:
                updated_tags = sorted(list(existing_tags.union(new_tags)))
                frontmatter['tags'] = updated_tags
                self.update_file_frontmatter(file_path, content, frontmatter, match.start(1), match.end(1))
                self.stats['tags_added'] += len(tags_to_add)
                self.stats['files_modified'] += 1

    def update_file_frontmatter(self, file_path: Path, content: str,
                               frontmatter: Dict[str, Any], start: int, end: int) -> None:
        """
        Update the YAML frontmatter in a file.
        Args:
            file_path (Path): Path to the file to update.
            content (str): Current file content.
            frontmatter (Dict[str, Any]): Updated frontmatter.
            start (int): Start index of YAML content in the file.
            end (int): End index of YAML content in the file.
        """
        try:
            if USE_RUAMEL:
                from io import StringIO
                yaml_string = StringIO()
                yaml_parser.dump(frontmatter, yaml_string)
                updated_yaml = yaml_string.getvalue()
            else:
                updated_yaml = pyyaml.dump(frontmatter, default_flow_style=False, sort_keys=False)
            updated_content = content[:start] + updated_yaml + content[end:]
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(updated_content)
        except Exception as e:
            logger.error(f"Error updating {file_path}: {str(e)}")
            self.stats['files_with_errors'] += 1


def add_nested_tags(directory: Path, dry_run: bool = False, verbose: bool = False) -> Dict[str, int]:
    """
    Add nested tags to markdown files in a directory.
    Args:
        directory (Path): The directory to process.
        dry_run (bool): If True, no files will be modified.
        verbose (bool): If True, additional information will be logged.
    Returns:
        Dict[str, int]: Statistics about the processing.
    """
    processor = MarkdownFrontmatterProcessor(dry_run=dry_run, verbose=verbose)
    if dry_run:
        print(f"{WARNING}[DRY RUN]{ENDC} Processing markdown files in: {directory}")
    else:
        print(f"Processing markdown files in: {directory}")
    stats = processor.process_directory(directory)
    return stats


def main() -> None:
    """
    Main entry point for the script.
    Parses command line arguments and calls the add_nested_tags function.
    """
    parser = argparse.ArgumentParser(
        description='Process markdown files to add nested tags based on YAML frontmatter.'
    )
    parser.add_argument(
        'directory', nargs='?', default=None,
        help='Directory to process (defaults to notebook vault)'
    )
    parser.add_argument(
        '--dry-run', action='store_true',
        help='Do not modify files, just show what would be done'
    )
    parser.add_argument(
        '--verbose', action='store_true',
        help='Show more detailed information about processing'
    )
    args = parser.parse_args()
    global logger
    logger, _ = setup_logging(debug=args.verbose)
    remove_timestamps_from_logger(logger)
    logging.getLogger().propagate = False
    if args.directory:
        directory = Path(args.directory)
    else:
        directory = Path(normalize_wsl_path(VAULT_LOCAL_ROOT))
        logger.info(f"No directory specified, using default vault: {directory}")
    if not directory.exists() or not directory.is_dir():
        logger.error(f"Directory not found or is not a directory: {directory}")
        exit(1)
    stats = add_nested_tags(directory, dry_run=args.dry_run, verbose=args.verbose)
    print(f"\n{BG_BLUE}{BOLD}{HEADER}   Nested Tag Processing Summary   {ENDC}\n")
    print(f"{OKBLUE}{BOLD}== Results =={ENDC}")
    print(f"  {OKCYAN}Files processed    {ENDC}: {OKGREEN}{stats['files_processed']}{ENDC}")
    print(f"  {OKCYAN}Files modified     {ENDC}: {OKGREEN}{stats['files_modified']}{ENDC}")
    print(f"  {OKCYAN}Tags added         {ENDC}: {OKGREEN}{stats['tags_added']}{ENDC}")
    print(f"  {OKCYAN}Index files cleared{ENDC}: {OKGREEN}{stats['index_files_cleared']}{ENDC}")
    if stats['files_with_errors']:
        print(f"  {FAIL}Files with errors  {ENDC}: {FAIL}{stats['files_with_errors']}{ENDC}")
    else:
        print(f"  {OKCYAN}Files with errors  {ENDC}: {OKGREEN}0{ENDC}")
    if args.dry_run:
        print(f"\n{WARNING}This was a dry run. No files were modified.{ENDC}")


if __name__ == "__main__":
    main()