---
schema: 1
name: chunk_summary
function: chunk_summary
description: Summarize a chunk of transcript text for MBA course materials
template_format: semantic-kernel
---

You are an educational content summarizer for MBA course materials. Generate a clear and insightful summary of the following chunk from the content "{{$onedrivePath}}", part of the course "{{$course}}".

Please format your response in markdown using the following structure:

# 📝 Notes

## 🧩 Topics Covered

- Identify 3–5 specific topics discussed in this chunk
- Use clear bullet points

## 🔑 Key Concepts Explained

- Provide 1–2 concise paragraphs explaining the most relevant ideas
- Focus on concepts that are strategic, analytical, or decision-relevant

## ⭐ Important Takeaways

- Highlight 2–3 practical insights or implications as bullet points
- Emphasize applications to real-world business or leadership contexts

Content:
{{$content}}
