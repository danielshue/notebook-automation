# Dynamic Prompt System - Integration Guide

## Overview

This document provides detailed guidance for completing Phase 6 (Integration) of the Dynamic Prompt System implementation. The foundational infrastructure is complete; this guide shows how to wire CLI options through the processing pipeline.

## Current State

### ✅ Completed Infrastructure

1. **Schema & Models**
   - `TemplateTypeSchema` has `Prompt` and `PathResolution` properties
   - `PathResolutionConfig` class for input/output root configuration
   - Template type inheritance extended in `MetadataSchemaLoader`

2. **Services**
   - `PathResolver` utility class (in `Core/Utils/PathResolver.cs`)
   - `LoadPromptForTemplateTypeAsync()` method in `PromptTemplateService`
   - Resolution order: CLI override → Template type → Inherited → Default

3. **CLI**
   - `--template-type` and `--prompt` options added to `video-notes` and `pdf-notes`
   - Variables captured: `templateTypeName` and `promptOverride`

4. **Configuration**
   - Base types: `base-template` (MBA), `base-generic` (ad-hoc)
   - New template types: `generic-video`, `generic-pdf`
   - Specialized prompt files created

### 🔄 Remaining: Pipeline Integration

CLI variables are captured but not passed through the processing pipeline.

## Integration Steps

### Step 1: Extend `VideoNoteBatchProcessor.ProcessVideosAsync()`

**File:** `src/c-sharp/NotebookAutomation.Core/Tools/VideoProcessing/VideoNoteBatchProcessor.cs`

**Current signature (line 86):**
```csharp
public async Task<BatchProcessResult> ProcessVideosAsync(
    string input,
    string? output,
    List<string> videoExtensions,
    string? openAiApiKey,
    bool dryRun = false,
    bool noSummary = false,
    bool forceOverwrite = false,
    bool retryFailed = false,
    int? timeoutSeconds = null,
    string? resourcesRoot = null,
    AppConfig? appConfig = null,
    bool noShareLinks = false)
```

**Add parameters:**
```csharp
public async Task<BatchProcessResult> ProcessVideosAsync(
    string input,
    string? output,
    List<string> videoExtensions,
    string? openAiApiKey,
    bool dryRun = false,
    bool noSummary = false,
    bool forceOverwrite = false,
    bool retryFailed = false,
    int? timeoutSeconds = null,
    string? resourcesRoot = null,
    AppConfig? appConfig = null,
    bool noShareLinks = false,
    string? templateTypeName = null,      // NEW
    string? promptOverride = null)        // NEW
```

**Update call to `batchProcessor.ProcessDocumentsAsync()` (line 100):**
```csharp
return await batchProcessor.ProcessDocumentsAsync(
    input,
    output,
    videoExtensions,
    openAiApiKey,
    dryRun,
    noSummary,
    forceOverwrite,
    retryFailed,
    timeoutSeconds,
    resourcesRoot,
    appConfig,
    "Video Note",
    "failed_videos.txt",
    noShareLinks,
    templateTypeName,    // NEW
    promptOverride)      // NEW
    .ConfigureAwait(false);
```

### Step 2: Extend `PdfNoteBatchProcessor.ProcessPdfsAsync()`

**File:** `src/c-sharp/NotebookAutomation.Core/Tools/PdfProcessing/PdfNoteBatchProcessor.cs`

Apply same pattern as VideoNoteBatchProcessor:
- Add `templateTypeName` and `promptOverride` parameters
- Pass them to `batchProcessor.ProcessDocumentsAsync()`

### Step 3: Extend `DocumentNoteBatchProcessor.ProcessDocumentsAsync()`

**File:** `src/c-sharp/NotebookAutomation.Core/Tools/Shared/DocumentNoteBatchProcessor.cs`

**Current signature (line 228):**
```csharp
public virtual async Task<BatchProcessResult> ProcessDocumentsAsync(
    string input,
    string? output,
    List<string> fileExtensions,
    string? openAiApiKey,
    bool dryRun = false,
    bool noSummary = false,
    bool forceOverwrite = false,
    bool retryFailed = false,
    int? timeoutSeconds = null,
    string? resourcesRoot = null,
    AppConfig? appConfig = null,
    string noteType = "Document Note",
    string failedFilesListName = "failed_files.txt",
    bool noShareLinks = false)
```

**Add parameters:**
```csharp
public virtual async Task<BatchProcessResult> ProcessDocumentsAsync(
    string input,
    string? output,
    List<string> fileExtensions,
    string? openAiApiKey,
    bool dryRun = false,
    bool noSummary = false,
    bool forceOverwrite = false,
    bool retryFailed = false,
    int? timeoutSeconds = null,
    string? resourcesRoot = null,
    AppConfig? appConfig = null,
    string noteType = "Document Note",
    string failedFilesListName = "failed_files.txt",
    bool noShareLinks = false,
    string? templateTypeName = null,      // NEW
    string? promptOverride = null)        // NEW
```

