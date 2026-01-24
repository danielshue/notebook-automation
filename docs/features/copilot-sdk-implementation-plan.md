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

- [x] **1.1** Add NuGet packages

  ```bash
  dotnet add package Microsoft.Extensions.AI
  dotnet add package Microsoft.SemanticKernel
  ```

- [x] **1.2** Create `ICopilotService` interface

  ```
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/ICopilotService.cs
  ```

- [x] **1.3** Create `CopilotService` implementation

  ```
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/CopilotService.cs
  ```

- [x] **1.4** Create `CopilotAvailabilityChecker` utility

  ```
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/CopilotAvailabilityChecker.cs
  ```

- [x] **1.5** Register services in DI container

  ```
  src/c-sharp/NotebookAutomation.Cli/Startup/CopilotServiceRegistration.cs
  ```

- [x] **1.6** Add Copilot configuration section to AppConfig

  ```
  src/c-sharp/NotebookAutomation.Core/Configuration/CopilotConfig.cs
  ```

- [x] **1.7** Write unit tests for availability checker
  ```
  src/c-sharp/NotebookAutomation.Tests/Cli/Services/Copilot/CopilotAvailabilityCheckerTests.cs
  ```

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

  ```
  src/c-sharp/NotebookAutomation.Cli/UI/ChatModeUI.cs
  ```

- [x] **2.2** Implement welcome banner with ASCII art

  ```
  src/c-sharp/NotebookAutomation.Cli/UI/WelcomeBanner.cs
  ```

- [x] **2.3** Create chat input loop with readline support

  ```
  Integrated in ChatModeUI.cs (RunChatLoopAsync method)
  ```

- [x] **2.4** Implement streaming response display

  ```
  Integrated in ChatModeUI.cs (DisplayStreamingResponseAsync method)
  ```

- [x] **2.5** Modify `Program.cs` to detect no-args and enter chat mode

  ```csharp
  if (isNoArgs && appConfig.Copilot.Enabled && appConfig.Copilot.AutoChatMode)
  {
      var copilotService = serviceProvider.GetService<ICopilotService>();
      var availability = await copilotService.CheckAvailabilityAsync();
      if (availability.IsAvailable)
      {
          return await chatUI.RunAsync(chatOptions);
      }
      // Fallback to help display
  }
  ```

- [x] **2.6** Implement built-in commands (help, exit, clear, history)

  ```
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/ChatBuiltInCommands.cs
  ```

- [x] **2.7** Add `CopilotCommands` for explicit `na chat` and `na ask`

  ```
  src/c-sharp/NotebookAutomation.Cli/Commands/CopilotCommands.cs
  ```

- [x] **2.8** Write unit tests for chat components
  ```
  src/c-sharp/NotebookAutomation.Tests/Cli/Services/Copilot/CopilotServiceTests.cs
  ```

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

- [x] **3.1** Create `NotebookTools` class for tool registration

  ```
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/NotebookTools.cs
  ```

- [x] **3.2** Implement Vault tools (4 tools)

  ```csharp
  AIFunctionFactory.Create(..., "vault_generate_index", "Generate index files");
  AIFunctionFactory.Create(..., "vault_ensure_metadata", "Ensure metadata consistency");
  AIFunctionFactory.Create(..., "vault_clean_index", "Remove index files");
  AIFunctionFactory.Create(..., "vault_sync", "Sync with OneDrive");
  ```

- [x] **3.3** Implement Tag tools (7 tools)

  ```csharp
  AIFunctionFactory.Create(..., "tag_add_nested", "Add nested tags");
  AIFunctionFactory.Create(..., "tag_consolidate", "Consolidate tags");
  AIFunctionFactory.Create(..., "tag_restructure", "Restructure tags");
  AIFunctionFactory.Create(..., "tag_update_frontmatter", "Update frontmatter");
  AIFunctionFactory.Create(..., "tag_diagnose_yaml", "Diagnose YAML issues");
  AIFunctionFactory.Create(..., "tag_metadata_check", "Check metadata consistency");
  AIFunctionFactory.Create(..., "tag_clean_index", "Remove tag info from index");
  ```

- [x] **3.4** Implement PDF tools (1 tool)

  ```csharp
  AIFunctionFactory.Create(..., "pdf_convert", "Convert PDF to notes");
  ```

- [x] **3.5** Implement Video tools (2 tools)

  ```csharp
  AIFunctionFactory.Create(..., "video_create_notes", "Create notes from video");
  AIFunctionFactory.Create(..., "video_consolidate_transcripts", "Consolidate transcripts");
  ```

- [x] **3.6** Implement Markdown tools (1 tool)

  ```csharp
  AIFunctionFactory.Create(..., "markdown_generate", "Convert to markdown");
  ```

- [x] **3.7** Implement Config tools (5 tools)

  ```csharp
  AIFunctionFactory.Create(..., "config_view", "View configuration");
  AIFunctionFactory.Create(..., "config_update", "Update configuration");
  AIFunctionFactory.Create(..., "config_validate", "Validate configuration");
  AIFunctionFactory.Create(..., "config_list_keys", "List config keys");
  AIFunctionFactory.Create(..., "config_secrets_status", "Check secrets status");
  ```

- [x] **3.8** Implement OneDrive tools (1 tool)

  ```csharp
  AIFunctionFactory.Create(..., "onedrive_refresh_token", "Refresh OneDrive token");
  ```

