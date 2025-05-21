using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.MarkdownGeneration;

namespace NotebookAutomation.Cli.MarkdownGenerator
{
    /// <summary>
    /// Entry point for the Markdown Generator CLI tool.
    /// 
    /// This program provides functionality for generating markdown files from various
    /// source formats, including HTML, TXT, and EPUB.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point for the application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Exit code (0 for success, non-zero for error).</returns>
        public static async Task<int> Main(string[] args)
        {
            // Create the root command with description
            var rootCommand = new RootCommand(
                description: "Markdown Generator - Generate markdown from HTML, TXT, and EPUB sources");
                
            // Define options
            var srcDirsOption = new Option<string[]>(
                aliases: new[] { "--src-dirs", "-s" },
                description: "Source directories containing files to convert")
            {
                AllowMultipleArgumentsPerToken = true
            };
            
            var destDirOption = new Option<string>(
                aliases: new[] { "--dest-dir", "-d" },
                description: "Destination directory for generated markdown files");
                
            var configOption = new Option<string>(
                aliases: new[] { "--config", "-c" },
                description: "Path to the configuration file");
                
            var debugOption = new Option<bool>(
                aliases: new[] { "--debug" },
                description: "Enable debug output");
                
            var verboseOption = new Option<bool>(
                aliases: new[] { "--verbose", "-v" },
                description: "Enable verbose output");
                
            var dryRunOption = new Option<bool>(
                aliases: new[] { "--dry-run" },
                description: "Simulate conversion without writing files");
                
            // Add options to the root command
            rootCommand.AddOption(srcDirsOption);
            rootCommand.AddOption(destDirOption);
            rootCommand.AddGlobalOption(configOption);
            rootCommand.AddGlobalOption(debugOption);
            rootCommand.AddGlobalOption(verboseOption);
            rootCommand.AddGlobalOption(dryRunOption);
            
            // Set the handler for the root command
            rootCommand.SetHandler(async (srcDirs, destDir, config, debug, verbose, dryRun) =>
            {
                await GenerateMarkdownAsync(srcDirs, destDir, config, debug, verbose, dryRun);
            }, srcDirsOption, destDirOption, configOption, debugOption, verboseOption, dryRunOption);
            
            // Execute the command
            return await rootCommand.InvokeAsync(args);
        }
        
        /// <summary>
        /// Generates markdown files from source files.
        /// </summary>
        /// <param name="sourceDirs">Source directories containing files to convert.</param>
        /// <param name="destDir">Destination directory for generated markdown files.</param>
        /// <param name="configPath">Optional path to configuration file.</param>
        /// <param name="debug">Whether to enable debug output.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <param name="dryRun">Whether to perform a dry run.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private static async Task GenerateMarkdownAsync(
            string[]? sourceDirs,
            string? destDir,
            string? configPath,
            bool debug,
            bool verbose,
            bool dryRun)
        {
            try
            {
                var configProvider = ConfigProvider.Create(configPath, debug);
                var logger = configProvider.Logger;
                if (sourceDirs == null || sourceDirs.Length == 0)
                {
                    logger.LogError("Source directories are required");
                    return;
                }
                string? openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (string.IsNullOrWhiteSpace(openAiApiKey) && configProvider.AppConfig?.OpenAi != null)
                {
                    openAiApiKey = configProvider.AppConfig.OpenAi.ApiKey;
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
                                string outputDir = destDir ?? (configProvider.AppConfig?.Paths?.NotebookVaultRoot ?? "Generated");
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
