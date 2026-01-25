---
description: Guidance for generating MSTest + Moq unit tests in this repository.
applyTo: "src/c-sharp/NotebookAutomation.Tests/**/*.cs"
---

# GitHub Copilot Test Generation Instructions (MSTest + Moq)

## Test Frameworks

- Use **MSTest** attributes (`[TestClass]`, `[TestMethod]`, `[TestInitialize]`, `[TestCleanup]`).
- Use **Moq** for mocking (`Mock<T>`, `It.IsAny<T>()`, `Setup`, `Verify`).

## Structure

- Follow **Arrange – Act – Assert**.
- Prefer **one behavior per test**.
- Use descriptive names consistent with the existing suite, e.g.:
  - `Method_WithCondition_ExpectedOutcome`
  - `Method_WhenCondition_DoesSomething`

## Coverage Expectations

- Cover:
  - Happy path
  - Null/empty/invalid inputs (when applicable)
  - Exception paths (use `Assert.ThrowsException<T>()`)
  - Boundary conditions

## Best Practices

- Keep tests deterministic (no timers, network, real filesystem outside temp folders).
- Prefer temp files/folders via `Path.GetTempFileName()` / `Path.GetTempPath()` and always clean up.
- Mock external dependencies via interfaces; avoid mocking “value objects”.
- Avoid over-mocking: assert observable behavior and important interactions.

## Reuse

- Reuse existing helpers/fixtures/utilities in the test project when they exist.
- Follow the guidance in `.github/instructions/copilot-test-reuse.instructions.md`.

## Assertions

- Prefer precise assertions (exact strings, counts, specific keys) rather than broad `Contains` when reasonable.
- If asserting on collections/dictionaries, verify both presence and value.

## Test Documentation

- Keep XML doc comments consistent with existing tests (summary per class and key tests).
- Do not add excessive commentary; the name + AAA blocks should carry most intent.
