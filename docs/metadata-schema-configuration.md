# Metadata Schema Configuration Guide

This guide provides comprehensive documentation for the `metadata-schema.yml` file, which serves as the central configuration for all metadata operations in the Notebook Automation toolkit.

## Overview

The `metadata-schema.yml` file defines the structure, validation rules, and behavior for all metadata processing operations. It replaces the legacy `metadata.yaml` approach with a unified, extensible schema system that supports:

- **Template Type Definitions**: Structured schema for different content types
- **Field Value Resolvers**: Dynamic field population through registered resolvers
- **Inheritance System**: Recursive inheritance from base types and universal fields
- **Reserved Tag Logic**: Protected system tags with automatic validation
- **Type Mapping**: Canonical type normalization and aliasing

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
    RequiredFields:     # List of required fields for validation
      - field-name-1
      - field-name-2
    Fields:             # Field definitions with defaults and resolvers
      field-name:
        Default: default-value
        Resolver: ResolverName
```

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
      title:
        Default: "PDF Note"
      tags:
        Default: [pdf, reference]
      page-count:
        Resolver: PdfPageCountResolver
```

### Inheritance System

Template types support recursive inheritance through the `BaseTypes` property:

#### Base Type Resolution

- **universal-fields**: Inherits all fields from the `UniversalFields` section
- **template-type-name**: Inherits all fields from another template type
- **Recursive**: Base types are resolved recursively, supporting deep inheritance chains

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
```

#### Resolver Lookup

The system supports flexible resolver lookup:

1. **Exact match**: Looks for resolver with exact name
2. **Suffix match**: Looks for registered resolvers ending with the specified name
3. **Fallback**: Uses default value if no resolver is found

## UniversalFields Section

Defines fields that are automatically inherited by all template types.

### Structure

```yaml
UniversalFields:
  - auto-generated-state
  - date-created
  - publisher
```

### Behavior

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

### Structure

```yaml
TypeMapping:
  template-type-name: canonical-type-name
```

### Purpose

- **Normalization**: Maps custom or alias type names to canonical schema types
- **Backwards Compatibility**: Supports legacy type names while migrating to new schema
- **Flexibility**: Allows multiple template types to map to the same canonical type

### Example

```yaml
TypeMapping:
  pdf-reference: note/case-study
  video-reference: note/video-note
  resource-reading: note/reading
  note/instruction: note/instruction
```

## ReservedTags Section

Defines protected system tags that cannot be overridden by custom metadata.

### Structure

```yaml
ReservedTags:
  - tag-name-1
  - tag-name-2
```

### Behavior

- **Protection**: Reserved tags cannot be overridden or used for custom metadata
- **Automatic Injection**: Reserved tags are automatically injected as fields in all template types
- **Validation**: System validates that reserved tags are not overridden in custom metadata

### Example

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
      title:
        Default: "Video Note"
      tags:
        Default: [video, reference]
      video-duration:
        Default: "00:00:00"

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
