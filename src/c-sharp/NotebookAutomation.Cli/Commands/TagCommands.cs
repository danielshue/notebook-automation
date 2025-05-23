using NotebookAutomation.Cli.Utilities;
using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.TagManagement;

namespace NotebookAutomation.Cli.Commands
{
    /// <summary>
    /// Provides CLI commands for managing tags in markdown files within an Obsidian vault.
    /// 
    /// This class registers the 'tag' command group and its subcommands for tag management operations,
    /// including adding nested tags based on frontmatter fields, cleaning tags from index files,
    /// consolidating tags, restructuring tags for consistency, adding example tags, and
    /// checking/enforcing metadata consistency.
    /// </summary>
    /// <remarks>
    /// The tag management commands utilize the <see cref="TagProcessor"/> class from the Core library
    /// to perform the actual tag operations. This class serves as a bridge between the command-line
    /// interface and the core functionality.
    /// </remarks>
    internal class TagCommands
    {
        /// <summary>
        /// Registers all tag-related commands with the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add tag commands to.</param>
        /// <param name="configOption">The global config file option.</param>
        /// <param name="debugOption">The global debug option.</param>
        /// <param name="verboseOption">The global verbose output option.</param>
        /// <param name="dryRunOption">The global dry run option to simulate actions without making changes.</param>
        public void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
        {
            var pathArg = new Argument<string>("path", "Path to the directory or file to process");

            // add-nested command
            var addNestedCommand = new Command("add-nested", "Add nested tags based on frontmatter fields");
            addNestedCommand.AddArgument(pathArg);
            addNestedCommand.SetHandler(async (InvocationContext context) =>
            {
                string path = context.ParseResult.GetValueForArgument(pathArg);
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                await this.ProcessTagsAsync(path, "add-nested", config, debug, verbose, dryRun);
            });

            // clean-index command
            var cleanIndexCommand = new Command("clean-index", "Clean tags from index files");
            cleanIndexCommand.AddArgument(pathArg);
            cleanIndexCommand.SetHandler(async (InvocationContext context) =>
            {
                string path = context.ParseResult.GetValueForArgument(pathArg);
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                await this.ProcessTagsAsync(path, "clean-index", config, debug, verbose, dryRun);
            });

            // consolidate command
            var consolidateCommand = new Command("consolidate", "Consolidate tags in markdown files");
            consolidateCommand.AddArgument(pathArg);
            consolidateCommand.SetHandler(async (InvocationContext context) =>
            {
                string path = context.ParseResult.GetValueForArgument(pathArg);
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                await this.ProcessTagsAsync(path, "consolidate", config, debug, verbose, dryRun);
            });

            // restructure-tags command
            var restructureCommand = new Command("restructure", "Restructure tags for consistency");
            restructureCommand.AddArgument(pathArg);
            restructureCommand.SetHandler(async (InvocationContext context) =>
            {
                string path = context.ParseResult.GetValueForArgument(pathArg);
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                await this.ProcessTagsAsync(path, "restructure-tags", config, debug, verbose, dryRun);
            });

            // add-example-tags command
            var addExampleCommand = new Command("add-example", "Add example tags to a file");
            addExampleCommand.AddArgument(pathArg);
            addExampleCommand.SetHandler(async (InvocationContext context) =>
            {
                string path = context.ParseResult.GetValueForArgument(pathArg);
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                await this.ProcessTagsAsync(path, "add-example-tags", config, debug, verbose, dryRun);
            });

            // metadata-check command
            var metadataCommand = new Command("metadata-check", "Check and enforce metadata consistency");
            metadataCommand.AddArgument(pathArg);
            metadataCommand.SetHandler(async (InvocationContext context) =>
            {
                string path = context.ParseResult.GetValueForArgument(pathArg);
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                await this.ProcessTagsAsync(path, "metadata-check", config, debug, verbose, dryRun);
            });

            // Create a parent command for all tag-related operations
            var tagCommand = new Command("tag", "Tag management commands");
            tagCommand.AddCommand(addNestedCommand);
            tagCommand.AddCommand(cleanIndexCommand);
            tagCommand.AddCommand(consolidateCommand);
            tagCommand.AddCommand(restructureCommand);
            tagCommand.AddCommand(addExampleCommand);
            tagCommand.AddCommand(metadataCommand);

            // Error handler for invalid tag subcommands
            tagCommand.TreatUnmatchedTokensAsErrors = true;
            tagCommand.SetHandler((InvocationContext context) =>
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation tag <subcommand> [options]",
                    "Please specify a tag subcommand to execute. Available tag commands:",
                    string.Join("\n", tagCommand.Subcommands.Select(cmd => $"  {cmd.Name,-15} {cmd.Description}")) +
                    "\n\nRun 'notebookautomation tag [command] --help' for more information on a specific command."
                );
            });

