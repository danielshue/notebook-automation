<%*
// Templater script for creating a lesson note
const fileName = tp.file.title;
const creationDate = tp.date.now("YYYY-MM-DD");
-%>
---
template-type: lesson-note
auto-generated-state: writable
created: <% creationDate %>
title: <% fileName %>
tags: #notes
---

# <% fileName %>

## Summary
<!-- Brief summary of the main concepts covered in this lesson -->

## Key Points
- 
- 
- 

## Questions
- 
- 

## Action Items
- [ ] 
- [ ] 

## Related Resources
- 
