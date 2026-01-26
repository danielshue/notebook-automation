// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.UI.Browser;

/// <summary>
/// Interface for file browser sources that can list, read, and manipulate files.
/// </summary>
/// <remarks>
/// Provides an abstraction layer for different file sources (Vault, OneDrive, etc.)
/// to be browsed through a unified interface.
/// </remarks>
public interface IFileBrowserSource
{
    /// <summary>
    /// Gets the name of the source (e.g., "Vault", "OneDrive").
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Gets the current path within the source.
    /// </summary>
    string CurrentPath { get; }

    /// <summary>
    /// Lists the contents of a directory.
    /// </summary>
    /// <param name="path">The path to list, or empty/null for root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing directory listing or error information.</returns>
    Task<BrowseResult<DirectoryListing>> ListDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the content of a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing file content or error information.</returns>
    Task<BrowseResult<FileContent>> ReadFileAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new file.
    /// </summary>
    /// <param name="path">The path for the new file.</param>
    /// <param name="content">The content of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or error information.</returns>
    Task<BrowseResult> CreateFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="content">The new content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or error information.</returns>
    Task<BrowseResult> UpdateFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or error information.</returns>
    Task<BrowseResult> DeleteFileAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tags for a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of tags, or empty if not supported.</returns>
    Task<IReadOnlyList<string>> GetTagsAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the tags for a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="tags">The new tags.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or error information.</returns>
    Task<BrowseResult> UpdateTagsAsync(
        string path,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether this source supports tag management.
    /// </summary>
    bool SupportsTagManagement { get; }

    /// <summary>
    /// Gets whether this source supports file creation.
    /// </summary>
    bool SupportsFileCreation { get; }

    /// <summary>
    /// Gets whether this source supports file deletion.
    /// </summary>
    bool SupportsFileDeletion { get; }
}
