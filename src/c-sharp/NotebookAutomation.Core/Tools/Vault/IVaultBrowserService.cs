// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Interface for vault browsing operations including directory listing, file operations, and metadata retrieval.
/// </summary>
/// <remarks>
/// Provides core functionality for navigating and manipulating files within an Obsidian vault or similar
/// file-based knowledge management system. All paths are relative to the configured vault root.
/// </remarks>
public interface IVaultBrowserService
{
    /// <summary>
    /// Gets the full path to the vault root directory.
    /// </summary>
    string VaultRootPath { get; }

    /// <summary>
    /// Lists the contents of a directory in the vault.
    /// </summary>
    /// <param name="relativePath">The path relative to the vault root, or empty for root.</param>
    /// <returns>A result containing directory listing or error information.</returns>
    VaultBrowserResult<VaultDirectoryListing> ListDirectory(string relativePath = "");

    /// <summary>
    /// Lists only markdown notes in a directory.
    /// </summary>
    /// <param name="relativePath">The path relative to the vault root, or empty for root.</param>
    /// <param name="recursive">Whether to include notes from subdirectories.</param>
    /// <returns>A result containing the list of notes or error information.</returns>
    VaultBrowserResult<IReadOnlyList<VaultNoteInfo>> ListNotes(string relativePath = "", bool recursive = false);

    /// <summary>
    /// Reads the content of a note.
    /// </summary>
    /// <param name="relativePath">The path to the note relative to the vault root.</param>
    /// <returns>A result containing the note content or error information.</returns>
    VaultBrowserResult<VaultNoteContent> ReadNote(string relativePath);

    /// <summary>
    /// Creates a new note in the vault.
    /// </summary>
    /// <param name="relativePath">The path for the new note relative to the vault root.</param>
    /// <param name="content">The content of the note.</param>
    /// <param name="overwrite">Whether to overwrite if the file exists.</param>
    /// <returns>A result indicating success or error information.</returns>
    VaultBrowserResult<VaultNoteInfo> CreateNote(string relativePath, string content, bool overwrite = false);

    /// <summary>
    /// Updates an existing note's content.
    /// </summary>
    /// <param name="relativePath">The path to the note relative to the vault root.</param>
    /// <param name="content">The new content for the note.</param>
    /// <returns>A result indicating success or error information.</returns>
    VaultBrowserResult<VaultNoteInfo> UpdateNote(string relativePath, string content);

    /// <summary>
    /// Appends content to an existing note.
    /// </summary>
    /// <param name="relativePath">The path to the note relative to the vault root.</param>
    /// <param name="content">The content to append.</param>
    /// <returns>A result indicating success or error information.</returns>
    VaultBrowserResult<VaultNoteInfo> AppendToNote(string relativePath, string content);

    /// <summary>
    /// Deletes a note from the vault.
    /// </summary>
    /// <param name="relativePath">The path to the note relative to the vault root.</param>
    /// <returns>A result indicating success or error information.</returns>
    VaultBrowserResult<bool> DeleteNote(string relativePath);

    /// <summary>
    /// Gets metadata for a note including frontmatter and file information.
    /// </summary>
    /// <param name="relativePath">The path to the note relative to the vault root.</param>
    /// <returns>A result containing note metadata or error information.</returns>
    VaultBrowserResult<VaultNoteMetadata> GetNoteMetadata(string relativePath);

    /// <summary>
    /// Gets information about the vault.
    /// </summary>
    /// <returns>A result containing vault information.</returns>
    VaultBrowserResult<VaultInfo> GetVaultInfo();

    /// <summary>
    /// Resolves a relative vault path to an absolute file system path.
    /// </summary>
    /// <param name="relativePath">The path relative to the vault root.</param>
    /// <returns>The absolute file system path.</returns>
    string ResolveFullPath(string relativePath);

    /// <summary>
    /// Gets the relative vault path from an absolute path.
    /// </summary>
    /// <param name="absolutePath">The absolute file system path.</param>
    /// <returns>The relative path within the vault, or null if outside vault.</returns>
    string? GetRelativePath(string absolutePath);
}