**Pass to `ProcessFilesAsync()` (line 281):**
```csharp
var (processedCount, failedCount, failedFiles) = await ProcessFilesAsync(
    files, effectiveOutput, effectiveResourcesRoot, forceOverwrite, dryRun,
    openAiApiKey, noSummary, timeoutSeconds, noShareLinks, noteType, appConfig,
    templateTypeName, promptOverride)  // NEW
    .ConfigureAwait(false);
```

### Step 4: Extend `DocumentNoteBatchProcessor.ProcessFilesAsync()`

**File:** `src/c-sharp/NotebookAutomation.Core/Tools/Shared/DocumentNoteBatchProcessor.cs`

**Find the method (around line 1231)** and add parameters:
```csharp
protected virtual async Task<(int processed, int failed, List<string> failedFiles)> ProcessFilesAsync(
    List<string> files,
    string effectiveOutput,
    string? resourcesRoot,
    bool forceOverwrite,
    bool dryRun,
    string? openAiApiKey,
    bool noSummary,
    int? timeoutSeconds,
    bool noShareLinks,
    string noteType,
    AppConfig? appConfig,
    string? templateTypeName = null,     // NEW
    string? promptOverride = null)       // NEW
```

**Pass to `ProcessSingleFileAsync()` calls:**
```csharp
await ProcessSingleFileAsync(
    filePath, queueItem, i + 1, files.Count, effectiveOutput,
    resourcesRoot, forceOverwrite, dryRun, openAiApiKey, noSummary,
    timeoutSeconds, noShareLinks, noteType, appConfig,
    templateTypeName, promptOverride)  // NEW
    .ConfigureAwait(false);
```

### Step 5: Extend `DocumentNoteBatchProcessor.ProcessSingleFileAsync()`

**File:** `src/c-sharp/NotebookAutomation.Core/Tools/Shared/DocumentNoteBatchProcessor.cs`

**Find the method** and add parameters:
```csharp
protected virtual async Task ProcessSingleFileAsync(
    string filePath,
    QueueItem? queueItem,
    int fileIndex,
    int totalFiles,
    string effectiveOutput,
    string? resourcesRoot,
    bool forceOverwrite,
    bool dryRun,
    string? openAiApiKey,
    bool noSummary,
    int? timeoutSeconds,
    bool noShareLinks,
    string noteType,
    AppConfig? appConfig,
    string? templateTypeName = null,     // NEW
    string? promptOverride = null)       // NEW
```

**Resolve template type schema:**
```csharp
// Resolve template type schema if specified
Tools.TemplateTypeSchema? resolvedTemplateType = null;
if (!string.IsNullOrEmpty(templateTypeName) && appConfig != null)
{
    var schemaLoader = new MetadataSchemaLoader(logger);
    var schemaPath = appConfig.Paths?.MetadataSchemaFile;
    if (!string.IsNullOrEmpty(schemaPath) && File.Exists(schemaPath))
    {
        var schema = await schemaLoader.LoadSchemaFromFileAsync(schemaPath);
        resolvedTemplateType = schemaLoader.ResolveTemplateType(templateTypeName, schema);
    }
}
```

**Pass to processor methods** (where processor.ProcessFileAsync is called):
```csharp
// Pass template type and prompt override to processor
var result = await processor.ProcessFileAsync(
    filePath,
    effectiveOutput,
    resourcesRoot,
    forceOverwrite,
    noSummary,
    openAiApiKey,
    timeoutSeconds,
    noShareLinks,
    resolvedTemplateType,    // NEW
    promptOverride)          // NEW
    .ConfigureAwait(false);
```

### Step 6: Update `VideoNoteProcessor.ProcessFileAsync()`

**File:** `src/c-sharp/NotebookAutomation.Core/Tools/VideoProcessing/VideoNoteProcessor.cs`

**Find ProcessFileAsync signature** and add parameters:
```csharp
public async Task<ProcessingResult> ProcessFileAsync(
    string filePath,
    string? outputDirectory,
    string? resourcesRoot,
    bool forceOverwrite,
    bool noSummary,
    string? openAiApiKey,
    int? timeoutSeconds,
    bool noShareLinks,
    Tools.TemplateTypeSchema? templateType = null,    // NEW
    string? promptOverride = null)                    // NEW
```

**Use LoadPromptForTemplateTypeAsync:**
```csharp
// Load prompt using template type resolution
var promptService = GetPromptService(); // or inject via constructor
string promptTemplate = await promptService.LoadPromptForTemplateTypeAsync(
    promptOverride,
    templateType,
    cancellationToken)
    .ConfigureAwait(false);
```

