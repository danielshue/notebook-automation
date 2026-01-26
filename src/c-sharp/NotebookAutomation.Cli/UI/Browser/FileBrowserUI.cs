// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

using Terminal.Gui;

using TGuiColor = Terminal.Gui.Color;

namespace NotebookAutomation.Cli.UI.Browser;

/// <summary>
/// Interactive file browser UI using Terminal.Gui v2.
/// </summary>
/// <remarks>
/// Provides a rich TUI for browsing and manipulating files in various sources (Vault, OneDrive, etc.).
/// </remarks>
public class FileBrowserUI
{
    private readonly IFileBrowserSource _source;
    private readonly ILogger<FileBrowserUI> _logger;
    private readonly FileBrowserState _state;
    private readonly FileBrowserOptions _options;

    private Window? _mainWindow;
    private ListView? _fileListView;
    private TextView? _previewTextView;
    private Label? _statusLabel;
    private Label? _pathLabel;
    private FrameView? _browserFrame;
    private FrameView? _previewFrame;
    private BrowseAction _resultAction = BrowseAction.None;
    private readonly TaskCompletionSource<BrowseSession> _completionSource = new();

    /// <summary>
    /// Initializes a new instance of <see cref="FileBrowserUI"/>.
    /// </summary>
    /// <param name="source">The file browser source.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">Browser options.</param>
    public FileBrowserUI(
        IFileBrowserSource source,
        ILogger<FileBrowserUI> logger,
        FileBrowserOptions? options = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new FileBrowserOptions();
        _state = new FileBrowserState
        {
            CurrentPath = _options.InitialPath ?? string.Empty
        };
    }

    /// <summary>
    /// Runs the interactive file browser.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The browse session result.</returns>
    public async Task<BrowseSession> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Load initial directory
            await RefreshDirectoryAsync(cancellationToken);

            // Initialize Terminal.Gui with mouse support
            Application.Init();

