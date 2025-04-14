<%*
// Templater script for creating a live session note
const fileName = tp.file.title;
const creationDate = tp.date.now("YYYY-MM-DD");
-%>
---
template-type: live-session-note
auto-generated-state: writable
created: <% creationDate %>
title: <% fileName %>
tags: #live-session #notes
---

# <% fileName %>

## Session Overview
<!-- Brief overview of the live session topics and objectives -->

## Key Takeaways
- 
- 
- 

## Discussion Points
- 
- 
- 

## Q&A Notes
- 
- 

## Follow-up Items
- [ ] 
- [ ] 

## Related Materials
- 
