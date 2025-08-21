# System Patterns: Notebook Automation Architecture

## Core Architecture Principles

### Location-Agnostic Design

The system is built around a fundamental principle of **location-agnostic processing**, enabling seamless operation across different computers, operating systems, and folder structures.

**Key Components:**

- **Document Placeholders**: Store relative paths in YAML frontmatter
- **Configuration-Based Resolution**: Environment-specific absolute path mapping
- **Multi-Strategy Path Resolution**: Intelligent file discovery across vault structures
- **Graceful Fallbacks**: Handle missing files and configurations

### Processing Pipeline Architecture

```
Input → Detection → Processing → AI Enhancement → Output → Integration
```

**Pipeline Stages:**

1. **Input Detection**: Identify file types and existing processing state
2. **Content Extraction**: Extract text content with format-specific processors
3. **AI Enhancement**: Generate summaries, Q&A, and vocabulary
4. **Template Application**: Apply structured templates with metadata
5. **Output Generation**: Create final markdown with rich frontmatter
6. **Tool Integration**: Integrate with Obsidian, Anki, and other tools

### Intelligent Processing Logic

**Smart Skip Behavior** (Recently Enhanced):

- Automatically detect existing AI content in markdown files
- Skip processing when AI content already exists (unless forced)
- Process files without AI content automatically
- Maintain backward compatibility with explicit force flags

## Key Design Patterns

### Factory Pattern - Content Processors

Different file types are handled by specialized processors:

- `MarkdownNoteProcessor`: HTML content processing
- `VideoNoteProcessor`: Video transcript processing  

- `PdfNoteProcessor`: PDF annotation extraction
- `EpubNoteProcessor`: EPUB content processing

### Template System

**Base Block Templates**: Configurable content structure

- Course hierarchies (Program → Course → Module → Lesson)

- Content types (Video, PDF, Reading, Assignment)
- Metadata schemas for consistent organization

### Dependency Injection

Modern C# dependency injection pattern:

- Service registration in `ServiceRegistration.cs`
- Constructor injection for all major components
- Interface-based design for testability

### Plugin Architecture

**Extensible Resolver System**:

- Custom processors can be added through plugin registration

- Resolver pattern for handling different content types
- Consistent interface contracts for all processors

## Data Flow Patterns

### Configuration Management

**Hierarchical Configuration Resolution**:

1. Command-line arguments (highest priority)
2. Configuration files (`config.json`)

3. Environment variables

4. Default values (lowest priority)

### Metadata Management

**Rich YAML Frontmatter Generation**:

- Course structure metadata

- Processing timestamps and state
- Content relationships and references
- AI-generated content indicators

### Error Handling Strategy

**Defensive Programming Approach**:

- Comprehensive try/catch blocks with specific exception types
- Graceful degradation for missing dependencies
- Detailed logging with contextual information
- Safe fallback behaviors for edge cases

## Component Relationships

### Core Processing Components

```
DocumentNoteBatchProcessor (Base)
├── MarkdownNoteProcessor
├── VideoNoteProcessor
├── PdfNoteProcessor
└── EpubNoteProcessor

```

### Configuration and Services

```
ConfigManager

├── AppConfig
├── YamlHelper


├── FileSystemWrapper
└── EnvironmentWrapper
```

### Integration Layer

```
VaultRootContextService



├── OneDriveService
├── MetadataTemplateManager
└── PathFormatter


```

## Modern C# Patterns

### File-Scoped Namespaces

All new files use file-scoped namespace declarations for cleaner code structure.

### Primary Constructors

Used for simple dependency injection scenarios with validation.

### Collection Expressions

Modern collection initialization syntax for arrays and lists.

### Pattern Matching

Extensive use of switch expressions and pattern matching for cleaner conditional logic.

### Async/Await Best Practices

- `ConfigureAwait(false)` for library code

- Proper cancellation token propagation
- Exception handling in async methods

## Integration Patterns

### Obsidian Plugin Integration

**Context Menu System**:

- Native file explorer integration
- Real-time processing feedback
- Configuration through plugin settings

### CLI Tool Design

**Command Pattern Implementation**:

- Separate command classes for different operations

- Standardized argument parsing and validation
- Consistent output formatting and logging

### OneDrive Synchronization

**Bidirectional Sync Strategy**:

- Folder structure synchronization without content transfer
- Shared link management for collaborative access
- Conflict resolution for concurrent modifications

## Performance Patterns

### Batch Processing Optimization

**Progress Tracking and Reporting**:

- Real-time progress updates during batch operations
- Detailed timing and performance metrics
- Memory-efficient processing for large content collections

### Caching Strategy

**Intelligent Content Detection**:

- Cache AI content detection results during session
- Efficient YAML parsing and metadata extraction
- File system operation optimization

This architecture enables robust, scalable, and maintainable content processing while providing excellent user experience across different platforms and use cases.
