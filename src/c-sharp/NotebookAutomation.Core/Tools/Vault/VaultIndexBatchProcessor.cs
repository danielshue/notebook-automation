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
    }

    /// <summary>
    /// Initializes the processing queue with folders from the specified vault directory.
    /// </summary>
    /// <param name="vaultPath">Path to the vault directory to scan for folders.</param>
    /// <param name="templateTypes">Optional filter for specific template types to generate. If specified, only folders matching these types will be queued.</param>
    /// <param name="vaultRoot">Optional vault root for hierarchy calculation. If null, uses vaultPath as the reference point for hierarchy detection.</param>
    /// <remarks>
    /// This method scans the vault directory recursively to identify all folders that require index generation.
    /// It applies filtering based on template types and ignores standard directories like hidden folders,
    /// templates, attachments, and resources. Each qualifying folder is added to the processing queue
    /// with an initial status of "Waiting".
    ///
    /// The method processes folders in a consistent alphabetical order to ensure predictable execution.
    /// Hierarchy levels are calculated using the provided vault root or the vault path as a fallback,
    /// which determines the appropriate template type for each folder.
    /// </remarks>
    /// <exception cref="DirectoryNotFoundException">Thrown when the vault path does not exist.</exception>
    /// <example>
    /// <code>
    /// // Initialize queue for all template types
    /// InitializeProcessingQueue("/path/to/vault");
    ///
    /// // Initialize queue for specific template types only
    /// var templateTypes = new List&lt;string&gt; { "course", "module" };
    /// InitializeProcessingQueue("/path/to/vault", templateTypes, "/vault/root");
    /// </code>
    /// </example>
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
    /// <param name="directoryPath">The directory path to check for exclusion criteria.</param>
    /// <returns>True if the directory should be ignored, false otherwise.</returns>
    /// <remarks>
    /// This method filters out directories that should not have index files generated:
    /// - Hidden directories (starting with '.')
    /// - Common resource directories: templates, attachments, resources, _templates
    ///
    /// The comparison is case-insensitive to handle various naming conventions.
    /// This helps maintain a clean vault structure by avoiding index generation
    /// in utility and resource directories.
    /// </remarks>
    /// <example>
    /// <code>
    /// // These would return true (ignored):
    /// IsIgnoredDirectory("/vault/.obsidian");       // Hidden directory
    /// IsIgnoredDirectory("/vault/templates");       // Template directory
    /// IsIgnoredDirectory("/vault/Attachments");     // Attachments (case-insensitive)
    ///
    /// // These would return false (not ignored):
    /// IsIgnoredDirectory("/vault/Course 1");        // Regular course folder
    /// IsIgnoredDirectory("/vault/Module 1");        // Regular module folder
    /// </code>
    /// </example>
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
    /// Updates the status of a specific queue item and fires progress events.
    /// </summary>
    /// <param name="folderPath">The folder path identifying the queue item to update.</param>
    /// <param name="status">The new processing status to assign.</param>
    /// <param name="stage">The new processing stage to assign.</param>
    /// <param name="statusMessage">The new status message describing current operation.</param>
    /// <param name="currentFolder">Current folder index for progress tracking (1-based).</param>
    /// <param name="totalFolders">Total number of folders for progress tracking.</param>
    /// <remarks>
    /// This method provides thread-safe updates to queue item status and automatically
    /// manages timing information:
    /// - Sets ProcessingStartTime when status changes to Processing (if not already set)
    /// - Sets ProcessingEndTime when status changes to Completed or Failed
    ///
    /// After updating the queue item, it fires both QueueChanged and ProcessingProgressChanged
    /// events to notify subscribers of the status change. This enables real-time monitoring
    /// and UI updates during batch processing operations.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Update status to indicate processing has started
    /// UpdateQueueItemStatus(
    ///     "/vault/Module 1",
    ///     DocumentProcessingStatus.Processing,
    ///     ProcessingStage.MarkdownCreation,
    ///     "Generating index for Module 1",
    ///     3, 10);
    ///
    /// // Update status to indicate completion
    /// UpdateQueueItemStatus(
    ///     "/vault/Module 1",
    ///     DocumentProcessingStatus.Completed,
    ///     ProcessingStage.Completed,
    ///     "Successfully generated index for Module 1",
    ///     3, 10);
    /// </code>
    /// </example>
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
    /// Generates vault index files for all folders in the specified directory structure.
    /// </summary>
    /// <param name="vaultPath">The path to start processing from (can be the vault root or any subdirectory).</param>
    /// <param name="dryRun">When true, simulates processing without making actual file changes.</param>
    /// <param name="templateTypes">Optional list of template types to filter by (e.g., "program", "course", "class", "module", "lesson"). If null, processes all folder types.</param>
    /// <param name="forceOverwrite">When true, regenerates index files even if they already exist and are up-to-date.</param>
    /// <param name="vaultRoot">
    /// The vault root path to use for hierarchy calculation. This is critical for correct template selection:
    /// - When null: Uses configured vault root from AppConfig
    /// - When "OVERRIDE" string: Uses the vaultPath as the vault root (level 0)
    /// - When explicit path: Uses that path as the vault root for hierarchy calculations
    /// </param>
    /// <returns>A <see cref="VaultIndexBatchResult"/> containing processing statistics and error information.</returns>
    /// <remarks>
    /// <para>
    /// This method is the main entry point for batch index generation. It performs the following operations:
    /// 1. Validates input parameters and directory existence
    /// 2. Optionally cleans up old index.md files
    /// 3. Initializes the processing queue with discovered folders
    /// 4. Processes each folder sequentially, generating appropriate index files
    /// 5. Tracks progress and errors throughout the operation
    /// 6. Saves a list of failed folders for retry purposes
    /// </para>
    ///
    /// <para>
    /// It's important to understand the difference between vaultPath and vaultRoot parameters:
    /// - <paramref name="vaultPath"/>: Where to START processing - can be any subdirectory within the vault
    /// - <paramref name="vaultRoot"/>: What to use as the BASE for hierarchy level calculation
    /// </para>
    ///
    /// <para>
    /// In most cases, vaultRoot should be the actual configured vault root from AppConfig,
    /// ensuring that hierarchy levels are calculated consistently regardless of where
    /// processing starts (e.g., a lesson directory is still level 5 even when starting from there).
    /// </para>
    ///
    /// <para>
    /// The method supports both full vault processing and partial processing with template type filters,
    /// making it suitable for targeted regeneration of specific hierarchy levels.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when vaultPath is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the vault directory does not exist.</exception>
    /// <example>
    /// <code>
    /// // Generate indexes for entire vault
    /// var result = await processor.GenerateIndexesAsync("/path/to/vault");
    ///
    /// // Dry run with specific template types
    /// var templateTypes = new List&lt;string&gt; { "course", "module" };
    /// var result = await processor.GenerateIndexesAsync(
    ///     "/path/to/vault",
    ///     dryRun: true,
    ///     templateTypes: templateTypes);
    ///
    /// // Force regeneration of all indexes
    /// var result = await processor.GenerateIndexesAsync(
    ///     "/path/to/vault",
    ///     forceOverwrite: true);
    ///
    /// // Process subdirectory with explicit vault root
    /// var result = await processor.GenerateIndexesAsync(
    ///     "/path/to/vault/Course 1",
    ///     vaultRoot: "/path/to/vault");
    /// </code>
    /// </example>
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
    /// Cleans up old index.md files throughout the vault directory structure.
    /// </summary>
    /// <param name="vaultPath">Path to the vault directory to clean recursively.</param>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    /// <remarks>
    /// This method recursively searches for and removes all existing "index.md" files
    /// within the vault directory tree. This cleanup step ensures that the batch
    /// index generation process starts with a clean slate, preventing conflicts
    /// between old and new index files.
    ///
    /// The method is fault-tolerant and will continue cleaning even if individual
    /// file deletions fail. Failed deletions are logged as warnings but do not
    /// stop the overall cleanup process.
    ///
    /// This method is automatically called by <see cref="GenerateIndexesAsync"/> when
    /// not running in dry-run mode.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Clean up old index files before regeneration
    /// await CleanupOldIndexFilesAsync("/path/to/vault");
    /// </code>
    /// </example>
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
                    _logger.LogDebug($"Deleted old index file: {indexFile}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to delete old index file: {indexFile}");
                }
            }

            if (indexFiles.Length > 0)
            {
                _logger.LogInformation($"Cleaned up {indexFiles.Length} old index.md files");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old index files");
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a standardized error result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message to include in the result.</param>
    /// <returns>A <see cref="VaultIndexBatchResult"/> with Success set to false and the provided error message.</returns>
    /// <remarks>
    /// This utility method provides a consistent way to create error results throughout
    /// the class. It ensures that error results always have the Success flag set to false
    /// and include descriptive error messages for debugging and user feedback.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create an error result for invalid input
    /// return CreateErrorResult("Vault path cannot be null or empty");
    ///
    /// // Create an error result for missing directory
    /// return CreateErrorResult($"Vault directory does not exist: {vaultPath}");
    /// </code>
    /// </example>
    private static VaultIndexBatchResult CreateErrorResult(string errorMessage)
    {
        return new VaultIndexBatchResult
        {
            Success = false,
            ErrorMessage = errorMessage,
        };
    }

    /// <summary>
    /// Logs a comprehensive summary of the batch processing results.
    /// </summary>
    /// <param name="result">The batch result containing processing statistics.</param>
    /// <param name="dryRun">Whether the operation was run in dry-run mode.</param>
    /// <remarks>
    /// This method provides standardized logging output for batch processing operations.
    /// It logs key metrics including processed, skipped, and failed folder counts,
    /// with special handling for dry-run mode indicators.
    ///
    /// When failures occur, additional warning-level logging is performed to highlight
    /// issues that may require attention. This helps with monitoring and troubleshooting
    /// batch operations in production environments.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Example logged output for normal operation:
    /// // "Index generation completed: 15 processed, 3 skipped, 0 failed out of 18 total folders"
    ///
    /// // Example logged output for dry-run:
    /// // "[DRY RUN] Index generation completed: 15 processed, 3 skipped, 0 failed out of 18 total folders"
    ///
    /// LogBatchResults(result, false);
    /// </code>
    /// </example>
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
    /// Saves the list of failed folders to a text file for analysis and retry purposes.
    /// </summary>
    /// <param name="vaultPath">The vault directory path where the failed folders file will be saved.</param>
    /// <param name="failedFolders">The list of folder paths that failed to process.</param>
    /// <returns>A task representing the asynchronous file save operation.</returns>
    /// <remarks>
    /// This method creates a "failed_index_folders.txt" file in the vault root directory
    /// containing the full paths of all folders that failed to process during the batch
    /// operation. This enables:
    ///
    /// - Easy identification of problematic folders for manual investigation
    /// - Potential retry operations on just the failed folders
    /// - Analysis of failure patterns across the vault structure
    ///
    /// The method is fault-tolerant and will log errors if the file cannot be written,
    /// but will not throw exceptions that would disrupt the main batch operation.
    /// </remarks>
    /// <example>
    /// <code>
    /// var failedFolders = new List&lt;string&gt;
    /// {
    ///     "/vault/Course 1/Module 1",
    ///     "/vault/Course 2/Module 3"
    /// };
    /// await SaveFailedFoldersList("/vault", failedFolders);
    ///
    /// // This creates: /vault/failed_index_folders.txt
    /// // Content:
    /// // /vault/Course 1/Module 1
    /// // /vault/Course 2/Module 3
    /// </code>
    /// </example>
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