- [x] **3.9** Create system message with tool context

  ```
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/SystemMessageBuilder.cs
  ```

- [x] **3.10** Write tests for each tool
  ```
  src/c-sharp/NotebookAutomation.Tests/Cli/Services/Copilot/NotebookToolsTests.cs
  src/c-sharp/NotebookAutomation.Tests/Cli/Services/Copilot/SystemMessageBuilderTests.cs
  ```

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

- [x] **4.1** Create `SessionManager` class

  ```
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/SessionManager.cs
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/ISessionManager.cs
  ```

- [x] **4.2** Implement session save/load functionality

  ```csharp
  public async Task SaveSessionAsync(CopilotSessionMetadata session);
  public async Task<CopilotSessionMetadata?> LoadSessionAsync(string sessionId);
  public async Task<IReadOnlyList<CopilotSessionMetadata>> ListSessionsAsync();
  public async Task DeleteSessionAsync(string sessionId);
  public async Task PurgeOldSessionsAsync(int retentionDays);
  ```

- [x] **4.3** Create `FirstRunExperience` class

  ```
  src/c-sharp/NotebookAutomation.Cli/UI/FirstRunExperience.cs
  ```

- [x] **4.4** Implement Git repository detection

  ```
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/GitService.cs
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/IGitService.cs
  ```

- [x] **4.5** Implement session retention preference prompt

  ```
  Integrated in FirstRunExperience.cs (PromptSessionRetentionAsync method)
  ```

- [x] **4.6** Add `--resume` and `--session` options to `na chat`

  ```
  src/c-sharp/NotebookAutomation.Cli/Commands/CopilotCommands.cs
  ```

- [x] **4.7** Implement session purge command

  ```
  Integrated in ChatBuiltInCommands.cs (/sessions purge command)
  ```

- [x] **4.8** Store first-run preferences in user settings

  ```
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/UserPreferencesService.cs
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/IUserPreferencesService.cs
  ```

- [x] **4.9** Write tests for session management
  ```
  src/c-sharp/NotebookAutomation.Tests/Cli/Services/Copilot/SessionManagerTests.cs
  ```

#### Deliverables

- ✅ Session save/load/list/delete/purge with file-based persistence
- ✅ First-run experience with Git detection and retention preferences
- ✅ `--resume` and `--session` options for `na chat`
- ✅ User preferences storage (~/.notebookautomation/preferences.json)
- ✅ Session storage (~/.notebookautomation/sessions/)

---

### Phase 5: Advanced Features

**Goal:** Add accessibility, internationalization, logging, and polish

**Duration:** 2-3 days

#### Tasks

- [x] **5.1** Implement keyboard shortcuts handler

  ```
  src/c-sharp/NotebookAutomation.Cli/UI/KeyboardHandler.cs
  ```

- [x] **5.2** Implement multi-line input support

  ```
  src/c-sharp/NotebookAutomation.Cli/UI/MultiLineInputHandler.cs
  ```

- [x] **5.3** Implement file attachment syntax (@file)

  ```
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/FileAttachmentParser.cs
  ```

- [x] **5.4** Add accessibility options (high contrast, screen reader)

  ```
  src/c-sharp/NotebookAutomation.Cli/UI/AccessibilityOptions.cs
  ```

- [x] **5.5** Implement session logging

  ```
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/SessionLogger.cs
  ```

- [x] **5.6** Add offline mode detection and handling

  ```
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/NetworkHandler.cs
  ```

- [x] **5.7** Implement rate limiting handler

  ```
  src/c-sharp/NotebookAutomation.Cli/Services/Copilot/RateLimitHandler.cs
  ```

- [x] **5.8** Add `na chat --help` output

  ```
  src/c-sharp/NotebookAutomation.Cli/Commands/CopilotCommands.cs
  ```

- [ ] **5.9** Final integration testing

  ```
  tests/Cli/Integration/CopilotIntegrationTests.cs
  ```

- [x] **5.10** Update documentation
  ```
  docs/README.md
  docs/chat-mode.md
  ```

#### Deliverables

- Keyboard shortcuts
- Multi-line input
- File attachments
- Accessibility features
- Session logging
- Network handling
- Complete documentation

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

## Detailed Implementation Specifications

### Phase 1: Foundation — Detailed Specifications

#### 1.1 NuGet Package Installation

**File:** `src/c-sharp/NotebookAutomation.Cli/NotebookAutomation.Cli.csproj`

```xml
<ItemGroup>
  <!-- Existing packages -->

  <!-- New Copilot SDK packages -->
  <PackageReference Include="GitHub.Copilot.SDK" Version="1.*" />
  <PackageReference Include="Microsoft.Extensions.AI" Version="9.*" />
</ItemGroup>
```

**Verification:**

```bash
cd src/c-sharp/NotebookAutomation.Cli
dotnet add package GitHub.Copilot.SDK
dotnet add package Microsoft.Extensions.AI
dotnet restore
dotnet build
```

---

#### 1.2 ICopilotService Interface

**File:** `src/c-sharp/NotebookAutomation.Cli/Services/Copilot/ICopilotService.cs`

