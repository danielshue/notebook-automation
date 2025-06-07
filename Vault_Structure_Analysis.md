---
auto-generated-state: writable
date-created: 2025-06-06
publisher: University of Illinois at Urbana-Champaign
tags: ''
---

# ASCII Vault Structure Map

## Path Analysis

**Vault Root**: `C:\Users\danshue.REDMOND\Vault\01_Projects\MBA\`
**Target Path**: `C:\Users\danshue.REDMOND\Vault\01_Projects\MBA\Value Chain Management\Operations Management\operations-management-organization-and-analysis`

## Hierarchy Detection

**Relative Path**: `Value Chain Management\Operations Management\operations-management-organization-and-analysis`

### Detected Hierarchy

- **Program**: Value Chain Management (Level 1)
- **Course**: Operations Management (Level 2)
- **Class**: operations-management-organization-and-analysis (Level 3)
- **Module**: (empty - only 3 levels deep)

---

## Recommended Vault Structure

```
C:\Users\danshue.REDMOND\Vault\01_Projects\MBA\
│
└── Value Chain Management/                    ← PROGRAM LEVEL
    ├── Value Chain Management.md             ← Program Index File
    │
    └── Operations Management/                ← COURSE LEVEL
        ├── Operations Management.md          ← Course Index File
        │
        └── operations-management-organization-and-analysis/  ← CLASS LEVEL
            ├── operations-management-organization-and-analysis.md  ← Class Index File
            │
            ├── Module 1 - Organizational Structure/         ← MODULE LEVEL (Suggested)
            │   ├── Module 1 - Organizational Structure.md   ← Module Index File
            │   │
            │   ├── Lesson 1 - Introduction/                 ← LESSON LEVEL
            │   │   ├── Lesson 1 - Introduction.md           ← Lesson Index File
            │   │   ├── lecture-video.mp4
            │   │   ├── video-transcript.md
            │   │   ├── lecture-notes.md
            │   │   └── required-reading.md
            │   │
            │   ├── Lesson 2 - Organizational Design/
            │   │   ├── Lesson 2 - Organizational Design.md
            │   │   ├── case-study.md
            │   │   ├── design-template.xlsx
            │   │   └── submission-guidelines.md
            │   │
            │   └── Resources/
            │       ├── additional-readings.md
            │       └── reference-materials.pdf
            │
            ├── Module 2 - Process Analysis/                 ← MODULE LEVEL (Suggested)
            │   ├── Module 2 - Process Analysis.md           ← Module Index File
            │   │
            │   ├── Lesson 1 - Process Mapping/              ← LESSON LEVEL
            │   │   ├── Lesson 1 - Process Mapping.md
            │   │   ├── mapping-tutorial.mp4
            │   │   ├── process-examples.md
            │   │   └── mapping-exercise.xlsx
            │   │
            │   ├── Lesson 2 - Analysis Techniques/
            │   │   ├── Lesson 2 - Analysis Techniques.md
            │   │   ├── analysis-methods.md
            │   │   ├── case-study-analysis.pdf
            │   │   └── technique-comparison.xlsx
            │   │
            │   └── Case Studies/
            │       ├── Case Studies.md
            │       └── Supply Chain Analysis/
            │           ├── Supply Chain Analysis.md
            │           ├── case-instructions.md
            │           ├── company-data.xlsx
            │           ├── market-analysis.pdf
            │           └── submission-template.md
            │
            ├── Module 3 - Performance Optimization/         ← MODULE LEVEL (Suggested)
            │   ├── Module 3 - Performance Optimization.md
            │   │
            │   ├── Lesson 1 - KPI Development/
            │   │   ├── Lesson 1 - KPI Development.md
            │   │   ├── kpi-framework.md
            │   │   ├── metrics-examples.xlsx
            │   │   └── dashboard-templates.xlsx
            │   │
            │   ├── Lesson 2 - Continuous Improvement/
            │   │   ├── Lesson 2 - Continuous Improvement.md
            │   │   ├── improvement-methodologies.md
            │   │   ├── lean-six-sigma.pdf
            │   │   └── project-template.md
            │   │
            │   └── Final Project/
            │       ├── Final Project.md
            │       ├── project-requirements.md
            │       ├── evaluation-rubric.md
            │       ├── presentation-template.pptx
            │       └── submission-portal.md
            │
            └── Resources/                                   ← CLASS RESOURCES
                ├── Resources.md
                ├── course-syllabus.md
                ├── reading-list.md
                ├── software-tools.md
                ├── glossary.md
                └── contact-information.md
