# GitHub Issues for Copilot SDK Integration

Use these templates to create GitHub issues for the Copilot SDK integration project.

---

## Epic Issue

**Title:** `[Epic] GitHub Copilot SDK Integration - Intelligent Chat Mode`

**Labels:** `enhancement`, `copilot-sdk`, `epic`

**Body:**

```markdown
## Overview

Integrate the GitHub Copilot SDK into Notebook Automation CLI to provide an intelligent, conversational interface. When users run `na` with no arguments, they'll enter an interactive chat mode powered by Copilot.

## Goals

1. **Natural Language Interface** — Manage vault through conversation
2. **Intelligent Reasoning** — Search, read, and synthesize vault content
3. **Backward Compatibility** — All existing CLI commands unchanged
4. **Progressive Enhancement** — Choice between commands or chat

## Documentation

- **Feature Specification:** [docs/features/copilot-sdk-integration.md](docs/features/copilot-sdk-integration.md)
- **Implementation Plan:** [docs/features/copilot-sdk-implementation-plan.md](docs/features/copilot-sdk-implementation-plan.md)

## Key Features

- `na` (no args) → Interactive chat mode
- `na chat` → Explicit chat mode entry
- `na ask "question"` → One-shot question
- All CLI commands available via natural language
- Session persistence and resume
- First-run experience with Git repo detection

## Implementation Phases

| Phase   | Description                      | Duration | Status             |
| ------- | -------------------------------- | -------- | ------------------ |
| Phase 1 | Foundation (SDK, interfaces, DI) | 1-2 days | ✅ Complete        |
| Phase 2 | Chat Mode Core (UI, streaming)   | 2-3 days | ✅ Complete        |
| Phase 3 | CLI Command Integration (tools)  | 3-4 days | ✅ Complete        |
| Phase 4 | Session Management               | 2-3 days | ✅ Complete        |
| Phase 5 | Advanced Features                | 2-3 days | ⏳ Mostly Complete |

**Total Estimated:** 10-15 days  
**Remaining:** Integration tests for Phase 5

## Child Issues

- [x] #\_\_ Phase 1: Foundation ✅
- [x] #\_\_ Phase 2: Chat Mode Core ✅
- [x] #\_\_ Phase 3: CLI Command Integration ✅
- [x] #\_\_ Phase 4: Session Management ✅
- [~] #\_\_ Phase 5: Advanced Features (integration tests remaining)

## Acceptance Criteria

- [x] `na` with no args enters chat mode (when Copilot available)
- [x] All existing CLI commands work unchanged
- [x] Graceful fallback when Copilot unavailable
- [x] Session persistence works
- [x] Documentation updated
```

---

## Phase 1 Issue

**Title:** `[Phase 1] Copilot SDK Foundation`

**Labels:** `enhancement`, `copilot-sdk`, `phase-1`

**Body:**

````markdown
## Goal

Add Copilot SDK dependency, create abstraction layer, and detect Copilot availability.

**Parent:** #\_\_ (Epic)

## Tasks

- [x] Add NuGet packages (`Microsoft.Extensions.AI`, `Microsoft.SemanticKernel`)
- [x] Create `ICopilotService` interface
- [x] Create `ICopilotSession` interface
- [x] Create supporting types (`CopilotAvailabilityResult`, `CopilotSessionConfig`, etc.)
- [x] Implement `CopilotAvailabilityChecker` (API key detection)
- [x] Create `CopilotConfig` configuration class
- [x] Update `AppConfig` with Copilot section
- [x] Create `CopilotServiceRegistration` for DI
- [x] Write unit tests for availability checker

## Key Interfaces

```csharp
public interface ICopilotService : IAsyncDisposable
{
    Task<CopilotAvailabilityResult> CheckAvailabilityAsync(CancellationToken ct = default);
    Task StartAsync(CopilotStartupOptions? options = null, CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task<ICopilotSession> CreateSessionAsync(CopilotSessionConfig? config = null, CancellationToken ct = default);
    Task<int> StartInteractiveChatAsync(ChatModeOptions? options = null, CancellationToken ct = default);
    Task<string> AskAsync(string prompt, AskOptions? options = null, CancellationToken ct = default);
}
```
````

