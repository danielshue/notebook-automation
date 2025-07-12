
# Template, Type, and Tagging Metadata Guide

This document defines the canonical rules for frontmatter metadata in the Notebook Automation system. It ensures consistent use of `template-type`, `type`, and `tags` fields for all content modalities.

## Processor Assignment of Template-Types

Several processors in the C# codebase are responsible for assigning or enriching metadata:

- **MetadataEnsureProcessor**
  - Main processor for analyzing markdown files and assigning `template-type`.
  - Uses filename patterns, directory names, and associated files to determine:
    - `note/instruction` for instruction files
    - `resource-reading` for reading materials
    - `pdf-reference` for case studies and PDF-associated files
    - `video-reference` for video-associated files
    - Leaves `index.md` for manual classification

- **MetadataHierarchyDetector**
  - Extracts hierarchy information (program, course, class, module, lesson) from file paths.
  - Updates metadata with hierarchy fields but does not directly assign `template-type`.

- **YamlHelper**
  - Parses and updates YAML frontmatter, but does not assign `template-type`.

- **CourseStructureExtractor**
  - Extracts module and lesson information for metadata enrichment.
  - Does not assign `template-type`.

**Summary:**
Only the `MetadataEnsureProcessor` assigns `template-type` values automatically. Other processors support metadata enrichment but do not set the template-type field.

## Template-Type and Type Logic (C# Implementation)

- **Automatic Detection:**  
  The `MetadataEnsureProcessor` analyzes file paths and names to set `template-type` automatically if not present.  
  - `-instructions.md` or "instruction" → `note/instruction`
  - "reading" in filename or directory → `resource-reading`
  - "case studies" in directory or "case-stud" in filename → `pdf-reference`
  - Associated `.pdf` file → `pdf-reference`
  - Associated video file (`.mp4`, `.mov`, `.avi`) → `video-reference`
  - `index.md` → requires manual classification

- **Type Assignment:**  
  The processor sets the `type` field based on `template-type`:
  - `pdf-reference` → `note/case-study`
  - `video-reference` → `note/video-note`
  - `resource-reading` → `note/reading`
  - `note/instruction` → `note/instruction`
  - Other templates may require manual assignment.

- **Required Fields:**  
  Each template-type enforces a set of required metadata fields.  
  - Universal: `auto-generated-state`, `date-created`, `publisher`
  - PDF: `type`, `comprehension`, `status`, `completion-date`, `authors`, `tags`, etc.
  - Video: `type`, `comprehension`, `status`, `video-duration`, `author`, `tags`, etc.
  - Reading: `type`, `comprehension`, `status`, `page-count`, `authors`, `tags`, etc.

- **Tags:**  
  Always present for all templates. Used for modality, context, and classification.

---

### Example: C#-Driven Metadata

```yaml
template-type: pdf-reference
type: note/case-study
tags:
  - case-study
  - finance
auto-generated-state: writable
date-created: 2025-07-11
publisher: University of Illinois at Urbana-Champaign
comprehension: 0
status: unread
authors: ""
```

---

### Automation Notes

- The processor only overwrites missing or empty fields unless `forceOverwrite` is enabled.
- Dry run mode previews changes without modifying files.
- Index files (`index.md`) require manual review for correct template-type and type assignment.

---

### Reserved Tags

- Use tags for modality (`case-study`, `live-class`, `reading`, etc.) and context (`finance`, `operations`, etc.).
- Tags must be lowercase and hyphenated.

---

### Updating This Guide

- Update this guide whenever new template-types, types, or reserved tags are added in code or metadata.
