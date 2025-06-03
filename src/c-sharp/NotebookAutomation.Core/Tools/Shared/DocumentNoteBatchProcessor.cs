using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Models;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NotebookAutomation.Core.Tools.Shared
{
    /// <summary>
    /// Generic batch processor for document note processors (PDF, video, etc.).
    /// Handles file discovery, error handling, and output for any DocumentNoteProcessorBase subclass.
    /// </summary>
    public partial class DocumentNoteBatchProcessor<TProcessor> where TProcessor : DocumentNoteProcessorBase
    {
        private readonly ILogger<DocumentNoteBatchProcessor<TProcessor>> _logger;
        private readonly TProcessor _processor;
        private readonly AISummarizer _aiSummarizer;

        // Queue-related fields
        private readonly List<QueueItem> _processingQueue = [];
        private readonly Lock _queueLock = new();

        [GeneratedRegex(@"^##\s+Notes\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline, "en-US")]
        private static partial System.Text.RegularExpressions.Regex MyRegex();

        private readonly Dictionary<string, List<string>> _documentTypeExtensions = new()
        {
            ["PDF"] = [".pdf"],
            ["VIDEO"] = [".mp4", ".mov", ".avi", ".mkv", ".wmv"]
        };

        /// <summary>
        /// Event triggered when processing progress changes.
        /// </summary>
        public event EventHandler<DocumentProcessingProgressEventArgs>? ProcessingProgressChanged;

        /// <summary>
        /// Event triggered when the processing queue changes.
        /// </summary>
        public event EventHandler<QueueChangedEventArgs>? QueueChanged;

        /// <summary>
        /// Gets a read-only view of the current processing queue
        /// </summary>
        public IReadOnlyList<QueueItem> Queue
        {
            get
            {
                lock (_queueLock)
                {
                    return _processingQueue.ToList().AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Raises the ProcessingProgressChanged event.
        /// </summary>
        /// <param name="filePath">The path of the file being processed.</param>
        /// <param name="status">The current processing status message.</param>
        /// <param name="currentFile">The current file index being processed.</param>
        /// <param name="totalFiles">The total number of files to process.</param>
        protected virtual void OnProcessingProgressChanged(string filePath, string status, int currentFile, int totalFiles)
        {
            ProcessingProgressChanged?.Invoke(this, new DocumentProcessingProgressEventArgs(filePath, status, currentFile, totalFiles));
        }

        /// <summary>
        /// Raises the QueueChanged event.
        /// </summary>
        /// <param name="changedItem">The specific item that changed, or null if the entire queue changed</param>
        protected virtual void OnQueueChanged(QueueItem? changedItem = null)
        {
            lock (_queueLock)
            {
                QueueChanged?.Invoke(this, new QueueChangedEventArgs(_processingQueue.AsReadOnly(), changedItem));
            }
        }

        /// <summary>
        /// Determines the document type based on file extension
        /// </summary>
        /// <param name="filePath">File path to check</param>
        /// <returns>Document type (e.g., "PDF", "VIDEO")</returns>
        protected virtual string GetDocumentType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            foreach (var kvp in _documentTypeExtensions)
            {
                if (kvp.Value.Contains(extension))
                    return kvp.Key;
            }

            // Fallback: check if the processor is specialized
            if (typeof(TProcessor).Name.Contains("Pdf", StringComparison.OrdinalIgnoreCase))
                return "PDF";
            if (typeof(TProcessor).Name.Contains("Video", StringComparison.OrdinalIgnoreCase))
                return "VIDEO";

            return "DOCUMENT"; // Generic fallback
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentNoteBatchProcessor{TProcessor}"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="processor">The document note processor instance.</param>
        /// <param name="aiSummarizer">The AI summarizer instance.</param>
        public DocumentNoteBatchProcessor(ILogger logger, TProcessor processor, AISummarizer aiSummarizer)
        {
            if (logger is ILogger<DocumentNoteBatchProcessor<TProcessor>> genericLogger)
            {
                _logger = genericLogger;
            }
            else
            {
                // Allow any ILogger for testing/mocking, but warn if not the expected type
                _logger = logger as ILogger<DocumentNoteBatchProcessor<TProcessor>> ?? throw new ArgumentException("Logger must be ILogger<DocumentNoteBatchProcessor<TProcessor>> or compatible mock");
                if (logger.GetType().Name.Contains("Mock") || logger.GetType().Name.Contains("Proxy"))
                {
                    // Allow for test mocks
                }
                else
                {
                    throw new ArgumentException("Logger must be ILogger<DocumentNoteBatchProcessor<TProcessor>>");
                }
            }
            _processor = processor;
            _aiSummarizer = aiSummarizer ?? throw new ArgumentNullException(nameof(aiSummarizer));
        }

        /// <summary>
        /// Processes one or more document files, generating markdown notes for each, with extended options.
        /// Returns a BatchProcessResult with summary and statistics for CLI/UI output.
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
        /// <param name="resourcesRoot">Optional override for OneDrive fullpath root directory.</param>
        /// <param name="appConfig">The application configuration object.</param>
        /// <param name="noteType">Type of note (e.g., "PDF Note", "Video Note").</param>
        /// <param name="failedFilesListName">Name of the failed files list file (defaults to "failed_files.txt").</param>
        /// <param name="noShareLinks">If true, skips OneDrive share link creation.</param>
        /// <returns>A BatchProcessResult containing processing statistics and summary information.</returns>
        public virtual async Task<BatchProcessResult> ProcessDocumentsAsync(
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
            string failedFilesListName = "failed_files.txt",
            bool noShareLinks = false)
        {
            // Validate input parameters and setup processing
            var setupResult = ValidateAndSetupProcessing(input, output, appConfig);
            if (!setupResult.HasValue)
            {
                return DocumentNoteBatchProcessor<TProcessor>.CreateErrorResult("Input validation failed");
            }

            var (effectiveInput, effectiveOutput) = setupResult.Value;

            // Clear any existing queue
            lock (_queueLock)
            {
                _processingQueue.Clear();
            }

            // Discover and filter files to process
            var files = DiscoverAndFilterFiles(effectiveInput, fileExtensions, retryFailed, effectiveOutput, failedFilesListName);
            if (files == null)
            {
                return DocumentNoteBatchProcessor<TProcessor>.CreateErrorResult("File discovery failed");
            }

            // Initialize processing queue with discovered files
            InitializeProcessingQueue(files);

            // Process all files
            var failedFilesForRetry = new List<string>();
            var batchStopwatch = Stopwatch.StartNew();
            var totalSummaryTime = TimeSpan.Zero;
            var totalTokens = 0;
            int processed = 0;
            int failed = 0;

            // Determine effective resources root
            string? effectiveResourcesRoot = resourcesRoot;
            if (string.IsNullOrWhiteSpace(effectiveResourcesRoot) && appConfig != null)
            {
                effectiveResourcesRoot = appConfig.Paths?.OnedriveFullpathRoot;
            }

            // Process each file
            for (int fileIndex = 0; fileIndex < files.Count; fileIndex++)
            {
                var filePath = files[fileIndex];
                var fileStopwatch = Stopwatch.StartNew();

                // Get the queue item for this file
                QueueItem? queueItem = null;
                lock (_queueLock)
                {
                    queueItem = _processingQueue.FirstOrDefault(q => q.FilePath == filePath);
                    if (queueItem != null)
                    {
                        // Update queue item status
                        queueItem.Status = DocumentProcessingStatus.Processing;
                        queueItem.Stage = ProcessingStage.NotStarted;
                        queueItem.StatusMessage = $"Processing {queueItem.DocumentType} file {fileIndex + 1}/{files.Count}: {Path.GetFileName(filePath)}";
                        queueItem.ProcessingStartTime = DateTime.Now;
                    }
                }

                // Notify queue change and send progress event
                if (queueItem != null)
                {
                    OnQueueChanged(queueItem);
                }

                OnProcessingProgressChanged(
                    filePath,
                    queueItem?.StatusMessage ?? $"Processing file {fileIndex + 1}/{files.Count}: {Path.GetFileName(filePath)}",
                    fileIndex + 1,
                    files.Count);                // Process the single file using the refactored helper method
                var (success, errorMessage) = await ProcessSingleFileAsync(
                    filePath, queueItem, fileIndex + 1, files.Count, effectiveOutput, effectiveResourcesRoot,
                    forceOverwrite, dryRun, openAiApiKey, noSummary, timeoutSeconds, noShareLinks, noteType);

                if (success)
                {
                    // Check if the file was skipped due to existing file and forceOverwrite: false
                    if (errorMessage?.Contains("Skipped") == true)
                    {
                        // Don't count skipped files as processed
                        _logger.LogDebugWithPath("File skipped: {FilePath}", filePath);
                    }
                    else
                    {
                        processed++;
                    }
                }
                else
                {
                    failed++;
                    failedFilesForRetry.Add(filePath);
                }

                fileStopwatch.Stop();
                _logger.LogInformationWithPath("Processing file: {FilePath} took {ElapsedMs} ms", filePath, fileStopwatch.ElapsedMilliseconds);

                // Report progress after each file
                OnProcessingProgressChanged(filePath, "Processed", processed, files.Count);
            }

            batchStopwatch.Stop();

            // Write failed files for retry if any
            if (failedFilesForRetry.Count > 0 && !dryRun)
            {
                var failedListPath = Path.Combine(effectiveOutput, failedFilesListName);
                File.WriteAllLines(failedListPath, failedFilesForRetry);
                _logger.LogInformationWithPath("Wrote failed file list to: {FilePath}", failedListPath);
            }

            // Calculate total summary time and tokens from queue items
            lock (_queueLock)
            {
                foreach (var item in _processingQueue)
                {
                    if (item.ProcessingStartTime.HasValue && item.ProcessingEndTime.HasValue)
                    {
                        var processingTime = item.ProcessingEndTime.Value - item.ProcessingStartTime.Value;
                        totalSummaryTime = totalSummaryTime.Add(processingTime);
                    }

                    if (item.Metadata.TryGetValue("TokenCount", out object? value) && value is int tokenCount)
                    {
                        totalTokens += tokenCount;
                    }
                }
            }

            // Compile and return results
            return CompileProcessingResults(processed, failed, batchStopwatch.Elapsed, totalSummaryTime, totalTokens);
        }

        /// <summary>
        /// Creates an error result for failed operations
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <returns>BatchProcessResult indicating failure</returns>
        private static BatchProcessResult CreateErrorResult(string errorMessage)
        {
            return new BatchProcessResult
            {
                Processed = 0,
                Failed = 1,
                Summary = $"Error: {errorMessage}",
                TotalBatchTime = TimeSpan.Zero,
                TotalSummaryTime = TimeSpan.Zero,
                TotalTokens = 0,
                AverageFileTimeMs = 0,
                AverageSummaryTimeMs = 0,
                AverageTokens = 0
            };
        }
        /// <summary>
        /// Processes a single file with complete error handling and progress tracking
        /// </summary>
        /// <param name="filePath">Path to the file to process</param>
        /// <param name="queueItem">Queue item for progress tracking</param>
        /// <param name="fileIndex">Current file index</param>
        /// <param name="totalFiles">Total number of files</param>
        /// <param name="effectiveOutput">Output directory</param>
        /// <param name="resourcesRoot">Resources root directory</param>
        /// <param name="forceOverwrite">Whether to overwrite existing files</param>
        /// <param name="dryRun">Whether this is a dry run</param>
        /// <param name="openAiApiKey">OpenAI API key</param>
        /// <param name="noSummary">Whether to skip summary generation</param>
        /// <param name="timeoutSeconds">API timeout in seconds</param>
        /// <param name="noShareLinks">Whether to skip share link generation</param>
        /// <param name="noteType">Type of note to generate</param>
        /// <returns>Tuple indicating success and any error message</returns>
        protected virtual async Task<(bool success, string? errorMessage)> ProcessSingleFileAsync(
            string filePath,
            QueueItem? queueItem,
            int fileIndex,
            int totalFiles,
            string effectiveOutput,
            string? resourcesRoot,
            bool forceOverwrite,
            bool dryRun,
            string? openAiApiKey,
            bool noSummary,
            int? timeoutSeconds,
            bool noShareLinks,
            string noteType)
        {
            try
            {
                _logger.LogInformationWithPath("Processing file: {FilePath}", filePath);
                string outputDir = effectiveOutput;
                Directory.CreateDirectory(outputDir);

                // Generate output path based on processor type and directory structure
                string outputPath = GenerateOutputPath(filePath, outputDir, resourcesRoot);

                // If not forceOverwrite and file exists, skip
                if (!forceOverwrite && File.Exists(outputPath))
                {
                    _logger.LogWarningWithPath("Output file exists and --force not set, skipping: {FilePath}", outputPath);
                    return (true, "Skipped - file exists");
                }

                // Extract content with progress tracking
                var (text, metadata) = await ExtractContentAsync(filePath, queueItem, fileIndex, totalFiles);

                // Add resources root to metadata if specified
                if (!string.IsNullOrWhiteSpace(resourcesRoot))
                {
                    metadata["resources_root"] = resourcesRoot;
                }

                // Generate AI summary with progress tracking
                var (summaryText, summaryTokens, summaryTime) = await GenerateAISummaryAsync(
                    filePath, text, metadata, queueItem, fileIndex, totalFiles,
                    openAiApiKey, noSummary, timeoutSeconds, resourcesRoot, noShareLinks);

                // Generate and save markdown
                await GenerateAndSaveMarkdownAsync(
                    filePath, summaryText, metadata, noteType, outputPath,
                    queueItem, fileIndex, totalFiles, forceOverwrite, dryRun);

                return (true, null);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to process: {ex.Message}";
                _logger.LogErrorWithPath(ex, "Failed to process file: {FilePath}", filePath);

                // Update queue status for failure
                if (queueItem != null)
                {
                    queueItem.Status = DocumentProcessingStatus.Failed;
                    queueItem.StatusMessage = errorMessage;
                    queueItem.ProcessingEndTime = DateTime.Now;
                    OnQueueChanged(queueItem);
                }

                return (false, errorMessage);
            }
        }

        /// <summary>
        /// Generates markdown content and saves it to file with progress tracking
        /// </summary>
        /// <param name="filePath">Source file path</param>
        /// <param name="summaryText">Generated summary text</param>
        /// <param name="metadata">File metadata</param>
        /// <param name="noteType">Type of note to generate</param>
        /// <param name="outputPath">Output file path</param>
        /// <param name="queueItem">Queue item for progress tracking</param>
        /// <param name="fileIndex">Current file index</param>
        /// <param name="totalFiles">Total number of files</param>
        /// <param name="forceOverwrite">Whether to overwrite existing files</param>
        /// <param name="dryRun">Whether this is a dry run</param>
        protected virtual async Task GenerateAndSaveMarkdownAsync(
            string filePath,
            string summaryText,
            Dictionary<string, object> metadata,
            string noteType,
            string outputPath,
            QueueItem? queueItem,
            int fileIndex,
            int totalFiles,
            bool forceOverwrite,
            bool dryRun)
        {
            string markdown;

            // Handle video processor special case
            if (typeof(TProcessor).Name.Contains("Video"))
            {
                // Video processors handle their own markdown generation
                // The markdown was already generated in GenerateAISummaryAsync
                markdown = summaryText; // For video processors, summaryText contains the full markdown
            }
            else
            {
                // Update queue status for markdown generation
                if (queueItem != null)
                {
                    queueItem.Stage = ProcessingStage.MarkdownCreation;
                    queueItem.StatusMessage = $"Generating markdown content ({fileIndex}/{totalFiles}): {Path.GetFileName(filePath)}";
                    OnQueueChanged(queueItem);
                }

                // Update progress to show markdown generation
                OnProcessingProgressChanged(
                    filePath,
                    queueItem?.StatusMessage ?? $"Generating markdown content ({fileIndex}/{totalFiles}): {Path.GetFileName(filePath)}",
                    fileIndex,
                    totalFiles);

                // Generate markdown using processor
                markdown = _processor.GenerateMarkdownNote(summaryText, metadata, noteType, includeNoteTypeTitle: false);
            }

            // Ensure markdown is initialized
            if (markdown == null)
            {
                _logger.LogErrorWithPath("Markdown generation failed for file: {FilePath}", filePath);
                markdown = "Error generating markdown content";
            }

            // Check if existing file has readonly auto-generated-state
            bool isReadOnly = false;
            if (File.Exists(outputPath))
            {
                var yamlHelper = new Utils.YamlHelper(_logger);
                isReadOnly = yamlHelper.IsFileReadOnly(outputPath);
                if (isReadOnly)
                {
                    _logger.LogInformationWithPath("Skipping file modification due to readonly auto-generated-state: {FilePath}", outputPath);
                    return; // Skip to next file
                }
            }

            // For video files, handle content preservation after "## Notes"
            if (typeof(TProcessor).Name.Contains("Video") && File.Exists(outputPath) && !forceOverwrite)
            {
                markdown = PreserveUserContentAfterNotes(outputPath, markdown);
            }

            if (!dryRun)
            {
                // Update queue status for file writing
                if (queueItem != null)
                {
                    queueItem.StatusMessage = $"Writing markdown to file ({fileIndex}/{totalFiles}): {Path.GetFileNameWithoutExtension(filePath)}.md";
                    OnQueueChanged(queueItem);
                }

                // Update progress to show file writing
                OnProcessingProgressChanged(
                    filePath,
                    queueItem?.StatusMessage ?? $"Writing markdown to file ({fileIndex}/{totalFiles}): {Path.GetFileNameWithoutExtension(filePath)}.md",
                    fileIndex,
                    totalFiles);

                await File.WriteAllTextAsync(outputPath, markdown);

                // Update queue status after completion
                if (queueItem != null)
                {
                    queueItem.Stage = ProcessingStage.Completed;
                    queueItem.Status = DocumentProcessingStatus.Completed;
                    queueItem.StatusMessage = $"Successfully saved markdown note ({fileIndex}/{totalFiles}): {Path.GetFileNameWithoutExtension(filePath)}.md";
                    queueItem.ProcessingEndTime = DateTime.Now;
                    OnQueueChanged(queueItem);
                }

                // Update progress after file is written
                OnProcessingProgressChanged(
                    filePath,
                    queueItem?.StatusMessage ?? $"Successfully saved markdown note ({fileIndex}/{totalFiles}): {Path.GetFileNameWithoutExtension(filePath)}.md",
                    fileIndex,
                    totalFiles);

                _logger.LogInformationWithPath("Markdown note saved to: {FilePath}", outputPath);
            }
            else
            {
                // Update queue status for dry run
                if (queueItem != null)
                {
                    queueItem.Stage = ProcessingStage.Completed;
                    queueItem.Status = DocumentProcessingStatus.Completed;
                    queueItem.StatusMessage = $"[DRY RUN] Markdown note would be generated for: {Path.GetFileName(filePath)}";
                    queueItem.ProcessingEndTime = DateTime.Now;
                    OnQueueChanged(queueItem);
                }

                _logger.LogInformationWithPath("[DRY RUN] Markdown note would be generated for: {FilePath}", filePath);
            }
        }

        /// <summary>
        /// Compiles processing results and statistics into a BatchProcessResult
        /// </summary>
        /// <param name="processed">Number of successfully processed files</param>
        /// <param name="failed">Number of failed files</param>
        /// <param name="batchTime">Total batch processing time</param>
        /// <param name="totalSummaryTime">Total AI summary time</param>
        /// <param name="totalTokens">Total tokens used</param>
        /// <returns>Compiled batch processing result</returns>
        protected virtual BatchProcessResult CompileProcessingResults(
            int processed,
            int failed,
            TimeSpan batchTime,
            TimeSpan totalSummaryTime,
            int totalTokens)
        {
            _logger.LogInformation("Document processing completed. Success: {Processed}, Failed: {Failed}", processed, failed);
            _logger.LogInformation("Total batch processing time: {ElapsedMs} ms", batchTime.TotalMilliseconds);
            _logger.LogInformation("Total summary time: {ElapsedMs} ms", totalSummaryTime.TotalMilliseconds);
            _logger.LogInformation("Total tokens for all summaries: {TotalTokens}", totalTokens);

            double avgFileTime = processed > 0 ? batchTime.TotalMilliseconds / processed : 0;
            double avgSummaryTime = processed > 0 ? totalSummaryTime.TotalMilliseconds / processed : 0;
            double avgTokens = processed > 0 ? (double)totalTokens / processed : 0;

            // Helper for formatting time
            string FormatTime(TimeSpan ts)
            {
                if (ts.TotalHours >= 1)
                    return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
                if (ts.TotalMinutes >= 1)
                    return $"{ts.Minutes}m {ts.Seconds}s";
                if (ts.TotalSeconds >= 1)
                    return $"{ts.Seconds}s {ts.Milliseconds}ms";
                return $"{ts.Milliseconds}ms";
            }

            string FormatMs(double ms)
            {
                if (ms >= 60000)
                {
                    var ts = TimeSpan.FromMilliseconds(ms);
                    return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
                }
                if (ms >= 1000)
                {
                    return $"{(ms / 1000):F2}s";
                }
                return $"{ms:F0}ms";
            }

            // Count queue stats by type and status
            Dictionary<string, int> documentTypeStats = [];
            Dictionary<DocumentProcessingStatus, int> statusStats = [];

            lock (_queueLock)
            {
                foreach (var item in _processingQueue)
                {
                    // Count by document type
                    if (!documentTypeStats.TryGetValue(item.DocumentType, out int docTypeValue))
                    {
                        docTypeValue = 0;
                        documentTypeStats[item.DocumentType] = docTypeValue;
                    }
                    documentTypeStats[item.DocumentType] = ++docTypeValue;

                    // Count by status
                    if (!statusStats.TryGetValue(item.Status, out int statusValue))
                    {
                        statusValue = 0;
                        statusStats[item.Status] = statusValue;
                    }
                    statusStats[item.Status] = ++statusValue;
                }
            }
            // Prepare type and status summary
            var typesSummary = string.Join(", ", documentTypeStats.Select(typeEntry => $"{typeEntry.Key}: {typeEntry.Value}"));

            // Prepare summary string for CLI or UI output
            string summary = "\n================ Batch Processing Summary ================\n"
                + $"Files processed: {processed}\n"
                + $"Files failed: {failed}\n";

            // Add document type statistics
            if (documentTypeStats.Count > 0)
            {
                summary += $"Document types: {typesSummary}\n";
            }

            // Add status statistics
            if (statusStats.TryGetValue(DocumentProcessingStatus.Completed, out int completedValue))
            {
                summary += $"Successfully completed: {completedValue}\n";
            }
            if (statusStats.TryGetValue(DocumentProcessingStatus.Failed, out int failedValue))
            {
                summary += $"Failed: {failedValue}\n";
            }

            // Add timing statistics
            summary += $"Total batch time: {FormatTime(batchTime)}\n"
                + $"Average time per file: {FormatMs(avgFileTime)}\n"
                + $"Total summary time: {FormatTime(totalSummaryTime)}\n"
                + $"Average summary time per file: {FormatMs(avgSummaryTime)}\n"
                + $"Total tokens for all summaries: {totalTokens}\n"
                + $"Average tokens per summary: {avgTokens:F2}\n"
                + "==========================================================\n";

            return new BatchProcessResult
            {
                Processed = processed,
                Failed = failed,
                Summary = summary,
                TotalBatchTime = batchTime,
                TotalSummaryTime = totalSummaryTime,
                TotalTokens = totalTokens,
                AverageFileTimeMs = avgFileTime,
                AverageSummaryTimeMs = avgSummaryTime,
                AverageTokens = avgTokens
            };
        }

        /// <summary>
        /// Generates the output path for a processed file, handling video-specific naming and directory structure.
        /// </summary>
        /// <param name="inputFilePath">The input file path.</param>
        /// <param name="outputDir">The base output directory.</param>
        /// <param name="resourcesRoot">The resources root directory for calculating relative paths.</param>
        /// <returns>The output file path.</returns>
        protected virtual string GenerateOutputPath(string inputFilePath, string outputDir, string? resourcesRoot)
        {
            // Check if this is a video processor by checking the processor type
            bool isVideoProcessor = typeof(TProcessor).Name.Contains("Video");

            if (isVideoProcessor)
            {
                // For video files, use -video.md suffix and preserve directory structure
                string fileName = Path.GetFileNameWithoutExtension(inputFilePath) + "-video.md";

                // If we have a resources root, preserve the directory structure
                if (!string.IsNullOrWhiteSpace(resourcesRoot) && Path.IsPathRooted(resourcesRoot))
                {
                    try
                    {
                        // Calculate relative path from resources root
                        var inputFileInfo = new FileInfo(inputFilePath);
                        var resourcesRootInfo = new DirectoryInfo(resourcesRoot);

                        // Get the relative path from resources root to the input file's directory
                        string relativePath = Path.GetRelativePath(resourcesRootInfo.FullName, inputFileInfo.DirectoryName ?? "");

                        // Log the original relative path for debugging
                        _logger.LogDebug("Original relative path from resources root: {RelativePath}", relativePath);

                        // If the relative path starts with "." it means the file is directly in the resources root
                        // In that case, use empty path to put files directly in output directory
                        if (relativePath == "." || relativePath == ".\\")
                        {
                            relativePath = "";
                        }

                        // Create the output directory structure
                        string targetDir = string.IsNullOrEmpty(relativePath) ? outputDir : Path.Combine(outputDir, relativePath);
                        Directory.CreateDirectory(targetDir);

                        var outputPath = Path.Combine(targetDir, fileName);
                        _logger.LogDebug("Generated output path: {OutputPath}", outputPath);

                        return outputPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to calculate relative path from resources root {ResourcesRoot} to {InputFile}, using flat structure",
                            resourcesRoot, inputFilePath);
                        // Fall back to flat structure
                        return Path.Combine(outputDir, fileName);
                    }
                }
                else
                {
                    // No resources root specified, use flat structure with -video.md suffix
                    return Path.Combine(outputDir, fileName);
                }
            }
            else
            {
                // For non-video files, use standard .md suffix and flat structure
                string fileName = Path.GetFileNameWithoutExtension(inputFilePath) + ".md";
                return Path.Combine(outputDir, fileName);
            }
        }

        /// <summary>
        /// Preserves user content that appears after the "## Notes" section in existing video markdown files.
        /// </summary>
        /// <param name="existingFilePath">Path to the existing markdown file.</param>
        /// <param name="newMarkdown">The newly generated markdown content.</param>
        /// <returns>The new markdown with preserved user content appended.</returns>
        protected virtual string PreserveUserContentAfterNotes(string existingFilePath, string newMarkdown)
        {
            try
            {
                if (!File.Exists(existingFilePath))
                {
                    return newMarkdown;
                }

                string existingContent = File.ReadAllText(existingFilePath);

                // Find the "## Notes" section in the existing file
                var notesRegex = MyRegex();

                var match = notesRegex.Match(existingContent);
                if (match.Success)
                {
                    // Extract everything after the "## Notes" line
                    int notesIndex = match.Index + match.Length;
                    string userContent = existingContent[notesIndex..].TrimStart('\r', '\n');

                    if (!string.IsNullOrWhiteSpace(userContent))
                    {
                        // Find the "## Notes" section in the new markdown and append the user content
                        var newNotesMatch = notesRegex.Match(newMarkdown);
                        if (newNotesMatch.Success)
                        {
                            int newNotesIndex = newNotesMatch.Index + newNotesMatch.Length;
                            string beforeNotes = newMarkdown[..newNotesIndex];

                            // Combine the new content up to ## Notes with the preserved user content
                            return beforeNotes + "\n\n" + userContent;
                        }
                        else
                        {
                            // If somehow the new markdown doesn't have "## Notes", just append it
                            return newMarkdown + "\n\n## Notes\n\n" + userContent;
                        }
                    }
                }

                return newMarkdown;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preserve user content from existing file: {FilePath}", existingFilePath);
                return newMarkdown;
            }
        }

        /// <summary>
        /// Validates input parameters and sets up processing configuration
        /// </summary>
        /// <param name="input">Input path</param>
        /// <param name="output">Output path</param>
        /// <param name="appConfig">Application configuration</param>
        /// <returns>Tuple containing effective input and output paths, or null if validation failed</returns>
        protected virtual (string effectiveInput, string effectiveOutput)? ValidateAndSetupProcessing(
            string input,
            string? output,
            AppConfig? appConfig)
        {
            string effectiveInput = input;
            string effectiveOutput = output ?? appConfig?.Paths?.NotebookVaultFullpathRoot ?? "Generated";

            if (string.IsNullOrWhiteSpace(effectiveInput))
            {
                _logger.LogError("Input path is required. Config: {Config}", appConfig?.Paths?.NotebookVaultFullpathRoot);
                return null;
            }

            return (effectiveInput, effectiveOutput);
        }

        /// <summary>
        /// Extracts content and metadata from a file with progress tracking
        /// </summary>
        /// <param name="filePath">Path to the file to extract from</param>
        /// <param name="queueItem">Queue item to update with progress</param>
        /// <param name="fileIndex">Current file index</param>
        /// <param name="totalFiles">Total number of files</param>
        /// <returns>Tuple containing extracted text and metadata</returns>
        protected virtual async Task<(string text, Dictionary<string, object> metadata)> ExtractContentAsync(
            string filePath,
            QueueItem? queueItem,
            int fileIndex,
            int totalFiles)
        {
            // Update queue and progress for content extraction
            if (queueItem != null)
            {
                queueItem.Stage = ProcessingStage.ContentExtraction;
                queueItem.StatusMessage = $"Extracting content from {queueItem.DocumentType} file {fileIndex}/{totalFiles}: {Path.GetFileName(filePath)}";
                OnQueueChanged(queueItem);
            }

            // For backward compatibility with current UI
            OnProcessingProgressChanged(
                filePath,
                queueItem?.StatusMessage ?? $"Extracting content from file {fileIndex}/{totalFiles}: {Path.GetFileName(filePath)}",
                fileIndex,
                totalFiles);

            var (text, metadata) = await _processor.ExtractTextAndMetadataAsync(filePath);

            // Store content length in queue metadata
            if (queueItem != null)
            {
                queueItem.Metadata["ContentLength"] = text.Length;
                queueItem.StatusMessage = $"Extracted {text.Length:N0} characters from {queueItem.DocumentType} file {fileIndex}/{totalFiles}: {Path.GetFileName(filePath)}";
                OnQueueChanged(queueItem);
            }

            // Update progress with content length information
            OnProcessingProgressChanged(
                filePath,
                queueItem?.StatusMessage ?? $"Extracted {text.Length:N0} characters from file {fileIndex}/{totalFiles}: {Path.GetFileName(filePath)}",
                fileIndex,
                totalFiles);

            return (text, metadata);
        }
        /// <summary>
        /// Generates AI summary using processor-specific methods
        /// </summary>
        /// <param name="filePath">Path to the file being processed</param>
        /// <param name="text">Extracted text content</param>
        /// <param name="metadata">File metadata</param>
        /// <param name="queueItem">Queue item to update with progress</param>
        /// <param name="fileIndex">Current file index</param>
        /// <param name="totalFiles">Total number of files</param>
        /// <param name="openAiApiKey">OpenAI API key</param>
        /// <param name="noSummary">Whether to skip summary generation</param>
        /// <param name="timeoutSeconds">API timeout in seconds</param>
        /// <param name="resourcesRoot">Resources root directory</param>
        /// <param name="noShareLinks">Whether to skip share link generation</param>
        /// <returns>Tuple containing summary text, token count, and summary time</returns>
        protected virtual async Task<(string summaryText, int summaryTokens, TimeSpan summaryTime)> GenerateAISummaryAsync(
            string filePath,
            string text,
            Dictionary<string, object> metadata,
            QueueItem? queueItem,
            int fileIndex,
            int totalFiles,
            string? openAiApiKey,
            bool noSummary,
            int? timeoutSeconds,
            string? resourcesRoot,
            bool noShareLinks)
        {
            var summaryStopwatch = Stopwatch.StartNew();
            string summaryText = string.Empty;
            int summaryTokens = 0;

            // Handle video processor special case - it generates full markdown, not just summary
            if (typeof(TProcessor).Name.Contains("Video"))
            {
                // Use reflection to call the specialized GenerateVideoNoteAsync method
                var generateMethod = _processor.GetType().GetMethod("GenerateVideoNoteAsync");
                if (generateMethod != null)
                {
                    _logger.LogDebug("Using specialized GenerateVideoNoteAsync method for video processing");
                    // Pass the noShareLinks parameter to the GenerateVideoNoteAsync method
                    var task = (Task<string>)generateMethod.Invoke(_processor,
                    [
                        filePath,
                        openAiApiKey,
                        null, // promptFileName 
                        noSummary,
                        timeoutSeconds,
                        resourcesRoot,
                        noShareLinks
                    ])!;

                    summaryText = await task; // For video, this contains the full markdown

                    // Estimate tokens
                    var estimateTokenMethod = _aiSummarizer.GetType().GetMethod("EstimateTokenCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (estimateTokenMethod != null && !noSummary)
                    {
                        // Extract summary part from the markdown for token estimation
                        var testSummary = summaryText.Length > 300 ? summaryText[..300] : summaryText;
                        var tokenResult = estimateTokenMethod.Invoke(_aiSummarizer, [testSummary]);
                        if (tokenResult != null)
                        {
                            summaryTokens = (int)tokenResult;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("VideoNoteProcessor found but GenerateVideoNoteAsync method not available. Using base method.");
                    summaryText = await _processor.GenerateAiSummaryAsync(text);
                }
            }
            else if (noSummary)
            {
                summaryText = string.Empty; // No summary when disabled
            }
            else
            {
                bool isPdfProcessor = typeof(TProcessor).Name.Contains("Pdf");

                if (isPdfProcessor)
                {
                    // Use reflection to call the specialized GeneratePdfSummaryAsync method
                    var generateMethod = _processor.GetType().GetMethod("GeneratePdfSummaryAsync");
                    if (generateMethod != null)
                    {
                        _logger.LogDebug("Using specialized GeneratePdfSummaryAsync method for PDF processing");

                        // Update queue status
                        if (queueItem != null)
                        {
                            queueItem.Stage = ProcessingStage.AISummaryGeneration;
                            queueItem.StatusMessage = $"Analyzing PDF content for AI summary ({fileIndex}/{totalFiles}): {Path.GetFileName(filePath)}";
                            OnQueueChanged(queueItem);
                        }

                        // Update progress to show we're preparing for AI summary generation
                        OnProcessingProgressChanged(
                            filePath,
                            queueItem?.StatusMessage ?? $"Analyzing PDF content for AI summary ({fileIndex}/{totalFiles}): {Path.GetFileName(filePath)}",
                            fileIndex,
                            totalFiles);

                        // Pass the metadata to the GeneratePdfSummaryAsync method
                        var task = (Task<string>)generateMethod.Invoke(_processor, [text, metadata, null])!;
                        summaryText = await task;

                        // Update queue status
                        if (queueItem != null)
                        {
                            queueItem.StatusMessage = $"AI summary generated for PDF ({fileIndex}/{totalFiles}): {Path.GetFileName(filePath)}";
                            OnQueueChanged(queueItem);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("PdfNoteProcessor found but GeneratePdfSummaryAsync method not available. Using base method.");
                        summaryText = await _processor.GenerateAiSummaryAsync(text);
                    }
                }
                else
                {
                    // For other processors, use the standard GenerateAiSummaryAsync
                    OnProcessingProgressChanged(
                        filePath,
                        $"Generating AI summary for file {fileIndex}/{totalFiles}: {Path.GetFileName(filePath)}",
                        fileIndex,
                        totalFiles);

                    summaryText = await _processor.GenerateAiSummaryAsync(text);
                }

                // Estimate tokens
                var estimateTokenMethod = _aiSummarizer.GetType().GetMethod("EstimateTokenCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (estimateTokenMethod != null)
                {
                    var tokenResult = estimateTokenMethod.Invoke(_aiSummarizer, [summaryText]);
                    if (tokenResult != null)
                    {
                        summaryTokens = (int)tokenResult;
                        // Add token count to progress message
                        OnProcessingProgressChanged(
                            filePath,
                            $"AI summary completed with {summaryTokens:N0} tokens ({fileIndex}/{totalFiles}): {Path.GetFileName(filePath)}",
                            fileIndex,
                            totalFiles);
                    }
                }
            }

            summaryStopwatch.Stop();
            return (summaryText, summaryTokens, summaryStopwatch.Elapsed);
        }

        /// <summary>
        /// Discovers files to process based on input path and file extensions
        /// </summary>
        /// <param name="effectiveInput">Effective input path</param>
        /// <param name="fileExtensions">List of file extensions to process</param>
        /// <param name="retryFailed">Whether to retry only failed files</param>
        /// <param name="effectiveOutput">Output directory for failed files list</param>
        /// <param name="failedFilesListName">Name of failed files list</param>
        /// <returns>List of files to process, or null if discovery failed</returns>
        protected virtual List<string>? DiscoverAndFilterFiles(
            string effectiveInput,
            List<string> fileExtensions,
            bool retryFailed,
            string effectiveOutput,
            string failedFilesListName)
        {
            var files = new List<string>();

            if (Directory.Exists(effectiveInput))
            {
                foreach (var ext in fileExtensions)
                {
                    files.AddRange(Directory.GetFiles(effectiveInput, "*" + ext, SearchOption.AllDirectories));
                }
                _logger.LogInformationWithPath("Found {Count} files in directory: {FilePath}", effectiveInput, files.Count);
            }
            else if (File.Exists(effectiveInput) && fileExtensions.Exists(ext => effectiveInput.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                files.Add(effectiveInput);
            }
            else
            {
                _logger.LogError("Input must be a file or directory containing valid files: {Input}", effectiveInput);
                return null;
            }

            // If retryFailed is set, filter files to only those that failed in previous run
            if (retryFailed)
            {
                var failedListPath = Path.Combine(effectiveOutput, failedFilesListName);
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

            return files;
        }

        /// <summary>
        /// Initializes the processing queue with discovered files
        /// </summary>
        /// <param name="files">List of files to add to queue</param>
        protected virtual void InitializeProcessingQueue(List<string> files)
        {
            // Clear any existing queue
            lock (_queueLock)
            {
                _processingQueue.Clear();
            }

            // Build the processing queue
            foreach (var filePath in files)
            {
                // Determine document type based on extension
                string documentType = GetDocumentType(filePath);

                // Create queue item
                var queueItem = new QueueItem(filePath, documentType);

                // Add to processing queue
                lock (_queueLock)
                {
                    _processingQueue.Add(queueItem);
                }

                _logger.LogDebug("Added {DocumentType} file to queue: {FilePath}", documentType, filePath);
            }

            // Notify that the queue has been populated
            OnQueueChanged();
        }
    }
}
