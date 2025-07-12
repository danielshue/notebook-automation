# Metadata Schema Integration Tasks

## Integration Task List

### Resolver Abstraction & Implementation

- Refactor all processors (Video, PDF, Markdown, Resource, Tag) to use the FieldValueResolverRegistry for field value resolution and metadata extraction.
- Implement specialized resolvers for each file type, following the IFileTypeMetadataResolver interface and documenting required context.
- Update the registry to support dynamic registration (including plugin DLLs) and runtime extension.
- Use constructor injection for registry and resolver dependencies in all processors for testability and extensibility.
- Document resolver usage and required context in code and integration docs for maintainability.
- Refactor batch processor logic to support composition of multiple resolvers.
- Implement runtime registry extension for plugin DLLs.
- Document required/optional resolvers for each processor in code and integration docs.
- Add integration tests for registry extension and resolver composition.
- Update build scripts to bundle plugin resolvers and registry extensions.
- Update deployment scripts to include schema and resolver DLLs.
- Add CI checks for resolver registration and schema validation.
- [ ] Implement MarkdownMetadataResolver with full extraction logic and context documentation
- [ ] Implement ResourceMetadataResolver with full extraction logic and context documentation
- [ ] Implement TranscriptResolver for transcript file discovery and loading
- [ ] Implement TagResolver for tag validation and normalization
- [ ] Write XML documentation for each resolver, specifying context requirements
- [ ] Add unit tests for each resolver covering happy path, edge cases, and error conditions

### Core C# Components

- Refactor MetadataTemplateManager and IMetadataTemplateManager to use MetadataSchemaLoader for template management and validation.
- Update MetadataHierarchyDetector and IMetadataHierarchyDetector to use schema loader for hierarchy-to-template mapping and reserved tag injection.
- Update YamlHelper and IYamlHelper to respect schema loader’s reserved tag and universal field inheritance.
- [ ] Analyze current MetadataTemplateManager usage and identify schema loader integration points
- [ ] Refactor MetadataTemplateManager to delegate template validation to MetadataSchemaLoader
- [ ] Update IMetadataTemplateManager interface to expose schema loader-based validation methods
- [ ] Refactor MetadataHierarchyDetector to use schema loader for hierarchy-to-template mapping
- [ ] Update IMetadataHierarchyDetector interface for schema loader integration
- [ ] Refactor YamlHelper to support reserved/universal field inheritance from schema loader
- [ ] Update IYamlHelper interface for schema loader integration
- [ ] Add XML documentation to all updated interfaces and classes
- [ ] Add/expand unit tests for template manager, hierarchy detector, and YAML helper covering schema loader logic

### CLI Commands

- Refactor CLI commands (ConfigCommands, MarkdownCommands, PdfCommands, TagCommands, VaultCommands, VideoCommands, OneDriveCommands) to use schema loader for metadata validation and generation.
- Ensure reserved tags are present in all CLI-generated metadata.
- [ ] Audit all CLI command classes for metadata validation/generation logic
- [ ] Refactor ConfigCommands to use schema loader for metadata validation/generation
- [ ] Refactor MarkdownCommands to use schema loader for metadata validation/generation
- [ ] Refactor PdfCommands to use schema loader for metadata validation/generation
- [ ] Refactor TagCommands to use schema loader for metadata validation/generation
- [ ] Refactor VaultCommands to use schema loader for metadata validation/generation
- [ ] Refactor VideoCommands to use schema loader for metadata validation/generation
- [ ] Refactor OneDriveCommands to use schema loader for metadata validation/generation
- [ ] Ensure reserved tags are injected in all CLI-generated metadata
- [ ] Add/expand unit tests for CLI commands covering schema loader and reserved tag logic

### Note and Vault Processing

- [ ] Remove old plugin modules (ResourceService.ts, IndexService.ts, config.ts) if present
- [ ] Add/expand plugin integration tests for schema loader and reserved/universal field logic

### Tests

- Expand and update tests to cover reserved tag inheritance, universal field injection, resolver logic, schema loading, and plugin integration.
- [ ] Audit all test files for coverage of reserved tag inheritance, universal field injection, resolver logic, schema loading, and plugin integration
- [ ] Expand MetadataSchemaLoaderTests for reserved tag inheritance and universal field injection
- [ ] Expand MetadataTemplateManagerTests for schema loader integration
- [ ] Expand MetadataHierarchyDetectorTests for reserved tag logic
- [ ] Expand MarkdownNoteBuilderBannerTests for universal/reserved field injection
- [ ] Expand VaultPathHandlingTests for schema loader integration
- [ ] Expand MetadataHierarchyDetectorPathTests for reserved tag logic
- [ ] Expand VaultIndexBatchProcessorTests for registry/resolver logic
- [ ] Expand VaultIndexContentGeneratorTests for registry/resolver logic
- [ ] Expand VaultIndexProcessorTests for registry/resolver logic
- [ ] Expand ObsidianPluginMainTests for schema loader integration (if present)
- [ ] Update/remove tests referencing metadata.yaml
- [ ] Update resource/index/config/loader/field/YAML schema tests for new logic

### Migration: metadata-schema.yml Config & Extension Switch

