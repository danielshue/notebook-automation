# Metadata Schema Configuration Guide

This guide provides comprehensive documentation for the `metadata-schema.yml` file, which serves as the central configuration for all metadata operations in the Notebook Automation toolkit.

## Overview

The `metadata-schema.yml` file defines the structure, validation rules, and behavior for all metadata processing operations. It replaces the legacy `metadata.yaml` approach with a unified, extensible schema system that supports:

- **Template Type Definitions**: Structured schema for different content types
- **Field Value Resolvers**: Dynamic field population through registered resolvers
- **Inheritance System**: Recursive inheritance from base types and universal fields
- **Reserved Tag Logic**: Protected system tags with automatic validation
- **Type Mapping**: Canonical type normalization and aliasing

> Note: As of the schema-driven metadata pipeline, all processors (PDF, Video, etc.) build frontmatter via this schema and a registry of resolvers. See “Pipeline Integration” and “Required Resolvers” below.

## Schema Loader and Registry Pattern

The **MetadataSchemaLoader** serves as the central component for schema-driven metadata automation, supporting:

- **Unified Schema File**: Single `metadata-schema.yml` configuration for all metadata operations
- **Registry Pattern**: Dynamic registration and runtime extension of field value resolvers  
- **Plugin Extensibility**: Support for custom resolvers loaded from DLL plugins
- **Inheritance System**: Recursive template type inheritance with base types and universal fields
- **Validation**: Robust schema validation with reserved tag enforcement

```csharp
// Load schema and register resolvers
var schemaLoader = new MetadataSchemaLoader("config/metadata-schema.yml", logger);
schemaLoader.LoadResolversFromDirectory("./resolvers");

// Access template definitions
var pdfSchema = schemaLoader.TemplateTypes["pdf-reference"];

// Resolve field values dynamically
var dateCreated = schemaLoader.ResolveFieldValue("pdf-reference", "date-created", context);
```

## Pipeline Integration

The schema powers a unified metadata pipeline that all note processors use:

- `IMetadataPipeline` orchestrates: optional AI/legacy frontmatter parsing (via `IYamlHelper`), running context resolvers (hierarchy and course structure), file-type resolvers (PDF page-count, video duration), and the OneDrive share link resolver.
- `IMetadataTemplateManager` applies template definitions from the schema using a template key (e.g., `pdf-reference`, `video-reference`).
- Merge precedence is enforced: CLI overrides > existing frontmatter (AI/legacy) > resolver-derived values > schema defaults.
- Required fields are validated after merge; violations are logged and should be surfaced to the caller.

For a conceptual overview, see the Metadata Extraction System document.

## Reserved Tags and Universal Fields

The metadata schema system enforces consistent metadata through reserved tags and universal fields:

- **Universal Fields**: Automatically inherited by all template types (e.g., `auto-generated-state`, `date-created`, `publisher`)
- **Reserved Tags**: Protected system tags that cannot be overridden (e.g., `case-study`, `video`, `pdf`)
- **Field Inheritance**: Reserved tags are automatically included as fields in all template types
- **Validation**: Automatic validation prevents accidental overrides and ensures data integrity

**Schema Structure:**

```yaml
# NOTE: All top-level keys must use PascalCase for C# compatibility
TemplateTypes:
  pdf-reference:
    BaseTypes:
      - universal-fields
    Type: note/case-study
    RequiredFields:
      - comprehension
      - status
      - tags
    Fields:
      date-created:
        Resolver: DateCreatedResolver
      share-link:
        Resolver: OneDriveShareLinkResolver
      status:
        Default: unread

UniversalFields:
  - auto-generated-state
  - date-created
  - publisher

ReservedTags:
  - auto-generated-state
  - case-study
  - video
  - pdf
```

## Migration from Legacy metadata.yaml

**Breaking Change**: The system has migrated from legacy `metadata.yaml` to the new `metadata-schema.yml` format.

**Key Changes:**

- File extension changed from `.yaml` to `.yml`
- Schema structure unified under PascalCase top-level keys
- Template definitions restructured with inheritance support
- Reserved tag logic enforced automatically

**Migration Steps:**

1. Update file references from `metadata.yaml` to `metadata-schema.yml`
2. Convert template definitions to new schema structure
3. Update code to use `MetadataSchemaLoader` instead of legacy template managers
4. Test reserved tag inheritance and validation

For detailed migration instructions, see the [Migration Guide](migration-guide.md).

Related docs:

- [Metadata Extraction System](./Metadata-Extraction-System.md)
- [Template, Type, and Tagging Guide](./Template-Metadata-Guide.md)

