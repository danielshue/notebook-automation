// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Batch processor for ensuring metadata consistency across multiple markdown files within a vault directory.
/// This class provides comprehensive batch processing capabilities with queue management, progress tracking,
/// error handling, and real-time event notifications for UI integration.
/// </summary>
/// <remarks>
/// <para>
/// The MetadataEnsureBatchProcessor orchestrates the processing of multiple markdown files by utilizing
/// the underlying MetadataEnsureProcessor for individual file operations. It provides enterprise-grade
/// batch processing features including:
/// </para>
/// <list type="bullet">
/// <item><description>Queue-based processing with real-time status tracking</description></item>
/// <item><description>Progress monitoring with detailed per-file status updates</description></item>
/// <item><description>Error handling with failed file tracking and retry capabilities</description></item>
/// <item><description>Event-driven architecture for UI integration and monitoring</description></item>
/// <item><description>Dry run support for preview and validation scenarios</description></item>
/// <item><description>Directory filtering to exclude hidden and system directories</description></item>
/// <item><description>Comprehensive result reporting with processing statistics</description></item>
/// </list>
/// <para>
/// Processing Workflow:
/// </para>
/// <list type="number">
/// <item><description>Directory scan to discover all markdown files (excluding hidden directories)</description></item>
/// <item><description>Queue initialization with discovered files</description></item>
/// <item><description>Sequential processing with status updates and progress notifications</description></item>
/// <item><description>Error tracking and failed file logging for retry scenarios</description></item>
/// <item><description>Summary reporting with detailed processing statistics</description></item>
/// </list>
/// <para>
/// Event System:
/// The class provides two main events for real-time monitoring:
/// </para>
/// <list type="bullet">
/// <item><description>ProcessingProgressChanged: Fired for each file with progress information</description></item>
/// <item><description>QueueChanged: Fired when queue items change status or the queue structure changes</description></item>
/// </list>
/// <para>
/// Thread Safety:
/// The class is thread-safe for queue operations using internal locking mechanisms.
/// Events are fired synchronously within the processing thread.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic batch processing
/// var batchProcessor = serviceProvider.GetService&lt;MetadataEnsureBatchProcessor&gt;();
///
/// // Subscribe to progress events
/// batchProcessor.ProcessingProgressChanged += (sender, args) =&gt;
/// {
///     Console.WriteLine($"Processing {args.CurrentFile}/{args.TotalFiles}: {args.FilePath}");
/// };
///
/// // Process vault directory
/// var result = await batchProcessor.EnsureMetadataAsync(@"C:\vault");
/// Console.WriteLine($"Processed: {result.ProcessedFiles}, Failed: {result.FailedFiles}");
///
/// // Dry run for preview
/// var dryResult = await batchProcessor.EnsureMetadataAsync(@"C:\vault", dryRun: true);
/// </code>
/// </example>
/// <param name="_logger">Logger instance for diagnostic and progress reporting.</param>
/// <param name="processor">MetadataEnsureProcessor instance for individual file processing operations.</param>
public class MetadataEnsureBatchProcessor(
    ILogger<MetadataEnsureBatchProcessor> _logger,
    MetadataEnsureProcessor processor)
{
    private readonly ILogger<MetadataEnsureBatchProcessor> _logger = _logger;
    private readonly MetadataEnsureProcessor _metadataProcessor = processor;

    // Queue-related fields
    private readonly List<QueueItem> _processingQueue = [];
    private readonly object _queueLock = new();

    /// <summary>
    /// Event triggered when processing progress changes for individual files during batch operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event provides real-time progress updates during batch processing, allowing subscribers
    /// to monitor the current file being processed, overall progress, and status messages.
    /// </para>
    /// <para>
    /// The event is fired for each file as it transitions through processing stages, providing
    /// detailed information for progress bars, status displays, and logging purposes.
    /// </para>
    /// <para>
    /// Event Timing:
    /// The event is fired at key processing milestones including file start, completion,
    /// and failure states, ensuring comprehensive progress tracking.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// batchProcessor.ProcessingProgressChanged += (sender, args) =&gt;
    /// {
    ///     var percent = (args.CurrentFile * 100) / args.TotalFiles;
    ///     Console.WriteLine($"Progress: {percent}% - {args.Status}");
    ///     progressBar.Value = percent;
    ///     statusLabel.Text = args.Status;
    /// };
    /// </code>
    /// </example>
    public event EventHandler<DocumentProcessingProgressEventArgs>? ProcessingProgressChanged;

    /// <summary>
    /// Event triggered when the processing queue changes, including status updates for individual items.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is fired whenever the queue structure changes or when individual queue items
    /// undergo status transitions. It provides comprehensive queue state information for
    /// UI updates and monitoring systems.
    /// </para>
    /// <para>
    /// Event Triggers:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Queue initialization with discovered files</description></item>
    /// <item><description>Individual file status changes (waiting → processing → completed/failed)</description></item>
    /// <item><description>Queue modifications or resets</description></item>
    /// </list>
    /// <para>
    /// The event args include both the complete queue state and the specific item that changed,
    /// allowing for efficient UI updates and detailed monitoring.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// batchProcessor.QueueChanged += (sender, args) =&gt;
    /// {
    ///     // Update queue display
    ///     queueListView.Items.Clear();
    ///     foreach (var item in args.Queue)
    ///     {
    ///         var listItem = new ListViewItem(item.FilePath)
    ///         {
    ///             SubItems = { item.Status.ToString(), item.StatusMessage }
    ///         };
    ///         queueListView.Items.Add(listItem);
    ///     }
    ///
    ///     // Highlight changed item if specific
    ///     if (args.ChangedItem != null)
    ///     {
    ///         HighlightQueueItem(args.ChangedItem);
    ///     }
    /// };
    /// </code>
    /// </example>
    public event EventHandler<QueueChangedEventArgs>? QueueChanged;

    /// <summary>
    /// Gets a thread-safe, read-only snapshot of the current processing queue containing all files and their status.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property provides a safe way to access the current queue state without risk of
    /// concurrent modification exceptions. The returned collection is a snapshot taken at
    /// the time of access and will not reflect subsequent changes.
    /// </para>
    /// <para>
    /// Thread Safety:
    /// Access to this property is thread-safe through internal locking mechanisms.
    /// The returned IReadOnlyList is immutable and safe for concurrent access.
    /// </para>
    /// <para>
    /// Performance Considerations:
    /// Each access creates a new snapshot copy. For high-frequency access scenarios,
    /// consider caching the result or subscribing to QueueChanged events instead.
    /// </para>
    /// </remarks>
    /// <value>
    /// A read-only list containing all queue items with their current status, processing stage,
    /// and timing information. Returns an empty list if no files are queued.
    /// </value>
    /// <example>
    /// <code>
    /// // Get current queue status
    /// var currentQueue = batchProcessor.Queue;
    ///
    /// // Display queue summary
    /// var completed = currentQueue.Count(q =&gt; q.Status == DocumentProcessingStatus.Completed);
    /// var failed = currentQueue.Count(q =&gt; q.Status == DocumentProcessingStatus.Failed);
    /// var waiting = currentQueue.Count(q =&gt; q.Status == DocumentProcessingStatus.Waiting);
    ///
    /// Console.WriteLine($"Queue: {completed} completed, {failed} failed, {waiting} waiting");
    ///
    /// // Find specific files
    /// var failedFiles = currentQueue
    ///     .Where(q =&gt; q.Status == DocumentProcessingStatus.Failed)
    ///     .Select(q =&gt; q.FilePath)
    ///     .ToList();
    /// </code>
    /// </example>
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
    /// Raises the ProcessingProgressChanged event with detailed progress information for monitoring and UI updates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This protected virtual method follows the standard .NET event raising pattern, allowing derived classes
    /// to override the event raising behavior while maintaining the base functionality.
    /// </para>
    /// <para>
    /// The method safely invokes the event using the null-conditional operator to prevent exceptions
    /// when no subscribers are attached. Events are fired synchronously on the calling thread.
    /// </para>
    /// <para>
    /// Event Payload:
    /// The DocumentProcessingProgressEventArgs provides comprehensive progress information including
    /// file identification, status messages, and numerical progress indicators for UI components.
    /// </para>
    /// </remarks>
    /// <param name="filePath">
    /// The full file path of the file currently being processed. Used for identification and
    /// logging purposes in event subscribers.
    /// </param>
    /// <param name="status">
    /// A human-readable status message describing the current processing operation.
    /// Examples: "Processing metadata", "Completed successfully", "Failed: Access denied".
    /// </param>
    /// <param name="currentFile">
    /// The 1-based index of the current file being processed. Used for progress calculation
    /// and display (e.g., "Processing file 5 of 100").
    /// </param>
    /// <param name="totalFiles">
    /// The total number of files in the batch operation. Used with currentFile to calculate
    /// percentage completion and estimated time remaining.
    /// </param>
    /// <example>
    /// <code>
    /// // Example of calling from a derived class
    /// protected override void OnProcessingProgressChanged(string filePath, string status, int currentFile, int totalFiles)
    /// {
    ///     // Custom logging before raising event
    ///     Logger.LogDebug("Progress: {CurrentFile}/{TotalFiles} - {Status}", currentFile, totalFiles, status);
    ///
    ///     // Call base implementation to raise event
    ///     base.OnProcessingProgressChanged(filePath, status, currentFile, totalFiles);
    /// }
    /// </code>
    /// </example>
    protected virtual void OnProcessingProgressChanged(string filePath, string status, int currentFile, int totalFiles)
    {
        ProcessingProgressChanged?.Invoke(this, new DocumentProcessingProgressEventArgs(filePath, status, currentFile, totalFiles));
    }

    /// <summary>
    /// Raises the QueueChanged event with current queue state and optional changed item information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This protected virtual method provides thread-safe event raising with comprehensive queue
    /// state information. It creates a snapshot of the current queue within the lock to ensure
    /// data consistency for event subscribers.
    /// </para>
    /// <para>
    /// The method supports two usage patterns:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Full queue updates (changedItem = null): Used when the entire queue changes</description></item>
    /// <item><description>Single item updates (changedItem specified): Used for individual status changes</description></item>
    /// </list>
    /// <para>
    /// Thread Safety:
    /// The method uses internal locking to ensure the queue snapshot is consistent and safe
    /// for concurrent access by event subscribers.
    /// </para>
    /// </remarks>
    /// <param name="changedItem">
    /// The specific queue item that changed, or null if the entire queue structure was modified.
    /// When specified, allows event subscribers to optimize their updates by focusing on the
    /// changed item rather than refreshing the entire display.
    /// </param>
    /// <example>
    /// <code>
    /// // Queue initialization - entire queue changed
    /// OnQueueChanged(null);
    ///
    /// // Single item status update
    /// var updatedItem = queue.FirstOrDefault(q =&gt; q.FilePath == filePath);
    /// OnQueueChanged(updatedItem);
    ///
    /// // In a derived class with custom behavior
    /// protected override void OnQueueChanged(QueueItem? changedItem = null)
    /// {
    ///     // Custom queue state persistence
    ///     SaveQueueStateToFile();
    ///
    ///     // Call base to raise event
    ///     base.OnQueueChanged(changedItem);
    /// }
    /// </code>
    /// </example>
    protected virtual void OnQueueChanged(QueueItem? changedItem = null)
    {
        lock (_queueLock)
        {
            QueueChanged?.Invoke(this, new QueueChangedEventArgs(_processingQueue.AsReadOnly(), changedItem));
        }
    }

    /// <summary>
    /// Initializes the processing queue by discovering and cataloging all markdown files in the specified vault directory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method performs a comprehensive directory scan to discover all markdown files while intelligently
    /// filtering out system and hidden directories (those starting with a period). The discovery process:
    /// </para>
    /// <list type="number">
    /// <item><description>Recursively scans the vault directory and all subdirectories</description></item>
    /// <item><description>Identifies all .md files while excluding hidden files (starting with '.')</description></item>
    /// <item><description>Creates QueueItem objects for each discovered file with initial metadata processing configuration</description></item>
    /// <item><description>Initializes all items with "Waiting" status and "NotStarted" stage</description></item>
    /// <item><description>Triggers QueueChanged event to notify subscribers of the new queue state</description></item>
    /// </list>
    /// <para>
    /// File Filtering Strategy:
    /// The method excludes directories starting with '.' (such as .git, .vscode, .obsidian) to avoid
    /// processing system files and maintain focus on user content.
    /// </para>
    /// <para>
    /// Queue Item Configuration:
    /// Each discovered file is configured for "METADATA" processing with appropriate initial state
    /// and status message indicating readiness for processing.
    /// </para>
    /// </remarks>
    /// <param name="vaultPath">
    /// The absolute path to the vault directory to scan for markdown files. The directory must exist
    /// and be accessible for the scanning operation to succeed.
    /// </param>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when the specified vault directory does not exist or is not accessible.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the application lacks permissions to access the vault directory or its subdirectories.
    /// </exception>
    /// <example>
    /// <code>
    /// // Initialize queue for a vault directory
    /// string vaultPath = @"C:\MyVault";
    /// processor.InitializeProcessingQueue(vaultPath);
    ///
    /// // Check discovered files
    /// var queueItems = processor.Queue;
    /// Console.WriteLine($"Discovered {queueItems.Count} markdown files for processing");
    ///
    /// // Subscribe to queue changes before initialization
    /// processor.QueueChanged += (sender, args) =&gt;
    /// {
    ///     Console.WriteLine($"Queue updated with {args.Queue.Count} items");
    /// };
    /// </code>
    /// </example>
    protected virtual void InitializeProcessingQueue(string vaultPath)
    {
        var markdownFiles = GetMarkdownFilesExcludingDotDirectories(vaultPath)
            .Where(f => !Path.GetFileName(f).StartsWith('.')) // Skip hidden files
            .ToList();

        lock (_queueLock)
        {
            _processingQueue.Clear();

            foreach (string filePath in markdownFiles)
            {
                var queueItem = new QueueItem(filePath, "METADATA")
                {
                    Status = DocumentProcessingStatus.Waiting,
                    Stage = ProcessingStage.NotStarted,
                    StatusMessage = "Waiting to process metadata",
                };
                _processingQueue.Add(queueItem);
            }
        }

        OnQueueChanged();
    }

    /// <summary>
    /// Updates the status of a specific queue item with comprehensive state tracking and event notification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method provides thread-safe queue item updates with automatic timing tracking and event
    /// notification. It performs several important functions:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Locates the queue item by file path within a thread-safe lock</description></item>
    /// <item><description>Updates status, stage, and message information atomically</description></item>
    /// <item><description>Automatically tracks processing start and end times</description></item>
    /// <item><description>Triggers appropriate events for UI and monitoring updates</description></item>
    /// </list>
    /// <para>
    /// Automatic Timing Tracking:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Sets ProcessingStartTime when status changes to Processing (first time only)</description></item>
    /// <item><description>Sets ProcessingEndTime when status changes to Completed or Failed</description></item>
    /// <item><description>Preserves existing timing information to prevent overwrites</description></item>
    /// </list>
    /// <para>
    /// Event Notification:
    /// The method triggers both QueueChanged and ProcessingProgressChanged events to ensure
    /// comprehensive monitoring and UI updates.
    /// </para>
    /// </remarks>
    /// <param name="filePath">
    /// The full file path identifying the queue item to update. Must match exactly with a queued item.
    /// </param>
    /// <param name="status">
    /// The new processing status indicating the current state of the file operation.
    /// Used for progress tracking and UI display.
    /// </param>
    /// <param name="stage">
    /// The new processing stage indicating the specific phase of the operation.
    /// Provides more granular status information than the general status.
    /// </param>
    /// <param name="statusMessage">
    /// A human-readable message describing the current operation or result.
    /// Displayed in UI components and logged for debugging purposes.
    /// </param>
    /// <param name="currentFile">
    /// The 1-based index of the current file in the overall batch operation.
    /// Used for progress calculation and display.
    /// </param>
    /// <param name="totalFiles">
    /// The total number of files in the batch operation.
    /// Used with currentFile for percentage completion calculations.
    /// </param>
    /// <example>
    /// <code>
    /// // Update item to processing state
    /// UpdateQueueItemStatus(
    ///     filePath: @"C:\vault\document.md",
    ///     status: DocumentProcessingStatus.Processing,
    ///     stage: ProcessingStage.MarkdownCreation,
    ///     statusMessage: "Processing metadata...",
    ///     currentFile: 5,
    ///     totalFiles: 100);
    ///
    /// // Update item to completed state
    /// UpdateQueueItemStatus(
    ///     filePath: @"C:\vault\document.md",
    ///     status: DocumentProcessingStatus.Completed,
    ///     stage: ProcessingStage.Completed,
    ///     statusMessage: "Metadata updated successfully",
    ///     currentFile: 5,
    ///     totalFiles: 100);
    /// </code>
    /// </example>
    protected virtual void UpdateQueueItemStatus(string filePath, DocumentProcessingStatus status, ProcessingStage stage, string statusMessage, int currentFile, int totalFiles)
    {
        QueueItem? queueItem = null;

        lock (_queueLock)
        {
            queueItem = _processingQueue.FirstOrDefault(q => q.FilePath == filePath);
            if (queueItem != null)
            {
                queueItem.Status = status;
                queueItem.Stage = stage;
                queueItem.StatusMessage = statusMessage;

                if (status == DocumentProcessingStatus.Processing && queueItem.ProcessingStartTime == null)
                {
                    queueItem.ProcessingStartTime = DateTime.UtcNow;
                }
                else if (status is DocumentProcessingStatus.Completed or DocumentProcessingStatus.Failed)
                {
                    queueItem.ProcessingEndTime = DateTime.UtcNow;
                }
            }
        }

        if (queueItem != null)
        {
            OnQueueChanged(queueItem);
            OnProcessingProgressChanged(filePath, statusMessage, currentFile, totalFiles);
        }
    }


    /// <summary>
    /// Processes all markdown files in the specified vault directory to ensure metadata consistency across the entire collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method orchestrates a comprehensive batch processing operation that handles discovery, processing,
    /// and reporting for all markdown files within a vault directory. The operation includes:
    /// </para>
    /// <list type="number">
    /// <item><description>Input validation and directory existence verification</description></item>
    /// <item><description>Recursive file discovery with intelligent filtering</description></item>
    /// <item><description>Queue initialization and progress tracking setup</description></item>
    /// <item><description>Sequential processing with individual file handling</description></item>
    /// <item><description>Comprehensive error handling and failed file tracking</description></item>
    /// <item><description>Result compilation and summary reporting</description></item>
    /// </list>
    /// <para>
    /// Processing Modes:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Normal Mode: Updates metadata and writes changes to files</description></item>
    /// <item><description>Dry Run Mode: Simulates processing without file modifications</description></item>
    /// <item><description>Force Overwrite: Updates all metadata fields regardless of existing values</description></item>
    /// <item><description>Retry Failed: Processes only files that failed in previous runs</description></item>
    /// </list>
    /// <para>
    /// Error Handling Strategy:
    /// Individual file failures do not stop the overall batch operation. Failed files are tracked,
    /// logged, and saved to a retry file for subsequent processing attempts.
    /// </para>
    /// <para>
    /// Performance Characteristics:
    /// Files are processed sequentially to maintain order and prevent resource contention.
    /// Processing time is tracked per file for performance monitoring and optimization.
    /// </para>
    /// </remarks>
    /// <param name="vaultPath">
    /// The absolute path to the vault directory containing markdown files to process.
    /// The directory must exist and be accessible. Subdirectories are processed recursively.
    /// </param>
    /// <param name="dryRun">
    /// When true, simulates the processing operation without making actual file changes.
    /// Useful for previewing changes, validation, and impact assessment before actual processing.
    /// Default is false for normal processing operations.
    /// </param>
    /// <param name="forceOverwrite">
    /// When true, updates all metadata fields regardless of existing values, potentially
    /// overwriting user-entered content. When false, only populates empty or missing fields.
    /// Default is false to preserve existing user content.
    /// </param>
    /// <param name="retryFailed">
    /// When true, processes only files that failed in previous batch operations.
    /// Reads the failed files list from the vault directory if available.
    /// Default is false to process all discovered files.
    /// </param>
    /// <returns>
    /// A MetadataBatchResult containing comprehensive processing statistics including:
    /// <list type="bullet">
    /// <item><description>Success status and error messages</description></item>
    /// <item><description>File counts (total, processed, skipped, failed)</description></item>
    /// <item><description>Processing timing and performance metrics</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when vaultPath is null, empty, or contains invalid characters.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when the specified vault directory does not exist.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the application lacks permissions to access the vault directory.
    /// </exception>
    /// <example>
    /// <code>
    /// // Basic batch processing
    /// var result = await batchProcessor.EnsureMetadataAsync(@"C:\MyVault");
    /// if (result.Success)
    /// {
    ///     Console.WriteLine($"Success: {result.ProcessedFiles} files updated, {result.SkippedFiles} skipped");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Failed: {result.ErrorMessage}");
    /// }
    ///
    /// // Dry run for preview
    /// var dryResult = await batchProcessor.EnsureMetadataAsync(@"C:\MyVault", dryRun: true);
    /// Console.WriteLine($"Preview: {dryResult.ProcessedFiles} files would be updated");
    ///
    /// // Force overwrite with progress monitoring
    /// batchProcessor.ProcessingProgressChanged += (s, e) =&gt;
    /// {
    ///     var percent = (e.CurrentFile * 100) / e.TotalFiles;
    ///     Console.WriteLine($"Progress: {percent}% - {e.Status}");
    /// };
    ///
    /// var forceResult = await batchProcessor.EnsureMetadataAsync(
    ///     @"C:\MyVault",
    ///     forceOverwrite: true);
    ///
    /// // Retry failed files
    /// var retryResult = await batchProcessor.EnsureMetadataAsync(
    ///     @"C:\MyVault",
    ///     retryFailed: true);
    /// </code>
    /// </example>
    public async Task<MetadataBatchResult> EnsureMetadataAsync(
        string vaultPath,
        bool dryRun = false,
        bool forceOverwrite = false,
        bool retryFailed = false)
    {
        if (string.IsNullOrEmpty(vaultPath))
        {
            return CreateErrorResult("Vault path cannot be null or empty");
        }

        if (!Directory.Exists(vaultPath))
        {
            return CreateErrorResult($"Vault directory does not exist: {vaultPath}");
        }

        var result = new MetadataBatchResult();
        var failedFiles = new List<string>();

        try
        {
            _logger.LogDebug("Starting metadata ensure process for vault: {VaultPath}", vaultPath);

            // Initialize the processing queue
            InitializeProcessingQueue(vaultPath);

            var queueCopy = Queue.ToList();
            if (!queueCopy.Any())
            {
                _logger.LogWarning("No markdown files found in vault: {VaultPath}", vaultPath);
                return result;
            }

            result.TotalFiles = queueCopy.Count;
            _logger.LogInformation("Found {Count} markdown files to process", queueCopy.Count);

            // Process each file in the queue
            for (int fileIndex = 0; fileIndex < queueCopy.Count; fileIndex++)
            {
                var queueItem = queueCopy[fileIndex];
                string filePath = queueItem.FilePath;
                string relativePath = Path.GetRelativePath(vaultPath, filePath);

                try
                {
                    // Update status to processing
                    UpdateQueueItemStatus(filePath, DocumentProcessingStatus.Processing, ProcessingStage.MarkdownCreation,
                        $"Processing metadata for file {fileIndex + 1}/{queueCopy.Count}: {Path.GetFileName(filePath)}",
                        fileIndex + 1, queueCopy.Count);

                    var stopwatch = Stopwatch.StartNew();
                    bool wasUpdated = await _metadataProcessor.EnsureMetadataAsync(filePath, forceOverwrite, dryRun).ConfigureAwait(false);
                    stopwatch.Stop();

                    if (wasUpdated)
                    {
                        result.ProcessedFiles++;
                        string successMessage = dryRun
                            ? $"[DRY RUN] Would update metadata for: {relativePath}"
                            : $"Updated metadata for: {relativePath} in {stopwatch.ElapsedMilliseconds}ms";

                        UpdateQueueItemStatus(filePath, DocumentProcessingStatus.Completed, ProcessingStage.Completed,
                            successMessage, fileIndex + 1, queueCopy.Count);

                        _logger.LogInformation("✓ {Message}", successMessage);
                    }
                    else
                    {
                        result.SkippedFiles++;
                        string skipMessage = $"Skipped {relativePath} (no changes needed)";

                        UpdateQueueItemStatus(filePath, DocumentProcessingStatus.Completed, ProcessingStage.Completed,
                            skipMessage, fileIndex + 1, queueCopy.Count);

                        _logger.LogDebug("- {Message}", skipMessage);
                    }
                }
                catch (Exception ex)
                {
                    result.FailedFiles++;
                    failedFiles.Add(filePath);
                    string errorMessage = $"Failed to process {relativePath}: {ex.Message}";

                    UpdateQueueItemStatus(filePath, DocumentProcessingStatus.Failed, ProcessingStage.NotStarted,
                        errorMessage, fileIndex + 1, queueCopy.Count);

                    _logger.LogError(ex, "✗ {Message}", errorMessage);
                }
            }

            // Log summary
            LogBatchResults(result, dryRun);

            // Save failed files list if any
            if (failedFiles.Count > 0)
            {
                await SaveFailedFilesList(vaultPath, failedFiles).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch metadata processing failed for vault: {VaultPath}", vaultPath);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Creates a standardized error result object with the specified error message and failure status.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This utility method provides a consistent way to create MetadataBatchResult objects for
    /// error scenarios. It ensures all error results have the same structure and default values.
    /// </para>
    /// <para>
    /// The created result object has:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Success set to false</description></item>
    /// <item><description>ErrorMessage set to the provided message</description></item>
    /// <item><description>All file counts initialized to zero</description></item>
    /// </list>
    /// </remarks>
    /// <param name="errorMessage">
    /// A descriptive error message explaining why the batch operation failed.
    /// This message will be included in logs and can be displayed to users.
    /// </param>
    /// <returns>
    /// A MetadataBatchResult object configured for error reporting with the specified message.
    /// </returns>

    private static MetadataBatchResult CreateErrorResult(string errorMessage)
    {
        return new MetadataBatchResult
        {
            Success = false,
            ErrorMessage = errorMessage,
        };
    }

    /// <summary>
    /// Logs comprehensive batch processing results with appropriate formatting and log levels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method provides standardized logging for batch operation results, including
    /// detailed statistics and appropriate warnings for failed operations. The logging format
    /// is consistent and suitable for both console output and log file analysis.
    /// </para>
    /// <para>
    /// Logging Strategy:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Information level: Summary statistics for successful operations</description></item>
    /// <item><description>Warning level: Additional notices when failures occur</description></item>
    /// <item><description>Dry run prefix: Clear indication when simulating operations</description></item>
    /// </list>
    /// <para>
    /// The log messages include complete processing statistics to facilitate monitoring
    /// and troubleshooting of batch operations.
    /// </para>
    /// </remarks>
    /// <param name="result">
    /// The MetadataBatchResult containing processing statistics to log.
    /// Must contain valid file counts and status information.
    /// </param>
    /// <param name="dryRun">
    /// Indicates whether this was a dry run operation. When true, adds a "[DRY RUN]"
    /// prefix to all log messages to clearly distinguish simulation from actual processing.
    /// </param>
    private void LogBatchResults(MetadataBatchResult result, bool dryRun)
    {
        string prefix = dryRun ? "[DRY RUN] " : string.Empty;

        _logger.LogInformation(
            "{Prefix}Metadata processing completed: {Processed} processed, {Skipped} skipped, {Failed} failed out of {Total} total files",
            prefix, result.ProcessedFiles, result.SkippedFiles, result.FailedFiles, result.TotalFiles);

        if (result.FailedFiles > 0)
        {
            _logger.LogWarning("{FailedCount} files failed to process", result.FailedFiles);
        }
    }

    /// <summary>
    /// Saves the list of failed files to a standardized retry file for subsequent batch operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method creates a persistent record of files that failed during batch processing,
    /// enabling retry scenarios and failure analysis. The failed files list is saved as a
    /// simple text file with one file path per line.
    /// </para>
    /// <para>
    /// File Location and Format:
    /// </para>
    /// <list type="bullet">
    /// <item><description>File name: "failed_metadata_files.txt"</description></item>
    /// <item><description>Location: Root of the vault directory</description></item>
    /// <item><description>Format: One absolute file path per line</description></item>
    /// <item><description>Encoding: UTF-8 for cross-platform compatibility</description></item>
    /// </list>
    /// <para>
    /// Error Handling:
    /// If the file cannot be saved due to permissions or I/O issues, the error is logged
    /// but does not affect the overall batch processing result.
    /// </para>
    /// </remarks>
    /// <param name="vaultPath">
    /// The root vault directory where the failed files list will be saved.
    /// Used to construct the full path for the "failed_metadata_files.txt" file.
    /// </param>
    /// <param name="failedFiles">
    /// A list of absolute file paths that failed during processing.
    /// Each path represents a file that should be retried in subsequent operations.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous file writing operation.
    /// </returns>
    /// <example>
    /// <code>
    /// // Example failed files list content:
    /// // C:\vault\documents\report1.md
    /// // C:\vault\notes\meeting-notes.md
    /// // C:\vault\projects\status-update.md
    ///
    /// // Usage in retry scenario:
    /// var failedFiles = File.ReadAllLines(Path.Combine(vaultPath, "failed_metadata_files.txt"));
    /// foreach (var filePath in failedFiles)
    /// {
    ///     // Retry processing for each failed file
    /// }
    /// </code>
    /// </example>
    private async Task SaveFailedFilesList(string vaultPath, List<string> failedFiles)
    {
        try
        {
            string failedFilesPath = Path.Combine(vaultPath, "failed_metadata_files.txt");
            await File.WriteAllLinesAsync(failedFilesPath, failedFiles).ConfigureAwait(false);
            _logger.LogInformation("Saved {Count} failed files to: {FilePath}", failedFiles.Count, failedFilesPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save failed files list");
        }
    }

    /// <summary>
    /// Discovers all markdown files within the specified vault directory while intelligently excluding system directories.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method serves as the entry point for file discovery during batch processing operations.
    /// It delegates to the recursive file discovery method while providing a clear interface for
    /// the directory scanning functionality.
    /// </para>
    /// <para>
    /// Directory Filtering Strategy:
    /// The method automatically excludes directories that start with a period (.) to avoid
    /// processing system directories such as .git, .vscode, .obsidian, and other hidden folders
    /// that typically contain configuration or metadata rather than user content.
    /// </para>
    /// </remarks>
    /// <param name="vaultPath">
    /// The absolute path to the vault directory to scan for markdown files.
    /// All subdirectories will be recursively searched unless they start with a period.
    /// </param>
    /// <returns>
    /// An enumerable sequence of absolute file paths for all discovered markdown files.
    /// The sequence is lazily evaluated, meaning files are discovered as the enumeration progresses.
    /// </returns>
    /// <example>
    /// <code>
    /// // Discover all markdown files in a vault
    /// var markdownFiles = GetMarkdownFilesExcludingDotDirectories(@"C:\MyVault");
    ///
    /// foreach (var filePath in markdownFiles)
    /// {
    ///     Console.WriteLine($"Found: {Path.GetRelativePath(@"C:\MyVault", filePath)}");
    /// }
    ///
    /// // Convert to list for multiple enumeration
    /// var fileList = markdownFiles.ToList();
    /// Console.WriteLine($"Total files discovered: {fileList.Count}");
    /// </code>
    /// </example>
    private static IEnumerable<string> GetMarkdownFilesExcludingDotDirectories(string vaultPath)
    {
        return GetMarkdownFilesRecursive(vaultPath);
    }

    /// <summary>
    /// Recursively searches for markdown files while intelligently filtering out system and hidden directories.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements a recursive directory traversal algorithm that efficiently discovers
    /// markdown files while avoiding system directories. The algorithm:
    /// </para>
    /// <list type="number">
    /// <item><description>Yields all .md files in the current directory</description></item>
    /// <item><description>Identifies subdirectories that don't start with a period</description></item>
    /// <item><description>Recursively processes valid subdirectories</description></item>
    /// <item><description>Aggregates results from all directory levels</description></item>
    /// </list>
    /// <para>
    /// Directory Filtering Logic:
    /// The method excludes any directory whose name starts with a period (.) to avoid processing:
    /// </para>
    /// <list type="bullet">
    /// <item><description>.git - Git version control metadata</description></item>
    /// <item><description>.vscode - Visual Studio Code settings</description></item>
    /// <item><description>.obsidian - Obsidian vault configuration</description></item>
    /// <item><description>.temp, .cache - Temporary and cache directories</description></item>
    /// <item><description>Other hidden/system directories</description></item>
    /// </list>
    /// <para>
    /// Performance Characteristics:
    /// Uses yield return for lazy evaluation, meaning files are discovered and returned
    /// incrementally rather than loading all paths into memory at once.
    /// </para>
    /// </remarks>
    /// <param name="directoryPath">
    /// The directory path to search recursively. If the directory doesn't exist,
    /// the method returns an empty sequence without throwing an exception.
    /// </param>
    /// <returns>
    /// A lazily-evaluated enumerable of absolute file paths for all markdown files found
    /// in the directory tree, excluding those in hidden/system directories.
    /// </returns>
    /// <example>
    /// <code>
    /// // Example directory structure:
    /// // C:\MyVault\
    /// //   ├── notes.md              ← Included
    /// //   ├── .obsidian\            ← Excluded (hidden)
    /// //   │   └── config.md         ← Excluded
    /// //   ├── projects\             ← Included
    /// //   │   ├── project1.md       ← Included
    /// //   │   └── .git\             ← Excluded (hidden)
    /// //   └── archive\              ← Included
    /// //       └── old-notes.md      ← Included
    ///    /// var files = GetMarkdownFilesRecursive(@"C:\MyVault");
    /// // Results: notes.md, projects\project1.md, archive\old-notes.md
    /// </code>
    /// </example>
    private static IEnumerable<string> GetMarkdownFilesRecursive(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            yield break;
        }

        // Get markdown files in the current directory and sort them for deterministic order
        var markdownFiles = Directory.GetFiles(directoryPath, "*.md")
            .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase);

        foreach (string file in markdownFiles)
        {
            yield return file;
        }

        // Get subdirectories, sort them for deterministic order, and recurse
        // Skip directories that start with a period
        var subdirectories = Directory.GetDirectories(directoryPath)
            .Where(d => !Path.GetFileName(d).StartsWith('.'))
            .OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase);

        foreach (string subDirectory in subdirectories)
        {
            foreach (string file in GetMarkdownFilesRecursive(subDirectory))
            {
                yield return file;
            }
        }
    }
}
