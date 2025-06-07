---
template-type: class
auto-generated-state: writable
banner: gies-banner.png
template-description: Top-level folder for a single class.
title: Digital Marketing
type: index
date-created: 2025-06-07
program: Digital Program
course: Marketing I Course
class: Digital-Marketing
publisher: University of Illinois at Urbana-Champaign
tags: ''
---

# Digital Marketing



🔙 [[Marketing I Course|Marketing I]] | 🏠 [[MBA|Home]] | 📊 [[Dashboard]] | 📝 [[Classes Assignments]]



## Modules

- [[Module1|Module1]]



## 📚 Readings

```base
filters:
  and:
    - course.contains("Marketing I Course")
    - class.contains("Digital-Marketing")
    - module.contains("")
    - type.contains("reading")
views:
  - type: table
    name: Table
    order:
      - file.name
      - type
      - status
      - module

```



## 📝 Instructions

```base
filters:
  and:
    - course.contains("Marketing I Course")
    - class.contains("Digital-Marketing")
    - module.contains("")
    - type.contains("instructions")
views:
  - type: table
    name: Table
    order:
      - file.name
      - type
      - status
      - module

```



## 📊 Case Studies

```base
filters:
  and:
    - course.contains("Marketing I Course")
    - class.contains("Digital-Marketing")
    - module.contains("")
    - type.contains("note/case-study")
views:
  - type: table
    name: Table
    order:
      - file.name
      - type
      - status
      - module

```



## 📽️ Videos

```base
filters:
  and:
    - course.contains("Marketing I Course")
    - class.contains("Digital-Marketing")
    - module.contains("")
    - type.contains("video-reference")
views:
  - type: table
    name: Table
    order:
      - file.name
      - type
      - status
      - module

```