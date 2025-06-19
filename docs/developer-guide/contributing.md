# Contributing to Notebook Automation

Thank you for your interest in contributing to Notebook Automation! This guide will help you get started with contributing to the project.

## Code of Conduct

This project adheres to a code of conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## How to Contribute

### Reporting Issues

Before creating an issue, please:
1. Search existing issues to avoid duplicates
2. Check if the issue exists in the latest version
3. Gather relevant information (see below)

**When reporting bugs, include:**
- Operating system and version
- .NET version (`dotnet --version`)
- Application version
- Steps to reproduce the issue
- Expected vs actual behavior
- Complete error messages and stack traces
- Sample files (if applicable and shareable)

**For feature requests, include:**
- Clear description of the feature
- Use cases and benefits
- Proposed implementation approach (if applicable)
- Acceptance criteria

### Development Setup

#### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com/)
- [PowerShell](https://docs.microsoft.com/powershell/) (for build scripts)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/) (recommended)

#### Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/your-username/notebook-automation.git
   cd notebook-automation
   ```
3. **Add upstream remote:**
   ```bash
   git remote add upstream https://github.com/original-owner/notebook-automation.git
   ```
4. **Install dependencies:**
   ```bash
   dotnet restore src/c-sharp/NotebookAutomation.sln
   ```
5. **Build the project:**
   ```bash
   dotnet build src/c-sharp/NotebookAutomation.sln
   ```
6. **Run tests:**
   ```bash
   dotnet test src/c-sharp/NotebookAutomation.sln
   ```

### Development Workflow

#### Creating a Branch

Create a feature branch for your work:
```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/issue-description
```

#### Making Changes

1. **Follow coding standards** (see [Coding Standards](#coding-standards))
2. **Write tests** for new functionality
3. **Update documentation** as needed
4. **Commit frequently** with clear messages

#### Testing Your Changes

```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

#### Before Submitting

1. **Sync with upstream:**
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```
2. **Run local CI build:**
   ```bash
   .\scripts\build-ci-local.ps1
   ```
3. **Ensure all tests pass**
4. **Check code formatting:**
   ```bash
   dotnet format --verify-no-changes
   ```

### Pull Request Process

#### Creating a Pull Request

1. **Push your branch** to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```
2. **Create a pull request** on GitHub
3. **Fill out the PR template** completely
4. **Link related issues** using keywords (e.g., "Fixes #123")

#### PR Requirements

- [ ] All tests pass
- [ ] Code follows project standards
- [ ] Documentation is updated
- [ ] Commit messages are clear
- [ ] No merge conflicts
- [ ] Changes are focused and atomic

#### PR Template

```markdown
## Summary
Brief description of the changes.

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update
- [ ] Performance improvement

## Changes Made
- List of specific changes
- Use bullet points for clarity

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests pass
- [ ] Manual testing completed

## Documentation
- [ ] Code comments added
- [ ] API documentation updated
- [ ] User documentation updated

## Checklist
- [ ] I have tested these changes locally
- [ ] I have added appropriate tests
- [ ] I have updated documentation
- [ ] My code follows the project's style guidelines
```

### Coding Standards

#### C# Style Guidelines

Follow the [Microsoft C# Coding Conventions](https://docs.microsoft.com/dotnet/csharp/programming-guide/inside-a-program/coding-conventions) and project-specific guidelines:

**Naming Conventions:**
- `PascalCase` for classes, methods, properties
- `camelCase` for parameters, local variables
- `_camelCase` for private fields
- `UPPER_CASE` for constants

**Code Structure:**
```csharp
namespace NotebookAutomation.Core.Services;

/// <summary>
/// Service for processing notebook files.
/// </summary>
public class NotebookProcessor(ILogger<NotebookProcessor> logger)
{
    private readonly ILogger<NotebookProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Processes the specified notebook file.
    /// </summary>
    /// <param name="filePath">Path to the notebook file.</param>
    /// <returns>Processing result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    public async Task<ProcessingResult> ProcessAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        
        _logger.LogInformation("Processing notebook: {FilePath}", filePath);
        
        // Implementation
        return new ProcessingResult();
    }
}
```

#### Documentation Standards

**XML Documentation:**
- All public classes, methods, and properties must have XML documentation
- Include `<summary>`, `<param>`, `<returns>`, and `<exception>` tags
- Provide examples for complex methods

**Code Comments:**
- Use comments to explain "why", not "what"
- Keep comments concise and accurate
- Update comments when code changes

#### Testing Standards

**Unit Tests:**
- Use MSTest framework
- Follow AAA pattern (Arrange, Act, Assert)
- One assertion per test method
- Descriptive test method names

```csharp
[TestMethod]
public async Task ProcessAsync_WithValidFile_ReturnsSuccess()
{
    // Arrange
    var processor = new NotebookProcessor(Mock.Of<ILogger<NotebookProcessor>>());
    var filePath = "test.ipynb";
    
    // Act
    var result = await processor.ProcessAsync(filePath);
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
}
```

**Test Organization:**
- Mirror the source code structure
- Use test categories: `[TestCategory("Unit")]`
- Mock external dependencies
- Use test fixtures for shared setup

### Project Structure

#### Adding New Features

When adding new features:
1. **Core logic** goes in `NotebookAutomation.Core`
2. **CLI commands** go in `NotebookAutomation.CLI`
3. **Tests** mirror the source structure
4. **Configuration** options go in appropriate config classes

#### File Organization

```
src/c-sharp/
├── NotebookAutomation.Core/
│   ├── Models/           # Data models
│   ├── Services/         # Business logic
│   ├── Extensions/       # Extension methods
│   └── Utilities/        # Helper functions
├── NotebookAutomation.CLI/
│   ├── Commands/         # CLI command handlers
│   └── Options/          # Command-line options
└── NotebookAutomation.Tests/
    ├── Unit/             # Unit tests
    ├── Integration/      # Integration tests
    └── TestUtilities/    # Test helpers
```

### Documentation

#### Types of Documentation

**Code Documentation:**
- XML comments for all public APIs
- Inline comments for complex logic
- README files for each major component

**User Documentation:**
- Getting started guides
- Configuration reference
- Usage examples
- Troubleshooting guides

**Developer Documentation:**
- Architecture decisions
- Build and deployment guides
- Contributing guidelines
- API reference

#### Documentation Tools

- **DocFX** for API documentation generation
- **Markdown** for user guides and tutorials
- **PlantUML** for diagrams and flowcharts

### Release Process

#### Version Management

- Follow [Semantic Versioning](https://semver.org/)
- Update version in all relevant files
- Tag releases with version numbers

#### Release Checklist

- [ ] All tests pass
- [ ] Documentation is updated
- [ ] Version numbers are updated
- [ ] Release notes are prepared
- [ ] Packages are built and tested
- [ ] Security review completed

### Getting Help

#### Development Questions

- **GitHub Discussions** - For general questions
- **GitHub Issues** - For bugs and feature requests
- **Code Reviews** - For implementation guidance

#### Resources

- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [C# Programming Guide](https://docs.microsoft.com/dotnet/csharp/)
- [MSTest Documentation](https://docs.microsoft.com/dotnet/core/testing/unit-testing-with-mstest)
- [Project Documentation](../index.md)

### Recognition

We appreciate all contributions to the project. Contributors will be:
- Listed in the project's contributors file
- Mentioned in release notes for significant contributions
- Invited to join the maintainers team for ongoing contributors

## License

By contributing to this project, you agree that your contributions will be licensed under the same license as the project (see LICENSE file).

## Questions?

If you have questions about contributing, please:
1. Check the existing documentation
2. Search previous discussions and issues
3. Create a new discussion or issue
4. Contact the maintainers directly

Thank you for contributing to Notebook Automation!