---
template-type: resource-reading
auto-generated-state: writable
date-created: 2025-06-06
publisher: University of Illinois at Urbana-Champaign
type: note/reading
comprehension: 0
status: unread
completion-date: ''
date-modified: ''
date-review: ''
onedrive-shared-link: ''
onedrive_fullpath_file_reference: ''
page-count: ''
pages: ''
authors: ''
tags: ''
---

# GitHub Copilot Code Generation Instructions

## Project Philosophy
- Write maintainable, readable code that prioritizes clarity over cleverness
- Follow SOLID principles in object-oriented design
- Optimize for developer experience and code readability
- Consider future maintenance needs in all implementations

## Code Documentation
- Every Python file should begin with a module-level docstring
- All functions and classes must have descriptive docstrings
- Use Google-style docstring format:
  ```python
  def function_name(param1, param2):
      """Short description of function.
      
      Longer description explaining details.
      
      Args:
          param1 (type): Description of param1.
          param2 (type): Description of param2.
          
      Returns:
          return_type: Description of return value.
          
      Raises:
          ExceptionType: When and why this exception is raised.
          
      Example:
          >>> function_name('example', 123)
          'result'
      """
  ```

## Python Coding Standards
- Follow PEP 8 style guide
- Use type hints for all function parameters and return values
- Prefer explicit imports over wildcards (e.g., `from module import specific_thing` over `from module import *`)
- Use descriptive variable names that indicate purpose and content
- Maximum line length of 100 characters
- Use 4 spaces for indentation (no tabs)
- Always include proper error handling
- Use context managers (`with` statements) for file operations
- Prefer list/dict comprehensions for simple transformations

## Project-Specific Patterns
- Use `ruamel.yaml` library for YAML operations to preserve formatting
- Use pathlib for file path manipulations rather than os.path
- Implement proper logging using the built-in logging module
- Use argparse for command-line argument parsing
- Follow the existing directory structure for new code:

  - `/tags` for tag manipulation scripts
  - `/obsidian` for Obsidian-specific tools
  - `/utilities` for general helper functions
  - `/tools` for core functionality modules

## CLI Output Standards
- All command-line tools (CLI) must provide clear, colorized, and highly readable output in any ANSI-compatible terminal.
- Use ANSI color codes for section headers, keys, and important values to improve scanability.
- Align keys and values for readability; use bold and background colors for section titles and tips.
- Each configuration or output parameter should include a concise, indented description or hint for the user.
- Use Unicode arrows (e.g., ↳) or similar symbols to visually connect descriptions to their parameters.
- Always include a usage tip or help message at the end of CLI output where appropriate.
- Maintain consistency in formatting and color usage across all CLI tools in the project.
- For any CLI that performs long-running or multi-step operations, display a clear, colorized progress indicator:
  - Use a dynamic progress bar (e.g., tqdm) or update the terminal with status lines for each major step.
  - Print status lines in color (green for success, yellow for warnings, red for errors) for each processed item or file.
  - Show a summary at the end (e.g., total processed, successes, failures) in a visually distinct format.
  - Ensure progress output is readable and does not flood the terminal.
  - Example:

```text
\033[94m\033[1m== Progress ==\033[0m
  [#####-----] 50% Complete (Processed 50/100 files)
  \033[92m✔ File1.md processed successfully\033[0m
  \033[91m✖ File2.md failed: [error message]\033[0m
  ...
\033[1mSummary:\033[0m 48 succeeded, 2 failed
```
- Example:

```text
\033[44m\033[1m\033[95m   MBA Notebook Automation Configuration   \033[0m

\033[94m\033[1m== Paths ==\033[0m
  \033[96m\033[1mresources_root  \033[0m: \033[92mC:\\Users\\username\\OneDrive\\Education\\MBA-Resources\033[0m
    \033[90m↳ Top-level folder in OneDrive for your resources.\033[0m
```

This standard applies to all new and updated CLI tools, including but not limited to: mba-configure, mba-add-nested-tags, mba-generate-pdf-notes, mba-generate-video-metadata, etc.

## Error Handling
- Use explicit exception types rather than catching generic exceptions
- Log exceptions with appropriate context information
- Include helpful error messages that guide the user to resolution
- Use the custom error handling utilities when available

## Testing Approach
- Write unit tests for all non-trivial functions
- Tests should be placed in `/tests` directory with similar structure to the code
- Use pytest as the testing framework
- Mock external dependencies in tests

## Obsidian Integration
- Use template strings for Obsidian templates
- Follow proper YAML frontmatter formatting
- Consider existing tag hierarchies when adding new tag-related functionality
- Use consistent metadata properties across scripts

## Performance Considerations
- Cache results of expensive operations when appropriate
- Use generators for processing large datasets
- Consider adding progress indicators for long-running operations

## Security
- Never hardcode credentials or API keys
- Use the project's configuration system for storing settings
- Implement proper input validation to prevent injection or path traversal
- Handle sensitive data according to best practices

## MBA Project-Specific Guidelines
- Course tags should follow the `mba/course/COURSENAME` format
- Lecture tags should follow the `mba/lecture/COURSENAME/LECTURE_NUMBER` format
- Classes should include proper metadata with course code and title
- Video processing scripts should handle multiple video formats
