// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Tests.Core.Configuration;


/// <summary>
/// Mock implementation of <see cref="IFileSystemWrapper"/> for unit testing.
/// </summary>

public class MockFileSystemWrapper : IFileSystemWrapper
{
    private readonly Dictionary<string, string> _files = new();

    /// <inheritdoc />
    public bool FileExists(string path) => _files.ContainsKey(path);

    /// <inheritdoc />
    public bool DirectoryExists(string path) => true;

    /// <inheritdoc />
    public Task<string> ReadAllTextAsync(string path) =>
        _files.TryGetValue(path, out var content)
            ? Task.FromResult(content)
            : throw new FileNotFoundException();

    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string content)
    {
        _files[path] = content;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public string CombinePath(params string[] paths) => string.Join("/", paths);

    /// <inheritdoc />
    public string? GetDirectoryName(string path) => path.Contains('/') ? path[..path.LastIndexOf('/')] : null;

    /// <inheritdoc />
    public string GetFullPath(string path) => path.StartsWith("/") ? path : "/mockroot/" + path;
}


/// <summary>
/// Mock implementation of <see cref="IEnvironmentWrapper"/> for unit testing.
/// </summary>

public class MockEnvironmentWrapper : IEnvironmentWrapper
{
    /// <inheritdoc />
    public string? GetEnvironmentVariable(string variableName) => null;

    /// <inheritdoc />
    public string? GetEnvironmentVariable(string variableName, EnvironmentVariableTarget target) => null;

    /// <inheritdoc />
    public string GetCurrentDirectory() => ".";

    /// <inheritdoc />
    public string GetExecutableDirectory() => ".";

    /// <inheritdoc />
    public bool IsDevelopment() => false;
}
