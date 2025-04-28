# GitHub Copilot General Instructions

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