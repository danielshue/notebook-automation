# Mastering PDF Annotations

Learn how to turn your highlighted PDFs into a powerful research database using Notebook Automation.

## Overview

Researchers and students often spend hours highlighting PDFs, only to have those insights trapped in the file. Notebook Automation extracts these highlights (and their context) into your Obsidian vault, making them searchable and linkable.

**What You'll Learn:**
- 📝 Best practices for highlighting
- 🔄 Syncing annotated PDFs from tablets
- 📄 customizing annotation templates
- 🖼️ Extracting diagrams and figures

---

## Part 1: The Annotation Workflow

### Step 1: Annotate Everywhere
Use your favorite PDF reader (iPad, Acrobat, Edge). Notebook Automation supports standard PDF annotations:

- **Highlights**: Extracted as quoted text
- **Comments/Sticky Notes**: Extracted as notes
- **Underlines**: Extracted as important text

> [!TIP]
> **Pro Tip**: Use different colors for different types of information (e.g., Yellow for general, Green for definitions, Red for disagreements).

### Step 2: Organize Your Source
Keep your "Working" PDFs separate from "Archived" ones.

```bash
mkdir -p source/research-papers/active
```

## Part 2: Configuring Extraction

### Basic Extraction
By default, `pdf-notes` extracts text. To prioritize annotations, ensure your config enables it.

**Run extraction:**
```bash
na pdf-notes -p "source/research-papers/active/paper.pdf" --verbose
```

**Output in Markdown:**
```markdown
## Annotations

> [Page 3] This algorithm improves efficiency by O(log n).
> *Note: This is the key finding we should reference.*
```

### Advanced: Customizing the Template
You can change how annotations appear in your notes by editing the template.

**Create `templates/annotation-template.md`:**
```markdown
### Extracted Highlights

{{#each annotations}}
- **Page {{page}}**: "{{content}}"
  - *Context*: {{comment}}
{{/each}}
```

**Apply via Config:**
Update `config.json` to point to your new template (if supported by your version) or use the `--template` flag if available.

## Part 3: Image Extraction
Research papers often have critical diagrams. Don't screenshot them manually!

**Command:**
```bash
na pdf-notes -p "paper.pdf" --extract-images
```

**Result:**
- Creates a folder `paper-images/`
- Extracts all embedded images
- Links them in your markdown note automatically

```markdown
## Figures
![Figure 1](paper-images/figure-1.png)
*Figure 1: System Architecture*
```

## Part 4: The Review Loop

Once extracted into your vault:

1. **Link to Concepts**: Turn keywords in your extracted highlights into wikilinks `[[Like This]]`.
2. **Tag Deeply**: Add `#methodology`, `#results` tags to specific sections.
3. **Graph View**: See which papers cite similar concepts.

## Summary

| Action                 | Command                                       |
| ---------------------- | --------------------------------------------- |
| **Extract Highlights** | `na pdf-notes -p "file.pdf"`                  |
| **Get Images**         | `na pdf-notes -p "file.pdf" --extract-images` |
| **Force Update**       | `na pdf-notes -p "file.pdf" --force`          |

**Next:** Try the [Video Quiz Generator](video-quiz-generator.md) to test your knowledge!