```csharp
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Abstraction for GitHub Copilot SDK operations.
/// Provides testability and allows graceful degradation when Copilot is unavailable.
/// </summary>
public interface ICopilotService : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the Copilot client is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Check if Copilot CLI is available and authenticated.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if Copilot is available and ready to use.</returns>
    Task<CopilotAvailabilityResult> CheckAvailabilityAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Start the Copilot client and establish connection.
    /// </summary>
    /// <param name="options">Client startup options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(
        CopilotStartupOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the Copilot client gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new conversation session with optional configuration.
    /// </summary>
    /// <param name="config">Session configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new Copilot session.</returns>
    Task<ICopilotSession> CreateSessionAsync(
        CopilotSessionConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resume an existing session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID to resume.</param>
    /// <param name="config">Optional configuration overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resumed session.</returns>
    Task<ICopilotSession> ResumeSessionAsync(
        string sessionId,
        CopilotSessionConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all available sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of session metadata.</returns>
    Task<IReadOnlyList<CopilotSessionMetadata>> ListSessionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Start interactive chat mode with the full UI experience.
    /// </summary>
    /// <param name="options">Chat mode options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code (0 for success).</returns>
    Task<int> StartInteractiveChatAsync(
        ChatModeOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a one-shot question and get a response (no interactive session).
    /// </summary>
    /// <param name="prompt">The question or prompt.</param>
    /// <param name="options">Ask options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response text.</returns>
    Task<string> AskAsync(
        string prompt,
        AskOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available models from the Copilot CLI.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available model names.</returns>
    Task<IReadOnlyList<string>> GetAvailableModelsAsync(
        CancellationToken cancellationToken = default);
}
```

---

#### 1.3 Supporting Types for ICopilotService

**File:** `src/c-sharp/NotebookAutomation.Cli/Services/Copilot/CopilotTypes.cs`

```csharp
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Result of checking Copilot availability.
/// </summary>
public record CopilotAvailabilityResult(
    bool IsAvailable,
    bool IsCliInstalled,
    bool IsAuthenticated,
    string? CliVersion,
    string? ErrorMessage);

/// <summary>
/// Options for starting the Copilot client.
/// </summary>
public record CopilotStartupOptions
{
    /// <summary>
    /// Path to Copilot CLI executable. Defaults to "copilot" from PATH.
    /// </summary>
    public string? CliPath { get; init; }

    /// <summary>
    /// Working directory for the CLI process.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Log level for SDK logging.
    /// </summary>
    public string LogLevel { get; init; } = "info";

    /// <summary>
    /// Whether to auto-restart on crash.
    /// </summary>
    public bool AutoRestart { get; init; } = true;
}

/// <summary>
/// Configuration for creating a Copilot session.
/// </summary>
public record CopilotSessionConfig
{
    /// <summary>
    /// Custom session ID. If null, one will be generated.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Model to use (e.g., "gpt-5", "claude-sonnet-4.5").
    /// If null, uses Copilot CLI default.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Enable streaming responses.
    /// </summary>
    public bool Streaming { get; init; } = true;

    /// <summary>
    /// Custom tools to make available to the model.
    /// </summary>
    public IReadOnlyList<object>? Tools { get; init; }

    /// <summary>
    /// System message configuration.
    /// </summary>
    public SystemMessageConfig? SystemMessage { get; init; }

    /// <summary>
    /// List of tool names to allow. If null, all tools are allowed.
    /// </summary>
    public IReadOnlyList<string>? AvailableTools { get; init; }

    /// <summary>
    /// List of tool names to exclude.
    /// </summary>
    public IReadOnlyList<string>? ExcludedTools { get; init; }
}

/// <summary>
/// System message configuration.
/// </summary>
public record SystemMessageConfig
{
    /// <summary>
    /// How to apply the system message.
    /// </summary>
    public SystemMessageMode Mode { get; init; } = SystemMessageMode.Append;

    /// <summary>
    /// The system message content.
    /// </summary>
    public required string Content { get; init; }
}

/// <summary>
/// How to apply a custom system message.
/// </summary>
public enum SystemMessageMode
{
    /// <summary>
    /// Append to the default system message.
    /// </summary>
    Append,

    /// <summary>
    /// Replace the default system message entirely.
    /// </summary>
    Replace
}

/// <summary>
/// Metadata about a saved session.
/// </summary>
public record CopilotSessionMetadata(
    string SessionId,
    string? Name,
    DateTime CreatedAt,
    DateTime LastAccessedAt,
    int MessageCount,
    string? LastTopic);

/// <summary>
/// Options for interactive chat mode.
/// </summary>
public record ChatModeOptions
{
    /// <summary>
    /// Resume the last session.
    /// </summary>
    public bool Resume { get; init; }

    /// <summary>
    /// Resume a specific session by name or ID.
    /// </summary>
    public string? SessionName { get; init; }

    /// <summary>
    /// Model to use for this session.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Enable debug logging.
    /// </summary>
    public bool Debug { get; init; }

    /// <summary>
    /// Enable high contrast mode.
    /// </summary>
    public bool HighContrast { get; init; }

    /// <summary>
    /// Skip the welcome banner.
    /// </summary>
    public bool NoBanner { get; init; }
}

/// <summary>
/// Options for one-shot ask command.
/// </summary>
public record AskOptions
{
    /// <summary>
    /// Model to use.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// File paths to attach as context.
    /// </summary>
    public IReadOnlyList<string>? Files { get; init; }

    /// <summary>
    /// Path context (e.g., a folder to focus on).
    /// </summary>
    public string? Path { get; init; }
}
```

