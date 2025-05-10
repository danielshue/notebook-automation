
# Notebook Automation TODO List

Quick reference for key tasks and improvements. For the full backlog, see [Project Backlog](docs/project_backlog.md).

*Last updated: May 9th, 2025, 2:30:00 pm*




## Immediate Priority: CLI Package Migration & Refactor

### [ ] generate_video_meta CLI Migration Checklist

- [x] Implement main CLI entry point (`main()` function)
- [x] Set up logging and handle CLI options (debug, dry-run, etc.)
- [x] Authenticate with Microsoft Graph API (if needed)
- [x] Find video files (single file, folder, or retry failed)
- [x] Process each video file:
    - [x] Extract metadata from the path
    - [x] Optionally generate transcript and summary
    - [x] Create OneDrive share link (unless skipped)
    - [x] Generate or update the Obsidian markdown note
    - [x] Log results and handle errors
- [x] Write results and failed files to JSON
- [x] Add proper error handling and logging throughout


### CLI Descriptions (for reference and documentation)

- **notebook-add-nested-tags**: Scans markdown files in a directory, extracts fields from YAML frontmatter, and generates nested tags (e.g., `type/note/lecture`). Adds or updates the `tags:` field, removes duplicates, and supports dry-run and verbose output. Colorized, user-friendly CLI for tag enrichment.

- **notebook-add-example-tags**: Adds a set of example nested tags to a specified markdown file. Useful for demonstrating or testing the tag system. Supports verbose output for per-file status.

- **notebook-clean-index-tags**: Removes all tags from index files (detected by filename or frontmatter) in a directory. Ensures index pages do not have tags. Supports verbose, per-file reporting.

- **notebook-consolidate-tags**: Consolidates tags in markdown files by removing duplicates and sorting them. Ensures a clean, consistent tag list in each file. Supports verbose, per-file output.

- **notebook-generate-tag-doc**: Scans notes to generate documentation of tag usage and hierarchy. Produces a markdown report summarizing tag structure and usage statistics. Colorized summary output.

- **notebook-restructure-tags**: Restructures tags in markdown files by converting all tags to lowercase and replacing spaces with dashes. Ensures tag format consistency. Supports verbose, per-file output.

- **notebook-tag-manager**: Flexible entry point for advanced tag management operations. Intended for bulk tag refactoring, tag analysis, validation, custom transformations, or integration with other tools. By default, prints a message indicating where tag management would occur. Extendable for custom workflows.

- [x] **Create CLI package structure**
  - [x] Create `mba_notebook_automation/cli/` directory
  - [x] Add `__init__.py` to `mba_notebook_automation/cli/`
- [x] **Move and refactor CLI scripts**
  - [x] Move `generate_pdf_notes_from_onedrive.py` to `cli/generate_pdf_notes.py`
  - [x] Move other CLI scripts (e.g., `add_nested_tags.py`, `generate_video_meta_from_onedrive.py`) into `cli/` as needed
  - [x] Update all imports in these scripts to use absolute imports (e.g., `from mba_notebook_automation.tools...`)
  - [x] Remove any `sys.path` hacks from the scripts
- [x] **Extract and centralize shared CLI logic**
  - [x] Create `cli/utils.py` for:
    - [x] ANSI color codes/constants
    - [x] Logging setup (including timestamp removal)
    - [x] Colorized output helpers
    - [x] Common argument parsing helpers
    - [x] Common error handling
  - [x] Refactor CLI modules to use these shared utilities
- [ ] **Update entry points and setup**
  - [x] Update `setup.py` to point CLI entry points to the new package modules (e.g., `mba_notebook_automation.cli.generate_pdf_notes:main`)
- [x] Rename CLI entry points to be program-agnostic (e.g., `notebook-generate-pdf-notes`)
- [ ] **Generalize and polish CLI output**
  - [x] Ensure all CLI help, docstrings, and output use neutral terms (not MBA-specific)
  - [x] Ensure all CLI output is colorized and user-friendly
- [x] Remove timestamps from all user-facing logs
  - [x] Ensure logger output is visible and respects verbosity flags
- [ ] **Test and enhance each CLI entry point**
  - [x] Test each CLI tool after migration to ensure correct behavior
  - [ ] Add or update unit tests for CLI logic (argument parsing, error handling, etc.)
  - [x] Ensure all CLI tools provide colorized progress and summary output
  - [x] Add and test --verbose flag for all tag-related CLIs (per-file output, colorized)
- [ ] **Documentation and migration guide**
  - [x] Update project documentation to reflect new CLI usage and structure
  - [x] Write a migration guide for users (old vs. new CLI commands, breaking changes, etc.)
  - [x] Update README and any onboarding docs
- [ ] **Clean up and finalize**
  - [x] Remove old standalone CLI scripts from the root or other locations
  - [ ] Ensure all code follows project coding standards and docstring conventions
  - [ ] Commit changes with clear, conventional commit messages

# TODO: Generalize for Any Program
- [x] Remove or generalize all references to "MBA" in CLI names, documentation, code comments, and configuration keys/values
- [ ] Use neutral terms like "notebook," "course," or "program" instead of "mba"
- [ ] Update CLI entry points, help messages, and documentation to reflect this broader scope
- [ ] Review configuration keys/values (e.g., paths, tag formats) for MBA-specific language and generalize as needed
- [ ] Update code and user-facing messages to remove MBA-specific references
- [ ] Create migration guide for users

## High Priority

- [ ] Set up testing infrastructure:
  - [ ] Configure pytest with proper structure
  - [ ] Add basic test cases for core functionality
  - [ ] Set up GitHub Actions for CI
- [ ] Update documentation:
  - [ ] Complete package installation guide
  - [ ] Update all paths in existing docs
  - [ ] Add module reference documentation
- [ ] Improve error handling and validation:
  - [ ] Add error handling to configuration script
  - [ ] Add input validation to key functions
  - [ ] Implement proper exception hierarchy

## Medium Priority

- [ ] Code quality improvements:
  - [ ] Add type hints across all Python files
  - [ ] Standardize docstrings using Google-style format
  - [ ] Add logging to all modules
- [ ] Maintenance tasks:
  - [ ] Create script to find orphaned files
  - [ ] Clean up archived files
  - [ ] Add version checking for dependencies
- [ ] Testing improvements:
  - [ ] Create fixtures for common test data
  - [ ] Add pytest markers for different test categories
  - [ ] Add integration tests

## Low Priority

- [ ] Performance improvements:
  - [ ] Optimize PDF processing
  - [ ] Implement caching for repeated operations
- [ ] User experience:
  - [ ] Add colorized output to console scripts
  - [ ] Improve progress indicators
  - [ ] Add interactive configuration wizard

## Recently Completed

- [x] Create dedicated directories for different scripts
- [x] Move tag-related scripts to `/tags` directory
- [x] Move test scripts to `/tests` directory
- [x] Update main README.md with new organization
- [x] Create onboarding guide for new courses
- [x] Consolidated all TODOs and recommendations into project backlog
