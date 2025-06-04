using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Models;

namespace NotebookAutomation.Core.Tools.Vault
{
    /// <summary>
    /// Batch processor for ensuring metadata in multiple markdown files.
    /// Provides queue management, progress tracking, and error handling capabilities
    /// with eventing support for UI integration and real-time monitoring.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the MetadataEnsureBatchProcessor class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <param name="processor">The metadata ensure processor instance.</param>
    /// <param name="appConfig">The application configuration.</param>
    public class MetadataEnsureBatchProcessor(
        ILogger<MetadataEnsureBatchProcessor> logger,
        MetadataEnsureProcessor processor,
        AppConfig appConfig)
    {
        private readonly ILogger<MetadataEnsureBatchProcessor> _logger = logger;
        private readonly MetadataEnsureProcessor _metadataProcessor = processor;
        private readonly AppConfig _appConfig = appConfig;

        // Queue-related fields
        private readonly List<QueueItem> _processingQueue = [];
        private readonly Lock _queueLock = new();

        /// <summary>
        /// Event triggered when processing progress changes.
        /// </summary>
        public event EventHandler<DocumentProcessingProgressEventArgs>? ProcessingProgressChanged;

        /// <summary>
        /// Event triggered when the processing queue changes.
        /// </summary>
        public event EventHandler<QueueChangedEventArgs>? QueueChanged;        /// <summary>
                                                                               /// Gets a read-only view of the current processing queue.
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
        /// Initializes the processing queue with markdown files from the specified directory.
        /// </summary>
        /// <param name="vaultPath">Path to the vault directory containing markdown files.</param>
        protected virtual void InitializeProcessingQueue(string vaultPath)
        {
            var markdownFiles = Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories)
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
                        StatusMessage = "Waiting to process metadata"
                    };
                    _processingQueue.Add(queueItem);
                }
            }

            OnQueueChanged();
        }

        /// <summary>
        /// Updates the status of a specific queue item and fires events.
        /// </summary>
        /// <param name="filePath">The file path to update.</param>
        /// <param name="status">The new processing status.</param>
        /// <param name="stage">The new processing stage.</param>
        /// <param name="statusMessage">The new status message.</param>
        /// <param name="currentFile">Current file index for progress tracking.</param>
        /// <param name="totalFiles">Total files for progress tracking.</param>
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
        /// Processes markdown files in the specified directory to ensure metadata consistency.
        /// </summary>
        /// <param name="vaultPath">Path to the vault directory containing markdown files.</param>
        /// <param name="dryRun">If true, simulates processing without making actual changes.</param>
        /// <param name="forceOverwrite">If true, updates metadata even if it already exists.</param>
        /// <param name="retryFailed">If true, retries only files that failed in previous runs.</param>
        /// <returns>A summary of processing results.</returns>
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
                _logger.LogInformation("Starting metadata ensure process for vault: {VaultPath}", vaultPath);

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
                        bool wasUpdated = await _metadataProcessor.EnsureMetadataAsync(filePath, forceOverwrite, dryRun);
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
                    await SaveFailedFilesList(vaultPath, failedFiles);
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
        /// Creates an error result with the specified message.
        /// </summary>
        private static MetadataBatchResult CreateErrorResult(string errorMessage)
        {
            return new MetadataBatchResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Logs the batch processing results.
        /// </summary>
        private void LogBatchResults(MetadataBatchResult result, bool dryRun)
        {
            string prefix = dryRun ? "[DRY RUN] " : "";

            _logger.LogInformation(
                "{Prefix}Metadata processing completed: {Processed} processed, {Skipped} skipped, {Failed} failed out of {Total} total files",
                prefix, result.ProcessedFiles, result.SkippedFiles, result.FailedFiles, result.TotalFiles);

            if (result.FailedFiles > 0)
            {
                _logger.LogWarning("{FailedCount} files failed to process", result.FailedFiles);
            }
        }

        /// <summary>
        /// Saves the list of failed files to a text file for retry purposes.
        /// </summary>
        private async Task SaveFailedFilesList(string vaultPath, List<string> failedFiles)
        {
            try
            {
                string failedFilesPath = Path.Combine(vaultPath, "failed_metadata_files.txt");
                await File.WriteAllLinesAsync(failedFilesPath, failedFiles);
                _logger.LogInformation("Saved {Count} failed files to: {FilePath}", failedFiles.Count, failedFilesPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save failed files list");
            }
        }
    }

    /// <summary>
    /// Result of batch metadata processing operation.
    /// </summary>
    public class MetadataBatchResult
    {
        /// <summary>
        /// Gets or sets whether the operation was successful.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Gets or sets the error message if the operation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the total number of files found.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of files that were processed (updated).
        /// </summary>
        public int ProcessedFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of files that were skipped (no changes needed).
        /// </summary>
        public int SkippedFiles { get; set; }        /// <summary>
                                                     /// Gets or sets the number of files that failed to process.
                                                     /// </summary>
        public int FailedFiles { get; set; }
    }
}
