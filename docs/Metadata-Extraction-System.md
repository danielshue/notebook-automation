# Metadata Extraction System Documentation

## Overview

The NotebookAutomation system employs a sophisticated multi-layered approach to automatically extract and assign metadata to files in your notebook vault. This documentation provides a comprehensive breakdown of how the system determines course, lesson, program, and module metadata.

## Architecture

The metadata extraction system consists of several specialized components:

- **`MetadataHierarchyDetector`** - Extracts program, course, and class information from directory hierarchy
- **`CourseStructureExtractor`** - Extracts module and lesson information from filenames and directory patterns
- **`TagProcessor`** - Applies extracted metadata to files
- **`MetadataEnsureProcessor`** - Orchestrates the entire metadata extraction process

## Metadata Field Extraction

### 1. Program Detection

The `MetadataHierarchyDetector` determines program information using the following priority order:

#### Priority 1: Explicit Override

- CLI parameter: `--program "Program Name"`
- Takes highest precedence when specified

#### Priority 2: Special Cases

- **Value Chain Management**: Hardcoded detection for "Value Chain Management" in path
- Handles special sub-project structure with `01_Projects` level

#### Priority 3: YAML Index Scanning

- Searches for `main-index.md` and `program-index.md` files
- Extracts `title` field from YAML frontmatter
- Scans up directory tree from file location

#### Priority 4: Path-based Fallback

- Uses directory names as program identifiers
- Analyzes directory structure relative to vault root

#### Priority 5: Default Fallback

- Assigns "MBA Program" if no other method succeeds

### 2. Course Detection

Course information is extracted using:

#### YAML Frontmatter (Primary)

```yaml
---
title: "Strategic Management"
type: course-index
---
```

#### Directory Structure (Secondary)

- Course folders positioned after program folders in hierarchy
- For Value Chain Management: Course appears after program (or after `01_Projects`)

#### Path Analysis (Fallback)

- Second level directory after program in hierarchy
- Directory name cleaning and formatting applied

### 3. Class Detection

Similar to course detection but looks for:

#### YAML Frontmatter

```yaml
---
title: "Operations Strategy"
type: class-index
---
```

#### Directory Positioning

- Third level in hierarchy: Program → Course → Class
- Scans for `class-index.md` files in directory tree

### 4. Module Detection

The `CourseStructureExtractor` uses multiple strategies for module extraction:

#### Strategy 1: Filename Pattern Recognition

**Supported Patterns:**

```text
Module-1-Introduction.pdf        → "Module 1 Introduction"
Module1BasicConcepts.mp4        → "Module 1 Basic Concepts"
Week1-Introduction.pdf          → "Week1 Introduction"
Unit-2-Advanced.pdf             → "Unit 2 Advanced"
01_course-overview.pdf          → "Course Overview Introduction"
02_session-planning-details.md  → "Session Planning Details"
```

**Regex Patterns Used:**

- Module filename: `(?i)module\s*[_-]?\s*(\d+)[_-]?\s*(.+?)(?:\.\w+)?$`
- Lesson filename: `(?i)lesson\s*[_-]?\s*(\d+)[_-]?\s*(.+?)(?:\.\w+)?$`
- Week/Unit filename: `(?i)(week|unit|session|class)\s*[_-]?\s*(\d+)[_-]?\s*(.+?)(?:\.\w+)?$`
- Compact module: `(?i)module(\d+)([a-zA-Z]+.*)`
- Numbered content: `^(\d+)[_-](.+)`

#### Strategy 2: Directory Keyword Search

**Keywords Detected:**

- "module" (case-insensitive)
- "course"
- "week"
- "unit"

**Process:**

1. Scans current directory name
2. Checks parent directories
3. Prioritizes explicit module keywords

#### Strategy 3: Numbered Directory Pattern Analysis

**Pattern Recognition:**

- Numbered prefixes: `01_`, `02-`, `03_`, etc.
- Enhanced patterns: "Week 1", "Unit 2", "Module 1", "Session 3"

**Directory Hierarchy Logic:**

```text
01_advanced-module/              ← Module (parent directory)
  02_detailed-lesson/            ← Lesson (child directory)
    video.mp4                    ← File gets both module + lesson
```

#### Strategy 4: Text Processing & Cleaning

**Cleaning Operations:**

1. Remove numbering prefixes (`01_`, `02-`)
2. Convert camelCase to spaced words ("BasicConcepts" → "Basic Concepts")
3. Replace hyphens and underscores with spaces
4. Apply title case formatting
5. Remove extra whitespace

**Regex Patterns for Cleaning:**

- Number prefix removal: `^(\d+)[_-]`
- CamelCase splitting: `(?<=[a-z])(?=[A-Z])`
- Whitespace normalization: `\s+`

### 5. Lesson Detection

Lesson extraction follows similar strategies as module detection:

#### Filename-Based Extraction

```text
Lesson-2-Details.md             → "Lesson 2 Details"
Lesson3AdvancedTopics.docx      → "Lesson 3 Advanced Topics"
Session-1-Introduction.pdf      → "Session 1 Introduction"
```

#### Directory Keyword Detection

**Keywords:**

- "lesson" (case-insensitive)
- "session"
- "lecture"
- "class"

#### Hierarchical Directory Analysis

**Logic Rules:**

1. If parent directory contains module indicators AND current directory is numbered → current = lesson
2. Module indicators: "module", "course", "week", "unit"
3. Uses numbered directory patterns to establish parent-child relationships

