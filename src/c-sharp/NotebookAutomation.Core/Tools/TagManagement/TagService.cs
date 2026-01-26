// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.TagManagement;

/// <summary>
/// Implementation of <see cref="ITagService"/> that wraps <see cref="TagProcessor"/>.
/// </summary>
public class TagService : ITagService
{
    private readonly ILogger<TagService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IYamlHelper _yamlHelper;
    private readonly IMetadataSchemaLoader? _schemaLoader;
    private readonly string _vaultRootPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagService"/> class.
    /// </summary>
    /// <param name="logger">Logger for service operations.</param>
    /// <param name="loggerFactory">Factory for creating TagProcessor loggers.</param>
    /// <param name="yamlHelper">YAML helper for frontmatter processing.</param>
    /// <param name="schemaLoader">Optional metadata schema loader.</param>
    /// <param name="vaultRootPath">Root path of the Obsidian vault.</param>
    public TagService(
        ILogger<TagService> logger,
        ILoggerFactory loggerFactory,
        IYamlHelper yamlHelper,
        IMetadataSchemaLoader? schemaLoader,
        string vaultRootPath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _yamlHelper = yamlHelper ?? throw new ArgumentNullException(nameof(yamlHelper));
        _schemaLoader = schemaLoader;
        _vaultRootPath = vaultRootPath ?? throw new ArgumentNullException(nameof(vaultRootPath));
    }

    /// <inheritdoc/>
    public async Task<TagOperationResult> AddNestedTagsAsync(string path, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding nested tags to {Path}, DryRun={DryRun}", path, dryRun);

        try
        {
            var resolvedPath = ResolvePath(path);
            if (!ValidatePath(resolvedPath, out var validationError))
            {
                return CreateErrorResult($"Invalid path: {validationError}", dryRun);
            }

            var processor = CreateTagProcessor(dryRun, verbose: false);

            Dictionary<string, int> stats;
            if (Directory.Exists(resolvedPath))
            {
                stats = await processor.ProcessDirectoryAsync(resolvedPath);
            }
            else if (File.Exists(resolvedPath))
            {
                var success = await processor.ProcessFileAsync(resolvedPath);
                stats = processor.Stats;
            }
            else
            {
                return CreateErrorResult($"Path not found: {resolvedPath}", dryRun);
            }

            return CreateResult(stats, dryRun, "Added nested tags");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding nested tags to {Path}", path);
            return CreateErrorResult($"Error: {ex.Message}", dryRun);
        }
    }

    /// <inheritdoc/>
    public async Task<TagOperationResult> ConsolidateTagsAsync(string? path = null, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Consolidating tags in {Path}, DryRun={DryRun}", path ?? "vault", dryRun);

        try
        {
            var resolvedPath = ResolvePath(path);
            if (!ValidatePath(resolvedPath, out var validationError))
            {
                return CreateErrorResult($"Invalid path: {validationError}", dryRun);
            }

            var processor = CreateTagProcessor(dryRun, verbose: false);

            // Consolidate uses the same processing as add-nested but focuses on tag deduplication
            var stats = await processor.ProcessDirectoryAsync(resolvedPath);

            return CreateResult(stats, dryRun, "Consolidated tags");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consolidating tags in {Path}", path);
            return CreateErrorResult($"Error: {ex.Message}", dryRun);
        }
    }

    /// <inheritdoc/>
    public async Task<TagOperationResult> RestructureTagsAsync(string? path = null, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restructuring tags in {Path}, DryRun={DryRun}", path ?? "vault", dryRun);

        try
        {
            var resolvedPath = ResolvePath(path);
            if (!ValidatePath(resolvedPath, out var validationError))
            {
                return CreateErrorResult($"Invalid path: {validationError}", dryRun);
            }

            var processor = CreateTagProcessor(dryRun, verbose: false);
            var stats = await processor.RestructureTagsInDirectoryAsync(resolvedPath);

            return CreateResult(stats, dryRun, "Restructured tags");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restructuring tags in {Path}", path);
            return CreateErrorResult($"Error: {ex.Message}", dryRun);
        }
    }

    /// <inheritdoc/>
    public async Task<TagOperationResult> UpdateFrontmatterAsync(string path, string key, string value, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating frontmatter {Key}={Value} in {Path}, DryRun={DryRun}", key, value, path, dryRun);

        if (string.IsNullOrWhiteSpace(key))
        {
            return CreateErrorResult("Key cannot be empty", dryRun);
        }

        try
        {
            var resolvedPath = ResolvePath(path);
            if (!ValidatePath(resolvedPath, out var validationError))
            {
                return CreateErrorResult($"Invalid path: {validationError}", dryRun);
            }

            var processor = CreateTagProcessor(dryRun, verbose: false);
            var stats = await processor.UpdateFrontmatterKeyAsync(resolvedPath, key, value);

            return CreateResult(stats, dryRun, $"Updated frontmatter key '{key}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating frontmatter in {Path}", path);
            return CreateErrorResult($"Error: {ex.Message}", dryRun);
        }
    }

