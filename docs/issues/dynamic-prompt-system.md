# Feature: Dynamic Prompt System with Template-Type Profiles

## Summary

Implement a dynamic prompt system that allows different content types to use different AI summarization prompts, with support for ad-hoc prompt files and flexible path resolution for processing files outside the configured vault.

## Motivation

Currently, the system uses hardcoded prompt files (`chunk_summary_prompt.md` and `final_summary_prompt.md`) for all content types. This limits flexibility:

- **Different content types need different prompts** - MBA course videos vs. technical PDFs vs. ad-hoc YouTube videos require different summarization approaches
- **Ad-hoc processing is MBA-centric** - Users processing a random YouTube video shouldn't need MBA-specific prompts
- **Path resolution is inflexible** - All outputs go to the configured vault, limiting one-off processing scenarios
- **Two-prompt complexity** - The chunk + final prompt system adds unnecessary complexity when a single prompt suffices for most cases

## Proposed Solution

### 1. Single Prompt Per Template-Type

Simplify from two prompts (chunk + final) to **one prompt per content type**:

- **Chunk extraction** becomes internal/hardcoded (simple extraction, not user-facing)
- **Content summarization** uses the template-type's configured prompt
- Chunking still happens for large content (>8K chars), but uses internal logic

### 2. Template-Type as Complete Processing Profile

Extend `TemplateTypes` in `metadata-schema.yml` to include:

```yaml
TemplateTypes:
  video-reference:
    Type: video-reference
    Prompt: video-reference # Name or full path
    PathResolution:
      InputRoot: onedrive # onedrive | vault | cwd
      OutputRoot: vault # vault | onedrive | cwd | input
    BaseTypes:
      - base-template
    Fields:
      # existing fields...

  generic-video:
    Type: generic-video
    Prompt: generic_prompt
    PathResolution:
      InputRoot: cwd
      OutputRoot: input # Output next to input file
    BaseTypes:
      - base-generic
    Fields:
      # minimal fields...
```

### 3. Inheritance via BaseTypes

Leverage existing `BaseTypes` inheritance for defaults:

```yaml
BaseTypes:
  base-template:
    # MBA defaults
    Prompt: default_prompt
    PathResolution:
      InputRoot: onedrive
      OutputRoot: vault
    Fields:
      # common MBA fields...

  base-generic:
    # Ad-hoc defaults
    Prompt: generic_prompt
    PathResolution:
      InputRoot: cwd
      OutputRoot: input
    Fields:
      # minimal fields...
```

**Inheritance Order** (first wins):

1. Explicit value on template-type
2. Inherited from BaseTypes (in order)
3. System defaults

### 4. PathResolution Configuration

New `PathResolution` configuration:

| Property     | Values                              | Description                 |
| ------------ | ----------------------------------- | --------------------------- |
| `InputRoot`  | `onedrive`, `vault`, `cwd`          | Where to find input files   |
| `OutputRoot` | `vault`, `onedrive`, `cwd`, `input` | Where to write output files |

**OutputRoot = `input`**: Special value meaning "same directory as input file"

### 5. CLI Options

Add new options to `video-notes` and `pdf-notes` commands:

```bash
# Use specific template type (determines prompt, paths, fields)
notebook-cli video-notes --template-type generic-video --url "https://..."

# Override prompt (name or full path)
notebook-cli video-notes --prompt my-custom-prompt --url "https://..."
notebook-cli video-notes --prompt "C:\prompts\special.md" --url "https://..."

# Combined
notebook-cli pdf-notes --template-type generic-pdf --prompt adhoc-summary input.pdf
```

**Resolution Order for Prompts:**

1. `--prompt` CLI option (if provided)
2. Template-type's `Prompt` field
3. Inherited `Prompt` from BaseTypes
4. Default prompt (`default_prompt.md`)

---

## Technical Design

### Schema & Model Changes

**File: `src/c-sharp/NotebookAutomation.Core/Schema/TemplateTypeSchema.cs`**

