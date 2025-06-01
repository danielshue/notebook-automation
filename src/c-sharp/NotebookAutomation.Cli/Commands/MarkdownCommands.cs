using NotebookAutomation.Cli.Utilities;
using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.MarkdownGeneration;
using NotebookAutomation.Core.Utils;
using NotebookAutomation.Core.Services;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace NotebookAutomation.Cli.Commands
{
    /// <summary>
    /// Provides CLI commands for generating markdown notes from various source formats.
    /// 
    /// This class registers the 'generate-markdown' command, which converts HTML, TXT, and EPUB files
    /// to markdown format, optionally using OpenAI for summarization.
    /// </summary>
    public class MarkdownCommands
    {
        private readonly ILogger<MarkdownCommands> _logger;
        private readonly AppConfig _appConfig;
        private readonly IServiceProvider _serviceProvider;

        public MarkdownCommands(ILogger<MarkdownCommands> logger, AppConfig appConfig, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _appConfig = appConfig;
            _serviceProvider = serviceProvider;
            _logger.LogInformationWithPath("Markdown command initialized", "MarkdownCommands.cs");
        }

        /// <summary>
        /// Registers the 'generate-markdown' command with the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add subcommands to.</param>
        /// <param name="configOption">The global config file option.</param>
        /// <param name="debugOption">The global debug option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        public void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
        {
            var srcDirsOption = new Option<string[]>(
                aliases: new[] { "--src-dirs", "-s" },
                description: "Source directories containing files to convert")
            {
                AllowMultipleArgumentsPerToken = true
            };
            var destDirOption = new Option<string>(
                aliases: new[] { "--dest-dir", "-d" },
                description: "Destination directory for generated markdown files");

            var markdownCommand = new Command("generate-markdown", "Generate markdown from HTML, TXT, and EPUB sources");
            markdownCommand.AddOption(srcDirsOption);
            markdownCommand.AddOption(destDirOption);
            markdownCommand.AddOption(configOption);
            markdownCommand.AddOption(debugOption);
            markdownCommand.AddOption(verboseOption);
            markdownCommand.AddOption(dryRunOption);

            markdownCommand.SetHandler(async (InvocationContext context) =>
            {
                string[] srcDirs = context.ParseResult.GetValueForOption(srcDirsOption) ?? Array.Empty<string>();
                string? destDir = context.ParseResult.GetValueForOption(destDirOption);
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
                        string.Join("\n", markdownCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}"))
                    );
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

                await this.ProcessMarkdownAsync(srcDirs, destDir, config, debug, verbose, dryRun);
            });

            rootCommand.AddCommand(markdownCommand);
        }

        /// <summary>
        /// Processes source files in the specified directories and generates markdown notes.
        /// </summary>
        /// <param name="sourceDirs">Array of source directories to process.</param>
        /// <param name="destDir">Destination directory for generated markdown files.</param>
        /// <param name="configPath">Path to the configuration file.</param>
        /// <param name="debug">Enable debug output.</param>
        /// <param name="verbose">Enable verbose output.</param>
        /// <param name="dryRun">Simulate actions without making changes.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessMarkdownAsync(
            string[]? sourceDirs,
            string? destDir,
            string? configPath,
            bool debug,
            bool verbose,
            bool dryRun)
        {
            try
            {
                // Use DI container to get services
                var serviceProvider = Program.ServiceProvider;
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("MarkdownCommands");
                var appConfig = serviceProvider.GetRequiredService<AppConfig>();
                var loggingService = serviceProvider.GetRequiredService<LoggingService>();
                var failedLogger = loggingService?.FailedLogger;

                // Validate OpenAI config before proceeding
                if (!ConfigValidation.RequireOpenAi(appConfig))
                {
                    logger.LogError("OpenAI configuration is missing or incomplete. Exiting.");
                    return;
                }
                if (sourceDirs == null || sourceDirs.Length == 0)
                {
                    logger.LogError("Source directories are required");
                    return;
                }

                string? openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY"); if (string.IsNullOrWhiteSpace(openAiApiKey) && appConfig?.AiService != null)
                {
                    openAiApiKey = appConfig.AiService.GetApiKey();
                }

                // Resolve dependencies for MarkdownNoteProcessor
                var aiSummarizer = serviceProvider.GetRequiredService<AISummarizer>();
                var processor = new MarkdownNoteProcessor(logger, aiSummarizer);
                foreach (var sourceDir in sourceDirs)
                {
                    if (!Directory.Exists(sourceDir))
                    {
                        logger.LogWarning("Source directory not found: {Dir}", sourceDir);
                        continue;
                    }

                    var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext != ".txt" && ext != ".html" && ext != ".htm" && ext != ".epub")
                            continue;

                        try
                        {
                            logger.LogInformation("Processing file: {File}", file);
                            string markdown = await processor.ConvertToMarkdownAsync(file, openAiApiKey, "chunk_summary_prompt.md");

                            if (!dryRun)
                            {
                                string outputDir = destDir ?? (appConfig?.Paths?.NotebookVaultFullpathRoot ?? "Generated");
                                Directory.CreateDirectory(outputDir);
                                string outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(file) + ".md");
                                await File.WriteAllTextAsync(outputPath, markdown);
                                logger.LogInformation("Markdown note saved to: {OutputPath}", outputPath);
                            }
                            else
                            {
                                logger.LogInformation("[DRY RUN] Markdown note would be generated for: {File}", file);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to process file: {File}", file);
                            failedLogger?.LogError(ex, "Failed to process file: {File}", file);
                        }
                    }
                }

                logger.LogInformation("Markdown generation complete");
            }
            catch (Exception ex)
            {
                _logger.LogErrorWithPath("Error in markdown command", "MarkdownCommands.cs", ex);
                AnsiConsoleHelper.WriteError($"Error generating markdown: {ex.Message}");
            }
        }
    }
}
