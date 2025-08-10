---
mode: 'agent'
tools: ['changes', 'codebase', 'editFiles', 'problems']
description: 'Ensure that C# types are documented with XML comments and follow best practices for documentation.'
---

# C# Documentation Best Practices

Focus specifically on the selected file.

- Public members should be documented with XML comments.
- It is encouraged to document internal members as well, especially if they are complex or not self-explanatory.
- Use `<summary>` for method descriptions. This should be a brief overview of what the method does.
- Use `<param>` for method parameters.
- Use `<returns>` for method return values.
- Use `<remarks>` for additional information, which can include implementation details, usage notes, or any other relevant context.
- Use `<example>` for usage examples on how to use the member.
- Use `<exception>` to document exceptions thrown by methods.
- Use `<see>` and `<seealso>` for references to other types or members.
- Use `<inheritdoc/>` to inherit documentation from base classes or interfaces.
  - Unless there is major behavior change, in which case you should document the differences.
- Use `<typeparam>` for type parameters in generic types or methods.
- Use `<typeparamref>` to reference type parameters in documentation.
- Use `<c>` for inline code snippets.
- Use `<code>` for code blocks.

## Project Requirements (Notebook Automation)

### Coverage and Scope

- 100% of public APIs MUST be documented: classes, interfaces, records, structs, enums, delegates, methods, constructors, properties, events, and fields (when public).
- Internal members SHOULD be documented when non-trivial, affect public behavior, or require explanation (async behavior, caching, IO, security, threading).
- Generated or implementation-only members MAY use `<inheritdoc/>` unless behavior deviates, in which case provide explicit docs.

### Style and Tone

- Write in clear, concise, third-person singular, present tense.
- First sentence of `<summary>` is a one-line overview; details go in `<remarks>`.
- Prefer imperative phrasing for behavior ("Returns", "Gets", "Sets", "Creates").
- Use `<para>` to separate paragraphs and `<list>` for lists inside `<remarks>`.

### Required Tags by Member Type

- Methods
  - `<summary>` what the method does and when to use it.
  - `<param>` for each parameter. Include units, allowed ranges, nullability, and defaults if applicable. Use `<paramref>` when referencing in remarks.
  - `<returns>` describing the meaning of the return value. For `Task`/`ValueTask`, describe the completion result; for `Task<T>`, describe `T`.
  - `<exception>` for each exception the method can throw and when.
  - If cancellation is supported, document `CancellationToken` semantics.
- Constructors
  - `<summary>` and `<param>` for all injected dependencies explaining roles and lifecycle expectations.
- Properties/Indexers
  - `<summary>` starts with "Gets", "Sets", or "Gets or sets".
  - Document units, ranges, default values, and side effects in `<remarks>`.
  - Use `<value>` to describe the value semantics when non-trivial (computed, cached, constrained, or with invariants). Recommended for all public properties.
- Events
  - `<summary>` describing when the event is raised and payload semantics.
  - Reference event args using `<see cref="..."/>`.
- Enums
  - `<summary>` for the enum and each member describing meaning and usage.

### Async, Cancellation, and Concurrency

- Clearly document async behavior and whether operations are idempotent.
- If the method honors `CancellationToken`, include it in `<param>` and explain what is cancellable.
- Note thread-safety and synchronization in `<remarks>` when relevant.

### Nullability and Defaults

- Explicitly document when parameters or return values can be `null`.
- State default values and configuration-driven behavior where applicable.

### Examples

- Provide minimal, focused examples within `<example>` blocks for complex or commonly used APIs.
- Use `<code language="csharp">` for syntax highlighting when appropriate.

### Inheritance and Overrides

- Use `<inheritdoc/>` for interface and base member implementations when behavior matches.
- When overriding behavior, provide a new `<summary>` and explain differences in `<remarks>`.

### Overloads and Obsolete/Experimental APIs

- Overloads
  - Document every overload. If behavior matches a base/peer overload, use `<inheritdoc/>` and add only overload-specific parameter notes.
  - Provide examples that show selecting between overloads when meaningful.
  - Avoid duplicating large remarks across overloads; reference shared details with `<see cref="..."/>`.
- Obsolete APIs
  - Mark with `[Obsolete("Reason and replacement", error: false)]`.
  - In XML docs, state deprecation and the recommended alternative in `<remarks>`, linking via `<see cref="..."/>`.
  - Keep summaries factual; do not remove existing behavior notes while deprecated.
- Experimental APIs
  - Annotate with an attribute (e.g., `[Experimental]`) if available and clearly state in `<remarks>` that the API is experimental and subject to change.
  - Include version introduced and usage cautions.

### Test Project Documentation

- Test classes SHOULD include a `<summary>` describing the unit under test and behaviors covered.
- Each `[TestMethod]` SHOULD include a `<summary>` starting with "Verifies that ..." and (optionally) a brief `<remarks>` for setup or edge cases.
- Do not over-document obvious assertions; focus on intent and notable scenarios.

### DocFX and Build Enforcement

- Documentation must build without DocFX errors. Warnings should be addressed proactively.
- Do not suppress CS1591 in production projects; tests MAY use minimal documentation but SHOULD follow the above guidance when behavior is non-trivial.
- Use the repository tasks `docs: build` / `docs: serve` to validate and preview documentation.

### Pull Request Checklist Addendum

- [ ] All new/changed public APIs have XML documentation.
- [ ] Exceptions, nullability, and cancellation are documented where applicable.
- [ ] Enums and their members are documented.
- [ ] Examples provided for complex or frequently used APIs.
- [ ] DocFX build passes locally (no errors).

### Quick Templates

Method (async with cancellation):

```xml
/// <summary>
/// Retrieves course metadata from the configured sources.
/// </summary>
/// <param name="courseId">The unique course identifier. Must not be <c>null</c> or empty.</param>
/// <param name="cancellationToken">Token to observe for cancellation of the operation.</param>
/// <returns>A task that completes with the retrieved <see cref="CourseMetadata"/>.</returns>
/// <exception cref="ArgumentException">Thrown when <paramref name="courseId"/> is null or empty.</exception>
/// <exception cref="InvalidOperationException">Thrown when the configuration is invalid.</exception>
public Task<CourseMetadata> GetCourseAsync(string courseId, CancellationToken cancellationToken = default)
```

Property:

```xml
/// <summary>
/// Gets or sets the effective vault root used for relative path calculations.
/// </summary>
/// <remarks>
/// Changing this value affects hierarchy resolution for program, course, and class.
/// </remarks>
public string EffectiveVaultRoot { get; set; }
```

Enum and members:

```xml
/// <summary>
/// Indicates the processing state of a generated note.
/// </summary>
public enum NoteProcessingState
{
    /// <summary>Note has been generated but not yet reviewed.</summary>
    Pending,
    /// <summary>Note has been reviewed and accepted.</summary>
    Approved,
    /// <summary>Note generation failed due to an error.</summary>
    Failed
}
```
