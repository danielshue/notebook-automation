// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Provides a concrete implementation of file system operations.
/// </summary>
/// <remarks>
/// This class wraps standard .NET file system operations, providing a testable
/// abstraction layer for components that need to interact with the file system.
/// </remarks>
public class FileSystemWrapper : IFileSystemWrapper
{
    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns>true if the file exists; otherwise, false.</returns>
    public bool FileExists(string path) => File.Exists(path);

    /// <summary>
    /// Determines whether the specified directory exists.
    /// </summary>
    /// <param name="path">The directory path to check.</param>
    /// <returns>true if the directory exists; otherwise, false.</returns>
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <summary>
    /// Reads all text from the specified file asynchronously.
    /// </summary>
    /// <param name="path">The file path to read.</param>
    /// <returns>The content of the file as a string.</returns>
    public async Task<string> ReadAllTextAsync(string path) => await File.ReadAllTextAsync(path);

    /// <summary>
    /// Writes all text to the specified file asynchronously.
    /// </summary>
    /// <param name="path">The file path to write to.</param>
    /// <param name="content">The content to write.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task WriteAllTextAsync(string path, string content) => await File.WriteAllTextAsync(path, content);

    /// <summary>
    /// Combines multiple path segments into a single path.
    /// </summary>
    /// <param name="paths">The path segments to combine.</param>
    /// <returns>The combined path.</returns>
    public string CombinePath(params string[] paths) => Path.Combine(paths);

    /// <summary>
    /// Gets the directory name from the specified path.
    /// </summary>
    /// <param name="path">The file or directory path.</param>
    /// <returns>The directory name, or null if the path is invalid.</returns>
    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

    /// <summary>
    /// Gets the full path for the specified path.
    /// </summary>
    /// <param name="path">The relative or absolute path.</param>
    /// <returns>The absolute path.</returns>
    public string GetFullPath(string path) => Path.GetFullPath(path);
}