**Use PathResolver for output directory:**
```csharp
// Resolve output path using PathResolver if template type has PathResolution
if (templateType?.PathResolution != null)
{
    var pathResolver = new Utils.PathResolver(appConfig);
    string outputRoot = pathResolver.ResolveOutputRoot(
        templateType.PathResolution,
        filePath);
    
    // Use resolved output root if different from provided
    if (!string.IsNullOrEmpty(outputRoot))
    {
        outputDirectory = outputRoot;
    }
}
```

### Step 7: Update `PdfNoteProcessor.ProcessFileAsync()`

**File:** `src/c-sharp/NotebookAutomation.Core/Tools/PdfProcessing/PdfNoteProcessor.cs`

Apply same pattern as VideoNoteProcessor:
- Add `templateType` and `promptOverride` parameters
- Use `LoadPromptForTemplateTypeAsync()`
- Use `PathResolver` for output directory

### Step 8: Update VideoCommands Call

**File:** `src/c-sharp/NotebookAutomation.Cli/Commands/VideoCommands.cs`

**Update the call to `batchProcessor.ProcessVideosAsync()` (around line 603):**
```csharp
return await batchProcessor.ProcessVideosAsync(
    resolvedInput,
    effectiveOutputDir,
    videoExtensions,
    openAiApiKey,
    dryRun,
    noSummary,
    force,
    retryFailed,
    timeout,
    localResourcesPathForBatchProcessor,
    appConfig,
    noShareLinks,
    templateTypeName,    // NEW - add this parameter
    promptOverride)      // NEW - add this parameter
    .ConfigureAwait(false);
```

### Step 9: Update PdfCommands Call

**File:** `src/c-sharp/NotebookAutomation.Cli/Commands/PdfCommands.cs`

Apply same pattern as VideoCommands - pass `templateTypeName` and `promptOverride` to the batch processor.

## Testing Strategy

### Unit Tests

Create tests for:
1. `PathResolver.ResolveInputRoot()` - test each root type
2. `PathResolver.ResolveOutputRoot()` - test including "input" special case
3. `PromptTemplateService.LoadPromptForTemplateTypeAsync()` - test resolution order
4. Template type inheritance with Prompt and PathResolution

### Integration Tests

Test complete flows:
1. Default MBA workflow (should work unchanged)
2. Generic video with `--template-type generic-video`
3. Custom prompt with `--prompt custom-prompt`
4. Full path prompt with `--prompt "D:\prompts\custom.md"`

### Manual Testing

```bash
# Test 1: Default behavior (unchanged)
na video-notes --url "https://..." --course "MBA 640"

# Test 2: Generic template
na video-notes --template-type generic-video --url "https://..."

# Test 3: Custom prompt name
na video-notes --prompt research-summary --url "https://..."

# Test 4: Custom prompt path
na video-notes --prompt "D:\prompts\custom.md" --url "https://..."

# Test 5: Combined options
na video-notes --template-type generic-video --prompt custom --url "https://..."
```

## Notes

- All changes maintain backward compatibility
- Default behavior uses existing prompts and vault paths
- PathResolver uses `GetEffectiveOneDriveRoot()` and `GetEffectiveVaultRoot()` from PathsConfig
- Template type resolution uses existing `MetadataSchemaLoader.ResolveTemplateType()`
- Prompt resolution order ensures CLI has highest priority

## Related Files

**Modified:**
- VideoCommands.cs
- PdfCommands.cs
- VideoNoteBatchProcessor.cs
- PdfNoteBatchProcessor.cs
- DocumentNoteBatchProcessor.cs (ProcessDocumentsAsync, ProcessFilesAsync, ProcessSingleFileAsync)
- VideoNoteProcessor.cs
- PdfNoteProcessor.cs

**New:**
- PathResolver.cs (already created)
- LoadPromptForTemplateTypeAsync() in PromptTemplateService (already created)
- Prompt files (already created)
- Updated metadata-schema.yml (already updated)

## Completion Checklist

- [ ] Step 1: VideoNoteBatchProcessor.ProcessVideosAsync()
- [ ] Step 2: PdfNoteBatchProcessor.ProcessPdfsAsync()
- [ ] Step 3: DocumentNoteBatchProcessor.ProcessDocumentsAsync()
- [ ] Step 4: DocumentNoteBatchProcessor.ProcessFilesAsync()
- [ ] Step 5: DocumentNoteBatchProcessor.ProcessSingleFileAsync()
- [ ] Step 6: VideoNoteProcessor.ProcessFileAsync()
- [ ] Step 7: PdfNoteProcessor.ProcessFileAsync()
- [ ] Step 8: Update VideoCommands call
- [ ] Step 9: Update PdfCommands call
- [ ] Unit tests for PathResolver
- [ ] Unit tests for LoadPromptForTemplateTypeAsync
- [ ] Integration tests
- [ ] Manual testing
- [ ] Documentation updates
