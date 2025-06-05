# GitHub Copilot Code Generation Instructions

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

### Best Practices Summary

1. **File-scoped namespaces** for all new files
2. **Primary constructors** for simple dependency injection scenarios
3. **Records** for immutable data, **classes with primary constructors** for mutable data with behavior
4. **Collection expressions** for initialization
5. **Pattern matching** and **switch expressions** for cleaner conditional logic
6. **Target-typed new** where type inference is clear
7. **Modern exception throwing** helpers where available
