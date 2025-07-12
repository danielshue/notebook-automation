
# Executive Summary

**Status:** Pending
**Added:** 2025-07-12
**Updated:** 2025-07-12

This initiative delivers a unified, extensible metadata schema loader and reserved tag logic for Notebook Automation. It replaces legacy approaches, improves data integrity, and enables future growth through plugin-based extensibility. Success will be measured by seamless migration, robust validation, and improved developer experience.

## Risk Assessment & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Migration complexity | High | Provide migration guide, automated scripts, and fallback options |
| Breaking changes | Medium | Communicate early, document changes, offer support during rollout |
| Resource constraints | Medium | Assign clear ownership, monitor progress, escalate blockers |
| Plugin compatibility | Medium | Early testing, clear extension points, community feedback |
| Documentation gaps | Medium | Prioritize docs, assign doc leads, review before release |

## Stakeholder & Team Assignments

| Area | Owner/Team |
|------|------------|
| Schema Loader & Registry | Core C# Team |
| CLI Refactor | CLI Team |
| Plugin Integration | Plugin Team |
| Migration & Docs | Migration/Docs Team |
| Testing | QA Team |

## Milestones & Timeline

| Milestone | Target Date | Dependencies |
|-----------|------------|--------------|
| Design & Planning | July 15 | None |
| Registry Implementation | July 22 | Design |
| Specialized Resolvers | July 30 | Registry |
| CLI/Processor Refactor | Aug 7 | Resolvers |
| Plugin Integration | Aug 14 | CLI/Processors |
| Migration & Docs | Aug 21 | All above |
| Final Testing & Review | Aug 28 | Migration/Docs |

## Acceptance Criteria & Review Process

- All metadata operations use the new schema loader and registry
- Reserved tags/universal fields present and validated in outputs
- Plugins register new resolvers at runtime
- Legacy `metadata.yaml` fully deprecated
- Migration guide and docs published
- All tests pass
- Review by leads and sign-off before release

## Change Management & Communication Plan

- Communicate changes via project updates, team meetings, and documentation
- Provide migration guide and training for affected teams
- Announce breaking changes and support channels
- Update user and developer documentation before rollout

## Open Questions & Decision Log

- [ ] Finalize plugin extension API details
- [ ] Confirm migration script requirements
- [ ] Assign documentation leads
- [ ] Review and approve timeline with stakeholders

## Cost & Effort Summary (Management Overview)

This initiative is a major release requiring significant cross-team effort and budget commitment:

- **Estimated Duration:** 26–41 days (full-time developer, plus management/coordination)
- **Estimated Cost:** $31,120–$57,040 (includes developer and management team costs)
- **Team Commitment:** Core C# team, CLI, Plugin, Migration/Docs, QA, and management oversight

Management should expect active involvement for planning, review, and coordination throughout the project lifecycle.

## Product Requirements Document (PRD)

## Overview

This PRD defines the product goals, user needs, requirements, and success criteria for the Metadata Schema Loader & Reserved Tag Integration. This upgrade is a major release that replaces legacy metadata management with a unified, extensible, and schema-driven approach, supporting future growth and plugin-based extensibility.

## Business Purpose & Release Context

This major release introduces a unified metadata schema loader, registry, and reserved tag logic, replacing legacy approaches and enabling extensible, schema-driven metadata management across the application. The refactor centralizes field and tag logic, supports plugin-based extensibility, and enforces consistent validation for all note, resource, and CLI operations. This upgrade is critical for:

- **Scalability:** Supports future growth and new file types via registry and plugin system.
- **Maintainability:** Reduces duplication, streamlines updates, and improves developer onboarding.
- **Data Integrity:** Enforces schema rules and reserved tags, preventing accidental overrides and inconsistencies.
- **Automation:** Enables robust automation and reporting by ensuring all metadata is schema-compliant.
- **Migration:** Smoothly transitions from legacy `metadata.yaml` to modern `.yml` schema, with clear deprecation and upgrade steps.

**Release Type:** Major release — introduces breaking changes, requires migration, and impacts all core, CLI, and plugin components.

## Business Goals

