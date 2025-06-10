// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Batch processor for generating vault index files in an Obsidian vault.
/// </summary>
/// <remarks>
/// The <c>VaultIndexBatchProcessor</c> class provides functionality for generating
/// index files for each folder in a vault directory. It supports queue management,
/// progress tracking, hierarchy detection, and error handling, with eventing capabilities
/// for real-time monitoring and UI integration.
///
/// Features:
/// - Generates folder-named index files (e.g., "Module 1.md" for "Module 1" folder)
/// - Auto-detects hierarchy levels (MBA, Program, Course, Class, Module, Lesson)
/// - Applies appropriate templates based on detected hierarchy
/// - Organizes content by type using YAML frontmatter and filename patterns
/// - Optional Obsidian Bases integration for dynamic content views
/// - Supports dry-run mode for previewing changes.
/// </remarks>
public class VaultIndexBatchProcessor(ILogger<VaultIndexBatchProcessor> _logger,
    IVaultIndexProcessor processor,
    IMetadataHierarchyDetector hierarchyDetector)
{
    private readonly ILogger<VaultIndexBatchProcessor> _logger = _logger;
    private readonly IVaultIndexProcessor _indexProcessor = processor;
    private readonly IMetadataHierarchyDetector _hierarchyDetector = hierarchyDetector;

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
    public event EventHandler<QueueChangedEventArgs>? QueueChanged;

    /// <summary>
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
    /// <param name="folderPath">The path of the folder being processed.</param>
    /// <param name="status">The current processing status message.</param>
    /// <param name="currentFolder">The current folder index being processed.</param>
    /// <param name="totalFolders">The total number of folders to process.</param>
    protected virtual void OnProcessingProgressChanged(string folderPath, string status, int currentFolder, int totalFolders)
    {
        ProcessingProgressChanged?.Invoke(this, new DocumentProcessingProgressEventArgs(folderPath, status, currentFolder, totalFolders));
    }

    /// <summary>
    /// Raises the QueueChanged event.
    /// </summary>
    /// <param name="changedItem">The specific item that changed, or null if the entire queue changed.</param>
    protected virtual void OnQueueChanged(QueueItem? changedItem = null)
    {
        lock (_queueLock)
        {
            QueueChanged?.Invoke(this, new QueueChangedEventArgs(_processingQueue.AsReadOnly(), changedItem));
        }
    }    /// <summary>
         /// Initializes the processing queue with folders from the specified vault directory.
         /// </summary>
         /// <param name="vaultPath">Path to the vault directory to scan for folders.</param>
         /// <param name="templateTypes">Optional filter for specific template types to generate.</param>
         /// <param name="vaultRoot">Optional vault root for hierarchy calculation. If null, uses vaultPath.</param>
    protected virtual void InitializeProcessingQueue(string vaultPath, List<string>? templateTypes = null, string? vaultRoot = null)
    { // Get all directories in the vault, including the vault root itself
        var directories = new List<string> { vaultPath }; // Start with vault root
        directories.AddRange(Directory.GetDirectories(vaultPath, "*", SearchOption.AllDirectories)
            .Where(d => !IsIgnoredDirectory(d)));

        directories = directories
            .OrderBy(d => d) // Process in consistent order
            .ToList();        // Note: The vault root itself should get a main index file at level 1.
        lock (_queueLock)
        {
            _processingQueue.Clear();

            foreach (string folderPath in directories)
            {                // Determine if this folder should be processed based on templateTypes filter
                if (templateTypes != null && templateTypes.Count > 0)
                {

                    // CRITICAL: Always use the actual vault root for hierarchy calculation (not the path being processed)
                    // vaultRoot should contain the correct vault root path:
                    //   1. For standard operation: The configured vault root (from AppConfig)
                    //   2. For --override-vault-root: The provided path becomes the new vault root (level 0)
                    // This ensures that hierarchy levels are calculated consistently based on the vault structure,
                    // even when starting processing from a subdirectory

                    // Enhanced debug logging for hierarchy calculation
                    _logger.LogDebug("InitializeProcessingQueue - Calculating hierarchy for '{FolderPath}' (relative to vault root '{VaultRoot}')",
                        folderPath, vaultRoot ?? "(null - using default)");

                    var hierarchyLevel = _hierarchyDetector.CalculateHierarchyLevel(folderPath, vaultRoot);
                    string templateType = _hierarchyDetector.GetTemplateTypeFromHierarchyLevel(hierarchyLevel);

                    // Log calculated hierarchy information
                    _logger.LogDebug("InitializeProcessingQueue - Hierarchy result: Level {Level} = '{TemplateType}'", hierarchyLevel, templateType);

                    _logger.LogDebug("InitializeProcessingQueue - Folder: '{FolderPath}' -> HierarchyLevel: {HierarchyLevel} -> TemplateType: '{TemplateType}' (Required: {RequiredTypes})",
                        folderPath, hierarchyLevel, templateType, string.Join(", ", templateTypes));

                    if (!templateTypes.Contains(templateType))
                    {
                        _logger.LogDebug("InitializeProcessingQueue - Skipping folder '{FolderPath}' - TemplateType '{TemplateType}' not in required types",
                            folderPath, templateType);
                        continue; // Skip this folder
                    }

                    _logger.LogDebug("InitializeProcessingQueue - Including folder '{FolderPath}' - TemplateType '{TemplateType}' matches required types",
                        folderPath, templateType);
                }
                var queueItem = new QueueItem(folderPath, "INDEX")
                {
                    Status = DocumentProcessingStatus.Waiting,
                    Stage = ProcessingStage.NotStarted,
                    StatusMessage = "Waiting to generate index",
                };
                _processingQueue.Add(queueItem);
            }
        }

        OnQueueChanged();
    }

    /// <summary>
    /// Determines if a directory should be ignored during processing.
    /// </summary>
    /// <param name="directoryPath">The directory path to check.</param>
    /// <returns>True if the directory should be ignored, false otherwise.</returns>
    private static bool IsIgnoredDirectory(string directoryPath)
    {
        string dirName = Path.GetFileName(directoryPath);

        // Skip hidden directories and common ignored folders
        return dirName.StartsWith('.') ||
               dirName.Equals("templates", StringComparison.OrdinalIgnoreCase) || dirName.Equals("attachments", StringComparison.OrdinalIgnoreCase) ||
               dirName.Equals("resources", StringComparison.OrdinalIgnoreCase) ||
               dirName.Equals("_templates", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Updates the status of a specific queue item and fires events.
    /// </summary>
    /// <param name="folderPath">The folder path to update.</param>
    /// <param name="status">The new processing status.</param>
    /// <param name="stage">The new processing stage.</param>
    /// <param name="statusMessage">The new status message.</param>
    /// <param name="currentFolder">Current folder index for progress tracking.</param>
    /// <param name="totalFolders">Total folders for progress tracking.</param>
    protected virtual void UpdateQueueItemStatus(string folderPath, DocumentProcessingStatus status, ProcessingStage stage, string statusMessage, int currentFolder, int totalFolders)
    {
        QueueItem? queueItem = null;

        lock (_queueLock)
        {
            queueItem = _processingQueue.FirstOrDefault(q => q.FilePath == folderPath);
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
            OnProcessingProgressChanged(folderPath, statusMessage, currentFolder, totalFolders);
        }
    }

    /// <summary>
    /// Generates vault index files for folders in the specified directory.
    /// </summary>
    /// <param name="vaultPath">Path to the vault directory.</param>
    /// <param name="dryRun">If true, simulates processing without making actual changes.</param>
    /// <param name="templateTypes">Optional filter for specific template types to generate.</param>
    /// <param name="forceOverwrite">If true, regenerates index files even if they already exist.</param>
    /// <returns>A summary of processing results.</returns>    /// <summary>
    /// Generates index files for a vault directory structure.
    /// </summary>
    /// <param name="vaultPath">The path to start processing from (can be the vault root or any subdirectory)</param>
    /// <param name="dryRun">When true, simulates processing without making changes</param>
    /// <param name="templateTypes">Optional list of template types to filter by (e.g., "program", "course", "class", "module", "lesson")</param>
    /// <param name="forceOverwrite">When true, regenerates indices even if they already exist</param>
    /// <param name="vaultRoot">
    ///   The vault root path to use for hierarchy calculation. This is CRITICAL for correct template selection.
    ///   When null: Uses configured vault root from AppConfig
    ///   When "OVERRIDE" string: Uses the vaultPath as the vault root (level 0)
    ///   When explicit path: Uses that path as the vault root
    /// </param>
    /// <returns>A result containing processing statistics and error information</returns>    /// <remarks>
    /// It's important to understand the difference between vaultPath and vaultRoot parameters:
    /// - vaultPath: Where to START processing - can be any subdirectory within the vault
    /// - vaultRoot: What to use as the BASE for hierarchy level calculation
    ///
    /// In most cases, vaultRoot should be the actual configured vault root from AppConfig,
    /// ensuring that hierarchy levels are calculated consistently regardless of where
    /// processing starts (e.g., a lesson dir is still level 5 even when starting from there).
    /// </remarks>
    public virtual async Task<VaultIndexBatchResult> GenerateIndexesAsync(
        string vaultPath,
        bool dryRun = false,
        List<string>? templateTypes = null,
        bool forceOverwrite = false,
        string? vaultRoot = null)
    {
        if (string.IsNullOrEmpty(vaultPath))
        {
            return CreateErrorResult("Vault path cannot be null or empty");
        }

        if (!Directory.Exists(vaultPath))
        {
            return CreateErrorResult($"Vault directory does not exist: {vaultPath}");
        }

        var result = new VaultIndexBatchResult();
        var failedFolders = new List<string>();

        try
        {
            _logger.LogInformation("Starting vault index generation for vault: {VaultPath}", vaultPath);

            // Clean up old index.md files first (if not dry run)
            if (!dryRun)
            {
                await CleanupOldIndexFilesAsync(vaultPath).ConfigureAwait(false);
            }

            // Initialize the processing queue
            InitializeProcessingQueue(vaultPath, templateTypes, vaultRoot);

            var queueCopy = Queue.ToList();
            if (!queueCopy.Any())
            {
                _logger.LogWarning("No folders found to process in vault: {VaultPath}", vaultPath);
                return result;
            }

            result.TotalFolders = queueCopy.Count;
            _logger.LogInformation("Found {Count} folders to process", queueCopy.Count);

            // Process each folder in the queue
            for (int folderIndex = 0; folderIndex < queueCopy.Count; folderIndex++)
            {
                var queueItem = queueCopy[folderIndex];
                string folderPath = queueItem.FilePath;
                string relativePath = Path.GetRelativePath(vaultPath, folderPath);

                try
                {
                    // Update status to processing
                    UpdateQueueItemStatus(folderPath, DocumentProcessingStatus.Processing, ProcessingStage.MarkdownCreation,
                        $"Generating index for folder {folderIndex + 1}/{queueCopy.Count}: {Path.GetFileName(folderPath)}",
                        folderIndex + 1, queueCopy.Count);

                    var stopwatch = Stopwatch.StartNew();                    // IMPORTANT: Always use the explicitly provided vaultRoot for hierarchy calculation
                    // This ensures consistency when running from subdirectories within the vault
                    string effectiveVaultPath = vaultRoot ?? vaultPath; // Fallback to vaultPath if vaultRoot is null

                    _logger.LogDebug($"Using vault root '{effectiveVaultPath}' for index generation on folder '{folderPath}'");
                    _logger.LogDebug($"Hierarchy levels will be calculated relative to: {effectiveVaultPath}");

                    // Log path relationships for debugging
                    string fullFolderPath = Path.GetFullPath(folderPath);
                    string fullVaultPath = Path.GetFullPath(effectiveVaultPath);
                    string relPath = Path.GetRelativePath(fullVaultPath, fullFolderPath);
                    _logger.LogDebug($"Folder relationship - Path: {fullFolderPath}, VaultRoot: {fullVaultPath}, RelativePath: {relPath}");

                    // Calculate expected hierarchy level for verification
                    int expectedLevel = relPath == "." ? 1 : relPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Length + 1;
                    _logger.LogDebug($"Expected hierarchy level: {expectedLevel} (template: {_hierarchyDetector.GetTemplateTypeFromHierarchyLevel(expectedLevel)})");

                    bool wasGenerated = await _indexProcessor.GenerateIndexAsync(
                        folderPath,
                        effectiveVaultPath,
                        forceOverwrite,
                        dryRun).ConfigureAwait(false);
                    stopwatch.Stop();

                    if (wasGenerated)
                    {
                        result.ProcessedFolders++;
                        string successMessage = dryRun
                            ? $"[DRY RUN] Would generate index for: {relativePath}"
                            : $"Generated index for: {relativePath} in {stopwatch.ElapsedMilliseconds}ms";

                        UpdateQueueItemStatus(folderPath, DocumentProcessingStatus.Completed, ProcessingStage.Completed,
                            successMessage, folderIndex + 1, queueCopy.Count);

                        _logger.LogInformation("✓ {Message}", successMessage);
                    }
                    else
                    {
                        result.SkippedFolders++;
                        string skipMessage = $"Skipped {relativePath} (no changes needed or no content)";

                        UpdateQueueItemStatus(folderPath, DocumentProcessingStatus.Completed, ProcessingStage.Completed,
                            skipMessage, folderIndex + 1, queueCopy.Count);

                        _logger.LogDebug("- {Message}", skipMessage);
                    }
                }
                catch (Exception ex)
                {
                    result.FailedFolders++;
                    failedFolders.Add(folderPath);
                    string errorMessage = $"Failed to process {relativePath}: {ex.Message}";

                    UpdateQueueItemStatus(folderPath, DocumentProcessingStatus.Failed, ProcessingStage.NotStarted,
                        errorMessage, folderIndex + 1, queueCopy.Count);

                    _logger.LogError(ex, "✗ {Message}", errorMessage);
                }
            }

            // Log summary
            LogBatchResults(result, dryRun);

            // Save failed folders list if any
            if (failedFolders.Count > 0)
            {
                await SaveFailedFoldersList(vaultPath, failedFolders).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch index generation failed for vault: {VaultPath}", vaultPath);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Cleans up old index.md files throughout the vault.
    /// </summary>
    /// <param name="vaultPath">Path to the vault directory.</param>
    private async Task CleanupOldIndexFilesAsync(string vaultPath)
    {
        try
        {
            var indexFiles = Directory.GetFiles(vaultPath, "index.md", SearchOption.AllDirectories);

            foreach (string indexFile in indexFiles)
            {
                try
                {
                    File.Delete(indexFile);
                    _logger.LogDebug("Deleted old index file: {FilePath}", indexFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old index file: {FilePath}", indexFile);
                }
            }

            if (indexFiles.Length > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old index.md files", indexFiles.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old index files");
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an error result with the specified message.
    /// </summary>
    private static VaultIndexBatchResult CreateErrorResult(string errorMessage)
    {
        return new VaultIndexBatchResult
        {
            Success = false,
            ErrorMessage = errorMessage,
        };
    }

    /// <summary>
    /// Logs the batch processing results.
    /// </summary>
    private void LogBatchResults(VaultIndexBatchResult result, bool dryRun)
    {
        string prefix = dryRun ? "[DRY RUN] " : string.Empty;

        _logger.LogInformation(
            "{Prefix}Index generation completed: {Processed} processed, {Skipped} skipped, {Failed} failed out of {Total} total folders",
            prefix, result.ProcessedFolders, result.SkippedFolders, result.FailedFolders, result.TotalFolders);

        if (result.FailedFolders > 0)
        {
            _logger.LogWarning("{FailedCount} folders failed to process", result.FailedFolders);
        }
    }

    /// <summary>
    /// Saves the list of failed folders to a text file for retry purposes.
    /// </summary>
    private async Task SaveFailedFoldersList(string vaultPath, List<string> failedFolders)
    {
        try
        {
            string failedFoldersPath = Path.Combine(vaultPath, "failed_index_folders.txt");
            await File.WriteAllLinesAsync(failedFoldersPath, failedFolders).ConfigureAwait(false);
            _logger.LogInformation("Saved {Count} failed folders to: {FilePath}", failedFolders.Count, failedFoldersPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save failed folders list");
        }
    }
}

/// <summary>
/// Result of batch vault index generation operation.
/// </summary>
public class VaultIndexBatchResult
{
    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the total number of folders found.
    /// </summary>
    public int TotalFolders { get; set; }

    /// <summary>
    /// Gets or sets the number of folders that were processed (index generated).
    /// </summary>
    public int ProcessedFolders { get; set; }

    /// <summary>
    /// Gets or sets the number of folders that were skipped (no changes needed).
    /// </summary>
    public int SkippedFolders { get; set; }

    /// <summary>
    /// Gets or sets the number of folders that failed to process.
    /// </summary>
    public int FailedFolders { get; set; }
}
