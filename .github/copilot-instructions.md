
# GitHub Copilot General Instructions for C# Development

## Project Philosophy
- Write maintainable, readable code that prioritizes clarity over cleverness
- Follow SOLID principles in object-oriented design
- Optimize for developer experience and code readability
- Consider future maintenance needs in all implementations
- Create modular, loosely coupled components that can be easily tested and extended

## Code Documentation
- Ensure all C# files have appropriate XML documentation comments
- All classes, methods, and properties should have descriptive documentation
- Use standard C# XML documentation format:
  ```csharp
  /// <summary>
  /// Short description of method.
  /// </summary>
  /// <param name="param1">Description of param1.</param>
  /// <param name="param2">Description of param2.</param>
  /// <returns>Description of return value.</returns>
  /// <exception cref="ExceptionType">When and why this exception is raised.</exception>
  /// <example>
  /// <code>
  /// var result = MethodName("example", 123);
  /// </code>
  /// </example>
  ```

## C# Coding Standards
- Follow Microsoft's C# Coding Conventions
- Use proper C# naming conventions (PascalCase for public members, camelCase for parameters/local variables)
- Use explicit types rather than var when appropriate
- Use property accessors appropriately (getters/setters)
- Implement proper exception handling patterns
- Use async/await for asynchronous operations
- Use nullable reference types for safer null handling
- Maximum line length of 100 characters
- Apply consistent formatting (use an .editorconfig file)
- Prefer LINQ for collection transformations where appropriate
- Use expression-bodied members for simple operations

## Project-Specific Patterns
- Use System.IO.Path or FileSystem abstractions instead of pathlib
- Implement a centralized logging system (consider NLog, Serilog, or Microsoft.Extensions.Logging)
- Use built-in configuration systems (Microsoft.Extensions.Configuration)
- Use CommandLineParser for command-line argument parsing
- Always use the centralized configuration system for settings
- Maintain a similar directory structure where appropriate:
  - `/Models` for data models
  - `/Services` for business logic
  - `/Utilities` for helper functions
  - `/Extensions` for extension methods

## Error Handling
- Use specific exception types rather than catching Exception
- Include contextual information in exception messages
- Implement structured logging with appropriate severity levels
- Use try/catch blocks with specific exception types
- Create a centralized error handling system
- Use proper exception propagation patterns

## Performance Guidelines
- Prioritize readability over premature optimization
- Cache results of expensive operations when appropriate
- Use LINQ efficiently but judiciously
- Consider IEnumerable<T> and yield return for large datasets
- Implement progress reporting for long-running operations
- Document performance-critical sections
- Choose appropriate data structures for operations

## Testing Approach
- Write unit tests for all non-trivial methods
- Organize tests to mirror the code structure
- Use xUnit, NUnit, or MSTest as the testing framework
- Mock external dependencies in tests (consider Moq or NSubstitute)
- Create proper test fixtures for reuse
- Aim for high test coverage of critical functionality

## Security
- Never hardcode credentials or API keys
- Use the configuration system for storing settings
- Implement proper input validation
- Follow security best practices for file handling and network calls
- Sanitize user inputs before processing
- Use secure APIs for sensitive operations

## Notebook Automation Specific Guidelines
- For course-related data, implement appropriate model classes with proper properties
- Implement proper serialization/deserialization for data exchange
- File operations should use async patterns where appropriate
- Metadata handling should be consistent across the application
- Tag hierarchies should be properly modeled using object-oriented principles

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