## File Structure

### Top-Level Sections

The schema file is organized into four main sections, all using **PascalCase** keys:

```yaml
# NOTE: All top-level keys must use PascalCase for C# compatibility
TemplateTypes:    # Template type definitions with inheritance
UniversalFields:  # Fields inherited by all template types
TypeMapping:      # Template type to canonical type mapping
ReservedTags:     # Protected system tags
```

### Case Sensitivity Requirements

**Critical**: All top-level YAML keys must use PascalCase (e.g., `TemplateTypes`, `UniversalFields`, `TypeMapping`, `ReservedTags`) to match C# property names. The deserializer is case-sensitive and will fail with incorrect casing.

## TemplateTypes Section

Defines the schema for each template type, including inheritance, required fields, and field definitions.

### Structure

```yaml
TemplateTypes:
  template-type-name:
    BaseTypes:          # Optional: List of base types to inherit from
      - universal-fields
      - other-template-type
    Type: canonical-type-name    # Canonical type for normalization
    Prompt: prompt-file-name     # Optional: AI prompt file to use (without .md extension)
    PathResolution:              # Optional: Input/output path configuration
      InputRoot: onedrive | vault | cwd
      OutputRoot: vault | onedrive | cwd | input
    RequiredFields:     # List of required fields for validation
      - field-name-1
      - field-name-2
    Fields:             # Field definitions with defaults and resolvers
      field-name:
        Default: default-value
        Resolver: ResolverName
```

### New Properties

#### Prompt Property

Specifies which AI prompt file to use for generating summaries:

```yaml
TemplateTypes:
  video-reference:
    Prompt: video-reference  # Uses prompts/video-reference.md
  generic-video:
    Prompt: generic_prompt   # Uses prompts/generic_prompt.md
```

**Characteristics:**
- Inherited from base types if not specified
- Can be overridden via CLI `--prompt` option
- Falls back to `final_summary_prompt` if not specified
- File extension (.md) is automatically added

#### PathResolution Property

Configures default input and output path roots for processing:

```yaml
TemplateTypes:
  base-template:
    PathResolution:
      InputRoot: onedrive    # Read from OneDrive sync folder
      OutputRoot: vault      # Write to vault structure
  base-generic:
    PathResolution:
      InputRoot: cwd         # Read from current directory
      OutputRoot: input      # Write adjacent to source file
```

**InputRoot Options:**
- `onedrive` - OneDrive sync folder (from config)
- `vault` - Vault root directory (from config)
- `cwd` - Current working directory

**OutputRoot Options:**
- `vault` - Vault root directory (from config)
- `onedrive` - OneDrive sync folder (from config)
- `cwd` - Current working directory
- `input` - Same directory as input file (for ad-hoc processing)

**Characteristics:**
- Inherited from base types if not specified
- Enables flexible processing of files outside vault structure
- `OutputRoot: input` is useful for ad-hoc content processing

### Example: PDF Reference Template

```yaml
TemplateTypes:
  pdf-reference:
    BaseTypes:
      - universal-fields
    Type: note/case-study
    RequiredFields:
      - comprehension
      - status
      - completion-date
      - authors
      - tags
    Fields:
      publisher:
        Default: University of Illinois at Urbana-Champaign
      status:
        Default: unread
      comprehension:
        Default: 0
      date-created:
        Resolver: DateCreatedResolver
      share-link:
        Resolver: OneDriveShareLinkResolver
      title:
        Resolver: TitleResolver
      tags:
        Default: [pdf, reference]
      page-count:
        Resolver: PdfPageCountResolver
```

### Inheritance System

Template types support recursive inheritance through the `BaseTypes` property:

#### Base Type Templates

The schema includes base template types that provide common defaults:

```yaml
TemplateTypes:
  # Base template for MBA course content
  base-template:
    Type: note/base
    Prompt: default_prompt
    PathResolution:
      InputRoot: onedrive
      OutputRoot: vault
    Fields:
      publisher:
        Default: University of Illinois at Urbana-Champaign
      date-created:
        Resolver: DateCreatedResolver
      tags:
        Default: []
  
  # Base template for ad-hoc/generic content
  base-generic:
    Type: note/generic
    Prompt: generic_prompt
    PathResolution:
      InputRoot: cwd
      OutputRoot: input
    Fields:
      date-created:
        Resolver: DateCreatedResolver
      tags:
        Default: [generic]
```

#### Base Type Resolution