---

#### 1.4 ICopilotSession Interface

**File:** `src/c-sharp/NotebookAutomation.Cli/Services/Copilot/ICopilotSession.cs`

```csharp
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Represents a single conversation session with Copilot.
/// </summary>
public interface ICopilotSession : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique session identifier.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Gets the current model being used.
    /// </summary>
    string? CurrentModel { get; }

    /// <summary>
    /// Gets whether the session is currently processing a message.
    /// </summary>
    bool IsProcessing { get; }

    /// <summary>
    /// Send a message and wait for the complete response.
    /// </summary>
    /// <param name="prompt">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete response text.</returns>
    Task<CopilotResponse> SendAndWaitAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message with file attachments.
    /// </summary>
    /// <param name="prompt">The message to send.</param>
    /// <param name="attachments">Files to attach.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete response text.</returns>
    Task<CopilotResponse> SendWithAttachmentsAsync(
        string prompt,
        IEnumerable<FileAttachment> attachments,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message and stream the response via events.
    /// </summary>
    /// <param name="prompt">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message ID.</returns>
    Task<string> SendAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to session events (streaming, tool execution, etc.).
    /// </summary>
    /// <param name="handler">Event handler.</param>
    /// <returns>Disposable to unsubscribe.</returns>
    IDisposable OnEvent(Action<CopilotSessionEvent> handler);

    /// <summary>
    /// Abort the currently processing message.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AbortAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all messages in this session.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of session events/messages.</returns>
    Task<IReadOnlyList<CopilotSessionEvent>> GetMessagesAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A response from Copilot.
/// </summary>
public record CopilotResponse(
    string Content,
    string MessageId,
    IReadOnlyList<ToolExecution>? ToolExecutions);

/// <summary>
/// A tool execution that occurred during response generation.
/// </summary>
public record ToolExecution(
    string ToolName,
    string Arguments,
    string Result,
    bool Success);

/// <summary>
/// File attachment for sending with messages.
/// </summary>
public record FileAttachment(
    string Path,
    string? DisplayName = null);

/// <summary>
/// Base class for session events.
/// </summary>
public abstract record CopilotSessionEvent(string Type);

/// <summary>
/// Streaming text delta event.
/// </summary>
public record AssistantMessageDeltaEvent(string DeltaContent)
    : CopilotSessionEvent("assistant_message_delta");

/// <summary>
/// Complete assistant message event.
/// </summary>
public record AssistantMessageCompleteEvent(string Content, string MessageId)
    : CopilotSessionEvent("assistant_message");

/// <summary>
/// Tool execution started event.
/// </summary>
public record ToolExecutionStartEvent(string ToolName, string ToolCallId)
    : CopilotSessionEvent("tool_execution_start");

/// <summary>
/// Tool execution completed event.
/// </summary>
public record ToolExecutionCompleteEvent(string ToolName, string ToolCallId, string Result, bool Success)
    : CopilotSessionEvent("tool_execution_complete");

/// <summary>
/// Session is idle (finished processing).
/// </summary>
public record SessionIdleEvent()
    : CopilotSessionEvent("session_idle");

/// <summary>
/// Session error event.
/// </summary>
public record SessionErrorEvent(string Message, string? Code)
    : CopilotSessionEvent("session_error");

/// <summary>
/// User message added event.
/// </summary>
public record UserMessageEvent(string Content, string MessageId)
    : CopilotSessionEvent("user_message");
```

---

#### 1.5 CopilotAvailabilityChecker

**File:** `src/c-sharp/NotebookAutomation.Cli/Services/Copilot/CopilotAvailabilityChecker.cs`

```csharp
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Checks whether GitHub Copilot CLI is installed and authenticated.
/// </summary>
public class CopilotAvailabilityChecker(ILogger<CopilotAvailabilityChecker> logger)
{
    private CopilotAvailabilityResult? _cachedResult;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Check if Copilot CLI is available and authenticated.
    /// </summary>
    /// <param name="forceRefresh">Bypass cache and check again.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Availability result with details.</returns>
    public async Task<CopilotAvailabilityResult> CheckAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (!forceRefresh && _cachedResult != null && DateTime.UtcNow < _cacheExpiry)
        {
            logger.LogDebug("Returning cached Copilot availability result");
            return _cachedResult;
        }

        logger.LogDebug("Checking Copilot CLI availability...");

        // Step 1: Check if CLI is installed
        var (isInstalled, cliVersion) = await CheckCliInstalledAsync(cancellationToken);
        if (!isInstalled)
        {
            _cachedResult = new CopilotAvailabilityResult(
                IsAvailable: false,
                IsCliInstalled: false,
                IsAuthenticated: false,
                CliVersion: null,
                ErrorMessage: "GitHub Copilot CLI not found in PATH. Install from: https://docs.github.com/copilot/how-tos/set-up/install-copilot-cli");
            _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
            return _cachedResult;
        }

        logger.LogDebug("Copilot CLI found, version: {Version}", cliVersion);

        // Step 2: Check authentication status
        var isAuthenticated = await CheckAuthenticationAsync(cancellationToken);
        if (!isAuthenticated)
        {
            _cachedResult = new CopilotAvailabilityResult(
                IsAvailable: false,
                IsCliInstalled: true,
                IsAuthenticated: false,
                CliVersion: cliVersion,
                ErrorMessage: "GitHub Copilot authentication required. Run: copilot auth login");
            _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
            return _cachedResult;
        }

        logger.LogDebug("Copilot CLI is authenticated and ready");

        _cachedResult = new CopilotAvailabilityResult(
            IsAvailable: true,
            IsCliInstalled: true,
            IsAuthenticated: true,
            CliVersion: cliVersion,
            ErrorMessage: null);
        _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
        return _cachedResult;
    }

    private async Task<(bool IsInstalled, string? Version)> CheckCliInstalledAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "copilot",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                var version = output.Trim();
                return (true, version);
            }

            return (false, null);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            logger.LogDebug("Copilot CLI not found: {Message}", ex.Message);
            return (false, null);
        }
    }

    private async Task<bool> CheckAuthenticationAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "copilot",
                    Arguments = "auth status",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            // Check for authenticated status in output
            var combinedOutput = $"{output} {error}".ToLowerInvariant();
            return process.ExitCode == 0 ||
                   combinedOutput.Contains("authenticated") ||
                   combinedOutput.Contains("logged in");
        }
        catch (Exception ex)
        {
            logger.LogDebug("Failed to check Copilot auth status: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Clear the cached availability result.
    /// </summary>
    public void ClearCache()
    {
        _cachedResult = null;
        _cacheExpiry = DateTime.MinValue;
    }
}
```

