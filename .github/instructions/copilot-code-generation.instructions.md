---
description: Guidance for generating C# code in this repository.
applyTo: "**/*.cs"
---

# GitHub Copilot Code Generation Instructions (C#)

## Goals

- Produce maintainable, readable code that matches this repo’s style.
- Prefer clear architecture and testability over cleverness.
- Keep changes minimal and focused on the ask.

## Project Conventions

- **License header**: For new C# source files, include the standard header:
  - `// Licensed under the MIT License. See LICENSE file in the project root for full license information.`
- **Namespaces**: Use **file-scoped namespaces** for new files.
- **Modern C#**: Prefer primary constructors for DI-style classes when initialization is simple.
- **Dependencies**: Depend on abstractions (interfaces), keep components loosely coupled.

## Code Style

- Use `ArgumentNullException.ThrowIfNull(...)` / `ArgumentException.ThrowIfNullOrEmpty(...)` where appropriate.
- Prefer `switch` expressions and pattern matching for simple branching.
- Prefer collection expressions (e.g., `[]`, `[1, 2, 3]`) where they improve readability.
- Avoid adding unnecessary `using` directives when a project `GlobalUsings.cs` already covers them.

## Error Handling

- Catch exceptions only when you can add context or take corrective action.
- Use specific exception types.
- Include actionable messages (what failed + which input/path/value).

## Design Guidance

- Follow SOLID with a strong preference for composition.
- Keep public APIs small and explicit; avoid “god” services.
- Use records for immutable DTOs and configuration-shaped data.

## Logging

- When logging, include context (paths, counts, identifiers) but do not log secrets.
- Prefer structured logging patterns when available.

## Documentation

- Add XML doc comments for public types/methods when the intent is not obvious.
- Keep comments focused on **why**, not **what**.
