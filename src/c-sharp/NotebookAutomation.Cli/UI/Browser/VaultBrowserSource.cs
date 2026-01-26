// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Tools.Vault;

namespace NotebookAutomation.Cli.UI.Browser;

/// <summary>
/// File browser source implementation for Obsidian Vault browsing.
/// </summary>
/// <remarks>
/// Wraps the existing <see cref="IVaultBrowserService"/> to provide browsing functionality
/// through the unified <see cref="IFileBrowserSource"/> interface.
/// </remarks>
public class VaultBrowserSource : IFileBrowserSource
{
    private readonly IVaultBrowserService _vaultBrowser;
    private readonly ILogger<VaultBrowserSource> _logger;
    private string _currentPath = string.Empty;

    /// <summary>
    /// Initializes a new instance of <see cref="VaultBrowserSource"/>.
    /// </summary>
    /// <param name="vaultBrowser">The vault browser service.</param>
    /// <param name="logger">The logger instance.</param>
    public VaultBrowserSource(
        IVaultBrowserService vaultBrowser,
        ILogger<VaultBrowserSource> logger)
    {
        _vaultBrowser = vaultBrowser ?? throw new ArgumentNullException(nameof(vaultBrowser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string SourceName => "Vault";

    /// <inheritdoc />
    public string CurrentPath => _currentPath;

    /// <inheritdoc />
    public bool SupportsTagManagement => true;

    /// <inheritdoc />
    public bool SupportsFileCreation => true;

    /// <inheritdoc />
    public bool SupportsFileDeletion => true;

    /// <inheritdoc />
    public Task<BrowseResult<DirectoryListing>> ListDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _currentPath = NormalizePath(path);
            var result = _vaultBrowser.ListDirectory(_currentPath);

            if (!result.IsSuccess)
            {
                return Task.FromResult(BrowseResult<DirectoryListing>.Failure(result.Error ?? "Unknown error"));
            }

            var items = new List<BrowseItem>();

            // Add directories first
            foreach (var dir in result.Value!.Directories)
            {
                items.Add(new BrowseItem(
                    Name: dir.Name,
                    Path: dir.RelativePath,
                    IsDirectory: true,
                    ItemCount: dir.ItemCount));
            }

            // Add files
            foreach (var file in result.Value.Files)
            {
                items.Add(new BrowseItem(
                    Name: file.Name,
                    Path: file.RelativePath,
                    IsDirectory: false,
                    SizeBytes: file.SizeBytes,
                    SizeFormatted: file.SizeFormatted,
                    LastModified: file.LastModified));
            }

            var hasParent = !string.IsNullOrEmpty(_currentPath) && _currentPath != "/";
            var listing = new DirectoryListing(_currentPath, items, hasParent);

            return Task.FromResult(BrowseResult<DirectoryListing>.Success(listing));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing directory: {Path}", path);
            return Task.FromResult(BrowseResult<DirectoryListing>.Failure($"Error listing directory: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<BrowseResult<FileContent>> ReadFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedPath = NormalizePath(path);
            var result = _vaultBrowser.ReadNote(normalizedPath);

            if (!result.IsSuccess)
            {
                return Task.FromResult(BrowseResult<FileContent>.Failure(result.Error ?? "Unknown error"));
            }

            var noteContent = result.Value!;

            // Get metadata for tags
            var metadataResult = _vaultBrowser.GetNoteMetadata(normalizedPath);
            var tags = metadataResult.IsSuccess
                ? metadataResult.Value!.Tags.ToList()
                : new List<string>();

            var info = new BrowseItem(
                Name: noteContent.Info.Name,
                Path: noteContent.Info.RelativePath,
                IsDirectory: false,
                SizeBytes: noteContent.Info.SizeBytes,
                SizeFormatted: noteContent.Info.SizeFormatted,
                LastModified: noteContent.Info.LastModified,
                Tags: tags);

            // Parse frontmatter if available
            Dictionary<string, object>? frontmatter = null;
            if (metadataResult.IsSuccess)
            {
                frontmatter = metadataResult.Value!.Frontmatter;
            }

            var content = new FileContent(
                Info: info,
                Content: noteContent.Content,
                Frontmatter: frontmatter,
                Body: noteContent.Body);

            return Task.FromResult(BrowseResult<FileContent>.Success(content));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {Path}", path);
            return Task.FromResult(BrowseResult<FileContent>.Failure($"Error reading file: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<BrowseResult> CreateFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedPath = NormalizePath(path);
            var result = _vaultBrowser.CreateNote(normalizedPath, content, overwrite: false);

            return Task.FromResult(result.IsSuccess
                ? BrowseResult.Success()
                : BrowseResult.Failure(result.Error ?? "Unknown error"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating file: {Path}", path);
            return Task.FromResult(BrowseResult.Failure($"Error creating file: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<BrowseResult> UpdateFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedPath = NormalizePath(path);
            var result = _vaultBrowser.UpdateNote(normalizedPath, content);

            return Task.FromResult(result.IsSuccess
                ? BrowseResult.Success()
                : BrowseResult.Failure(result.Error ?? "Unknown error"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file: {Path}", path);
            return Task.FromResult(BrowseResult.Failure($"Error updating file: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<BrowseResult> DeleteFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedPath = NormalizePath(path);
            var result = _vaultBrowser.DeleteNote(normalizedPath);

            return Task.FromResult(result.IsSuccess
                ? BrowseResult.Success()
                : BrowseResult.Failure(result.Error ?? "Unknown error"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {Path}", path);
            return Task.FromResult(BrowseResult.Failure($"Error deleting file: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetTagsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedPath = NormalizePath(path);
            var result = _vaultBrowser.GetNoteMetadata(normalizedPath);

            if (!result.IsSuccess)
            {
                return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
            }

            return Task.FromResult<IReadOnlyList<string>>(result.Value!.Tags.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tags: {Path}", path);
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }
    }

    /// <inheritdoc />
    public Task<BrowseResult> UpdateTagsAsync(
        string path,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedPath = NormalizePath(path);

            // Read the current note
            var readResult = _vaultBrowser.ReadNote(normalizedPath);
            if (!readResult.IsSuccess)
            {
                return Task.FromResult(BrowseResult.Failure(readResult.Error ?? "Could not read note"));
            }

            var metadataResult = _vaultBrowser.GetNoteMetadata(normalizedPath);
            var frontmatter = metadataResult.IsSuccess
                ? metadataResult.Value!.Frontmatter
                : new Dictionary<string, object>();

            // Update tags in frontmatter
            frontmatter["tags"] = tags.ToList();

            // Rebuild the content with updated frontmatter
            var newContent = BuildContentWithFrontmatter(frontmatter, readResult.Value!.Body);

            // Update the note
            var updateResult = _vaultBrowser.UpdateNote(normalizedPath, newContent);

            return Task.FromResult(updateResult.IsSuccess
                ? BrowseResult.Success()
                : BrowseResult.Failure(updateResult.Error ?? "Unknown error"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tags: {Path}", path);
            return Task.FromResult(BrowseResult.Failure($"Error updating tags: {ex.Message}"));
        }
    }

    /// <summary>
    /// Normalizes a path for vault operations.
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        // Remove leading/trailing slashes and normalize
        return path.Trim().TrimStart('/').TrimStart('\\');
    }

    /// <summary>
    /// Builds markdown content with YAML frontmatter.
    /// </summary>
    private static string BuildContentWithFrontmatter(Dictionary<string, object> frontmatter, string body)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");

        foreach (var kvp in frontmatter)
        {
            if (kvp.Value is IList<string> list)
            {
                sb.AppendLine($"{kvp.Key}:");
                foreach (var item in list)
                {
                    sb.AppendLine($"  - {item}");
                }
            }
            else if (kvp.Value is IEnumerable<object> enumerable && kvp.Value is not string)
            {
                sb.AppendLine($"{kvp.Key}:");
                foreach (var item in enumerable)
                {
                    sb.AppendLine($"  - {item}");
                }
            }
            else
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append(body.TrimStart());

        return sb.ToString();
    }
}
