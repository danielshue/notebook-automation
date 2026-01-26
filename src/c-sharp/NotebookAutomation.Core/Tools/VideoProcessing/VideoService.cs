// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.VideoTranscriptProcessing;

namespace NotebookAutomation.Core.Tools.VideoProcessing;

/// <summary>
/// Service for video processing operations.
/// </summary>
/// <remarks>
/// This service wraps <see cref="VideoNoteBatchProcessor"/> and
/// <see cref="VideoTranscriptConsolidationService"/> to provide a unified API for Copilot tools.
/// </remarks>
/// <param name="logger">The logger instance.</param>
/// <param name="videoBatchProcessor">The video batch processor.</param>
/// <param name="consolidationService">The transcript consolidation service.</param>
/// <param name="appConfig">The application configuration.</param>
/// <param name="userSecrets">The user secrets helper for API keys.</param>
public class VideoService(
    ILogger<VideoService> logger,
    VideoNoteBatchProcessor videoBatchProcessor,
    VideoTranscriptConsolidationService consolidationService,
    AppConfig appConfig,
    UserSecretsHelper userSecrets) : IVideoService
{
    private readonly ILogger<VideoService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly VideoNoteBatchProcessor _videoBatchProcessor = videoBatchProcessor ?? throw new ArgumentNullException(nameof(videoBatchProcessor));
    private readonly VideoTranscriptConsolidationService _consolidationService = consolidationService ?? throw new ArgumentNullException(nameof(consolidationService));
    private readonly AppConfig _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
    private readonly UserSecretsHelper _userSecrets = userSecrets ?? throw new ArgumentNullException(nameof(userSecrets));

    /// <inheritdoc />
    public async Task<VideoOperationResult> CreateNotesAsync(
        string inputPath,
        string? outputPath = null,
        bool dryRun = false,
        bool noSummary = false,
        bool forceOverwrite = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating video notes from path: {Path}", inputPath);

        try
        {
            var resolvedInput = ResolvePath(inputPath, useOneDriveRoot: true);
            if (!ValidatePath(resolvedInput, out var validationError))
            {
                return CreateErrorResult($"Invalid path: {validationError}");
            }

            var resolvedOutput = outputPath != null
                ? ResolvePath(outputPath, useOneDriveRoot: false)
                : GetDefaultOutputPath();

            var videoExtensions = _appConfig.VideoExtensions.Count > 0
                ? _appConfig.VideoExtensions
                : [".mp4", ".mov", ".avi", ".mkv", ".webm"];

            var apiKey = noSummary ? null : _userSecrets.GetOpenAIApiKey();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = await _videoBatchProcessor.ProcessVideosAsync(
                resolvedInput,
                resolvedOutput,
                videoExtensions,
                apiKey,
                dryRun,
                noSummary,
                forceOverwrite,
                retryFailed: false,
                timeoutSeconds: null,
                resourcesRoot: _appConfig.Paths?.OnedriveFullpathRoot,
                appConfig: _appConfig,
                noShareLinks: false
            );

            stopwatch.Stop();

            return new VideoOperationResult
            {
                Success = result.Failed == 0,
                Message = result.Summary,
                FilesFound = result.Processed + result.Failed,
                NotesCreated = result.Processed,
                Failed = result.Failed,
                DryRun = dryRun,
                ProcessingTime = stopwatch.Elapsed,
                TotalTokens = result.TotalTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating video notes from {Path}", inputPath);
            return CreateErrorResult($"Error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<VideoConsolidationResult> ConsolidateTranscriptsAsync(
        string inputPath,
        bool recursive = false,
        bool force = false,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Consolidating transcripts from path: {Path}", inputPath);

        try
        {
            var resolvedPath = ResolvePath(inputPath, useOneDriveRoot: true);
            if (!ValidatePath(resolvedPath, out var validationError))
            {
                return CreateConsolidationErrorResult($"Invalid path: {validationError}");
            }

            var request = new VideoTranscriptConsolidationRequest(
                resolvedPath,
                recursive,
                force,
                dryRun);

            var result = await _consolidationService.ConsolidateAsync(request, cancellationToken);

            return new VideoConsolidationResult
            {
                Success = true,
                Message = $"Consolidated {result.AggregatedCount} transcripts into {result.OutputPath}",
                OutputPath = result.OutputPath,
                TranscriptsAggregated = result.AggregatedCount,
                Skipped = result.SkippedCount,
                WasWritten = result.WasWritten,
                DryRun = dryRun
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consolidating transcripts from {Path}", inputPath);
            return CreateConsolidationErrorResult($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Resolves a path, handling relative and absolute paths.
    /// </summary>
    private string ResolvePath(string path, bool useOneDriveRoot)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        var root = useOneDriveRoot
            ? _appConfig.Paths?.GetEffectiveOneDriveRoot()
            : _appConfig.Paths?.GetEffectiveVaultRoot();

        if (string.IsNullOrEmpty(root))
        {
            root = useOneDriveRoot
                ? _appConfig.Paths?.OnedriveFullpathRoot
                : _appConfig.Paths?.NotebookVaultFullpathRoot;
        }

        if (string.IsNullOrEmpty(root))
        {
            throw new InvalidOperationException(
                useOneDriveRoot
                    ? "OneDrive root path is not configured"
                    : "Vault root path is not configured");
        }

        return Path.GetFullPath(Path.Combine(root, path.TrimStart('/', '\\')));
    }

    /// <summary>
    /// Validates that a path exists and is within allowed bounds.
    /// </summary>
    private bool ValidatePath(string path, out string? error)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Path cannot be empty";
            return false;
        }

        // For directories, check if it exists
        if (Directory.Exists(path))
        {
            error = null;
            return true;
        }

        // For files, check if it exists
        if (File.Exists(path))
        {
            error = null;
            return true;
        }

        error = $"Path does not exist: {path}";
        return false;
    }

    /// <summary>
    /// Gets the default output path for video notes.
    /// </summary>
    private string GetDefaultOutputPath()
    {
        var vaultRoot = _appConfig.Paths?.GetEffectiveVaultRoot()
            ?? _appConfig.Paths?.NotebookVaultFullpathRoot;

        if (string.IsNullOrEmpty(vaultRoot))
        {
            throw new InvalidOperationException("Vault root path is not configured");
        }

        return vaultRoot;
    }

    /// <summary>
    /// Creates an error result for CreateNotes operation.
    /// </summary>
    private static VideoOperationResult CreateErrorResult(string message)
    {
        return new VideoOperationResult
        {
            Success = false,
            Message = message,
            ErrorMessage = message,
            FilesFound = 0,
            NotesCreated = 0,
            Failed = 0,
            DryRun = false,
            ProcessingTime = TimeSpan.Zero,
            TotalTokens = 0
        };
    }

    /// <summary>
    /// Creates an error result for ConsolidateTranscripts operation.
    /// </summary>
    private static VideoConsolidationResult CreateConsolidationErrorResult(string message)
    {
        return new VideoConsolidationResult
        {
            Success = false,
            Message = message,
            ErrorMessage = message,
            OutputPath = null,
            TranscriptsAggregated = 0,
            Skipped = 0,
            WasWritten = false,
            DryRun = false
        };
    }
}
