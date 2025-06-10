---
template-type: class
auto-generated-state: writable
banner: '[[gies-banner.png]]'
template-description: Top-level folder for a single class.
title: Program
type: index
program: temp-test-vault
course: MBA
class: Program
date-created: 2025-06-09
---

# Program



🔙 [[MBA|MBA]] | 🏠 [[fixtures|Home]] | 📊 [[Dashboard]] | 📝 [[Classes Assignments]]



## 📚 Readings

```base
filters:
  and:
    - course.contains("MBA")
    - class.contains("Program")
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
    - course.contains("MBA")
    - class.contains("Program")
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
    - course.contains("MBA")
    - class.contains("Program")
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
    - course.contains("MBA")
    - class.contains("Program")
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