# .github Directory

This directory contains configuration files and instructions for GitHub Copilot and other GitHub-related functionality.

## Copilot Instruction Files

These files provide guidance to GitHub Copilot to ensure it generates code and content that matches the project's style, standards, and best practices.

### Primary Instruction Files

- **copilot-instructions.md**: General instructions for all Copilot interactions
- **copilot-code-generation.md**: Specific instructions for code generation
- **copilot-commit-message-generation.md**: Instructions for generating git commit messages
- **copilot-pull-request-description-instructions.md**: Instructions for PR descriptions
- **copilot-review-instructions.md**: Guidelines for code review comments

### Additional Files

- **copilot-test-generation.md**: Instructions for generating test code
- **copilot-code-reuse-instructions.md**: Guidelines for code reuse
- **copilot-test-reuse-instructions.md**: Guidelines for test reuse

## VS Code Configuration

These Copilot instruction files are referenced in the VS Code `settings.json` file to customize Copilot behavior.

Example configuration:

```jsonc
"github.copilot.chat.codeGeneration.instructions": [
  {
    "file": "D:\\repos\\mba-notebook-automation\\docs\\copilot-codeGeneration-instructions.md"
  }
],
"github.copilot.chat.pullRequestDescriptionGeneration.instructions": [
  {
    "file": "D:\\repos\\mba-notebook-automation\\docs\\copilot-pull-request-description-instructions.md"
  }
],
"github.copilot.chat.commitMessageGeneration.instructions": [
  {
    "file": "D:\\repos\\mba-notebook-automation\\docs\\copilot-commit-message-instructions.md"
  }
],
"github.copilot.chat.testGeneration.instructions": [
  {
    "file": "D:\\repos\\mba-notebook-automation\\docs\\copilot-test-instructions.md"
  }
]
```

## Note

There are duplicate instruction files in both the `.github` directory and the `docs` directory. 
The files in `docs` are referenced in VS Code settings, while the files in `.github` serve as reference for GitHub-related operations.
When updating instruction files, make sure to keep both versions in sync.
