---
applyTo: "**"
---

# GitHub Copilot Code Generation Instructions

## Code Structure and Philosophy

- Write maintainable, readable code that prioritizes clarity over cleverness
- Follow SOLID principles in object-oriented design
- Optimize for developer experience and code readability
- Create modular, loosely coupled components that can be easily tested and extended
- Favor composition over inheritance
- Use appropriate design patterns when applicable
- Follow the Dependency Inversion Principle - depend on abstractions, not implementations

## Modern C# Code Generation Guidelines

### File Structure

- **Always use file-scoped namespaces** for new files:

  ```csharp
  namespace NotebookAutomation.Core.Services;

  public class MyService
  {
      // Class content
  }
  ```

### Class Construction Patterns

- **Prefer primary constructors** for classes that primarily initialize dependencies:

  ```csharp
  public class DocumentProcessor(ILogger<DocumentProcessor> logger, AppConfig config)
  {
      private readonly ILogger<DocumentProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      private readonly AppConfig _config = config ?? throw new ArgumentNullException(nameof(config));

      public async Task ProcessAsync(string filePath) => await ProcessDocumentAsync(filePath);
  }
  ```

- **Use traditional constructors** for complex initialization logic:

  ```csharp
  public class DatabaseService
  {
      private readonly IDbConnection _connection;

      public DatabaseService(string connectionString)
      {
          _connection = CreateConnection(connectionString);
          _connection.Open();
          InitializeSchema();
      }
  }
  ```

### Data Classes and Records

- **Use record types** for immutable data:

  ```csharp
  public record CourseMetadata(string Name, string Code, DateTime StartDate);
  public record struct Point(double X, double Y);
  ```

- **Use primary constructors with classes** for mutable data with behavior:

  ```csharp
  public class Student(string name, int age)
  {
      public string Name { get; set; } = name;
      public int Age { get; set; } = age;
      public List<Course> Courses { get; } = [];

      public void EnrollIn(Course course) => Courses.Add(course);
  }
  ```

### Collection and Object Initialization

- **Use collection expressions** for arrays and lists:

  ```csharp
  string[] extensions = [".md", ".txt", ".html"];
  List<int> numbers = [1, 2, 3, 4, 5];
  Dictionary<string, int> counts = [];
  ```

- **Use target-typed new** where type is clear:

  ```csharp
  List<string> items = new();
  Dictionary<string, object> metadata = new();
  ```

### Pattern Matching and Conditionals

- **Use switch expressions** for value transformations:

  ```csharp
  string GetFileType(string extension) => extension.ToLower() switch
  {
      ".md" => "Markdown",
      ".txt" => "Text",
      ".html" => "HTML",
      _ => "Unknown"
  };
  ```

- **Use pattern matching** in conditionals:

  ```csharp
  if (result is { IsSuccess: true, Data: var data })
  {
      ProcessData(data);
  }
  ```

### Exception Handling

- **Use specific exception types** with modern syntax:

  ```csharp
  ArgumentException.ThrowIfNullOrEmpty(filePath);
  ObjectDisposedException.ThrowIf(_disposed, this);
  ```

## Error Handling

- Use explicit try/catch blocks with specific exception types (C#) or try/except (Python)
- Include contextual information in error messages
- Propagate exceptions appropriately (don't hide errors)
- Log errors with appropriate severity levels

### Global Usings Maintenance

- Place all commonly used namespaces in `GlobalUsings.cs` to reduce repetitive imports.
- Organize namespaces alphabetically for readability.
- Regularly review and update the file to ensure it reflects current project needs.
- Example structure:

  ```csharp
  global using System;
  global using System.Collections.Generic;
  global using System.Linq;
  global using Microsoft.Extensions.Logging;
  ```

## Performance Guidelines

- Prefer readable code over premature optimization
- Document performance-critical sections
- Select appropriate data structures for operations
- Include time/space complexity notes for algorithms when relevant

### Best Practices Summary

1. **File-scoped namespaces** for all new files
2. **Primary constructors** for simple dependency injection scenarios
3. **Records** for immutable data, **classes with primary constructors** for mutable data with behavior
4. **Collection expressions** for initialization
5. **Pattern matching** and **switch expressions** for cleaner conditional logic
6. **Target-typed new** where type inference is clear
7. **Modern exception throwing** helpers where available

## Reusability

- Parameterize functions instead of hardcoding values
- Create pure functions when possible (no side effects)
- Use dependency injection where appropriate
- Design intuitive interfaces that minimize required knowledge

## Code Style

- Follow PEP 8 style guidelines (for Python)
- Group imports in standard order: standard library, third-party, local
- Use meaningful whitespace to improve readability
- Include inline comments for complex logic or non-obvious implementations
- Explain "why" rather than "what" in comments

## Security Best Practices

- Never hardcode sensitive information (credentials, API keys)
- Sanitize user inputs before processing
- Use safe APIs for risky operations (file handling, network calls)
- Document security assumptions and requirements
- Use principle of least privilege when accessing resources

## Security Documentation

- Document security assumptions and requirements in a dedicated section of the README or a separate SECURITY.md file.
- Example:

  ```markdown
  ## Security Assumptions
  - All user inputs are sanitized before processing.
  - Sensitive data is stored securely using encryption.

  ## Security Requirements
  - Use HTTPS for all network communications.
  - Implement role-based access control for sensitive operations.
  ```

## Common Patterns to Implement

- Configuration management from environment variables
- Resource cleanup with context managers
- Logging with appropriate levels
- Factory methods for complex object creation
- Separation of data access from business logic
