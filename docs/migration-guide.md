# Migration Guide: Metadata Schema Integration

This guide provides step-by-step instructions for migrating from legacy `metadata.yaml` to the new unified metadata schema system with `metadata-schema.yml`.

## Overview

The Metadata Schema Integration introduces a unified, extensible metadata schema loader and reserved tag logic. This major release replaces legacy approaches with a schema-driven system that supports:

- **Unified Schema File**: Single `metadata-schema.yml` for all metadata operations
- **Registry Pattern**: Dynamic registration and runtime extension of resolvers
- **Reserved Tag Logic**: Enforced validation and universal field inheritance
- **Plugin Extensibility**: Support for custom resolvers via DLL plugins
- **Schema Validation**: Robust validation and error handling

## Breaking Changes

### File Extension Change
- **Legacy**: `metadata.yaml` (deprecated)
- **New**: `metadata-schema.yml` (required)

### YAML Key Case Sensitivity
- **Important**: All top-level YAML keys must use PascalCase (e.g., `TemplateTypes`, `UniversalFields`, `TypeMapping`, `ReservedTags`)
- **Reason**: Required for C# deserialization; keys are case-sensitive and must match property names

### Schema Structure Changes
- **Legacy**: Template definitions mixed with schema configuration
- **New**: Structured schema with template types, universal fields, type mappings, and reserved tags

## Migration Steps

### Step 1: Create New Schema File

1. **Create `metadata-schema.yml`** in your config directory:
   ```bash
   cp config/metadata-schema.yaml config/metadata-schema.yml
   ```

2. **Update file extension references** in your configuration:
   ```json
   {
     "SchemaPath": "config/metadata-schema.yml"
   }
   ```

### Step 2: Update Schema Structure

Migrate from the legacy template-based structure to the new schema format:

#### Legacy Structure (metadata.yaml)
```yaml
# Multiple template definitions separated by ---
template-type: pdf-reference
auto-generated-state: writable
type: note/case-study
title: PDF Title
# ... other fields
---
template-type: video-reference
auto-generated-state: writable
type: note/video-note
# ... other fields
```

#### New Structure (metadata-schema.yml)
```yaml
# NOTE: All top-level keys must use PascalCase
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

UniversalFields:
  - auto-generated-state
  - date-created
  - publisher

TypeMapping:
  pdf-reference: note/case-study
  video-reference: note/video-note

ReservedTags:
  - auto-generated-state
  - case-study
  - video
  - pdf
```

### Step 3: Update Code References

Update your code to use the new schema loader:

#### Legacy Code
```csharp
// Legacy approach - deprecated
var templateManager = new MetadataTemplateManager(configPath);
var metadata = templateManager.LoadTemplate("pdf-reference");
```

#### New Code
```csharp
// New approach - recommended
var schemaLoader = new MetadataSchemaLoader("config/metadata-schema.yml", logger);
var pdfSchema = schemaLoader.TemplateTypes["pdf-reference"];
var dateCreated = schemaLoader.ResolveFieldValue("pdf-reference", "date-created", context);
```

### Step 4: Update Configuration

Update your application configuration to use the new schema system:

#### Legacy Configuration
```json
{
  "MetadataTemplatePath": "config/metadata.yaml",
  "TemplateProcessing": {
    "DefaultTemplate": "pdf-reference"
  }
}
```

#### New Configuration
```json
{
  "MetadataSchemaPath": "config/metadata-schema.yml",
  "SchemaProcessing": {
    "LoadResolversFromDirectory": "resolvers/",
    "ValidateReservedTags": true
  }
}
```

### Step 5: Plugin Migration

If you have custom plugins, update them to use the new registry pattern:

#### Legacy Plugin Approach
```csharp
// Legacy - manual registration
public class CustomProcessor
{
    public void ProcessMetadata(string templateType)
    {
        // Manual template loading and processing
    }
}
```

#### New Plugin Approach
```csharp
// New - registry-based approach
public class CustomFieldResolver : IFieldValueResolver
{
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        return fieldName switch
        {
            "custom-field" => ComputeCustomValue(context),
            _ => null
        };
    }
}

// Register in plugin initialization
public class CustomPlugin
{
    public void Initialize(IMetadataSchemaLoader schemaLoader)
    {
        schemaLoader.ResolverRegistry.Register("CustomFieldResolver", new CustomFieldResolver());
    }
}
```

## Reserved Tags and Universal Fields

### Understanding Reserved Tags
Reserved tags are automatically inherited as fields by all template types that include universal fields:

```yaml
UniversalFields:
  - auto-generated-state
  - date-created
  - publisher

ReservedTags:
  - auto-generated-state  # This becomes a field in all templates
  - case-study
  - video
  - pdf
```

### Validation Rules
- Reserved tags cannot be overridden by custom metadata
- Universal fields are automatically added to all template types
- PascalCase keys are required for C# compatibility

## Troubleshooting

### Common Issues

#### Issue: "Schema file not found"
**Cause**: File path incorrect or file doesn't exist
**Solution**: 
```bash
# Verify file exists
ls -la config/metadata-schema.yml

# Check configuration path
grep -r "SchemaPath" config/
```

#### Issue: "Invalid YAML key case"
**Cause**: Using camelCase or snake_case instead of PascalCase
**Solution**:
```yaml
# ❌ Wrong - camelCase
templateTypes:
  pdf-reference:

# ❌ Wrong - snake_case  
template_types:
  pdf-reference:

# ✅ Correct - PascalCase
TemplateTypes:
  pdf-reference:
```

#### Issue: "Resolver not found"
**Cause**: Resolver not registered in registry
**Solution**:
```csharp
// Register resolver before use
schemaLoader.ResolverRegistry.Register("DateCreatedResolver", new DateCreatedResolver());

// Or load from directory
schemaLoader.LoadResolversFromDirectory("./resolvers");
```

#### Issue: "Reserved tag validation failed"
**Cause**: Attempting to override reserved tags
**Solution**: Remove reserved tag overrides from custom metadata; they are automatically inherited

### Migration Validation

Run validation checks to ensure successful migration:

```bash
# Build and test
dotnet build
dotnet test

# Validate schema file
# Use your application's schema validation command
./NotebookAutomation.Cli validate-schema --path config/metadata-schema.yml

# Check for deprecated references
grep -r "metadata.yaml" docs/
grep -r "metadata.yaml" src/
```

## Best Practices

### Schema Design
- Use PascalCase for all top-level YAML keys
- Define universal fields for common metadata
- Use reserved tags for system-critical fields
- Implement custom resolvers for dynamic values

### Plugin Development
- Register resolvers in plugin initialization
- Use descriptive resolver names
- Handle null context gracefully
- Follow naming conventions

### Testing
- Test schema loading in unit tests
- Validate reserved tag inheritance
- Test resolver registration and lookup
- Verify migration scripts work correctly

## Support

If you encounter issues during migration:

1. **Check the troubleshooting section** above
2. **Review the API documentation** for detailed interface information
3. **Examine the test files** for working examples
4. **Open an issue** on GitHub with your specific problem

## Next Steps

After successful migration:

1. **Remove legacy files**: Delete `metadata.yaml` and related legacy code
2. **Update documentation**: Ensure all references point to the new schema
3. **Test thoroughly**: Run full test suite and manual validation
4. **Deploy gradually**: Consider phased rollout for production systems

For detailed API documentation, see the [API Reference](api/index.md).