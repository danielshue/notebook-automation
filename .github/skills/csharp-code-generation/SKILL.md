---
name: csharp-code-generation
description: Generate C# code for Notebook Automation that matches repo conventions (file-scoped namespaces, modern C#, clean DI, safe error handling).
examples:
  - "Add a new service/processor under src/c-sharp that follows repo conventions"
  - "Refactor an existing Core tool to reuse shared utilities"
  - "Implement a small feature and add/adjust unit tests"
---

# C# Code Generation (Notebook Automation)

## When to use

Use this skill when the user asks to:

- Add or refactor C# implementation code under `src/c-sharp/**`.
- Create new services/processors/commands and keep them consistent with this repo.

## Workflow

1. Identify the target project (`NotebookAutomation.Core`, `NotebookAutomation.Cli`, etc.).
2. Search for existing implementations to reuse (especially under `src/c-sharp/NotebookAutomation.Core/Tools` and utilities).
3. Follow repo conventions:
   - Standard license header for new C# files
   - File-scoped namespaces
   - Prefer primary constructors for simple dependency initialization
   - Depend on abstractions (interfaces) and keep code testable
4. Add/adjust tests when behavior changes (see the `csharp-test-generation` skill).

## Examples (input → expected output)

### Example: Add a new processor/service

**Input:** “Add a new processor under Core/Tools that reuses existing utilities.”

**Expected output:**

- New code placed in the correct project/folder
- Uses existing abstractions/utilities where available
- Small, focused diff with consistent style (file-scoped namespaces, modern C#)
- Unit tests added/updated when behavior changes

## Conventions to follow

This repo’s baseline rules are captured in:

- [Code generation instructions](../../instructions/copilot-code-generation.instructions.md)
- [Code reuse priority](../../instructions/copilot-code-reuse.instructions.md)

## Output expectations

- Small, focused diffs; avoid unrelated reformatting.
- Clear exceptions and messages when failing on paths/inputs.
- No hard-coded secrets.
