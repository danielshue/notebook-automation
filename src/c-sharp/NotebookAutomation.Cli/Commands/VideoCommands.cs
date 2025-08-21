// Licensed under the MIT License. See LICENSE file in the project root for full license information.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace NotebookAutomation.Cli.Commands;

/// <summary>
/// Provides CLI commands for processing video files and generating markdown notes within the Notebook Automation CLI.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="VideoCommands"/> class is responsible for registering and handling the <c>video-notes</c> command,
/// which enables users to process video files or directories, extract metadata, and generate markdown notes with
/// appropriate frontmatter and metadata. The command supports advanced features such as:
/// </para>
/// <list type="bullet">
///   <item>Video file discovery and filtering (by extension, directory, or file)</item>
///   <item>Metadata extraction (duration, resolution, codec, etc.)</item>
///   <item>Markdown note generation with YAML frontmatter and references to the original video</item>
///   <item>Integration with the Core library for video processing and batch operations</item>
///   <item>OneDrive integration for resource path resolution and share link creation</item>
///   <item>Customizable output directories and vault root overrides</item>
///   <item>Retrying failed files, force overwrite, and dry-run simulation</item>
///   <item>Comprehensive logging, progress reporting, and AI-powered summary generation</item>
/// </list>
/// <para>
/// The class leverages dependency injection for configuration, logging, and service resolution, and ensures
/// robust cross-platform path handling by combining OneDrive root and resource base paths as needed.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code language="shell">
/// na.exe video-notes --path "C:\Users\me\Videos" --overwrite-output-dir "C:\Notes"
/// </code>
/// </para>
/// <para>
/// <b>Example: Registering the command</b>
/// <code language="csharp">
/// var videoCommands = new VideoCommands(logger);
/// videoCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
/// </code>
/// </para>
/// <para>
/// For details on path resolution and configuration, see <see cref="PathUtils.ResolveInputPath(string, string?, string?)"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var rootCommand = new RootCommand();
/// var logger = new LoggerFactory().CreateLogger<VideoCommands>();
/// var videoCommands = new VideoCommands(logger);
/// videoCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
/// rootCommand.Invoke("video-notes --path videos --output notes");
/// </code>
/// </example>
internal class VideoCommands
{
    private readonly ILogger<VideoCommands> logger;


    /// <summary>
    /// Initializes a new instance of the <see cref="VideoCommands"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic and operational logging.</param>
    /// <remarks>
    /// The constructor sets up the logger and logs initialization for debugging purposes.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is <c>null</c>.</exception>
    public VideoCommands(ILogger<VideoCommands> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.logger.LogDebug("Video command initialized");
    }