            try
            {
                CreateUI();
                Application.Run(_mainWindow!);
                return await _completionSource.Task;
            }
            finally
            {
                Application.Shutdown();

                // Fully reset console state after Terminal.Gui shutdown
                Console.ResetColor();
                Console.CursorVisible = true;
                Console.OutputEncoding = System.Text.Encoding.UTF8;

                // Clear any remaining Terminal.Gui artifacts
                Console.Clear();
            }
        }
        catch (OperationCanceledException)
        {
            return new BrowseSession(_source, null, BrowseAction.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in file browser");
            return new BrowseSession(_source, null, BrowseAction.Cancelled);
        }
    }

    /// <summary>
    /// Creates the Terminal.Gui UI components.
    /// </summary>
    private void CreateUI()
    {
        _mainWindow = new Window()
        {
            Title = $" {_source.SourceName} Browser",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = CreateColorScheme()
        };

        // Menu bar at the top
        var menuBar = CreateMenuBar();
        _mainWindow.Add(menuBar);

        // Breadcrumb navigation bar below menu
        _pathLabel = new Label
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(1),
            Height = 1,
            Text = GetCurrentPathDisplay(),
            ColorScheme = new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(TGuiColor.BrightCyan, TGuiColor.Blue),
                Focus = new Terminal.Gui.Attribute(TGuiColor.BrightCyan, TGuiColor.Blue)
            }
        };
        _mainWindow.Add(_pathLabel);

        // Browser frame (left side - always visible)
        _browserFrame = new FrameView()
        {
            Title = "[ Files ]",
            X = 0,
            Y = 2,
            Width = Dim.Percent(50),
            Height = Dim.Fill(2)
        };

        // File list view
        _fileListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            AllowsMarking = false,
            CanFocus = true
        };

        UpdateFileList();
        _fileListView.SelectedItemChanged += OnFileListSelectionChanged;
        _fileListView.OpenSelectedItem += OnFileListOpenItem;

        _browserFrame.Add(_fileListView);
        _mainWindow.Add(_browserFrame);

        // Preview frame (right side - initially showing instructions)
        _previewFrame = new FrameView()
        {
            Title = "[ Preview ]",
            X = Pos.Right(_browserFrame),
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            Visible = true
        };

        _previewTextView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            Text = "\n  Select a file to preview...\n\n  Keyboard Shortcuts:\n  ─────────────────────\n  r - Read/Preview\n  e - Edit\n  d - Delete\n  t - Manage Tags\n  n - New File\n  q - Quit\n  Tab - Switch Source\n  F5 - Refresh"
        };

        _previewFrame.Add(_previewTextView);
        _mainWindow.Add(_previewFrame);

        // Classic Turbo Pascal status bar at bottom
        _statusLabel = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            Text = GetStatusText(),
            ColorScheme = new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(TGuiColor.Black, TGuiColor.Cyan),
                Focus = new Terminal.Gui.Attribute(TGuiColor.Black, TGuiColor.Cyan)
            }
        };
        _mainWindow.Add(_statusLabel);

        // Keyboard shortcuts
        SetupKeyBindings();
    }

    /// <summary>
    /// Creates the classic Turbo Pascal color scheme for the browser.
    /// </summary>
    private ColorScheme CreateColorScheme()
    {
        return new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(TGuiColor.Yellow, TGuiColor.Blue),
            Focus = new Terminal.Gui.Attribute(TGuiColor.White, TGuiColor.Cyan),
            HotNormal = new Terminal.Gui.Attribute(TGuiColor.BrightYellow, TGuiColor.Blue),
            HotFocus = new Terminal.Gui.Attribute(TGuiColor.BrightYellow, TGuiColor.Cyan),
            Disabled = new Terminal.Gui.Attribute(TGuiColor.DarkGray, TGuiColor.Blue)
        };
    }

    /// <summary>
    /// Creates the menu bar for the browser.
    /// </summary>
    private MenuBar CreateMenuBar()
    {
        var menuBar = new MenuBar
        {
            Menus =
            [
                new MenuBarItem("_File", new MenuItem[]
                {
                    new("_New File (n)", "Create a new file in the current directory", () => _ = CreateNewFileAsync()),
                    new("_Open/Preview (r)", "Preview the selected file", () => _ = PreviewSelectedFileAsync()),
                    new("_Edit (e)", "Edit the selected file", () => _ = EditSelectedFileAsync()),
                    null!, // Separator
                    new("_Delete (d)", "Delete the selected file", () => _ = DeleteSelectedFileAsync()),
                    null!, // Separator
                    new("E_xit (q)", "Close the browser", () => ExitBrowser(BrowseAction.Cancelled))
                }),
                new MenuBarItem("_View", new MenuItem[]
                {
                    new("_Refresh (F5)", "Refresh the current directory", () => _ = RefreshDirectoryAsync()),
                    null!, // Separator
                    new("Exit _Preview (Esc)", "Return to file browser", ExitPreviewMode,
                        () => _state.IsPreviewMode),
                }),
                new MenuBarItem("_Navigate", new MenuItem[]
                {
                    new("_Back (←)", "Go to parent directory", () => _ = NavigateBackAsync()),
                    new("_Switch Source (Tab)", "Switch between Vault and OneDrive", () => ExitBrowser(BrowseAction.SwitchSource)),
                }),
                new MenuBarItem("_Actions", new MenuItem[]
                {
                    new("_Tags (t)", "Manage tags for selected file", () => _ = ManageTagsAsync()),
                    new("_Copy Path (c)", "Copy file path to clipboard", CopyPathToClipboard,
                        () => _state.IsPreviewMode),
                    new("_Send to Copilot (s)", "Send file to Copilot for analysis", () => ExitBrowser(BrowseAction.SendToCopilot),
                        () => _state.IsPreviewMode),
                }),
                new MenuBarItem("_Help", new MenuItem[]
                {
                    new("_Keyboard Shortcuts", "Show keyboard shortcuts", ShowKeyboardHelp),
                    null!, // Separator
                    new("_About", "About this browser", ShowAbout)
                })
            ]
        };

        return menuBar;
    }

    /// <summary>
    /// Shows keyboard shortcuts help dialog.
    /// </summary>
    private void ShowKeyboardHelp()
    {
        var help = @"Keyboard Shortcuts:

Navigation:
  ↑↓        Navigate file list
  Enter     Open folder or preview file
  ←/Bksp    Go to parent directory
  Tab       Switch between Vault and OneDrive

File Operations:
  r         Read/Preview file
  e         Edit file in external editor
  d         Delete file (with confirmation)
  n         Create new file
  t         Manage tags

Preview Mode:
  ↑↓        Scroll content
  PgUp/PgDn Page up/down
  c         Copy file path to clipboard
  s         Send to Copilot
  q/Esc     Exit preview

General:
  q/Esc     Quit browser (when not in preview)
  F5        Refresh directory";

        MessageBox.Query("Keyboard Shortcuts", help, "OK");
    }

    /// <summary>
    /// Shows about dialog.
    /// </summary>
    private void ShowAbout()
    {
        var about = $@"
    ██████╗ ██████╗ ██████╗ ██╗██╗      ██████╗ ████████╗
   ██╔════╝██╔═══██╗██╔══██╗██║██║     ██╔═══██╗╚══██╔══╝
   ██║     ██║   ██║██████╔╝██║██║     ██║   ██║   ██║   
   ██║     ██║   ██║██╔═══╝ ██║██║     ██║   ██║   ██║   
   ╚██████╗╚██████╔╝██║     ██║███████╗╚██████╔╝   ██║   
    ╚═════╝ ╚═════╝ ╚═╝     ╚═╝╚══════╝ ╚═════╝    ╚═╝   

            Notebook Automation Browser v1.0
            
            Powered by GitHub Copilot
            
Source: {_source.SourceName}
Path: {(_state.CurrentPath == string.Empty ? "/" : _state.CurrentPath)}

A powerful file browser for the Notebook Automation system.";

        MessageBox.Query("About", about, "OK");
    }

    /// <summary>
    /// Sets up keyboard bindings.
    /// </summary>
    private void SetupKeyBindings()
    {
        if (_mainWindow == null)
        {
            return;
        }

        // Q or Escape to quit
        _mainWindow.KeyDown += (sender, e) =>
        {
            var key = (char)e;
            if (e == Key.Esc || key == 'q' || key == 'Q')
            {
                if (_state.IsPreviewMode)
                {
                    ExitPreviewMode();
                }
                else
                {
                    ExitBrowser(BrowseAction.Cancelled);
                }
            }
            else if (e == Key.Tab)
            {
                ExitBrowser(BrowseAction.SwitchSource);
            }
            else if (key == 'r' || key == 'R')
            {
                _ = PreviewSelectedFileAsync();
            }
            else if (key == 'e' || key == 'E')
            {
                _ = EditSelectedFileAsync();
            }
            else if (key == 'd' || key == 'D')
            {
                _ = DeleteSelectedFileAsync();
            }
            else if (key == 't' || key == 'T')
            {
                _ = ManageTagsAsync();
            }
            else if (key == 'n' || key == 'N')
            {
                _ = CreateNewFileAsync();
            }
            else if (key == 's' || key == 'S')
            {
                if (_state.IsPreviewMode)
                {
                    ExitBrowser(BrowseAction.SendToCopilot);
                }
            }
            else if (key == 'c' || key == 'C')
            {
                if (_state.IsPreviewMode)
                {
                    CopyPathToClipboard();
                }
            }
            else if (e == Key.Backspace || e == Key.CursorLeft)
            {
                if (!_state.IsPreviewMode)
                {
                    _ = NavigateBackAsync();
                }
            }
            else if (e == Key.F5)
            {
                _ = RefreshDirectoryAsync();
            }
        };
    }

    /// <summary>
    /// Gets the current path display string.
    /// </summary>
    private string GetCurrentPathDisplay()
    {
        var path = string.IsNullOrEmpty(_state.CurrentPath) ? "/" : $"/{_state.CurrentPath}";
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            return " ROOT";
        }

        return " ROOT \\\\ " + string.Join(" \\\\ ", segments);
    }

    /// <summary>
    /// Gets the status bar text.
    /// </summary>
    private string GetStatusText()
    {
        var itemCount = _state.CurrentListing?.Items.Count ?? 0;
        var selectedIndex = _fileListView?.SelectedItem ?? 0;
        var totalWithParent = _state.CurrentListing?.HasParent == true ? itemCount + 1 : itemCount;

        var position = totalWithParent > 0 ? $" [{selectedIndex + 1}/{totalWithParent}]" : " [0/0]";

        if (_state.IsPreviewMode)
        {
            return $" {position} | ↑↓ Scroll | PgUp/PgDn Page | e Edit | t Tags | c Copy | s Send to Copilot | Esc Exit Preview | q Quit ";
        }

        return $" {position} | ↑↓ Navigate | Enter Open | ← Back | r Preview | e Edit | d Delete | t Tags | n New | Tab Switch | F5 Refresh | q Quit ";
    }

    /// <summary>
    /// Updates the file list with current directory contents.
    /// </summary>
    private void UpdateFileList()
    {
        if (_fileListView == null || _state.CurrentListing == null)
        {
            return;
        }

        var items = new List<string>();

        // Add parent directory if not at root
        if (_state.CurrentListing.HasParent)
        {
            items.Add("<DIR> ..");
        }

        // Add all items
        foreach (var item in _state.CurrentListing.Items)
        {
            var icon = item.IsDirectory ? "<DIR>" : GetFileIcon(item.Name);
            var size = item.IsDirectory
                ? (item.ItemCount.HasValue ? $"({item.ItemCount} items)" : "")
                : (item.SizeFormatted ?? "");

            items.Add($"{icon} {item.Name} {size}");
        }

        _fileListView.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(items));

        if (_state.SelectedIndex >= 0 && _state.SelectedIndex < items.Count)
        {
            _fileListView.SelectedItem = _state.SelectedIndex;
        }
    }

    /// <summary>
    /// Handles file list selection changes.
    /// </summary>
    private void OnFileListSelectionChanged(object? sender, ListViewItemEventArgs args)
    {
        var offset = _state.CurrentListing?.HasParent == true ? 1 : 0;
        _state.SelectedIndex = args.Item - offset;
    }

    /// <summary>
    /// Handles file list open item (Enter key).
    /// </summary>
    private void OnFileListOpenItem(object? sender, ListViewItemEventArgs args)
    {
        _ = HandleEnterAsync();
    }

    /// <summary>
    /// Handles Enter key - navigate into directory or preview file.
    /// </summary>
    private async Task HandleEnterAsync()
    {
        // Handle parent directory selection
        if (_state.CurrentListing?.HasParent == true && _state.SelectedIndex == -1)
        {
            await NavigateBackAsync();
            return;
        }

        var selected = _state.SelectedItem;
        if (selected == null)
        {
            return;
        }

        if (selected.IsDirectory)
        {
            // Navigate into directory
            _state.NavigationHistory.Push(_state.CurrentPath);
            _state.CurrentPath = selected.Path;
            _state.SelectedIndex = 0;
            await RefreshDirectoryAsync();
            UpdateFileList();
            UpdatePathDisplay();
        }
        else
        {
            // Preview file
            if (_options.AllowFileSelection)
            {
                await PreviewSelectedFileAsync();
            }
        }
    }

    /// <summary>
    /// Navigates to the parent directory.
    /// </summary>
    private async Task NavigateBackAsync()
    {
        if (_state.NavigationHistory.Count > 0)
        {
            _state.CurrentPath = _state.NavigationHistory.Pop();
            _state.SelectedIndex = 0;
            await RefreshDirectoryAsync();
        }
        else if (!string.IsNullOrEmpty(_state.CurrentPath))
        {
            // Go to parent by path manipulation
            var lastSeparator = _state.CurrentPath.LastIndexOfAny(['/', '\\']);
            _state.CurrentPath = lastSeparator > 0
                ? _state.CurrentPath[..lastSeparator]
                : string.Empty;
            _state.SelectedIndex = 0;
            await RefreshDirectoryAsync();
        }

        UpdateFileList();
        UpdatePathDisplay();
    }

    /// <summary>
    /// Previews the selected file.
    /// </summary>
    private async Task PreviewSelectedFileAsync()
    {
        var selected = _state.SelectedItem;
        if (selected == null || selected.IsDirectory)
        {
            return;
        }

        var result = await _source.ReadFileAsync(selected.Path, CancellationToken.None);
        if (result.IsSuccess && result.Value != null)
        {
            _state.PreviewContent = result.Value;
            _state.IsPreviewMode = true;
            ShowPreview();
        }
        else
        {
            ShowMessage($"Error: {result.Error}", "Error");
        }
    }

    /// <summary>
    /// Shows the preview panel.
    /// </summary>
    private void ShowPreview()
    {
        if (_previewTextView == null || _previewFrame == null || _browserFrame == null || _state.PreviewContent == null)
        {
            return;
        }

        // Update frame titles to highlight preview mode
        var info = _state.PreviewContent.Info;
        _previewFrame.Title = $"[ {info.Name} - {info.SizeFormatted ?? "?"} ]";
        _browserFrame.Title = "[ Files ]";

        // Set content - convert markdown to plain text for Terminal.Gui display
        var content = _state.PreviewContent.Body ?? _state.PreviewContent.Content;
        _previewTextView.Text = MarkdownRenderer.ToPlainText(content);

        // Update status
        if (_statusLabel != null)
        {
            _statusLabel.Text = GetStatusText();
        }

        _previewTextView.SetFocus();
    }

    /// <summary>
    /// Exits preview mode.
    /// </summary>
    private void ExitPreviewMode()
    {
        _state.IsPreviewMode = false;
        _state.PreviewContent = null;

        if (_browserFrame != null && _previewFrame != null && _previewTextView != null)
        {
            // Reset titles
            _previewFrame.Title = "📄 Preview";
            _browserFrame.Title = "📋 Files";

            // Clear preview content and show helpful instructions
            _previewTextView.Text = "\n  Select a file to preview...\n\n  Keyboard Shortcuts:\n  ─────────────────────\n  r - Read/Preview\n  e - Edit\n  d - Delete\n  t - Manage Tags\n  n - New File\n  q - Quit\n  Tab - Switch Source\n  F5 - Refresh";
        }

        if (_statusLabel != null)
        {
            _statusLabel.Text = GetStatusText();
        }

        _fileListView?.SetFocus();
    }

    /// <summary>
    /// Opens selected file in external editor.
    /// </summary>
    private async Task EditSelectedFileAsync()
    {
        var selected = _state.SelectedItem;
        if (selected == null || selected.IsDirectory)
        {
            return;
        }

        await LaunchEditorAsync(selected.Path);
    }

    /// <summary>
    /// Launches an external editor.
    /// </summary>
    private Task LaunchEditorAsync(string path)
    {
        try
        {
            ShowMessage("Opening file in editor...", "Editor");

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            };

            System.Diagnostics.Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open editor for {Path}", path);
            ShowMessage($"Could not open editor: {ex.Message}", "Error");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes the selected file with confirmation.
    /// </summary>
    private async Task DeleteSelectedFileAsync()
    {
        var selected = _state.SelectedItem;
        if (selected == null || selected.IsDirectory)
        {
            return;
        }

        if (!_source.SupportsFileDeletion)
        {
            ShowMessage("This source does not support file deletion", "Not Supported");
            return;
        }

        var confirmed = ShowConfirmation($"Delete {selected.Name}?", "Confirm Delete");
        if (!confirmed)
        {
            return;
        }

        var result = await _source.DeleteFileAsync(selected.Path, CancellationToken.None);
        if (result.IsSuccess)
        {
            ShowMessage("File deleted", "Success");
            await RefreshDirectoryAsync();
            UpdateFileList();
        }
        else
        {
            ShowMessage($"Error: {result.Error}", "Error");
        }
    }

    /// <summary>
    /// Manages tags for the selected file.
    /// </summary>
    private async Task ManageTagsAsync()
    {
        var path = _state.IsPreviewMode
            ? _state.PreviewContent?.Info.Path
            : _state.SelectedItem?.Path;

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (!_source.SupportsTagManagement)
        {
            ShowMessage("This source does not support tag management", "Not Supported");
            return;
        }

        var currentTags = await _source.GetTagsAsync(path, CancellationToken.None);
        var tagsInput = ShowInputDialog(
            $"Current tags: {string.Join(", ", currentTags)}\n\nEnter new tags (comma-separated):",
            "Manage Tags",
            string.Join(", ", currentTags));

        if (tagsInput == null)
        {
            return;
        }

        var newTags = tagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().TrimStart('#'))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToList();

        var result = await _source.UpdateTagsAsync(path, newTags, CancellationToken.None);
        if (result.IsSuccess)
        {
            ShowMessage("Tags updated", "Success");

            // Refresh preview if in preview mode
            if (_state.IsPreviewMode && _state.PreviewContent != null)
            {
                var refreshResult = await _source.ReadFileAsync(path, CancellationToken.None);
                if (refreshResult.IsSuccess)
                {
                    _state.PreviewContent = refreshResult.Value;
                    ShowPreview();
                }
            }
        }
        else
        {
            ShowMessage($"Error: {result.Error}", "Error");
        }
    }

    /// <summary>
    /// Creates a new file in the current directory.
    /// </summary>
    private async Task CreateNewFileAsync()
    {
        if (!_source.SupportsFileCreation)
        {
            ShowMessage("This source does not support file creation", "Not Supported");
            return;
        }

        var fileName = ShowInputDialog("Enter file name (without .md):", "New File", string.Empty);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var title = ShowInputDialog("Enter title (optional):", "File Title", string.Empty) ?? string.Empty;
        var tagsInput = ShowInputDialog("Enter tags (comma-separated, optional):", "Tags", string.Empty) ?? string.Empty;

        var tags = tagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().TrimStart('#'))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        var content = new StringBuilder();
        content.AppendLine("---");
        if (!string.IsNullOrWhiteSpace(title))
        {
            content.AppendLine($"title: {title}");
        }
        content.AppendLine($"created: {DateTime.Now:yyyy-MM-dd}");
        if (tags.Count > 0)
        {
            content.AppendLine("tags:");
            foreach (var tag in tags)
            {
                content.AppendLine($"  - {tag}");
            }
        }
        content.AppendLine("---");
        content.AppendLine();
        content.AppendLine($"# {(string.IsNullOrWhiteSpace(title) ? fileName : title)}");
        content.AppendLine();

        var path = string.IsNullOrEmpty(_state.CurrentPath)
            ? $"{fileName}.md"
            : $"{_state.CurrentPath}/{fileName}.md";

        var result = await _source.CreateFileAsync(path, content.ToString(), CancellationToken.None);

        if (result.IsSuccess)
        {
            ShowMessage($"Created: {fileName}.md", "Success");
            await RefreshDirectoryAsync();
            UpdateFileList();
        }
        else
        {
            ShowMessage($"Error: {result.Error}", "Error");
        }
    }

    /// <summary>
    /// Copies the current file path to clipboard.
    /// </summary>
    private void CopyPathToClipboard()
    {
        var path = _state.PreviewContent?.Info.Path ?? _state.SelectedItem?.Path;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-command \"Set-Clipboard -Value '{path.Replace("'", "''")}'\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                System.Diagnostics.Process.Start(startInfo)?.WaitForExit();
            }

            ShowMessage("Path copied to clipboard", "Success");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to copy path to clipboard");
            ShowMessage($"Could not copy to clipboard: {ex.Message}", "Error");
        }
    }

    /// <summary>
    /// Refreshes the current directory listing.
    /// </summary>
    private async Task RefreshDirectoryAsync(CancellationToken cancellationToken = default)
    {
        var result = await _source.ListDirectoryAsync(_state.CurrentPath, cancellationToken);

        if (result.IsSuccess)
        {
            _state.CurrentListing = result.Value;
            _state.SelectedIndex = Math.Min(_state.SelectedIndex, _state.CurrentListing!.Items.Count - 1);
            if (_state.SelectedIndex < 0 && _state.CurrentListing.Items.Count > 0)
            {
                _state.SelectedIndex = 0;
            }
        }
        else
        {
            _logger.LogError("Failed to list directory: {Error}", result.Error);
        }
    }

    /// <summary>
    /// Updates the path display label.
    /// </summary>
    private void UpdatePathDisplay()
    {
        if (_pathLabel != null)
        {
            _pathLabel.Text = GetCurrentPathDisplay();
        }
    }

    /// <summary>
    /// Exits the browser with the specified action.
    /// </summary>
    private void ExitBrowser(BrowseAction action)
    {
        _resultAction = action;
        var selectedPath = action == BrowseAction.SendToCopilot
            ? (_state.PreviewContent?.Info.Path ?? _state.SelectedItem?.Path)
            : _state.SelectedItem?.Path;

        _completionSource.TrySetResult(new BrowseSession(_source, selectedPath, action));
        Application.RequestStop();
    }

    /// <summary>
    /// Shows a message dialog.
    /// </summary>
    private void ShowMessage(string message, string title)
    {
        MessageBox.Query(title, message, "OK");
    }

    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    private bool ShowConfirmation(string message, string title)
    {
        var result = MessageBox.Query(title, message, "Yes", "No");
        return result == 0;
    }

    /// <summary>
    /// Shows an input dialog.
    /// </summary>
    private string? ShowInputDialog(string message, string title, string defaultValue)
    {
        var input = defaultValue;
        var ok = false;

        var dialog = new Dialog()
        {
            Title = title,
            Width = 60,
            Height = 10,
            ColorScheme = new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(TGuiColor.Yellow, TGuiColor.Blue),
                Focus = new Terminal.Gui.Attribute(TGuiColor.White, TGuiColor.Cyan),
                HotNormal = new Terminal.Gui.Attribute(TGuiColor.BrightYellow, TGuiColor.Blue),
                HotFocus = new Terminal.Gui.Attribute(TGuiColor.BrightYellow, TGuiColor.Cyan)
            }
        };

        var label = new Label()
        {
            Text = message,
            X = 1,
            Y = 1,
            Width = Dim.Fill(1)
        };

        var textField = new TextField()
        {
            Text = defaultValue,
            X = 1,
            Y = 3,
            Width = Dim.Fill(1)
        };

        var okButton = new Button()
        {
            Text = "OK",
            X = Pos.Center() - 10,
            Y = Pos.AnchorEnd(1),
            IsDefault = true
        };

        okButton.Accepting += (sender, e) =>
        {
            input = textField.Text.ToString() ?? string.Empty;
            ok = true;
            Application.RequestStop();
        };

        var cancelButton = new Button()
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.AnchorEnd(1)
        };

        cancelButton.Accepting += (sender, e) =>
        {
            Application.RequestStop();
        };

        dialog.Add(label, textField, okButton, cancelButton);
        Application.Run(dialog);

        return ok ? input : null;
    }

    /// <summary>
    /// Gets an icon for a file based on extension.
    /// </summary>
    private static string GetFileIcon(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".md" => "<MD>",
            ".txt" => "<TXT>",
            ".pdf" => "<PDF>",
            ".json" => "<JSN>",
            ".yaml" or ".yml" => "<YML>",
            ".jpg" or ".jpeg" or ".png" or ".gif" => "<IMG>",
            ".mp4" or ".mkv" or ".avi" => "<VID>",
            ".mp3" or ".wav" or ".flac" => "<SND>",
            _ => "     "
        };
    }
}
