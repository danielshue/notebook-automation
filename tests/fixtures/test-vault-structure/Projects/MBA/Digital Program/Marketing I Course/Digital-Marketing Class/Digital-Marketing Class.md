---
template-type: class
auto-generated-state: writable
banner: gies-banner.png
template-description: Top-level folder for a single class.
title: Digital Marketing Class
type: index
date-created: 2025-06-07
program: Digital Program
course: Marketing I Course
class: Digital-Marketing Class
publisher: University of Illinois at Urbana-Champaign
lesson: Digital Marketing Class
tags: ''
---

# Digital Marketing Class



🔙 [[Marketing I Course|Marketing I]] | 🏠 [[MBA|Home]] | 📊 [[Dashboard]] | 📝 [[Classes Assignments]]



## 📚 Readings

```base
filters:
  and:
    - course.contains("Marketing I Course")
    - class.contains("Digital-Marketing Class")
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
    - class.contains("Digital-Marketing Class")
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
    - class.contains("Digital-Marketing Class")
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
    - class.contains("Digital-Marketing Class")
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