    /// <inheritdoc/>
    public async Task<YamlDiagnosisResult> DiagnoseYamlAsync(string? path = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Diagnosing YAML in {Path}", path ?? "vault");

        try
        {
            var resolvedPath = ResolvePath(path);
            if (!ValidatePath(resolvedPath, out var validationError))
            {
                return new YamlDiagnosisResult
                {
                    Success = false,
                    Message = $"Invalid path: {validationError}",
                    FilesScanned = 0,
                    FilesWithIssues = 0
                };
            }

            var processor = CreateTagProcessor(dryRun: true, verbose: false);
            var results = await processor.DiagnoseFrontmatterIssuesAsync(resolvedPath);

            var issues = results.Select(r => new YamlIssue
            {
                FilePath = r.FilePath,
                Description = r.DiagnosticMessage,
                SuggestedFix = GetSuggestedFix(r.DiagnosticMessage)
            }).ToList();

            // Count files scanned by counting markdown files
            var filesScanned = Directory.Exists(resolvedPath)
                ? Directory.GetFiles(resolvedPath, "*.md", SearchOption.AllDirectories).Length
                : 1;

            return new YamlDiagnosisResult
            {
                Success = true,
                Message = issues.Count == 0
                    ? "No YAML issues found"
                    : $"Found {issues.Count} file(s) with YAML issues",
                FilesScanned = filesScanned,
                FilesWithIssues = issues.Count,
                Issues = issues
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error diagnosing YAML in {Path}", path);
            return new YamlDiagnosisResult
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                FilesScanned = 0,
                FilesWithIssues = 0
            };
        }
    }

    /// <inheritdoc/>
    public async Task<TagOperationResult> CheckMetadataAsync(string? path = null, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking metadata in {Path}, DryRun={DryRun}", path ?? "vault", dryRun);

        try
        {
            var resolvedPath = ResolvePath(path);
            if (!ValidatePath(resolvedPath, out var validationError))
            {
                return CreateErrorResult($"Invalid path: {validationError}", dryRun);
            }

            var processor = CreateTagProcessor(dryRun, verbose: false);
            var stats = await processor.CheckAndEnforceMetadataConsistencyAsync(resolvedPath);

            return CreateResult(stats, dryRun, "Checked metadata consistency");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking metadata in {Path}", path);
            return CreateErrorResult($"Error: {ex.Message}", dryRun);
        }
    }

    /// <inheritdoc/>
    public async Task<TagOperationResult> CleanIndexFilesAsync(string? path = null, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning index files in {Path}, DryRun={DryRun}", path ?? "vault", dryRun);

        try
        {
            var resolvedPath = ResolvePath(path);
            if (!ValidatePath(resolvedPath, out var validationError))
            {
                return CreateErrorResult($"Invalid path: {validationError}", dryRun);
            }

            var processor = CreateTagProcessor(dryRun, verbose: false);

            // Find all index files
            var indexFiles = new List<string>();
            if (Directory.Exists(resolvedPath))
            {
                indexFiles.AddRange(Directory.GetFiles(resolvedPath, "_index.md", SearchOption.AllDirectories));
                indexFiles.AddRange(Directory.GetFiles(resolvedPath, "index.md", SearchOption.AllDirectories));
            }
            else if (File.Exists(resolvedPath) && IsIndexFile(resolvedPath))
            {
                indexFiles.Add(resolvedPath);
            }

            var filesProcessed = 0;
            var filesModified = 0;
            var filesWithErrors = 0;

            foreach (var indexFile in indexFiles)
            {
                filesProcessed++;
                try
                {
                    var content = await File.ReadAllTextAsync(indexFile, cancellationToken);
                    var frontmatterYaml = _yamlHelper.ExtractFrontmatter(content);
                    if (string.IsNullOrWhiteSpace(frontmatterYaml))
                    {
                        continue; // No frontmatter to process
                    }

                    var frontmatter = _yamlHelper.ParseYamlToDictionary(frontmatterYaml);
                    var body = _yamlHelper.RemoveFrontmatter(content);

                    var success = await processor.ClearTagsFromFileAsync(indexFile, frontmatter, body);
                    if (success)
                    {
                        filesModified++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing index file {File}", indexFile);
                    filesWithErrors++;
                }
            }

            return new TagOperationResult
            {
                Success = filesWithErrors == 0,
                Message = $"Cleaned {filesModified} of {filesProcessed} index files",
                FilesProcessed = filesProcessed,
                FilesModified = filesModified,
                TagsAdded = 0,
                FilesWithErrors = filesWithErrors,
                DryRun = dryRun
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning index files in {Path}", path);
            return CreateErrorResult($"Error: {ex.Message}", dryRun);
        }
    }

    #region Private Helpers

    private TagProcessor CreateTagProcessor(bool dryRun, bool verbose)
    {
        var tagProcessorLogger = _loggerFactory.CreateLogger<TagProcessor>();
        var failedLogger = _loggerFactory.CreateLogger("FailedOperations");

        return new TagProcessor(
            tagProcessorLogger,
            failedLogger,
            _yamlHelper,
            dryRun,
            verbose,
            resolverRegistry: _schemaLoader?.ResolverRegistry);
    }

    private string ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return _vaultRootPath;
        }

        // If it's already an absolute path, return it
        if (Path.IsPathRooted(path))
        {
            return PathUtils.NormalizePath(path);
        }

        // Resolve relative path against vault root
        return PathUtils.NormalizePath(Path.Combine(_vaultRootPath, path));
    }