## Files to Create

- `Services/Copilot/ICopilotService.cs`
- `Services/Copilot/ICopilotSession.cs`
- `Services/Copilot/CopilotTypes.cs`
- `Services/Copilot/CopilotAvailabilityChecker.cs`
- `Configuration/CopilotConfig.cs`
- `Startup/CopilotServiceRegistration.cs`

## Acceptance Criteria

- [x] `dotnet build` succeeds with new packages
- [x] `CopilotAvailabilityChecker` correctly detects API key presence
- [x] `CopilotAvailabilityChecker` correctly detects authentication status
- [x] All services registered in DI container
- [x] Unit tests passing

## Status: ✅ COMPLETE

## Duration Estimate

1-2 days

````

---

## Phase 2 Issue

**Title:** `[Phase 2] Chat Mode Core`

**Labels:** `enhancement`, `copilot-sdk`, `phase-2`

**Body:**

```markdown
## Goal

Implement interactive chat mode when no arguments provided.

**Parent:** #__ (Epic)
**Depends on:** #__ (Phase 1)

## Tasks

- [x] Create `WelcomeBanner` class with ASCII art
- [x] Create `ChatModeUI` class for input/output handling
- [x] Implement streaming response display
- [x] Create `ChatBuiltInCommands` (help, exit, clear, history, model, session)
- [x] Modify `Program.cs` to enter chat mode when no args
- [x] Create `CopilotCommands` class for `na chat` and `na ask`
- [x] Register new commands with System.CommandLine
- [x] Write unit tests

## New Commands

| Command | Description |
|---------|-------------|
| `na` (no args) | Enter chat mode |
| `na chat` | Explicit chat mode |
| `na chat --resume` | Resume last session |
| `na chat --model <model>` | Use specific model |
| `na ask "question"` | One-shot question |

## Built-in Chat Commands

| Command | Action |
|---------|--------|
| `help` | Show capabilities |
| `help <topic>` | Topic-specific help |
| `exit` / `quit` | Exit chat |
| `clear` | Clear screen |
| `history` | Show history |
| `model [name]` | Show/change model |
| `!<cmd>` | Direct CLI execution |

## Files to Create

- `UI/WelcomeBanner.cs`
- `UI/ChatModeUI.cs`
- `Services/Copilot/ChatBuiltInCommands.cs`
- `Commands/CopilotCommands.cs`

## Acceptance Criteria

- [x] `na` with no args shows welcome banner and enters chat
- [x] User can type messages and receive streaming responses
- [x] `help` command shows capabilities
- [x] `exit` command exits chat mode
- [x] `na chat` and `na ask` commands work
- [x] Graceful fallback when Copilot unavailable

## Status: ✅ COMPLETE

## Duration Estimate

2-3 days
````

---

## Phase 3 Issue

**Title:** `[Phase 3] CLI Command Integration`

**Labels:** `enhancement`, `copilot-sdk`, `phase-3`

**Body:**

```markdown
## Goal

Expose all existing CLI commands as Copilot-callable tools.

**Parent:** #** (Epic)
**Depends on:** #** (Phase 2)

## Tasks

- [x] Create `INotebookTools` interface
- [x] Implement `NotebookTools` class with all tool registrations
- [x] Create `ISystemMessageBuilder` interface
- [x] Implement system message with tool context
- [x] Register Vault tools (4 tools)
- [x] Register Tag tools (7 tools)
- [x] Register PDF tools (1 tool)
- [x] Register Video tools (2 tools)
- [x] Register Markdown tools (1 tool)
- [x] Register Config tools (5 tools)
- [x] Register OneDrive tools (1 tool)
- [x] Write unit tests for each tool

## Tool Definitions

### Vault Tools