- **universal-fields**: Inherits all fields from the `UniversalFields` section
- **template-type-name**: Inherits all fields from another template type
- **Recursive**: Base types are resolved recursively, supporting deep inheritance chains
- **Property Inheritance**: `Prompt` and `PathResolution` properties are inherited from base types

#### Field Inheritance Rules

1. Fields from base types are added only if they don't already exist in the derived type
2. Universal fields are always included if not present
3. Reserved tags are automatically injected as fields in all template types
4. Field definitions in derived types override those in base types

### Field Definitions

Each field in the `Fields` section can specify:

#### Default Values

Static default values used when no resolver is present:

```yaml
Fields:
  status:
    Default: unread
  tags:
    Default: [pdf, reference]
  comprehension:
    Default: 0
```

#### Resolvers

Dynamic value resolution through registered resolvers:

```yaml
Fields:
  date-created:
    Resolver: DateCreatedResolver
  page-count:
    Resolver: PdfPageCountResolver
  share-link:
    Resolver: OneDriveShareLinkResolver
```

#### Resolver Lookup

The system supports flexible resolver lookup:

1. **Exact match**: Looks for resolver with exact name
2. **Suffix match**: Looks for registered resolvers ending with the specified name
3. **Fallback**: Uses default value if no resolver is found

## UniversalFields Section

Defines fields that are automatically inherited by all template types.

### Structure (UniversalFields)

```yaml
UniversalFields:
  - auto-generated-state
  - date-created
  - publisher
```

### Behavior (UniversalFields)

- **Automatic Inheritance**: All fields in this list are automatically added to every template type
- **Reserved Tag Integration**: Reserved tags are automatically included as universal fields
- **Override Protection**: Universal fields can be overridden by specific template type definitions

### Example Usage

```yaml
UniversalFields:
  - auto-generated-state  # Present in all template types
  - date-created         # Present in all template types
  - publisher           # Present in all template types
```

## TypeMapping Section

Provides mapping from template type names to canonical type names for normalization.

### Structure (TypeMapping)

```yaml
TypeMapping:
  template-type-name: canonical-type-name
```

### Purpose

- **Normalization**: Maps custom or alias type names to canonical schema types
- **Backwards Compatibility**: Supports legacy type names while migrating to new schema
- **Flexibility**: Allows multiple template types to map to the same canonical type

### Example (TypeMapping)

```yaml
TypeMapping:
  pdf-reference: note/case-study
  video-reference: note/video-note
  resource-reading: note/reading
  note/instruction: note/instruction
```

## ReservedTags Section

Defines protected system tags that cannot be overridden by custom metadata.

### Structure (ReservedTags)

```yaml
ReservedTags:
  - tag-name-1
  - tag-name-2
```

### Behavior (ReservedTags)

- **Protection**: Reserved tags cannot be overridden or used for custom metadata
- **Automatic Injection**: Reserved tags are automatically injected as fields in all template types
- **Validation**: System validates that reserved tags are not overridden in custom metadata

### Example (ReservedTags)

```yaml
ReservedTags:
  - auto-generated-state
  - case-study
  - live-class
  - reading
  - finance
  - operations
  - video
  - pdf

## Required Resolvers

The following resolvers must be implemented and registered with the resolver registry used by the schema loader/pipeline:

- `DateCreatedResolver` — Provides `date-created` where not present
- `PdfPageCountResolver` — Populates `page-count` for PDFs
- `VideoDurationResolver` — Populates `video-duration` for videos
- `OneDriveShareLinkResolver` (required) — Populates `share-link` with a stable OneDrive sharing URL when files are under the OneDrive resources root
- Hierarchy and course structure adapters:
  - `ProgramResolver`, `CourseResolver`, `ClassResolver`
  - `ModuleResolver`, `LessonResolver`

If any required resolver is unavailable, validation should fail for templates that declare those fields.
```

## Complete Example

Here's a complete example of a `metadata-schema.yml` file:

```yaml
## NOTE: All top-level keys must use PascalCase for C# compatibility
# Metadata Schema for Notebook Automation
# This file defines all template-types, type mappings, required fields, and reserved tags

TemplateTypes:
  pdf-reference:
    BaseTypes:
      - universal-fields
    Type: note/case-study
    RequiredFields:
      - comprehension
      - status
      - completion-date
      - authors
      - tags
    Fields:
      publisher:
        Default: University of Illinois at Urbana-Champaign
      status:
        Default: unread
      comprehension:
        Default: 0
      date-created:
        Resolver: DateCreatedResolver
      share-link:
        Resolver: OneDriveShareLinkResolver
      title:
        Default: "PDF Note"
      tags:
        Default: [pdf, reference]
      page-count:
        Resolver: PdfPageCountResolver

  video-reference:
    BaseTypes:
      - universal-fields
    Type: note/video-note
    RequiredFields:
      - comprehension
      - status
      - video-duration
      - author
      - tags
    Fields:
      publisher:
        Default: University of Illinois at Urbana-Champaign
      status:
        Default: unwatched
      comprehension:
        Default: 0
      date-created:
        Resolver: DateCreatedResolver
      share-link:
        Resolver: OneDriveShareLinkResolver
      title:
        Resolver: TitleResolver
      tags:
        Default: [video, reference]
      video-duration:
        Resolver: VideoDurationResolver

UniversalFields:
  - auto-generated-state
  - date-created
  - publisher

TypeMapping:
  pdf-reference: note/case-study
  video-reference: note/video-note
  resource-reading: note/reading

ReservedTags:
  - auto-generated-state
  - case-study
  - live-class
  - reading
  - finance
  - operations
  - video
  - pdf
```

## Usage in Code

### Loading the Schema

```csharp
var schemaLoader = new MetadataSchemaLoader("config/metadata-schema.yml", logger);
```

### Accessing Template Types

```csharp
// Get template type schema
var pdfSchema = schemaLoader.TemplateTypes["pdf-reference"];

// Access template properties
var requiredFields = pdfSchema.RequiredFields;
var canonicalType = pdfSchema.Type;
var fields = pdfSchema.Fields;
```

### Resolving Field Values

```csharp
// Resolve field value with context
var context = new Dictionary<string, object> { ["user"] = "daniel" };
var dateCreated = schemaLoader.ResolveFieldValue("pdf-reference", "date-created", context);

// Get default value if no resolver
var defaultStatus = schemaLoader.ResolveFieldValue("pdf-reference", "status");
```

### Registry Access

```csharp
// Register custom resolver
schemaLoader.ResolverRegistry.Register("CustomResolver", new CustomFieldValueResolver());

// Load resolvers from directory
schemaLoader.LoadResolversFromDirectory("./resolvers");

// Get registered resolver
var resolver = schemaLoader.ResolverRegistry.Get("DateCreatedResolver");
```

## Best Practices

### Schema Design

1. **Use PascalCase** for all top-level keys to ensure C# compatibility
2. **Define universal fields** for metadata common to all template types
3. **Use reserved tags** for system-critical fields that shouldn't be overridden
4. **Implement inheritance** to reduce duplication and maintain consistency
5. **Provide meaningful defaults** for all fields to ensure robust operation

### Field Naming

1. Use **kebab-case** for field names (e.g., `date-created`, `page-count`)
2. Use **descriptive names** that clearly indicate the field's purpose
3. **Avoid conflicts** with reserved tags and universal fields
4. **Be consistent** with naming conventions across template types

### Resolver Implementation

1. **Implement IFieldValueResolver** for custom field logic
2. **Handle null context** gracefully in resolver implementations
3. **Use descriptive resolver names** for easy identification
4. **Register resolvers** before using them in field definitions

### Validation

1. **Test schema loading** in unit tests to catch configuration errors
2. **Validate required fields** are present in all template instances
3. **Check reserved tag inheritance** to ensure system integrity
4. **Test resolver registration** and lookup functionality

## Common Pitfalls

### Case Sensitivity Issues

❌ **Wrong**: Using camelCase or snake_case

```yaml
templateTypes:    # Will fail - camelCase
template_types:   # Will fail - snake_case
```

✅ **Correct**: Using PascalCase

```yaml
TemplateTypes:    # Correct - PascalCase
```

### Missing Required Fields

❌ **Wrong**: Forgetting to define required fields

```yaml
TemplateTypes:
  pdf-reference:
    Type: note/case-study
    # Missing RequiredFields - validation will fail
```

✅ **Correct**: Defining required fields

```yaml
TemplateTypes:
  pdf-reference:
    Type: note/case-study
    RequiredFields:
      - status
      - tags
```

### Resolver Not Found

❌ **Wrong**: Using unregistered resolver

```yaml
Fields:
  date-created:
    Resolver: NonExistentResolver  # Will fail at runtime
```

✅ **Correct**: Register resolver before use

```csharp
schemaLoader.ResolverRegistry.Register("DateCreatedResolver", new DateCreatedResolver());
```

For more information, see the [Migration Guide](migration-guide.md) and [API Reference](api/index.md).