```

---

## Current State Analysis

### What You Currently Have

```
C:\Users\danshue.REDMOND\Vault\01_Projects\MBA\
└── Value Chain Management/
    └── Operations Management/
        └── operations-management-organization-and-analysis/  ← You are here
```

### Hierarchy Detection Results

- **Program**: "Value Chain Management"
- **Course**: "Operations Management"
- **Class**: "operations-management-organization-and-analysis"
- **Module**: "" (empty - needs to be added)

---

## Recommendations

### 1. **Add Module Structure**

Your current path only goes 3 levels deep. Consider adding module-level organization within your class:

```
operations-management-organization-and-analysis/
├── operations-management-organization-and-analysis.md  ← Class Index
├── Fundamentals/                                      ← Module 1
├── Advanced Topics/                                   ← Module 2
└── Practical Applications/                            ← Module 3
```

### 2. **Create Index Files**

Add index files at each level following the naming convention:

- `Value Chain Management.md` (Program index)
- `Operations Management.md` (Course index)
- `operations-management-organization-and-analysis.md` (Class index)

### 3. **Organize Content by Type**

Within each module, organize content by type:

```
Fundamentals/
├── Fundamentals.md                    ← Module Index
├── Introduction/                      ← Lesson
│   ├── Introduction.md               ← Lesson Index
│   ├── lecture-video.mp4
│   ├── transcript.md
│   └── notes.md
├── Core Concepts/                     ← Lesson
└── Resources/                         ← Module Resources
```

### 4. **Metadata Behavior**

With this structure, your metadata will include:

**For Class Index** (`operations-management-organization-and-analysis.md`):

- program: "Value Chain Management"
- course: "Operations Management"
- class: "operations-management-organization-and-analysis"

**For Module Index** (e.g., `Fundamentals.md`):

- program: "Value Chain Management"
- course: "Operations Management"
- class: "operations-management-organization-and-analysis"
- module: "Fundamentals"

**For Lesson Content** (e.g., files in `Introduction/`):

- program: "Value Chain Management"
- course: "Operations Management"
- class: "operations-management-organization-and-analysis"
- module: "Fundamentals"

---

## File Naming Conventions

### Index Files

- Named after their containing folder
- Use `.md` extension
- Example: `operations-management-organization-and-analysis.md`

### Content Files

- Descriptive names based on content type
- Examples:
  - `lecture-video.mp4`
  - `video-transcript.md`
  - `lecture-notes.md`
  - `case-study.md`
  - `assignment-instructions.md`

### Folder Names

- Use descriptive, hierarchical naming
- Consistent with academic/business structure
- Examples:
  - `Module 1 - Topic Name`
  - `Lesson 1 - Specific Topic`
  - `Case Studies`
  - `Resources`

---

## Index File Content Suggestions

### Class Index Example (`operations-management-organization-and-analysis.md`)

```markdown
---
title: Operations Management Organization and Analysis
type: class-index
program: Value Chain Management
course: Operations Management
class: operations-management-organization-and-analysis
---

# Operations Management Organization and Analysis

## Course Overview
This class focuses on the organizational aspects and analytical methods used in operations management within value chain contexts.

## Modules
- [[Fundamentals]] - Core concepts and principles
- [[Advanced Topics]] - Complex analytical methods
- [[Practical Applications]] - Real-world case studies

## Learning Objectives
- Understand organizational structures in operations
- Apply analytical methods to operations problems
- Develop practical solutions for operational challenges
```

This structure will ensure your vault works optimally with the MetadataHierarchyDetector and provides a clear, navigable learning environment.
