# Notebook Automation + GitHub Copilot SDK — Technical Implementation Plan

**Status:** Draft  
**Created:** 2026-01-24  
**Branch:** GHCP-SDK  
**Related:** [Feature Specification](./copilot-sdk-integration.md)

---

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Architecture](#architecture)
  - [High-Level Architecture](#high-level-architecture)
  - [Component Diagram](#component-diagram)
  - [Data Flow](#data-flow)
- [Implementation Phases](#implementation-phases)
  - [Phase 1: Foundation](#phase-1-foundation)
  - [Phase 2: Chat Mode Core](#phase-2-chat-mode-core)
  - [Phase 3: CLI Command Integration](#phase-3-cli-command-integration)
  - [Phase 4: Session Management](#phase-4-session-management)
  - [Phase 5: Advanced Features](#phase-5-advanced-features)
- [Technical Details](#technical-details)
  - [Project Structure](#project-structure)
  - [Key Interfaces](#key-interfaces)
  - [Custom Tools Registration](#custom-tools-registration)
  - [Event Handling](#event-handling)
- [Detailed Implementation Specifications](#detailed-implementation-specifications)
  - [Phase 1 Detailed Specifications](#phase-1-foundation--detailed-specifications)
  - [Phase 2 Detailed Specifications](#phase-2-chat-mode-core--detailed-specifications)
  - [Phase 3 Detailed Specifications](#phase-3-cli-command-integration--detailed-specifications)
  - [Phase 4 Detailed Specifications](#phase-4-session-management--detailed-specifications)
  - [Phase 5 Detailed Specifications](#phase-5-advanced-features--detailed-specifications)
- [Acceptance Criteria Summary](#acceptance-criteria-summary)
- [Testing Strategy](#testing-strategy)
- [Migration & Compatibility](#migration--compatibility)
- [Risks & Mitigations](#risks--mitigations)
- [Timeline Estimate](#timeline-estimate)
- [GitHub Issues](#github-issues)

---

## Overview

This document outlines the technical implementation plan for integrating the GitHub Copilot SDK into the Notebook Automation CLI. The implementation will be done in phases, with each phase delivering incremental value while maintaining backward compatibility.

### Goals

1. Add Copilot SDK as a NuGet dependency
2. Create abstraction layer for testability
3. Implement chat mode as default when no args provided
4. Expose all CLI commands as Copilot-callable tools
5. Add session persistence and first-run experience

---

## Prerequisites

### Required Dependencies

| Dependency                | Version  | Purpose                                  |
| ------------------------- | -------- | ---------------------------------------- |
| `GitHub.Copilot.SDK`      | Latest   | Core SDK for Copilot integration         |
| `Microsoft.Extensions.AI` | Latest   | AI function factory for tool definitions |
| `Spectre.Console`         | Existing | Enhanced terminal UI for chat mode       |

### Environment Requirements

- GitHub Copilot CLI installed and in PATH
- GitHub Copilot subscription (free tier or paid)
- .NET 8.0 or later

### Installation Commands

```bash
# Add Copilot SDK
dotnet add package GitHub.Copilot.SDK

# Add Microsoft.Extensions.AI for tool definitions
dotnet add package Microsoft.Extensions.AI
```

---

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Program.cs                                     │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │ if (args.Length == 0 && CopilotAvailable)                       │    │
│  │     → CopilotChatService.StartInteractiveAsync()                │    │
│  │ else                                                             │    │
│  │     → Traditional CLI routing                                    │    │
│  └─────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    ▼                               ▼
┌─────────────────────────────────┐   ┌─────────────────────────────────┐
│      ICopilotService            │   │    Existing CLI Commands        │
│  (Abstraction Layer)            │   │    (Unchanged)                  │
├─────────────────────────────────┤   ├─────────────────────────────────┤
│ + StartAsync()                  │   │ TagCommands                     │
│ + StopAsync()                   │   │ VaultCommands                   │
│ + CreateSessionAsync()          │   │ PdfCommands                     │
│ + SendMessageAsync()            │   │ VideoCommands                   │
│ + RegisterToolsAsync()          │   │ MarkdownCommands                │
│ + IsAvailableAsync()            │   │ ConfigCommands                  │
└─────────────────────────────────┘   └─────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        CopilotService                                    │
│  (Implementation wrapping GitHub.Copilot.SDK)                           │
├─────────────────────────────────────────────────────────────────────────┤
│ - CopilotClient _client                                                 │
│ - CopilotSession _session                                               │
│ - NotebookTools _tools                                                  │
└─────────────────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    GitHub.Copilot.SDK                                    │
│  (NuGet Package)                                                        │
├─────────────────────────────────────────────────────────────────────────┤
│ CopilotClient → CopilotSession → Copilot CLI (JSON-RPC)                │
└─────────────────────────────────────────────────────────────────────────┘
```

### Component Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        NotebookAutomation.Cli                            │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐          │
│  │   Program.cs    │  │ CopilotCommands │  │  ChatModeUI     │          │
│  │   (Entry Point) │  │ (na chat, ask)  │  │  (Spectre)      │          │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘          │
│           │                    │                    │                    │
│           └────────────────────┼────────────────────┘                    │
│                                │                                         │
│                    ┌───────────▼───────────┐                            │
│                    │   ICopilotService     │                            │
│                    │   (Interface)         │                            │
│                    └───────────┬───────────┘                            │
│                                │                                         │
│           ┌────────────────────┼────────────────────┐                   │
│           ▼                    ▼                    ▼                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐         │
│  │ CopilotService  │  │ NotebookTools   │  │ SessionManager  │         │
│  │ (SDK Wrapper)   │  │ (Custom Tools)  │  │ (Persistence)   │         │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘         │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                      NotebookAutomation.Core                             │
├──────────────────────────────────────────────────────────────────────────┤
│  Existing services (TagService, VaultService, PdfService, etc.)         │
└──────────────────────────────────────────────────────────────────────────┘
```

### Data Flow

```
User Input                Processing                      Output
─────────────────────────────────────────────────────────────────────

"Convert my PDFs"    ──►  CopilotService.SendAsync()
                                   │
                                   ▼
                          Copilot SDK Session
                                   │
                                   ▼
                          Tool Call: convert_pdfs
                                   │
                                   ▼
                          NotebookTools.ConvertPdfsAsync()
                                   │
                                   ▼
                          PdfCommands (existing)
                                   │
                                   ▼
                          Tool Result returned
                                   │
                                   ▼
                          Copilot generates response  ──►  "✓ Converted 5 PDFs"
```

---

## Implementation Phases

### Phase 1: Foundation ✅ COMPLETE

**Goal:** Add SDK dependency, create abstraction layer, detect Copilot availability

**Duration:** 1-2 days

#### Tasks

- [x] **1.1** Add NuGet packages (`Microsoft.Extensions.AI`, `Microsoft.SemanticKernel`)
- [x] **1.2** Create `ICopilotService` interface
- [x] **1.3** Create `CopilotService` implementation
- [x] **1.4** Create `CopilotAvailabilityChecker` utility
- [x] **1.5** Register services in DI container — `CopilotServiceRegistration`
- [x] **1.6** Add Copilot configuration section to AppConfig — `CopilotConfig`
- [x] **1.7** Write unit tests for availability checker — `CopilotAvailabilityCheckerTests`

#### Deliverables

- ✅ ICopilotService interface
- ✅ CopilotService implementation with live AI (Azure OpenAI/OpenAI via Semantic Kernel)
- ✅ Availability detection (API key based)
- ✅ Unit tests

---

### Phase 2: Chat Mode Core ✅ COMPLETE

**Goal:** Implement interactive chat mode when no args provided

**Duration:** 2-3 days

#### Tasks

- [x] **2.1** Create `ChatModeUI` class using Spectre.Console
- [x] **2.2** Implement welcome banner with ASCII art — `WelcomeBanner`
- [x] **2.3** Create chat input loop with readline support — Integrated in `ChatModeUI`
- [x] **2.4** Implement streaming response display — Integrated in `ChatModeUI`
- [x] **2.5** Modify `Program.cs` to detect no-args and enter chat mode
- [x] **2.6** Implement built-in commands (help, exit, clear, history) — `ChatBuiltInCommands`
- [x] **2.7** Add `CopilotCommands` for explicit `na chat` and `na ask`
- [x] **2.8** Write unit tests for chat components — `CopilotServiceTests`

#### Deliverables

- ✅ Interactive chat mode with streaming responses
- ✅ Welcome banner with high contrast support
- ✅ Streaming responses via Microsoft.Extensions.AI
- ✅ `na chat` and `na ask` commands with full options
- ✅ Built-in commands (help, exit, clear, history, tools, status)

---

### Phase 3: CLI Command Integration ✅ COMPLETE

**Goal:** Expose all existing CLI commands as Copilot-callable tools

**Duration:** 3-4 days

#### Tasks

- [x] **3.1** Create `NotebookTools` class for tool registration — `INotebookTools`, `NotebookTools`
- [x] **3.2** Implement Vault tools (4 tools): `vault_generate_index`, `vault_ensure_metadata`, `vault_clean_index`, `vault_sync`
- [x] **3.3** Implement Tag tools (7 tools): `tag_add_nested`, `tag_consolidate`, `tag_restructure`, `tag_update_frontmatter`, `tag_diagnose_yaml`, `tag_metadata_check`, `tag_clean_index`
- [x] **3.4** Implement PDF tools (1 tool): `pdf_convert`
- [x] **3.5** Implement Video tools (2 tools): `video_create_notes`, `video_consolidate_transcripts`
- [x] **3.6** Implement Markdown tools (1 tool): `markdown_generate`
- [x] **3.7** Implement Config tools (5 tools): `config_view`, `config_update`, `config_validate`, `config_list_keys`, `config_secrets_status`
- [x] **3.8** Implement OneDrive tools (1 tool): `onedrive_refresh_token`
- [x] **3.9** Create system message with tool context — `ISystemMessageBuilder`, `SystemMessageBuilder`
- [x] **3.10** Write tests for each tool — `NotebookToolsTests`, `SystemMessageBuilderTests`

#### Deliverables

- ✅ 21 CLI commands available as AI tools across 7 categories
- ✅ System message builder with dynamic tool context
- ✅ Tool execution with proper error handling
- ✅ Unit tests for tools and system message builder

---

### Phase 4: Session Management ✅ COMPLETE

**Goal:** Implement session persistence, first-run experience, and Git detection

**Duration:** 2-3 days

#### Tasks

- [x] **4.1** Create `SessionManager` class — `ISessionManager`, `SessionManager`
- [x] **4.2** Implement session save/load functionality (`SaveSessionAsync`, `LoadSessionAsync`, `ListSessionsAsync`, `DeleteSessionAsync`, `PurgeOldSessionsAsync`)
- [x] **4.3** Create `FirstRunExperience` class
- [x] **4.4** Implement Git repository detection — `IGitService`, `GitService`
- [x] **4.5** Implement session retention preference prompt — Integrated in `FirstRunExperience`
- [x] **4.6** Add `--resume` and `--session` options to `na chat` — `CopilotCommands`
- [x] **4.7** Implement session purge command — Integrated in `ChatBuiltInCommands`
- [x] **4.8** Store first-run preferences in user settings — `IUserPreferencesService`, `UserPreferencesService`
- [x] **4.9** Write tests for session management — `SessionManagerTests`

#### Deliverables

- ✅ Session save/load/list/delete/purge with file-based persistence
- ✅ First-run experience with Git detection and retention preferences
- ✅ `--resume` and `--session` options for `na chat`
- ✅ User preferences storage (~/.notebookautomation/preferences.json)
- ✅ Session storage (~/.notebookautomation/sessions/)

---

### Phase 5: Advanced Features ✅ MOSTLY COMPLETE

**Goal:** Add accessibility, internationalization, logging, and polish

**Duration:** 2-3 days

#### Tasks

- [x] **5.1** Implement keyboard shortcuts handler — `KeyboardHandler`
- [x] **5.2** Implement multi-line input support — `MultiLineInputHandler`
- [x] **5.3** Implement file attachment syntax (@file) — `FileAttachmentParser`
- [x] **5.4** Add accessibility options (high contrast, screen reader) — `AccessibilityOptions`
- [x] **5.5** Implement session logging — `SessionLogger`
- [x] **5.6** Add offline mode detection and handling — `NetworkHandler`
- [x] **5.7** Implement rate limiting handler — `RateLimitHandler`
- [x] **5.8** Add `na chat --help` output — Integrated in `CopilotCommands`
- [ ] **5.9** Final integration testing — `CopilotIntegrationTests` (NOT IMPLEMENTED)
- [x] **5.10** Update documentation

#### Deliverables

- ✅ Keyboard shortcuts
- ✅ Multi-line input
- ✅ File attachments
- ✅ Accessibility features
- ✅ Session logging
- ✅ Network handling
- ✅ Documentation updated
- ⬜ Integration tests (remaining)

---

## Technical Details

### Project Structure

```
src/c-sharp/NotebookAutomation.Cli/
├── Commands/
│   ├── CopilotCommands.cs          # na chat, na ask commands
│   └── ... (existing)
├── Services/
│   └── Copilot/
│       ├── ICopilotService.cs       # Main interface
│       ├── CopilotService.cs        # SDK wrapper implementation
│       ├── CopilotAvailabilityChecker.cs
│       ├── NotebookTools.cs         # Custom tools for Copilot
│       ├── SessionManager.cs        # Session persistence
│       ├── SystemMessageBuilder.cs  # System prompt builder
│       ├── ChatBuiltInCommands.cs   # help, exit, clear, etc.
│       ├── FileAttachmentParser.cs  # @file syntax handler
│       ├── NetworkHandler.cs        # Offline/rate limit handling
│       ├── RateLimitHandler.cs
│       ├── SessionLogger.cs
│       └── UserPreferences.cs
├── UI/
│   ├── ChatModeUI.cs               # Main chat UI orchestration
│   ├── WelcomeBanner.cs            # ASCII art banner
│   ├── ChatInputHandler.cs         # Input with readline
│   ├── StreamingResponseRenderer.cs # Streaming display
│   ├── FirstRunExperience.cs       # First-run prompts
│   ├── KeyboardHandler.cs          # Shortcuts
│   ├── MultiLineInputHandler.cs
│   └── AccessibilityOptions.cs
└── Configuration/
    └── CopilotConfig.cs            # Copilot-specific config
```

### Key Interfaces

```csharp
namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Abstraction for Copilot SDK operations.
/// </summary>
public interface ICopilotService : IAsyncDisposable
{
    /// <summary>
    /// Check if Copilot CLI is available and authenticated.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Start the Copilot client.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the Copilot client.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new conversation session.
    /// </summary>
    Task<ICopilotSession> CreateSessionAsync(
        SessionConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resume an existing session.
    /// </summary>
    Task<ICopilotSession> ResumeSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Start interactive chat mode.
    /// </summary>
    Task<int> StartInteractiveChatAsync(
        ChatModeOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a one-shot message and return the response.
    /// </summary>
    Task<string> AskAsync(
        string prompt,
        AskOptions? options = null,
        CancellationToken cancellationToken = default);
}

public interface ICopilotSession : IAsyncDisposable
{
    string SessionId { get; }

    Task<string> SendAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    Task<string> SendWithAttachmentsAsync(
        string prompt,
        IEnumerable<FileAttachment> attachments,
        CancellationToken cancellationToken = default);

    IDisposable OnEvent(Action<SessionEvent> handler);

    Task AbortAsync(CancellationToken cancellationToken = default);
}
```

### Custom Tools Registration

```csharp
namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Registers Notebook Automation commands as Copilot-callable tools.
/// </summary>
public class NotebookTools(
    IServiceProvider serviceProvider,
    ILogger<NotebookTools> logger)
{
    public IReadOnlyList<AIFunction> GetTools()
    {
        return
        [
            // Vault tools
            AIFunctionFactory.Create(
                async ([Description("Path within the vault")] string? path) =>
                {
                    var vaultCommands = serviceProvider.GetRequiredService<VaultCommands>();
                    return await vaultCommands.GenerateIndexAsync(path ?? "/");
                },
                "vault_generate_index",
                "Generate index files for directories in the vault"),

            AIFunctionFactory.Create(
                async ([Description("Path within the vault")] string? path) =>
                {
                    var vaultCommands = serviceProvider.GetRequiredService<VaultCommands>();
                    return await vaultCommands.EnsureMetadataAsync(path ?? "/");
                },
                "vault_ensure_metadata",
                "Ensure metadata consistency across markdown files"),

            // Tag tools
            AIFunctionFactory.Create(
                async (
                    [Description("Path to file or folder")] string path,
                    [Description("Frontmatter key to update")] string key,
                    [Description("New value for the key")] string value) =>
                {
                    var tagCommands = serviceProvider.GetRequiredService<TagCommands>();
                    return await tagCommands.UpdateFrontmatterAsync(path, key, value);
                },
                "tag_update_frontmatter",
                "Update or add a frontmatter key-value pair in markdown files"),

            // PDF tools
            AIFunctionFactory.Create(
                async (
                    [Description("Path to PDF file or folder")] string path,
                    [Description("Output directory for notes")] string? output,
                    [Description("Extract images from PDFs")] bool extractImages = false) =>
                {
                    var pdfCommands = serviceProvider.GetRequiredService<PdfCommands>();
                    return await pdfCommands.ConvertAsync(path, output, extractImages);
                },
                "pdf_convert",
                "Convert PDF files to markdown notes"),

            // ... additional tools
        ];
    }
}
```

### Event Handling

```csharp
public class CopilotService : ICopilotService
{
    private readonly CopilotClient _client;
    private CopilotSession? _session;

    public async Task<int> StartInteractiveChatAsync(
        ChatModeOptions? options,
        CancellationToken cancellationToken)
    {
        await using var session = await CreateSessionAsync(new SessionConfig
        {
            Streaming = true,
            Tools = _notebookTools.GetTools(),
            SystemMessage = _systemMessageBuilder.Build()
        }, cancellationToken);

        var done = new TaskCompletionSource();

        session.OnEvent(evt =>
        {
            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    _ui.WriteStreaming(delta.Data.DeltaContent);
                    break;

                case AssistantMessageEvent msg:
                    _ui.WriteComplete(msg.Data.Content);
                    break;

                case ToolExecutionStartEvent toolStart:
                    _ui.WriteToolStart(toolStart.Data.ToolName);
                    break;

                case ToolExecutionCompleteEvent toolComplete:
                    _ui.WriteToolComplete(toolComplete.Data.Result);
                    break;

                case SessionIdleEvent:
                    done.SetResult();
                    break;

                case SessionErrorEvent error:
                    _ui.WriteError(error.Data.Message);
                    done.SetException(new CopilotException(error.Data.Message));
                    break;
            }
        });

        // Chat loop
        while (!cancellationToken.IsCancellationRequested)
        {
            var input = await _ui.ReadInputAsync(cancellationToken);

            if (_builtInCommands.TryHandle(input, out var result))
            {
                if (result == BuiltInCommandResult.Exit)
                    break;
                continue;
            }

            done = new TaskCompletionSource();
            await session.SendAsync(input, cancellationToken);
            await done.Task;
        }

        return 0;
    }
}
```

---

## Testing Strategy

### Unit Tests

| Component                    | Test Coverage              |
| ---------------------------- | -------------------------- |
| `CopilotAvailabilityChecker` | CLI detection, auth status |
| `NotebookTools`              | Each tool function         |
| `SessionManager`             | Save, load, list, purge    |
| `ChatBuiltInCommands`        | Command parsing, execution |
| `FileAttachmentParser`       | @file syntax parsing       |
| `SystemMessageBuilder`       | Message construction       |

### Integration Tests

| Scenario            | Description                       |
| ------------------- | --------------------------------- |
| Chat mode entry     | No args → chat mode activation    |
| Tool invocation     | Natural language → tool execution |
| Session persistence | Save → exit → resume              |
| Error handling      | Network failure, rate limiting    |

### Mock Strategy

```csharp
public class MockCopilotService : ICopilotService
{
    public Queue<string> QueuedResponses { get; } = new();
    public List<string> ReceivedPrompts { get; } = new();

    public Task<string> AskAsync(string prompt, AskOptions? options, CancellationToken ct)
    {
        ReceivedPrompts.Add(prompt);
        return Task.FromResult(QueuedResponses.Dequeue());
    }
}
```

---

## Migration & Compatibility

### Backward Compatibility

- All existing CLI commands continue to work unchanged
- `na --help` still shows traditional help
- Configuration file format extended (not changed)
- No breaking changes to existing workflows

### Graceful Degradation

```csharp
// In Program.cs
if (args.Length == 0)
{
    var copilot = serviceProvider.GetService<ICopilotService>();

    if (copilot != null && await copilot.IsAvailableAsync())
    {
        return await copilot.StartInteractiveChatAsync();
    }

    // Fallback: show traditional help
    var helpService = serviceProvider.GetRequiredService<HelpDisplayService>();
    await helpService.DisplayCustomHelpAsync(rootCommand, configPath, isDebugMode, args);
    return 0;
}
```

---

## Risks & Mitigations

| Risk                      | Likelihood | Impact | Mitigation                                 |
| ------------------------- | ---------- | ------ | ------------------------------------------ |
| Copilot CLI not installed | Medium     | High   | Graceful fallback to traditional CLI       |
| Rate limiting             | Medium     | Medium | Implement backoff, show quota warnings     |
| SDK breaking changes      | Low        | High   | Pin SDK version, abstract behind interface |
| Network failures          | Medium     | Medium | Offline mode, retry logic                  |
| Large file handling       | Low        | Medium | Chunking, progress indicators              |

---

## Timeline Estimate

| Phase                       | Duration       | Dependencies |
| --------------------------- | -------------- | ------------ |
| Phase 1: Foundation         | 1-2 days       | None         |
| Phase 2: Chat Mode Core     | 2-3 days       | Phase 1      |
| Phase 3: CLI Integration    | 3-4 days       | Phase 2      |
| Phase 4: Session Management | 2-3 days       | Phase 3      |
| Phase 5: Advanced Features  | 2-3 days       | Phase 4      |
| **Total**                   | **10-15 days** |              |

---

## GitHub Issues

The following GitHub issues should be created to track implementation:

### Epic Issue

```markdown
# [Epic] GitHub Copilot SDK Integration

## Overview

Integrate GitHub Copilot SDK to provide intelligent chat mode for Notebook Automation CLI.

## Feature Specification

See: docs/features/copilot-sdk-integration.md

## Implementation Plan

See: docs/features/copilot-sdk-implementation-plan.md

## Child Issues

- [ ] #XXX Phase 1: Foundation
- [ ] #XXX Phase 2: Chat Mode Core
- [ ] #XXX Phase 3: CLI Command Integration
- [ ] #XXX Phase 4: Session Management
- [ ] #XXX Phase 5: Advanced Features

## Labels

- enhancement
- copilot-sdk
- epic
```

### Phase Issues (Template)

```markdown
# [Phase X] <Phase Name>

## Goal

<Goal from implementation plan>

## Tasks

- [ ] Task 1
- [ ] Task 2
- ...

## Acceptance Criteria

- [ ] Criteria 1
- [ ] Criteria 2

## Dependencies

- Depends on: #XXX

## Labels

- enhancement
- copilot-sdk
- phase-X
```

---

## Implementation Reference

All detailed implementation specifications have been completed. Refer to the actual source code for implementation details:

### Core Services (Services/Copilot/)

| Interface                 | Implementation           | Description                           |
| ------------------------- | ------------------------ | ------------------------------------- |
| `ICopilotService`         | `CopilotService`         | Main Copilot operations abstraction   |
| `ICopilotSession`         | `CopilotSession`         | Conversation session management       |
| `ISessionManager`         | `SessionManager`         | Session persistence (save/load/purge) |
| `IUserPreferencesService` | `UserPreferencesService` | User preferences storage              |
| `ISystemMessageBuilder`   | `SystemMessageBuilder`   | System message construction           |
| `IGitService`             | `GitService`             | Git repository detection              |
| `INotebookTools`          | `NotebookTools`          | CLI commands as AI tools (21 tools)   |

### Supporting Types (Services/Copilot/CopilotTypes.cs)

- `CopilotAvailabilityResult` - Availability check result
- `CopilotStartupOptions` - Client startup options
- `CopilotSessionConfig` - Session configuration
- `CopilotSessionMetadata` - Session metadata
- `ChatModeOptions` - Chat mode options
- `AskOptions` - One-shot ask options
- `CopilotResponse` - Response container
- `FileAttachment` - File attachment record
- Session events: `AssistantMessageDeltaEvent`, `AssistantMessageCompleteEvent`, `ToolExecutionStartEvent`, `ToolExecutionCompleteEvent`, `SessionIdleEvent`, `SessionErrorEvent`, `UserMessageEvent`

### UI Components (UI/)

| Class                   | Description                          |
| ----------------------- | ------------------------------------ |
| `ChatModeUI`            | Main chat UI orchestration           |
| `WelcomeBanner`         | ASCII art welcome banner             |
| `FirstRunExperience`    | First-run setup flow                 |
| `KeyboardHandler`       | Keyboard shortcuts                   |
| `MultiLineInputHandler` | Multi-line input support             |
| `AccessibilityOptions`  | High contrast, screen reader support |

### Additional Services

| Class                        | Description                                      |
| ---------------------------- | ------------------------------------------------ |
| `CopilotAvailabilityChecker` | CLI/API key detection                            |
| `CopilotInstallationGuide`   | Installation guidance                            |
| `ChatBuiltInCommands`        | Built-in chat commands (help, exit, clear, etc.) |
| `FileAttachmentParser`       | @file syntax parsing                             |
| `NetworkHandler`             | Offline mode detection                           |
| `RateLimitHandler`           | Rate limiting handling                           |
| `SessionLogger`              | Session interaction logging                      |

### Configuration

| Class                        | Location                                                       |
| ---------------------------- | -------------------------------------------------------------- |
| `CopilotConfig`              | `NotebookAutomation.Core/Configuration/CopilotConfig.cs`       |
| `CopilotServiceRegistration` | `NotebookAutomation.Cli/Startup/CopilotServiceRegistration.cs` |

### Commands

| Class             | Description                         |
| ----------------- | ----------------------------------- |
| `CopilotCommands` | `na chat` and `na ask` CLI commands |

### Tests

| Test Class                        | Coverage                    |
| --------------------------------- | --------------------------- |
| `CopilotAvailabilityCheckerTests` | Availability detection      |
| `CopilotServiceTests`             | Core service functionality  |
| `NotebookToolsTests`              | Tool registrations          |
| `SystemMessageBuilderTests`       | System message construction |
| `SessionManagerTests`             | Session persistence         |

---

_Document maintained in: `docs/features/copilot-sdk-implementation-plan.md`_