/// <summary>
/// Result wrapper for vault browser operations.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public class VaultBrowserResult<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the result value if successful.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static VaultBrowserResult<T> Success(T value) => new() { IsSuccess = true, Value = value };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static VaultBrowserResult<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Represents a directory listing in the vault.
/// </summary>
public class VaultDirectoryListing
{
    /// <summary>
    /// Gets or sets the relative path of this directory.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets or sets the subdirectories.
    /// </summary>
    public required IReadOnlyList<VaultBrowserDirectoryInfo> Directories { get; init; }

    /// <summary>
    /// Gets or sets the files in this directory.
    /// </summary>
    public required IReadOnlyList<VaultBrowserFileInfo> Files { get; init; }
}

/// <summary>
/// Information about a directory in the vault.
/// </summary>
public class VaultBrowserDirectoryInfo
{
    /// <summary>
    /// Gets the directory name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the relative path from the vault root.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// Gets the number of items in the directory.
    /// </summary>
    public int ItemCount { get; init; }
}

/// <summary>
/// Information about a file in the vault.
/// </summary>
public class VaultBrowserFileInfo
{
    /// <summary>
    /// Gets the file name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the relative path from the vault root.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Gets the formatted file size.
    /// </summary>
    public required string SizeFormatted { get; init; }

    /// <summary>
    /// Gets the last modified timestamp.
    /// </summary>
    public DateTime LastModified { get; init; }
}

/// <summary>
/// Information about a note (markdown file) in the vault.
/// </summary>
public class VaultNoteInfo
{
    /// <summary>
    /// Gets the note name (without extension).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the file name (with extension).
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the relative path from the vault root.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Gets the formatted file size.
    /// </summary>
    public required string SizeFormatted { get; init; }

    /// <summary>
    /// Gets the last modified timestamp.
    /// </summary>
    public DateTime LastModified { get; init; }
}

/// <summary>
/// Content of a note including raw text and extracted metadata.
/// </summary>
public class VaultNoteContent
{
    /// <summary>
    /// Gets the note information.
    /// </summary>
    public required VaultNoteInfo Info { get; init; }

    /// <summary>
    /// Gets the raw content of the note.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the extracted frontmatter as YAML, if present.
    /// </summary>
    public string? Frontmatter { get; init; }

    /// <summary>
    /// Gets the content without frontmatter.
    /// </summary>
    public required string Body { get; init; }
}

/// <summary>
/// Metadata about a note including frontmatter and file system information.
/// </summary>
public class VaultNoteMetadata
{
    /// <summary>
    /// Gets the note information.
    /// </summary>
    public required VaultNoteInfo Info { get; init; }

    /// <summary>
    /// Gets the parsed frontmatter as a dictionary.
    /// </summary>
    public required Dictionary<string, object> Frontmatter { get; init; }

    /// <summary>
    /// Gets the tags extracted from the frontmatter.
    /// </summary>
    public required IReadOnlySet<string> Tags { get; init; }

    /// <summary>
    /// Gets the file creation timestamp.
    /// </summary>
    public DateTime Created { get; init; }
}

/// <summary>
/// Information about the vault itself.
/// </summary>
public class VaultInfo
{
    /// <summary>
    /// Gets the vault name (root folder name).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the full path to the vault root.
    /// </summary>
    public required string RootPath { get; init; }

    /// <summary>
    /// Gets the total number of notes in the vault.
    /// </summary>
    public int TotalNotes { get; init; }

    /// <summary>
    /// Gets the total number of folders in the vault.
    /// </summary>
    public int TotalFolders { get; init; }

    /// <summary>
    /// Gets the total size of the vault in bytes.
    /// </summary>
    public long TotalSizeBytes { get; init; }

    /// <summary>
    /// Gets the formatted total size.
    /// </summary>
    public required string TotalSizeFormatted { get; init; }
}
