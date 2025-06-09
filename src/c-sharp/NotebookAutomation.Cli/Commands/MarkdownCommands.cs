// Licensed under the MIT License. See LICENSE file in the project root for full license information.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace NotebookAutomation.Cli.Commands;

/// <summary>
/// Provides CLI commands for generating markdown notes from various source formats.
/// </summary>
/// <remarks>
/// <para>
/// This class registers the 'generate-markdown' command, which converts HTML, TXT, and EPUB files
/// to markdown format, optionally using OpenAI for summarization. It supports:
/// <list type="bullet">
/// <item><description>Source file discovery and filtering</description></item>
/// <item><description>Markdown note generation with YAML frontmatter</description></item>
/// <item><description>Integration with AI summarization for enhanced notes</description></item>
/// </list>
/// </para>
/// <para>
/// The markdown generation functionality utilizes the <see cref="MarkdownNoteProcessor"/>
/// from the Core library to handle the actual processing of source files.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var rootCommand = new RootCommand();
/// var markdownCommands = new MarkdownCommands(logger, appConfig, serviceProvider);
/// markdownCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
/// rootCommand.Invoke("generate-markdown --src-dirs input --dest-dir output");
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
        this.logger.LogInformationWithPath("Markdown command initialized", "MarkdownCommands.cs");
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
    /// rootCommand.Invoke("generate-markdown --src-dirs input --dest-dir output");
    /// </code>
    /// </example>
    public void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
    {
        var srcDirsOption = new Option<string[]>(
            aliases: ["--src-dirs", "-s"],
            description: "Source directories containing files to convert")
        {
            AllowMultipleArgumentsPerToken = true,
        };
        var destDirOption = new Option<string>(
            aliases: ["--dest-dir", "-d"],
            description: "Destination directory for generated markdown files");

        var vaultRootOverrideOption = new Option<string?>(
            aliases: ["--override-vault-root"],
            description: "Specify the explicit vault root path (overrides the config)");

        var markdownCommand = new Command("generate-markdown", "Generate markdown from HTML, TXT, and EPUB sources");
        markdownCommand.AddOption(srcDirsOption);
        markdownCommand.AddOption(destDirOption);
        markdownCommand.AddOption(vaultRootOverrideOption);
        markdownCommand.AddOption(configOption);
        markdownCommand.AddOption(debugOption);
        markdownCommand.AddOption(verboseOption);
        markdownCommand.AddOption(dryRunOption);
        markdownCommand.SetHandler(async context =>
        {
            string[] srcDirs = context.ParseResult.GetValueForOption(srcDirsOption) ?? [];
            string? destDir = context.ParseResult.GetValueForOption(destDirOption);
            string? vaultRootOverride = context.ParseResult.GetValueForOption(vaultRootOverrideOption);
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            // Print usage/help if required argument is missing
            if (srcDirs == null || srcDirs.Length == 0)
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation generate-markdown --src-dirs <dir> [options]",
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

            await ProcessMarkdownAsync(srcDirs, destDir, vaultRootOverride, config, debug, verbose, dryRun).ConfigureAwait(false);
        });

        rootCommand.AddCommand(markdownCommand);
    }

    /// <summary>
    /// Processes source files in the specified directories and generates markdown notes.
    /// </summary>
    /// <param name="sourceDirs">Array of source directories to process.</param>
    /// <param name="destDir">Destination directory for generated markdown files.</param>
    /// <param name="vaultRootOverride">Explicit vault root path override.</param>
    /// <param name="configPath">Path to the configuration file.</param>
    /// <param name="debug">Enable debug output.</param>
    /// <param name="verbose">Enable verbose output.</param>
    /// <param name="dryRun">Simulate actions without making changes.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ProcessMarkdownAsync(
        string[]? sourceDirs,
        string? destDir,
        string? vaultRootOverride,
        string? configPath,
        bool debug,
        bool verbose,
        bool dryRun)
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
            var loggingService = scopedServices.GetRequiredService<LoggingService>();
            var failedLogger = loggingService?.FailedLogger;

            // Validate OpenAI config before proceeding
            if (!ConfigValidation.RequireOpenAi(appConfig))
            {
                logger.LogErrorWithPath("OpenAI configuration is missing or incomplete. Exiting.", "MarkdownCommands.cs");
                return;
            }

            if (sourceDirs == null || sourceDirs.Length == 0)
            {
                logger.LogErrorWithPath("Source directories are required", "MarkdownCommands.cs");
                return;
            }

            // Determine effective output directory for vault root context
            string effectiveOutputDir = destDir ?? appConfig.Paths?.NotebookVaultFullpathRoot ?? "Generated";
            effectiveOutputDir = Path.GetFullPath(effectiveOutputDir);

            // Set up vault root override in scoped context
            var vaultRootContext = scopedServices.GetRequiredService<VaultRootContextService>();

            // Use explicit vault root override if provided, otherwise use effective output directory
            string finalVaultRoot = vaultRootOverride ?? effectiveOutputDir;
            vaultRootContext.VaultRootOverride = finalVaultRoot;
            logger.LogInformationWithPath("Using vault root override for metadata hierarchy: {VaultRoot}", "MarkdownCommands.cs", finalVaultRoot);

            string? openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(openAiApiKey) && appConfig?.AiService != null)
            {
                openAiApiKey = appConfig.AiService.GetApiKey();
            }

            // Get MarkdownNoteProcessor from DI instead of manual creation
            var processor = scopedServices.GetRequiredService<MarkdownNoteProcessor>();
            foreach (var sourceDir in sourceDirs)
            {
                if (!Directory.Exists(sourceDir))
                {
                    logger.LogWarningWithPath("Source directory not found: {Dir}", "MarkdownCommands.cs", sourceDir);
                    continue;
                }

                var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".txt" && ext != ".html" && ext != ".htm" && ext != ".epub")
                    {
                        continue;
                    }

                    try
                    {
                        logger.LogInformationWithPath("Processing file: {File}", file);
                        string markdown = await processor.ConvertToMarkdownAsync(file, openAiApiKey, "chunk_summary_prompt.md").ConfigureAwait(false);
                        if (!dryRun)
                        {
                            Directory.CreateDirectory(effectiveOutputDir);
                            string outputPath = Path.Combine(effectiveOutputDir, Path.GetFileNameWithoutExtension(file) + ".md");
                            await File.WriteAllTextAsync(outputPath, markdown).ConfigureAwait(false);
                            logger.LogInformationWithPath("Markdown note saved to: {OutputPath}", outputPath);
                        }
                        else
                        {
                            logger.LogInformationWithPath("[DRY RUN] Markdown note would be generated for: {File}", file);
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandler.HandleException(ex, $"processing file {file}");
                        failedLogger?.LogError(ex, "Failed to process file: {File}", file);
                    }
                }
            }

            logger.LogInformationWithPath("Markdown generation complete", "MarkdownCommands.cs");
        }
        catch (Exception ex)
        {
            ExceptionHandler.HandleException(ex, "markdown generation");
        }
    }
}
