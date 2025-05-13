"""
CLI for restructuring tags in markdown files in a directory.
Generalized for any program or course structure.

This script scans a folder for markdown (.md) files and restructures tags according to project rules.

Example:
    $ notebook-restructure-tags <directory>
"""

import argparse
import re
from pathlib import Path
from typing import Dict, Any

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

def restructure_tags(directory: Path, verbose: bool = False) -> Dict[str, int]:
    """Restructure tags in markdown files in a directory.
    
    Scans all markdown files in the specified directory and applies tag restructuring
    rules, updating the YAML frontmatter while preserving its format. Works with both
    ruamel.yaml (preferred) and PyYAML as fallback.
    
    Args:
        directory (Path): The directory containing markdown files to process
        verbose (bool): Whether to print detailed progress information. 
            Defaults to False.
    
    Returns:
        Dict[str, int]: Statistics about the processing, including counts of
            files processed, modified, and any errors encountered
            
    Example:
        >>> stats = restructure_tags(Path("./notes"), verbose=True)
        >>> print(f"Modified {stats['files_modified']} of {stats['files_processed']} files")
        Modified 15 of 30 files
    """
    from notebook_automation.cli.utils import OKGREEN, OKCYAN, WARNING, FAIL, ENDC
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
                if verbose:
                    print(f"{WARNING}No YAML frontmatter found in: {item}{ENDC}")
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
                if verbose:
                    print(f"{FAIL}Error parsing YAML in {item}{ENDC}")
                continue
            # Example restructuring: convert all tags to lowercase and replace spaces with dashes
            if 'tags' in frontmatter and frontmatter['tags']:
                before = frontmatter['tags']
                if isinstance(frontmatter['tags'], list):
                    tags = [tag.lower().replace(' ', '-') for tag in frontmatter['tags']]
                    frontmatter['tags'] = tags
                elif isinstance(frontmatter['tags'], str):
                    tags = [t.strip().lower().replace(' ', '-') for t in frontmatter['tags'].split(',')]
                    frontmatter['tags'] = tags
                if before != frontmatter['tags']:
                    if verbose:
                        print(f"{OKGREEN}Tags restructured in {item}: {frontmatter['tags']}{ENDC}")
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
                    except Exception:
                        stats['files_with_errors'] += 1
                        if verbose:
                            print(f"{FAIL}Error updating {item}{ENDC}")
                else:
                    if verbose:
                        print(f"{OKCYAN}No tag changes needed in: {item}{ENDC}")
            else:
                if verbose:
                    print(f"{OKCYAN}No tags found in: {item}{ENDC}")
    return stats

def main() -> None:
    """Main entry point for the tag restructuring CLI tool.
    
    Parses command line arguments and invokes the restructure_tags function
    with the specified directory. Outputs statistics about the processing
    results to provide feedback to the user.
    
    Args:
        None
        
    Returns:
        None: This function doesn't return a value
        
    Example:
        When called from the command line:
        $ notebook-restructure-tags ~/notes --verbose
    """
    parser = argparse.ArgumentParser(
        description='Restructure tags in markdown files (lowercase, dashes, etc).'
    )
    parser.add_argument(
        'directory', nargs='?', default=None,
        help='Directory to process (default: notebook vault root)'
    )
    parser.add_argument(
        '--verbose', action='store_true',
        help='Show more detailed information about processing'
    )
    parser.add_argument(
        '-c', '--config', type=str, default=None, help='Path to config.json')
    args = parser.parse_args()
    from notebook_automation.tools.utils import config as config_utils
    config = config_utils.load_config_data(args.config)
    if args.directory:
        directory = Path(args.directory)
    else:
        from notebook_automation.tools.utils.paths import normalize_wsl_path
        directory = Path(normalize_wsl_path(config['paths']['notebook_vault_root']))
        print(f"No directory specified, using default vault: {directory}")
    stats = restructure_tags(directory, verbose=args.verbose)
    print(f"\n{OKGREEN}Tag restructuring complete!{ENDC}")
    print(f"  Files processed: {stats['files_processed']}")
    print(f"  Files modified:  {stats['files_modified']}")
    if stats['files_with_errors']:
        print(f"{FAIL}  Files with errors: {stats['files_with_errors']}{ENDC}")
    else:
        print(f"  Files with errors: 0")

if __name__ == "__main__":
    main()