- Centralize metadata schema management for all note, resource, and CLI operations
- Enable extensibility via registry and plugin system for new file types and metadata extraction logic
- Enforce reserved tag and universal field logic for data integrity and automation
- Streamline migration from legacy `metadata.yaml` to modern `.yml` schema

## User Needs

- Developers need a consistent, schema-driven way to manage metadata across all components
- Plugin authors need a registry system to add new resolvers and extend metadata extraction logic
- CLI users need reliable, validated metadata outputs with reserved tags enforced
- Administrators need a clear migration path and documentation for upgrading schema files

## Product Requirements

1. The system must support a unified metadata schema file (`metadata-schema.yml`) for all metadata operations
2. Reserved tags and universal fields must be enforced and validated in all outputs
3. Registry pattern must allow dynamic registration and runtime extension of resolvers, including plugin DLLs
4. All core managers, CLI commands, note/vault processors, and plugins must use the schema loader for validation and generation
5. Migration tools and documentation must be provided for transitioning from `metadata.yaml` to `.yml`
6. Build and deployment scripts must bundle the schema file and all required resolvers
7. Comprehensive test coverage must validate reserved tag logic, schema loading, registry extension, and migration

## Success Criteria

- All metadata operations use the new schema loader and registry pattern
- Reserved tags and universal fields are present and validated in all outputs
- Plugins can register new resolvers at runtime without codebase changes
- Legacy `metadata.yaml` is fully deprecated and replaced by `.yml` schema
- Migration documentation and tools are available and verified
- All tests pass for schema, registry, resolver, and migration logic

---

## Metadata Schema Loader & Reserved Tag Integration Tasks

## Context & Rationale

### Original Request

Compile a detailed integration plan for the new metadata schema loader and reserved tag logic, listing all places in the codebase where changes are needed. Store as a tasks document for review before execution.

### Thought Process

- Scanned the codebase for all components, managers, CLI commands, note/vault processors, tag/resource handlers, and plugin modules that interact with metadata, templates, fields, tags, and schema logic.
- Identified all areas where the new loader and reserved tag inheritance should be integrated.
- Organized the plan by codebase area and function for clarity and review.

## Integration Task List

---

## Implementation Meta-Tasks, Impact Estimate, and Value Summary

### Implementation Meta-Tasks

### Overall Impact Estimate

**Codebase Impact:**

- Touches all core C# components, CLI commands, note/vault processors, tag/resource handlers, plugin modules, and build/deployment scripts.
- Requires refactoring of existing logic, interface updates, new resolver implementations, and migration from legacy schema/config.
- Significant updates to documentation, diagrams, and developer onboarding materials.
- Comprehensive test suite expansion and validation of migration and extensibility features.

**Revised Estimated Time to Complete:**

- Design & Planning: 2–4 days
- Resolver Abstraction & Registry Implementation: 4–6 days
- Specialized Resolver Implementations: 5–8 days
- Refactoring Processors & CLI Commands: 5–7 days
- Plugin Integration & Build/Deployment: 3–5 days
- Migration & Documentation: 3–5 days
- Test Expansion & Validation: 4–6 days

**Total Estimate:**
**26–41 days** (assuming 1 developer, full focus, moderate-to-high complexity; add 20–30% for team coordination, review, and unforeseen issues)

**Estimated Cost:**
Assuming a developer rate of $800/day and management team rate of $1,200/day (for coordination, review, and oversight):

**Low estimate (developer only):** 26 days × $800 = **$20,800**
**High estimate (developer only):** 41 days × $800 = **$32,800**
**Team/coordination buffer (20–30%):** $4,160–$9,840
**Management team cost (20–30% of total days):**
  
- Low: 5 days × $1,200 = **$6,000**
- High: 12 days × $1,200 = **$14,400**

**Total estimated cost range:** **$31,120–$57,040**

### Value Provided by New Metadata Schema/Resolver Functionality

- **Extensibility:** Enables dynamic addition of new file types and metadata extraction logic via registry and plugin system.
- **Maintainability:** Centralizes schema, reserved tag, and field logic, reducing duplication and risk of inconsistencies.
- **Testability:** Improves dependency injection and abstraction, making all components easier to unit test and mock.
- **Configurability:** Allows runtime configuration and extension of metadata schema, supporting future requirements.
- **Robustness:** Ensures reserved tags and universal fields are always present and validated, improving data integrity.
- **Developer Experience:** Clear documentation, modular design, and registry pattern make onboarding and future changes easier.
- **Plugin Ecosystem:** Supports plugin-based resolvers and schema extensions, enabling community and third-party contributions.
- **Migration Path:** Smooth transition from legacy `metadata.yaml` to modern `.yml` schema, with clear deprecation and upgrade steps.

