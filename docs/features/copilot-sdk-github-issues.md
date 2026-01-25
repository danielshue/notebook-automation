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

| Phase | Description | Duration |
|-------|-------------|----------|
| Phase 1 | Foundation (SDK, interfaces, DI) | 1-2 days |
| Phase 2 | Chat Mode Core (UI, streaming) | 2-3 days |
| Phase 3 | CLI Command Integration (tools) | 3-4 days |
| Phase 4 | Session Management | 2-3 days |
| Phase 5 | Advanced Features | 2-3 days |

**Total Estimated:** 10-15 days

## Child Issues

- [ ] #__ Phase 1: Foundation
- [ ] #__ Phase 2: Chat Mode Core
- [ ] #__ Phase 3: CLI Command Integration
- [ ] #__ Phase 4: Session Management
- [ ] #__ Phase 5: Advanced Features

## Acceptance Criteria

- [ ] `na` with no args enters chat mode (when Copilot available)
- [ ] All existing CLI commands work unchanged
- [ ] Graceful fallback when Copilot unavailable
- [ ] Session persistence works
- [ ] Documentation updated
```

---

## Phase 1 Issue

**Title:** `[Phase 1] Copilot SDK Foundation`

**Labels:** `enhancement`, `copilot-sdk`, `phase-1`

**Body:**

```markdown
## Goal

Add Copilot SDK dependency, create abstraction layer, and detect Copilot availability.

**Parent:** #__ (Epic)

## Tasks

- [ ] Add NuGet packages (`GitHub.Copilot.SDK`, `Microsoft.Extensions.AI`)
- [ ] Create `ICopilotService` interface
- [ ] Create `ICopilotSession` interface
- [ ] Create supporting types (`CopilotAvailabilityResult`, `CopilotSessionConfig`, etc.)
- [ ] Implement `CopilotAvailabilityChecker` (CLI detection, auth check)
- [ ] Create `CopilotConfig` configuration class
- [ ] Update `AppConfig` with Copilot section
- [ ] Create `CopilotServiceRegistration` for DI
- [ ] Write unit tests for availability checker

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

## Files to Create

- `Services/Copilot/ICopilotService.cs`
- `Services/Copilot/ICopilotSession.cs`
- `Services/Copilot/CopilotTypes.cs`
- `Services/Copilot/CopilotAvailabilityChecker.cs`
- `Configuration/CopilotConfig.cs`
- `Startup/CopilotServiceRegistration.cs`

## Acceptance Criteria

- [ ] `dotnet build` succeeds with new packages
- [ ] `CopilotAvailabilityChecker` correctly detects Copilot CLI presence
- [ ] `CopilotAvailabilityChecker` correctly detects authentication status
- [ ] All services registered in DI container
- [ ] Unit tests passing

## Duration Estimate

1-2 days
```

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

- [ ] Create `WelcomeBanner` class with ASCII art
- [ ] Create `ChatModeUI` class for input/output handling
- [ ] Implement streaming response display
- [ ] Create `ChatBuiltInCommands` (help, exit, clear, history, model, session)
- [ ] Modify `Program.cs` to enter chat mode when no args
- [ ] Create `CopilotCommands` class for `na chat` and `na ask`
- [ ] Register new commands with System.CommandLine
- [ ] Write integration tests

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

- [ ] `na` with no args shows welcome banner and enters chat
- [ ] User can type messages and receive streaming responses
- [ ] `help` command shows capabilities
- [ ] `exit` command exits chat mode
- [ ] `na chat` and `na ask` commands work
- [ ] Graceful fallback when Copilot unavailable

## Duration Estimate

2-3 days
```

---

## Phase 3 Issue

**Title:** `[Phase 3] CLI Command Integration`

**Labels:** `enhancement`, `copilot-sdk`, `phase-3`

**Body:**

