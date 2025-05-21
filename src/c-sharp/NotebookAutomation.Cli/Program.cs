using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Cli
{
    /// <summary>
    /// Main entry point for the Notebook Automation CLI.
    /// 
    /// This program provides a unified command-line interface for accessing
    /// all the notebook automation tools.
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
                description: "MBA Notebook Automation CLI - Tools for managing Obsidian notebooks");

            // Global options
            var configOption = new Option<string>(
                aliases: new[] { "--config", "-c" },
                description: "Path to the configuration file");
            var debugOption = new Option<bool>(
                aliases: new[] { "--debug", "-d" },
                description: "Enable debug output");
            var verboseOption = new Option<bool>(
                aliases: new[] { "--verbose", "-v" },
                description: "Enable verbose output");
            var dryRunOption = new Option<bool>(
                aliases: new[] { "--dry-run" },
                description: "Simulate actions without making changes");

            rootCommand.AddGlobalOption(configOption);
            rootCommand.AddGlobalOption(debugOption);
            rootCommand.AddGlobalOption(verboseOption);
            rootCommand.AddGlobalOption(dryRunOption);

            // Create commands
            CreateTagCommands(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
            CreatePdfCommands(rootCommand);
            CreateMarkdownCommands(rootCommand);
            CreateVideoCommands(rootCommand);
            CreateVaultCommands(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
            CreateConfigCommand(rootCommand, configOption, debugOption);
            CreateVersionCommand(rootCommand);

            // Set a root handler to initialize configuration and logging
            rootCommand.SetHandler((string configPath, bool debug) =>
            {
                var config = ConfigProvider.Initialize(configPath, debug);
                var logger = config.Logger;
                logger.LogInformation("Notebook Automation CLI initialized");
                if (debug)
                {
                    logger.LogDebug("Debug logging enabled");
                }
            }, configOption, debugOption);

            // Execute the command
            return await rootCommand.InvokeAsync(args);
        }
        
        /// <summary>
        /// Creates commands related to tag management.
        /// </summary>
        /// <param name="rootCommand">The parent command to add to.</param>
        private static void CreateTagCommands(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
        {
            var tagCommand = new Command("tag", "Tag management commands");
            var pathArg = new Argument<string>("path", "Path to the directory or file to process");

            // Add-nested-tags command
            var addNestedCommand = new Command("add-nested", "Add nested tags based on frontmatter fields");
            addNestedCommand.AddArgument(pathArg);
            addNestedCommand.SetHandler(async (InvocationContext context) =>
            {
                string path = context.ParseResult.GetValueForArgument(pathArg);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                string? config = context.ParseResult.GetValueForOption(configOption);
                await ExecuteTagCommand("add-nested", path, debug, verbose, dryRun, config);
            });

            // Clean-index command
            var cleanIndexCommand = new Command("clean-index", "Clean tags from index files");
            cleanIndexCommand.AddArgument(pathArg);
            cleanIndexCommand.SetHandler(async (InvocationContext context) =>
            {
                string path = context.ParseResult.GetValueForArgument(pathArg);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                string? config = context.ParseResult.GetValueForOption(configOption);
                await ExecuteTagCommand("clean-index", path, debug, verbose, dryRun, config);
            });

            // Consolidate command
            var consolidateCommand = new Command("consolidate", "Consolidate tags across files");
            consolidateCommand.AddArgument(pathArg);
            consolidateCommand.SetHandler(async (InvocationContext context) =>
            {
                string path = context.ParseResult.GetValueForArgument(pathArg);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                string? config = context.ParseResult.GetValueForOption(configOption);
                await ExecuteTagCommand("consolidate", path, debug, verbose, dryRun, config);
            });

            tagCommand.AddCommand(addNestedCommand);
            tagCommand.AddCommand(cleanIndexCommand);
            tagCommand.AddCommand(consolidateCommand);
            rootCommand.Add(tagCommand);
        }
        
        /// <summary>
        /// Creates commands related to vault management.
        /// </summary>
        /// <param name="rootCommand">The parent command to add to.</param>
        private static void CreateVaultCommands(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
        {
            var vaultCommand = new Command("vault", "Vault management commands");
            var pathArg = new Argument<string>("path", "Path to the vault directory");

            // Generate index command
            var generateIndexCommand = new Command("generate-index", "Generate a vault index");
            generateIndexCommand.AddArgument(pathArg);
            generateIndexCommand.SetHandler(async (InvocationContext context) =>
            {
                string path = context.ParseResult.GetValueForArgument(pathArg);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                string? config = context.ParseResult.GetValueForOption(configOption);
                await ExecuteVaultCommand("generate-index", path, debug, verbose, dryRun, config);
            });

            // Ensure metadata command
            var ensureMetadataCommand = new Command("ensure-metadata", "Ensure consistent metadata across vault files");
            ensureMetadataCommand.AddArgument(pathArg);
            ensureMetadataCommand.SetHandler(async (InvocationContext context) =>
            {
                string path = context.ParseResult.GetValueForArgument(pathArg);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                string? config = context.ParseResult.GetValueForOption(configOption);
                await ExecuteVaultCommand("ensure-metadata", path, debug, verbose, dryRun, config);
            });

            vaultCommand.AddCommand(generateIndexCommand);
            vaultCommand.AddCommand(ensureMetadataCommand);
            rootCommand.Add(vaultCommand);
        }
        
        /// <summary>
        /// Creates the configuration command.
        /// </summary>
        /// <param name="rootCommand">The parent command to add to.</param>
        private static void CreateConfigCommand(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption)
        {
            var configCommand = new Command("config", "Display or update configuration");
            configCommand.SetHandler(async (InvocationContext context) =>
            {
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                string? config = context.ParseResult.GetValueForOption(configOption);
                var configProvider = ConfigProvider.Create(config, debug);
                var logger = configProvider.Logger;
                logger.LogInformation("Current configuration:");
                logger.LogInformation($"Notebook Vault Root: {configProvider.AppConfig.Paths.NotebookVaultRoot}");
                logger.LogInformation($"Obsidian Vault Root: {configProvider.AppConfig.Paths.ObsidianVaultRoot}");
                logger.LogInformation($"Resources Root: {configProvider.AppConfig.Paths.ResourcesRoot}");
                logger.LogInformation($"Logging Directory: {configProvider.AppConfig.Paths.LoggingDir}");
                await Task.CompletedTask;
            });
            rootCommand.Add(configCommand);
        }
        
        /// <summary>
        /// Creates the version command.
        /// </summary>
        /// <param name="rootCommand">The parent command to add to.</param>
        private static void CreateVersionCommand(RootCommand rootCommand)
        {
            var versionCommand = new Command("version", "Display version information");
            
            versionCommand.SetHandler(() =>
            {
                Console.WriteLine($"Notebook Automation CLI v1.0.0");
                Console.WriteLine($"Running on .NET {Environment.Version}");
                Console.WriteLine($"(c) 2023 Notebook Automation Team");
            });
            
            rootCommand.Add(versionCommand);
        }
        
        /// <summary>
        /// Executes a tag-related command.
        /// </summary>
        /// <param name="command">The specific tag command to execute.</param>
        /// <param name="path">The target path.</param>
        /// <param name="debug">Whether to enable debug output.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <param name="dryRun">Whether to perform a dry run.</param>
        /// <param name="configPath">Optional path to the configuration file.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private static async Task ExecuteTagCommand(
            string command,
            string path,
            bool debug,
            bool verbose,
            bool dryRun,
            string? configPath = null)
        {
            var configProvider = ConfigProvider.Create(configPath, debug);
            var logger = configProvider.Logger;
            
            logger.LogInformation("Executing tag command: {Command} on {Path}", command, path);
            
            if (dryRun)
            {
                logger.LogInformation("[DRY RUN] No changes will be made");
            }
            
            if (verbose)
            {
                logger.LogInformation("Verbose output enabled");
            }
            
            // Execute placeholder - this would actually call the specialized CLI tools
            logger.LogInformation("Redirecting to specialized CLI tool...");
            
            // In a real implementation, we would:
            // 1. Launch the appropriate specialized CLI tool process
            // 2. Pass through the arguments
            // 3. Capture and relay output
            
            // For now, just simulate the command execution
            logger.LogInformation("Simulating execution of tag command: {Command}", command);
            await Task.Delay(500); // Simulate work
            
            logger.LogInformation("Command completed successfully");
        }
        
        /// <summary>
        /// Executes a vault-related command.
        /// </summary>
        /// <param name="command">The specific vault command to execute.</param>
        /// <param name="path">The target path.</param>
        /// <param name="debug">Whether to enable debug output.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <param name="dryRun">Whether to perform a dry run.</param>
        /// <param name="configPath">Optional path to the configuration file.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private static async Task ExecuteVaultCommand(
            string command,
            string path,
            bool debug,
            bool verbose,
            bool dryRun,
            string? configPath = null)
        {
            var configProvider = ConfigProvider.Create(configPath, debug);
            var logger = configProvider.Logger;
            
            logger.LogInformation("Executing vault command: {Command} on {Path}", command, path);
            
            if (dryRun)
            {
                logger.LogInformation("[DRY RUN] No changes will be made");
            }
            
            if (verbose)
            {
                logger.LogInformation("Verbose output enabled");
            }
            
            // Execute placeholder - this would actually call the specialized CLI tools
            logger.LogInformation("Redirecting to specialized CLI tool...");
            
            // For now, just simulate the command execution
            logger.LogInformation("Simulating execution of vault command: {Command}", command);
            await Task.Delay(500); // Simulate work
            
            logger.LogInformation("Command completed successfully");
        }
        
        /// <summary>
        /// Creates commands related to PDF notes processing (placeholder).
        /// </summary>
        /// <param name="rootCommand">The parent command to add to.</param>
        private static void CreatePdfCommands(RootCommand rootCommand)
        {
            var pdfCommand = new Command("pdf-notes", "PDF notes processing commands (not yet implemented)");
            rootCommand.Add(pdfCommand);
        }

        /// <summary>
        /// Creates commands related to markdown generation (placeholder).
        /// </summary>
        /// <param name="rootCommand">The parent command to add to.</param>
        private static void CreateMarkdownCommands(RootCommand rootCommand)
        {
            var mdCommand = new Command("markdown", "Markdown generation commands (not yet implemented)");
            rootCommand.Add(mdCommand);
        }

        /// <summary>
        /// Creates commands related to video metadata handling (placeholder).
        /// </summary>
        /// <param name="rootCommand">The parent command to add to.</param>
        private static void CreateVideoCommands(RootCommand rootCommand)
        {
            var videoCommand = new Command("video-meta", "Video metadata commands (not yet implemented)");
            rootCommand.Add(videoCommand);
        }
    }
}
