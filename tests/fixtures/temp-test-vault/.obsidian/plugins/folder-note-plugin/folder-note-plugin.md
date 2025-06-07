---
template-type: class-index
auto-generated-state: writable
banner: gies-banner.png
template-description: Top-level folder for a single class.
title: Folder Note Plugin
type: index
index-type: class
date-created: 2025-06-07
program: plugins
course: folder-note-plugin
class: 
module: 
---

# Folder Note Plugin



🔙 [[plugins|Plugins]] | 🏠 [[|Home]] | 📊 [[Dashboard]] | 📝 [[Classes Assignments]]



## 📚 Readings

```base
filters:
  and:
    - course.contains("folder-note-plugin")
    - class.contains("")
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
    - course.contains("folder-note-plugin")
    - class.contains("")
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
    - course.contains("folder-note-plugin")
    - class.contains("")
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
    - course.contains("folder-note-plugin")
    - class.contains("")
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