---

#### 1.6 CopilotConfig

**File:** `src/c-sharp/NotebookAutomation.Core/Configuration/CopilotConfig.cs`

```csharp
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Configuration for GitHub Copilot SDK integration.
/// </summary>
public class CopilotConfig
{
    /// <summary>
    /// Whether Copilot chat mode is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default model to use. Null means let Copilot CLI decide.
    /// </summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// Directory where sessions are stored.
    /// </summary>
    public string SessionDirectory { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".notebookautomation",
            "sessions");

    /// <summary>
    /// Session retention policy: "forever", "90d", "30d", "7d".
    /// </summary>
    public string SessionRetention { get; set; } = "forever";

    /// <summary>
    /// Enable streaming responses.
    /// </summary>
    public bool Streaming { get; set; } = true;

    /// <summary>
    /// Preferred language for responses.
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Accessibility settings.
    /// </summary>
    public CopilotAccessibilityConfig Accessibility { get; set; } = new();

    /// <summary>
    /// Logging settings.
    /// </summary>
    public CopilotLoggingConfig Logging { get; set; } = new();

    /// <summary>
    /// Telemetry settings.
    /// </summary>
    public CopilotTelemetryConfig Telemetry { get; set; } = new();

    /// <summary>
    /// Parse session retention string to TimeSpan.
    /// Returns null for "forever".
    /// </summary>
    public TimeSpan? GetSessionRetentionTimeSpan()
    {
        return SessionRetention.ToLowerInvariant() switch
        {
            "forever" => null,
            "90d" => TimeSpan.FromDays(90),
            "30d" => TimeSpan.FromDays(30),
            "7d" => TimeSpan.FromDays(7),
            _ => null
        };
    }
}

/// <summary>
/// Accessibility configuration.
/// </summary>
public class CopilotAccessibilityConfig
{
    /// <summary>
    /// Enable high contrast mode.
    /// </summary>
    public bool HighContrast { get; set; }

    /// <summary>
    /// Reduce motion/animations.
    /// </summary>
    public bool ReducedMotion { get; set; }

    /// <summary>
    /// Announce progress for screen readers.
    /// </summary>
    public bool AnnounceProgress { get; set; } = true;
}

/// <summary>
/// Logging configuration.
/// </summary>
public class CopilotLoggingConfig
{
    /// <summary>
    /// Log level: "debug", "info", "warning", "error".
    /// </summary>
    public string Level { get; set; } = "info";

    /// <summary>
    /// Log session interactions to file.
    /// </summary>
    public bool SessionLogging { get; set; }
}

/// <summary>
/// Telemetry configuration.
/// </summary>
public class CopilotTelemetryConfig
{
    /// <summary>
    /// Enable anonymous telemetry.
    /// </summary>
    public bool Enabled { get; set; }
}
```

---

#### 1.7 Update AppConfig

**File:** `src/c-sharp/NotebookAutomation.Core/Configuration/AppConfig.cs` (add to existing)

```csharp
// Add to existing AppConfig class:

/// <summary>
/// GitHub Copilot SDK configuration.
/// </summary>
public CopilotConfig? Copilot { get; set; }

/// <summary>
/// Get Copilot config with defaults if not configured.
/// </summary>
public CopilotConfig GetCopilotConfig() => Copilot ?? new CopilotConfig();
```

---

#### 1.8 Service Registration

**File:** `src/c-sharp/NotebookAutomation.Cli/Startup/CopilotServiceRegistration.cs`

