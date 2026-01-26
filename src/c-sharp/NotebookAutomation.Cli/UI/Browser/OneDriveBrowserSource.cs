// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Cli.UI.Browser;

/// <summary>
/// File browser source implementation for OneDrive browsing.
/// </summary>
/// <remarks>
/// Wraps the existing <see cref="IOneDriveService"/> to provide browsing functionality
/// through the unified <see cref="IFileBrowserSource"/> interface.
/// </remarks>
public class OneDriveBrowserSource : IFileBrowserSource
{
    private readonly IOneDriveService _oneDriveService;
    private readonly ILogger<OneDriveBrowserSource> _logger;
    private string _currentPath = string.Empty;
    private bool _isAuthenticated;

    /// <summary>
    /// Initializes a new instance of <see cref="OneDriveBrowserSource"/>.
    /// </summary>
    /// <param name="oneDriveService">The OneDrive service.</param>
    /// <param name="logger">The logger instance.</param>
    public OneDriveBrowserSource(
        IOneDriveService oneDriveService,
        ILogger<OneDriveBrowserSource> logger)
    {
        _oneDriveService = oneDriveService ?? throw new ArgumentNullException(nameof(oneDriveService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string SourceName => "OneDrive";

    /// <inheritdoc />
    public string CurrentPath => _currentPath;

    /// <inheritdoc />
    public bool SupportsTagManagement => false;

    /// <inheritdoc />
    public bool SupportsFileCreation => true;

    /// <inheritdoc />
    public bool SupportsFileDeletion => false; // Not implemented yet

    /// <summary>
    /// Ensures the OneDrive service is authenticated.
    /// </summary>
    public async Task EnsureAuthenticatedAsync()
    {
        if (_isAuthenticated)
        {
            return;
        }

        try
        {
            var isValid = await _oneDriveService.IsTokenValidAsync();
            if (!isValid)
            {
                await _oneDriveService.AuthenticateAsync();
            }

            _isAuthenticated = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate with OneDrive");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BrowseResult<DirectoryListing>> ListDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            _currentPath = NormalizePath(path);
            var items = await _oneDriveService.ListFilesDetailedAsync(_currentPath, cancellationToken);

            var browseItems = new List<BrowseItem>();

            // Sort: directories first, then files
            var sortedItems = items
                .OrderByDescending(i => i.TryGetValue("isFolder", out var isFolder) && isFolder is true)
                .ThenBy(i => i.TryGetValue("name", out var name) ? name?.ToString() : "")
                .ToList();

            foreach (var item in sortedItems)
            {
                var name = item.TryGetValue("name", out var n) ? n?.ToString() ?? "" : "";
                var isFolder = item.TryGetValue("isFolder", out var f) && f is true;
                var size = item.TryGetValue("size", out var s) && s is long sizeVal ? sizeVal : (long?)null;
                var modified = item.TryGetValue("lastModifiedDateTime", out var m) && m is string modStr
                    ? DateTime.TryParse(modStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt) ? dt : (DateTime?)null
                    : null;
                var childCount = item.TryGetValue("childCount", out var cc) && cc is int ccVal ? ccVal : (int?)null;

                var itemPath = string.IsNullOrEmpty(_currentPath)
                    ? name
                    : $"{_currentPath}/{name}";

                browseItems.Add(new BrowseItem(
                    Name: name,
                    Path: itemPath,
                    IsDirectory: isFolder,
                    SizeBytes: isFolder ? null : size,
                    SizeFormatted: isFolder ? null : FormatSize(size),
                    LastModified: modified,
                    ItemCount: childCount));
            }

            var hasParent = !string.IsNullOrEmpty(_currentPath);
            var listing = new DirectoryListing(_currentPath, browseItems, hasParent);

            return BrowseResult<DirectoryListing>.Success(listing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing OneDrive directory: {Path}", path);
            return BrowseResult<DirectoryListing>.Failure($"Error listing directory: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<BrowseResult<FileContent>> ReadFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var normalizedPath = NormalizePath(path);
            var fileName = Path.GetFileName(normalizedPath);

            // Create a temporary file to download content
            var tempPath = Path.Combine(Path.GetTempPath(), $"na_onedrive_{Guid.NewGuid()}{Path.GetExtension(fileName)}");

            try
            {
                await _oneDriveService.DownloadFileAsync(normalizedPath, tempPath, cancellationToken);

                var content = await File.ReadAllTextAsync(tempPath, cancellationToken);
                var fileInfo = new FileInfo(tempPath);

                var info = new BrowseItem(
                    Name: fileName,
                    Path: normalizedPath,
                    IsDirectory: false,
                    SizeBytes: fileInfo.Length,
                    SizeFormatted: FormatSize(fileInfo.Length),
                    LastModified: fileInfo.LastWriteTime);

                // For markdown files, try to parse frontmatter
                string? body = null;
                Dictionary<string, object>? frontmatter = null;

                if (fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    (frontmatter, body) = ParseMarkdownContent(content);
                }

                var fileContent = new FileContent(
                    Info: info,
                    Content: content,
                    Frontmatter: frontmatter,
                    Body: body ?? content);

                return BrowseResult<FileContent>.Success(fileContent);
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading OneDrive file: {Path}", path);
            return BrowseResult<FileContent>.Failure($"Error reading file: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<BrowseResult> CreateFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var normalizedPath = NormalizePath(path);

            // Create a temporary file with the content
            var tempPath = Path.Combine(Path.GetTempPath(), $"na_onedrive_upload_{Guid.NewGuid()}.tmp");

            try
            {
                await File.WriteAllTextAsync(tempPath, content, cancellationToken);
                await _oneDriveService.UploadFileAsync(tempPath, normalizedPath, cancellationToken);

                return BrowseResult.Success();
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating OneDrive file: {Path}", path);
            return BrowseResult.Failure($"Error creating file: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<BrowseResult> UpdateFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        // OneDrive upload overwrites existing files
        return await CreateFileAsync(path, content, cancellationToken);
    }

    /// <inheritdoc />
    public Task<BrowseResult> DeleteFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(BrowseResult.Failure("File deletion is not yet supported for OneDrive"));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetTagsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        // OneDrive doesn't support tags in the same way as Obsidian
        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    /// <inheritdoc />
    public Task<BrowseResult> UpdateTagsAsync(
        string path,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(BrowseResult.Failure("Tag management is not supported for OneDrive"));
    }

    /// <summary>
    /// Normalizes a path for OneDrive operations.
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        // Remove leading/trailing slashes and normalize
        return path.Trim().TrimStart('/').TrimEnd('/');
    }

    /// <summary>
    /// Formats a file size in bytes to a human-readable string.
    /// </summary>
    private static string? FormatSize(long? bytes)
    {
        if (!bytes.HasValue)
        {
            return null;
        }

        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)bytes.Value;
        var suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:0.##} {suffixes[suffixIndex]}";
    }

    /// <summary>
    /// Parses markdown content to extract frontmatter and body.
    /// </summary>
    private static (Dictionary<string, object>? Frontmatter, string Body) ParseMarkdownContent(string content)
    {
        if (!content.StartsWith("---"))
        {
            return (null, content);
        }

        var endIndex = content.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            return (null, content);
        }

        var frontmatterText = content[3..endIndex].Trim();
        var body = content[(endIndex + 3)..].TrimStart('\r', '\n');

        // Simple frontmatter parsing (key: value format)
        var frontmatter = new Dictionary<string, object>();
        foreach (var line in frontmatterText.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = trimmed[..colonIndex].Trim();
                var value = trimmed[(colonIndex + 1)..].Trim();
                frontmatter[key] = value;
            }
        }

        return (frontmatter, body);
    }
}