| Tool                    | Parameters   | Description                 |
| ----------------------- | ------------ | --------------------------- |
| `vault_generate_index`  | `path?`      | Generate index files        |
| `vault_ensure_metadata` | `path?`      | Ensure metadata consistency |
| `vault_clean_index`     | `path?`      | Remove index files          |
| `vault_sync`            | `direction?` | Sync with OneDrive          |

### Tag Tools

| Tool                     | Parameters         | Description                |
| ------------------------ | ------------------ | -------------------------- |
| `tag_add_nested`         | `path`             | Add nested tags            |
| `tag_consolidate`        | `path?`            | Consolidate duplicate tags |
| `tag_restructure`        | `path?`            | Restructure tags           |
| `tag_update_frontmatter` | `path, key, value` | Update frontmatter         |
| `tag_diagnose_yaml`      | `path?`            | Diagnose YAML issues       |
| `tag_metadata_check`     | `path?`            | Check metadata             |
| `tag_clean_index`        | `path?`            | Clean tags from index      |

### Other Tools

- `pdf_convert` - Convert PDF to notes
- `video_create_notes` - Create notes from video
- `video_consolidate_transcripts` - Consolidate transcripts
- `markdown_generate` - Convert to markdown
- `config_view`, `config_update`, `config_validate`, `config_list_keys`, `config_secrets_status`
- `onedrive_refresh_token`

## Files to Create

- `Services/Copilot/INotebookTools.cs`
- `Services/Copilot/NotebookTools.cs`
- `Services/Copilot/ISystemMessageBuilder.cs`
- `Services/Copilot/SystemMessageBuilder.cs`

## Acceptance Criteria

- [x] All 21 tools registered and callable
- [x] Natural language requests trigger correct tools
- [x] Tool results display correctly in chat
- [x] System message provides context about tools
- [x] Existing command logic reused (no duplication)

## Status: ✅ COMPLETE

## Duration Estimate

3-4 days
```

---

## Phase 4 Issue

**Title:** `[Phase 4] Session Management`

**Labels:** `enhancement`, `copilot-sdk`, `phase-4`

**Body:**

```markdown
## Goal

Implement session persistence, first-run experience, and Git detection.

**Parent:** #** (Epic)
**Depends on:** #** (Phase 3)

## Tasks

- [x] Create `ISessionManager` interface
- [x] Implement session save/load/list/purge
- [x] Create `IUserPreferencesService` interface
- [x] Implement user preferences storage
- [x] Create `FirstRunExperience` class
- [x] Implement first-run setup flow
- [x] Create `IGitService` interface
- [x] Implement Git repository detection
- [x] Add `--resume` and `--session` options to `na chat`
- [x] Implement `session` built-in commands
- [x] Implement `na chat purge` subcommand
- [x] Write unit tests

## Session Commands

| Command                               | Description             |
| ------------------------------------- | ----------------------- |
| `session save <name>`                 | Save current session    |
| `session list`                        | List saved sessions     |
| `session load <name>`                 | Load saved session      |
| `session purge --older-than <period>` | Purge old sessions      |
| `na chat --resume`                    | Resume last session     |
| `na chat --session <name>`            | Resume specific session |

## First-Run Experience

1. Detect if current directory is Git repo
2. Prompt to initialize Git (optional)
3. Prompt for session retention preference
4. Save preferences

## Session Storage
```

~/.notebookautomation/sessions/
├── index.json
├── <sessionId>.json
└── ...

```

## Files to Create

- `Services/Copilot/ISessionManager.cs`
- `Services/Copilot/SessionManager.cs`
- `Services/Copilot/IUserPreferencesService.cs`
- `Services/Copilot/UserPreferencesService.cs`
- `Services/Copilot/IGitService.cs`
- `Services/Copilot/GitService.cs`
- `UI/FirstRunExperience.cs`

## Acceptance Criteria

- [x] Sessions persist to `~/.notebookautomation/sessions/`
- [x] `session save/list/load` commands work
- [x] `--resume` resumes last session
- [x] First-run shows on first launch
- [x] Git repo detection works
- [x] Session retention preference saved
- [x] `na chat purge` cleans old sessions

