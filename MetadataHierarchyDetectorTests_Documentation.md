---
auto-generated-state: writable
date-created: 2025-06-06
publisher: University of Illinois at Urbana-Champaign
tags: ''
---

# MetadataHierarchyDetectorTests - Detailed Method Documentation

## Overview

This test class validates the functionality of the `MetadataHierarchyDetector` utility, which is responsible for detecting organizational hierarchy from file paths within an Obsidian-style vault structure and updating metadata with appropriate hierarchy information.

## Test Architecture

- **Vault Structure**: Program → Course → Class → Module → Lesson/Content
- **Path-Based Detection**: Hierarchy is determined solely by folder depth relative to vault root
- **Index File Naming**: Index files are named after their containing folder (e.g., MBA.md)
- **Metadata Inclusion**: Only appropriate hierarchy levels are included based on index type

---

## Test Methods Documentation

### Setup and Teardown Methods

#### `Setup()`

**Purpose**: Initializes test dependencies before each test method execution.
**Functionality**:

- Creates mock logger for `MetadataHierarchyDetector`
- Sets up real `AppConfig` instance with temporary vault root path
- Stores configuration in `_testAppConfig` field for test usage

#### `Cleanup()`

**Purpose**: Cleans up temporary files and directories after tests complete.
**Functionality**:

- Removes temporary vault directory structure
- Ensures no test artifacts remain on the filesystem

---

### Core Hierarchy Detection Tests

#### `FindHierarchyInfo_ValueChainManagementPath_DetectsCorrectHierarchy()`

**Purpose**: Validates hierarchy detection for a standard 4-level path structure.
**Test Structure**: VaultRoot → Value Chain Management → Supply Chain → Class 1 → video.mp4
**Expected Results**:

- Program: "Value Chain Management"
- Course: "Supply Chain"
- Class: "Class 1"
- Module: "" (empty, as this is only 4 levels deep)
**Validation**: Tests basic path-based hierarchy detection without hardcoded program names.

#### `FindHierarchyInfo_ProjectsStructurePath_DetectsCorrectHierarchy()`

**Purpose**: Tests hierarchy detection for project-based folder structures.
**Test Structure**: VaultRoot → Value Chain Management → Supply Chain → Project 1 → video.mp4
**Expected Results**:

- Program: "Value Chain Management"
- Course: "Supply Chain"
- Class: "Project 1"
- Module: "" (empty)
**Validation**: Ensures "Projects" naming doesn't trigger special handling (pure path-based).

#### `FindHierarchyInfo_CompleteVaultStructure_DetectsCorrectHierarchy()`

**Purpose**: Comprehensive test using the full temporary vault structure created by `CreateTemporaryVaultStructure()`.
**Test Coverage**:

- Tests multiple file types (video transcripts, lecture files, etc.)
- Validates hierarchy at different depth levels
- Ensures consistency across various content types
**Expected Hierarchy**: MBA → Program → Finance → Investment → Fundamentals → Intro
**Validation**: Tests deep hierarchy detection (6+ levels) and content file handling.

---

### Lesson and Module Level Tests

#### `FindHierarchyInfo_LessonLevelHierarchy_DetectsCorrectHierarchy()`

**Purpose**: Specifically validates hierarchy detection at the lesson level (deepest content level).
**Test Structure**: Tests both lesson folders and content files within lessons
**Key Validations**:

- Lesson-level folders correctly detect all 4 hierarchy levels
- Content files within lessons inherit the same hierarchy as their parent lesson
- Module level is properly detected for deep hierarchies
**Expected Results**: All 4 hierarchy levels (program, course, class, module) populated.

#### `FindHierarchyInfo_ModuleLevelHierarchy_DetectsCorrectHierarchy()`

**Purpose**: Tests hierarchy detection specifically at the module level (4th level).
**Test Scenarios**:

- Standard module folder (Fundamentals)
- Alternative module folder (Case Studies)
**Key Validations**:
- Module-level folders detect parent class as "module" value
- All higher hierarchy levels are correctly identified
- Different module types follow same detection logic

#### `FindHierarchyInfo_CaseStudyContent_DetectsCorrectHierarchy()`