### Integration Task List (Restructured by Area)

### Resolver Abstraction & Implementation

Summary: Refactor and extend all processors to use a registry-based resolver pattern for metadata extraction, enabling extensibility and consistent logic across file types.

**Tasks:**

- Refactor all processors (Video, PDF, Markdown, Resource, Tag) to use the FieldValueResolverRegistry for field value resolution and metadata extraction.
  - *Why:* Centralizes metadata extraction logic, enables extensibility, and ensures consistent field resolution across all file types.
- Implement specialized resolvers for each file type, following the IFileTypeMetadataResolver interface and documenting required context.
  - *Why:* Allows for tailored metadata extraction per file type, supporting future expansion and plugin-based logic.
- Update the registry to support dynamic registration (including plugin DLLs) and runtime extension.
  - *Why:* Enables runtime extension and third-party contributions, making the system adaptable and future-proof.
- Use constructor injection for registry and resolver dependencies in all processors for testability and extensibility.
  - *Why:* Improves testability and supports dependency injection best practices for maintainable code.
- Document resolver usage and required context in code and integration docs for maintainability.
  - *Why:* Ensures future developers understand integration points and required context for each resolver.
- Refactor batch processor logic to support composition of multiple resolvers.
  - *Why:* Supports complex metadata extraction scenarios and improves modularity.
- Implement runtime registry extension for plugin DLLs.
  - *Why:* Allows plugins to add new resolvers without codebase changes, supporting a plugin ecosystem.
- Document required/optional resolvers for each processor in code and integration docs.
  - *Why:* Clarifies integration requirements and supports maintainability.
- Add integration tests for registry extension and resolver composition.
  - *Why:* Validates extensibility and ensures robust integration of new resolvers.
- Update build scripts to bundle plugin resolvers and registry extensions.
  - *Why:* Ensures all required components are included in builds for deployment and testing.
- Update deployment scripts to include schema and resolver DLLs.
  - *Why:* Guarantees correct deployment of all schema and resolver logic.
- Add CI checks for resolver registration and schema validation.
  - *Why:* Automates validation and prevents integration errors during development.
- [ ] Implement MarkdownMetadataResolver with full extraction logic and context documentation
- [ ] Implement ResourceMetadataResolver with full extraction logic and context documentation
- [ ] Implement TranscriptResolver for transcript file discovery and loading
- [ ] Implement TagResolver for tag validation and normalization
- [ ] Write XML documentation for each resolver, specifying context requirements
- [ ] Add unit tests for each resolver covering happy path, edge cases, and error conditions

### Core C# Components

Summary: Update core managers and helpers to use the schema loader for template management, hierarchy mapping, and reserved tag logic, ensuring schema-driven consistency.

**Tasks:**

- Refactor MetadataTemplateManager and IMetadataTemplateManager to use MetadataSchemaLoader for template management and validation.
  - *Why:* Ensures template management is schema-driven, improving consistency and validation accuracy.
- Update MetadataHierarchyDetector and IMetadataHierarchyDetector to use schema loader for hierarchy-to-template mapping and reserved tag injection.
  - *Why:* Guarantees correct mapping of note hierarchy to templates and enforces reserved tag logic.
- Update YamlHelper and IYamlHelper to respect schema loader’s reserved tag and universal field inheritance.
  - *Why:* Centralizes field inheritance logic, reducing duplication and risk of errors.

**Subtasks:**

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

Summary: Refactor CLI commands to use the schema loader for metadata validation and generation, enforcing reserved tag logic and schema compliance in all outputs.

**Tasks:**

- Refactor CLI commands (ConfigCommands, MarkdownCommands, PdfCommands, TagCommands, VaultCommands, VideoCommands, OneDriveCommands) to use schema loader for metadata validation and generation.
  - *Why:* Provides consistent metadata validation and generation across all CLI operations, reducing manual errors.
