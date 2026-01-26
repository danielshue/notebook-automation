// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.UI.Browser;

/// <summary>
/// Represents an item (file or directory) in the file browser.
/// </summary>
/// <param name="Name">The display name of the item.</param>
/// <param name="Path">The full path within the source.</param>
/// <param name="IsDirectory">Whether this is a directory.</param>
/// <param name="SizeBytes">The size in bytes (null for directories).</param>
/// <param name="SizeFormatted">The formatted size string.</param>
/// <param name="LastModified">The last modified timestamp.</param>
/// <param name="Tags">Optional tags associated with the item.</param>
/// <param name="ItemCount">Number of items in directory (directories only).</param>
public record BrowseItem(
    string Name,
    string Path,
    bool IsDirectory,
    long? SizeBytes = null,
    string? SizeFormatted = null,
    DateTime? LastModified = null,
    IReadOnlyList<string>? Tags = null,
    int? ItemCount = null);

/// <summary>
/// Represents the contents of a directory.
/// </summary>
/// <param name="CurrentPath">The current directory path.</param>
/// <param name="Items">The items in the directory.</param>
/// <param name="HasParent">Whether this directory has a parent (not root).</param>
public record DirectoryListing(
    string CurrentPath,
    IReadOnlyList<BrowseItem> Items,
    bool HasParent);

/// <summary>
/// Represents the content of a file.
/// </summary>
/// <param name="Info">Basic item information.</param>
/// <param name="Content">The raw file content.</param>
/// <param name="Frontmatter">Parsed frontmatter dictionary (for markdown files).</param>
/// <param name="Body">Content without frontmatter.</param>
public record FileContent(
    BrowseItem Info,
    string Content,
    Dictionary<string, object>? Frontmatter = null,
    string? Body = null);

/// <summary>
/// Represents the result of a browse session.
/// </summary>
/// <param name="Source">The browser source used.</param>
/// <param name="SelectedPath">The path selected by the user, if any.</param>
/// <param name="LastAction">The last action taken.</param>
public record BrowseSession(
    IFileBrowserSource Source,
    string? SelectedPath = null,
    BrowseAction LastAction = BrowseAction.None);

/// <summary>
/// Actions that can result from a browse session.
/// </summary>
public enum BrowseAction
{
    /// <summary>
    /// No action taken.
    /// </summary>
    None,

    /// <summary>
    /// User selected a file.
    /// </summary>
    Selected,

    /// <summary>
    /// User cancelled the browse operation.
    /// </summary>
    Cancelled,

    /// <summary>
    /// User requested to switch to a different source.
    /// </summary>
    SwitchSource,

    /// <summary>
    /// User sent a file to Copilot for processing.
    /// </summary>
    SendToCopilot
}

/// <summary>
/// Result wrapper for browse operations.
/// </summary>
public class BrowseResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static BrowseResult Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static BrowseResult Failure(string error) => new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Result wrapper for browse operations with a value.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public class BrowseResult<T> : BrowseResult
{
    /// <summary>
    /// Gets the result value if successful.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The result value.</param>
    /// <returns>A successful result.</returns>
    public static BrowseResult<T> Success(T value) => new() { IsSuccess = true, Value = value };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static new BrowseResult<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Options for the file browser UI.
/// </summary>
public class FileBrowserOptions
{
    /// <summary>
    /// Gets or sets the initial path to display.
    /// </summary>
    public string? InitialPath { get; set; }

    /// <summary>
    /// Gets or sets whether to allow file selection.
    /// </summary>
    public bool AllowFileSelection { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow directory selection.
    /// </summary>
    public bool AllowDirectorySelection { get; set; }

    /// <summary>
    /// Gets or sets whether to show hidden files (files starting with .).
    /// </summary>
    public bool ShowHiddenFiles { get; set; }

    /// <summary>
    /// Gets or sets the file filter (e.g., "*.md").
    /// </summary>
    public string? FileFilter { get; set; }

    /// <summary>
    /// Gets or sets whether to enable file operations (create, edit, delete).
    /// </summary>
    public bool EnableFileOperations { get; set; } = true;
}

/// <summary>
/// Represents the state of the file browser.
/// </summary>
public class FileBrowserState
{
    /// <summary>
    /// Gets or sets the current path.
    /// </summary>
    public string CurrentPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current directory listing.
    /// </summary>
    public DirectoryListing? CurrentListing { get; set; }

    /// <summary>
    /// Gets or sets the selected item index.
    /// </summary>
    public int SelectedIndex { get; set; }

    /// <summary>
    /// Gets or sets the navigation history for back navigation.
    /// </summary>
    public Stack<string> NavigationHistory { get; } = new();

    /// <summary>
    /// Gets or sets the currently selected file for preview.
    /// </summary>
    public FileContent? PreviewContent { get; set; }

    /// <summary>
    /// Gets or sets whether the browser is in preview mode.
    /// </summary>
    public bool IsPreviewMode { get; set; }

    /// <summary>
    /// Gets or sets the current scroll offset in preview mode.
    /// </summary>
    public int PreviewScrollOffset { get; set; }

    /// <summary>
    /// Gets or sets the total number of lines in the preview content.
    /// </summary>
    public int PreviewTotalLines { get; set; }

    /// <summary>
    /// Gets or sets the current filter text.
    /// </summary>
    public string? FilterText { get; set; }

    /// <summary>
    /// Gets the selected item, or null if none selected.
    /// </summary>
    public BrowseItem? SelectedItem =>
        CurrentListing?.Items != null && SelectedIndex >= 0 && SelectedIndex < CurrentListing.Items.Count
            ? CurrentListing.Items[SelectedIndex]
            : null;
}
