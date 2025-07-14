# Architecture Diagrams: Metadata Schema System

This document provides visual diagrams illustrating the architecture and flow of the metadata schema system, registry pattern, and resolver workflow.

## Registry Pattern Architecture

The metadata schema system uses a registry pattern for extensible field value resolution:

```
┌─────────────────────────────────────────────────────────────────┐
│                    MetadataSchemaLoader                         │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              FieldValueResolverRegistry                 │    │
│  │                                                         │    │
│  │  ┌─────────────────┐  ┌─────────────────┐              │    │
│  │  │  Field Resolvers│  │ File Type       │              │    │
│  │  │  - DateCreated  │  │ Resolvers       │              │    │
│  │  │  - PageCount    │  │ - PDF           │              │    │
│  │  │  - UserName     │  │ - Video         │              │    │
│  │  │  - Custom...    │  │ - Markdown      │              │    │
│  │  └─────────────────┘  └─────────────────┘              │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              Template Types                             │    │
│  │  - pdf-reference                                        │    │
│  │  - video-reference                                      │    │
│  │  - resource-reading                                     │    │
│  │  - note/instruction                                     │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              Universal Fields                           │    │
│  │  - auto-generated-state                                 │    │
│  │  - date-created                                         │    │
│  │  - publisher                                            │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              Reserved Tags                              │    │
│  │  - case-study                                           │    │
│  │  - video                                                │    │
│  │  - pdf                                                  │    │
│  │  - reading                                              │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

## Resolver Flow Diagram

The following diagram shows how field values are resolved through the registry:

```
┌─────────────────┐
│  Application    │
│  Requests Field │
│  Resolution     │
└─────────┬───────┘
          │
          v
┌─────────────────┐
│ Schema Loader   │
│ ResolveField()  │
└─────────┬───────┘
          │
          v
┌─────────────────┐     ┌─────────────────┐
│ Check Template  │────▶│ Template Exists?│
│ Type Exists     │     │                 │
└─────────┬───────┘     └─────────┬───────┘
          │                       │
          v                       │ No
┌─────────────────┐               │
│ Check Field     │               │
│ Exists in       │               │
│ Template        │               │
└─────────┬───────┘               │
          │                       │
          v                       │
┌─────────────────┐     ┌─────────v───────┐
│ Field Has       │────▶│ Return null     │
│ Resolver?       │     │                 │
└─────────┬───────┘     └─────────────────┘
          │
          v Yes
┌─────────────────┐
│ Registry        │
│ Lookup          │
│ Resolver        │
└─────────┬───────┘
          │
          v
┌─────────────────┐     ┌─────────────────┐
│ Exact Name      │────▶│ Resolver Found? │
│ Match?          │     │                 │
└─────────┬───────┘     └─────────┬───────┘
          │                       │
          v No                    v Yes
┌─────────────────┐     ┌─────────────────┐
│ Partial Name    │     │ Invoke Resolver │
│ Match (suffix)  │     │ with Context    │
└─────────┬───────┘     └─────────┬───────┘
          │                       │
          v                       │
┌─────────────────┐               │
│ Resolver Found? │               │
└─────────┬───────┘               │
          │                       │
          v Yes                   │
┌─────────────────┐               │
│ Invoke Resolver │               │
│ with Context    │               │
└─────────┬───────┘               │
          │                       │
          v                       │
┌─────────────────┐               │
│ Return Resolved │◀──────────────┘
│ Value           │
└─────────┬───────┘
          │
          v
┌─────────────────┐
│ Return to       │
│ Application     │
└─────────────────┘
```

## Schema Inheritance Flow

Template types support inheritance from base types and universal fields:

```
┌─────────────────┐
│ Template Type   │
│ Definition      │
│ (pdf-reference) │
└─────────┬───────┘
          │
          v
┌─────────────────┐     ┌─────────────────┐
│ Has Base Types? │────▶│ Process Base    │
│                 │     │ Types           │
└─────────┬───────┘     └─────────┬───────┘
          │                       │
          v No                    v
┌─────────────────┐     ┌─────────────────┐
│ Inject Reserved │     │ For Each Base   │
│ Tags as Fields  │     │ Type            │
└─────────┬───────┘     └─────────┬───────┘
          │                       │
          v                       v
┌─────────────────┐     ┌─────────────────┐
│ Template Type   │     │ Is "universal-  │
│ Resolution      │     │ fields"?        │
│ Complete        │     └─────────┬───────┘
└─────────────────┘               │
                                  v Yes
                        ┌─────────────────┐
                        │ Add Universal   │
                        │ Fields to       │
                        │ Template        │
                        └─────────┬───────┘
                                  │
                                  v No
                        ┌─────────────────┐
                        │ Is Another      │
                        │ Template Type?  │
                        └─────────┬───────┘
                                  │
                                  v Yes
                        ┌─────────────────┐
                        │ Recursively     │
                        │ Resolve Base    │
                        │ Template        │
                        └─────────┬───────┘
                                  │
                                  v
                        ┌─────────────────┐
                        │ Inherit Fields  │
                        │ from Base       │
                        │ (if not exist)  │
                        └─────────┬───────┘
                                  │
                                  v
                        ┌─────────────────┐
                        │ Continue with   │
                        │ Next Base Type  │
                        └─────────────────┘
```

## Plugin Loading Architecture

The system supports loading plugins from DLL files:

```
┌─────────────────┐
│ Application     │
│ Startup         │
└─────────┬───────┘
          │
          v
┌─────────────────┐
│ Call            │
│ LoadResolvers   │
│ FromDirectory() │
└─────────┬───────┘
          │
          v
