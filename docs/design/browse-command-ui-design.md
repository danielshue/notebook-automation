# Browse Command UI Design Proposal

## Overview

This document proposes a new **Browse** command for the Copilot chat UI that enables users to navigate and interact with files in their Obsidian Vault or OneDrive storage using a cursor-based, interactive console interface.

## User Story

> As a user of the Copilot UI, I want to issue a `browse` command that allows me to see files in either my Vault or OneDrive, navigate using keyboard/mouse, review files, and perform operations similar to the Obsidian plugin.

## Design Goals

1. **Interactive Navigation** - Tree-based or list-based file browsing with keyboard controls
2. **Dual Source Support** - Browse both local Vault and OneDrive files seamlessly
3. **File Operations** - Support common operations: read, create, edit, delete, search, tag
4. **Consistent UX** - Follow existing Spectre.Console patterns used in the codebase
5. **Extensibility** - Easy to add new operations and file sources

## UI Mockups

### Main Browse Menu

```
╭──────────────────────────────────────────────────────╮
│ 📁 File Browser                                       │
├──────────────────────────────────────────────────────┤
│ Source: [●] Vault  [○] OneDrive                      │
│ Path: /Learning Resources/Courses                    │
├──────────────────────────────────────────────────────┤
│ ▶ 📁 Azure                         [5 items]         │
│   📁 Python                        [12 items]        │
│   📁 JavaScript                    [8 items]         │
│   📄 Course Index.md               2.3 KB            │
│   📄 Learning Plan.md              1.1 KB            │
├──────────────────────────────────────────────────────┤
│ [↑↓] Navigate  [Enter] Open  [←] Back  [→] Enter    │
│ [r] Read  [e] Edit  [d] Delete  [t] Tags  [q] Quit  │
╰──────────────────────────────────────────────────────╯
```

### File Preview Panel

```
╭──────────────────────────────────────────────────────╮
│ 📄 Course Index.md                                   │
├──────────────────────────────────────────────────────┤
│ Path: /Learning Resources/Courses/Course Index.md    │
│ Size: 2.3 KB  |  Modified: 2025-01-20 14:32         │
│ Tags: #course, #index, #learning                    │
├──────────────────────────────────────────────────────┤
│ ---                                                  │
│ title: Course Index                                  │
│ tags: [course, index, learning]                     │
│ ---                                                  │
│                                                      │
│ # Course Index                                       │
│                                                      │
│ This document provides an overview of all courses... │
├──────────────────────────────────────────────────────┤
│ [e] Edit  [t] Manage Tags  [c] Copy Path  [q] Back  │
╰──────────────────────────────────────────────────────╯
```

### Operations Menu (Context Actions)

```
╭─────────────────────────────────────╮
│ Actions for: Course Index.md       │
├─────────────────────────────────────┤
│ ▶ Read/Preview                     │
│   Edit in Editor                   │
│   ───────────────────              │
│   Manage Tags                      │
│   View/Edit Metadata               │
│   ───────────────────              │
│   Copy Path                        │
│   Create Share Link (OneDrive)     │
│   ───────────────────              │
│   Delete                           │
│   ───────────────────              │
│   Send to Copilot                  │
╰─────────────────────────────────────╯
```

## Command Syntax

```
browse                    # Browse default source (Vault)
browse vault              # Browse Vault files
browse vault [path]       # Browse Vault at specific path
browse onedrive           # Browse OneDrive files
browse onedrive [path]    # Browse OneDrive at specific path
browse search <query>     # Search across sources
```

## Architecture

### Component Structure

```
NotebookAutomation.Cli/
├── UI/
│   ├── ChatModeUI.cs (existing)
│   ├── FileBrowserUI.cs (new - main browser UI)
│   └── Components/
│       ├── FileListComponent.cs (new - renders file lists)
│       ├── FilePreviewComponent.cs (new - renders file preview)
│       └── FileOperationsMenu.cs (new - context menu)
├── Services/
│   └── Copilot/
│       └── ChatBuiltInCommands.cs (modified - add browse command)
```

### Key Classes

#### IFileBrowserSource Interface

```csharp
public interface IFileBrowserSource
{
    string SourceName { get; }
    string CurrentPath { get; }
    Task<BrowseResult<DirectoryListing>> ListDirectoryAsync(string path);
    Task<BrowseResult<FileContent>> ReadFileAsync(string path);
    Task<BrowseResult> CreateFileAsync(string path, string content);
    Task<BrowseResult> DeleteFileAsync(string path);
    Task<IReadOnlyList<string>> GetTagsAsync(string path);
    Task<BrowseResult> UpdateTagsAsync(string path, IReadOnlyList<string> tags);
}
```

#### VaultBrowserSource (wraps existing VaultBrowserService)

```csharp
public class VaultBrowserSource : IFileBrowserSource
{
    private readonly IVaultBrowserService _vaultBrowser;
    // Implementation wrapping VaultBrowserService
}
```

