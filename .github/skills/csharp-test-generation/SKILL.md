---
name: csharp-test-generation
description: Generate MSTest + Moq unit tests for Notebook Automation, following existing patterns in src/c-sharp/NotebookAutomation.Tests.
examples:
	- "Add MSTest + Moq tests for a new service in NotebookAutomation.Core"
	- "Create a regression test for a bug fix"
	- "Increase coverage for edge cases (null/empty/exception paths)"
---

# C# Test Generation (MSTest + Moq)

## When to use

Use this skill when the user asks to:

- Add new unit tests or increase coverage for C# code.
- Create tests for bug fixes or regressions.

## Workflow

1. Locate the production code and existing tests near it (mirror the folder structure when possible).
2. Reuse existing fixtures/utilities and established patterns.
3. Write tests using Arrange–Act–Assert.
4. Prefer deterministic tests; use temp files/folders and clean up.

## Conventions to follow

- [Test generation instructions](../../instructions/copilot-test-generation.instructions.md)
- [Test reuse guidance](../../instructions/copilot-test-reuse.instructions.md)

## Practical patterns (from this repo)

- Use `[TestInitialize]` for setup and `[TestCleanup]` for temp-file cleanup.
- Use `Assert.ThrowsException<T>()` for exception paths.
- Use Moq `Setup(...)`/`Verify(...)` for external dependency interactions.

## Examples (input → expected output)

### Example: Add tests for a bug fix

**Input:** “Fix a null-handling bug and add coverage.”

**Expected output:**

- A regression test that fails before the fix and passes after
- Clear Arrange–Act–Assert structure
- Minimal mocking (only external dependencies)
