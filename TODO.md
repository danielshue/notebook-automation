
# Notebook Automation TODO List

Quick reference for key tasks and improvements. For the full backlog, see [Project Backlog](docs/project_backlog.md).

*Last updated: May 12th, 2025, 10:45:00 am*

## Immediate Priority: CLI Package Migration & Refactor

### [x] generate_video_meta CLI Migration Checklist

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
- [x] **Update entry points and setup**
  - [x] Update `setup.py` to point CLI entry points to the new package modules (e.g., `mba_notebook_automation.cli.generate_pdf_notes:main`)
  - [x] Rename CLI entry points to be program-agnostic (e.g., `notebook-generate-pdf-notes`)
- [x] **Generalize and polish CLI output**
  - [x] Ensure all CLI help, docstrings, and output use neutral terms (not MBA-specific)
  - [x] Ensure all CLI output is colorized and user-friendly
  - [x] Remove timestamps from all user-facing logs
  - [x] Ensure logger output is visible and respects verbosity flags
- [x] **Test and enhance each CLI entry point**
  - [x] Test each CLI tool after migration to ensure correct behavior
  - [x] Add or update unit tests for CLI logic (argument parsing, error handling, etc.)
  - [x] Ensure all CLI tools provide colorized progress and summary output
  - [x] Add and test --verbose flag for all tag-related CLIs (per-file output, colorized)
- [x] **Documentation and migration guide**
  - [x] Update project documentation to reflect new CLI usage and structure
  - [x] Write a migration guide for users (old vs. new CLI commands, breaking changes, etc.)
  - [x] Update README and any onboarding docs
- [x] **Clean up and finalize**
  - [x] Remove old standalone CLI scripts from the root or other locations
  - [x] Consolidate duplicate video processing tools and remove video directory
  - [x] Reorganize CLI files and utilities into proper directory structure
  - [x] Ensure all code follows project coding standards and docstring conventions
  - [x] Commit changes with clear, conventional commit messages

# TODO: Generalize for Any Program
- [x] Remove or generalize all references to "MBA" in CLI names, documentation, code comments, and configuration keys/values
- [x] Use neutral terms like "notebook," "course," or "program" instead of "mba"
- [x] Update CLI entry points, help messages, and documentation to reflect this broader scope
- [x] Review configuration keys/values (e.g., paths, tag formats) for MBA-specific language and generalize as needed
- [x] Update code and user-facing messages to remove MBA-specific references
- [x] Create migration guide for users

## High Priority

- [x] Set up testing infrastructure:
  - [x] Configure pytest with proper structure
  - [x] Add basic test cases for core functionality
  - [ ] Set up GitHub Actions for CI
- [x] Update documentation:
  - [x] Complete package installation guide
  - [x] Update all paths in existing docs
  - [x] Add module reference documentation
- [x] Improve error handling and validation:
  - [x] Add error handling to configuration script
  - [x] Add input validation to key functions
  - [x] Implement proper exception hierarchy

## Medium Priority

- [x] Code quality improvements:
  - [x] Add type hints across all Python files
  - [x] Standardize docstrings using Google-style format
  - [x] Add logging to all modules
- [x] Maintenance tasks:
  - [x] Create script to find orphaned files
  - [x] Clean up archived files
  - [x] Add version checking for dependencies
- [x] Testing improvements:
  - [x] Create fixtures for common test data
  - [x] Add pytest markers for different test categories
  - [x] Add integration tests

## Low Priority

- [x] Performance improvements:
  - [x] Optimize PDF processing
  - [x] Implement caching for repeated operations
- [x] User experience:
  - [x] Add colorized output to console scripts
  - [x] Improve progress indicators
  - [ ] Add interactive configuration wizard

## Next Steps

- [ ] Set up GitHub Actions for CI/CD pipeline
  - [ ] Create workflow for running tests
  - [ ] Add linting and formatting checks
  - [ ] Configure automated releases
- [ ] Implement interactive configuration wizard
  - [ ] Create guided setup for new users
  - [ ] Add validation for configuration values
  - [ ] Support configuration migration from old format
- [ ] MCP Integration
  - [ ] Create companion MCP server project
  - [ ] Implement notebook-to-MCP adapters
  - [ ] Add MCP operation handlers
  - [ ] Create MCP-specific configuration options
  - [ ] Add documentation for MCP integration
- [ ] Documentation refinements
  - [ ] Add contributor guidelines
  - [ ] Create detailed API documentation
  - [ ] Add workflow diagrams for complex processes

## Recently Completed

- [x] Create dedicated directories for different scripts
- [x] Move tag-related scripts to `/tags` directory
- [x] Move test scripts to `/tests` directory
- [x] Update main README.md with new organization
- [x] Create onboarding guide for new courses
- [x] Consolidated all TODOs and recommendations into project backlog
- [x] Add comprehensive type hints across all Python files
- [x] Standardize documentation with Google-style docstrings
- [x] Migrate all CLI tools to the new package structure
- [x] Remove MBA-specific language throughout the codebase