**Purpose**: Validates hierarchy detection for specialized content types like case studies.
**Test Structure**: Case Studies → Market Analysis → content files
**Key Validations**:

- Case study content follows same hierarchy rules
- Specialized content types don't break detection logic
- Sub-modules within modules are handled correctly

#### `FindHierarchyInfo_DeepContentHierarchy_ValidatesAllLevels()`

**Purpose**: Comprehensive validation of hierarchy detection across multiple depth levels and content types.
**Test Coverage**:

- Multiple test cases with different expected hierarchies
- Various content types (transcripts, lectures, courses, programs)
- Edge cases and boundary conditions
**Validation Method**: Uses parameterized test approach to validate multiple scenarios.

---

### Metadata Update Tests

#### `UpdateMetadataWithHierarchy_AddsHierarchyInfo()`

**Purpose**: Tests basic functionality of adding hierarchy information to metadata.
**Test Scenario**: Standard metadata dictionary with hierarchy information
**Key Validations**:

- Hierarchy values are correctly added to metadata
- Original metadata values are preserved
- No existing values are overwritten

#### `UpdateMetadataWithHierarchy_DoesNotOverrideExistingValues()`

**Purpose**: Ensures that existing metadata values are not overwritten when hierarchy is added.
**Test Scenario**: Metadata already containing hierarchy values
**Key Validations**:

- Pre-existing hierarchy values remain unchanged
- New hierarchy values don't override existing ones
- Original metadata integrity is maintained

#### `UpdateMetadataWithHierarchy_RespectsIndexTypeHierarchy()`

**Purpose**: Validates that different index types include appropriate hierarchy levels.
**Test Scenarios**:

- Program index (includes only program)
- Course index (includes program, course)
- Class index (includes program, course, class)
- Content files (include all available levels)
**Key Validations**:
- Index type determines which hierarchy levels are included
- Lower-level indices don't include inappropriate upper-level information
- Content files receive full hierarchy context

#### `UpdateMetadataWithHierarchy_LessonIndex_IncludesAllHierarchyLevels()`

**Purpose**: Specifically tests that lesson-level indices include all hierarchy information.
**Test Scenario**: Lesson index with complete 4-level hierarchy
**Key Validations**:

- All hierarchy levels (program, course, class, module) are included
- Original metadata (title, type) is preserved
- Lesson indices provide full hierarchical context

#### `UpdateMetadataWithHierarchy_ModuleIndex_IncludesCorrectHierarchyLevels()`

**Purpose**: Validates that module-level indices include appropriate hierarchy levels.
**Test Scenario**: Module index with complete hierarchy information
**Key Validations**:

- All relevant hierarchy levels are included for module indices
- Module-level metadata receives proper hierarchical context
- Original metadata integrity is maintained

---

### Infrastructure and Utility Tests

#### `CreateTemporaryVaultStructure_CreatesCorrectDirectoryHierarchy()`

**Purpose**: Validates that the test vault builder creates the expected directory structure.
**Test Coverage**:

- All expected directories are created
- Directory hierarchy matches the designed structure
- Folder names and organization are correct

#### `CreateTemporaryVaultStructure_CreatesCorrectIndexFiles()`

**Purpose**: Ensures index files are created with correct naming convention.
**Key Validations**:

- Index files are named after their containing folder
- All expected index files are present
- Index file placement follows hierarchy rules

#### `CreateTemporaryVaultStructure_CreatesCorrectContentFiles()`

**Purpose**: Validates creation of content files (videos, transcripts, notes, etc.).
**Test Coverage**:

- All content file types are created
- Content files are placed in appropriate locations
- File naming follows expected conventions

#### `CreateTemporaryVaultStructure_CreatesMinimalVaultStructure()`

**Purpose**: Tests minimal vault creation (single class without complex hierarchy).
**Test Scenario**: Basic vault with minimal structure
**Key Validations**:

- Minimal structures are handled correctly
- Single-level vaults work as expected
- No unnecessary complexity is added

#### `CreateTemporaryVaultStructure_CreatesAdditionalContentTypes()`

**Purpose**: Validates creation of specialized content types (case studies, instructions, data files).
**Test Coverage**:

- Case study materials
- Instructional content
- Data files (Excel, etc.)
- Template files

#### `CreateTemporaryVaultStructure_ReturnsAllExpectedPathKeys()`

**Purpose**: Ensures the vault builder returns comprehensive path mapping.
**Key Validations**:

- All created paths are included in return dictionary
- Path keys match expected naming conventions
- No paths are missing from the return value

#### `CreateTemporaryVaultStructure_VaultRootAccessible()`

**Purpose**: Validates that vault root path is accessible and properly configured.
**Key Validations**:

- Vault root path exists and is accessible
- Path permissions allow read/write operations
- Vault root is properly configured in test setup

#### `VaultRoot_ExposesCorrectPath()`

**Purpose**: Tests that vault root path is correctly exposed for testing purposes.
**Key Validations**:

- Vault root path matches configuration
- Path is accessible from test methods
- Configuration consistency is maintained

#### `CreateTemporaryVaultStructure_CreatesCompleteHierarchy()`

**Purpose**: Comprehensive test of complete vault hierarchy creation.
**Test Coverage**:

- Full hierarchy from program to lesson level
- All intermediate levels are properly created
- Hierarchy relationships are correctly established

#### `CreateTemporaryVaultStructure_HandlesMultipleInvocations()`

**Purpose**: Ensures vault builder handles multiple calls correctly (idempotency).
**Key Validations**:

- Multiple invocations don't break existing structure
- Consistent results across multiple calls
- No conflicts or duplicate creation issues

---

### Debug and Development Tests

#### `DebugPathStructure()`

**Purpose**: Development utility for visualizing vault structure and hierarchy detection.
**Functionality**:

- Prints complete vault structure to console
- Shows hierarchy detection results for various paths
- Provides detailed debugging information for development
**Note**: This is a development/debugging test, not typically run in production test suites.

---

### New Content Type Tests

#### `FindHierarchyInfo_NewContentTypes_DetectsCorrectHierarchy()`

**Purpose**: Validates hierarchy detection for modern content types (videos, case studies, interactive content).
**Test Coverage**:

- Video content with transcripts
- Case study materials with data files
- Interactive content and templates
**Key Validations**:
- New content types follow same hierarchy rules
- Specialized content doesn't break detection logic
- Content type diversity is properly handled

---

## Test Data and Structures

### Temporary Vault Structure

The `CreateTemporaryVaultStructure()` method creates a comprehensive test vault with:

```
TestVault/
├── MBA/                          (Program Level)
│   ├── MBA.md
│   ├── Program/                  (Course Level)
│   │   ├── Program.md
│   │   └── Finance/              (Class Level)
│   │       ├── Finance.md
│   │       └── Investment/       (Module Level)
│   │           ├── Investment.md
│   │           ├── Fundamentals/ (Lesson Level)
│   │           │   ├── Fundamentals.md
│   │           │   └── Intro/    (Content Level)
│   │           │       ├── Intro.md
│   │           │       ├── video-transcript.md
│   │           │       ├── lecture.mp4
│   │           │       ├── lecture-notes.md
│   │           │       └── required-reading.md
│   │           └── Case Studies/
│   │               ├── Case Studies.md
│   │               └── Market Analysis/
│   │                   ├── Market Analysis.md
│   │                   ├── instructions.md
│   │                   ├── market-data.xlsx
│   │                   └── submission-template.md
│   └── Resources/
│       ├── Resources.md
│       └── essay-template.md
└── SingleClass/                  (Minimal Structure)
    └── SingleClass.md
```

### Hierarchy Mapping

- **Level 1**: Program (e.g., "MBA")
- **Level 2**: Course (e.g., "Program")
- **Level 3**: Class (e.g., "Finance")
- **Level 4**: Module (e.g., "Investment")
- **Level 5+**: Lesson/Content (e.g., "Fundamentals", "Intro")

### Index Type Behavior

- **program-index**: Includes only program level
- **course-index**: Includes program, course levels
- **class-index**: Includes program, course, class levels
- **module-index**: Includes program, course, class, module levels
- **lesson-index**: Includes all hierarchy levels
- **content**: Includes all available hierarchy levels
