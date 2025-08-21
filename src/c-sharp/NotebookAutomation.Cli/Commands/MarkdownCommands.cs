// Licensed under the MIT License. See LICENSE file in the project root for full license information.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace NotebookAutomation.Cli.Commands;

/// <summary>
/// Provides CLI commands for generating markdown notes from various source formats.
/// </summary>
/// <remarks>
/// <para>
/// This class registers the 'generate-markdown' command, which converts HTML, TXT, and EPUB files
/// to markdown format using simple HTML stripping and text conversion. It supports:
/// <list type="bullet">
/// <item><description>Source file discovery and filtering</description></item>
/// <item><description>Markdown note generation with YAML frontmatter</description></item>
/// <item><description>Batch processing with progress tracking</description></item>
/// </list>
/// </para>
/// <para>
/// The markdown generation functionality utilizes the <see cref="MarkdownNoteBatchProcessor"/>
/// for consistent batch processing architecture and basic HTML-to-markdown conversion
/// without requiring OpenAI or other AI services.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var rootCommand = new RootCommand();
/// var markdownCommands = new MarkdownCommands(logger, appConfig, serviceProvider);
/// markdownCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
/// rootCommand.Invoke("generate-markdown --path \"Financial Management/Course Content\"");
/// </code>
/// </example>
internal class MarkdownCommands
{
    private readonly ILogger<MarkdownCommands> logger;
    private readonly AppConfig appConfig;
    private readonly IServiceProvider serviceProvider;

    public MarkdownCommands(ILogger<MarkdownCommands> logger, AppConfig appConfig, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.appConfig = appConfig;
        this.serviceProvider = serviceProvider;
        this.logger.LogDebug($"Markdown command initialized");
    }

