"""
CLI for generating tag documentation from markdown files in a directory.
Generalized for any program or course structure.

This script scans a folder for markdown (.md) files and generates documentation for tag usage.

Example:
    $ notebook-generate-tag-doc <directory>
"""


import argparse
import re
from pathlib import Path
from typing import Dict, Any
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
from notebook_automation.cli.utils import OKGREEN, FAIL, WARNING, ENDC
from notebook_automation.tools.utils.config import setup_logging

def generate_tag_doc(directory: Path) -> Dict[str, int]:
    """
    Generate tag documentation from markdown files in a directory.
    Args:
        directory (Path): The directory to process.
    Returns:
        Dict[str, int]: Statistics about the processing.
    """
    yaml_frontmatter_pattern = re.compile(r'^---\s*\n(.*?)\n---\s*\n', re.DOTALL)
    tag_usage = {}
    stats = {
        'files_processed': 0,
        'files_with_errors': 0
    }
    logger = logging.getLogger(__name__)
    for item in directory.glob('**/*.md'):
        if item.is_file():
            stats['files_processed'] += 1
            try:
                content = item.read_text(encoding='utf-8')
            except UnicodeDecodeError:
                content = item.read_text(encoding='latin1')
            match = yaml_frontmatter_pattern.search(content)
            if not match:
                continue
            yaml_content = match.group(1)
            try:
                if USE_RUAMEL:
                    from io import StringIO
                    yaml_stream = StringIO(yaml_content)
                    frontmatter = yaml_parser.load(yaml_stream) or {}
                else:
                    frontmatter = pyyaml.safe_load(yaml_content) or {}
            except Exception:
                stats['files_with_errors'] += 1
                continue
            if 'tags' in frontmatter and frontmatter['tags']:
                tags = frontmatter['tags']
                if isinstance(tags, str):
                    tags = [t.strip() for t in tags.split(',')]
                for tag in tags:
                    tag_usage.setdefault(tag, 0)
                    tag_usage[tag] += 1
    # Output tag usage summary
    logger.info(f"\n{OKGREEN}Tag Usage Documentation:{ENDC}")
    for tag, count in sorted(tag_usage.items()):
        logger.info(f"  {tag}: {count}")
    logger.info(f"\nFiles processed: {stats['files_processed']}")
    if stats['files_with_errors']:
        logger.error(f"{FAIL}Files with errors: {stats['files_with_errors']}{ENDC}")
    else:
        logger.info(f"Files with errors: 0")
    return stats

def main() -> None:
    """
    Main entry point for the script.
    Parses command line arguments and calls the generate_tag_doc function.
    """
    parser = argparse.ArgumentParser(
        description='Generate tag usage documentation from markdown files.'
    )
    parser.add_argument(
        'directory', nargs='?', default='.',
        help='Directory to process (default: current directory)'
    )
    parser.add_argument(
        '--verbose', action='store_true',
        help='Show detailed processing output'
    )
    args = parser.parse_args()
    logger, _ = setup_logging(debug=args.verbose)
    directory = Path(args.directory)
    generate_tag_doc(directory)

if __name__ == "__main__":
    main()