```csharp
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace NotebookAutomation.Cli.Startup;

/// <summary>
/// Registers Copilot-related services in the DI container.
/// </summary>
public static class CopilotServiceRegistration
{
    /// <summary>
    /// Add Copilot services to the service collection.
    /// </summary>
    public static IServiceCollection AddCopilotServices(this IServiceCollection services)
    {
        // Availability checker (singleton for caching)
        services.AddSingleton<CopilotAvailabilityChecker>();

        // Main Copilot service (scoped per operation)
        services.AddScoped<ICopilotService, CopilotService>();

        // Notebook tools (custom tools for Copilot)
        services.AddScoped<NotebookTools>();

        // Session manager
        services.AddSingleton<SessionManager>();

        // UI components
        services.AddTransient<ChatModeUI>();
        services.AddTransient<WelcomeBanner>();
        services.AddTransient<FirstRunExperience>();

        // Built-in commands handler
        services.AddTransient<ChatBuiltInCommands>();

        // System message builder
        services.AddTransient<SystemMessageBuilder>();

        return services;
    }
}
```

---

### Phase 2: Chat Mode Core — Detailed Specifications

#### 2.1 WelcomeBanner

**File:** `src/c-sharp/NotebookAutomation.Cli/UI/WelcomeBanner.cs`

```csharp
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Spectre.Console;

namespace NotebookAutomation.Cli.UI;

/// <summary>
/// Displays the ASCII art welcome banner for chat mode.
/// </summary>
public class WelcomeBanner(CopilotConfig config)
{
    private static readonly string AsciiArt = @"
  _   _       _       _                 _
 | \ | | ___ | |_ ___| |__   ___   ___ | | __
 |  \| |/ _ \| __/ _ \ '_ \ / _ \ / _ \| |/ /
 | |\  | (_) | ||  __/ |_) | (_) | (_) |   <
 |_| \_|\___/ \__\___|_.__/ \___/ \___/|_|\_\
     _         _                        _   _
    / \  _   _| |_ ___  _ __ ___   __ _| |_(_) ___  _ __
   / _ \| | | | __/ _ \| '_ ` _ \ / _` | __| |/ _ \| '_ \
  / ___ \ |_| | || (_) | | | | | | (_| | |_| | (_) | | | |
 /_/   \_\__,_|\__\___/|_| |_| |_|\__,_|\__|_|\___/|_| |_|
";

    private static readonly string[] Tips =
    [
        "You can convert files by just describing what you want, like \"Convert all HTMLs in my Data Science folder to markdown\"",
        "Use 'help tags' to learn about tagging features",
        "Type '!command' to run CLI commands directly, like '!config view'",
        "Save your session with 'session save <name>' to continue later",
        "Use '@filename.md' to reference files in your prompts",
        "Ask 'What did I learn last week?' to see your recent notes"
    ];

    /// <summary>
    /// Display the welcome banner.
    /// </summary>
    /// <param name="showTip">Whether to show a random tip.</param>
    public void Display(bool showTip = true)
    {
        var style = config.Accessibility.HighContrast
            ? new Style(Color.White, Color.Black)
            : new Style(Color.Cyan1);

        AnsiConsole.Write(new Text(AsciiArt, style));
        AnsiConsole.WriteLine();

        // Copyright and tagline
        AnsiConsole.MarkupLine(" [dim]© 2026 Daniel Shue | Your intelligent vault assistant[/]");
        AnsiConsole.WriteLine();

        // Copilot box
        var panel = new Panel(
            "[bold]🤖 Powered by GitHub Copilot[/]\n\n" +
            "I can help you manage your notes vault, convert documents,\n" +
            "organize notes, and answer questions about your content.\n\n" +
            "[dim]Commands: 'help' | 'exit' | 'clear' | 'history'[/]")
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0, 1, 0)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Random tip
        if (showTip)
        {
            var tip = Tips[Random.Shared.Next(Tips.Length)];
            AnsiConsole.MarkupLine($"[yellow]💡 Tip:[/] {tip}");
            AnsiConsole.WriteLine();
        }
    }
}
```

---

#### 2.2 ChatModeUI

**File:** `src/c-sharp/NotebookAutomation.Cli/UI/ChatModeUI.cs`