## Status: ✅ COMPLETE

## Duration Estimate

2-3 days
```

---

## Phase 5 Issue

**Title:** `[Phase 5] Advanced Features`

**Labels:** `enhancement`, `copilot-sdk`, `phase-5`

**Body:**

```markdown
## Goal

Add accessibility, internationalization, logging, and polish.

**Parent:** #** (Epic)
**Depends on:** #** (Phase 4)

## Tasks

- [x] Implement keyboard shortcuts (`KeyboardHandler`)
- [x] Implement multi-line input support (`MultiLineInputHandler`)
- [x] Create `FileAttachmentParser` for @file syntax
- [x] Create `NetworkHandler` for offline detection
- [x] Create `RateLimitHandler` for quota management
- [x] Create `AccessibilityOptions` for high contrast/screen readers
- [x] Create `SessionLogger` for session logging
- [x] Add `--high-contrast` and `--no-banner` options
- [x] Implement `na chat --help` output
- [x] Update documentation
- [ ] Final integration testing (`CopilotIntegrationTests`)

## Keyboard Shortcuts

| Shortcut  | Action             |
| --------- | ------------------ |
| `Ctrl+C`  | Cancel/Exit        |
| `Ctrl+D`  | Exit (EOF)         |
| `↑` / `↓` | History navigation |
| `Ctrl+L`  | Clear screen       |
| `Tab`     | Auto-complete      |

## File Attachment Syntax
```

You ❯ @Notes/Finance/Budget.md What are the key points?
You ❯ Compare @file1.md and @file2.md

```

## Network Handling

- Detect offline mode
- Graceful degradation message
- Connection recovery

## Rate Limiting

- Detect approaching quota
- Show warnings
- Offer options (wait, pause, offline)

## Files Created

- ✅ `UI/KeyboardHandler.cs`
- ✅ `UI/MultiLineInputHandler.cs`
- ✅ `UI/AccessibilityOptions.cs`
- ✅ `Services/Copilot/FileAttachmentParser.cs`
- ✅ `Services/Copilot/NetworkHandler.cs`
- ✅ `Services/Copilot/RateLimitHandler.cs`
- ✅ `Services/Copilot/SessionLogger.cs`

## Acceptance Criteria

- [x] Keyboard shortcuts work
- [x] Multi-line input supported (triple backticks)
- [x] @file attachment syntax works
- [x] Offline mode handled gracefully
- [x] Rate limiting shows user feedback
- [x] High contrast mode works
- [x] Session logging available
- [x] Documentation updated
- [ ] All integration tests passing

## Status: ⏳ MOSTLY COMPLETE (integration tests remaining)

## Duration Estimate

2-3 days
```

---

## GitHub CLI Commands

To create these issues using the GitHub CLI (`gh`), run these commands from the repository root:

```bash
# Create Epic issue
gh issue create --title "[Epic] GitHub Copilot SDK Integration - Intelligent Chat Mode" --label "enhancement,copilot-sdk,epic" --body-file .github/issues/copilot-epic.md

# Create Phase issues (after Epic is created, update parent reference)
gh issue create --title "[Phase 1] Copilot SDK Foundation" --label "enhancement,copilot-sdk,phase-1" --body-file .github/issues/copilot-phase1.md
gh issue create --title "[Phase 2] Chat Mode Core" --label "enhancement,copilot-sdk,phase-2" --body-file .github/issues/copilot-phase2.md
gh issue create --title "[Phase 3] CLI Command Integration" --label "enhancement,copilot-sdk,phase-3" --body-file .github/issues/copilot-phase3.md
gh issue create --title "[Phase 4] Session Management" --label "enhancement,copilot-sdk,phase-4" --body-file .github/issues/copilot-phase4.md
gh issue create --title "[Phase 5] Advanced Features" --label "enhancement,copilot-sdk,phase-5" --body-file .github/issues/copilot-phase5.md
```

Or create them manually via GitHub web interface using the content above.