#### OneDriveBrowserSource (new)

```csharp
public class OneDriveBrowserSource : IFileBrowserSource
{
    private readonly IOneDriveService _oneDriveService;
    // Implementation wrapping OneDriveService
}
```

#### FileBrowserUI

```csharp
public class FileBrowserUI
{
    private readonly IFileBrowserSource _source;
    private readonly ILogger<FileBrowserUI> _logger;

    public async Task<BrowseSession> RunAsync(
        string? initialPath = null,
        CancellationToken cancellationToken = default);
}
```

### Integration with ChatBuiltInCommands

Add new command handling in `ChatBuiltInCommands.cs`:

```csharp
// In IsBuiltInCommand method
_ when command.StartsWith("browse") => true,

// In ExecuteAsync method
if (command == "browse" || command.StartsWith("browse "))
{
    await HandleBrowseCommandAsync(command, session, cancellationToken);
    return false;
}
```

## Keyboard Controls

| Key                | Action                                   |
| ------------------ | ---------------------------------------- |
| `↑` / `↓`          | Navigate up/down in file list            |
| `Enter` or `→`     | Open folder / Select file                |
| `←` or `Backspace` | Go to parent directory                   |
| `r`                | Read/Preview selected file               |
| `e`                | Edit selected file                       |
| `d`                | Delete selected file (with confirmation) |
| `t`                | Manage tags for selected file            |
| `n`                | Create new file in current directory     |
| `s`                | Search in current directory              |
| `/`                | Quick search/filter                      |
| `Tab`              | Switch between Vault and OneDrive        |
| `c`                | Copy path to clipboard                   |
| `l`                | Create share link (OneDrive only)        |
| `q` or `Esc`       | Exit browser / Go back                   |
| `?`                | Show help                                |

## File Operations

### Vault Operations (using existing VaultBrowserService)

- List directories and files
- Read note content (with frontmatter parsing)
- Create new notes
- Update note content
- Delete notes
- Get/update tags via frontmatter
- Get note metadata

### OneDrive Operations (using existing OneDriveService)

- List files and folders
- Download file content for preview
- Create share links
- Search files
- Navigate folder hierarchy

## Implementation Phases

### Phase 1: Core Browser UI

- [ ] Create `FileBrowserUI` with basic navigation
- [ ] Implement `VaultBrowserSource` wrapping `VaultBrowserService`
- [ ] Add `browse vault` command to `ChatBuiltInCommands`
- [ ] Basic file listing and navigation

### Phase 2: File Operations

- [ ] File preview/read functionality
- [ ] Tag management integration
- [ ] File creation and deletion
- [ ] Edit file (launch external editor)

### Phase 3: OneDrive Integration

- [ ] Implement `OneDriveBrowserSource`
- [ ] Add `browse onedrive` command
- [ ] Share link creation
- [ ] Tab switching between sources

### Phase 4: Advanced Features

- [ ] Search functionality
- [ ] Quick filter (type to filter)
- [ ] Breadcrumb navigation
- [ ] Recent files list
- [ ] Favorites/bookmarks

## Data Models

```csharp
public record BrowseItem(
    string Name,
    string Path,
    bool IsDirectory,
    long? SizeBytes,
    DateTime? LastModified,
    IReadOnlyList<string>? Tags = null);

public record DirectoryListing(
    string CurrentPath,
    IReadOnlyList<BrowseItem> Items,
    bool HasParent);

public record FileContent(
    BrowseItem Info,
    string Content,
    Dictionary<string, object>? Frontmatter = null,
    string? Body = null);

public record BrowseSession(
    IFileBrowserSource Source,
    string? SelectedPath = null,
    BrowseAction LastAction = BrowseAction.None);

public enum BrowseAction
{
    None,
    Selected,
    Cancelled,
    SwitchSource
}
```

## Dependencies

- **Spectre.Console** - Already used for UI components (SelectionPrompt, Table, Panel)
- **VaultBrowserService** - Existing service for vault operations
- **OneDriveService** - Existing service for OneDrive operations
- **TagService** - Existing service for tag management

## Testing Strategy

1. **Unit Tests**
   - Test `VaultBrowserSource` wrapper
   - Test `OneDriveBrowserSource` wrapper
   - Test navigation state management

2. **Integration Tests**
   - Test command parsing in `ChatBuiltInCommands`
   - Test file operations end-to-end

3. **Manual Testing**
   - UI rendering and responsiveness
   - Keyboard navigation
   - Error handling (permissions, network, etc.)

## Security Considerations

- Confirm destructive operations (delete)
- Sanitize displayed content to prevent markup injection
- Respect OneDrive permissions
- Don't expose full paths in error messages

## Future Enhancements

- Multi-select for batch operations
- Drag-and-drop simulation (multi-item move)
- Split view (Vault ↔ OneDrive)
- Custom file type handlers
- Plugin system for additional operations