```csharp
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Spectre.Console;

namespace NotebookAutomation.Cli.UI;

/// <summary>
/// Orchestrates the chat mode user interface.
/// </summary>
public class ChatModeUI(
    WelcomeBanner welcomeBanner,
    CopilotConfig config,
    ILogger<ChatModeUI> logger)
{
    private readonly StringBuilder _currentResponse = new();
    private bool _isStreaming;

    /// <summary>
    /// Display the welcome experience.
    /// </summary>
    /// <param name="isFirstRun">Whether this is the first run.</param>
    public void ShowWelcome(bool isFirstRun)
    {
        Console.Clear();
        welcomeBanner.Display(showTip: !isFirstRun);
    }

    /// <summary>
    /// Read user input with prompt.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's input.</returns>
    public async Task<string> ReadInputAsync(CancellationToken cancellationToken)
    {
        Console.Write("\n");
        AnsiConsole.Markup("[green]You ❯[/] ");

        var input = await Task.Run(() => Console.ReadLine() ?? string.Empty, cancellationToken);
        return input.Trim();
    }

    /// <summary>
    /// Write streaming response delta.
    /// </summary>
    /// <param name="delta">The text delta to append.</param>
    public void WriteStreaming(string delta)
    {
        if (!_isStreaming)
        {
            _isStreaming = true;
            Console.Write("\n");
            AnsiConsole.Markup("[blue]🤖[/] ");
        }

        _currentResponse.Append(delta);
        Console.Write(delta);
    }

    /// <summary>
    /// Complete the streaming response.
    /// </summary>
    public void CompleteStreaming()
    {
        if (_isStreaming)
        {
            Console.WriteLine();
            _isStreaming = false;
            _currentResponse.Clear();
        }
    }

    /// <summary>
    /// Write a complete (non-streaming) response.
    /// </summary>
    /// <param name="content">The response content.</param>
    public void WriteResponse(string content)
    {
        Console.Write("\n");
        AnsiConsole.Markup("[blue]🤖[/] ");
        Console.WriteLine(content);
    }

    /// <summary>
    /// Write tool execution start.
    /// </summary>
    /// <param name="toolName">Name of the tool.</param>
    public void WriteToolStart(string toolName)
    {
        var displayName = FormatToolName(toolName);
        AnsiConsole.MarkupLine($"\n   [dim][[Executing: {displayName}]][/]");
    }

    /// <summary>
    /// Write tool execution result.
    /// </summary>
    /// <param name="toolName">Name of the tool.</param>
    /// <param name="result">The result.</param>
    /// <param name="success">Whether it succeeded.</param>
    public void WriteToolResult(string toolName, string result, bool success)
    {
        var icon = success ? "[green]✓[/]" : "[red]✗[/]";

        // Format result with indentation
        var lines = result.Split('\n');
        foreach (var line in lines)
        {
            AnsiConsole.MarkupLine($"   {icon} {Markup.Escape(line)}");
        }
    }

    /// <summary>
    /// Write an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public void WriteError(string message)
    {
        AnsiConsole.MarkupLine($"\n[red]⚠️ Error:[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Write a warning message.
    /// </summary>
    /// <param name="message">The warning message.</param>
    public void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine($"\n[yellow]⚠️[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Write an info message.
    /// </summary>
    /// <param name="message">The info message.</param>
    public void WriteInfo(string message)
    {
        AnsiConsole.MarkupLine($"\n[dim]{Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Clear the screen.
    /// </summary>
    public void Clear()
    {
        Console.Clear();
    }

    private static string FormatToolName(string toolName)
    {
        // Convert snake_case to readable format
        // e.g., "vault_generate_index" → "vault generate-index"
        return toolName.Replace('_', ' ').Replace("  ", " ");
    }
}
```

---

#### 2.3 ChatBuiltInCommands

**File:** `src/c-sharp/NotebookAutomation.Cli/Services/Copilot/ChatBuiltInCommands.cs`

