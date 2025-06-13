// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Defines an abstraction for file system operations to enable testability.
/// </summary>
/// <remarks>
/// This interface wraps common file system operations, allowing for easy mocking
/// and testing of components that interact with the file system.
/// </remarks>
public interface IFileSystemWrapper
{
    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns>true if the file exists; otherwise, false.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Determines whether the specified directory exists.
    /// </summary>
    /// <param name="path">The directory path to check.</param>
    /// <returns>true if the directory exists; otherwise, false.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Reads all text from the specified file asynchronously.
    /// </summary>
    /// <param name="path">The file path to read.</param>
    /// <returns>The content of the file as a string.</returns>
    Task<string> ReadAllTextAsync(string path);

    /// <summary>
    /// Writes all text to the specified file asynchronously.
    /// </summary>
    /// <param name="path">The file path to write to.</param>
    /// <param name="content">The content to write.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteAllTextAsync(string path, string content);

    /// <summary>
    /// Combines multiple path segments into a single path.
    /// </summary>
    /// <param name="paths">The path segments to combine.</param>
    /// <returns>The combined path.</returns>
    string CombinePath(params string[] paths);

    /// <summary>
    /// Gets the directory name from the specified path.
    /// </summary>
    /// <param name="path">The file or directory path.</param>
    /// <returns>The directory name, or null if the path is invalid.</returns>
    string? GetDirectoryName(string path);

    /// <summary>
    /// Gets the full path for the specified path.
    /// </summary>
    /// <param name="path">The relative or absolute path.</param>
    /// <returns>The absolute path.</returns>
    string GetFullPath(string path);
}
