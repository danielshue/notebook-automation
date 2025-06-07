---
template-type: class
auto-generated-state: writable
banner: "gies-banner.png"
template-description: Top-level folder for a single class.
title: Corporate Finance
type: index
date-created: 2025-06-07
program: Digital Program
course: Finance I Course
class: Corporate-Finance
publisher: University of Illinois at Urbana-Champaign
tags: ''
---

# Corporate Finance



🔙 [[Finance I Course|Finance I]] | 🏠 [[MBA|Home]] | 📊 [[Dashboard]] | 📝 [[Classes Assignments]]



## Modules

- [[Module 1|Content]]



## 📚 Readings

```base
filters:
  and:
    - course.contains("Finance I Course")
    - class.contains("Corporate-Finance")
    - type.contains("reading")
views:
  - type: table
    name: Table
    order:
      - file.name
      - type
      - status
      - module
    columnSize:
      note.type: 98
    sort:
      - column: note.module
        direction: DESC
      - column: file.name
        direction: DESC

```



## 📝 Instructions

```base
filters:
  and:
    - course.contains("Finance I Course")
    - class.contains("Corporate-Finance")
    - type.contains("instructions")
views:
  - type: table
    name: Table
    order:
      - file.name
      - type
      - status
      - module
    sort:
      - column: note.module
        direction: DESC
    columnSize:
      note.type: 98

```



## 📊 Case Studies

```base
filters:
  and:
    - course.contains("Finance I Course")
    - class.contains("Corporate-Finance")
    - type.contains("note/case-study")
views:
  - type: table
    name: Table
    order:
      - file.name
      - type
      - status
      - module
    sort:
      - column: note.module
        direction: DESC
    columnSize:
      note.type: 127
      note.status: 75

```



## 📽️ Videos

```base
filters:
  and:
    - course.contains("Finance I Course")
    - class.contains("Corporate-Finance")
    - type.contains("video-reference")
views:
  - type: table
    name: Table
    order:
      - file.name
      - type
      - status
      - module
    sort: []
    columnSize:
      note.type: 119
      note.status: 98

```