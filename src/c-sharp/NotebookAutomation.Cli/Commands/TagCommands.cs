// <copyright file="TagCommands.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Cli/Commands/TagCommands.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
using System.Runtime.CompilerServices;

using NotebookAutomation.Cli.Utilities;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.TagManagement;
using NotebookAutomation.Core.Utils;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace NotebookAutomation.Cli.Commands;

/// <summary>
/// Provides CLI commands for managing tags in markdown files within an Obsidian vault.
/// </summary>
/// <remarks>
/// This class registers the 'tag' command group and its subcommands for tag management operations,
/// including adding nested tags based on frontmatter fields, cleaning tags from index files,
/// consolidating tags, restructuring tags for consistency, adding example tags, and
/// checking/enforcing metadata consistency.
/// </remarks>
internal class TagCommands
{
    private readonly ILogger<TagCommands> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly AppConfig appConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagCommands"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information and errors.</param>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    public TagCommands(ILogger<TagCommands> logger, IServiceProvider serviceProvider)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.appConfig = serviceProvider.GetRequiredService<AppConfig>();
        this.logger.LogDebug("Tag command initialized");
    }

    /// <summary>
    /// Registers all tag-related commands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to add tag commands to.</param>
    /// <param name="configOption">The global config file option.</param>
    /// <param name="debugOption">The global debug option.</param>
    /// <param name="verboseOption">The global verbose output option.</param>
    /// <param name="dryRunOption">The global dry run option to simulate actions without making changes.</param>
    public void Register(
        RootCommand rootCommand,
        Option<string> configOption,
        Option<bool> debugOption,
        Option<bool> verboseOption,
        Option<bool> dryRunOption)
    {
        ArgumentNullException.ThrowIfNull(rootCommand);
        ArgumentNullException.ThrowIfNull(configOption);
        ArgumentNullException.ThrowIfNull(debugOption);
        ArgumentNullException.ThrowIfNull(verboseOption);
        ArgumentNullException.ThrowIfNull(dryRunOption);
        var pathArg = new Argument<string>("path", "Path to the directory or file to process");

        // Add override-vault-root option to specify the explicit vault root path when different from the starting path
        var vaultRootOverrideOption = new Option<string?>("--override-vault-root", "Specify the explicit vault root path (overrides the config)");

        // add-nested command
        var addNestedCommand = new Command("add-nested", "Add nested tags based on frontmatter fields");
        addNestedCommand.AddArgument(pathArg);
        addNestedCommand.AddOption(vaultRootOverrideOption);
        addNestedCommand.SetHandler(async context =>
        {
            // Print usage/help if required argument is missing
            var pathValue = context.ParseResult.GetValueForArgument(pathArg);
            if (string.IsNullOrEmpty(pathValue))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation tag add-nested <path> [options]",
                    addNestedCommand.Description ?? string.Empty,
                    string.Join("\n", addNestedCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", addNestedCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string path = pathValue;
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            string? vaultRoot = context.ParseResult.GetValueForOption(vaultRootOverrideOption);
            await this.ProcessTagsAsync(path, "add-nested", config, debug, verbose, dryRun, vaultRoot).ConfigureAwait(false);
        });        // clean-index command
        var cleanIndexCommand = new Command("clean-index", "Clean tags from index files");
        cleanIndexCommand.AddArgument(pathArg);
        cleanIndexCommand.AddOption(vaultRootOverrideOption);
        cleanIndexCommand.SetHandler(async context =>
        {
            var pathValue = context.ParseResult.GetValueForArgument(pathArg);
            if (string.IsNullOrEmpty(pathValue))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation tag clean-index <path> [options]",
                    cleanIndexCommand.Description ?? string.Empty,
                    string.Join("\n", cleanIndexCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", cleanIndexCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string path = pathValue;
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            string? vaultRoot = context.ParseResult.GetValueForOption(vaultRootOverrideOption);
            await this.ProcessTagsAsync(path, "clean-index", config, debug, verbose, dryRun, vaultRoot).ConfigureAwait(false);
        });        // consolidate command
        var consolidateCommand = new Command("consolidate", "Consolidate tags in markdown files");
        consolidateCommand.AddArgument(pathArg);
        consolidateCommand.AddOption(vaultRootOverrideOption);
        consolidateCommand.SetHandler(async context =>
        {
            var pathValue = context.ParseResult.GetValueForArgument(pathArg);
            if (string.IsNullOrEmpty(pathValue))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation tag consolidate <path> [options]",
                    consolidateCommand.Description ?? string.Empty,
                    string.Join("\n", consolidateCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", consolidateCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string path = pathValue;
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            string? vaultRoot = context.ParseResult.GetValueForOption(vaultRootOverrideOption);
            await this.ProcessTagsAsync(path, "consolidate", config, debug, verbose, dryRun, vaultRoot).ConfigureAwait(false);
        });        // restructure-tags command
        var restructureCommand = new Command("restructure", "Restructure tags for consistency");
        restructureCommand.AddArgument(pathArg);
        restructureCommand.AddOption(vaultRootOverrideOption);
        restructureCommand.SetHandler(async context =>
        {
            var pathValue = context.ParseResult.GetValueForArgument(pathArg);
            if (string.IsNullOrEmpty(pathValue))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation tag restructure <path> [options]",
                    restructureCommand.Description ?? string.Empty,
                    string.Join("\n", restructureCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", restructureCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string path = pathValue;
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            string? vaultRoot = context.ParseResult.GetValueForOption(vaultRootOverrideOption);
            await this.ProcessTagsAsync(path, "restructure-tags", config, debug, verbose, dryRun, vaultRoot).ConfigureAwait(false);
        });        // add-example-tags command
        var addExampleCommand = new Command("add-example", "Add example tags to a file");
        addExampleCommand.AddArgument(pathArg);
        addExampleCommand.AddOption(vaultRootOverrideOption);
        addExampleCommand.SetHandler(async context =>
        {
            var pathValue = context.ParseResult.GetValueForArgument(pathArg);
            if (string.IsNullOrEmpty(pathValue))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation tag add-example <path> [options]",
                    addExampleCommand.Description ?? string.Empty,
                    string.Join("\n", addExampleCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", addExampleCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string path = pathValue;
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            string? vaultRoot = context.ParseResult.GetValueForOption(vaultRootOverrideOption);
            await this.ProcessTagsAsync(path, "add-example-tags", config, debug, verbose, dryRun, vaultRoot).ConfigureAwait(false);
        });        // metadata-check command
        var metadataCommand = new Command("metadata-check", "Check and enforce metadata consistency");
        metadataCommand.AddArgument(pathArg);
        metadataCommand.AddOption(vaultRootOverrideOption);
        metadataCommand.SetHandler(async context =>
        {
            var pathValue = context.ParseResult.GetValueForArgument(pathArg);
            if (string.IsNullOrEmpty(pathValue))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation tag metadata-check <path> [options]",
                    metadataCommand.Description ?? string.Empty,
                    string.Join("\n", metadataCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", metadataCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string path = pathValue;
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            string? vaultRoot = context.ParseResult.GetValueForOption(vaultRootOverrideOption);
            await this.ProcessTagsAsync(path, "metadata-check", config, debug, verbose, dryRun, vaultRoot).ConfigureAwait(false);
        });        // update-frontmatter command
        var updateFrontmatterCommand = new Command("update-frontmatter", "Update or add a specific key-value pair in frontmatter");
        updateFrontmatterCommand.AddArgument(pathArg);
        var keyArg = new Argument<string>("key", "The frontmatter key to add or update");
        var valueArg = new Argument<string>("value", "The value to set for the key");
        updateFrontmatterCommand.AddArgument(keyArg);
        updateFrontmatterCommand.AddArgument(valueArg);
        updateFrontmatterCommand.AddOption(vaultRootOverrideOption);
        updateFrontmatterCommand.SetHandler(async context =>
        {
            var pathValue = context.ParseResult.GetValueForArgument(pathArg);
            var keyValue = context.ParseResult.GetValueForArgument(keyArg);
            var valueValue = context.ParseResult.GetValueForArgument(valueArg);
            if (string.IsNullOrEmpty(pathValue) || string.IsNullOrEmpty(keyValue) || string.IsNullOrEmpty(valueValue))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation tag update-frontmatter <path> <key> <value> [options]",
                    updateFrontmatterCommand.Description ?? string.Empty,
                    string.Join("\n", updateFrontmatterCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", updateFrontmatterCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string path = pathValue;
            string key = keyValue;
            string value = valueValue;
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            string? vaultRoot = context.ParseResult.GetValueForOption(vaultRootOverrideOption);
            await this.ProcessUpdateFrontmatterAsync(path, key, value, config, debug, verbose, dryRun, vaultRoot).ConfigureAwait(false);
        });        // diagnose-yaml command
        var diagnoseYamlCommand = new Command("diagnose-yaml", "Diagnose YAML frontmatter issues in markdown files");
        diagnoseYamlCommand.AddArgument(pathArg);
        diagnoseYamlCommand.AddOption(vaultRootOverrideOption);
        diagnoseYamlCommand.SetHandler(async context =>
        {
            var pathValue = context.ParseResult.GetValueForArgument(pathArg);
            if (string.IsNullOrEmpty(pathValue))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation tag diagnose-yaml <path> [options]",
                    diagnoseYamlCommand.Description ?? string.Empty,
                    string.Join("\n", diagnoseYamlCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", diagnoseYamlCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string path = pathValue;
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            string? vaultRoot = context.ParseResult.GetValueForOption(vaultRootOverrideOption);
            await this.DiagnoseYamlAsync(path, config, debug, verbose, vaultRoot).ConfigureAwait(false);
        });

        // Create a parent command for all tag-related operations
        var tagCommand = new Command("tag", "Tag management commands");
        tagCommand.AddCommand(addNestedCommand);
        tagCommand.AddCommand(cleanIndexCommand);
        tagCommand.AddCommand(consolidateCommand);
        tagCommand.AddCommand(restructureCommand);
        tagCommand.AddCommand(addExampleCommand);
        tagCommand.AddCommand(metadataCommand);
        tagCommand.AddCommand(updateFrontmatterCommand);
        tagCommand.AddCommand(diagnoseYamlCommand);

        // Error handler for invalid tag subcommands
        tagCommand.TreatUnmatchedTokensAsErrors = true;
        tagCommand.SetHandler(context =>
        {
            AnsiConsoleHelper.WriteUsage(
                "Usage: notebookautomation tag <subcommand> [options]",
                "Please specify a tag subcommand to execute. Available tag commands:",
                string.Join("\n", tagCommand.Subcommands.Select(cmd => $"  {cmd.Name,-15} {cmd.Description}")) +
                "\n\nRun 'notebookautomation tag [command] --help' for more information on a specific command.");
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
    /// <param name="verbose">Whether verbose output is enabled.</param>    /// <param name="dryRun">Whether to simulate without making changes.</param>
    /// <param name="vaultRoot">Override vault root path.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ProcessTagsAsync(string path, string command, string? configPath, bool debug, bool verbose, bool dryRun, string? vaultRoot = null)
    {
        if (string.IsNullOrEmpty(configPath))
        {
            this.logger.LogErrorWithPath("Configuration is missing or incomplete. Exiting.", "TagCommands.cs");
            return;
        }

        if (string.IsNullOrEmpty(path))
        {
            this.logger.LogErrorWithPath("Path is required: {FilePath}", "TagCommands.cs", path ?? "unknown");
            return;
        }

        if (!Directory.Exists(path) && !File.Exists(path))
        {
            this.logger.LogErrorWithPath("Path does not exist: {FilePath}", "TagCommands.cs", path);
            return;
        }

        // Validate that the path is within the configured vault root or that vault root override is provided
        if (string.IsNullOrEmpty(vaultRoot))
        {
            string? configuredVaultRoot = this.appConfig.Paths?.NotebookVaultFullpathRoot;
            if (!string.IsNullOrEmpty(configuredVaultRoot))
            {
                string normalizedPath = Path.GetFullPath(path).Replace('\\', '/');
                string normalizedConfigRoot = Path.GetFullPath(configuredVaultRoot).Replace('\\', '/');

                if (!normalizedPath.StartsWith(normalizedConfigRoot, StringComparison.OrdinalIgnoreCase))
                {
                    string errorMessage = $"Error: The specified path '{path}' is not within the configured vault root '{configuredVaultRoot}'.\n" +
                                         $"To process files outside the configured vault root, use the --override-vault-root flag:\n" +
                                         $"  tag {command} \"{path}\" --override-vault-root \"{path}\"";

                    AnsiConsoleHelper.WriteError(errorMessage);
                    this.logger.LogErrorWithPath(
                        "Path validation failed: {FilePath} is not within configured vault root {VaultRoot}",
                        "TagCommands.cs", path, configuredVaultRoot);
                    return;
                }
            }
        }

        this.logger.LogInformationWithPath("Executing tag command: {Command} on path: {FilePath}", command, path, "TagCommands.cs");

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
            AnsiConsoleHelper.WriteInfo($"Using config file: {activeConfigPath}\n");                // Create a new TagProcessor with command-specific options

            // For TagProcessor we need to get the IYamlHelper from DI
            var tagProcessorLogger = loggerFactory.CreateLogger<TagProcessor>();
            var yamlHelper = serviceProvider.GetRequiredService<IYamlHelper>();
            var tagProcessor = new TagProcessor(
                tagProcessorLogger,
                failedLogger,
                yamlHelper,
                dryRun,
                verbose);

            switch (command.ToLowerInvariant())
            {
                case "add-nested":
                    var stats = await tagProcessor.ProcessDirectoryAsync(path).ConfigureAwait(false);
                    LogStats(logger, stats);
                    break;

                case "clean-index":
                    logger.LogInformationWithPath("Clean index functionality uses the same processor", "TagCommands.cs");
                    stats = await tagProcessor.ProcessDirectoryAsync(path).ConfigureAwait(false);
                    LogStats(logger, stats);
                    break;

                case "consolidate":
                    logger.LogInformationWithPath("Consolidate tags functionality not yet implemented", "TagCommands.cs");
                    break;

                case "restructure-tags":
                    stats = await tagProcessor.RestructureTagsInDirectoryAsync(path).ConfigureAwait(false);
                    LogStats(logger, stats);
                    break;

                case "add-example-tags":
                    var success = await tagProcessor.AddExampleTagsToFileAsync(path).ConfigureAwait(false);
                    logger.LogInformationWithPath(success ? "Example tags added." : "Failed to add example tags.", "TagCommands.cs");
                    break;

                case "metadata-check":
                    stats = await tagProcessor.CheckAndEnforceMetadataConsistencyAsync(path).ConfigureAwait(false);
                    LogStats(logger, stats);
                    break;

                default:
                    logger.LogErrorWithPath("Unknown command: {Command}", "TagCommands.cs", command);
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
    /// Processes the update-frontmatter command with the specified options.
    /// </summary>
    /// <param name="path">The path to the file to process.</param>
    /// <param name="key">The frontmatter key to add or update.</param>
    /// <param name="value">The value to set for the key.</param>
    /// <param name="configPath">The optional path to the configuration file.</param>
    /// <param name="debug">Whether debug mode is enabled.</param>
    /// <param name="verbose">Whether verbose output is enabled.</param>    /// <param name="dryRun">Whether to simulate without making changes.</param>
    /// <param name="vaultRoot">Override vault root path.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ProcessUpdateFrontmatterAsync(string path, string key, string value, string? configPath, bool debug, bool verbose, bool dryRun, string? vaultRoot = null)
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

            // Get AppConfig for vault validation
            var appConfig = serviceProvider.GetRequiredService<AppConfig>();

            // Validate that the path is within the configured vault root or that vault root override is provided
            if (string.IsNullOrEmpty(vaultRoot))
            {
                string? configuredVaultRoot = appConfig.Paths?.NotebookVaultFullpathRoot;
                if (!string.IsNullOrEmpty(configuredVaultRoot))
                {
                    string normalizedPath = Path.GetFullPath(path).Replace('\\', '/');
                    string normalizedConfigRoot = Path.GetFullPath(configuredVaultRoot).Replace('\\', '/');

                    if (!normalizedPath.StartsWith(normalizedConfigRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        string errorMessage = $"Error: The specified path '{path}' is not within the configured vault root '{configuredVaultRoot}'.\n" +
                                             $"To process files outside the configured vault root, use the --override-vault-root flag:\n" +
                                             $"  tag update-frontmatter \"{path}\" --override-vault-root \"{path}\"";

                        AnsiConsoleHelper.WriteError(errorMessage);
                        logger.LogError(
                            "Path validation failed: {FilePath} is not within configured vault root {VaultRoot}",
                            path, configuredVaultRoot);
                        return;
                    }
                }
            }

            logger.LogInformation("Executing update-frontmatter command on path: {Path}, key: {Key}, value: {Value}", path, key, value); // Create a new TagProcessor with command-specific options
            var tagProcessorLogger = loggerFactory.CreateLogger<TagProcessor>();
            var yamlHelper = serviceProvider.GetRequiredService<IYamlHelper>();
            var tagProcessor = new TagProcessor(
                tagProcessorLogger,
                failedLogger,
                yamlHelper,
                dryRun,
                verbose);

            AnsiConsoleHelper.WriteInfo($"Updating frontmatter key '{key}' to value '{value}' in {path}...");
            if (dryRun)
            {
                AnsiConsoleHelper.WriteInfo("[DRY RUN] No files will be modified");
            }

            var stats = await tagProcessor.UpdateFrontmatterKeyAsync(path, key, value).ConfigureAwait(false);
            LogStats(logger, stats);

            if (stats["FilesModified"] > 0)
            {
                AnsiConsoleHelper.WriteSuccess($"Successfully updated {stats["FilesModified"]} of {stats["FilesProcessed"]} files");
            }
            else if (stats["FilesProcessed"] > 0)
            {
                AnsiConsoleHelper.WriteInfo($"No files needed updates out of {stats["FilesProcessed"]} files processed");
            }
            else
            {
                AnsiConsoleHelper.WriteWarning($"No markdown files found at {path}");
            }

            if (stats["FilesWithErrors"] > 0)
            {
                AnsiConsoleHelper.WriteError($"Encountered errors in {stats["FilesWithErrors"]} files. Check the log for details.");
            }
        }
        catch (Exception ex)
        {
            AnsiConsoleHelper.WriteError($"Error processing command: {ex.Message}");
        }
    }

    /// <summary>
    /// Diagnoses YAML frontmatter issues in markdown files.
    /// </summary>
    /// <param name="path">Path to the directory to process.</param>
    /// <param name="configPath">Optional path to configuration file.</param>
    /// <param name="debug">Whether to output debug information.</param>             /// <param name="verbose">Whether to output verbose information.</param>
    /// <param name="vaultRoot">Override vault root path.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DiagnoseYamlAsync(string path, string? configPath, bool debug, bool verbose, string? vaultRoot = null)
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
            var failedLogger = loggingService.FailedLogger;            // Always show the active config file being used
            string activeConfigPath = configPath ?? AppConfig.FindConfigFile() ?? "config.json";
            AnsiConsoleHelper.WriteInfo($"Using config file: {activeConfigPath}\n");

            // Get AppConfig for vault validation
            var appConfig = serviceProvider.GetRequiredService<AppConfig>();

            // Validate that the path is within the configured vault root or that vault root override is provided
            if (string.IsNullOrEmpty(vaultRoot))
            {
                string? configuredVaultRoot = appConfig.Paths?.NotebookVaultFullpathRoot;
                if (!string.IsNullOrEmpty(configuredVaultRoot))
                {
                    string normalizedPath = Path.GetFullPath(path).Replace('\\', '/');
                    string normalizedConfigRoot = Path.GetFullPath(configuredVaultRoot).Replace('\\', '/');

                    if (!normalizedPath.StartsWith(normalizedConfigRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        string errorMessage = $"Error: The specified path '{path}' is not within the configured vault root '{configuredVaultRoot}'.\n" +
                                             $"To process files outside the configured vault root, use the --override-vault-root flag:\n" +
                                             $"  tag diagnose-yaml \"{path}\" --override-vault-root \"{path}\"";

                        AnsiConsoleHelper.WriteError(errorMessage);
                        logger.LogError(
                            "Path validation failed: {FilePath} is not within configured vault root {VaultRoot}",
                            path, configuredVaultRoot);
                        return;
                    }
                }
            }

            AnsiConsoleHelper.WriteInfo("[bold]Diagnosing YAML frontmatter issues in markdown files...[/]");
            AnsiConsoleHelper.WriteInfo($"Path: [cyan]{path}[/]");

            // Create processor
            var tagProcessorLogger = loggerFactory.CreateLogger<TagProcessor>();
            var yamlHelper = serviceProvider.GetRequiredService<IYamlHelper>();
            var processor = new TagProcessor(tagProcessorLogger, failedLogger, yamlHelper, false, verbose);

            // Run diagnosis
            var results = await processor.DiagnoseFrontmatterIssuesAsync(path).ConfigureAwait(false);

            // Display results
            if (results.Count > 0)
            {
                AnsiConsoleHelper.WriteWarning($"Found {results.Count} files with YAML issues:");

                foreach (var (filePath, message) in results)
                {
                    AnsiConsoleHelper.WriteWarning($"File: {filePath}");
                    AnsiConsoleHelper.WriteWarning($"Issue: {message}");
                    Console.WriteLine();
                }

                AnsiConsoleHelper.WriteInfo("Suggestions to fix YAML issues:");
                Console.WriteLine("1. Ensure your frontmatter has proper YAML syntax");
                Console.WriteLine("2. Check for missing colons after keys");
                Console.WriteLine("3. Verify proper indentation in nested structures");
                Console.WriteLine("4. Ensure values with special characters are properly quoted");
                Console.WriteLine("5. Confirm frontmatter is enclosed by triple dashes (---) on separate lines");
            }
            else
            {
                AnsiConsoleHelper.WriteSuccess("No YAML frontmatter issues found!");
            }
        }
        catch (Exception ex)
        {
            AnsiConsoleHelper.WriteError($"Error diagnosing YAML issues: {ex.Message}");
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
    private static void LogStats(ILogger logger, Dictionary<string, int> stats)
    {
        logger.LogInformation("Tag processing completed with the following statistics:");
        logger.LogInformation("- Files processed: {Count}", stats.GetValueOrDefault("FilesProcessed", 0));
        logger.LogInformation("- Files modified: {Count}", stats.GetValueOrDefault("FilesModified", 0));
        logger.LogInformation("- Tags added: {Count}", stats.GetValueOrDefault("TagsAdded", 0));
        logger.LogInformation("- Files with errors: {Count}", stats.GetValueOrDefault("FilesWithErrors", 0));
    }
}
