"""
CLI for adding example nested tags to a markdown file.
Generalized for any program or course structure.

This script adds an example nested tag structure to a specified markdown file to demonstrate hierarchical tagging.

Example:
    $ notebook-add-example-tags <file_path>
"""

import argparse
import re
from pathlib import Path
from typing import List

# Use ruamel.yaml if available, else fallback to PyYAML
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

def add_example_tags(file_path: Path, verbose: bool = False) -> bool:
    """Add example nested tags to an existing markdown file.

    Opens a markdown file, parses its YAML frontmatter (if present), and adds a set
    of example hierarchical tags to demonstrate the nested tag structure. Uses the
    appropriate YAML library to preserve formatting and quotes where possible.

    Args:
        file_path (Path): Path to the markdown file to update.
        verbose (bool, optional): Whether to print detailed progress information. Defaults to False.

    Returns:
        bool: True if the file was successfully updated, False otherwise.

    Raises:
        OSError: If there is an error writing to the file, the exception is caught and False is returned.

    Example:
        >>> from pathlib import Path
        >>> success = add_example_tags(Path("./notes/sample.md"), verbose=True)
        >>> if success:
        ...     print("Tags added successfully")
        ... else:
        ...     print("Failed to add tags")
        Tags added successfully
    """
    if not file_path.exists():
        print(f"{FAIL}File does not exist: {file_path}{ENDC}")
        return False

    if verbose:
        print(f"Adding example tags to: {file_path}")

    # Example nested tags to add (generalized, not MBA-specific)
    example_tags = [
        "type/note/lecture",
        "type/note/reference",
        "course/finance/corporate-finance",
        "course/accounting/financial",
        "skill/quantitative",
        "tool/excel",
        "status/active",
        "priority/high"
    ]

    try:
        content = file_path.read_text(encoding='utf-8')
    except UnicodeDecodeError:
        content = file_path.read_text(encoding='latin1')

    yaml_match = re.search(r'^---\s+(.*?)\s+---', content, re.DOTALL)

    if yaml_match:
        frontmatter_text = yaml_match.group(1)
        try:
            if USE_RUAMEL:
                from io import StringIO
                yaml_stream = StringIO(frontmatter_text)
                frontmatter = yaml_parser.load(yaml_stream)
                if not isinstance(frontmatter, dict):
                    frontmatter = {}
            else:
                frontmatter = pyyaml.safe_load(frontmatter_text)
                if not isinstance(frontmatter, dict):
                    frontmatter = {}
        except Exception:
            frontmatter = {}

        # Add or update tags
        before = frontmatter.get('tags', [])
        if 'tags' in frontmatter and isinstance(frontmatter['tags'], list):
            frontmatter['tags'].extend(example_tags)
            # Remove duplicates while preserving order
            seen = set()
            frontmatter['tags'] = [tag for tag in frontmatter['tags'] if not (tag in seen or seen.add(tag))]
        else:
            frontmatter['tags'] = example_tags

        # Convert back to YAML
        if USE_RUAMEL:
            from io import StringIO
            yaml_string = StringIO()
            yaml_parser.dump(frontmatter, yaml_string)
            new_frontmatter = yaml_string.getvalue()
        else:
            new_frontmatter = pyyaml.dump(frontmatter, default_flow_style=False)

        # Replace old frontmatter with new one
        new_content = content.replace(yaml_match.group(0), f"---\n{new_frontmatter}---")
        if verbose:
            if before != frontmatter['tags']:
                print(f"{OKGREEN}Tags updated in {file_path}: {frontmatter['tags']}{ENDC}")
            else:
                print(f"{WARNING}No tag changes needed in: {file_path}{ENDC}")
    else:
        # No frontmatter exists, add one
        if USE_RUAMEL:
            from io import StringIO
            yaml_string = StringIO()
            yaml_parser.dump({'tags': example_tags}, yaml_string)
            tags_yaml = yaml_string.getvalue()
        else:
            tags_yaml = pyyaml.dump({'tags': example_tags}, default_flow_style=False)
        new_content = f"---\n{tags_yaml}---\n\n{content}"
        if verbose:
            print(f"{OKGREEN}Frontmatter created and tags added in {file_path}{ENDC}")

    try:
        file_path.write_text(new_content, encoding='utf-8')
        if not verbose:
            print(f"{OKGREEN}Successfully added example tags to {file_path}{ENDC}")
        return True
    except Exception as e:
        print(f"{FAIL}Error adding tags to {file_path}: {e}{ENDC}")
        return False


def main() -> None:
    """Main entry point for the example tag adding CLI tool.

    Parses command line arguments, sets up the file path, and invokes the add_example_tags
    function with the specified file. Exits with code 1 if the operation fails.

    Args:
        None

    Returns:
        None: This function doesn't return a value.

    Example:
        When called from the command line:
            $ notebook-add-example-tags ./notes/sample.md --verbose
    """
    parser = argparse.ArgumentParser(
        description='Add example nested tags to a markdown file.'
    )
    parser.add_argument(
        'file_path',
        help='Path to the markdown file to update'
    )
    parser.add_argument(
        '--verbose', action='store_true',
        help='Show more detailed information about processing'
    )
    args = parser.parse_args()
    file_path = Path(args.file_path)
    success = add_example_tags(file_path, verbose=args.verbose)
    if not success:
        exit(1)


if __name__ == "__main__":
    main()