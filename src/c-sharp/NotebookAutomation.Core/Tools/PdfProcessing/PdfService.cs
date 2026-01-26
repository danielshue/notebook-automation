// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Core.Tools.PdfProcessing;

/// <summary>
/// Service for PDF processing operations.
/// </summary>
/// <remarks>
/// This service wraps <see cref="PdfNoteBatchProcessor"/> to provide a unified API for Copilot tools.
/// </remarks>
/// <param name="logger">The logger instance.</param>
/// <param name="pdfBatchProcessor">The PDF batch processor.</param>
/// <param name="appConfig">The application configuration.</param>
/// <param name="userSecrets">The user secrets helper for API keys.</param>
public class PdfService(
    ILogger<PdfService> logger,
    PdfNoteBatchProcessor pdfBatchProcessor,
    AppConfig appConfig,
    UserSecretsHelper userSecrets) : IPdfService
{
    private readonly ILogger<PdfService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly PdfNoteBatchProcessor _pdfBatchProcessor = pdfBatchProcessor ?? throw new ArgumentNullException(nameof(pdfBatchProcessor));
    private readonly AppConfig _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
    private readonly UserSecretsHelper _userSecrets = userSecrets ?? throw new ArgumentNullException(nameof(userSecrets));

    /// <inheritdoc />
    public async Task<PdfOperationResult> ConvertAsync(
        string inputPath,
        string? outputPath = null,
        bool dryRun = false,
        bool noSummary = false,
        bool forceOverwrite = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Converting PDFs from path: {Path}", inputPath);

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

            var pdfExtensions = _appConfig.PdfExtensions.Count > 0
                ? _appConfig.PdfExtensions
                : [".pdf"];

            var apiKey = noSummary ? null : _userSecrets.GetOpenAIApiKey();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = await _pdfBatchProcessor.ProcessPdfsAsync(
                resolvedInput,
                resolvedOutput,
                pdfExtensions,
                apiKey,
                dryRun,
                noSummary,
                forceOverwrite,
                retryFailed: false,
                timeoutSeconds: null,
                resourcesRoot: _appConfig.Paths?.OnedriveFullpathRoot,
                appConfig: _appConfig
            );

            stopwatch.Stop();

            return new PdfOperationResult
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
            _logger.LogError(ex, "Error converting PDFs from {Path}", inputPath);
            return CreateErrorResult($"Error: {ex.Message}");
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
    /// Gets the default output path for PDF notes.
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
    /// Creates an error result for Convert operation.
    /// </summary>
    private static PdfOperationResult CreateErrorResult(string message)
    {
        return new PdfOperationResult
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
}
