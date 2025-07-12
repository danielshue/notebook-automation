# Developer Guide

Welcome to the Notebook Automation Developer Guide. This section provides comprehensive documentation for developers working with or extending the Notebook Automation system.

## Overview

The Notebook Automation system is designed to process educational content (videos and PDFs) and generate structured markdown notes with AI-powered summaries. The system follows modern C# development practices and includes robust error handling, configurability, and extensibility.

## Documentation Sections

### [Architecture](architecture.md)
High-level system architecture, component relationships, and design patterns used throughout the codebase.

### [AI Summary Processing Flow](ai-summary-flow.md)
Comprehensive guide to how the system processes documents to generate AI summaries, including:
- Video and PDF processing workflows
- Chunking strategies for large documents
- Template and prompt system
- Error handling and retry logic

### [Metadata Schema Loader Integration](../Metadata-Schema-Loader-Integration-Issue.md)
Epic issue documentation for the comprehensive integration of metadata schema loader and reserved tag logic, including:
- Implementation timeline and milestones
- Risk assessment and mitigation strategies
- Team assignments and responsibilities
- Detailed task breakdown and acceptance criteria

### [Contributing](contributing.md)
Guidelines for contributing to the project, including coding standards, pull request processes, and development workflow.

### [Building from Source](building.md)
Instructions for setting up the development environment and building the project from source code.

### [Testing](testing.md)
Testing strategies, test organization, and guidelines for writing effective unit and integration tests.

## Quick Start for Developers

1. **Clone the Repository**
   ```bash
   git clone https://github.com/yourusername/notebook-automation.git
   cd notebook-automation
   ```

2. **Install Dependencies**
   ```bash
   dotnet restore src/c-sharp/NotebookAutomation.sln
   ```

3. **Build the Solution**
   ```bash
   dotnet build src/c-sharp/NotebookAutomation.sln
   ```

4. **Run Tests**
   ```bash
   dotnet test src/c-sharp/NotebookAutomation.sln
   ```

## Key Technologies

- **.NET 9**: Target framework
- **Microsoft Semantic Kernel**: AI integration
- **Xabe.FFmpeg**: Video metadata extraction
- **YamlDotNet**: YAML processing
- **MSTest**: Unit testing framework

## Development Principles

- **Maintainability**: Clear, readable code that prioritizes long-term maintenance
- **SOLID Principles**: Object-oriented design following established patterns
- **Testability**: Comprehensive unit test coverage with proper mocking
- **Configuration**: Externalized configuration for flexibility
- **Logging**: Structured logging for debugging and monitoring

## Support and Community

- **Issues**: Report bugs and feature requests on GitHub
- **Discussions**: Join development discussions
- **Documentation**: Contribute to documentation improvements

For questions or support, please refer to the main project documentation or open an issue on the GitHub repository.
