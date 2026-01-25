// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

using NotebookAutomation.Core.Utils;

/// <summary>
/// Implementation of <see cref="IVaultBrowserService"/> for browsing and manipulating vault contents.
/// </summary>
/// <remarks>
/// Provides comprehensive file operations for navigating and manipulating an Obsidian vault or similar
/// file-based knowledge management system. Uses the configured vault root path for all operations.
/// </remarks>
public class VaultBrowserService : IVaultBrowserService
{
    private readonly ILogger<VaultBrowserService> _logger;
    private readonly IYamlHelper _yamlHelper;

    /// <inheritdoc />
    public string VaultRootPath { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="VaultBrowserService"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="yamlHelper">The YAML helper for parsing frontmatter.</param>
    /// <param name="vaultRootPath">The full path to the vault root directory.</param>
    public VaultBrowserService(
        ILogger<VaultBrowserService> logger,
        IYamlHelper yamlHelper,
        string vaultRootPath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _yamlHelper = yamlHelper ?? throw new ArgumentNullException(nameof(yamlHelper));
        VaultRootPath = PathUtils.NormalizePath(vaultRootPath ?? throw new ArgumentNullException(nameof(vaultRootPath)));

        if (!Directory.Exists(VaultRootPath))
        {
            _logger.LogWarning("Vault root path does not exist: {VaultRootPath}", VaultRootPath);
        }
    }

    /// <inheritdoc />
    public string ResolveFullPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return VaultRootPath;
        }

        // Normalize the path - handle both forward and backslashes
        var normalizedPath = PathUtils.NormalizePath(relativePath);

        // If it's already absolute and within vault, return normalized
        if (Path.IsPathRooted(normalizedPath))
        {
            var normalizedVault = PathUtils.NormalizePath(VaultRootPath);
            if (normalizedPath.StartsWith(normalizedVault, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath;
            }
        }

        // Combine with vault root
        return PathUtils.NormalizePath(Path.Combine(VaultRootPath, normalizedPath));
    }

    /// <inheritdoc />
    public string? GetRelativePath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
        {
            return null;
        }

        var normalizedAbsolute = PathUtils.NormalizePath(absolutePath);
        var normalizedVault = PathUtils.NormalizePath(VaultRootPath);

