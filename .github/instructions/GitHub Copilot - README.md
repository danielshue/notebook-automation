
# .github Copilot Instructions Directory

This directory contains all instruction files and configuration for GitHub Copilot and related automation. The approach is to centralize project-specific meta instructions in `copilot-instructions.md` and reference all detailed guidelines in separate instruction files. This ensures Copilot and contributors have clear, maintainable, and discoverable guidance for every aspect of the project.

## Centralized Instruction Reference

**copilot-instructions.md** is the entry point for all Copilot guidance. It provides:

- Project-specific meta instructions
- References to all detailed instruction files below

## Instruction Files

- **copilot-instructions.md**: Project meta instructions and reference list
- **aspnet-rest-apis.instructions.md**: ASP.NET REST API development
- **copilot-code-generation.instructions.md**: Code generation and structure
- **copilot-code-reuse.instructions.md**: Code reuse and package discovery
- **copilot-commit-message-instructions.md**: Commit message format and emoji usage
- **copilot-pull-request-description.instructions.md**: Pull request title and description guidelines
- **copilot-review-instructions.md**: Code review process and comment format
- **copilot-test-generation.instructions.md**: Test generation and coverage
- **copilot-test-reuse.instructions.md**: Test reuse and fixture strategy
- **copilot-thought-logging.instructions.md**: Thought logging and process tracking
- **dotnet-maui.instructions.md**: .NET MAUI component and app patterns
- **localization.instructions.md**: Localization for markdown documents
- **memory-bank.instructions.md**: Memory bank structure and workflows

## VS Code Configuration

These instruction files can be referenced in VS Code `settings.json` to customize Copilot behavior. Example configuration:

```jsonc
"github.copilot.chat.codeGeneration.instructions": [
  {
    "file": "D:\\source\\notebook-automation\\.github\\instructions\\copilot-code-generation.instructions.md"
  }
],
"github.copilot.chat.pullRequestDescriptionGeneration.instructions": [
  {
    "file": "D:\\source\\notebook-automation\\.github\\instructions\\copilot-pull-request-description.instructions.md"
  }
],
"github.copilot.chat.commitMessageGeneration.instructions": [
  {
    "file": "D:\\source\\notebook-automation\\.github\\instructions\\copilot-commit-message-instructions.md"
  }
],
"github.copilot.chat.testGeneration.instructions": [
  {
    "file": "D:\\source\\notebook-automation\\.github\\instructions\\copilot-test-generation.instructions.md"
  }
]
```

## Note

If you maintain duplicate instruction files in both `.github` and `docs`, keep them in sync. The `.github` versions are authoritative for GitHub and Copilot automation. Update all references and links as new instruction files are added.
