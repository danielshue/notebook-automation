# Developer Documentation

Welcome to the Notebook Automation Developer Documentation. This section provides comprehensive guides for developers who want to contribute to, extend, or integrate with the Notebook Automation system.

## Quick Links

- **[Getting Started](getting-started.md)** - Set up your development environment
- **[Architecture](architecture.md)** - System design and component overview
- **[Building](building.md)** - Build from source and deployment
- **[Contributing](contributing.md)** - Contribution guidelines and workflow
- **[Testing](testing.md)** - Testing strategies and best practices
- **[API Reference](../api/index.md)** - Complete C# API documentation

## For Developers

### Development Setup

Get your development environment ready:

1. **[Getting Started](getting-started.md)** - Prerequisites and initial setup
2. **[Building from Source](building.md)** - Build, test, and run locally
3. **[Development Workflow](development-workflow.md)** - Daily development practices

### Understanding the System

Deep dive into the architecture and design:

- **[System Architecture](architecture.md)** - Component relationships and design patterns
- **[Processing Pipeline](processing-pipeline.md)** - How content flows through the system
- **[Location-Agnostic Design](location-agnostic-design.md)** - Portable path resolution
- **[AI Integration](ai-integration.md)** - How AI services are integrated

### Extending the System

Build custom functionality:

- **[Plugin Development](plugin-development.md)** - Create custom processors and resolvers
- **[Custom Templates](custom-templates.md)** - Define your own note formats
- **[Adding New Commands](adding-commands.md)** - Extend the CLI with new commands
- **[Integration Guide](integration-guide.md)** - Integrate with external systems

### Contributing

Help improve Notebook Automation:

- **[Contributing Guide](contributing.md)** - How to contribute code, docs, or ideas
- **[Code Style](code-style.md)** - Coding standards and conventions
- **[Pull Request Process](pr-process.md)** - Submit and review pull requests
- **[Testing Guidelines](testing.md)** - Write effective tests

## Architecture Overview

Notebook Automation is built with a modular architecture:

```
┌─────────────────────────────────────────────────────┐
│                  CLI / Plugin UI                     │
├─────────────────────────────────────────────────────┤
│              Command Handlers Layer                  │
├─────────────────────────────────────────────────────┤
│                  Core Services                       │
│  ┌──────────┬──────────┬──────────┬──────────┐     │
│  │ PDF      │ Video    │ OneDrive │ AI       │     │
│  │ Processor│ Processor│ Service  │ Service  │     │
│  └──────────┴──────────┴──────────┴──────────┘     │
├─────────────────────────────────────────────────────┤
│              Configuration & Logging                 │
└─────────────────────────────────────────────────────┘
```

Key architectural principles:

- **Dependency Injection** - Loose coupling and testability
- **SOLID Principles** - Clean, maintainable object-oriented design
- **Plugin System** - Extensible processor and resolver registry
- **Location-Agnostic** - Portable across different environments

See [Architecture](architecture.md) for details.

## Technology Stack

### Core Technologies

- **.NET 10.0** - Modern C# 14 features and performance
- **Microsoft Semantic Kernel** - AI integration framework
- **YamlDotNet** - YAML frontmatter processing
- **Xabe.FFmpeg** - Video metadata extraction
- **Microsoft Graph SDK** - OneDrive integration

### Development Tools

- **MSTest** - Unit testing framework
- **Moq** - Mocking library for tests
- **PowerShell** - Build and automation scripts
- **DocFX** - API documentation generation

### Obsidian Plugin

- **TypeScript** - Plugin implementation
- **Obsidian API** - Native vault integration
- **Node.js** - Build toolchain

See [Technology Stack](technology-stack.md) for complete details.

## Development Workflow

### Typical Development Cycle

1. **Fork & Clone** - Get the code
   ```bash
   git clone https://github.com/your-username/notebook-automation.git
   cd notebook-automation
   ```

2. **Create Feature Branch**
   ```bash
   git checkout -b feature/my-awesome-feature
   ```

3. **Make Changes** - Write code and tests
   ```bash
   # Build
   dotnet build src/c-sharp/NotebookAutomation.sln
   
   # Test
   dotnet test src/c-sharp/NotebookAutomation.sln
   ```

4. **Commit & Push**
   ```bash
   git add .
   git commit -m "Add awesome feature"
   git push origin feature/my-awesome-feature
   ```

5. **Open Pull Request** - Submit for review

See [Development Workflow](development-workflow.md) for details.

## Testing

We maintain high test coverage:

- **Unit Tests** - Test individual components in isolation
- **Integration Tests** - Test component interactions
- **End-to-End Tests** - Test complete workflows

```bash
# Run all tests
dotnet test src/c-sharp/NotebookAutomation.sln

# Run specific test project
dotnet test src/c-sharp/NotebookAutomation.Tests/

# Run tests with coverage
dotnet test src/c-sharp/NotebookAutomation.sln --collect:"XPlat Code Coverage"
```

See [Testing Guide](testing.md) for best practices.

## API Documentation

Complete API reference documentation is available:

- **[API Reference](../api/index.md)** - Generated from XML documentation
- **[Core Interfaces](../api/interfaces.md)** - Key abstractions
- **[Processing Services](../api/processors.md)** - Document processors
- **[Configuration](../api/configuration.md)** - Configuration system

## Plugin Development

Extend Notebook Automation with custom processors:

```csharp
public class CustomProcessor : IDocumentProcessor
{
    public async Task<ProcessingResult> ProcessAsync(
        string sourcePath, 
        ProcessingOptions options)
    {
        // Your custom processing logic
        return new ProcessingResult { Success = true };
    }
}

// Register in DI container
services.AddTransient<IDocumentProcessor, CustomProcessor>();
```

See [Plugin Development Guide](plugin-development.md) for complete tutorials.

## Debugging

### Debug the CLI

```bash
# Run with debugger attached (VS Code)
F5 in VS Code with launch.json configured

# Run with verbose logging
dotnet run --project src/c-sharp/NotebookAutomation.Cli -- <command> --verbose
```

### Debug the Obsidian Plugin

1. Enable developer mode in Obsidian
2. Open Developer Tools (Ctrl+Shift+I)
3. Set breakpoints in TypeScript source
4. Trigger plugin functionality

See [Debugging Guide](debugging.md) for advanced techniques.

## Release Process

For maintainers preparing releases:

1. **Update Version** - Bump version in project files
2. **Update Changelog** - Document changes
3. **Run Full Test Suite** - Ensure quality
4. **Build Release** - Create platform-specific packages
5. **Create GitHub Release** - Tag and publish
6. **Update Documentation** - Reflect new features

See [Release Process](release-process.md) for complete checklist.

## Community

### Get Help

- **[GitHub Discussions](https://github.com/danielshue/notebook-automation/discussions)** - Ask questions
- **[GitHub Issues](https://github.com/danielshue/notebook-automation/issues)** - Report bugs
- **[Contributing Guide](contributing.md)** - Learn how to contribute

### Stay Updated

- **⭐ Star the repository** - Get notified of updates
- **👀 Watch releases** - Be informed of new versions
- **📢 Follow discussions** - Participate in feature planning

## Code of Conduct

We are committed to providing a welcoming and inspiring community for all. Please read our [Code of Conduct](../../CODE_OF_CONDUCT.md) before contributing.

## License

Notebook Automation is licensed under the MIT License. See [LICENSE](../../LICENSE.md) for details.

---

**[📖 Documentation Home](../index.md)** • **[🚀 Getting Started](getting-started.md)** • **[🏗️ Architecture](architecture.md)**