## Decision Flow & Logic

### Overall Processing Order

1. **Filename Analysis**: First attempts extraction from filename patterns
2. **Keyword Search**: Looks for explicit module/lesson keywords in directories
3. **Pattern Analysis**: Analyzes numbered directory structures
4. **Hierarchical Inference**: Uses directory relationships to determine module vs lesson
5. **Single-level Handling**: Treats standalone numbered directories as modules

### Single vs Multi-Level Course Handling

#### Single-Level Courses

```text
Course/
  01_introduction-to-strategy/
    video.mp4                    ← Gets module: "Introduction To Strategy"
```

#### Multi-Level Courses

```text
Course/
  01_strategy-fundamentals/      ← Module: "Strategy Fundamentals"
    02_competitive-analysis/     ← Lesson: "Competitive Analysis"
      video.mp4                  ← Gets both module + lesson
```

### Special Cases

#### Case Studies

- Typically generate module metadata only
- Lesson metadata usually not assigned for case study content
- Depends on directory structure and naming

#### Live Sessions

- May be handled as lessons depending on directory structure
- "Live Session" directories often treated as lesson containers

#### Mixed Content

- System prioritizes most specific pattern match
- Filename patterns take precedence over directory patterns

## Integration Points

### MetadataEnsureProcessor Flow

1. Creates `MetadataHierarchyDetector` instance
2. Creates `CourseStructureExtractor` instance
3. Calls `FindHierarchyInfo()` for program/course/class
4. Calls `ExtractModuleAndLesson()` for module/lesson
5. Passes extracted metadata to `TagProcessor`

### Metadata Field Updates

- **ADD operations**: When metadata field doesn't exist
- **MODIFY operations**: When improving existing metadata (generic → specific)
- **PRESERVE operations**: Good existing metadata is not overwritten

### Logging and Debugging

#### Verbose Mode

Enable with CLI flag for detailed extraction logging:

```bash
dotnet run -- vault ensure-metadata --verbose
```

#### Log Analysis

Common log patterns:

```text
[INFO] Found 'Value Chain Management' in path, using it as program name
[DEBUG] Filename extraction result - Module: Module 1 Introduction, Lesson: null
[DEBUG] Successfully extracted - Module: 'Strategy Fundamentals', Lesson: 'Competitive Analysis'
```

## Configuration

### Configurable Elements

#### CLI Parameters

- `--program "Program Name"` - Override program detection
- `--verbose` - Enable detailed logging
- `--config path/to/config.json` - Custom configuration file

#### Configuration File

```json
{
  "Paths": {
    "NotebookVaultFullpathRoot": "C:/path/to/vault"
  },
  "Logging": {
    "LogLevel": "Information"
  }
}
```

### Customization Options

#### Regex Pattern Modification

The system uses compiled regex patterns that can be modified in:

- `CourseStructureExtractor.cs` - Module/lesson filename patterns
- `MetadataHierarchyDetector.cs` - Hierarchy detection patterns

#### Keyword Lists

Add new keywords for module/lesson detection by modifying the keyword detection logic in `CourseStructureExtractor`.

## Testing

### Unit Tests

Comprehensive test coverage in:

- `CourseStructureExtractorTests.cs` - Tests all extraction strategies
- `MetadataHierarchyDetectorTests.cs` - Tests hierarchy detection

### Test Categories

- Filename pattern recognition
- Directory structure analysis
- Hierarchical relationship detection
- Text cleaning and formatting
- Special case handling

### Running Tests

```bash
dotnet test src/c-sharp/NotebookAutomation.Core.Tests/
```

## Performance Considerations

### Efficiency Optimizations

- Regex patterns are compiled for better performance
- Directory scanning limited to necessary levels
- Caching of frequently accessed configuration values

### Memory Management

- Uses readonly and static members where appropriate
- Disposes of file system resources properly
- Minimal object allocation in hot paths

## Troubleshooting

### Common Issues

#### Missing Metadata

1. Check file path structure matches expected hierarchy
2. Verify filename patterns match supported formats
3. Enable verbose logging to see extraction attempts

#### Incorrect Module/Lesson Assignment

1. Review directory naming conventions
2. Check for conflicting patterns in path
3. Verify numbered prefixes are correctly formatted

#### Program/Course Detection Failures

1. Ensure index files have proper YAML frontmatter
2. Check vault root path configuration
3. Verify directory structure follows expected hierarchy

### Debug Commands

```bash
# Test specific file
dotnet run -- vault ensure-metadata --file "path/to/file.md" --verbose

# Test directory
dotnet run -- vault ensure-metadata --directory "path/to/dir" --verbose

# Dry run to see what would change
dotnet run -- vault ensure-metadata --dry-run --verbose
```

## Best Practices

### Directory Organization

- Use consistent numbering schemes (`01_`, `02_`, etc.)
- Include descriptive names after numbers
- Maintain clear hierarchy: Program → Course → Class → Module → Lesson

### Filename Conventions

- Include module/lesson indicators in filenames when possible
- Use consistent separators (hyphens or underscores)
- Avoid special characters that might interfere with pattern matching

### Index File Management

- Create index files with proper YAML frontmatter
- Use descriptive titles in frontmatter
- Maintain index files at appropriate hierarchy levels

This documentation should be updated as the system evolves and new patterns or features are added.