```csharp
public class TemplateTypeSchema
{
    public string Type { get; set; } = string.Empty;
    public List<string> RequiredFields { get; set; } = [];
    public List<string>? BaseTypes { get; set; }
    public Dictionary<string, FieldDefinition>? Fields { get; set; }

    // NEW
    public string? Prompt { get; set; }
    public PathResolutionConfig? PathResolution { get; set; }
}

public class PathResolutionConfig
{
    public string InputRoot { get; set; } = "onedrive";  // onedrive | vault | cwd
    public string OutputRoot { get; set; } = "vault";    // vault | onedrive | cwd | input
}
```

### Inheritance Extension

**File: `src/c-sharp/NotebookAutomation.Core/Services/MetadataSchemaLoader.cs`**

Extend `ResolveTemplateType()` to inherit `Prompt` and `PathResolution`:

```csharp
public TemplateTypeSchema? ResolveTemplateType(string templateTypeName)
{
    // ... existing logic ...

    // Inherit Prompt if not set
    if (string.IsNullOrEmpty(resolved.Prompt) && baseType.Prompt != null)
    {
        resolved.Prompt = baseType.Prompt;
    }

    // Inherit PathResolution if not set
    if (resolved.PathResolution == null && baseType.PathResolution != null)
    {
        resolved.PathResolution = baseType.PathResolution;
    }

    // ... existing field inheritance ...
}
```

### Prompt Resolution Service

**File: `src/c-sharp/NotebookAutomation.Core/Services/PromptTemplateService.cs`**

Add new method:

```csharp
public async Task<string> LoadPromptForTemplateTypeAsync(
    string? promptOverride,
    TemplateTypeSchema? templateType,
    CancellationToken cancellationToken = default)
{
    // Resolution order:
    // 1. CLI override (if provided)
    if (!string.IsNullOrEmpty(promptOverride))
    {
        return await LoadPromptAsync(promptOverride, cancellationToken);
    }

    // 2. Template-type's prompt (already resolved via inheritance)
    if (templateType?.Prompt != null)
    {
        return await LoadPromptAsync(templateType.Prompt, cancellationToken);
    }

    // 3. System default
    return await LoadTemplateAsync("default_prompt", cancellationToken);
}

private async Task<string> LoadPromptAsync(string promptNameOrPath, CancellationToken ct)
{
    // If full path, load directly
    if (Path.IsPathRooted(promptNameOrPath) || promptNameOrPath.Contains(Path.DirectorySeparatorChar))
    {
        return await File.ReadAllTextAsync(promptNameOrPath, ct);
    }

    // Otherwise, load from prompts_path
    return await LoadTemplateAsync(promptNameOrPath, ct);
}
```

### Path Resolution Utilities

**File: `src/c-sharp/NotebookAutomation.Core/Utils/PathResolver.cs`** (new file)

```csharp
public class PathResolver(AppConfig config)
{
    public string ResolveInputRoot(PathResolutionConfig? pathConfig)
    {
        var inputRoot = pathConfig?.InputRoot ?? "onedrive";
        return inputRoot.ToLowerInvariant() switch
        {
            "onedrive" => config.OneDriveBasePath,
            "vault" => config.NotebookVaultResourcesBasePath,
            "cwd" => Directory.GetCurrentDirectory(),
            _ => config.OneDriveBasePath
        };
    }

    public string ResolveOutputRoot(PathResolutionConfig? pathConfig, string inputFilePath)
    {
        var outputRoot = pathConfig?.OutputRoot ?? "vault";
        return outputRoot.ToLowerInvariant() switch
        {
            "vault" => config.NotebookVaultResourcesBasePath,
            "onedrive" => config.OneDriveBasePath,
            "cwd" => Directory.GetCurrentDirectory(),
            "input" => Path.GetDirectoryName(inputFilePath) ?? Directory.GetCurrentDirectory(),
            _ => config.NotebookVaultResourcesBasePath
        };
    }
}
```

### AISummarizer Simplification

**File: `src/c-sharp/NotebookAutomation.Core/Services/AISummarizer.cs`**

Simplify chunking to use internal extraction:

```csharp
private const string ChunkExtractionPrompt = @"
Extract and summarize the key points from the following content chunk.
Focus on main concepts, important details, and actionable information.
Present the information clearly and concisely.

Content:
{{$content}}
";

public async Task<string> SummarizeAsync(
    string content,
    string promptTemplate,  // Single prompt for final summary
    Dictionary<string, string>? variables = null,
    CancellationToken cancellationToken = default)
{
    if (content.Length <= ChunkThreshold)
    {
        return await ExecuteSummaryAsync(promptTemplate, content, variables, cancellationToken);
    }

    // Large content: chunk, extract, then final summary
    var chunks = _chunkingService.ChunkText(content);
    var extractedContent = new StringBuilder();

    foreach (var chunk in chunks)
    {
        var extraction = await ExecuteSummaryAsync(
            ChunkExtractionPrompt,
            chunk,
            null,
            cancellationToken);
        extractedContent.AppendLine(extraction);
    }

    return await ExecuteSummaryAsync(
        promptTemplate,
        extractedContent.ToString(),
        variables,
        cancellationToken);
}
```

---

## Implementation Plan

### Phase 1: Schema & Model Changes

- [ ] Add `Prompt` property to `TemplateTypeSchema`
- [ ] Add `PathResolutionConfig` class
- [ ] Add `PathResolution` property to `TemplateTypeSchema`
- [ ] Update YAML deserialization if needed

### Phase 2: Inheritance Extension

- [ ] Extend `ResolveTemplateType()` to inherit `Prompt`
- [ ] Extend `ResolveTemplateType()` to inherit `PathResolution`
- [ ] Add unit tests for inheritance behavior

### Phase 3: Prompt Resolution

- [ ] Add `LoadPromptForTemplateTypeAsync()` to `PromptTemplateService`
- [ ] Add `LoadPromptAsync()` helper for name vs path resolution
- [ ] Update `AISummarizer` to use single prompt pattern
- [ ] Hardcode chunk extraction prompt internally
- [ ] Add unit tests

### Phase 4: Path Resolution

- [ ] Create `PathResolver` class (or add to `PathUtils`)
- [ ] Implement `ResolveInputRoot()`
- [ ] Implement `ResolveOutputRoot()`
- [ ] Add unit tests

### Phase 5: CLI Options

- [ ] Add `--template-type` option to `VideoCommands`
- [ ] Add `--prompt` option to `VideoCommands`
- [ ] Add `--template-type` option to `PdfCommands`
- [ ] Add `--prompt` option to `PdfCommands`
- [ ] Wire options through command handlers

### Phase 6: Processor Integration

- [ ] Update `VideoNoteProcessor` to use resolved prompt
- [ ] Update `VideoNoteProcessor` to use resolved paths
- [ ] Update `VideoNoteBatchProcessor` for consistency
- [ ] Update `PdfProcessor` similarly (if applicable)

### Phase 7: Prompt Files

- [ ] Create `prompts/video-reference.md` (MBA video prompt)
- [ ] Create `prompts/pdf-reference.md` (MBA PDF prompt)
- [ ] Create `prompts/generic_prompt.md` (ad-hoc content)
- [ ] Rename `final_summary_prompt.md` → `default_prompt.md`
- [ ] Remove or deprecate `chunk_summary_prompt.md`

### Phase 8: Schema YAML Updates

- [ ] Add `base-template` base type with MBA defaults
- [ ] Add `base-generic` base type with ad-hoc defaults
- [ ] Update `video-reference` with `Prompt` and `PathResolution`
- [ ] Add `generic-video` template type
- [ ] Add `generic-pdf` template type
- [ ] Update `pdf-reference` similarly

### Phase 9: Tests

- [ ] Update `MetadataSchemaLoaderTests` for new inheritance
- [ ] Update `PromptTemplateServiceTests` for new methods
- [ ] Add `PathResolverTests`
- [ ] Update `AISummarizerTests` for simplified flow
- [ ] Update CLI integration tests

### Phase 10: Documentation

**User Documentation:**

- [ ] Update `docs/cli-reference.md` - Add --template-type and --prompt options
- [ ] Update `docs/cli-cheat-sheet.md` - Add quick examples
- [ ] Update `docs/user-guide/file-processing.md` - Explain template types
- [ ] Update `docs/user-guide/output-management.md` - PathResolution explanation
- [ ] Update `docs/configuration/ai-services.md` - Prompt configuration
- [ ] Update `docs/metadata-schema-configuration.md` - Schema changes
- [ ] Update `docs/Template-Metadata-Guide.md` - Template-type profiles

**Developer Documentation:**

