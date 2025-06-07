# Developer Guide

Welcome to the Notebook Automation developer guide. This section provides comprehensive information for developers who want to contribute to, extend, or customize the Notebook Automation tool.

## Overview

This guide covers:

- **Architecture and Design** - Understanding the codebase structure and design patterns
- **Contributing Guidelines** - How to contribute code, documentation, and bug reports
- **Building and Testing** - Setting up development environment and running tests
- **Extension Points** - How to customize and extend functionality
- **API Documentation** - Detailed API reference for developers

## Getting Started

### Prerequisites

To develop Notebook Automation, you'll need:

- **.NET 9.0 SDK** or later
- **Git** for version control
- **IDE/Editor** (Visual Studio, VS Code, Rider, etc.)
- **Basic C# knowledge** and familiarity with .NET ecosystem

### Quick Setup

1. **Clone the repository**:

   ```bash
   git clone https://github.com/your-repo/notebook-automation.git
   cd notebook-automation
   ```

2. **Restore dependencies**:

   ```bash
   dotnet restore src/c-sharp/NotebookAutomation.sln
   ```

3. **Build the solution**:

   ```bash
   dotnet build src/c-sharp/NotebookAutomation.sln
   ```

4. **Run tests**:

   ```bash
   dotnet test src/c-sharp/NotebookAutomation.sln
   ```

## Development Workflow

### Code Organization

The codebase follows a modular architecture:

```
src/c-sharp/
├── NotebookAutomation.Core/          # Core business logic
├── NotebookAutomation.Cli/           # Command-line interface
├── NotebookAutomation.Services/      # External service integrations
└── NotebookAutomation.Tests/         # Unit and integration tests
```

### Coding Standards

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
- Use modern C# features (records, pattern matching, etc.)
- Write comprehensive XML documentation
- Maintain high test coverage
- Follow SOLID principles

### Development Process

1. **Create feature branch** from `main`
2. **Implement changes** with tests
3. **Run quality checks** (build, test, lint)
4. **Submit pull request** with clear description
5. **Address feedback** and iterate

## Architecture Overview

### Core Components

**NotebookAutomation.Core**

- Configuration management
- Document processing pipeline
- Metadata extraction engine
- Core business models

**NotebookAutomation.Cli**

- Command-line interface
- Argument parsing
- User interaction and feedback

**NotebookAutomation.Services**

- AI service integrations (OpenAI, Azure, Foundry)
- File system operations
- External API clients

### Design Patterns

- **Dependency Injection** for service management
- **Strategy Pattern** for AI provider abstraction
- **Builder Pattern** for configuration
- **Pipeline Pattern** for document processing

## Contributing

### Types of Contributions

We welcome various types of contributions:

- **Bug Reports** - Help identify and fix issues
- **Feature Requests** - Suggest new functionality
- **Code Contributions** - Implement features or fixes
- **Documentation** - Improve guides and API docs
- **Testing** - Add test coverage or improve test quality

### Contribution Process

1. **Check existing issues** to avoid duplication
2. **Create issue** for discussion (for large changes)
3. **Fork repository** and create feature branch
4. **Implement changes** following coding standards
5. **Add tests** for new functionality
6. **Update documentation** as needed
7. **Submit pull request** with clear description

### Code Review Guidelines

All contributions go through code review:

- **Functionality** - Does it work as intended?
- **Code Quality** - Is it maintainable and well-structured?
- **Testing** - Is it adequately tested?
- **Documentation** - Are changes documented?
- **Performance** - Does it meet performance requirements?

## Building and Testing

### Build Configuration

The solution supports multiple build configurations:

```bash
# Debug build (default)
dotnet build src/c-sharp/NotebookAutomation.sln

# Release build
dotnet build src/c-sharp/NotebookAutomation.sln --configuration Release

# Publish for deployment
dotnet publish src/c-sharp/NotebookAutomation.Cli/NotebookAutomation.Cli.csproj --configuration Release
```

### Testing Strategy

We use a comprehensive testing approach:

- **Unit Tests** - Test individual components in isolation
- **Integration Tests** - Test component interactions
- **End-to-End Tests** - Test complete workflows
- **Performance Tests** - Validate performance requirements

### Running Tests

```bash
# Run all tests
dotnet test src/c-sharp/NotebookAutomation.sln

# Run specific test project
dotnet test src/c-sharp/NotebookAutomation.Tests/

# Run with coverage
dotnet test src/c-sharp/NotebookAutomation.sln --collect:"XPlat Code Coverage"
```

## Extension Points

### Custom AI Providers

Implement the `IAIProvider` interface to add new AI services:

```csharp
public class CustomAIProvider : IAIProvider
{
    public async Task<string> GenerateSummaryAsync(string content, CancellationToken cancellationToken)
    {
        // Implement custom AI integration
    }
}
```

### Custom Document Processors

Extend document processing by implementing `IDocumentProcessor`:

```csharp
public class CustomDocumentProcessor : IDocumentProcessor
{
    public bool CanProcess(string filePath) => filePath.EndsWith(".custom");

    public async Task<DocumentMetadata> ProcessAsync(string filePath, CancellationToken cancellationToken)
    {
        // Implement custom processing logic
    }
}
```

### Custom Output Formatters

Add new output formats by implementing `IOutputFormatter`:

```csharp
public class CustomOutputFormatter : IOutputFormatter
{
    public string FormatName => "custom";

    public async Task WriteAsync(DocumentMetadata metadata, Stream output, CancellationToken cancellationToken)
    {
        // Implement custom output formatting
    }
}
```

## API Reference

For detailed API documentation, see the [API Reference](../api/index.md) section.

### Key Interfaces

- `IAIProvider` - AI service abstraction
- `IDocumentProcessor` - Document processing interface
- `IOutputFormatter` - Output formatting interface
- `IConfigurationService` - Configuration management
- `ILoggingService` - Logging abstraction

### Core Models

- `DocumentMetadata` - Extracted document information
- `AppConfig` - Application configuration
- `ProcessingOptions` - Processing parameters
- `AIServiceConfig` - AI service configuration

## Best Practices

### Performance

- Use `async/await` for I/O operations
- Implement proper cancellation support
- Cache expensive operations appropriately
- Monitor memory usage in long-running operations

### Error Handling

- Use specific exception types
- Include contextual information in error messages
- Implement proper logging for debugging
- Follow fail-fast principles

### Security

- Never log sensitive information (API keys, etc.)
- Validate all external inputs
- Use secure coding practices
- Follow principle of least privilege

## Resources

### Documentation

- [Architecture Overview](architecture.md) - Detailed system architecture
- [Contributing Guide](contributing.md) - Contribution guidelines
- [Building Guide](building.md) - Build and deployment instructions
- [Testing Guide](testing.md) - Testing strategies and tools

### External Resources

- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [C# Programming Guide](https://docs.microsoft.com/en-us/dotnet/csharp/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)

## Support

### Developer Support

- **GitHub Issues** - For bug reports and feature requests
- **GitHub Discussions** - For general questions and discussions
- **Code Reviews** - Through pull request process

### Community

- Join our developer community discussions
- Follow the project for updates
- Star the repository to show support

## Next Steps

1. **Read the [Architecture Guide](architecture.md)** to understand the system design
2. **Review [Contributing Guidelines](contributing.md)** before making changes
3. **Set up your development environment** using the [Building Guide](building.md)
4. **Write tests** following the [Testing Guide](testing.md)
5. **Browse the [API Reference](../api/index.md)** for technical details
