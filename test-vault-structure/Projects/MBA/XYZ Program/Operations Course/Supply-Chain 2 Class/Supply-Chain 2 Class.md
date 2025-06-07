---
template-type: class
auto-generated-state: writable
banner: gies-banner.png
template-description: Top-level folder for a single class.
title: Supply Chain 2 Class
type: index
date-created: 2025-06-07
program: XYZ Program
course: Operations Course
class: Supply-Chain 2 Class
publisher: University of Illinois at Urbana-Champaign
lesson: Supply Chain 2 Class
tags: ''
---

# Supply Chain 2 Class



🔙 [[Operations Course|Operations]] | 🏠 [[MBA|Home]] | 📊 [[Dashboard]] | 📝 [[Classes Assignments]]



## Modules

- [[Module 1 XYZ|1 XYZ]]

- [[Module 2|Content]]

- [[Resources|Resources]]



## 📚 Readings

```base
filters:
  and:
    - course.contains("Operations Course")
    - class.contains("Supply-Chain 2 Class")
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
    - course.contains("Operations Course")
    - class.contains("Supply-Chain 2 Class")
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
    - course.contains("Operations Course")
    - class.contains("Supply-Chain 2 Class")
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
    - course.contains("Operations Course")
    - class.contains("Supply-Chain 2 Class")
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