┌─────────────────┐     ┌─────────────────┐
│ Scan Plugin     │────▶│ Directory       │
│ Directory       │     │ Exists?         │
└─────────┬───────┘     └─────────┬───────┘
          │                       │
          v                       v No
┌─────────────────┐     ┌─────────────────┐
│ Find *.dll      │     │ Log Warning     │
│ Files           │     │ Return          │
└─────────┬───────┘     └─────────────────┘
          │
          v
┌─────────────────┐
│ For Each DLL    │
│ File            │
└─────────┬───────┘
          │
          v
┌─────────────────┐     ┌─────────────────┐
│ Load Assembly   │────▶│ Assembly Load   │
│ from DLL        │     │ Successful?     │
└─────────┬───────┘     └─────────┬───────┘
          │                       │
          v                       v No
┌─────────────────┐     ┌─────────────────┐
│ Scan Types in   │     │ Log Error       │
│ Assembly        │     │ Continue        │
└─────────┬───────┘     └─────────────────┘
          │
          v
┌─────────────────┐
│ Find Types      │
│ Implementing    │
│ IFieldValue     │
│ Resolver        │
└─────────┬───────┘
          │
          v
┌─────────────────┐     ┌─────────────────┐
│ Create Instance │────▶│ Instance        │
│ of Resolver     │     │ Creation        │
└─────────┬───────┘     │ Successful?     │
          │             └─────────┬───────┘
          v                       │
┌─────────────────┐               v No
│ Register with   │     ┌─────────────────┐
│ Registry        │     │ Log Error       │
│ using Full Name │     │ Continue        │
└─────────┬───────┘     └─────────────────┘
          │
          v
┌─────────────────┐
│ Log Success     │
│ Continue with   │
│ Next Type       │
└─────────────────┘
```

## Migration Flow

The migration from legacy `metadata.yaml` to new `metadata-schema.yml`:

```
┌─────────────────┐
│ Legacy System   │
│ (metadata.yaml) │
└─────────┬───────┘
          │
          v
┌─────────────────┐
│ Multiple        │
│ Template        │
│ Definitions     │
│ (--- separated) │
└─────────┬───────┘
          │
          v
┌─────────────────┐
│ Manual Field    │
│ Definitions     │
│ Per Template    │
└─────────┬───────┘
          │
          v Migration
┌─────────────────┐
│ New Schema      │
│ (metadata-      │
│ schema.yml)     │
└─────────┬───────┘
          │
          v
┌─────────────────┐
│ Structured      │
│ Schema with     │
│ TemplateTypes   │
│ Section         │
└─────────┬───────┘
          │
          v
┌─────────────────┐
│ Universal       │
│ Fields          │
│ Section         │
└─────────┬───────┘
          │
          v
┌─────────────────┐
│ Reserved Tags   │
│ Section         │
└─────────┬───────┘
          │
          v
┌─────────────────┐
│ Type Mapping    │
│ Section         │
└─────────┬───────┘
          │
          v
┌─────────────────┐
│ Inheritance     │
│ Support with    │
│ Base Types      │
└─────────┬───────┘
          │
          v
┌─────────────────┐
│ Field Value     │
│ Resolvers with  │
│ Registry        │
└─────────────────┘
```

## Field Resolution Priority

The system resolves field values in a specific priority order:

```
Priority 1: Resolver (if configured)
    │
    ├─ Exact Name Match
    │   └─ registry.Get("ResolverName")
    │
    ├─ Partial Name Match  
    │   └─ registry.Get("*.ResolverName")
    │
    └─ Resolver Not Found
        │
        v
Priority 2: Schema Default Value
    │
    └─ fieldSchema.Default
        │
        v
Priority 3: Null (field not found)
```

## Security Considerations

The plugin system includes security considerations:

```
┌─────────────────┐
│ Plugin DLL      │
│ Loading         │
└─────────┬───────┘
          │
          v
┌─────────────────┐     ┌─────────────────┐
│ Validate DLL    │────▶│ Trusted Source? │
│ Source          │     │                 │
└─────────┬───────┘     └─────────┬───────┘
          │                       │
          v                       v No
┌─────────────────┐     ┌─────────────────┐
│ Load Assembly   │     │ Security Risk   │
│ with            │     │ - Reject DLL    │
│ Restrictions    │     └─────────────────┘
└─────────┬───────┘
          │
          v
┌─────────────────┐
│ Validate        │
│ Interface       │
│ Implementation  │
└─────────┬───────┘
          │
          v
┌─────────────────┐     ┌─────────────────┐
│ Sandbox         │────▶│ Context         │
│ Resolver        │     │ Validation      │
│ Execution       │     │ Required        │
└─────────┬───────┘     └─────────────────┘
          │
          v
┌─────────────────┐
│ Safe Execution  │
│ with Error      │
│ Handling        │
└─────────────────┘
```

## Usage Examples

### Basic Field Resolution
```csharp
// Load schema
var loader = new MetadataSchemaLoader("metadata-schema.yml", logger);

// Resolve field value
var context = new Dictionary<string, object> { ["filePath"] = "/path/to/file.pdf" };
var dateCreated = loader.ResolveFieldValue("pdf-reference", "date-created", context);
```

### Plugin Registration
```csharp
// Manual registration
loader.ResolverRegistry.Register("CustomResolver", new CustomResolver());

// Automatic plugin loading
loader.LoadResolversFromDirectory("./plugins");
```

### Template Type Usage
```csharp
// Access template schema
var pdfSchema = loader.TemplateTypes["pdf-reference"];
var requiredFields = pdfSchema.RequiredFields;
var fieldDefinitions = pdfSchema.Fields;
```

These diagrams illustrate the key architectural patterns and flows in the metadata schema system, providing a visual understanding of how the components interact to provide extensible, schema-driven metadata processing.