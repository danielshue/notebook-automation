# MBA Notebook Automation TODO List

Quick reference for key tasks and improvements. For the full backlog, see [Project Backlog](docs/project_backlog.md).

*Last updated: May 9th, 2025, 2:30:00 pm*

## Immediate Priority (Package Migration)

- [ ] Move remaining scripts from root into package:
  - [ ] `generate_markdown_from_html_and_text_in_vault.py`
  - [ ] `generate_obsidian_templates.py`
  - [ ] `ensure_consistent_metadata.py`
  - [ ] Other utility scripts
- [ ] Remove old duplicated directories after verifying migration:
  - [ ] `/tools`
  - [ ] `/utilities`
  - [ ] `/tags`
  - [ ] `/video`
- [ ] Test all entry points defined in setup.py
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