        if (!normalizedAbsolute.StartsWith(normalizedVault, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return PathUtils.MakeRelative(normalizedVault, normalizedAbsolute);
    }

    /// <inheritdoc />
    public VaultBrowserResult<VaultDirectoryListing> ListDirectory(string relativePath = "")
    {
        try
        {
            var fullPath = ResolveFullPath(relativePath);

            if (!Directory.Exists(fullPath))
            {
                return VaultBrowserResult<VaultDirectoryListing>.Failure($"Directory not found: {relativePath}");
            }

            var directories = new List<VaultBrowserDirectoryInfo>();
            foreach (var dir in Directory.GetDirectories(fullPath))
            {
                var dirInfo = new DirectoryInfo(dir);
                var dirRelativePath = GetRelativePath(dir) ?? dirInfo.Name;

                // Skip hidden directories (starting with .)
                if (dirInfo.Name.StartsWith('.'))
                {
                    continue;
                }

                var itemCount = Directory.GetFileSystemEntries(dir).Length;
                directories.Add(new VaultBrowserDirectoryInfo
                {
                    Name = dirInfo.Name,
                    RelativePath = dirRelativePath,
                    ItemCount = itemCount
                });
            }

            var files = new List<VaultBrowserFileInfo>();
            foreach (var file in Directory.GetFiles(fullPath))
            {
                var fileInfo = new FileInfo(file);
                var fileRelativePath = GetRelativePath(file) ?? fileInfo.Name;

                // Skip hidden files (starting with .)
                if (fileInfo.Name.StartsWith('.'))
                {
                    continue;
                }

                files.Add(new VaultBrowserFileInfo
                {
                    Name = fileInfo.Name,
                    RelativePath = fileRelativePath,
                    SizeBytes = fileInfo.Length,
                    SizeFormatted = FileSizeFormatter.FormatFileSizeToString(fileInfo.Length),
                    LastModified = fileInfo.LastWriteTime
                });
            }

            var listing = new VaultDirectoryListing
            {
                Path = string.IsNullOrEmpty(relativePath) ? "/" : relativePath,
                Directories = directories.OrderBy(d => d.Name).ToList(),
                Files = files.OrderBy(f => f.Name).ToList()
            };

            return VaultBrowserResult<VaultDirectoryListing>.Success(listing);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied to directory: {Path}", relativePath);
            return VaultBrowserResult<VaultDirectoryListing>.Failure($"Access denied: {relativePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing directory: {Path}", relativePath);
            return VaultBrowserResult<VaultDirectoryListing>.Failure($"Error listing directory: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public VaultBrowserResult<IReadOnlyList<VaultNoteInfo>> ListNotes(string relativePath = "", bool recursive = false)
    {
        try
        {
            var fullPath = ResolveFullPath(relativePath);

            if (!Directory.Exists(fullPath))
            {
                return VaultBrowserResult<IReadOnlyList<VaultNoteInfo>>.Failure($"Directory not found: {relativePath}");
            }

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var notes = new List<VaultNoteInfo>();

            foreach (var file in Directory.GetFiles(fullPath, "*.md", searchOption))
            {
                var fileInfo = new FileInfo(file);

                // Skip hidden files
                if (fileInfo.Name.StartsWith('.'))
                {
                    continue;
                }

                var fileRelativePath = GetRelativePath(file) ?? fileInfo.Name;
                notes.Add(new VaultNoteInfo
                {
                    Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
                    FileName = fileInfo.Name,
                    RelativePath = fileRelativePath,
                    SizeBytes = fileInfo.Length,
                    SizeFormatted = FileSizeFormatter.FormatFileSizeToString(fileInfo.Length),
                    LastModified = fileInfo.LastWriteTime
                });
            }

            return VaultBrowserResult<IReadOnlyList<VaultNoteInfo>>.Success(
                notes.OrderBy(n => n.Name).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing notes in: {Path}", relativePath);
            return VaultBrowserResult<IReadOnlyList<VaultNoteInfo>>.Failure($"Error listing notes: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public VaultBrowserResult<VaultNoteContent> ReadNote(string relativePath)
    {
        try
        {
            var fullPath = ResolveFullPath(relativePath);

            // Add .md extension if not present
            if (!fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                fullPath += ".md";
            }

            if (!File.Exists(fullPath))
            {
                return VaultBrowserResult<VaultNoteContent>.Failure($"Note not found: {relativePath}");
            }

            var fileInfo = new FileInfo(fullPath);
            var content = File.ReadAllText(fullPath);
            var frontmatter = _yamlHelper.ExtractFrontmatter(content);
            var body = _yamlHelper.RemoveFrontmatter(content);

            var noteInfo = new VaultNoteInfo
            {
                Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
                FileName = fileInfo.Name,
                RelativePath = GetRelativePath(fullPath) ?? relativePath,
                SizeBytes = fileInfo.Length,
                SizeFormatted = FileSizeFormatter.FormatFileSizeToString(fileInfo.Length),
                LastModified = fileInfo.LastWriteTime
            };

            return VaultBrowserResult<VaultNoteContent>.Success(new VaultNoteContent
            {
                Info = noteInfo,
                Content = content,
                Frontmatter = frontmatter,
                Body = body
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading note: {Path}", relativePath);
            return VaultBrowserResult<VaultNoteContent>.Failure($"Error reading note: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public VaultBrowserResult<VaultNoteInfo> CreateNote(string relativePath, string content, bool overwrite = false)
    {
        try
        {
            var fullPath = ResolveFullPath(relativePath);

            // Add .md extension if not present
            if (!fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                fullPath += ".md";
            }

            if (File.Exists(fullPath) && !overwrite)
            {
                return VaultBrowserResult<VaultNoteInfo>.Failure($"Note already exists: {relativePath}");
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                PathUtils.EnsureDirectoryExists(directory);
            }

            File.WriteAllText(fullPath, content);
            var fileInfo = new FileInfo(fullPath);

            var noteInfo = new VaultNoteInfo
            {
                Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
                FileName = fileInfo.Name,
                RelativePath = GetRelativePath(fullPath) ?? relativePath,
                SizeBytes = fileInfo.Length,
                SizeFormatted = FileSizeFormatter.FormatFileSizeToString(fileInfo.Length),
                LastModified = fileInfo.LastWriteTime
            };

            _logger.LogInformation("Created note: {Path}", noteInfo.RelativePath);
            return VaultBrowserResult<VaultNoteInfo>.Success(noteInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating note: {Path}", relativePath);
            return VaultBrowserResult<VaultNoteInfo>.Failure($"Error creating note: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public VaultBrowserResult<VaultNoteInfo> UpdateNote(string relativePath, string content)
    {
        try
        {
            var fullPath = ResolveFullPath(relativePath);

            // Add .md extension if not present
            if (!fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                fullPath += ".md";
            }

            if (!File.Exists(fullPath))
            {
                return VaultBrowserResult<VaultNoteInfo>.Failure($"Note not found: {relativePath}");
            }

            File.WriteAllText(fullPath, content);
            var fileInfo = new FileInfo(fullPath);

            var noteInfo = new VaultNoteInfo
            {
                Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
                FileName = fileInfo.Name,
                RelativePath = GetRelativePath(fullPath) ?? relativePath,
                SizeBytes = fileInfo.Length,
                SizeFormatted = FileSizeFormatter.FormatFileSizeToString(fileInfo.Length),
                LastModified = fileInfo.LastWriteTime
            };

            _logger.LogInformation("Updated note: {Path}", noteInfo.RelativePath);
            return VaultBrowserResult<VaultNoteInfo>.Success(noteInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating note: {Path}", relativePath);
            return VaultBrowserResult<VaultNoteInfo>.Failure($"Error updating note: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public VaultBrowserResult<VaultNoteInfo> AppendToNote(string relativePath, string content)
    {
        try
        {
            var fullPath = ResolveFullPath(relativePath);

            // Add .md extension if not present
            if (!fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                fullPath += ".md";
            }

            if (!File.Exists(fullPath))
            {
                return VaultBrowserResult<VaultNoteInfo>.Failure($"Note not found: {relativePath}");
            }

            // Append with newline
            var existingContent = File.ReadAllText(fullPath);
            var newContent = existingContent.TrimEnd() + Environment.NewLine + Environment.NewLine + content;
            File.WriteAllText(fullPath, newContent);

            var fileInfo = new FileInfo(fullPath);

            var noteInfo = new VaultNoteInfo
            {
                Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
                FileName = fileInfo.Name,
                RelativePath = GetRelativePath(fullPath) ?? relativePath,
                SizeBytes = fileInfo.Length,
                SizeFormatted = FileSizeFormatter.FormatFileSizeToString(fileInfo.Length),
                LastModified = fileInfo.LastWriteTime
            };

            _logger.LogInformation("Appended to note: {Path}", noteInfo.RelativePath);
            return VaultBrowserResult<VaultNoteInfo>.Success(noteInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error appending to note: {Path}", relativePath);
            return VaultBrowserResult<VaultNoteInfo>.Failure($"Error appending to note: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public VaultBrowserResult<bool> DeleteNote(string relativePath)
    {
        try
        {
            var fullPath = ResolveFullPath(relativePath);

            // Add .md extension if not present
            if (!fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                fullPath += ".md";
            }

            if (!File.Exists(fullPath))
            {
                return VaultBrowserResult<bool>.Failure($"Note not found: {relativePath}");
            }

            File.Delete(fullPath);
            _logger.LogInformation("Deleted note: {Path}", relativePath);
            return VaultBrowserResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting note: {Path}", relativePath);
            return VaultBrowserResult<bool>.Failure($"Error deleting note: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public VaultBrowserResult<VaultNoteMetadata> GetNoteMetadata(string relativePath)
    {
        try
        {
            var fullPath = ResolveFullPath(relativePath);

            // Add .md extension if not present
            if (!fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                fullPath += ".md";
            }

            if (!File.Exists(fullPath))
            {
                return VaultBrowserResult<VaultNoteMetadata>.Failure($"Note not found: {relativePath}");
            }

            var fileInfo = new FileInfo(fullPath);
            var content = File.ReadAllText(fullPath);
            var frontmatter = _yamlHelper.ExtractFrontmatter(content);
            var frontmatterDict = frontmatter != null
                ? _yamlHelper.ParseYamlToDictionary(frontmatter)
                : new Dictionary<string, object>();
            var tags = YamlHelper.ExtractTags(frontmatterDict);

            var noteInfo = new VaultNoteInfo
            {
                Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
                FileName = fileInfo.Name,
                RelativePath = GetRelativePath(fullPath) ?? relativePath,
                SizeBytes = fileInfo.Length,
                SizeFormatted = FileSizeFormatter.FormatFileSizeToString(fileInfo.Length),
                LastModified = fileInfo.LastWriteTime
            };

            return VaultBrowserResult<VaultNoteMetadata>.Success(new VaultNoteMetadata
            {
                Info = noteInfo,
                Frontmatter = frontmatterDict,
                Tags = tags,
                Created = fileInfo.CreationTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting note metadata: {Path}", relativePath);
            return VaultBrowserResult<VaultNoteMetadata>.Failure($"Error getting note metadata: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public VaultBrowserResult<VaultInfo> GetVaultInfo()
    {
        try
        {
            if (!Directory.Exists(VaultRootPath))
            {
                return VaultBrowserResult<VaultInfo>.Failure($"Vault root not found: {VaultRootPath}");
            }

            var noteCount = 0;
            var folderCount = 0;
            long totalSize = 0;

            foreach (var file in Directory.GetFiles(VaultRootPath, "*.md", SearchOption.AllDirectories))
            {
                var fileInfo = new FileInfo(file);
                if (!fileInfo.Name.StartsWith('.'))
                {
                    noteCount++;
                    totalSize += fileInfo.Length;
                }
            }

            foreach (var dir in Directory.GetDirectories(VaultRootPath, "*", SearchOption.AllDirectories))
            {
                var dirInfo = new DirectoryInfo(dir);
                if (!dirInfo.Name.StartsWith('.'))
                {
                    folderCount++;
                }
            }

            return VaultBrowserResult<VaultInfo>.Success(new VaultInfo
            {
                Name = new DirectoryInfo(VaultRootPath).Name,
                RootPath = VaultRootPath,
                TotalNotes = noteCount,
                TotalFolders = folderCount,
                TotalSizeBytes = totalSize,
                TotalSizeFormatted = FileSizeFormatter.FormatFileSizeToString(totalSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vault info");
            return VaultBrowserResult<VaultInfo>.Failure($"Error getting vault info: {ex.Message}");
        }
    }
}
