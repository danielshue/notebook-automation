using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.TagManagement;

namespace NotebookAutomation.Cli.TagManager
{
    /// <summary>
    /// Entry point for the Tag Manager CLI tool.
    /// Provides functionality for managing tags in Obsidian notes.
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
            var rootCommand = new RootCommand("Tag Manager - Tools for managing tags in Obsidian notes");

            // Common options
            var pathArg = new Argument<string>("path", "Path to the directory or file to process");
            var configOption = new Option<string>(new[] { "--config", "-c" }, "Path to the configuration file");
            var debugOption = new Option<bool>(new[] { "--debug", "-d" }, "Enable debug output");
            var verboseOption = new Option<bool>(new[] { "--verbose", "-v" }, "Enable verbose output");
            var dryRunOption = new Option<bool>(new[] { "--dry-run" }, "Simulate actions without making changes");

            rootCommand.AddGlobalOption(configOption);
            rootCommand.AddGlobalOption(debugOption);
            rootCommand.AddGlobalOption(verboseOption);
            rootCommand.AddGlobalOption(dryRunOption);

            // add-nested command
            var addNestedCommand = new Command("add-nested", "Add nested tags based on frontmatter fields");
            addNestedCommand.AddArgument(pathArg);
            addNestedCommand.SetHandler(async (string path, string config, bool debug, bool verbose, bool dryRun) =>
            {
                await ProcessTagsAsync(path, "add-nested", config, debug, verbose, dryRun);
            }, pathArg, configOption, debugOption, verboseOption, dryRunOption);

            // clean-index command
            var cleanIndexCommand = new Command("clean-index", "Clean tags from index files");
            cleanIndexCommand.AddArgument(pathArg);
            cleanIndexCommand.SetHandler(async (string path, string config, bool debug, bool verbose, bool dryRun) =>
            {
                await ProcessTagsAsync(path, "clean-index", config, debug, verbose, dryRun);
            }, pathArg, configOption, debugOption, verboseOption, dryRunOption);

            // consolidate command
            var consolidateCommand = new Command("consolidate", "Consolidate tags across files");
            consolidateCommand.AddArgument(pathArg);
            consolidateCommand.SetHandler(async (string path, string config, bool debug, bool verbose, bool dryRun) =>
            {
                await ProcessTagsAsync(path, "consolidate", config, debug, verbose, dryRun);
            }, pathArg, configOption, debugOption, verboseOption, dryRunOption);

            rootCommand.AddCommand(addNestedCommand);
            rootCommand.AddCommand(cleanIndexCommand);
            rootCommand.AddCommand(consolidateCommand);

            return await rootCommand.InvokeAsync(args);
        }

        /// <summary>
        /// Processes tags based on the specified command.
        /// </summary>
        /// <param name="path">Path to process.</param>
        /// <param name="command">Command to execute.</param>
        /// <param name="configPath">Optional configuration file path.</param>
        /// <param name="debug">Whether to enable debug output.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <param name="dryRun">Whether to perform a dry run.</param>
        private static async Task ProcessTagsAsync(string path, string command, string? configPath, bool debug, bool verbose, bool dryRun)
        {
            try
            {
                var configProvider = ConfigProvider.Create(configPath, debug);
                var logger = configProvider.Logger;
                var failedLogger = configProvider.FailedLogger;

                logger.LogInformation("Executing tag command: {Command} on path: {Path}", command, path);

                var tagProcessor = new TagProcessor(
                    configProvider.GetLogger<TagProcessor>(),
                    failedLogger,
                    dryRun,
                    verbose);

                switch (command.ToLowerInvariant())
                {
                    case "add-nested":
                        var stats = await tagProcessor.ProcessDirectoryAsync(path);
                        LogStats(logger, stats);
                        break;
                    case "clean-index":
                        logger.LogInformation("Clean index functionality uses the same processor");
                        stats = await tagProcessor.ProcessDirectoryAsync(path);
                        LogStats(logger, stats);
                        break;
                    case "consolidate":
                        logger.LogInformation("Consolidate tags functionality not yet implemented");
                        break;
                    default:
                        logger.LogError("Unknown command: {Command}", command);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing tags: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs tag processing statistics.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="stats">Statistics dictionary.</param>
        private static void LogStats(ILogger logger, System.Collections.Generic.Dictionary<string, int> stats)
        {
            logger.LogInformation("Tag processing completed with the following statistics:");
            logger.LogInformation("- Files processed: {Count}", stats.GetValueOrDefault("FilesProcessed", 0));
            logger.LogInformation("- Files modified: {Count}", stats.GetValueOrDefault("FilesModified", 0));
            logger.LogInformation("- Tags added: {Count}", stats.GetValueOrDefault("TagsAdded", 0));
            logger.LogInformation("- Index files cleared: {Count}", stats.GetValueOrDefault("IndexFilesCleared", 0));
            logger.LogInformation("- Files with errors: {Count}", stats.GetValueOrDefault("FilesWithErrors", 0));
        }
    }
}
