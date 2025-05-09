```markdown
# Enhanced MBA Vault Organization: Nested Tag Strategy

This document outlines a revised approach to organizing your MBA study materials using Obsidian's nested tag feature, now with additional elements for more precise categorization of your academic work.

## Enhanced Tag Structure

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

program/        # Program categorization
  imba          # Illinois iMBA program
  emba          # Executive MBA (if applicable)
  certificate   # Certificate programs

course/         # Course codes
  ACCY501       # Accounting for Managers
  ACCY502       # Managerial Accounting
  FIN501        # Corporate Finance
  FIN571        # Investments
  MKTG571       # Marketing Management
  BADM508       # Leadership and Teams
  BADM520       # Strategic Management
  BADM509       # Managing Organizations
  ECON528       # Statistics and Econometrics  
  ECON540       # Managerial Economics
  # Add other course codes as needed

term/           # Academic terms
  2023-fall
  2024-spring
  2024-summer
  2024-fall
  2025-spring
  2025-summer
  2025-fall
  2026-spring
  2026-summer
  2026-fall
  2027-spring
  2027-summer
  2027-fall

mba/            # Subject area categorization
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

## Updated Implementation Strategy

With this enhanced tag structure, we'll implement a more detailed organization system:

1. **Course-Specific Tags**
   - Each note will include the specific course code (e.g., `course/FIN571`)
   - Subject area tags provide cross-cutting organization (`mba/course/finance/investments`)
   - Program tags identify which degree program the note belongs to (`program/imba`)

2. **Time-Based Organization**
   - Term tags allow filtering by semester (`term/2024-spring`)
   - This enables you to find all notes from a specific academic period

3. **Example Tag Combinations**

For a lecture note in Corporate Finance:
```
- type/note/lecture
- program/imba
- course/FIN501
- term/2024-spring
- mba/course/finance/corporate-finance
- status/active
```

For a case study assignment in Marketing:
```
- type/assignment/case-study
- program/imba
- course/MKTG571
- term/2024-fall
- mba/course/marketing/strategy
- status/active
- priority/high
```

## Template Updates

All templates will be updated to include these new tag categories, with placeholders for course codes and terms that you can fill in when creating a new note.

## Automation Tools

The restructuring script will be updated to identify potential course codes and terms from file paths and content, making it easier to apply these tags to your existing notes.
```
