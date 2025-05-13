"""
CLI tool to generate a Class Dashboard.md in each class folder, filling in program, course, and class metadata.

This script walks the vault, finds all class folders (those containing a class-index.md),
and creates or overwrites a Class Dashboard.md file in each, using a template with
{{class_name}}, {{course_name}}, and {{program_name}} replaced by the correct values.

Usage:
    python create_class_dashboards.py /path/to/vault --dry-run --verbose

"""
import argparse
import os
from pathlib import Path
from notebook_automation.tools.utils.config import setup_logging, NOTEBOOK_VAULT_ROOT
from notebook_automation.tools.utils.paths import normalize_wsl_path
from notebook_automation.cli.ensure_metadata import MetadataUpdater

# Dashboard template (from Class Dashboard.md)
DASHBOARD_TEMPLATE = '''---
title: 📚 {{class_name}} Dashboard
auto-generated-state: writable
banner: '[[gies-banner.png]]'
banner_x: 0.25
class: {{class_name}}
course: {{course_name}}
date-created: {{date_created}}
date-modified: {{date_modified}}
linter-yaml-title-alias: 📚 {{class_name}} Dashboard
program: {{program_name}}
tags: []
---

# 📚 {{class_name}} Dashboard

## 📖 Required Readings
```dataview
TABLE pages, status
FROM ""
WHERE (type = "reading" OR type = "instructions") AND class = this.class
SORT file.name ASC
```

## 🎥 Videos

```dataview
TABLE title, file.folder AS "Folder", status
FROM ""
WHERE type = "video-reference" AND class = this.class
SORT Folder ASC
```

## 📚 Case Studies

```dataview
TABLE status
FROM ""
WHERE type = "note/case-study" AND class = this.class
SORT file.name ASC
```

## 📝 Assignments

```dataview
TABLE title AS "Assignment", type, status, due
FROM ""
WHERE class = this.class AND contains(type, "assignment")
```

## ✅ Tasks

```tasks
path includes accounting-for-manager
not done
sort by due
```
'''

from datetime import date

def render_dashboard(class_name: str, course_name: str, program_name: str) -> str:
    today = date.today().isoformat()
    return (
        DASHBOARD_TEMPLATE
        .replace('{{class_name}}', class_name)
        .replace('{{course_name}}', course_name)
        .replace('{{program_name}}', program_name)
        .replace('{{date_created}}', today)
        .replace('{{date_modified}}', today)
    )


def find_class_folders(notebook_vault_root: Path):
    """Yield all likely class folders: folders whose name starts with 'Class' (case-insensitive) and are at least 3 levels deep (Program/Course/Class)."""
    for root, dirs, files in os.walk(notebook_vault_root):
        path = Path(root)
        # Check for at least 3 levels deep (Program/Course/Class)
        if len(path.relative_to(notebook_vault_root).parts) >= 3 and path.name.lower().startswith('class'):
            yield path


def main():
    parser = argparse.ArgumentParser(description="Create Class Dashboard.md in each class folder.")
    parser.add_argument('vault', nargs='?', default=None, help="Vault root path (default: config)")
    parser.add_argument('--dry-run', action='store_true', help="Don't write files, just print actions")
    parser.add_argument('--verbose', '-v', action='store_true', help="Verbose output")
    parser.add_argument('-c', '--config', type=str, default=None, help='Path to config.json')
    args = parser.parse_args()

    from notebook_automation.tools.utils import config as config_utils
    config = config_utils.load_config_data(args.config)
    logger, _ = setup_logging(debug=args.verbose)
    if args.vault:
        notebook_vault_root = Path(normalize_wsl_path(args.vault))
    else:
        notebook_vault_root = Path(normalize_wsl_path(config['paths']['notebook_vault_root']))
        logger.info(f"No vault specified, using default from config: {notebook_vault_root}")
    updater = MetadataUpdater(verbose=args.verbose)

    for class_folder in find_class_folders(notebook_vault_root):
        class_index = class_folder / 'class-index.md'
        meta = updater.find_parent_index_info(class_index)
        class_name = meta.get('class') or class_folder.name
        course_name = meta.get('course') or class_folder.parent.name
        program_name = meta.get('program') or class_folder.parent.parent.name
        dashboard_path = class_folder / 'Class Dashboard.md'
        dashboard_content = render_dashboard(class_name, course_name, program_name)
        if args.dry_run:
            logger.info(f"[DRY RUN] Would write dashboard to {dashboard_path}")
        else:
            with open(dashboard_path, 'w', encoding='utf-8') as f:
                f.write(dashboard_content)
            logger.info(f"Wrote dashboard to {dashboard_path}")

if __name__ == '__main__':
    main()