```markdown
## Goal

Expose all existing CLI commands as Copilot-callable tools.

**Parent:** #__ (Epic)
**Depends on:** #__ (Phase 2)

## Tasks

- [ ] Create `INotebookTools` interface
- [ ] Implement `NotebookTools` class with all tool registrations
- [ ] Create `ISystemMessageBuilder` interface
- [ ] Implement system message with tool context
- [ ] Register Vault tools (4 tools)
- [ ] Register Tag tools (7 tools)
- [ ] Register PDF tools (1 tool)
- [ ] Register Video tools (2 tools)
- [ ] Register Markdown tools (1 tool)
- [ ] Register Config tools (5 tools)
- [ ] Register OneDrive tools (1 tool)
- [ ] Write unit tests for each tool

## Tool Definitions

### Vault Tools
| Tool | Parameters | Description |
|------|------------|-------------|
| `vault_generate_index` | `path?` | Generate index files |
| `vault_ensure_metadata` | `path?` | Ensure metadata consistency |
| `vault_clean_index` | `path?` | Remove index files |
| `vault_sync` | `direction?` | Sync with OneDrive |

### Tag Tools
| Tool | Parameters | Description |
|------|------------|-------------|
| `tag_add_nested` | `path` | Add nested tags |
| `tag_consolidate` | `path?` | Consolidate duplicate tags |
| `tag_restructure` | `path?` | Restructure tags |
| `tag_update_frontmatter` | `path, key, value` | Update frontmatter |
| `tag_diagnose_yaml` | `path?` | Diagnose YAML issues |
| `tag_metadata_check` | `path?` | Check metadata |
| `tag_clean_index` | `path?` | Clean tags from index |

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

- [ ] All 21 tools registered and callable
- [ ] Natural language requests trigger correct tools
- [ ] Tool results display correctly in chat
- [ ] System message provides context about tools
- [ ] Existing command logic reused (no duplication)

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

**Parent:** #__ (Epic)
**Depends on:** #__ (Phase 3)

## Tasks

- [ ] Create `ISessionManager` interface
- [ ] Implement session save/load/list/purge
- [ ] Create `IUserPreferencesService` interface
- [ ] Implement user preferences storage
- [ ] Create `IFirstRunExperience` interface
- [ ] Implement first-run setup flow
- [ ] Create `IGitService` interface
- [ ] Implement Git repository detection
- [ ] Add `--resume` and `--session` options to `na chat`
- [ ] Implement `session` built-in commands
- [ ] Implement `na chat purge` subcommand
- [ ] Write unit tests

## Session Commands

| Command | Description |
|---------|-------------|
| `session save <name>` | Save current session |
| `session list` | List saved sessions |
| `session load <name>` | Load saved session |
| `session purge --older-than <period>` | Purge old sessions |
| `na chat --resume` | Resume last session |
| `na chat --session <name>` | Resume specific session |

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

- [ ] Sessions persist to `~/.notebookautomation/sessions/`
- [ ] `session save/list/load` commands work
- [ ] `--resume` resumes last session
- [ ] First-run shows on first launch
- [ ] Git repo detection works
- [ ] Session retention preference saved
- [ ] `na chat purge` cleans old sessions

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

**Parent:** #__ (Epic)
**Depends on:** #__ (Phase 4)

## Tasks

- [ ] Create `IInputHandler` interface with readline support
- [ ] Implement keyboard shortcuts (Ctrl+C, Up/Down, etc.)
- [ ] Implement multi-line input support
- [ ] Create `IFileAttachmentParser` for @file syntax
- [ ] Create `INetworkHandler` for offline detection
- [ ] Create `IRateLimitHandler` for quota management
- [ ] Create `IAccessibilityService` for high contrast/screen readers
- [ ] Create `ISessionLogger` for session logging
- [ ] Add `--high-contrast` and `--no-banner` options
- [ ] Implement `na chat --help` output
- [ ] Update documentation
- [ ] Final integration testing

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+C` | Cancel/Exit |
| `Ctrl+D` | Exit (EOF) |
| `↑` / `↓` | History navigation |
| `Ctrl+L` | Clear screen |
| `Tab` | Auto-complete |

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

## Files to Create

- `UI/InputHandler.cs`
- `UI/KeyboardHandler.cs`
- `UI/MultiLineInputHandler.cs`
- `Services/Copilot/FileAttachmentParser.cs`
- `Services/Copilot/NetworkHandler.cs`
- `Services/Copilot/RateLimitHandler.cs`
- `Services/Copilot/AccessibilityService.cs`
- `Services/Copilot/SessionLogger.cs`

## Acceptance Criteria

- [ ] Keyboard shortcuts work
- [ ] Multi-line input supported (triple backticks)
- [ ] @file attachment syntax works
- [ ] Offline mode handled gracefully
- [ ] Rate limiting shows user feedback
- [ ] High contrast mode works
- [ ] Session logging available
- [ ] Documentation updated
- [ ] All integration tests passing

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
