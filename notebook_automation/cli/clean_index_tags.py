"""
CLI for clearing tags from index markdown files in a directory.
Generalized for any program or course structure.

This script recursively scans a folder for markdown (.md) files and removes all tags from files whose YAML frontmatter contains 'index-type'.

Example:
    $ notebook-clean-index-tags <directory>
"""

import argparse
import re
from pathlib import Path
from typing import Dict, Any

from notebook_automation.tools.utils.config import setup_logging
from notebook_automation.cli.utils import remove_timestamps_from_logger

try:
    from ruamel.yaml import YAML
    USE_RUAMEL = True
    yaml_parser = YAML()
    yaml_parser.preserve_quotes = True
    yaml_parser.width = 4096
except ImportError:
    import yaml as pyyaml
    USE_RUAMEL = False

def clean_index_tags(directory: Path, logger) -> Dict[str, int]:
    """Remove all tags from markdown files with 'index-type' in frontmatter.
    
    Index files should not have tags as they serve as structural elements in the
    knowledge base rather than content. This function scans all markdown files in the
    specified directory and removes tags from any file identified as an index.
    Works with both ruamel.yaml (preferred) and PyYAML as fallback.
    
    Args:
        directory (Path): The directory containing markdown files to process
        logger: Logger instance for output and error reporting
    
    Returns:
        Dict[str, int]: Statistics about the processing, including counts of
            files processed, modified, and any errors encountered
            
    Example:
        >>> stats = clean_index_tags(Path("./notes"), logger)
        >>> print(f"Modified {stats['files_modified']} of {stats['files_processed']} files")
        Modified 5 of 30 files
    """
    yaml_frontmatter_pattern = re.compile(r'^---\s*\n(.*?)\n---\s*\n', re.DOTALL)
    stats = {
        'files_processed': 0,
        'files_modified': 0,
        'files_with_errors': 0
    }
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
            except Exception as e:
                logger.error(f"YAML parse error in {item}: {e}")
                stats['files_with_errors'] += 1
                continue
            if 'index-type' in frontmatter and 'tags' in frontmatter and frontmatter['tags']:
                frontmatter['tags'] = []
                # Write back updated frontmatter
                try:
                    if USE_RUAMEL:
                        from io import StringIO
                        yaml_string = StringIO()
                        yaml_parser.dump(frontmatter, yaml_string)
                        updated_yaml = yaml_string.getvalue()
                    else:
                        updated_yaml = pyyaml.dump(frontmatter, default_flow_style=False, sort_keys=False)
                    updated_content = content[:match.start(1)] + updated_yaml + content[match.end(1):]
                    item.write_text(updated_content, encoding='utf-8')
                    stats['files_modified'] += 1
                    logger.info(f"Cleared tags from index file: {item}")
                except Exception as e:
                    logger.error(f"Failed to write updated tags for {item}: {e}")
                    stats['files_with_errors'] += 1
    return stats


def main() -> None:
    """Main entry point for the index tag cleaning CLI tool.
    
    Parses command line arguments, sets up logging, and invokes the clean_index_tags
    function with the specified directory. Outputs statistics about the processing
    results to provide feedback to the user on the files processed and modified.
    
    Args:
        None
        
    Returns:
        None: This function doesn't return a value
        
    Example:
        When called from the command line:
        $ notebook-clean-index-tags ~/notes --verbose
    """
    parser = argparse.ArgumentParser(
        description='Remove all tags from markdown files with index-type in YAML frontmatter.'
    )
    parser.add_argument(
        'directory', nargs='?', default=None,
        help='Directory to process (default: notebook vault root)'
    )
    parser.add_argument(
        '--verbose', action='store_true',
        help='Enable verbose (debug) logging'
    )
    parser.add_argument(
        '-c', '--config', type=str, default=None, help='Path to config.json')
    args = parser.parse_args()
    from notebook_automation.tools.utils import config as config_utils
    config = config_utils.load_config_data(args.config)
    logger, _ = setup_logging(debug=args.verbose)
    remove_timestamps_from_logger(logger)
    if args.directory:
        directory = Path(args.directory)
    else:
        from notebook_automation.tools.utils.paths import normalize_wsl_path
        directory = Path(normalize_wsl_path(config['paths']['notebook_vault_root']))
        logger.info(f"No directory specified, using default vault: {directory}")
    logger.info(f"Starting index tag cleaning in: {directory}")
    stats = clean_index_tags(directory, logger)
    logger.info("Index tag cleaning complete!")
    logger.info(f"  Files processed: {stats['files_processed']}")
    logger.info(f"  Files modified:  {stats['files_modified']}")
    if stats['files_with_errors']:
        logger.error(f"  Files with errors: {stats['files_with_errors']}")
    else:
        logger.info(f"  Files with errors: 0")

if __name__ == "__main__":
    main()