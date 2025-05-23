using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Core.Tools.Shared
{
    /// <summary>
    /// Generic batch processor for document note processors (PDF, video, etc.).
    /// Handles file discovery, error handling, and output for any DocumentNoteProcessorBase subclass.
    /// </summary>
    public class DocumentNoteBatchProcessor<TProcessor> where TProcessor : DocumentNoteProcessorBase
    {
        private readonly ILogger _logger;
        private readonly TProcessor _processor;

        public DocumentNoteBatchProcessor(ILogger logger, TProcessor processor)
        {
            _logger = logger;
            _processor = processor;
        }        /// <summary>
        /// Processes one or more document files, generating markdown notes for each, with extended options.
        /// </summary>
        /// <param name="input">Input file path or directory containing files.</param>
        /// <param name="output">Output directory where markdown notes will be saved.</param>
        /// <param name="fileExtensions">List of file extensions to recognize as valid files.</param>
        /// <param name="openAiApiKey">Optional OpenAI API key for generating summaries.</param>
        /// <param name="dryRun">If true, simulates processing without writing output files.</param>
        /// <param name="noSummary">If true, disables OpenAI summary generation.</param>
        /// <param name="forceOverwrite">If true, overwrites existing notes.</param>
        /// <param name="retryFailed">If true, retries only failed files from previous run.</param>
        /// <param name="timeoutSeconds">Optional API request timeout in seconds.</param>
        /// <param name="resourcesRoot">Optional override for resources root directory.</param>
        /// <param name="appConfig">The application configuration object.</param>
        /// <param name="noteType">Type of note (e.g., "PDF Note", "Video Note").</param>
        /// <param name="failedFilesListName">Name of the failed files list file (defaults to "failed_files.txt").</param>
        /// <returns>A tuple containing the count of successfully processed files and the count of failures.</returns>
        public async Task<(int processed, int failed)> ProcessDocumentsAsync(
            string input,
            string? output,
            List<string> fileExtensions,
            string? openAiApiKey,
            bool dryRun = false,
            bool noSummary = false,
            bool forceOverwrite = false,
            bool retryFailed = false,
            int? timeoutSeconds = null,
            string? resourcesRoot = null,
            AppConfig? appConfig = null,
            string noteType = "Document Note",
            string failedFilesListName = "failed_files.txt")
        {
            var processed = 0;
            var failed = 0;
            var files = new List<string>();
            string effectiveInput = input;
            string effectiveOutput = output ?? appConfig?.Paths?.NotebookVaultRoot ?? "Generated";

            if (string.IsNullOrWhiteSpace(effectiveInput))
            {
                _logger.LogError("Input path is required. Config: {Config}", appConfig?.Paths?.NotebookVaultRoot);
                return (0, 1);
            }

            if (Directory.Exists(effectiveInput))
            {
                foreach (var ext in fileExtensions)
                {
                    files.AddRange(Directory.GetFiles(effectiveInput, "*" + ext, SearchOption.AllDirectories));
                }
                _logger.LogInformation("Found {Count} files in directory: {Dir}", files.Count, effectiveInput);
            }
            else if (File.Exists(effectiveInput) && fileExtensions.Exists(ext => effectiveInput.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                files.Add(effectiveInput);
            }
            else
            {
                _logger.LogError("Input must be a file or directory containing valid files: {Input}. Config: {Config}", effectiveInput, appConfig?.Paths?.NotebookVaultRoot);
                return (0, 1);
            }            // If retryFailed is set, filter files to only those that failed in previous run
            if (retryFailed)
            {
                var failedListPath = Path.Combine(effectiveOutput ?? "Generated", failedFilesListName);
                if (File.Exists(failedListPath))
                {
                    var failedFiles = new HashSet<string>(File.ReadAllLines(failedListPath));
                    files = files.FindAll(f => failedFiles.Contains(f));
                    _logger.LogInformation("Retrying {Count} previously failed files.", files.Count);
                }
                else
                {
                    _logger.LogWarning("No {FileName} found for retry; processing all files.", failedFilesListName);
                }
            }

            var failedFilesForRetry = new List<string>();
            foreach (var filePath in files)
            {
                try
                {
                    _logger.LogInformation("Processing file: {FilePath}", filePath);
                    string outputDir = effectiveOutput ?? "Generated";
                    Directory.CreateDirectory(outputDir);
                    string outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(filePath) + ".md");

                    // If not forceOverwrite and file exists, skip
                    if (!forceOverwrite && File.Exists(outputPath))
                    {
                        _logger.LogWarning("Output file exists and --force not set, skipping: {OutputPath}", outputPath);
                        continue;
                    }

                    // Determine effective resources root
                    string? effectiveResourcesRoot = resourcesRoot;
                    if (string.IsNullOrWhiteSpace(effectiveResourcesRoot) && appConfig != null)
                    {
                        effectiveResourcesRoot = appConfig.Paths?.ResourcesRoot;
                    }

                    // Extraction and note generation
                    var (text, metadata) = await _processor.ExtractTextAndMetadataAsync(filePath);
                    if (!string.IsNullOrWhiteSpace(effectiveResourcesRoot))
                    {
                        metadata["resources_root"] = effectiveResourcesRoot;
                    }
                    string summary;
                    if (noSummary)
                    {
                        summary = "[Summary generation disabled by --no-summary flag.]";
                    }
                    else
                    {
                        summary = await _processor.GenerateAiSummaryAsync(text, openAiApiKey);
                    }
                    string markdown = _processor.GenerateMarkdownNote(summary, metadata, noteType);

                    if (!dryRun)
                    {
                        await File.WriteAllTextAsync(outputPath, markdown);
                        _logger.LogInformation("Markdown note saved to: {OutputPath}", outputPath);
                    }
                    else
                    {
                        _logger.LogInformation("[DRY RUN] Markdown note would be generated for: {FilePath}", filePath);
                    }
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process file: {FilePath}", filePath);
                    failedFilesForRetry.Add(filePath);
                    failed++;
                }
            }            // Write failed files for retry if any
            if (failedFilesForRetry.Count > 0 && !dryRun)
            {
                var failedListPath = Path.Combine(effectiveOutput ?? "Generated", failedFilesListName);
                File.WriteAllLines(failedListPath, failedFilesForRetry);
                _logger.LogInformation("Wrote failed file list to: {Path}", failedListPath);
            }
            _logger.LogInformation("Document processing completed. Success: {Processed}, Failed: {Failed}", processed, failed);
            return (processed, failed);
        }
    }
}