- [ ] Update `docs/developer-guide/ai-summary-flow.md` - New prompt flow
- [ ] Update `docs/developer-guide/contributing.md` - New patterns

---

## Schema Example

Complete example of the updated `metadata-schema.yml`:

```yaml
BaseTypes:
  base-template:
    Prompt: default_prompt
    PathResolution:
      InputRoot: onedrive
      OutputRoot: vault
    Fields:
      course_name:
        Type: string
        Description: "Name of the course"
      module_number:
        Type: number
        Description: "Module number"
      # ... other common fields

  base-generic:
    Prompt: generic_prompt
    PathResolution:
      InputRoot: cwd
      OutputRoot: input
    Fields:
      title:
        Type: string
        Description: "Content title"
      source_url:
        Type: url
        Description: "Source URL"

TemplateTypes:
  video-reference:
    Type: video-reference
    Prompt: video-reference
    BaseTypes:
      - base-template
    RequiredFields:
      - course_name
      - module_number
    Fields:
      # video-specific fields...

  pdf-reference:
    Type: pdf-reference
    Prompt: pdf-reference
    BaseTypes:
      - base-template
    RequiredFields:
      - course_name
    Fields:
      # pdf-specific fields...

  generic-video:
    Type: generic-video
    BaseTypes:
      - base-generic
    Fields:
      channel:
        Type: string
        Description: "YouTube channel name"

  generic-pdf:
    Type: generic-pdf
    BaseTypes:
      - base-generic
    Fields:
      author:
        Type: string
        Description: "Document author"
```

---

## CLI Usage Examples

```bash
# MBA course video (default behavior)
notebook-cli video-notes --url "https://youtube.com/..." --course "MBA 640" --module 5

# Ad-hoc YouTube video with generic template
notebook-cli video-notes --template-type generic-video --url "https://youtube.com/..."
# Output: ./video-title.md (next to where you run the command)

# MBA video with custom prompt
notebook-cli video-notes --prompt research-summary --url "https://youtube.com/..."

# Ad-hoc video with full path to prompt file
notebook-cli video-notes --template-type generic-video --prompt "D:\prompts\my-prompt.md" --url "..."

# PDF with generic template
notebook-cli pdf-notes --template-type generic-pdf input.pdf
# Output: ./input.md (next to input file)
```

---

## Acceptance Criteria

- [ ] Single prompt per template-type works correctly
- [ ] `--template-type` option changes processing profile
- [ ] `--prompt` option overrides template-type's prompt
- [ ] Full path prompts load correctly
- [ ] PathResolution `input` outputs next to input file
- [ ] Inheritance resolves Prompt and PathResolution correctly
- [ ] Existing MBA workflows continue to work unchanged
- [ ] Documentation is complete and accurate
- [ ] All tests pass

---

## Breaking Changes

**None expected** - All changes are additive:

- Default behavior remains unchanged (video-reference/pdf-reference with existing prompts)
- New options are optional
- Existing prompt files continue to work

---

## Related Files

**Core Changes:**

- `src/c-sharp/NotebookAutomation.Core/Schema/TemplateTypeSchema.cs`
- `src/c-sharp/NotebookAutomation.Core/Services/MetadataSchemaLoader.cs`
- `src/c-sharp/NotebookAutomation.Core/Services/PromptTemplateService.cs`
- `src/c-sharp/NotebookAutomation.Core/Services/AISummarizer.cs`
- `src/c-sharp/NotebookAutomation.CLI/Commands/VideoCommands.cs`
- `src/c-sharp/NotebookAutomation.CLI/Commands/PdfCommands.cs`

**New Files:**

- `src/c-sharp/NotebookAutomation.Core/Utils/PathResolver.cs`
- `prompts/video-reference.md`
- `prompts/pdf-reference.md`
- `prompts/generic_prompt.md`

**Configuration:**

- `config/metadata-schema.yml`

**Documentation:**

- `docs/cli-reference.md`
- `docs/cli-cheat-sheet.md`
- `docs/user-guide/file-processing.md`
- `docs/user-guide/output-management.md`
- `docs/configuration/ai-services.md`
- `docs/metadata-schema-configuration.md`
- `docs/Template-Metadata-Guide.md`
- `docs/developer-guide/ai-summary-flow.md`
- `docs/developer-guide/contributing.md`
