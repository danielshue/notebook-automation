---
applyTo: "**/*.cs"
description: Guidelines for reusing existing code and utilities in the Notebook Automation project.
---

# Code Reuse Priority Hierarchy

## 1. Reuse Existing Classes and Utilities First

- Always search for and utilize existing classes in the `src/c-sharp/NotebookAutomation.Core/Tools/` directory before creating new code
- Consider using dependency injection and extending existing services rather than duplicating functionality
- Example approach: Inject existing services through constructor parameters

## 2. Component Discovery Process

When implementing a solution:

1. First analyze the `src/c-sharp/NotebookAutomation.Core/Tools/` directory structure to identify relevant components:
   - `MarkdownGeneration/` - Markdown processing and generation utilities
   - `PdfProcessing/` - PDF handling and annotation extraction
   - `Resolvers/` - Path and resource resolution services
   - `Shared/` - Common utilities and helpers
   - `TagManagement/` - Tag processing and management
   - `Vault/` - Vault operations and file management
   - `VideoProcessing/` - Video content processing
   - `VideoTranscriptProcessing/` - Transcript handling and consolidation
2. Check for functionality that matches or can be adapted to current requirements
3. Consider composition of existing utilities to meet new requirements through dependency injection
4. Only proceed to creating new components when existing ones cannot fulfill requirements

## 3. Component Extension Guidelines

- Do not modify existing public interfaces without careful consideration
- When additional functionality is needed:
  - Create derived classes that extend existing base classes
  - Use composition through constructor injection to combine existing services
  - Create wrapper classes that utilize existing components
  - Add extension methods for cross-cutting concerns

## 4. New Component Creation Criteria

Only create new components when:

- No suitable existing component exists in the `Tools` directory or `Services` namespace
- Extending existing components would violate their single responsibility principle
- The functionality is significantly different from anything available
- Attempting to reuse would create excessive complexity or tight coupling

## 5. Documentation Requirements for Reuse

When reusing components:

- Add XML documentation comments explaining which components were reused and why
- Note any limitations of the reused components
- Explain integration points between existing and new code
- Document any assumptions or dependencies

## 6. New Component Structure

When a new component must be created:

- Place it in the appropriate subdirectory of `src/c-sharp/NotebookAutomation.Core/Tools/` or create a new category if needed
- Follow existing C# naming conventions (PascalCase for classes and public members)
- Create proper interfaces for dependency injection and future reuse
- Add comprehensive XML documentation comments
- Register services in the dependency injection container as appropriate