```csharp
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Handles built-in chat commands like help, exit, clear, etc.
/// </summary>
public class ChatBuiltInCommands(
    ChatModeUI ui,
    SessionManager sessionManager,
    ILogger<ChatBuiltInCommands> logger)
{
    /// <summary>
    /// Try to handle a built-in command.
    /// </summary>
    /// <param name="input">The user input.</param>
    /// <param name="session">The current session.</param>
    /// <param name="result">The result of handling.</param>
    /// <returns>True if the input was a built-in command.</returns>
    public bool TryHandle(string input, ICopilotSession? session, out BuiltInCommandResult result)
    {
        result = BuiltInCommandResult.Continue;

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;

        var command = parts[0].ToLowerInvariant();
        var args = parts.Skip(1).ToArray();

        switch (command)
        {
            case "exit":
            case "quit":
                result = BuiltInCommandResult.Exit;
                return true;

            case "help":
                HandleHelp(args);
                return true;

            case "clear":
                ui.Clear();
                return true;

            case "history":
                HandleHistory(session);
                return true;

            case "model":
                HandleModel(args, session);
                return true;

            case "session":
                HandleSession(args, session);
                return true;

            default:
                // Check for ! prefix (direct CLI command)
                if (input.StartsWith('!'))
                {
                    HandleDirectCommand(input[1..]);
                    return true;
                }
                return false;
        }
    }

    private void HandleHelp(string[] args)
    {
        if (args.Length == 0)
        {
            ShowGeneralHelp();
        }
        else
        {
            ShowTopicHelp(args[0]);
        }
    }

    private void ShowGeneralHelp()
    {
        ui.WriteResponse(@"Here's what I can help you with:

   📚 **Managing Your Vault**
   • Search and find notes
   • Organize folders and structure
   • Generate index files
   • Ensure metadata consistency

   📄 **Converting Content**
   • HTML → Markdown
   • PDF → Notes with extracted text/images
   • Video transcripts → Organized notes
   • EPUB → Markdown

   🏷️ **Tagging & Metadata**
   • Add/remove tags
   • Update frontmatter
   • Consolidate and restructure tags

   ⚙️ **Configuration**
   • View and update settings
   • Validate configuration
   • Manage paths and secrets

   💡 Type 'help <topic>' for details (tags, vault, pdf, video, config)
      Or just ask me anything!");
    }

    private void ShowTopicHelp(string topic)
    {
        var helpText = topic.ToLowerInvariant() switch
        {
            "tags" or "tag" => GetTagsHelp(),
            "vault" => GetVaultHelp(),
            "pdf" => GetPdfHelp(),
            "video" => GetVideoHelp(),
            "config" => GetConfigHelp(),
            "session" or "sessions" => GetSessionHelp(),
            _ => $"Unknown topic: {topic}. Try: tags, vault, pdf, video, config, session"
        };

        ui.WriteResponse(helpText);
    }

    private static string GetTagsHelp() => @"**Tagging & Metadata Features**

   I can help you manage tags in your notes vault:

   🏷️ **Adding Tags**
   • ""Add #finance tag to all notes in my Budget folder""
   • ""Tag this note with #important #review""

   🔄 **Organizing Tags**
   • ""Consolidate duplicate tags""
   • ""Restructure my tags for consistency""

   📝 **Frontmatter**
   • ""Update the author field in my course notes""
   • ""Add a 'status: draft' to new notes""

   🔧 **CLI Commands**
   • !tag add-nested, !tag consolidate, !tag update-frontmatter";

    private static string GetVaultHelp() => @"**Vault Management Features**

   📁 **Organization**
   • ""Generate index files for my vault""
   • ""Ensure metadata consistency""
   • ""Sync my vault with OneDrive""

   🔍 **Search & Discovery**
   • ""Find all notes about Python""
   • ""What did I learn last week?""

   🔧 **CLI Commands**
   • !vault generate-index, !vault ensure-metadata, !vault vault-sync";

    private static string GetPdfHelp() => @"**PDF Conversion Features**

   📄 **Converting PDFs**
   • ""Convert my PDF lectures to notes""
   • ""Extract text and images from this PDF""

   Options:
   • --extract-images: Include diagrams and images
   • --output: Specify output directory

   🔧 **CLI Command**
   • !pdf-notes --path ""path/to/pdfs""";

    private static string GetVideoHelp() => @"**Video Notes Features**

   📺 **Creating Notes**
   • ""Create notes from this YouTube video: [url]""
   • ""Generate notes from videos in this folder""

   📝 **Transcripts**
   • ""Consolidate video transcripts in my ML course""

   🔧 **CLI Commands**
   • !video-notes --url ""..."", !video-transcripts consolidate";

    private static string GetConfigHelp() => @"**Configuration Features**

   ⚙️ **Viewing & Updating**
   • ""Show my configuration""
   • ""Change my vault path to...""
   • ""What settings can I change?""

   ✅ **Validation**
   • ""Validate my configuration""

   🔧 **CLI Commands**
   • !config view, !config update, !config validate";

    private static string GetSessionHelp() => @"**Session Management**

   💾 **Saving & Loading**
   • session save <name>  - Save current session
   • session list         - List saved sessions
   • session load <name>  - Load a saved session

   🗑️ **Cleanup**
   • session purge --older-than 30d

   Resume from CLI:
   • na chat --resume
   • na chat --session ""name""";

    private void HandleHistory(ICopilotSession? session)
    {
        if (session == null)
        {
            ui.WriteWarning("No active session.");
            return;
        }

        // TODO: Get and display session history
        ui.WriteInfo("Session history display not yet implemented.");
    }

    private void HandleModel(string[] args, ICopilotSession? session)
    {
        if (args.Length == 0)
        {
            var current = session?.CurrentModel ?? "default";
            ui.WriteResponse($"Current model: {current}\n\nTo change: model <name>\nExample: model gpt-5");
        }
        else
        {
            // TODO: Change model
            ui.WriteInfo($"Model change to '{args[0]}' not yet implemented.");
        }
    }

    private void HandleSession(string[] args, ICopilotSession? session)
    {
        if (args.Length == 0)
        {
            ShowTopicHelp("session");
            return;
        }

        var subcommand = args[0].ToLowerInvariant();
        var subArgs = args.Skip(1).ToArray();

        switch (subcommand)
        {
            case "save":
                if (subArgs.Length == 0)
                    ui.WriteError("Usage: session save <name>");
                else
                    HandleSessionSave(subArgs[0], session);
                break;

            case "list":
                HandleSessionList();
                break;

            case "load":
                if (subArgs.Length == 0)
                    ui.WriteError("Usage: session load <name>");
                else
                    ui.WriteInfo($"Session load '{subArgs[0]}' - restart with: na chat --session \"{subArgs[0]}\"");
                break;

            case "purge":
                HandleSessionPurge(subArgs);
                break;

            default:
                ui.WriteError($"Unknown session command: {subcommand}");
                break;
        }
    }

    private void HandleSessionSave(string name, ICopilotSession? session)
    {
        if (session == null)
        {
            ui.WriteWarning("No active session to save.");
            return;
        }

        // TODO: Save session
        ui.WriteInfo($"Session saved as \"{name}\"");
    }

    private void HandleSessionList()
    {
        // TODO: List sessions from SessionManager
        ui.WriteInfo("Session listing not yet implemented.");
    }

    private void HandleSessionPurge(string[] args)
    {
        // TODO: Parse --older-than and purge
        ui.WriteInfo("Session purge not yet implemented.");
    }

    private void HandleDirectCommand(string command)
    {
        ui.WriteInfo($"[Executing: na {command}]");

        // TODO: Execute CLI command and capture output
        ui.WriteInfo("Direct CLI execution not yet implemented.");
    }
}

/// <summary>
/// Result of handling a built-in command.
/// </summary>
public enum BuiltInCommandResult
{
    /// <summary>Continue the chat loop.</summary>
    Continue,

    /// <summary>Exit chat mode.</summary>
    Exit
}
```

---

_Document maintained in: `docs/features/copilot-sdk-implementation-plan.md`_
