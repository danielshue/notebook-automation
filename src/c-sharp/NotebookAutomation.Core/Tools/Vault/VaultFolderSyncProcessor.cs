// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Core.Utils;

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
    AppConfig appConfig,
    IMarkdownNoteBuilder markdownNoteBuilder,
    IMetadataTemplateManager templateManager) : IVaultFolderSyncProcessor
{
    private readonly ILogger<VaultFolderSyncProcessor> _logger = logger;
    private readonly AppConfig _appConfig = appConfig;
    private readonly IMarkdownNoteBuilder _markdownNoteBuilder = markdownNoteBuilder;
    private readonly IMetadataTemplateManager _templateManager = templateManager;

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
        bool recursive = false,
        List<string>? documentTypes = null)
    {
        if (string.IsNullOrEmpty(oneDrivePath))
        {
            return CreateErrorResult("OneDrive path cannot be null or empty");
        }

        try
        {
            _logger.LogDebug($"Starting directory synchronization from OneDrive path: {oneDrivePath}");
            _logger.LogDebug("=== SYNCING DIRECTORIES ===");
            _logger.LogDebug($"OneDrive Path: {oneDrivePath}");
            _logger.LogDebug($"Vault Path: {vaultPath ?? "using default from config"}");

            // Use effective vault root (combined root + resources basepath) when vaultPath not provided
            var defaultVaultRoot = _appConfig.Paths.GetEffectiveVaultRoot();
            var targetVaultPath = vaultPath ?? defaultVaultRoot;

            if (string.IsNullOrEmpty(targetVaultPath))
            {
                return CreateErrorResult("Cannot determine vault target path. Neither vaultPath parameter nor effective vault root is provided.");
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

            _logger.LogDebug($"Source: {fullOneDriveSource}");
            _logger.LogDebug($"Target: {targetVaultPath}");

            // Validate that the OneDrive source exists
            if (!Directory.Exists(fullOneDriveSource))
            {
                return CreateErrorResult($"OneDrive source directory does not exist: {fullOneDriveSource}");
            }

            if (dryRun)
            {
                _logger.LogDebug("DRY RUN: Simulating directory synchronization");
            }

            if (bidirectional)
            {
                _logger.LogDebug("BIDIRECTIONAL MODE: Synchronizing in both directions");
            }

            if (recursive)
            {
                _logger.LogDebug("RECURSIVE MODE: Processing subdirectories recursively");
            }
            else
            {
                _logger.LogDebug("NON-RECURSIVE MODE: Processing only immediate children");
            }

            var result = new VaultFolderSyncResult();
            var failedFolders = new List<string>();
            var createdVaultDirectories = new HashSet<string>();

            // Phase 1: OneDrive to Vault synchronization
            _logger.LogDebug("Phase 1: Synchronizing OneDrive directories to vault");
            await SyncOneDriveToVaultAsync(fullOneDriveSource, targetVaultPath, result, failedFolders, dryRun, recursive, createdVaultDirectories).ConfigureAwait(false);

            // Phase 2: Vault to OneDrive synchronization (if bidirectional)
            if (bidirectional)
            {
                _logger.LogDebug("Phase 2: Synchronizing vault directories to OneDrive");
                await SyncVaultToOneDriveAsync(targetVaultPath, fullOneDriveSource, result, failedFolders, dryRun, recursive, createdVaultDirectories).ConfigureAwait(false);
            }

            // Phase 3: Create placeholder markdown files for document types (if requested)
            if (documentTypes?.Count > 0)
            {
                _logger.LogDebug("Phase 3: Creating placeholder markdown files for document types");
                await CreatePlaceholderMarkdownFilesAsync(fullOneDriveSource, targetVaultPath, result, documentTypes, dryRun, recursive).ConfigureAwait(false);
            }

            _logger.LogInformation($"Directory synchronization completed: {result.SynchronizedFolders}/{result.TotalFolders} synchronized, {result.CreatedVaultFolders} vault folders created, {result.CreatedOneDriveFolders} OneDrive folders created, {result.SkippedFolders} skipped, {result.FailedFolders} failed");
            if (documentTypes?.Count > 0)
            {
                _logger.LogInformation($"Created {result.CreatedPlaceholderFiles} placeholder markdown files");
            }

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

        _logger.LogDebug($"Found {sourceDirectories.Count} OneDrive directories to synchronize to vault");

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
                    _logger.LogDebug($"DRY RUN: Would create vault directory: {targetDir}");
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
                    _logger.LogDebug($"Created vault directory: {targetDir}");
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

        _logger.LogDebug($"Found {sourceDirectories.Count} vault directories to synchronize to OneDrive");

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
                    _logger.LogDebug($"DRY RUN: Would create OneDrive directory: {targetDir}");
                    result.CreatedOneDriveFolders++;
                    result.SynchronizedFolders++;
                }
                else
                {
                    // Create the target directory
                    Directory.CreateDirectory(targetDir);
                    result.CreatedOneDriveFolders++;
                    result.SynchronizedFolders++;
                    _logger.LogDebug($"Created OneDrive directory: {targetDir}");
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
    /// Creates placeholder markdown files for document types found in the OneDrive source.
    /// </summary>
    /// <param name="oneDriveSource">The OneDrive source path to scan for documents.</param>
    /// <param name="vaultTarget">The vault target path where placeholder files will be created.</param>
    /// <param name="result">The result object to update with placeholder file counts.</param>
    /// <param name="documentTypes">The list of document types to create placeholders for.</param>
    /// <param name="dryRun">Whether this is a dry run (don't actually create files).</param>
    /// <param name="recursive">Whether to scan subdirectories recursively.</param>
    private async Task CreatePlaceholderMarkdownFilesAsync(
        string oneDriveSource,
        string vaultTarget,
        VaultFolderSyncResult result,
        List<string> documentTypes,
        bool dryRun,
        bool recursive)
    {
        try
        {
            _logger.LogDebug($"Scanning for document files in: {oneDriveSource}");
            _logger.LogDebug($"Document types to process: {string.Join(", ", documentTypes)}");

            // Map document types to file extensions using configuration
            var extensionMap = GetExtensionsForDocumentTypes(documentTypes);
            _logger.LogDebug($"Extensions to scan: {string.Join(", ", extensionMap)}");

            if (extensionMap.Count == 0)
            {
                _logger.LogWarning("No valid document types specified or no extensions mapped");
                return;
            }

            // Get search option for directory scanning
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            int placeholderCount = 0;

            // Scan for each extension
            foreach (var extension in extensionMap)
            {
                var searchPattern = $"*{extension}";
                _logger.LogDebug($"Searching for files with pattern: {searchPattern}");

                if (!Directory.Exists(oneDriveSource))
                {
                    _logger.LogWarning($"OneDrive source directory does not exist: {oneDriveSource}");
                    continue;
                }

                var documentFiles = Directory.GetFiles(oneDriveSource, searchPattern, searchOption);
                _logger.LogDebug($"Found {documentFiles.Length} files with extension {extension}");

                foreach (var documentFile in documentFiles)
                {
                    try
                    {
                        var placeholderCreated = await CreatePlaceholderForDocumentAsync(
                            documentFile, oneDriveSource, vaultTarget, extension, dryRun);

                        if (placeholderCreated)
                        {
                            placeholderCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error creating placeholder for document: {documentFile}");
                    }
                }
            }

            result.CreatedPlaceholderFiles = placeholderCount;
            _logger.LogInformation($"Created {placeholderCount} placeholder markdown files");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during placeholder markdown file creation");
        }
    }

    /// <summary>
    /// Creates a placeholder markdown file for a specific document.
    /// </summary>
    /// <param name="documentPath">Full path to the document file.</param>
    /// <param name="oneDriveSource">The OneDrive source root path.</param>
    /// <param name="vaultTarget">The vault target root path.</param>
    /// <param name="extension">The file extension of the document.</param>
    /// <param name="dryRun">Whether this is a dry run.</param>
    /// <returns>True if placeholder was created, false otherwise.</returns>
    private async Task<bool> CreatePlaceholderForDocumentAsync(
        string documentPath,
        string oneDriveSource,
        string vaultTarget,
        string extension,
        bool dryRun)
    {
        try
        {
            // Calculate relative path from OneDrive source
            var relativePath = Path.GetRelativePath(oneDriveSource, documentPath);
            var relativeDir = Path.GetDirectoryName(relativePath) ?? "";
            var fileName = Path.GetFileNameWithoutExtension(documentPath);

            // Determine target directory in vault
            var targetDir = string.IsNullOrEmpty(relativeDir)
                ? vaultTarget
                : Path.Combine(vaultTarget, relativeDir);

            // Determine template type based on extension
            var templateType = GetTemplateTypeForExtension(extension);
            if (string.IsNullOrEmpty(templateType))
            {
                _logger.LogWarning($"No template type mapped for extension: {extension}");
                return false;
            }

            // Create markdown filename with appropriate suffix based on content type
            var contentTypeSuffix = GetContentTypeSuffix(templateType);
            var markdownFileName = $"{fileName}{contentTypeSuffix}.md";
            var markdownPath = Path.Combine(targetDir, markdownFileName);

            // Check if markdown file already exists - if so, skip it
            if (File.Exists(markdownPath))
            {
                _logger.LogDebug($"Markdown file already exists, skipping: {markdownPath}");
                return false; // Not created, already exists
            }
            if (string.IsNullOrEmpty(templateType))
            {
                _logger.LogWarning($"No template type mapped for extension: {extension}");
                return false;
            }

            _logger.LogDebug($"Creating placeholder for {documentPath} -> {markdownPath} (template: {templateType})");

            if (dryRun)
            {
                _logger.LogInformation($"[DRY RUN] Would create placeholder: {markdownPath}");
                return true;
            }

            // Ensure target directory exists
            Directory.CreateDirectory(targetDir);

            // Create context for template resolution
            var context = new Dictionary<string, object>
            {
                ["title"] = fileName,
                ["source_file"] = documentPath,
                ["filePath"] = markdownPath,  // Use vault target path for hierarchy detection (camelCase for resolvers)
                ["relative_path"] = relativePath,
                ["target_directory"] = targetDir,
                ["skip_onedrive_share_link"] = true  // Skip OneDriveShareLinkResolver for placeholder creation
            };

            // Get template metadata using the template system with resolvers
            var templateMetadata = _templateManager.GetTemplate(templateType);
            if (templateMetadata == null)
            {
                _logger.LogWarning($"No template found for type: {templateType}");
                return false;
            }

            // Enhance template metadata with resolved field values using context
            // Note: OneDriveShareLinkResolver should check for skip_onedrive_share_link flag
            var metadata = _templateManager.ResolveTemplateFields(templateType, context);

            // Overlay template defaults with resolved values
            foreach (var kvp in templateMetadata)
            {
                if (!metadata.ContainsKey(kvp.Key))
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }

            // Generate friendly title using FriendlyTitleHelper
            var friendlyTitle = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);

            // Override specific fields for placeholder creation
            metadata["title"] = friendlyTitle;
            metadata["template-type"] = templateType;
            metadata["auto-generated-state"] = "pending";
            metadata["created"] = DateTime.UtcNow.ToString("yyyy-MM-dd");
            metadata["type"] = GetTypeForTemplateType(templateType);

            // Add OneDrive relative path (from resources root to document)
            var resourcesRoot = string.IsNullOrEmpty(_appConfig.Paths.OnedriveResourcesBasepath)
                ? _appConfig.Paths.OnedriveFullpathRoot
                : Path.Combine(_appConfig.Paths.OnedriveFullpathRoot, _appConfig.Paths.OnedriveResourcesBasepath);
            var oneDriveRelativePath = Path.GetRelativePath(resourcesRoot, documentPath).Replace('\\', '/');
            metadata["onedrive_relative_path"] = oneDriveRelativePath;

            // For videos, also add transcript OneDrive path
            if (templateType == "video-reference")
            {
                var transcriptPath = Path.ChangeExtension(oneDriveRelativePath, ".txt");
                metadata["transcript-onedrive-relative-path"] = transcriptPath;
            }

            // Remove source_file metadata since onedrive_relative_path provides this info
            metadata.Remove("source_file");

            // Remove share-link field if it was resolved, as we want to skip it for placeholders
            metadata.Remove("share-link");
            metadata.Remove("onedrive-shared-link");  // Remove canonical field name as well

            // Create markdown content with frontmatter and body structure
            var markdownWithFrontmatter = _markdownNoteBuilder.CreateMarkdownWithFrontmatter(metadata, markdownFileName);

            // Add friendly title heading and Notes section (following DocumentNoteProcessorBase pattern)
            var markdownContent = markdownWithFrontmatter + $"# {friendlyTitle}\n\n## Notes\n";

            // Write the placeholder file
            await File.WriteAllTextAsync(markdownPath, markdownContent);
            _logger.LogDebug($"Created placeholder file: {markdownPath}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating placeholder for {documentPath}");
            return false;
        }
    }

    /// <summary>
    /// Maps document type names to file extensions using configuration.
    /// </summary>
    /// <param name="documentTypes">List of document type names.</param>
    /// <returns>List of file extensions to search for.</returns>
    private List<string> GetExtensionsForDocumentTypes(List<string> documentTypes)
    {
        var extensions = new List<string>();

        foreach (var docType in documentTypes)
        {
            switch (docType.ToLowerInvariant())
            {
                case "videos":
                case "video":
                    if (_appConfig.VideoExtensions?.Count > 0)
                    {
                        _logger.LogDebug($"Using video extensions from config: {string.Join(", ", _appConfig.VideoExtensions)}");
                        extensions.AddRange(_appConfig.VideoExtensions);
                    }
                    else
                    {
                        _logger.LogDebug($"Video extensions from config is null or empty (Count: {_appConfig.VideoExtensions?.Count ?? -1}), using fallback");
                        // Fallback to default video extensions
                        extensions.AddRange([".mp4", ".mov", ".avi", ".mkv", ".wmv", ".flv"]);
                    }
                    break;
                case "pdf":
                case "pdfs":
                    if (_appConfig.PdfExtensions?.Count > 0)
                    {
                        _logger.LogDebug($"Using PDF extensions from config: {string.Join(", ", _appConfig.PdfExtensions)}");
                        extensions.AddRange(_appConfig.PdfExtensions);
                    }
                    else
                    {
                        _logger.LogDebug($"PDF extensions from config is null or empty (Count: {_appConfig.PdfExtensions?.Count ?? -1}), using fallback");
                        // Fallback to default PDF extension
                        extensions.Add(".pdf");
                    }
                    break;
                case "html":
                case "htm":
                    if (_appConfig.HtmlExtensions?.Count > 0)
                    {
                        _logger.LogDebug($"Using HTML extensions from config: {string.Join(", ", _appConfig.HtmlExtensions)}");
                        extensions.AddRange(_appConfig.HtmlExtensions);
                    }
                    else
                    {
                        _logger.LogDebug($"HTML extensions from config is null or empty (Count: {_appConfig.HtmlExtensions?.Count ?? -1}), using fallback");
                        // Fallback to default HTML extensions
                        extensions.AddRange([".html", ".htm", ".epub"]);
                    }
                    break;
                default:
                    // Log unknown document type but continue processing
                    break;
            }
        }

        return extensions.Distinct().ToList();
    }

    /// <summary>
    /// Gets the template type for a given file extension.
    /// </summary>
    /// <param name="extension">The file extension (with dot).</param>
    /// <returns>The template type string or empty if not mapped.</returns>
    private static string GetTemplateTypeForExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".mp4" or ".mov" or ".avi" or ".mkv" or ".wmv" or ".flv" => "video-reference",
            ".pdf" => "pdf-reference",
            ".html" or ".htm" => "resource-reading", // HTML files are typically reading materials
            _ => ""
        };
    }

    /// <summary>
    /// Gets the document type for a given template type.
    /// </summary>
    /// <param name="templateType">The template type string.</param>
    /// <returns>The document type string.</returns>
    private static string GetTypeForTemplateType(string templateType)
    {
        return templateType switch
        {
            "video-reference" => "note/video-note",
            "pdf-reference" => "note/case-study",
            "resource-reading" => "note/reading",
            _ => "note/general"
        };
    }

    /// <summary>
    /// Gets the content type suffix for placeholder file naming.
    /// </summary>
    /// <param name="templateType">The template type string.</param>
    /// <returns>The suffix to add to the filename (e.g., "-video", "-pdf").</returns>
    private static string GetContentTypeSuffix(string templateType)
    {
        return templateType switch
        {
            "video-reference" => "-video",
            "pdf-reference" => "-pdf",
            "resource-reading" => "-reading",
            _ => ""
        };
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