- Add a config setting in the Obsidian plugin for the new metadata schema file (prefer .yml extension for consistency).
- Update plugin logic to load and use metadata-schema.yml for all metadata operations.
- Update C# AppConfig logic to resolve and provide access to the new schema file, mirroring legacy metadata.yaml resolution.
- Refactor all references and documentation to use .yml extension instead of .yaml.
- Bundle the new schema file with the plugin; update build and deployment scripts to include it.
- Deprecate/remove references to metadata.yaml throughout the codebase and documentation.
- [ ] Audit all config and extension references for schema file usage
- [ ] Add config setting in Obsidian plugin for new metadata schema file (.yml)
- [ ] Update plugin logic to load/use metadata-schema.yml for all metadata operations
- [ ] Update C# AppConfig logic to resolve/provide access to new schema file
- [ ] Refactor all references/documentation to use .yml extension
- [ ] Update build/deployment scripts to bundle new schema file
- [ ] Remove references to metadata.yaml in codebase and documentation
- [ ] Add/expand migration tests for config and extension switch

### Documentation Updates

- Update the main README to describe the new metadata schema loader, registry pattern, reserved tag logic, and migration steps from `metadata.yaml` to `.yml`.
- Add a migration guide detailing upgrade steps, breaking changes, and troubleshooting for legacy users.
- Update API documentation for all affected interfaces, classes, and CLI commands to reflect schema loader and registry usage.
- Document the structure and usage of the new `metadata-schema.yml` file, including reserved tags and universal fields.
- Update plugin documentation to describe new config settings, schema file usage, and resolver extension points.
- Add/expand XML documentation comments in all updated C# interfaces, classes, and methods.
- Ensure all new and refactored code includes inline comments explaining non-obvious logic and security assumptions.
- Update or create diagrams illustrating the registry pattern, resolver flow, and migration process.
- Review and update all references to `metadata.yaml` in documentation, replacing with `.yml` and new schema logic.
- [ ] Update README with schema loader, registry, reserved tag, and migration details
- [ ] Add migration guide for legacy users
- [ ] Update API docs for affected interfaces, classes, and CLI commands
- [ ] Document `metadata-schema.yml` structure and usage
- [ ] Update plugin documentation for config, schema, and resolver extension
- [ ] Add/expand XML documentation in C# code
- [ ] Add inline comments for complex logic and security assumptions
- [ ] Update/create diagrams for registry, resolver, and migration
- [ ] Replace all `metadata.yaml` references in docs with `.yml` and new schema logic

## Progress Tracking

**Overall Status:** Not Started

### Subtasks

| ID  | Description                                               | Status      | Updated     | Notes |
| --- | --------------------------------------------------------- | ----------- | ----------- | ----- |
| 1.1 | Refactor MetadataTemplateManager for schema loader usage   | Not Started | 2025-07-12  |       |
| 1.2 | Update MetadataHierarchyDetector for reserved tag logic    | Not Started | 2025-07-12  |       |
| 1.3 | Update YamlHelper for universal/reserved field inheritance | Not Started | 2025-07-12  |       |
| 2.1 | Refactor CLI commands for schema loader integration        | Not Started | 2025-07-12  |       |
| 2.2 | Ensure reserved tags in CLI-generated metadata             | Not Started | 2025-07-12  |       |
| 3.1 | Update note/vault processors for universal/reserved fields | Not Started | 2025-07-12  |       |
| 3.2 | Validate field values using registry/resolver logic        | Not Started | 2025-07-12  |       |
| 4.1 | Refactor TagProcessor for reserved tag validation          | Not Started | 2025-07-12  |       |
| 5.1 | Update resource processors for schema loader usage         | Not Started | 2025-07-12  |       |
| 6.1 | Integrate schema loader into Obsidian plugin (main.ts)     | Not Started | 2025-07-12  |       |
| 6.2 | Ensure reserved/universal fields in plugin notes/indexes   | Not Started | 2025-07-12  |       |
| 6.3 | Bundle schema file with plugin, update build/deploy scripts| Not Started | 2025-07-12  |       |
| 6.4 | Deprecate/remove metadata.yaml references                  | Not Started | 2025-07-12  |       |
| 6.5 | Remove old plugin modules if present                       | Not Started | 2025-07-12  |       |
| 7.1 | Expand/update MetadataSchemaLoaderTests                    | Not Started | 2025-07-12  |       |
| 7.2 | Expand/update MetadataTemplateManagerTests                 | Not Started | 2025-07-12  |       |
| 7.3 | Expand/update MetadataHierarchyDetectorTests               | Not Started | 2025-07-12  |       |
| 7.4 | Expand/update MarkdownNoteBuilderBannerTests               | Not Started | 2025-07-12  |       |
| 7.5 | Expand/update VaultPathHandlingTests                       | Not Started | 2025-07-12  |       |
| 7.6 | Expand/update MetadataHierarchyDetectorPathTests           | Not Started | 2025-07-12  |       |
| 7.7 | Expand/update VaultIndexBatchProcessorTests                | Not Started | 2025-07-12  |       |
| 7.8 | Expand/update VaultIndexContentGeneratorTests              | Not Started | 2025-07-12  |       |
| 7.9 | Expand/update VaultIndexProcessorTests                     | Not Started | 2025-07-12  |       |
| 7.10| Expand/update ObsidianPluginMainTests (if present)         | Not Started | 2025-07-12  |       |
| 7.11| Update/remove tests referencing metadata.yaml              | Not Started | 2025-07-12  |       |
| 7.12| Update resource/index/config/loader/field/YAML schema tests| Not Started | 2025-07-12  |       |