            rootCommand.AddCommand(tagCommand);
        }

        /// <summary>
        /// Processes the tag command with the specified options.
        /// </summary>
        /// <param name="path">The path to process.</param>
        /// <param name="command">The tag command to execute.</param>
        /// <param name="configPath">The optional path to the configuration file.</param>
        /// <param name="debug">Whether debug mode is enabled.</param>
        /// <param name="verbose">Whether verbose output is enabled.</param>
        /// <param name="dryRun">Whether to simulate without making changes.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessTagsAsync(string path, string command, string? configPath, bool debug, bool verbose, bool dryRun)
        {
            try
            {
                // Initialize dependency injection if needed
                if (configPath != null)
                {
                    if (!System.IO.File.Exists(configPath))
                    {
                        AnsiConsoleHelper.WriteError($"Configuration file not found: {configPath}");
                        return;
                    }
                    Program.SetupDependencyInjection(configPath, debug);
                }

                var serviceProvider = Program.ServiceProvider;
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("TagCommands");
                var loggingService = serviceProvider.GetRequiredService<LoggingService>();
                var failedLogger = loggingService.FailedLogger;

                // Always show the active config file being used
                string activeConfigPath = configPath ?? AppConfig.FindConfigFile() ?? "config.json";
                AnsiConsoleHelper.WriteInfo($"Using config file: {activeConfigPath}\n");

                logger.LogInformation("Executing tag command: {Command} on path: {Path}", command, path);
                // Create a new TagProcessor with command-specific options
                // For TagProcessor we can't use DI directly since we need to pass dryRun and verbose
                var tagProcessorLogger = loggerFactory.CreateLogger<TagProcessor>();
                var tagProcessor = new TagProcessor(
                    tagProcessorLogger,
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
                        
                    case "restructure-tags":
                        stats = await tagProcessor.RestructureTagsInDirectoryAsync(path);
                        LogStats(logger, stats);
                        break;
                        
                    case "add-example-tags":
                        var success = await tagProcessor.AddExampleTagsToFileAsync(path);
                        logger.LogInformation(success ? "Example tags added." : "Failed to add example tags.");
                        break;
                        
                    case "metadata-check":
                        stats = await tagProcessor.CheckAndEnforceMetadataConsistencyAsync(path);
                        LogStats(logger, stats);
                        break;
                        
                    default:
                        logger.LogError("Unknown command: {Command}", command);
                        break;
                }
            }
            catch (Exception ex)
            {
                AnsiConsoleHelper.WriteError($"Error processing tags: {ex.Message}");
                if (debug)
                {
                    AnsiConsoleHelper.WriteError(ex.ToString());
                }
            }
        }

        /// <summary>
        /// Logs tag processing statistics to the provided logger.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="stats">Dictionary containing statistics information.</param>
        /// <remarks>
        /// <list type="bullet">
        ///   <item>
        ///     <description>FilesProcessed: Number of files processed.</description>
        ///   </item>
        ///   <item>
        ///     <description>FilesUpdated: Number of files that were modified.</description>
        ///   </item>
        ///   <item>
        ///     <description>TagsAdded: Number of tags that were added.</description>
        ///   </item>
        ///   <item>
        ///     <description>TagsRemoved: Number of tags that were removed.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        private void LogStats(ILogger logger, Dictionary<string, int> stats)
        {
            logger.LogInformation("Tag processing completed with the following statistics:");
            logger.LogInformation("- Files processed: {Count}", stats.GetValueOrDefault("FilesProcessed", 0));
            logger.LogInformation("- Files updated: {Count}", stats.GetValueOrDefault("FilesUpdated", 0));
            logger.LogInformation("- Tags added: {Count}", stats.GetValueOrDefault("TagsAdded", 0));
            logger.LogInformation("- Tags removed: {Count}", stats.GetValueOrDefault("TagsRemoved", 0));
        }
    }
}
