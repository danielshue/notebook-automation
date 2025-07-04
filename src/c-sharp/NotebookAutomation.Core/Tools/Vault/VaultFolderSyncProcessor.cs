// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Processor for synchronizing directory structures between OneDrive and Obsidian vault.
/// </summary>
/// <remarks>
/// <para>
/// The VaultFolderSyncProcessor class provides functionality for ensuring that the directory
/// structure present in OneDrive is replicated in the corresponding vault location, and optionally
/// vice versa in bidirectional mode. It analyzes the folder hierarchies and creates matching 
/// directories as needed.
/// </para>
/// <para>
/// Key Capabilities:
/// </para>
/// <list type="bullet">
/// <item><description>Unidirectional synchronization (OneDrive → Vault)</description></item>
/// <item><description>Bidirectional synchronization (OneDrive ↔ Vault)</description></item>
/// <item><description>Recursive directory structure analysis</description></item>
/// <item><description>Intelligent path mapping between OneDrive and vault locations</description></item>
/// <item><description>Directory creation with proper error handling</description></item>
/// <item><description>Dry run support for preview and validation scenarios</description></item>
/// <item><description>Progress tracking and event-driven updates</description></item>
/// <item><description>Comprehensive logging and error reporting</description></item>
/// </list>
/// <para>
/// Path Mapping Strategy:
/// The processor uses configuration settings to map OneDrive paths to vault paths:
/// - OneDrive source: Uses onedrive_fullpath_root + notebook_vault_resources_basepath
/// - Vault target: Uses notebook_vault_fullpath_root as the base destination
/// </para>
/// <para>
/// Synchronization Process:
/// </para>
/// <list type="number">
/// <item><description>Phase 1: Scan OneDrive directory structure and create missing vault directories</description></item>
/// <item><description>Phase 2 (bidirectional only): Scan vault directory structure and create missing OneDrive directories</description></item>
/// <item><description>Track statistics and report progress for both phases</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Basic bidirectional synchronization (OneDrive ↔ Vault) - DEFAULT
/// var processor = serviceProvider.GetService&lt;VaultFolderSyncProcessor&gt;();
/// var result = await processor.SyncDirectoriesAsync(
///     @"MBA/Finance",
///     @"C:\Users\user\Vault");
///
/// // Explicit bidirectional synchronization with recursive scanning
/// var bidirectionalResult = await processor.SyncDirectoriesAsync(
///     @"MBA/Finance",
///     @"C:\Users\user\Vault",
///     dryRun: false,
///     bidirectional: true,
///     recursive: true);
///
/// // Unidirectional synchronization (OneDrive → Vault only) with non-recursive scanning
/// var unidirectionalResult = await processor.SyncDirectoriesAsync(
///     @"MBA/Finance",
///     @"C:\Users\user\Vault",
///     dryRun: false,
///     bidirectional: false,
///     recursive: false);
///
/// if (result.Success)
/// {
///     Console.WriteLine($"Synchronized {result.SynchronizedFolders} folders");
///     Console.WriteLine($"Created {result.CreatedVaultFolders} vault directories");
///     Console.WriteLine($"Created {result.CreatedOneDriveFolders} OneDrive directories");
/// }
///
/// // Dry run for preview with recursive scanning
/// var previewResult = await processor.SyncDirectoriesAsync(
///     oneDrivePath,
///     vaultPath,
///     dryRun: true,
///     bidirectional: true,
///     recursive: true);
/// </code>
/// </example>
public class VaultFolderSyncProcessor(
    ILogger<VaultFolderSyncProcessor> logger,
    AppConfig appConfig) : IVaultFolderSyncProcessor
{
    private readonly ILogger<VaultFolderSyncProcessor> _logger = logger;
    private readonly AppConfig _appConfig = appConfig;

    /// <summary>
    /// Event triggered when processing progress changes.
    /// </summary>
    public event EventHandler<DocumentProcessingProgressEventArgs>? ProcessingProgressChanged;


    /// <summary>
    /// Synchronizes directory structures between OneDrive and vault locations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the primary method for synchronizing folder structures from OneDrive to the vault.
    /// It orchestrates the complete synchronization workflow including directory discovery,
    /// path mapping, existence checking, and directory creation.
    /// </para>
    /// <para>
    /// Processing Workflow:
    /// </para>
    /// <list type="number">
    /// <item><description>Validates input paths and configuration</description></item>
    /// <item><description>Constructs full OneDrive source path using configuration</description></item>
    /// <item><description>Recursively scans OneDrive directory structure</description></item>
    /// <item><description>Maps each OneDrive path to corresponding vault path</description></item>
    /// <item><description>Checks for existing directories in vault</description></item>
    /// <item><description>Creates missing directories with proper error handling</description></item>
    /// <item><description>Tracks statistics and reports progress</description></item>
    /// </list>
    /// <para>
    /// Path Construction:
    /// The method uses configuration settings to build the complete source path:
    /// OneDriveSourcePath = onedrive_fullpath_root + notebook_vault_resources_basepath + relativePath
    /// </para>
    /// <para>
    /// Directory Creation:
    /// New directories are created with system default permissions. The process handles
    /// permission errors gracefully and continues with remaining directories.
    /// </para>
    /// <para>
    /// Error Handling:
    /// Individual directory creation failures are logged and tracked but do not stop
    /// the overall synchronization process, ensuring maximum coverage even with partial failures.
    /// </para>
    /// </remarks>
    /// <param name="oneDrivePath">
    /// The relative path within OneDrive to synchronize from.
    /// This path is combined with the configured onedrive_fullpath_root and 
    /// notebook_vault_resources_basepath to form the complete source path.
    /// </param>
    /// <param name="vaultPath">
    /// The target vault path where directories should be synchronized to.
    /// This should be an absolute path within the vault structure.
    /// If not provided, uses the configured notebook_vault_fullpath_root.
    /// </param>
    /// <param name="dryRun">
    /// When true, simulates the synchronization process without creating actual directories.
    /// Useful for previewing changes, validation, and testing scenarios.
    /// All processing steps are performed except the final directory creation operation.
    /// Default is false for normal operation.
    /// </param>
    /// <param name="bidirectional">
    /// When true, performs bidirectional synchronization - creates missing directories
    /// in both OneDrive and vault. When false, only creates missing vault directories.
    /// Default is true for bidirectional synchronization to keep both locations in sync.
    /// </param>
    /// <param name="recursive">
    /// When true, scans subdirectories recursively to synchronize the entire directory tree.
    /// When false, only synchronizes the immediate children of the specified directory.
    /// Default is false for non-recursive operation (immediate children only).
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous synchronization operation.
    /// The task result contains:
    /// <list type="bullet">
    /// <item><description>Success status indicating overall operation completion</description></item>
    /// <item><description>Total count of directories processed</description></item>
    /// <item><description>Count of directories successfully synchronized</description></item>
    /// <item><description>Count of new directories created</description></item>
    /// <item><description>Count of directories skipped (already exist)</description></item>
    /// <item><description>Count of directories that failed to synchronize</description></item>
    /// <item><description>Error message for critical failures</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when oneDrivePath is null or empty, or when required configuration is missing.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when the source OneDrive directory does not exist or is inaccessible.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the application lacks permissions to read source or create target directories.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when file system operations fail due to disk space, locks, or other I/O issues.
    /// </exception>
    /// <example>
    /// <code>
    /// // Synchronize specific OneDrive folder to vault
    /// var processor = serviceProvider.GetService&lt;VaultFolderSyncProcessor&gt;();
    /// var result = await processor.SyncDirectoriesAsync(
    ///     "MBA/Finance",
    ///     @"C:\Users\user\Vault\MBA");
    ///
    /// if (result.Success)
    /// {
    ///     Console.WriteLine($"Synchronization completed successfully");
    ///     Console.WriteLine($"Processed: {result.SynchronizedFolders}/{result.TotalFolders}");
    ///     Console.WriteLine($"Created: {result.CreatedVaultFolders} new directories");
    /// }
    ///
    /// // Preview synchronization without making changes (recursive)
    /// var previewResult = await processor.SyncDirectoriesAsync(
    ///     "MBA/Finance",
    ///     vaultPath,
    ///     dryRun: true,
    ///     recursive: true);
    ///
    /// Console.WriteLine($"Would create {previewResult.CreatedVaultFolders} directories");
    ///
    /// // Use default vault path from configuration (non-recursive)
    /// var defaultResult = await processor.SyncDirectoriesAsync("MBA/Finance", null, recursive: false);
    /// </code>
    /// </example>
    public async Task<VaultFolderSyncResult> SyncDirectoriesAsync(
        string oneDrivePath,
        string? vaultPath,
        bool dryRun = false,
        bool bidirectional = true,
        bool recursive = false)
    {
        if (string.IsNullOrEmpty(oneDrivePath))
        {
            return CreateErrorResult("OneDrive path cannot be null or empty");
        }

        try
        {
            _logger.LogDebug($"Starting directory synchronization from OneDrive path: {oneDrivePath}");
            _logger.LogInformation("=== SYNCING DIRECTORIES ===");
            _logger.LogInformation($"OneDrive Path: {oneDrivePath}");
            _logger.LogInformation($"Vault Path: {vaultPath ?? "using default from config"}");

            // Use default vault path from configuration if not provided
            var targetVaultPath = vaultPath ?? _appConfig.Paths.NotebookVaultFullpathRoot;

            if (string.IsNullOrEmpty(targetVaultPath))
            {
                return CreateErrorResult("Cannot determine vault target path. Neither vaultPath parameter nor AppConfig.Paths.NotebookVaultFullpathRoot is provided.");
            }

            // Construct the full OneDrive source path using configuration
            var onedriveRoot = _appConfig.Paths.OnedriveFullpathRoot;
            var resourcesBasePath = _appConfig.Paths.OnedriveResourcesBasepath;

            if (string.IsNullOrEmpty(onedriveRoot))
            {
                return CreateErrorResult("OneDrive root path not configured. Please set paths.onedrive_fullpath_root in configuration.");
            }

            // Build the complete OneDrive source path
            var fullOneDriveSource = string.IsNullOrEmpty(resourcesBasePath)
                ? Path.Combine(onedriveRoot, oneDrivePath)
                : Path.Combine(onedriveRoot, resourcesBasePath, oneDrivePath);

            _logger.LogInformation($"Source: {fullOneDriveSource}");
            _logger.LogInformation($"Target: {targetVaultPath}");

            // Validate that the OneDrive source exists
            if (!Directory.Exists(fullOneDriveSource))
            {
                return CreateErrorResult($"OneDrive source directory does not exist: {fullOneDriveSource}");
            }

            if (dryRun)
            {
                _logger.LogInformation("DRY RUN: Simulating directory synchronization");
            }

            if (bidirectional)
            {
                _logger.LogInformation("BIDIRECTIONAL MODE: Synchronizing in both directions");
            }

            if (recursive)
            {
                _logger.LogInformation("RECURSIVE MODE: Processing subdirectories recursively");
            }
            else
            {
                _logger.LogInformation("NON-RECURSIVE MODE: Processing only immediate children");
            }

            var result = new VaultFolderSyncResult();
            var failedFolders = new List<string>();
            var createdVaultDirectories = new HashSet<string>();

            // Phase 1: OneDrive to Vault synchronization
            _logger.LogInformation("Phase 1: Synchronizing OneDrive directories to vault");
            await SyncOneDriveToVaultAsync(fullOneDriveSource, targetVaultPath, result, failedFolders, dryRun, recursive, createdVaultDirectories).ConfigureAwait(false);

            // Phase 2: Vault to OneDrive synchronization (if bidirectional)
            if (bidirectional)
            {
                _logger.LogInformation("Phase 2: Synchronizing vault directories to OneDrive");
                await SyncVaultToOneDriveAsync(targetVaultPath, fullOneDriveSource, result, failedFolders, dryRun, recursive, createdVaultDirectories).ConfigureAwait(false);
            }

            _logger.LogInformation($"Directory synchronization completed: {result.SynchronizedFolders}/{result.TotalFolders} synchronized, {result.CreatedVaultFolders} vault folders created, {result.CreatedOneDriveFolders} OneDrive folders created, {result.SkippedFolders} skipped, {result.FailedFolders} failed");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during directory synchronization: {ex.Message}");
            return CreateErrorResult($"Directory synchronization failed: {ex.Message}");
        }
    }


    /// <summary>
    /// Synchronizes directories from OneDrive to vault.
    /// </summary>
    /// <param name="oneDriveSource">The OneDrive source path.</param>
    /// <param name="vaultTarget">The vault target path.</param>
    /// <param name="result">The result object to update.</param>
    /// <param name="failedFolders">List to track failed folders.</param>
    /// <param name="dryRun">Whether this is a dry run.</param>
    /// <param name="recursive">Whether to scan subdirectories recursively.</param>
    /// <param name="createdVaultDirectories">Set to track directories created in vault during this sync.</param>
    private async Task SyncOneDriveToVaultAsync(
        string oneDriveSource,
        string vaultTarget,
        VaultFolderSyncResult result,
        List<string> failedFolders,
        bool dryRun,
        bool recursive,
        HashSet<string> createdVaultDirectories)
    {
        // Discover all directories in the OneDrive source
        var sourceDirectories = await DiscoverDirectoriesAsync(oneDriveSource, recursive).ConfigureAwait(false);
        result.TotalFolders += sourceDirectories.Count;

        _logger.LogInformation($"Found {sourceDirectories.Count} OneDrive directories to synchronize to vault");

        // Process each directory
        for (int i = 0; i < sourceDirectories.Count; i++)
        {
            var sourceDir = sourceDirectories[i];
            string relativePath = Path.GetRelativePath(oneDriveSource, sourceDir);
            string targetDir = Path.Combine(vaultTarget, relativePath);

            try
            {
                // Report progress
                OnProcessingProgressChanged(
                    sourceDir,
                    $"OneDrive→Vault: {i + 1}/{sourceDirectories.Count}: {Path.GetFileName(sourceDir)}",
                    i + 1,
                    sourceDirectories.Count);

                _logger.LogDebug($"Processing OneDrive directory: {sourceDir} -> {targetDir}");

                // Check if target directory already exists
                if (Directory.Exists(targetDir))
                {
                    result.SkippedFolders++;
                    result.SynchronizedFolders++;
                    _logger.LogDebug($"Vault directory already exists: {targetDir}");
                    continue;
                }

                if (dryRun)
                {
                    _logger.LogInformation($"DRY RUN: Would create vault directory: {targetDir}");
                    result.CreatedVaultFolders++;
                    result.SynchronizedFolders++;
                    createdVaultDirectories.Add(targetDir);
                }
                else
                {
                    // Create the target directory
                    Directory.CreateDirectory(targetDir);
                    result.CreatedVaultFolders++;
                    result.SynchronizedFolders++;
                    createdVaultDirectories.Add(targetDir);
                    _logger.LogInformation($"Created vault directory: {targetDir}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to synchronize OneDrive directory to vault: {sourceDir} -> {targetDir}");
                failedFolders.Add(sourceDir);
                result.FailedFolders++;
            }
        }
    }


    /// <summary>
    /// Synchronizes directories from vault to OneDrive (bidirectional mode).
    /// </summary>
    /// <param name="vaultSource">The vault source path.</param>
    /// <param name="oneDriveTarget">The OneDrive target path.</param>
    /// <param name="result">The result object to update.</param>
    /// <param name="failedFolders">List to track failed folders.</param>
    /// <param name="dryRun">Whether this is a dry run.</param>
    /// <param name="recursive">Whether to scan subdirectories recursively.</param>
    /// <param name="createdVaultDirectories">Set of directories created in vault during this sync to exclude from processing.</param>
    private async Task SyncVaultToOneDriveAsync(
        string vaultSource,
        string oneDriveTarget,
        VaultFolderSyncResult result,
        List<string> failedFolders,
        bool dryRun,
        bool recursive,
        HashSet<string> createdVaultDirectories)
    {
        // Discover all directories in the vault source
        var sourceDirectories = await DiscoverDirectoriesAsync(vaultSource, recursive).ConfigureAwait(false);
        var originalTotalFolders = result.TotalFolders;
        result.TotalFolders += sourceDirectories.Count;

        _logger.LogInformation($"Found {sourceDirectories.Count} vault directories to synchronize to OneDrive");

        // Process each directory
        for (int i = 0; i < sourceDirectories.Count; i++)
        {
            var sourceDir = sourceDirectories[i];
            string relativePath = Path.GetRelativePath(vaultSource, sourceDir);
            string targetDir = Path.Combine(oneDriveTarget, relativePath);

            try
            {
                // Report progress
                OnProcessingProgressChanged(
                    sourceDir,
                    $"Vault→OneDrive: {i + 1}/{sourceDirectories.Count}: {Path.GetFileName(sourceDir)}",
                    originalTotalFolders + i + 1,
                    result.TotalFolders);

                _logger.LogDebug($"Processing vault directory: {sourceDir} -> {targetDir}");

                // Skip directories that were created in vault during this sync to avoid circular sync
                if (createdVaultDirectories.Contains(sourceDir))
                {
                    _logger.LogDebug($"Skipping vault directory created in this sync: {sourceDir}");
                    continue;
                }

                // Check if target directory already exists
                if (Directory.Exists(targetDir))
                {
                    result.SkippedFolders++;
                    result.SynchronizedFolders++;
                    _logger.LogDebug($"OneDrive directory already exists: {targetDir}");
                    continue;
                }

                if (dryRun)
                {
                    _logger.LogInformation($"DRY RUN: Would create OneDrive directory: {targetDir}");
                    result.CreatedOneDriveFolders++;
                    result.SynchronizedFolders++;
                }
                else
                {
                    // Create the target directory
                    Directory.CreateDirectory(targetDir);
                    result.CreatedOneDriveFolders++;
                    result.SynchronizedFolders++;
                    _logger.LogInformation($"Created OneDrive directory: {targetDir}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to synchronize vault directory to OneDrive: {sourceDir} -> {targetDir}");
                failedFolders.Add(sourceDir);
                result.FailedFolders++;
            }
        }
    }


    /// <summary>
    /// Discovers all directories in the specified path.
    /// </summary>
    /// <remarks>
    /// This method performs a scan of the directory structure to identify
    /// all subdirectories that need to be synchronized. When recursive is true,
    /// it returns directories in depth-first order to ensure proper creation hierarchy.
    /// When recursive is false, it returns only immediate child directories.
    /// </remarks>
    /// <param name="path">The root path to scan for directories.</param>
    /// <param name="recursive">Whether to scan subdirectories recursively.</param>
    /// <returns>A list of all directory paths found, sorted for consistent processing order.</returns>
    private async Task<List<string>> DiscoverDirectoriesAsync(string path, bool recursive)
    {
        var directories = new List<string>();

        try
        {
            await Task.Run(() =>
            {
                // Get directories based on recursive flag
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var foundDirectories = Directory.GetDirectories(path, "*", searchOption);

                // Sort for consistent processing order
                directories.AddRange(foundDirectories.OrderBy(d => d));

                var searchMode = recursive ? "recursively" : "non-recursively";
                _logger.LogDebug($"Discovered {directories.Count} directories {searchMode} in {path}");
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error discovering directories in {path}");
            throw;
        }

        return directories;
    }


    /// <summary>
    /// Raises the ProcessingProgressChanged event.
    /// </summary>
    /// <param name="directoryPath">The path of the directory being processed.</param>
    /// <param name="status">The current processing status message.</param>
    /// <param name="currentDirectory">The current directory index being processed.</param>
    /// <param name="totalDirectories">The total number of directories to process.</param>
    protected virtual void OnProcessingProgressChanged(string directoryPath, string status, int currentDirectory, int totalDirectories)
    {
        ProcessingProgressChanged?.Invoke(this, new DocumentProcessingProgressEventArgs(directoryPath, status, currentDirectory, totalDirectories));
    }


    /// <summary>
    /// Creates a VaultFolderSyncResult indicating an error condition.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A VaultFolderSyncResult with Success set to false and the provided error message.</returns>
    private static VaultFolderSyncResult CreateErrorResult(string errorMessage)
    {
        return new VaultFolderSyncResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
