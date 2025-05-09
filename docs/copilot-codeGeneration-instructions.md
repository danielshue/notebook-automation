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
