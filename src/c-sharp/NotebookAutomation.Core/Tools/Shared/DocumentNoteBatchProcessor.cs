// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Shared;

/// <summary>
/// Generic batch processor for document note processors (PDF, video, etc.).
/// </summary>
/// <remarks>
/// <para>
/// This class provides functionality for batch processing of documents using any subclass of
/// <see cref="DocumentNoteProcessorBase"/>. It supports:
/// <list type="bullet">
/// <item><description>File discovery and filtering based on extensions</description></item>
/// <item><description>Queue management for processing tasks</description></item>
/// <item><description>Error handling and logging</description></item>
/// <item><description>Event-driven updates for processing progress and queue changes</description></item>
/// </list>
/// </para>
/// <para>
/// The batch processor integrates with the AI summarizer for generating summaries and provides
/// detailed diagnostic information during processing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var processor = new DocumentNoteBatchProcessor<PdfNoteProcessor>(logger, pdfProcessor, aiSummarizer);
/// processor.ProcessingProgressChanged += (sender, args) => Console.WriteLine(args.Progress);
/// processor.QueueChanged += (sender, args) => Console.WriteLine(args.Queue);
/// processor.StartProcessing("input", "output");
/// </code>
/// </example>
public partial class DocumentNoteBatchProcessor<TProcessor>
    where TProcessor : DocumentNoteProcessorBase
{
    private readonly ILogger<DocumentNoteBatchProcessor<TProcessor>> logger;
    private readonly TProcessor processor;
    private readonly IAISummarizer aiSummarizer;

    // Queue-related fields
    private readonly List<QueueItem> processingQueue = [];
    private readonly Lock queueLock = new();

    /// <summary>
    /// Matches markdown headers for notes (e.g., "## Notes").
    /// </summary>
    /// <remarks>
    /// <para>
    /// This regex is used to identify markdown headers specifically labeled as "Notes" in the content.
    /// It matches headers with two hashes followed by the word "Notes".
    /// </para>
    /// </remarks>    [GeneratedRegex(@"^##\s+Notes\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline, "en-US")]
    [GeneratedRegex(@"^##\s+Notes\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline, "en-US")]
    internal static partial Regex NotesHeaderRegex();

    /// <summary>
    /// Maps document types (e.g., PDF, VIDEO) to their associated file extensions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This dictionary is used to filter and identify files for processing based on their extensions.
    /// Supported document types and extensions include:
    /// <list type="bullet">
    /// <item><description>PDF: ".pdf"</description></item>
    /// <item><description>VIDEO: ".mp4", ".mov", ".avi", ".mkv", ".wmv"</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private readonly Dictionary<string, List<string>> documentTypeExtensions = new()
    {
        ["PDF"] = [".pdf"],
        ["VIDEO"] = [".mp4", ".mov", ".avi", ".mkv", ".wmv"],
    };

    /// <summary>
    /// Event triggered when processing progress changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised whenever the progress of the batch processing operation changes.
    /// Subscribers can use this event to update progress indicators or logs.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// processor.ProcessingProgressChanged += (sender, args) => Console.WriteLine(args.Progress);
    /// </code>
    /// </example>
    public event EventHandler<DocumentProcessingProgressEventArgs>? ProcessingProgressChanged;

    /// <summary>
    /// Event triggered when the processing queue changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised whenever the processing queue is updated (e.g., items added or removed).
    /// Subscribers can use this event to monitor the state of the queue.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// processor.QueueChanged += (sender, args) => Console.WriteLine(args.Queue);
    /// </code>
    /// </example>
    public event EventHandler<QueueChangedEventArgs>? QueueChanged;

    /// <summary>
    /// Gets a read-only view of the current processing queue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property provides a snapshot of the current processing queue, allowing external
    /// components to inspect the queue without modifying it.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var queue = processor.Queue;
    /// foreach (var item in queue)
    /// {
    ///     Console.WriteLine(item.FilePath);
    /// }
    /// </code>
    /// </example>
    public IReadOnlyList<QueueItem> Queue
    {
        get
        {
            lock (queueLock)
            {
                return processingQueue.ToList().AsReadOnly();
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
    /// <param name="changedItem">The specific item that changed, or null if the entire queue changed.</param>
    protected virtual void OnQueueChanged(QueueItem? changedItem = null)
    {
        lock (queueLock)
        {
            QueueChanged?.Invoke(this, new QueueChangedEventArgs(processingQueue.AsReadOnly(), changedItem));
        }
    }

    /// <summary>
    /// Determines the document type based on file extension.
    /// </summary>
    /// <param name="filePath">File path to check.</param>
    /// <returns>Document type (e.g., "PDF", "VIDEO").</returns>
    protected virtual string GetDocumentType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        foreach (var kvp in documentTypeExtensions)
        {
            if (kvp.Value.Contains(extension))
            {
                return kvp.Key;
            }
        }

        // Fallback: check if the processor is specialized
        if (typeof(TProcessor).Name.Contains("Pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "PDF";
        }

        if (typeof(TProcessor).Name.Contains("Video", StringComparison.OrdinalIgnoreCase))
        {
            return "VIDEO";
        }

        return "DOCUMENT"; // Generic fallback
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentNoteBatchProcessor{TProcessor}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="processor">The document note processor instance.</param>    /// <param name="aiSummarizer">The AI summarizer instance.</param>
    public DocumentNoteBatchProcessor(ILogger logger, TProcessor processor, IAISummarizer aiSummarizer)
    {
        if (logger is ILogger<DocumentNoteBatchProcessor<TProcessor>> genericLogger)
        {
            this.logger = genericLogger;
        }
        else
        {
            // Allow any ILogger for testing/mocking, but warn if not the expected type
            this.logger = logger as ILogger<DocumentNoteBatchProcessor<TProcessor>> ?? throw new ArgumentException("Logger must be ILogger<DocumentNoteBatchProcessor<TProcessor>> or compatible mock");
            if (logger.GetType().Name.Contains("Mock") || logger.GetType().Name.Contains("Proxy"))
            {
                // Allow for test mocks
            }
            else
            {
                throw new ArgumentException("Logger must be ILogger<DocumentNoteBatchProcessor<TProcessor>>");
            }
        }

        this.processor = processor;
        this.aiSummarizer = aiSummarizer ?? throw new ArgumentNullException(nameof(aiSummarizer));
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
        lock (queueLock)
        {
            processingQueue.Clear();
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
        }        // Process files with optional parallelization
        var (processedCount, failedCount, failedFiles) = await ProcessFilesAsync(
            files, effectiveOutput, effectiveResourcesRoot, forceOverwrite, dryRun,
            openAiApiKey, noSummary, timeoutSeconds, noShareLinks, noteType, appConfig).ConfigureAwait(false);

        processed = processedCount;
        failed = failedCount;
        failedFilesForRetry.AddRange(failedFiles);

        batchStopwatch.Stop();

        // Write failed files for retry if any
        if (failedFilesForRetry.Count > 0 && !dryRun)
        {
            var failedListPath = Path.Combine(effectiveOutput, failedFilesListName);
            File.WriteAllLines(failedListPath, failedFilesForRetry);
            logger.LogInformation($"Wrote failed file list to: {failedListPath}");
        }

        // Calculate total summary time and tokens from queue items
        lock (queueLock)
        {
            foreach (var item in processingQueue)
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
    /// Creates an error result for failed operations.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>BatchProcessResult indicating failure.</returns>
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
            AverageTokens = 0,
        };
    }

    /// <summary>
    /// Processes a single file with complete error handling and progress tracking.
    /// </summary>
    /// <param name="filePath">Path to the file to process.</param>
    /// <param name="queueItem">Queue item for progress tracking.</param>
    /// <param name="fileIndex">Current file index.</param>
    /// <param name="totalFiles">Total number of files.</param>
    /// <param name="effectiveOutput">Output directory.</param>
    /// <param name="resourcesRoot">Resources root directory.</param>
    /// <param name="forceOverwrite">Whether to overwrite existing files.</param>
    /// <param name="dryRun">Whether this is a dry run.</param>
    /// <param name="openAiApiKey">OpenAI API key.</param>
    /// <param name="noSummary">Whether to skip summary generation.</param>
    /// <param name="timeoutSeconds">API timeout in seconds.</param>
    /// <param name="noShareLinks">Whether to skip share link generation.</param>
    /// <param name="noteType">Type of note to generate.</param>
    /// <returns>Tuple indicating success and any error message.</returns>
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
            logger.LogInformation($"Processing file: {filePath}");
            string outputDir = effectiveOutput;
            Directory.CreateDirectory(outputDir);

            // Generate output path based on processor type and directory structure
            logger.LogInformation($"ABOUT TO CALL GenerateOutputPath with filePath={filePath}, outputDir={outputDir}, resourcesRoot={resourcesRoot ?? "null"}");
            string outputPath = GenerateOutputPath(filePath, outputDir, resourcesRoot);
            logger.LogInformation($"GenerateOutputPath RETURNED: {outputPath}");

            // If not forceOverwrite and file exists, skip
            if (!forceOverwrite && File.Exists(outputPath))
            {
                logger.LogWarning($"Output file exists and --force not set, skipping: {outputPath}");
                return (true, "Skipped - file exists");
            }

            // Extract content with progress tracking
            var (text, metadata) = await ExtractContentAsync(filePath, queueItem, fileIndex, totalFiles).ConfigureAwait(false);

            // Add resources root to metadata if specified
            if (!string.IsNullOrWhiteSpace(resourcesRoot))
            {
                metadata["resources_root"] = resourcesRoot;
            }

            // Generate AI summary with progress tracking
            var (summaryText, summaryTokens, summaryTime) = await GenerateAISummaryAsync(
                filePath, text, metadata, queueItem, fileIndex, totalFiles,
                openAiApiKey, noSummary, timeoutSeconds, resourcesRoot, noShareLinks).ConfigureAwait(false);

            // Generate and save markdown
            await GenerateAndSaveMarkdownAsync(
                filePath, summaryText, metadata, noteType, outputPath,
                queueItem, fileIndex, totalFiles, forceOverwrite, dryRun).ConfigureAwait(false);

            return (true, null);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to process: {ex.Message}";
            logger.LogError(ex, $"Failed to process file: {filePath}");

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
    /// Generates markdown content and saves it to file with progress tracking.
    /// </summary>
    /// <param name="filePath">Source file path.</param>
    /// <param name="summaryText">Generated summary text.</param>
    /// <param name="metadata">File metadata.</param>
    /// <param name="noteType">Type of note to generate.</param>
    /// <param name="outputPath">Output file path.</param>
    /// <param name="queueItem">Queue item for progress tracking.</param>
    /// <param name="fileIndex">Current file index.</param>
    /// <param name="totalFiles">Total number of files.</param>
    /// <param name="forceOverwrite">Whether to overwrite existing files.</param>
    /// <param name="dryRun">Whether this is a dry run.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
            markdown = processor.GenerateMarkdownNote(summaryText, metadata, noteType, includeNoteTypeTitle: false);
        }

        // Ensure markdown is initialized
        if (markdown == null)
        {
            logger.LogError($"Markdown generation failed for file: {filePath}");
            markdown = "Error generating markdown content";
        }

        // Check if existing file has readonly auto-generated-state
        bool isReadOnly = false;
        if (File.Exists(outputPath))
        {
            var yamlHelper = new YamlHelper(logger);
            isReadOnly = yamlHelper.IsFileReadOnly(outputPath);
            if (isReadOnly)
            {
                logger.LogInformation($"Skipping file modification due to readonly auto-generated-state: {outputPath}");
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

            await File.WriteAllTextAsync(outputPath, markdown).ConfigureAwait(false);

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

            logger.LogInformation($"Markdown note saved to: {outputPath}");
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

            logger.LogInformation($"[DRY RUN] Markdown note would be generated for: {filePath}");
        }
    }

    /// <summary>
    /// Compiles processing results and statistics into a BatchProcessResult.
    /// </summary>
    /// <param name="processed">Number of successfully processed files.</param>
    /// <param name="failed">Number of failed files.</param>
    /// <param name="batchTime">Total batch processing time.</param>
    /// <param name="totalSummaryTime">Total AI summary time.</param>
    /// <param name="totalTokens">Total tokens used.</param>
    /// <returns>Compiled batch processing result.</returns>
    protected virtual BatchProcessResult CompileProcessingResults(
        int processed,
        int failed,
        TimeSpan batchTime,
        TimeSpan totalSummaryTime,
        int totalTokens)
    {
        logger.LogInformation("Document processing completed. Success: {Processed}, Failed: {Failed}", processed, failed);
        logger.LogInformation("Total batch processing time: {ElapsedSeconds:F1}s", batchTime.TotalSeconds);
        logger.LogInformation("Total summary time: {ElapsedSeconds:F1}s", totalSummaryTime.TotalSeconds);
        logger.LogInformation("Total tokens for all summaries: {TotalTokens}", totalTokens);

        double avgFileTime = processed > 0 ? batchTime.TotalMilliseconds / processed : 0;
        double avgSummaryTime = processed > 0 ? totalSummaryTime.TotalMilliseconds / processed : 0;
        double avgTokens = processed > 0 ? (double)totalTokens / processed : 0;

        // Helper for formatting time
        string FormatTime(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
            {
                return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
            }

            if (ts.TotalMinutes >= 1)
            {
                return $"{ts.Minutes}m {ts.Seconds}s";
            }

            if (ts.TotalSeconds >= 1)
            {
                return $"{ts.Seconds}s {ts.Milliseconds}ms";
            }

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
                return $"{ms / 1000:F2}s";
            }

            return $"{ms:F0}ms";
        }

        // Count queue stats by type and status
        Dictionary<string, int> documentTypeStats = [];
        Dictionary<DocumentProcessingStatus, int> statusStats = [];

        lock (queueLock)
        {
            foreach (var item in processingQueue)
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
            AverageTokens = avgTokens,
        };
    }    /// <summary>
         /// Generates the output path for a processed file, handling video-specific naming and directory structure.
         /// </summary>
         /// <param name="inputFilePath">The input file path.</param>
         /// <param name="outputDir">The base output directory.</param>
         /// <param name="resourcesRoot">The resources root directory for calculating relative paths.</param>
         /// <returns>The output file path.</returns>
    protected virtual string GenerateOutputPath(string inputFilePath, string outputDir, string? resourcesRoot)
    {
        // Add detailed debug logging using LogInformation to ensure visibility
        logger.LogInformation("GenerateOutputPath called with:");
        logger.LogInformation($"  inputFilePath: {inputFilePath}");
        logger.LogInformation($"  outputDir: {outputDir}");
        logger.LogInformation($"  resourcesRoot: {resourcesRoot ?? "null"}");

        // Check processor type to determine file suffix
        bool isVideoProcessor = typeof(TProcessor).Name.Contains("Video");
        string fileSuffix = isVideoProcessor ? "-video.md" : ".md";
        string fileName = Path.GetFileNameWithoutExtension(inputFilePath) + fileSuffix;

        logger.LogInformation($"  isVideoProcessor: {isVideoProcessor}");
        logger.LogInformation($"  fileName: {fileName}");

        // If we have a resources root, preserve the directory structure for all processors
        if (!string.IsNullOrWhiteSpace(resourcesRoot) && Path.IsPathRooted(resourcesRoot))
        {
            logger.LogDebug("Resources root is valid, preserving directory structure");
            try
            {
                // Calculate relative path from resources root
                var inputFileInfo = new FileInfo(inputFilePath);
                var resourcesRootInfo = new DirectoryInfo(resourcesRoot);                // Get the relative path from resources root to the input file's directory
                string relativePath = Path.GetRelativePath(resourcesRootInfo.FullName, inputFileInfo.DirectoryName ?? string.Empty);

                // Log the original relative path for debugging
                logger.LogDebug("Original relative path from resources root: {RelativePath}", relativePath);

                // If the relative path contains ".." or is an absolute path, it means the file is outside the resources root
                // In that case, use flat structure (just the filename in output directory)
                if (relativePath.Contains("..") || Path.IsPathRooted(relativePath))
                {
                    logger.LogInformation("Input file is outside resources root, using flat structure");
                    return Path.Combine(outputDir, fileName);
                }

                // If the relative path starts with "." it means the file is directly in the resources root
                // In that case, use empty path to put files directly in output directory
                if (relativePath == "." || relativePath == ".\\")
                {
                    relativePath = string.Empty;
                }

                // Create the output directory structure
                string targetDir = string.IsNullOrEmpty(relativePath) ? outputDir : Path.Combine(outputDir, relativePath);
                Directory.CreateDirectory(targetDir);

                var outputPath = Path.Combine(targetDir, fileName);
                logger.LogDebug("Generated output path: {OutputPath}", outputPath);

                return outputPath;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to calculate relative path from resources root {ResourcesRoot} to {InputFile}, using flat structure",
                    resourcesRoot, inputFilePath);

                // Fall back to flat structure
                return Path.Combine(outputDir, fileName);
            }
        }
        else
        {
            // No resources root specified, use flat structure
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
            var notesRegex = NotesHeaderRegex();

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
            logger.LogError(ex, "Failed to preserve user content from existing file: {FilePath}", existingFilePath);
            return newMarkdown;
        }
    }

    /// <summary>
    /// Validates input parameters and sets up processing configuration.
    /// </summary>
    /// <param name="input">Input path.</param>
    /// <param name="output">Output path.</param>
    /// <param name="appConfig">Application configuration.</param>
    /// <returns>Tuple containing effective input and output paths, or null if validation failed.</returns>
    protected virtual (string effectiveInput, string effectiveOutput)? ValidateAndSetupProcessing(
        string input,
        string? output,
        AppConfig? appConfig)
    {
        string effectiveInput = input;
        string effectiveOutput = output ?? appConfig?.Paths?.NotebookVaultFullpathRoot ?? "Generated";

        if (string.IsNullOrWhiteSpace(effectiveInput))
        {
            logger.LogError("Input path is required. Config: {Config}", appConfig?.Paths?.NotebookVaultFullpathRoot);
            return null;
        }

        return (effectiveInput, effectiveOutput);
    }

    /// <summary>
    /// Extracts content and metadata from a file with progress tracking.
    /// </summary>
    /// <param name="filePath">Path to the file to extract from.</param>
    /// <param name="queueItem">Queue item to update with progress.</param>
    /// <param name="fileIndex">Current file index.</param>
    /// <param name="totalFiles">Total number of files.</param>
    /// <returns>Tuple containing extracted text and metadata.</returns>
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

        var (text, metadata) = await processor.ExtractTextAndMetadataAsync(filePath).ConfigureAwait(false);

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
    /// Generates AI summary using processor-specific methods.
    /// </summary>
    /// <param name="filePath">Path to the file being processed.</param>
    /// <param name="text">Extracted text content.</param>
    /// <param name="metadata">File metadata.</param>
    /// <param name="queueItem">Queue item to update with progress.</param>
    /// <param name="fileIndex">Current file index.</param>
    /// <param name="totalFiles">Total number of files.</param>
    /// <param name="openAiApiKey">OpenAI API key.</param>
    /// <param name="noSummary">Whether to skip summary generation.</param>
    /// <param name="timeoutSeconds">API timeout in seconds.</param>
    /// <param name="resourcesRoot">Resources root directory.</param>
    /// <param name="noShareLinks">Whether to skip share link generation.</param>
    /// <returns>Tuple containing summary text, token count, and summary time.</returns>
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
            var generateMethod = processor.GetType().GetMethod("GenerateVideoNoteAsync");
            if (generateMethod != null)
            {
                logger.LogDebug("Using specialized GenerateVideoNoteAsync method for video processing");

                // Pass the noShareLinks parameter to the GenerateVideoNoteAsync method
                var task = (Task<string>)generateMethod.Invoke(
                    processor,
                    [
                    filePath,
                    openAiApiKey,
                    null, // promptFileName
                    noSummary,
                    timeoutSeconds,
                    resourcesRoot,
                    noShareLinks
                ])!;

                summaryText = await task.ConfigureAwait(false); // For video, this contains the full markdown

                // Estimate tokens
                var estimateTokenMethod = aiSummarizer.GetType().GetMethod("EstimateTokenCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (estimateTokenMethod != null && !noSummary)
                {
                    // Extract summary part from the markdown for token estimation
                    var testSummary = summaryText.Length > 300 ? summaryText[..300] : summaryText;
                    var tokenResult = estimateTokenMethod.Invoke(aiSummarizer, [testSummary]);
                    if (tokenResult != null)
                    {
                        summaryTokens = (int)tokenResult;
                    }
                }
            }
            else
            {
                logger.LogWarning("VideoNoteProcessor found but GenerateVideoNoteAsync method not available. Using base method.");
                summaryText = await processor.GenerateAiSummaryAsync(text).ConfigureAwait(false);
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
                var generateMethod = processor.GetType().GetMethod("GeneratePdfSummaryAsync");
                if (generateMethod != null)
                {
                    logger.LogDebug("Using specialized GeneratePdfSummaryAsync method for PDF processing");

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
                    var task = (Task<string>)generateMethod.Invoke(processor, [text, metadata, null])!;
                    summaryText = await task.ConfigureAwait(false);

                    // Update queue status
                    if (queueItem != null)
                    {
                        queueItem.StatusMessage = $"AI summary generated for PDF ({fileIndex}/{totalFiles}): {Path.GetFileName(filePath)}";
                        OnQueueChanged(queueItem);
                    }
                }
                else
                {
                    logger.LogWarning("PdfNoteProcessor found but GeneratePdfSummaryAsync method not available. Using base method.");
                    summaryText = await processor.GenerateAiSummaryAsync(text).ConfigureAwait(false);
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

                summaryText = await processor.GenerateAiSummaryAsync(text).ConfigureAwait(false);
            }

            // Estimate tokens
            var estimateTokenMethod = aiSummarizer.GetType().GetMethod("EstimateTokenCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (estimateTokenMethod != null)
            {
                var tokenResult = estimateTokenMethod.Invoke(aiSummarizer, [summaryText]);
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
    /// Discovers files to process based on input path and file extensions.
    /// </summary>
    /// <param name="effectiveInput">Effective input path.</param>
    /// <param name="fileExtensions">List of file extensions to process.</param>
    /// <param name="retryFailed">Whether to retry only failed files.</param>
    /// <param name="effectiveOutput">Output directory for failed files list.</param>
    /// <param name="failedFilesListName">Name of failed files list.</param>
    /// <returns>List of files to process, or null if discovery failed.</returns>
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

            logger.LogInformation($"Found {files.Count} files in directory: {effectiveInput}");
        }
        else if (File.Exists(effectiveInput) && fileExtensions.Exists(ext => effectiveInput.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            files.Add(effectiveInput);
        }
        else
        {
            logger.LogError("Input must be a file or directory containing valid files: {Input}", effectiveInput);
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
                logger.LogInformation("Retrying {Count} previously failed files.", files.Count);
            }
            else
            {
                logger.LogWarning("No {FileName} found for retry; processing all files.", failedFilesListName);
            }
        }

        return files;
    }

    /// <summary>
    /// Initializes the processing queue with discovered files.
    /// </summary>
    /// <param name="files">List of files to add to queue.</param>
    protected virtual void InitializeProcessingQueue(List<string> files)
    {
        // Clear any existing queue
        lock (queueLock)
        {
            processingQueue.Clear();
        }

        // Build the processing queue
        foreach (var filePath in files)
        {
            // Determine document type based on extension
            string documentType = GetDocumentType(filePath);

            // Create queue item
            var queueItem = new QueueItem(filePath, documentType);

            // Add to processing queue
            lock (queueLock)
            {
                processingQueue.Add(queueItem);
            }

            logger.LogDebug("Added {DocumentType} file to queue: {FilePath}", documentType, filePath);
        }

        // Notify that the queue has been populated
        OnQueueChanged();
    }

    /// <summary>
    /// Processes files with optional parallelization based on configuration.
    /// </summary>
    /// <param name="files">List of files to process.</param>
    /// <param name="effectiveOutput">Output directory path.</param>
    /// <param name="effectiveResourcesRoot">Resources root path.</param>
    /// <param name="forceOverwrite">Whether to overwrite existing files.</param>
    /// <param name="dryRun">Whether to perform a dry run.</param>
    /// <param name="openAiApiKey">OpenAI API key.</param>
    /// <param name="noSummary">Whether to skip AI summary generation.</param>
    /// <param name="timeoutSeconds">Timeout in seconds.</param>
    /// <param name="noShareLinks">Whether to skip share link creation.</param>
    /// <param name="noteType">Type of note being generated.</param>
    /// <param name="appConfig">Application configuration containing parallel processing settings.</param>
    /// <returns>Tuple containing processed count, failed count, and list of failed files.</returns>
    protected virtual async Task<(int processed, int failed, List<string> failedFiles)> ProcessFilesAsync(
        List<string> files,
        string effectiveOutput,
        string? effectiveResourcesRoot,
        bool forceOverwrite,
        bool dryRun,
        string? openAiApiKey,
        bool noSummary,
        int? timeoutSeconds,
        bool noShareLinks,
        string noteType,
        AppConfig? appConfig)
    {
        var failedFiles = new List<string>();
        int processed = 0;
        int failed = 0;        // Get parallelization settings from configuration
        var maxFileParallelism = appConfig?.AiService?.Timeout?.MaxFileParallelism ?? 1;
        var fileRateLimitMs = appConfig?.AiService?.Timeout?.FileRateLimitMs ?? 200;

        // Decide whether to use parallel or sequential processing
        if (maxFileParallelism > 1 && files.Count > 1)
        {
            logger.LogInformation("Processing {FileCount} files with parallelism of {MaxParallelism} and rate limit of {RateLimit}ms",
                files.Count, maxFileParallelism, fileRateLimitMs);

            var (parallelProcessed, parallelFailed, parallelFailedFiles) = await ProcessFilesInParallelAsync(
                files, effectiveOutput, effectiveResourcesRoot, forceOverwrite, dryRun,
                openAiApiKey, noSummary, timeoutSeconds, noShareLinks, noteType,
                maxFileParallelism, fileRateLimitMs).ConfigureAwait(false);

            processed = parallelProcessed;
            failed = parallelFailed;
            failedFiles.AddRange(parallelFailedFiles);
        }
        else
        {
            logger.LogInformation("Processing {FileCount} files sequentially", files.Count);

            var (sequentialProcessed, sequentialFailed, sequentialFailedFiles) = await ProcessFilesSequentiallyAsync(
                files, effectiveOutput, effectiveResourcesRoot, forceOverwrite, dryRun,
                openAiApiKey, noSummary, timeoutSeconds, noShareLinks, noteType).ConfigureAwait(false);

            processed = sequentialProcessed;
            failed = sequentialFailed;
            failedFiles.AddRange(sequentialFailedFiles);
        }

        return (processed, failed, failedFiles);
    }


    /// <summary>
    /// Processes files sequentially (the original implementation).
    /// </summary>
    protected virtual async Task<(int processed, int failed, List<string> failedFiles)> ProcessFilesSequentiallyAsync(
        List<string> files,
        string effectiveOutput,
        string? effectiveResourcesRoot,
        bool forceOverwrite,
        bool dryRun,
        string? openAiApiKey,
        bool noSummary,
        int? timeoutSeconds,
        bool noShareLinks,
        string noteType)
    {
        var failedFiles = new List<string>();
        int processed = 0;
        int failed = 0;

        // Process each file sequentially
        for (int fileIndex = 0; fileIndex < files.Count; fileIndex++)
        {
            var filePath = files[fileIndex];
            var fileStopwatch = Stopwatch.StartNew();

            // Get the queue item for this file
            QueueItem? queueItem = null;
            lock (queueLock)
            {
                queueItem = processingQueue.FirstOrDefault(q => q.FilePath == filePath);
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
                files.Count);

            // Process the single file
            var (success, errorMessage) = await ProcessSingleFileAsync(
                filePath, queueItem, fileIndex + 1, files.Count, effectiveOutput, effectiveResourcesRoot,
                forceOverwrite, dryRun, openAiApiKey, noSummary, timeoutSeconds, noShareLinks, noteType).ConfigureAwait(false);

            if (success)
            {
                // Check if the file was skipped due to existing file and forceOverwrite: false
                if (errorMessage?.Contains("Skipped") == true)
                {
                    // Don't count skipped files as processed
                    logger.LogDebug($"File skipped: {filePath}");
                }
                else
                {
                    processed++;
                }
            }
            else
            {
                failed++;
                failedFiles.Add(filePath);
            }
            fileStopwatch.Stop();
            logger.LogInformation($"Processing file: {filePath} took {fileStopwatch.Elapsed.TotalSeconds:F1}s");

            // Report progress after each file
            OnProcessingProgressChanged(filePath, "Processed", processed + failed, files.Count);
        }

        return (processed, failed, failedFiles);
    }


    /// <summary>
    /// Processes files in parallel with concurrency control and rate limiting.
    /// </summary>
    protected virtual async Task<(int processed, int failed, List<string> failedFiles)> ProcessFilesInParallelAsync(
        List<string> files,
        string effectiveOutput,
        string? effectiveResourcesRoot,
        bool forceOverwrite,
        bool dryRun,
        string? openAiApiKey,
        bool noSummary,
        int? timeoutSeconds,
        bool noShareLinks,
        string noteType,
        int maxParallelism,
        int rateLimitMs)
    {
        var failedFiles = new ConcurrentBag<string>();
        var processedCount = 0;
        var failedCount = 0;

        var maxConcurrency = Math.Min(maxParallelism, files.Count);
        var rateLimitDelay = TimeSpan.FromMilliseconds(rateLimitMs);

        // Create a semaphore to control concurrency
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        // Create tasks for all files, maintaining order for progress reporting
        var fileTasks = files.Select(async (filePath, fileIndex) =>
        {
            // Rate limiting - stagger the start of requests
            if (rateLimitDelay > TimeSpan.Zero)
            {
                await Task.Delay(rateLimitDelay * fileIndex).ConfigureAwait(false);
            }

            // Wait for available slot
            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                return await ProcessSingleFileInParallelAsync(
                    filePath,
                    fileIndex,
                    files.Count,
                    effectiveOutput,
                    effectiveResourcesRoot,
                    forceOverwrite,
                    dryRun,
                    openAiApiKey,
                    noSummary,
                    timeoutSeconds,
                    noShareLinks,
                    noteType).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        });

        // Wait for all files to complete
        var results = await Task.WhenAll(fileTasks).ConfigureAwait(false);

        // Process results
        foreach (var (success, errorMessage, filePath) in results)
        {
            if (success)
            {
                // Check if the file was skipped
                if (errorMessage?.Contains("Skipped") == true)
                {
                    logger.LogDebug($"File skipped: {filePath}");
                }
                else
                {
                    Interlocked.Increment(ref processedCount);
                }
            }
            else
            {
                Interlocked.Increment(ref failedCount);
                failedFiles.Add(filePath);
            }
        }

        logger.LogInformation("Completed parallel processing: {ProcessedCount} processed, {FailedCount} failed",
            processedCount, failedCount);

        return (processedCount, failedCount, failedFiles.ToList());
    }


    /// <summary>
    /// Processes a single file in parallel context with thread-safe queue operations.
    /// </summary>
    protected virtual async Task<(bool success, string? errorMessage, string filePath)> ProcessSingleFileInParallelAsync(
        string filePath,
        int fileIndex,
        int totalFiles,
        string effectiveOutput,
        string? effectiveResourcesRoot,
        bool forceOverwrite,
        bool dryRun,
        string? openAiApiKey,
        bool noSummary,
        int? timeoutSeconds,
        bool noShareLinks,
        string noteType)
    {
        var fileStopwatch = Stopwatch.StartNew();

        // Get the queue item for this file (thread-safe)
        QueueItem? queueItem = null;
        lock (queueLock)
        {
            queueItem = processingQueue.FirstOrDefault(q => q.FilePath == filePath);
            if (queueItem != null)
            {
                // Update queue item status
                queueItem.Status = DocumentProcessingStatus.Processing;
                queueItem.Stage = ProcessingStage.NotStarted;
                queueItem.StatusMessage = $"Processing {queueItem.DocumentType} file {fileIndex + 1}/{totalFiles}: {Path.GetFileName(filePath)}";
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
            queueItem?.StatusMessage ?? $"Processing file {fileIndex + 1}/{totalFiles}: {Path.GetFileName(filePath)}",
            fileIndex + 1,
            totalFiles);

        try
        {
            // Process the single file
            var (success, errorMessage) = await ProcessSingleFileAsync(
                filePath, queueItem, fileIndex + 1, totalFiles, effectiveOutput, effectiveResourcesRoot,
                forceOverwrite, dryRun, openAiApiKey, noSummary, timeoutSeconds, noShareLinks, noteType).ConfigureAwait(false); fileStopwatch.Stop();
            logger.LogInformation($"Processing file: {filePath} took {fileStopwatch.Elapsed.TotalSeconds:F1}s");

            return (success, errorMessage, filePath);
        }
        catch (Exception ex)
        {
            fileStopwatch.Stop();
            var errorMessage = $"Failed to process: {ex.Message}";
            logger.LogError(ex, $"Failed to process file: {filePath}");

            // Update queue item status for failure (thread-safe)
            if (queueItem != null)
            {
                lock (queueLock)
                {
                    queueItem.Status = DocumentProcessingStatus.Failed;
                    queueItem.StatusMessage = errorMessage;
                    queueItem.ProcessingEndTime = DateTime.Now;
                }
                OnQueueChanged(queueItem);
            }

            return (false, errorMessage, filePath);
        }
    }
}
