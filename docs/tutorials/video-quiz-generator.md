# Video Quiz Generator

Turn passive video watching into active learning by automatically generating quizzes from your lecture videos.

## Overview

Active Recall is one of the most effective ways to learn. Notebook Automation can use AI to analyze lecture transcripts and generate quiz questions, allowing you to test your understanding immediately.

**Use Cases:**
- 🎓 **Students**: Preparing for exams
- 👩‍🏫 **Educators**: Creating quick knowledge checks
- 💼 **Professionals**: Validating understanding of training videos

---

## Part 1: Generating a Quiz

To generate a quiz, you use the `--quiz` modifier (if supported by your installed version) or configure a custom prompt. 

*Note: If your CLI version doesn't have a direct `--quiz` flag, we'll show you how to do it via Custom Prompts.*

### Using Custom Prompts for Quizzes

You can instruct the AI to output a quiz instead of a summary.

**1. Create a Quiz Config (`config.quiz.json`):**
```json
{
  "AIService": {
    "CustomPrompts": {
      "SummaryPrompt": "Analyze the following transcript. Instead of a summary, generate 5 multiple-choice questions based on the key concepts. Format the output as Markdown with the question, options, and the answer hidden in a collapsible block."
    }
  }
}
```

**2. Run the Processing:**
```bash
na video-notes -p "lecture-01.mp4" --config config.quiz.json --verbose
```

### Resulting Output
The AI will generate a note that looks like this:

```markdown
# Quiz: Lecture 01

## Question 1
What is the primary function of the Mitochondria?
A) DNA replication
B) Energy production
C) Protein synthesis
D) Waste removal

<details>
<summary>Click to reveal answer</summary>
**Answer: B) Energy production**
*Context: Discussed at timestamp 12:30*
</details>

...
```

## Part 2: Integration with Anki

If you use Anki for spaced repetition, you can format your prompt to output CSV data compatible with Anki import.

**Prompt for Anki:**
`"Generate 10 flashcards based on this transcript. Format strictly as CSV: Question,Answer. No headers."`

**Workflow:**
1. Run `na video-notes` with Anki config.
2. Open the generated `.md` file.
3. Copy the CSV content.
4. Import into Anki.

## Part 3: Study Workflow

1. **Watch**: Watch the lecture video.
2. **Generate**: Run the quiz generation script.
3. **Test**: Attempt the quiz immediately in Obsidian.
4. **Review**: Use the generated links to jump back to the specific timestamp in the video loop for any questions you missed.

> [!TIP]
> **Active Recall**: Don't just read the answer. Try to answer out loud before clicking "reveal".

## Summary

| Goal            | Method                                     |
| --------------- | ------------------------------------------ |
| **Self-Test**   | Use Collapsible Markdown config            |
| **Flashcards**  | Use CSV/Anki prompt config                 |
| **Group Study** | Generate questions without answers visible |

**Next:** Learn how to keep your vault healthy with [Vault Maintenance](vault-maintenance.md).