- Ensure reserved tags are present in all CLI-generated metadata.
  - *Why:* Maintains data integrity and enforces schema rules in all CLI outputs.

**Subtasks:**

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

Summary: Remove legacy plugin modules and expand integration tests to validate schema loader and reserved/universal field logic in note and vault processing.

**Tasks:**

- [ ] Remove old plugin modules (ResourceService.ts, IndexService.ts, config.ts) if present
- [ ] Add/expand plugin integration tests for schema loader and reserved/universal field logic

### Tests

Summary: Expand and update tests to cover reserved tag inheritance, universal field injection, resolver logic, schema loading, and plugin integration for robust validation.

**Tasks:**

- Expand and update tests to cover reserved tag inheritance, universal field injection, resolver logic, schema loading, and plugin integration.
  - *Why:* Ensures all new and refactored logic is robust, reliable, and regression-proof.

**Subtasks:**

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

...existing code...

### Documentation Updates

Summary: Ensure all new features, migration steps, and usage patterns are clearly documented for developers and users.

**Tasks:**

- Update the main README to describe the new metadata schema loader, registry pattern, reserved tag logic, and migration steps from `metadata.yaml` to `.yml`.
- Add a migration guide detailing upgrade steps, breaking changes, and troubleshooting for legacy users.
- Update API documentation for all affected interfaces, classes, and CLI commands to reflect schema loader and registry usage.
- Document the structure and usage of the new `metadata-schema.yml` file, including reserved tags and universal fields.
- Update plugin documentation to describe new config settings, schema file usage, and resolver extension points.
- Add/expand XML documentation comments in all updated C# interfaces, classes, and methods.
- Ensure all new and refactored code includes inline comments explaining non-obvious logic and security assumptions.
- Update or create diagrams illustrating the registry pattern, resolver flow, and migration process.
- Review and update all references to `metadata.yaml` in documentation, replacing with `.yml` and new schema logic.

**Subtasks:**

- [ ] Update README with schema loader, registry, reserved tag, and migration details
- [ ] Add migration guide for legacy users
- [ ] Update API docs for affected interfaces, classes, and CLI commands
- [ ] Document `metadata-schema.yml` structure and usage
- [ ] Update plugin documentation for config, schema, and resolver extension
- [ ] Add/expand XML documentation in C# code
- [ ] Add inline comments for complex logic and security assumptions
- [ ] Update/create diagrams for registry, resolver, and migration
- [ ] Replace all `metadata.yaml` references in docs with `.yml` and new schema logic

Summary: Migrate all config and extension references to the new `.yml` schema file, update build/deployment scripts, and deprecate legacy `metadata.yaml` throughout the codebase and documentation.

**Tasks:**

- Add a config setting in the Obsidian plugin for the new metadata schema file (prefer .yml extension for consistency).
  - *Why:* Supports migration and future extensibility of schema logic in the plugin.
- Update plugin logic to load and use metadata-schema.yml for all metadata operations.
  - *Why:* Ensures all plugin operations use the latest schema and reserved tag logic.
- Update C# AppConfig logic to resolve and provide access to the new schema file, mirroring legacy metadata.yaml resolution.
  - *Why:* Maintains compatibility and supports a smooth migration path.
- Refactor all references and documentation to use .yml extension instead of .yaml.
  - *Why:* Standardizes schema file usage and prevents confusion/errors.
- Bundle the new schema file with the plugin; update build and deployment scripts to include it.
  - *Why:* Guarantees all deployments include the correct schema logic.
- Deprecate/remove references to metadata.yaml throughout the codebase and documentation.
  - *Why:* Finalizes migration and eliminates legacy dependencies.

**Subtasks:**

- [ ] Audit all config and extension references for schema file usage
- [ ] Add config setting in Obsidian plugin for new metadata schema file (.yml)
- [ ] Update plugin logic to load/use metadata-schema.yml for all metadata operations
- [ ] Update C# AppConfig logic to resolve/provide access to new schema file
- [ ] Refactor all references/documentation to use .yml extension
- [ ] Update build/deployment scripts to bundle new schema file
- [ ] Remove references to metadata.yaml in codebase and documentation
- [ ] Add/expand migration tests for config and extension switch

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