    /// <summary>
    /// Registers the 'generate-markdown' command with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to add subcommands to.</param>
    /// <param name="configOption">The global config file option.</param>
    /// <param name="debugOption">The global debug option.</param>
    /// <param name="verboseOption">The global verbose option.</param>
    /// <param name="dryRunOption">The global dry-run option.</param>
    /// <remarks>
    /// <para>
    /// This method adds the 'generate-markdown' command to the root command, enabling users to convert
    /// source files to markdown format. It defines options for source directories, destination directory,
    /// and other global settings.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var rootCommand = new RootCommand();
    /// markdownCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
    /// rootCommand.Invoke("generate-markdown --path \"Financial Management/Course Content\"");
    /// </code>
    /// </example>
    public void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
    {
        var pathOption = new Option<string>(
            aliases: ["--path", "-p"],
            description: "Relative path within the vault/OneDrive structure for processing");

        var vaultRootOverrideOption = new Option<string?>(
            aliases: ["--override-vault-root"],
            description: "Specify the explicit vault root path (overrides the config)");

        var extractFromMarkdownOption = new Option<bool>(
            aliases: ["--extract-from-markdown"],
            description: "Extract HTML content from OneDrive path specified in markdown frontmatter");

        var noShareLinksOption = new Option<bool>(
            aliases: ["--no-share-links"],
            description: "Disable OneDrive share link generation");

        var markdownCommand = new Command("generate-markdown", "Generate markdown from HTML, TXT, and EPUB sources");
        markdownCommand.AddOption(pathOption);
        markdownCommand.AddOption(vaultRootOverrideOption);
        markdownCommand.AddOption(extractFromMarkdownOption);
        markdownCommand.AddOption(noShareLinksOption);
        markdownCommand.AddOption(configOption);
        markdownCommand.AddOption(debugOption);
        markdownCommand.AddOption(verboseOption);
        markdownCommand.AddOption(dryRunOption);
        markdownCommand.SetHandler(async context =>
        {
            string? path = context.ParseResult.GetValueForOption(pathOption);
            string? vaultRootOverride = context.ParseResult.GetValueForOption(vaultRootOverrideOption);
            bool extractFromMarkdown = context.ParseResult.GetValueForOption(extractFromMarkdownOption);
            bool noShareLinks = context.ParseResult.GetValueForOption(noShareLinksOption);
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            // Print usage/help if required argument is missing
            if (string.IsNullOrWhiteSpace(path))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation generate-markdown --path <path> [options]",
                    markdownCommand.Description ?? string.Empty,
                    string.Join("\n", markdownCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            // Initialize dependency injection if needed
            if (Program.ServiceProvider == null && config != null)
            {
                if (!System.IO.File.Exists(config))
                {
                    AnsiConsoleHelper.WriteError($"Configuration file not found: {config}");
                    return;
                }

                Program.SetupDependencyInjection(config, debug);
            }

            await ProcessMarkdownAsync(path, vaultRootOverride, config, debug, verbose, dryRun, extractFromMarkdown, noShareLinks).ConfigureAwait(false);
        });

        rootCommand.AddCommand(markdownCommand);
    }

    /// <summary>
    /// Processes source files in the specified path and generates markdown notes.
    /// </summary>
    /// <param name="relativePath">Relative path within the vault/OneDrive structure for processing.</param>
    /// <param name="vaultRootOverride">Explicit vault root path override.</param>
    /// <param name="configPath">Path to the configuration file.</param>
    /// <param name="debug">Enable debug output.</param>
    /// <param name="verbose">Enable verbose output.</param>
    /// <param name="dryRun">Simulate actions without making changes.</param>
    /// <param name="extractFromMarkdown">Extract HTML content from OneDrive path specified in markdown frontmatter.</param>
    /// <param name="noShareLinks">Disable OneDrive share link generation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ProcessMarkdownAsync(
        string? relativePath,
        string? vaultRootOverride,
        string? configPath,
        bool debug,
        bool verbose,
        bool dryRun,
        bool extractFromMarkdown = false,
        bool noShareLinks = false)
    {
        try
        {
            // Use DI container to get services and create scoped context for vault root override
            var serviceProvider = Program.ServiceProvider;
            using var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            var loggerFactory = scopedServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("MarkdownCommands");
            var appConfig = scopedServices.GetRequiredService<AppConfig>();

            // Note: MarkdownNoteBatchProcessor handles HTML-to-markdown conversion
            // without requiring OpenAI configuration for basic HTML processing

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                logger.LogError($"Relative path is required.");
                return;
            }

            // Handle extract-from-markdown mode
            if (extractFromMarkdown)
            {
                await ProcessExtractFromMarkdownAsync(relativePath, vaultRootOverride, appConfig, logger, scopedServices, dryRun, noShareLinks);
                return;
            }

            // Determine effective vault root (combined root + resources basepath if configured)
            var effectiveVaultRoot = appConfig.Paths?.GetEffectiveVaultRoot();
            logger.LogDebug("Resolved effective vault root: {effectiveVaultRoot} (raw: {rawRoot}, basepath: {basepath})",
                effectiveVaultRoot,
                appConfig.Paths?.NotebookVaultFullpathRoot,
                appConfig.Paths?.NotebookVaultResourcesBasepath);

            // Construct OneDrive source path from relative path
            string oneDriveSourcePath = Path.Combine(
                appConfig.Paths?.GetEffectiveOneDriveRoot() ?? string.Empty,
                relativePath);

            logger.LogDebug("Using effective OneDrive root from configuration: {oneDriveRoot}",
                appConfig.Paths?.GetEffectiveOneDriveRoot());

            // Check if OneDrive source path exists (can be either directory or file)
            if (!Directory.Exists(oneDriveSourcePath) && !File.Exists(oneDriveSourcePath))
            {
                logger.LogWarning($"OneDrive source path not found: {oneDriveSourcePath}");
                return;
            }

            // Determine if we're processing a single file or a directory
            bool isProcessingSingleFile = File.Exists(oneDriveSourcePath);

            // Construct vault destination path appropriately for file vs directory
            string vaultDestPath;
            if (isProcessingSingleFile)
            {
                // For single file: destination should be the directory where the converted file will be saved
                // Remove the filename from the relative path to get just the directory structure
                string relativeDirPath = Path.GetDirectoryName(relativePath) ?? string.Empty;
                vaultDestPath = string.IsNullOrEmpty(relativeDirPath)
                    ? effectiveVaultRoot ?? appConfig.Paths?.NotebookVaultFullpathRoot ?? "Generated"
                    : Path.Combine(effectiveVaultRoot ?? appConfig.Paths?.NotebookVaultFullpathRoot ?? "Generated", relativeDirPath);

                logger.LogDebug("Processing single file. Source: {source}, Destination directory: {destDir}",
                    oneDriveSourcePath, vaultDestPath);
            }
            else
            {
                // For directory: destination includes the full relative path structure
                vaultDestPath = Path.Combine(
                    effectiveVaultRoot ?? appConfig.Paths?.NotebookVaultFullpathRoot ?? "Generated",
                    relativePath);

                logger.LogDebug("Processing directory. Source: {source}, Destination: {dest}",
                    oneDriveSourcePath, vaultDestPath);
            }

            // Set up vault root override in scoped context
            var vaultRootContext = scopedServices.GetRequiredService<VaultRootContextService>();

            // Use explicit vault root override if provided, otherwise use effective vault root
            string finalVaultRoot = vaultRootOverride
                                    ?? effectiveVaultRoot
                                    ?? appConfig.Paths?.NotebookVaultFullpathRoot
                                    ?? Path.GetDirectoryName(vaultDestPath)
                                    ?? "Generated";
            vaultRootContext.VaultRootOverride = finalVaultRoot;
            logger.LogDebug($"Using vault root override for metadata hierarchy: {finalVaultRoot}");

            // Create vault destination directory if it doesn't exist
            Directory.CreateDirectory(vaultDestPath);

            // Get MarkdownNoteBatchProcessor from DI for proper batch processing
            var batchProcessor = scopedServices.GetRequiredService<MarkdownNoteBatchProcessor>();

            // Use the newer Spectre.Console status display with live updates
            var result = await AnsiConsoleHelper.WithStatusAsync(
                async (updateStatus) =>
                {
                    // Hook up progress events to update the status
                    batchProcessor.ProcessingProgressChanged += (sender, e) =>
                    {
                        // Escape any markup to avoid Spectre.Console parsing issues
                        string safeStatus = e.Status.Replace("[", "[[").Replace("]", "]]");

                        // The status already contains file count information, so we don't need to add it
                        updateStatus(safeStatus);
                    };

                    return await batchProcessor.ProcessFilesAsync(
                        input: oneDriveSourcePath,
                        output: vaultDestPath,
                        fileExtensions: appConfig.HtmlExtensions.Concat([".txt", ".epub"]).ToList(),
                        openAiApiKey: null, // No OpenAI needed for basic HTML-to-markdown conversion
                        dryRun: dryRun,
                        noSummary: true, // Skip AI summarization - just do basic HTML-to-markdown conversion
                        forceOverwrite: true, // Allow overwriting existing files
                        retryFailed: false,
                        timeoutSeconds: null,
                        resourcesRoot: null,
                        appConfig: appConfig,
                        noShareLinks: noShareLinks
                    ).ConfigureAwait(false);
                },
                $"Processing markdown files from {(isProcessingSingleFile ? "file" : "directory")}: {oneDriveSourcePath}").ConfigureAwait(false);

            logger.LogInformation("Markdown generation completed successfully");
            logger.LogInformation($"Processed files: {result.Processed}, Failed: {result.Failed}");


            if (result.Failed > 0)
            {
                logger.LogWarning("Some files failed to process. Check the logs for details.");
            }

            if (!string.IsNullOrEmpty(result.Summary))
            {
                logger.LogInformation($"Summary: {result.Summary}");
            }
        }
        catch (Exception ex)
        {
            ExceptionHandler.HandleException(ex, "markdown generation");
        }
    }

    /// <summary>
    /// Processes a markdown file to extract HTML content from OneDrive path specified in frontmatter.
    /// </summary>
    /// <param name="relativePath">Relative path to the markdown file within the vault.</param>
    /// <param name="vaultRootOverride">Explicit vault root path override.</param>
    /// <param name="appConfig">The application configuration.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="scopedServices">The scoped service provider.</param>
    /// <param name="dryRun">Whether to perform a dry run.</param>
    /// <param name="noShareLinks">Disable OneDrive share link generation.</param>
    private async Task ProcessExtractFromMarkdownAsync(
        string relativePath,
        string? vaultRootOverride,
        AppConfig appConfig,
        ILogger logger,
        IServiceProvider scopedServices,
        bool dryRun,
        bool noShareLinks)
    {
        try
        {
            // Get the effective vault root and base path for proper path construction
            string effectiveVaultRoot = vaultRootOverride ?? appConfig.Paths?.GetEffectiveVaultRoot() ?? string.Empty;
            string vaultResourcesBasePath = appConfig.Paths?.NotebookVaultResourcesBasepath ?? string.Empty;
            string rawVaultRoot = appConfig.Paths?.NotebookVaultFullpathRoot ?? string.Empty;

            if (string.IsNullOrEmpty(rawVaultRoot))
            {
                logger.LogError("Vault root path is not configured and no override provided.");
                return;
            }

            // Check if the relativePath starts with the vault resources base path
            // If so, we should use the raw vault root to avoid path duplication
            string basePath = vaultResourcesBasePath.Trim('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            string normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            bool pathStartsWithBasePath = !string.IsNullOrEmpty(basePath) &&
                                        normalizedRelativePath.StartsWith(basePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

            string vaultRootToUse = pathStartsWithBasePath ? rawVaultRoot : effectiveVaultRoot;

            logger.LogDebug($"Path analysis - relativePath: '{relativePath}', basePath: '{basePath}', pathStartsWithBasePath: {pathStartsWithBasePath}");
            logger.LogDebug($"Using vault root: '{vaultRootToUse}' (raw: '{rawVaultRoot}', effective: '{effectiveVaultRoot}')");

            // Construct the full path to the target markdown file location
            string markdownFilePath = Path.Combine(vaultRootToUse, relativePath);

            logger.LogDebug($"Target markdown file path: {markdownFilePath}");

            // For Extract HTML Content mode, we need to find the corresponding HTML file
            // If the markdown file exists, read the frontmatter to get the OneDrive path
            // If it doesn't exist, infer the HTML file path from the markdown file path
            string? oneDriveRelativePath = null;
            Dictionary<string, object>? frontmatterDict = null;

            if (File.Exists(markdownFilePath))
            {
                logger.LogInformation($"Processing existing markdown file: {markdownFilePath}");

                // Read and parse the frontmatter from the existing markdown file
                var yamlHelper = scopedServices.GetRequiredService<IYamlHelper>();
                string markdownContent = await File.ReadAllTextAsync(markdownFilePath);
                string? frontmatterYaml = yamlHelper.ExtractFrontmatter(markdownContent);

                if (!string.IsNullOrEmpty(frontmatterYaml))
                {
                    frontmatterDict = yamlHelper.ParseYamlToDictionary(frontmatterYaml);
                    if (frontmatterDict.TryGetValue("onedrive_relative_path", out var oneDriveRelativePathObj))
                    {
                        oneDriveRelativePath = oneDriveRelativePathObj?.ToString();
                    }
                }
            }

            // If we couldn't get the OneDrive path from frontmatter, infer it from the markdown file path
            if (string.IsNullOrWhiteSpace(oneDriveRelativePath))
            {
                // Convert the markdown file path to an HTML file path
                // Remove the markdown file name and add .html extension to the base name
                string markdownFileName = Path.GetFileNameWithoutExtension(markdownFilePath);

                // Remove common suffixes like "-reading" to get the base HTML filename
                string htmlFileName = markdownFileName.Replace("-reading", "").Replace("_reading", "");

                // For inference, we only need the HTML filename, not the full directory path
                // The OneDrive base path will be combined later
                oneDriveRelativePath = htmlFileName + ".html";

                logger.LogInformation($"Inferred OneDrive relative path: {oneDriveRelativePath}");
            }
            else
            {
                logger.LogInformation($"Found OneDrive relative path in frontmatter: {oneDriveRelativePath}");
            }

            if (string.IsNullOrWhiteSpace(oneDriveRelativePath))
            {
                logger.LogError($"Could not determine HTML file path for markdown file: {markdownFilePath}");
                return;
            }

            // Construct the full OneDrive source path
            string oneDriveSourcePath = Path.Combine(
                appConfig.Paths?.GetEffectiveOneDriveRoot() ?? string.Empty,
                oneDriveRelativePath);

            if (!File.Exists(oneDriveSourcePath))
            {
                logger.LogError($"OneDrive source file not found: {oneDriveSourcePath}");
                return;
            }

            // Validate that the source file has a compatible extension for HTML content extraction
            var supportedExtensions = appConfig.HtmlExtensions.Concat([".txt", ".epub"]).ToList();
            string fileExtension = Path.GetExtension(oneDriveSourcePath).ToLowerInvariant();

            if (!supportedExtensions.Contains(fileExtension))
            {
                logger.LogError($"Input must be a file or directory containing valid files: {oneDriveSourcePath}");
                logger.LogError($"Extract HTML Content feature only supports files with extensions: {string.Join(", ", supportedExtensions)}");
                logger.LogError($"Found file with extension: {fileExtension}");
                AnsiConsoleHelper.WriteError($"Extracting HTML content from: {Environment.NewLine}{oneDriveSourcePath}");
                return;
            }

            logger.LogInformation($"Processing source file: {oneDriveSourcePath}");

            // Check if this is a preprocessing file (auto-generated-state=pending)
            bool isPreprocessingFile = frontmatterDict?.TryGetValue("auto-generated-state", out var stateObj) == true &&
                                     stateObj?.ToString() == "pending";

            // Set up vault root override in scoped context
            var vaultRootContext = scopedServices.GetRequiredService<VaultRootContextService>();
            vaultRootContext.VaultRootOverride = vaultRootToUse;

            // For Extract HTML Content, we want to generate the file directly at the markdown file location
            // Create the output directory (parent directory of the target markdown file)
            string outputDirectory = Path.GetDirectoryName(markdownFilePath) ?? vaultRootToUse;
            Directory.CreateDirectory(outputDirectory);

            // Get MarkdownNoteBatchProcessor from DI for proper batch processing
            var batchProcessor = scopedServices.GetRequiredService<MarkdownNoteBatchProcessor>();

            // Configure OneDrive service for share link generation
            var oneDriveService = scopedServices.GetRequiredService<IOneDriveService>();
            string localResourcesPath = appConfig.Paths?.GetEffectiveVaultRoot() ?? string.Empty;
            string oneDriveResourcesBasePath = appConfig.Paths?.OnedriveResourcesBasepath ?? string.Empty;
            oneDriveService.ConfigureVaultRoots(localResourcesPath, oneDriveResourcesBasePath);

            // Process the single source file and generate the markdown directly at the target location
            // Override the resourcesRoot to null to prevent directory structure preservation
            var result = await AnsiConsoleHelper.WithStatusAsync(
                async (updateStatus) =>
                {
                    // Hook up progress events to update the status
                    batchProcessor.ProcessingProgressChanged += (sender, e) =>
                    {
                        // Escape any markup to avoid Spectre.Console parsing issues
                        string safeStatus = e.Status.Replace("[", "[[").Replace("]", "]]");
                        updateStatus(safeStatus);
                    };

                    return await batchProcessor.ProcessFilesAsync(
                        input: oneDriveSourcePath,
                        output: outputDirectory,
                        fileExtensions: appConfig.HtmlExtensions.Concat([".txt", ".epub"]).ToList(),
                        openAiApiKey: null, // No OpenAI needed for basic HTML-to-markdown conversion
                        dryRun: dryRun,
                        noSummary: true, // Skip AI summarization - just do basic HTML-to-markdown conversion
                        forceOverwrite: true, // Force overwrite to replace existing content
                        retryFailed: false,
                        timeoutSeconds: null,
                        resourcesRoot: null, // Set to null to prevent OneDrive directory structure preservation
                        appConfig: appConfig,
                        noShareLinks: noShareLinks
                    ).ConfigureAwait(false);
                },
                $"Extracting HTML content from: {oneDriveSourcePath}").ConfigureAwait(false);

            logger.LogInformation("HTML content extraction completed successfully");
            logger.LogInformation($"Processed files: {result.Processed}, Failed: {result.Failed}");

            if (result.Failed > 0)
            {
                logger.LogWarning("Some files failed to process. Check the logs for details.");
                return;
            }

            // Find the generated markdown file
            // The batch processor preserves the OneDrive directory structure, so search recursively
            var generatedFiles = Directory.GetFiles(outputDirectory, "*.md", SearchOption.AllDirectories)
                .Where(f => Path.GetFileName(f).StartsWith(Path.GetFileNameWithoutExtension(oneDriveSourcePath)))
                .ToArray();

            if (generatedFiles.Length == 0)
            {
                logger.LogError($"No generated markdown file found starting with: {Path.GetFileNameWithoutExtension(oneDriveSourcePath)}");
                return;
            }

            string expectedGeneratedFile = generatedFiles[0]; // Take the first match

            logger.LogInformation($"Generated file found at: {expectedGeneratedFile}");

            if (!dryRun)
            {
                // Read the generated content and replace the original file
                string generatedContent = await File.ReadAllTextAsync(expectedGeneratedFile);

                // Ensure the target directory exists
                string targetDirectory = Path.GetDirectoryName(markdownFilePath) ?? string.Empty;
                Directory.CreateDirectory(targetDirectory);

                await File.WriteAllTextAsync(markdownFilePath, generatedContent);
                logger.LogInformation($"Successfully replaced original markdown file: {markdownFilePath}");

                // Note: In extract-from-markdown mode, we want to keep the generated file as it IS the target file
            }
            else
            {
                logger.LogInformation($"Dry run: Would replace {markdownFilePath} with content from {expectedGeneratedFile}");
            }

            if (!string.IsNullOrEmpty(result.Summary))
            {
                logger.LogInformation($"Summary: {result.Summary}");
            }
        }
        catch (Exception ex)
        {
            ExceptionHandler.HandleException(ex, "extract HTML content from markdown frontmatter");
        }
    }
}
