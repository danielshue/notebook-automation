---
title: "\U0001F4DA {{class_name}} Dashboard"
auto-generated-state: writable
banner: '[[gies-banner.png]]'
banner_x: 0.25
publisher: University of Illinois at Urbana-Champaign
date-created: <% tp.date.now("YYYY-MM-DD") %>
date-modified: <% tp.date.now("YYYY-MM-DD") %>
linter-yaml-title-alias: "\U0001F4DA {{class_name}} Dashboard"
tags: []
---

# 📚 {{class_name}} Dashboard

## 📖 Required Readings
```dataview
TABLE pages, status
FROM ""
WHERE (type = "reading" OR type = "instructions") AND class = this.class
SORT file.name ASC
```

## 🎥 Videos

```dataview
TABLE title, file.folder AS "Folder", status
FROM ""
WHERE type = "video-reference" AND class = this.class
SORT Folder ASC
```

## 📚 Case Studies

```dataview
TABLE status
FROM ""
WHERE type = "note/case-study" AND class = this.class
SORT file.name ASC
```

## 📝 Assignments

```dataview
TABLE title AS "Assignment", type, status, due
FROM ""
WHERE class = this.class AND contains(type, "assignment")
```

## ✅ Tasks

```tasks
path includes accounting-for-manager
not done
sort by due
```