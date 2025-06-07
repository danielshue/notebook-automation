---
auto-generated-state: writable
date-created: 2025-06-06
publisher: University of Illinois at Urbana-Champaign
tags: ''
---

# MBA Vault Organization: Nested Tag Strategy

This document outlines a comprehensive approach to organizing your MBA study materials using Obsidian's nested tag feature. The strategy focuses on creating a flexible, cross-cutting organization system that complements your folder structure.

## Core Strategy

We'll implement a nested tag structure that categorizes content by:
1. Content type
2. Course and subject area
3. Status and workflow stage
4. Tools and skills

## Tag Structure Overview

```
type/           # Content type categorization
  note/         # Different types of notes
    lecture
    case-study
    literature
    meeting
  assignment/   # Assignment categories
    individual
    group
    draft
    final
  project/      # Project statuses
    proposal
    active
    completed
    archived
  resource/     # Resource types
    textbook
    article
    paper
    presentation
    template

mba/            # MBA-specific content
  course/       # Course categories
    finance/    # Finance subcategories
      corporate-finance
      investments
      valuation
    accounting/
      financial
      managerial
      audit
      tax
    marketing/
      digital
      strategy
      consumer-behavior
      analytics
    strategy/
      corporate
      competitive
      global
      innovation
    operations/
      supply-chain
      project-management
      quality-management
      process-design
  skill/        # Skills developed
    quantitative
    qualitative
    presentation
    negotiation
    leadership
  tool/         # Tools learned/used
    excel
    python
    r
    powerbi
    tableau

status/         # Workflow status
  active
  review
  complete
  archived

priority/       # Priority level
  high
  medium
  low
```

## Implementation Approach

This strategy will be implemented in phases:

1. **Setup Phase**
   - Document tag system in a central reference note
   - Create templates with pre-populated tag structures
   - Configure dataview queries for tag-based dashboards

2. **Initial Implementation**
   - Start with one course as a pilot
   - Tag new notes systematically
   - Create course-specific dashboards

3. **Expansion Phase**
   - Apply tagging to existing notes
   - Refine tag structure based on usage patterns
   - Develop additional specialized templates

4. **Maintenance Phase**
   - Periodically review and update tag structure
   - Use automation tools to maintain consistency
   - Generate updated documentation of tag usage

## Tools and Support Scripts

We have developed a Python script (`generate_tag_doc.py`) that can scan your vault and generate documentation of all tags in use, organized by hierarchy. This will help maintain consistency and provide visibility into how the tagging system evolves over time.

### Tag Documentation Generator

The `generate_tag_doc.py` script scans your entire vault to:
- Extract all tags from YAML frontmatter and inline Markdown
- Build a hierarchical representation of nested tags
- Generate a formatted Markdown document showing the complete tag structure
- Identify potential inconsistencies in tag usage

### Example Dataview Queries

To leverage this tag structure, you can use Dataview queries like:

```dataview
TABLE file.name as "Document", file.mtime as "Last Modified"
FROM #type/note/lecture AND #mba/course/finance
SORT file.mtime DESC
```

## Benefits of This Approach

- **Flexibility**: Tags cut across folder structures for multi-dimensional organization
- **Discoverability**: Easily find related content regardless of storage location
- **Consistency**: Standardized tags improve overall organization
- **Scalability**: Structure can grow with your knowledge base
- **Integration**: Works well with Dataview for dynamic content aggregation

## Template Examples

### Basic Lecture Note Template

```markdown
---
title: {{title}}
date: {{date}}
tags:
  - type/note/lecture
  - mba/course/
  - status/active
---

# {{title}}

## Key Concepts

- 

## Notes

-

## Questions & Follow-ups

-
```

### Case Study Template

```markdown
---
title: {{title}}
date: {{date}}
tags:
  - type/note/case-study
  - mba/course/
  - status/active
---

# {{title}}

## Case Overview

## Key Issues

## Analysis

## Recommendations

## Lessons Learned
```

## Integration with Existing MBA Workspace

Looking at the current workspace structure:
- MBA content resides in `Vault/01_Projects/MBA/`
- Course resources are in `MBA-Resources/`

This tagging system can complement this structure by:
1. Maintaining the existing folder organization
2. Adding consistent tags to all files
3. Enabling cross-referencing between related content in different folders

## Index Pages and Tagging

Index pages serve as structural navigation elements and should follow different tagging guidelines:

1. **Avoid Content Tags**: Index pages should not include content-type tags like `#type/note/lecture`, as they are not content themselves but organizational tools.

2. **Use Structural Tags**: If needed, index pages may use special structural tags that won't pollute content searches:
   - `#structure/index`
   - `#structure/main-index`
   - `#structure/course-index` 
   - `#structure/module-index`

3. **Breadcrumbs for Navigation**: Instead of relying on tags for navigation, index pages should use the YAML breadcrumbs system for bidirectional navigation.

4. **Template Designation**: Index pages should include `template-type: [index-type]` in their frontmatter to identify their role in the knowledge structure.

## Next Steps

1. Review the proposed tag structure and customize for your specific needs
2. Set up initial templates for common note types in your Obsidian vault
3. Create a central tag reference document in `Vault/04_Reference/`
4. Begin implementing the system with your most active MBA course
5. Run the tag documentation generator periodically to monitor system evolution

*Note: For more specific template examples and Dataview query samples to leverage this tagging system, consider creating companion reference documents in your vault.*