    /// <summary>
    /// Registers the <c>video-notes</c> command with the specified root command, enabling CLI video processing.
    /// </summary>
    /// <param name="rootCommand">The root command to which the <c>video-notes</c> command will be added.</param>
    /// <param name="configOption">The global config file option for specifying the configuration file path.</param>
    /// <param name="debugOption">The global debug option to enable debug logging.</param>
    /// <param name="verboseOption">The global verbose output option for detailed output.</param>
    /// <param name="dryRunOption">The global dry run option to simulate actions without making changes.</param>
    /// <remarks>
    /// <para>
    /// This method adds the <c>video-notes</c> command to the root command, allowing users to process video files or directories,
    /// extract metadata, and generate markdown notes. It defines options for input, output, vault root overrides, OneDrive integration,
    /// summary generation, retrying failed files, force overwrite, API timeout, and more. The method configures dependency injection,
    /// resolves paths using <see cref="PathUtils.ResolveInputPath(string, string?, string?)"/>, and sets up the processing pipeline.
    /// </para>
    /// <para>
    /// The command handler performs the following steps:
    /// <list type="number">
    ///   <item>Validates and resolves input and output paths, including OneDrive and resource base paths.</item>
    ///   <item>Initializes dependency injection and retrieves required services.</item>
    ///   <item>Configures OneDrive vault roots and handles authentication refresh if requested.</item>
    ///   <item>Outputs configuration and environment details in debug/verbose mode.</item>
    ///   <item>Validates AI/OpenAI configuration and retrieves API keys.</item>
    ///   <item>Processes video files or directories, reporting progress and handling errors.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Example CLI Usage:</b>
    /// <code language="shell">
    /// na.exe video-notes --path "C:\Users\me\Videos" --overwrite-output-dir "C:\Notes" --no-summary
    /// </code>
    /// </para>
    /// <para>
    /// <b>Example C# Usage:</b>
    /// <code language="csharp">
    /// var videoCommands = new VideoCommands(logger);
    /// videoCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="rootCommand"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if dependency injection setup fails or required services are missing.</exception>
    /// <example>
    /// <code>
    /// var rootCommand = new RootCommand();
    /// var logger = new LoggerFactory().CreateLogger<VideoCommands>();
    /// var videoCommands = new VideoCommands(logger);
    /// videoCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
    /// rootCommand.Invoke("video-notes --path videos --output notes");
    /// </code>
    /// </example>
    public void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
    {
        var pathOption = new Option<string?>(
            aliases: ["--path", "-p"],
            description: "Path to the video file or directory (will auto-detect if it's a file or folder)")
        {
            IsRequired = true,
        };
        var outputOption = new Option<string>(
            aliases: ["--overwrite-output-dir", "-o"],
            description: "Override the default output directory (normally uses notebook_vault_fullpath_root from config)");

        var vaultRootOverrideOption = new Option<string?>(
            aliases: ["--override-vault-root"],
            description: "Specify the explicit vault root path (overrides the config)");

        var resourcesRootOption = new Option<string?>(
            aliases: ["--onedrive-fullpath-root"],
            description: "Override OneDrive fullpath root directory");
        var noSummaryOption = new Option<bool>(
            aliases: ["--no-summary"],
            description: "Skip summary generation (summary is generated by default)");
        var retryFailedOption = new Option<bool>(
            aliases: ["--retry-failed"],
            description: "Retry only failed files from previous run");
        var forceOption = new Option<bool>(
            aliases: ["--force"],
            description: "Overwrite existing notes");
        var timeoutOption = new Option<int?>(
            aliases: ["--timeout"],
            description: "Set API request timeout (seconds)");
        var refreshAuthOption = new Option<bool>(
            aliases: ["--refresh-auth"],
            description: "Force refresh Microsoft Graph API authentication");
        var noShareLinksOption = new Option<bool>(
            aliases: ["--no-share-links"],
            description: "Skip OneDrive share link creation (links are created by default)");
        var videoCommand = new Command("video-notes", "Video notes and metadata commands");

        videoCommand.AddOption(pathOption);
        videoCommand.AddOption(outputOption);
        videoCommand.AddOption(vaultRootOverrideOption);
        videoCommand.AddOption(resourcesRootOption);
        videoCommand.AddOption(noSummaryOption);
        videoCommand.AddOption(retryFailedOption);
        videoCommand.AddOption(forceOption);
        videoCommand.AddOption(timeoutOption);
        videoCommand.AddOption(refreshAuthOption);
        videoCommand.AddOption(noShareLinksOption);
        videoCommand.SetHandler(async context =>
        {
            string? input = context.ParseResult.GetValueForOption(pathOption);
            string? overrideOutputDir = context.ParseResult.GetValueForOption(outputOption);
            string? vaultRootOverride = context.ParseResult.GetValueForOption(vaultRootOverrideOption);
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            string? resourcesRoot = context.ParseResult.GetValueForOption(resourcesRootOption);
            bool noSummary = context.ParseResult.GetValueForOption(noSummaryOption);
            bool retryFailed = context.ParseResult.GetValueForOption(retryFailedOption);
            bool force = context.ParseResult.GetValueForOption(forceOption);
            int? timeout = context.ParseResult.GetValueForOption(timeoutOption);
            bool refreshAuth = context.ParseResult.GetValueForOption(refreshAuthOption);
            bool noShareLinks = context.ParseResult.GetValueForOption(noShareLinksOption);

            // Print usage/help if required argument is missing
            if (string.IsNullOrEmpty(input))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation video-notes --path <file|dir> [options]",
                    videoCommand.Description ?? string.Empty,
                    string.Join("\n", videoCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }            // Initialize dependency injection if needed
            if (Program.ServiceProvider == null)
            {
                if (config != null && !File.Exists(config))
                {
                    AnsiConsoleHelper.WriteError($"Configuration file not found: {config}");
                    return;
                }

                Program.SetupDependencyInjection(config, debug);
            }            // Use DI container to get services and create scoped context for vault root override
            var serviceProvider = Program.ServiceProvider;
            if (serviceProvider == null)
            {
                AnsiConsoleHelper.WriteError("Failed to initialize dependency injection. Service provider is null.");
                return;
            }

            using var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            var loggerFactory = scopedServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("VideoCommands");
            var loggingService = scopedServices.GetRequiredService<LoggingService>();
            var appConfig = scopedServices.GetRequiredService<AppConfig>();

            // Determine effective combined vault root (root + resources basepath if configured)
            var effectiveVaultRoot = appConfig.Paths?.GetEffectiveVaultRoot();
            logger.LogDebug("Resolved effective vault root: {effectiveVaultRoot} (raw: {rawRoot}, basepath: {basepath})",
                effectiveVaultRoot,
                appConfig.Paths?.NotebookVaultFullpathRoot,
                appConfig.Paths?.NotebookVaultResourcesBasepath);
            // Determine effective output directory for vault root context
            string effectiveOutputDir = overrideOutputDir
                                        ?? effectiveVaultRoot
                                        ?? appConfig.Paths?.NotebookVaultFullpathRoot
                                        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Generated");

            if (string.IsNullOrWhiteSpace(effectiveOutputDir))
            {
                effectiveOutputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Generated");
            }

            effectiveOutputDir = Path.GetFullPath(effectiveOutputDir);
            // Set up vault root override in scoped context
            var vaultRootContext = scopedServices.GetRequiredService<VaultRootContextService>();
            // Use explicit vault root override if provided, otherwise use effective vault root (fallback to output dir)
            string vaultRootForHierarchy = vaultRootOverride
                                           ?? effectiveVaultRoot
                                           ?? appConfig.Paths?.NotebookVaultFullpathRoot
                                           ?? effectiveOutputDir;
            vaultRootContext.VaultRootOverride = vaultRootForHierarchy;
            logger.LogInformation("Using vault root override for metadata hierarchy: {vaultRootForHierarchy}", vaultRootForHierarchy);
            var batchProcessor = scopedServices.GetRequiredService<VideoNoteBatchProcessor>();

            // Handle refresh auth flag - set force refresh on OneDriveService if requested
            if (refreshAuth)
            {
                try
                {
                    var oneDriveService = scopedServices.GetService<IOneDriveService>();
                    if (oneDriveService != null)
                    {
                        oneDriveService.SetForceRefresh(true);
                        AnsiConsoleHelper.WriteInfo("Force refresh authentication enabled for OneDrive");
                    }
                    else
                    {
                        logger.LogWarning("OneDrive service not available - --refresh-auth flag ignored");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandler.HandleException(ex, "setting force refresh on OneDrive service");
                }
            }
            // Determine effective resources root (prioritize command line over config)
            string? effectiveResourcesRoot = resourcesRoot;
            if (string.IsNullOrWhiteSpace(effectiveResourcesRoot) && appConfig?.Paths != null)
            {
                // Use the proper helper method to get the effective OneDrive root (fullpath + basepath)
                effectiveResourcesRoot = appConfig.Paths.GetEffectiveOneDriveRoot();
            }

            // Check if input is a markdown file - if so, extract onedrive_relative_path from frontmatter
            string inputForProcessing = input;
            string? actualMarkdownFilePath = null;

            if (input.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                // First, try the input as an absolute path
                if (File.Exists(input))
                {
                    actualMarkdownFilePath = input;
                }
                // If not found as absolute path, try to find it in the vault structure
                else if (!string.IsNullOrWhiteSpace(effectiveVaultRoot))
                {
                    // Try different possible locations in the vault
                    var vaultRoot = appConfig?.Paths?.NotebookVaultFullpathRoot;
                    var possiblePaths = new[]
                    {
                        Path.Combine(effectiveVaultRoot, input.Replace('/', '\\')),
                        Path.Combine(effectiveVaultRoot, Path.GetFileName(input))
                    };

                    // Add vault root path if available and different from effective vault root
                    if (!string.IsNullOrWhiteSpace(vaultRoot) && vaultRoot != effectiveVaultRoot)
                    {
                        possiblePaths = possiblePaths.Concat(new[] { Path.Combine(vaultRoot, input.Replace('/', '\\')) }).ToArray();
                    }

                    foreach (var possiblePath in possiblePaths)
                    {
                        if (File.Exists(possiblePath))
                        {
                            actualMarkdownFilePath = possiblePath;
                            logger.LogDebug($"Found Document Placeholder file: '{possiblePath}' (from input: '{input}')");
                            break;
                        }
                    }
                }

                // If we found a markdown file, process its frontmatter
                if (!string.IsNullOrWhiteSpace(actualMarkdownFilePath))
                {
                    try
                    {
                        var yamlHelper = new YamlHelper(logger);
                        var markdownContent = await File.ReadAllTextAsync(actualMarkdownFilePath);
                        var frontmatter = yamlHelper.ExtractFrontmatter(markdownContent);

                        if (!string.IsNullOrWhiteSpace(frontmatter))
                        {
                            var metadata = yamlHelper.ParseYamlToDictionary(frontmatter);

                            // Check if this is a video-related placeholder by examining template-type
                            bool isVideoRelated = false;
                            if (metadata.TryGetValue("template-type", out var templateTypeObj) &&
                                templateTypeObj is string templateType)
                            {
                                // Check if this is a video-related template type
                                isVideoRelated = templateType.Contains("video", StringComparison.OrdinalIgnoreCase) ||
                                               templateType.Contains("Video", StringComparison.OrdinalIgnoreCase);

                                if (!isVideoRelated)
                                {
                                    logger.LogError($"Document placeholder has template-type '{templateType}' which is not video-related. Use the appropriate processor for this template type.");
                                    context.ExitCode = 1;
                                    return;
                                }

                                logger.LogDebug($"Confirmed video-related template-type: '{templateType}'");
                            }
                            else
                            {
                                logger.LogWarning($"Document placeholder does not have a template-type field. Proceeding with video processing.");
                            }

                            // Check if this is a pending file and log it for user awareness
                            if (metadata.TryGetValue("auto-generated-state", out var autoGenStateObj) &&
                                autoGenStateObj is string autoGenState &&
                                autoGenState.Equals("pending", StringComparison.OrdinalIgnoreCase))
                            {
                                logger.LogDebug($"Detected pending file: '{input}' - will auto-process without --force and preserve content after ## Notes");
                            }
                            // Legacy check for old "status" field (for backward compatibility)
                            else if (metadata.TryGetValue("status", out var statusObj) &&
                                statusObj is string status &&
                                status.Equals("placeholder", StringComparison.OrdinalIgnoreCase))
                            {
                                logger.LogDebug($"Detected legacy placeholder file: '{input}' - will auto-process without --force and preserve content after ## Notes");
                            }

                            // Extract onedrive_relative_path for processing
                            if (metadata.TryGetValue("onedrive_relative_path", out var relativePathObj) &&
                                relativePathObj is string relativePath &&
                                !string.IsNullOrWhiteSpace(relativePath))
                            {
                                inputForProcessing = relativePath;
                                logger.LogDebug($"Extracted onedrive_relative_path from markdown file: '{input}' -> '{inputForProcessing}'");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, $"Failed to extract onedrive_relative_path from markdown file '{actualMarkdownFilePath ?? input}', using original path");
                    }
                }
            }

            // Resolve input path - prioritize vault root for relative paths, fall back to OneDrive root
            string resolvedInput;
            if (!Path.IsPathRooted(inputForProcessing) && !string.IsNullOrWhiteSpace(effectiveVaultRoot))
            {
                // For relative paths when we have a vault root, try vault root first
                var vaultBasedPath = Path.Combine(effectiveVaultRoot, inputForProcessing);
                if (File.Exists(vaultBasedPath))
                {
                    resolvedInput = PathUtils.NormalizePath(vaultBasedPath);
                    logger.LogDebug($"Input path resolution: '{inputForProcessing}' -> '{resolvedInput}' (vault root: {effectiveVaultRoot})");
                }
                else
                {
                    // Fall back to OneDrive root if file not found in vault
                    resolvedInput = PathUtils.ResolveInputPath(inputForProcessing, effectiveResourcesRoot);
                    logger.LogDebug($"Input path resolution: '{inputForProcessing}' -> '{resolvedInput}' (OneDrive root fallback: {effectiveResourcesRoot ?? "(none)"})");
                }
            }
            else
            {
                // For absolute paths or when no vault root, use OneDrive root resolution
                resolvedInput = PathUtils.ResolveInputPath(inputForProcessing, effectiveResourcesRoot);
                logger.LogDebug($"Input path resolution: '{inputForProcessing}' -> '{resolvedInput}' (OneDrive root: {effectiveResourcesRoot ?? "(none)"})");
            }

            // If input was a markdown file, validate that the resolved path points to a video file
            if (input.EndsWith(".md", StringComparison.OrdinalIgnoreCase) && inputForProcessing != input)
            {
                if (!File.Exists(resolvedInput))
                {
                    logger.LogError($"Video file referenced in document placeholder does not exist: '{resolvedInput}' (from onedrive_relative_path: '{inputForProcessing}')");
                    context.ExitCode = 1;
                    return;
                }

                // Check if the resolved file is actually a video file
                var supportedVideoExtensions = appConfig?.VideoExtensions ?? [".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv", ".mpg", ".mpeg", ".m4v"];
                var fileExtension = Path.GetExtension(resolvedInput);
                if (!supportedVideoExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
                {
                    logger.LogError($"Document placeholder references a non-video file: '{resolvedInput}' (extension: '{fileExtension}'). Expected video extensions: {string.Join(", ", supportedVideoExtensions)}");
                    context.ExitCode = 1;
                    return;
                }

                logger.LogInformation($"Processing video file '{resolvedInput}' referenced by document placeholder '{input}'");
            }
            // Build the full local resources path for path calculations
            string? localResourcesPathForBatchProcessor = null;

            // Configure OneDriveService with vault roots if available
            if (!string.IsNullOrWhiteSpace(effectiveResourcesRoot) && appConfig?.Paths != null)
            {
                try
                {
                    var oneDriveService = scopedServices.GetService<IOneDriveService>();
                    if (oneDriveService != null && !string.IsNullOrWhiteSpace(appConfig.Paths.OnedriveResourcesBasepath))
                    {
                        // Since effectiveResourcesRoot already contains the complete path (fullpath + basepath),
                        // use it directly as the local resources path
                        var localResourcesPath = Path.GetFullPath(effectiveResourcesRoot);

                        // Store this for batch processor to use for path calculations
                        localResourcesPathForBatchProcessor = localResourcesPath;

                        // Configure vault roots: local resources folder -> OneDrive resources path
                        oneDriveService.ConfigureVaultRoots(localResourcesPath, appConfig.Paths.OnedriveResourcesBasepath);
                        logger.LogDebug($"Configured OneDrive vault roots - Local: {localResourcesPath}, OneDrive: {appConfig.Paths.OnedriveResourcesBasepath}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to configure OneDrive vault roots");
                }
            }

            // Output the configured settings before processing (only in debug or verbose mode)
            if (debug || verbose)
            {
                AnsiConsoleHelper.WriteInfo($"Configured settings:");
                AnsiConsoleHelper.WriteInfo($"  Debug: {debug}");
                AnsiConsoleHelper.WriteInfo($"  Config file: {config}");
                AnsiConsoleHelper.WriteInfo($"  Input: {input}");
                AnsiConsoleHelper.WriteInfo($"  Output directory: {overrideOutputDir ?? "(default)"}");
                AnsiConsoleHelper.WriteInfo($"  Dry run: {dryRun}");
                AnsiConsoleHelper.WriteInfo($"  Skip summary: {noSummary}");
                AnsiConsoleHelper.WriteInfo($"  Force overwrite: {force}");
                AnsiConsoleHelper.WriteInfo($"  Retry failed: {retryFailed}");
                AnsiConsoleHelper.WriteInfo($"  Timeout: {(timeout.HasValue ? timeout.Value.ToString() : "(default)")}");
                AnsiConsoleHelper.WriteInfo($"  OneDrive fullpath root: {effectiveResourcesRoot ?? "(not configured)"}");
                AnsiConsoleHelper.WriteInfo($"  Config OneDrive root: {appConfig?.Paths?.OnedriveFullpathRoot ?? "(not set)"}");
                AnsiConsoleHelper.WriteInfo($"  Skip share links: {noShareLinks}");
                AnsiConsoleHelper.WriteInfo($"  Video extensions: {string.Join(", ", appConfig?.VideoExtensions ?? [])}");

                // Display more detailed AI service configuration
                AnsiConsoleHelper.WriteInfo($"  AI Provider: {appConfig?.AiService?.Provider ?? "openai"}");

                string selectedModel = "(not set)";
                string selectedEndpoint = "(not set)";

                switch (appConfig?.AiService?.Provider?.ToLowerInvariant())
                {
                    case "azure":
                        selectedModel = appConfig?.AiService?.Azure?.Model ?? "(not set)";
                        selectedEndpoint = appConfig?.AiService?.Azure?.Endpoint ?? "(not set)";
                        AnsiConsoleHelper.WriteInfo($"  AI Model: {selectedModel}");
                        AnsiConsoleHelper.WriteInfo($"  AI Deployment: {appConfig?.AiService?.Azure?.Deployment ?? "(not set)"}");
                        AnsiConsoleHelper.WriteInfo($"  AI Endpoint: {selectedEndpoint}");
                        break;

                    case "foundry":
                        selectedModel = appConfig?.AiService?.Foundry?.Model ?? "(not set)";
                        selectedEndpoint = appConfig?.AiService?.Foundry?.Endpoint ?? "(not set)";
                        AnsiConsoleHelper.WriteInfo($"  AI Model: {selectedModel}");
                        AnsiConsoleHelper.WriteInfo($"  AI Endpoint: {selectedEndpoint}");
                        break;

                    case "openai":
                    default:
                        selectedModel = appConfig?.AiService?.OpenAI?.Model ?? "(not set)";
                        selectedEndpoint = appConfig?.AiService?.OpenAI?.Endpoint ?? "https://api.openai.com/v1/chat/completions";
                        AnsiConsoleHelper.WriteInfo($"  AI Model: {selectedModel}");
                        AnsiConsoleHelper.WriteInfo($"  AI Endpoint: {selectedEndpoint}");
                        break;
                }

                // Display API key status (without revealing the key)
                string? apiKey = appConfig?.AiService?.GetApiKey();
                AnsiConsoleHelper.WriteInfo($"  API Key: {(string.IsNullOrEmpty(apiKey) ? "Not configured" : "Configured")}"); AnsiConsoleHelper.WriteInfo($"  Logging Dir: {appConfig?.Paths?.LoggingDir}");
            }

            // Validate OpenAI config before proceeding
            if (appConfig == null || !await ConfigValidation.RequireOpenAi(appConfig))
            {
                logger.LogError("OpenAI configuration is missing or incomplete. Exiting.");
                return;
            }

            // Get video extensions from config
            var videoExtensions = appConfig.VideoExtensions ?? [".mp4", ".mov", ".avi", ".mkv", ".webm"];

            // Get OpenAI API key from environment or config
            string? openAiApiKey = appConfig.AiService?.GetApiKey();

            // Process videos            // Verify that we have an input source
            if (string.IsNullOrWhiteSpace(input))
            {
                logger.LogError("Path is required. Use --path/-p to specify a video file or folder.");
                return;
            }

            // Auto-detect if input is a file or folder using resolved path
            bool isFile = File.Exists(resolvedInput);
            bool isDirectory = Directory.Exists(resolvedInput);

            if (!isFile && !isDirectory)
            {
                logger.LogError("Input path does not exist or is not accessible: {ResolvedPath} (original: {OriginalPath})", resolvedInput, input);
                return;
            }

            logger.LogInformation(
                "Processing {Type}: {Path}",
                isFile ? "file" : "directory",
                resolvedInput);
            logger.LogDebug($"Output will be written to: {overrideOutputDir ?? effectiveVaultRoot ?? appConfig.Paths?.NotebookVaultFullpathRoot ?? "Generated"}");

            try
            {
                // Use the newer Spectre.Console status display with live updates
                var result = await AnsiConsoleHelper.WithStatusAsync(
                    async (updateStatus) =>
                    { // Hook up progress events to update the status
                        batchProcessor.ProcessingProgressChanged += (sender, e) =>
                        {
                            // Escape any markup to avoid Spectre.Console parsing issues
                            string safeStatus = e.Status.Replace("[", "[[").Replace("]", "]]");

                            // The status already contains file count information, so we don't need to add it
                            updateStatus(safeStatus);
                        }; return await batchProcessor.ProcessVideosAsync(
                            resolvedInput,
                            effectiveOutputDir,
                            videoExtensions,
                            openAiApiKey,
                            dryRun,
                            noSummary,
                            force,
                            retryFailed,
                            timeout,
                            localResourcesPathForBatchProcessor,
                            appConfig,
                            noShareLinks).ConfigureAwait(false);
                    },
                    $"Processing video files from {(isFile ? "file" : "directory")}: {resolvedInput}").ConfigureAwait(false);

                logger.LogInformation($"Video processing completed. Success: {result.Processed}, Failed: {result.Failed}");
                if (!string.IsNullOrWhiteSpace(result.Summary))
                {
                    AnsiConsoleHelper.WriteInfo(result.Summary);
                }
            }
            catch (Exception ex)
            {
                // No need to stop spinner manually, WithStatusAsync handles this
                AnsiConsoleHelper.WriteError($"Error processing video files: {ex.Message}");
                logger.LogError(ex, "Error processing video files");
            }
        });

        rootCommand.AddCommand(videoCommand);
    }
}
