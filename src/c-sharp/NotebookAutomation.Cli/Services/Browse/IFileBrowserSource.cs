// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Cli.Models.Browse;

namespace NotebookAutomation.Cli.Services.Browse;

/// <summary>
/// Interface for file browser sources (Vault, OneDrive, etc.).
/// </summary>
public interface IFileBrowserSource
{
    /// <summary>
    /// Gets the name of this source (e.g., "Vault", "OneDrive").
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Gets the current path being browsed.
    /// </summary>
    string CurrentPath { get; }

    /// <summary>
    /// Lists the contents of a directory.
    /// </summary>
    /// <param name="path">The path to list (relative or absolute depending on source).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the directory listing or error information.</returns>
    Task<BrowseResult<DirectoryListing>> ListDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the content of a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the file content or error information.</returns>
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
    /// Deletes a file.
    /// </summary>
    /// <param name="path">The path to the file to delete.</param>
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
    /// <returns>A list of tags.</returns>
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
}
