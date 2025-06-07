---
auto-generated-state: writable
date-created: 2025-06-06
publisher: University of Illinois at Urbana-Champaign
tags: ''
---

# Video Metadata CLI Parity: Python vs C#

## Outstanding Tasks for Full Parity with generate_video_meta.py

- [ ] Add support for all CLI options present in Python:
    - [ ] --single-file / -f (process a single video file)
    - [ ] --folder (process all video files in a directory)
    - [ ] --resources-root (override resources root directory)
    - [ ] --no-summary (disable OpenAI summary generation)
    - [ ] --retry-failed (retry only failed files from previous run)
    - [ ] --force (overwrite existing notes)
    - [ ] --timeout (set API request timeout)
    - [ ] --refresh-auth (force refresh Microsoft Graph API authentication)
    - [ ] --no-share-links (skip OneDrive share link creation)
    - [X] -c / --config (config file path)
    - [X] --verbose, --debug, --dry-run (already present)
- [ ] Implement video file discovery logic (single file, folder, retry failed)
- [ ] Implement transcript file discovery and language-specific handling
- [ ] Integrate OpenAI summary generation (with --no-summary option)
- [ ] Implement OneDrive share link creation (with --no-share-links and --timeout)
- [ ] Implement markdown note generation with metadata, transcript, summary, and share link
- [ ] Implement results and failed files JSON output (video_links_results.json, failed_video_files.json)
- [ ] Add colorized and detailed logging (match Python's rich output)
- [ ] Add progress reporting for batch operations (progress bar, summary)
- [ ] Add robust error handling and logging for all steps
- [ ] Ensure configuration and path handling matches Python logic
- [ ] Add comprehensive unit tests for all new features and edge cases

## Notes

- The current C# implementation (VideoCommands/VideoNoteProcessingEntrypoint) only supports basic input/output and lacks most advanced CLI options and features from the Python version.
- Full feature parity will require significant CLI, service, and utility enhancements in the C# codebase.

# Python to C# Migration Plan

## Overview

This document tracks our progress in migrating the Python codebase to C#. The original Python code is located in `../src/python` folder.

## Migration Phases

### Phase 1: Analysis and Planning

- [ ] Inventory all Python modules and their dependencies
- [ ] Identify third-party libraries used and find C# equivalents
- [ ] Document the current architecture and data flow
- [ ] Define target C# architecture (considering SOLID principles)
- [ ] Establish coding standards for C# implementation
- [ ] Create a test strategy for validating migrated components

### Phase 2: Core Infrastructure Setup

- [ ] Set up C# project structure and solution file
- [ ] Configure build system and dependency management
- [ ] Establish logging framework equivalent to Python implementation
- [ ] Implement configuration management system
- [ ] Create basic utility/helper classes
- [ ] Setup unit testing framework

### Phase 3: Data Model Migration

- [ ] Convert Python data models/classes to C# classes
- [ ] Implement proper property accessors (getters/setters)
- [ ] Ensure data serialization/deserialization works correctly
- [ ] Validate model equivalence through unit tests
- [ ] Document any behavioral differences between Python and C# implementations

### Phase 4: Business Logic Migration

- [ ] Migrate core algorithms and business logic
- [ ] Rewrite service classes
- [ ] Implement interfaces for dependency injection
- [ ] Convert Python-specific patterns to C# equivalents
- [ ] Ensure error handling follows C# conventions
- [ ] Write unit tests for all business logic

### Phase 5: External Integrations

- [ ] Update file I/O operations to use C# patterns
- [ ] Migrate network/API communication code
- [ ] Implement database access layer (if applicable)
- [ ] Update any third-party service integrations
- [ ] Test all external integrations

### Phase 6: User Interface (if applicable)

- [ ] Decide on UI framework (WPF, WinForms, MAUI, etc.)
- [ ] Implement view models/controllers
- [ ] Create user interface elements
- [ ] Connect UI to business logic
- [ ] Implement any Python script command-line interfaces as C# equivalents

### Phase 7: Testing and Validation

- [ ] Complete unit test coverage
- [ ] Perform integration testing
- [ ] Validate functionality against original Python implementation
- [ ] Performance testing and optimization
- [ ] Security review

### Phase 8: Documentation and Deployment

- [ ] Update API documentation
- [ ] Create/update developer guides
- [ ] Document architecture changes
- [ ] Prepare deployment scripts/pipeline
- [ ] Plan deprecation timeline for Python codebase

## Coding Standards and Guidelines

### Project Philosophy

- [ ] Write maintainable, readable code that prioritizes clarity over cleverness
- [ ] Follow SOLID principles in object-oriented design
- [ ] Optimize for developer experience and code readability
- [ ] Consider future maintenance needs in all implementations
- [ ] Create modular, loosely coupled components that can be easily tested and extended

### Code Documentation

- [ ] Ensure all C# files have appropriate XML documentation comments
- [ ] All classes, methods, and properties should have descriptive documentation
- [ ] Use standard C# XML documentation format:

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

### C# Coding Standards

- [ ] Follow Microsoft's C# Coding Conventions
- [ ] Use proper C# naming conventions (PascalCase for public members, camelCase for parameters/local variables)
- [ ] Use explicit types rather than var when appropriate
- [ ] Use property accessors appropriately
- [ ] Implement proper exception handling patterns
- [ ] Use async/await for asynchronous operations
- [ ] Use nullable reference types for safer null handling
- [ ] Apply consistent formatting (use an .editorconfig file)

### Project-Specific Patterns

- [ ] Create C# equivalents for the Python project's patterns
- [ ] Use System.IO.Path or FileSystem abstractions instead of pathlib
- [ ] Implement a centralized logging system (consider NLog, Serilog, or Microsoft.Extensions.Logging)
- [ ] Use built-in configuration systems (Microsoft.Extensions.Configuration)
- [ ] Maintain a similar directory structure where appropriate:
  - `/Models` for data models
  - `/Services` for business logic
  - `/Utilities` for helper functions
  - `/Extensions` for extension methods

### Error Handling

- [ ] Use specific exception types rather than catching Exception
- [ ] Include contextual information in exception messages
- [ ] Implement structured logging with appropriate severity levels
- [ ] Use try/catch blocks with specific exception types
- [ ] Create a centralized error handling system

### Performance Guidelines

- [ ] Prioritize readability over premature optimization
- [ ] Cache results of expensive operations when appropriate
- [ ] Use LINQ efficiently but judiciously
- [ ] Consider IEnumerable<T> and yield return for large datasets
- [ ] Implement progress reporting for long-running operations
- [ ] Document performance-critical sections
- [ ] Choose appropriate data structures for operations

### Security

- [ ] Never hardcode credentials or API keys
- [ ] Use the configuration system for storing settings
- [ ] Implement proper input validation
- [ ] Follow security best practices for file handling and network calls
- [ ] Use secure APIs for sensitive operations

### Testing Approach

- [ ] Write unit tests for all non-trivial methods
- [ ] Organize tests to mirror the code structure
- [ ] Use xUnit, NUnit, or MSTest as the testing framework
- [ ] Mock external dependencies in tests (consider Moq or NSubstitute)
- [ ] Create proper test fixtures for reuse
- [ ] Aim for high test coverage of critical functionality

## Current Status

- Phase: Planning
- Completed Tasks: 0
- In Progress: Initial analysis
- Blocked Items: None

## Next Steps

1. Complete inventory of Python modules
2. Document current architecture
3. Establish C# project structure
4. Begin migrating core data models

## Notes and Decisions

- (Add important decisions and notes about the migration here)

## Weekly Progress Updates

### Week of [Current Date]

- Started migration planning
- Created this tracking document