    private bool ValidatePath(string resolvedPath, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            error = "Path is empty";
            return false;
        }

        // Check if path exists
        if (!Directory.Exists(resolvedPath) && !File.Exists(resolvedPath))
        {
            error = $"Path does not exist: {resolvedPath}";
            return false;
        }

        // Validate path is within vault (security check)
        var normalizedPath = Path.GetFullPath(resolvedPath).Replace('\\', '/');
        var normalizedVaultRoot = Path.GetFullPath(_vaultRootPath).Replace('\\', '/');

        if (!normalizedPath.StartsWith(normalizedVaultRoot, StringComparison.OrdinalIgnoreCase))
        {
            error = $"Path is outside the vault root: {resolvedPath}";
            return false;
        }

        return true;
    }

    private static bool IsIndexFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.Equals("_index.md", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("index.md", StringComparison.OrdinalIgnoreCase);
    }

    private static TagOperationResult CreateResult(Dictionary<string, int> stats, bool dryRun, string operation)
    {
        var filesProcessed = stats.GetValueOrDefault("FilesProcessed", 0);
        var filesModified = stats.GetValueOrDefault("FilesModified", 0);
        var tagsAdded = stats.GetValueOrDefault("TagsAdded", 0);
        var filesWithErrors = stats.GetValueOrDefault("FilesWithErrors", 0);

        var dryRunPrefix = dryRun ? "[DRY RUN] " : "";
        var message = $"{dryRunPrefix}{operation}: processed {filesProcessed} files, modified {filesModified}, added {tagsAdded} tags";

        if (filesWithErrors > 0)
        {
            message += $", {filesWithErrors} errors";
        }

        return new TagOperationResult
        {
            Success = filesWithErrors == 0,
            Message = message,
            FilesProcessed = filesProcessed,
            FilesModified = filesModified,
            TagsAdded = tagsAdded,
            FilesWithErrors = filesWithErrors,
            DryRun = dryRun
        };
    }

    private static TagOperationResult CreateErrorResult(string error, bool dryRun)
    {
        return new TagOperationResult
        {
            Success = false,
            Message = error,
            FilesProcessed = 0,
            FilesModified = 0,
            TagsAdded = 0,
            FilesWithErrors = 1,
            DryRun = dryRun,
            ErrorMessage = error
        };
    }

    private static string? GetSuggestedFix(string diagnosticMessage)
    {
        // Provide common fix suggestions based on diagnostic patterns
        if (diagnosticMessage.Contains("missing colon", StringComparison.OrdinalIgnoreCase))
        {
            return "Add a colon after the key name (e.g., 'title: My Title')";
        }

        if (diagnosticMessage.Contains("indentation", StringComparison.OrdinalIgnoreCase))
        {
            return "Use consistent indentation (2 spaces recommended)";
        }

        if (diagnosticMessage.Contains("quote", StringComparison.OrdinalIgnoreCase))
        {
            return "Wrap values containing special characters in quotes";
        }

        if (diagnosticMessage.Contains("frontmatter", StringComparison.OrdinalIgnoreCase))
        {
            return "Ensure frontmatter is enclosed by '---' on separate lines";
        }

        return null;
    }

    #endregion
}
