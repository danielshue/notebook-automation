using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.MarkdownGeneration;

namespace NotebookAutomation.Cli.Commands
{
    /// <summary>
    /// Provides CLI commands for generating markdown notes from various source formats.
    /// 
    /// This class registers the 'generate-markdown' command, which converts HTML, TXT, and EPUB files
    /// to markdown format, optionally using OpenAI for summarization.
    /// </summary>
    internal class MarkdownCommands
    {
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
                
                // Initialize dependency injection if needed
                if (Program.ServiceProvider == null && config != null)
                {
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
                
                if (sourceDirs == null || sourceDirs.Length == 0)
                {
                    logger.LogError("Source directories are required");
                    return;
                }
                
                string? openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (string.IsNullOrWhiteSpace(openAiApiKey) && appConfig?.OpenAi != null)
                {
                    openAiApiKey = appConfig.OpenAi.ApiKey;
                }
                
                var processor = new MarkdownNoteProcessor(logger);
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
                                string outputDir = destDir ?? (appConfig?.Paths?.NotebookVaultRoot ?? "Generated");
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
                Console.Error.WriteLine($"Error generating markdown: {ex.Message}");
            }
        }
    }
}
