# Technology Context: Notebook Automation

## Technology Stack

### Core Platform

**Runtime Environment:**

- **.NET 9.0**: Latest LTS version with modern C# language features
- **C# 13**: File-scoped namespaces, primary constructors, collection expressions
- **Cross-Platform**: Windows, macOS, Linux support

**Application Architecture:**

- **CLI Application**: Command-line interface for automation and scripting
- **Obsidian Plugin**: TypeScript/JavaScript plugin for native integration
- **Library Components**: Reusable core processing libraries

### Key Dependencies

**AI and ML Integration:**

- **OpenAI API**: GPT models for content summarization and analysis
- **Custom AI Service Layer**: Abstraction for multiple AI providers
- **Token Management**: Efficient API usage and cost optimization

**File Processing:**

- **HtmlAgilityPack**: HTML parsing and content extraction
- **YamlDotNet**: YAML frontmatter processing
- **System.Text.Json**: JSON configuration and serialization
- **iText7/PdfPig**: PDF processing and annotation extraction

**Development Infrastructure:**

- **MSTest**: Unit testing framework
- **Moq**: Mocking framework for isolated testing
- **Microsoft.Extensions.DI**: Dependency injection container
- **Microsoft.Extensions.Logging**: Structured logging framework

### Build and Deployment

**Build System:**

- **MSBuild**: Primary build system with custom targets
- **PowerShell Scripts**: Cross-platform build automation
- **GitVersion**: Semantic versioning with Git integration
- **GitHub Actions**: CI/CD pipeline for automated builds and releases

**Packaging and Distribution:**

- **Self-Contained Deployments**: Platform-specific executables
- **NuGet Packages**: Library component distribution
- **Obsidian Plugin Store**: Plugin distribution through community channels

## Development Environment

### IDE and Tooling

**Primary Development:**

- **Visual Studio Code**: Primary IDE with C# and TypeScript support
- **C# Dev Kit**: Microsoft's official C# extension for VS Code
- **Obsidian**: Plugin development and testing environment

**Code Quality Tools:**

- **EditorConfig**: Consistent code formatting across editors
- **Roslyn Analyzers**: Static code analysis for C#
- **ESLint/Prettier**: JavaScript/TypeScript code quality
- **Markdownlint**: Documentation quality enforcement

### Testing Infrastructure

**Unit Testing:**

- **MSTest Framework**: Modern test framework with attributes and assertions
- **Moq**: Behavior verification and dependency isolation
- **Test Coverage**: Coverlet for code coverage reporting
- **Test Data Management**: Structured test fixtures and sample data

**Integration Testing:**

- **File System Testing**: Temporary directories and file manipulation
- **Configuration Testing**: Multiple environment scenarios
- **Plugin Testing**: Obsidian vault integration testing

## Configuration Management

### Configuration Sources

**Hierarchical Configuration:**

1. **Command-line Arguments**: Highest priority, runtime overrides
2. **Configuration Files**: JSON-based structured configuration
3. **Environment Variables**: System-level configuration
4. **Default Values**: Fallback configuration

**Configuration Schema:**

- **Typed Configuration Classes**: Strong typing with validation
- **Nested Configuration**: Hierarchical organization of settings
- **Environment-Specific**: Development, testing, production variants

### Environment Setup

**Development Requirements:**

- **.NET 9.0 SDK**: Required for building and running
- **PowerShell**: Cross-platform scripting for build automation
- **Node.js**: Required for Obsidian plugin development
- **Git**: Version control and build versioning

**Optional Dependencies:**

- **Docker**: Containerized development environment
- **Azure CLI**: Cloud deployment capabilities
- **Obsidian**: Plugin testing and development

## Data Processing Architecture

### Content Processing Pipeline

**Input Handlers:**

- **File Type Detection**: Automatic format identification
- **Content Extractors**: Format-specific content extraction
- **Metadata Parsers**: YAML frontmatter and document metadata

**Processing Engine:**

- **Batch Processing**: Efficient handling of multiple files
- **Progress Tracking**: Real-time processing status
- **Error Recovery**: Graceful handling of processing failures

**Output Generators:**

- **Template System**: Configurable output formatting
- **Markdown Generation**: Rich markdown with frontmatter
- **Tool Integration**: Obsidian and Anki export formats

### AI Integration Architecture

**Service Abstraction:**

- **AI Service Interface**: Provider-agnostic AI integration
- **Configuration Management**: API keys and service settings
- **Error Handling**: Network failures and API limits
- **Caching Strategy**: Efficient API usage patterns

**Content Analysis:**

- **Summarization**: Multi-level content summaries
- **Question Generation**: Educational Q&A creation
- **Vocabulary Extraction**: Key terms and definitions
- **Metadata Enhancement**: Automated tagging and categorization

## Security and Privacy

### Data Handling

**Local Processing:**

- **No Cloud Storage**: All processing happens locally
- **Temporary Files**: Secure temporary file handling
- **API Key Management**: Secure storage of sensitive credentials

**Privacy Protection:**

- **Content Privacy**: User content never stored remotely
- **Configurable AI Usage**: Optional AI processing features
- **Audit Logging**: Transparent processing activity logs

### Security Best Practices

**Code Security:**

- **Input Validation**: Sanitization of all user inputs
- **Path Traversal Protection**: Safe file system operations
- **Dependency Scanning**: Regular security vulnerability scans
- **Secret Management**: Secure handling of API keys and tokens

## Performance Characteristics

### Optimization Strategies

**File Processing:**

- **Streaming Processing**: Memory-efficient file handling
- **Parallel Processing**: Multi-threaded batch operations
- **Caching**: Intelligent caching of processed content
- **Progress Reporting**: Real-time feedback for long operations

**Resource Management:**

- **Memory Usage**: Efficient memory allocation and cleanup
- **Disk Usage**: Temporary file management and cleanup
- **Network Usage**: Optimized AI API requests
- **CPU Usage**: Balanced processing load distribution

### Scalability Considerations

**Batch Processing:**

- **Large File Collections**: Efficient handling of thousands of files
- **Progress Persistence**: Resume interrupted batch operations
- **Error Recovery**: Retry mechanisms for failed operations
- **Resource Limits**: Configurable processing constraints

This technology foundation provides a robust, scalable, and maintainable platform for educational content processing while maintaining excellent performance and user experience across different platforms and use cases.