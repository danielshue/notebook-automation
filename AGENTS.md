# Agent Instructions 

## Project Philosophy
- Write maintainable, readable code that prioritizes clarity over cleverness
- Follow SOLID principles in object-oriented design
- Optimize for developer experience and code readability
- Consider future maintenance needs in all implementations
- Create modular, loosely coupled components that can be easily tested and extended

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
- Follow PEP 8 style guide for Python code
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
- Implement proper logging using the centralize logging module in the config.py
- Use argparse for command-line argument parsing
- Always use the centralized configuration system for settings
- Follow the existing directory structure for new code:
  - `/tags` for tag manipulation scripts
  - `/obsidian` for Obsidian-specific tools
  - `/utilities` for general helper functions
  - `/tools` for core functionality modules

## Error Handling
- Use explicit exception types rather than catching generic exceptions
- Include contextual information in error messages
- Log errors with appropriate severity levels
- Propagate exceptions appropriately (don't hide errors)
- Use explicit try/except blocks with specific exception types
- Use the centralize error handling module for consistent error management

## Performance Guidelines
- Prefer readable code over premature optimization
- Cache results of expensive operations when appropriate
- Use generators for processing large datasets
- Consider adding progress indicators for long-running operations
- Document performance-critical sections
- Select appropriate data structures for operations

## Obsidian Integration
- Use template strings for Obsidian templates
- Follow proper YAML frontmatter formatting
- Consider existing tag hierarchies when adding new tag-related functionality
- Use consistent metadata properties across scripts

## Testing Approach
- Write unit tests for all non-trivial functions
- Tests should be placed in `/tests` directory with similar structure to the code
- Use pytest as the testing framework
- Mock external dependencies in tests
- Create proper test fixtures for reuse
- Aim for high test coverage of critical functionality

## Security
- Never hardcode credentials or API keys
- Use the project's configuration system for storing settings
- Implement proper input validation to prevent injection or path traversal
- Handle sensitive data according to best practices
- Sanitize user inputs before processing
- Use safe APIs for risky operations (file handling, network calls)

## MBA Project-Specific Guidelines
- Course tags should follow the `mba/course/COURSENAME` format
- Lecture tags should follow the `mba/lecture/COURSENAME/LECTURE_NUMBER` format
- Classes should include proper metadata with course code and title
- Video processing scripts should handle multiple video formats
- Tag hierarchies should be maintained according to the documented structure

## Commit Messages
- Follow the conventional commits format:
  - `<type>(<scope>): <short summary>`
  - Example: `feat(tags): implement hierarchical tag generation`
- Types include: feat, fix, docs, style, refactor, perf, test, chore, build, ci
- Keep summary concise and in imperative mood (e.g., "add" not "added")
- Reference issues in footer when applicable

## Pull Requests
- Begin title with a category tag: [Feature], [Fix], [Refactor], etc.
- Include clear sections for Summary, Changes, Testing, and Documentation
- Link to related issues
- Note any breaking changes explicitly
