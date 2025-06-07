// <copyright file="OneDriveCommands.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Cli/Commands/OneDriveCommands.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
using System.Runtime.CompilerServices;

using NotebookAutomation.Cli.Utilities;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace NotebookAutomation.Cli.Commands;

/// <summary>
/// Provides CLI commands for interacting with OneDrive through Microsoft Graph API.
/// </summary>
/// <remarks>
/// <para>
/// This class registers the 'onedrive' command group and its subcommands for OneDrive operations,
/// including:
/// <list type="bullet">
/// <item><description>Listing files and folders</description></item>
/// <item><description>Downloading files from OneDrive</description></item>
/// <item><description>Uploading files to OneDrive</description></item>
/// <item><description>Searching for files</description></item>
/// <item><description>Synchronizing files between local and OneDrive locations</description></item>
/// </list>
/// </para>
/// <para>
/// The OneDrive commands utilize the <see cref="OneDriveService"/> from the Core library
/// to perform the actual OneDrive operations. These commands require proper authentication
/// with Microsoft Graph API, which is handled by the OneDriveService.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var rootCommand = new RootCommand();
/// var oneDriveCommands = new OneDriveCommands(logger);
/// oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
/// rootCommand.Invoke("onedrive list --path /Documents");
/// </code>
/// </example>
internal class OneDriveCommands
{
    private readonly ILogger<OneDriveCommands> logger;

    public OneDriveCommands(ILogger<OneDriveCommands> logger)
    {
        this.logger = logger;
        this.logger.LogInformationWithPath("OneDrive command initialized", "OneDriveCommands.cs");
    }

    /// <summary>
    /// Registers all OneDrive-related commands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to add OneDrive commands to.</param>
    /// <param name="configOption">The global config file option.</param>
    /// <param name="debugOption">The global debug option.</param>
    /// <param name="verboseOption">The global verbose output option.</param>
    /// <param name="dryRunOption">The global dry run option to simulate actions without making changes.</param>
    /// <remarks>
    /// <para>
    /// This method adds the 'onedrive' command group to the root command, enabling users to perform
    /// various OneDrive operations. It defines subcommands for listing, downloading, uploading,
    /// searching, and synchronizing files.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var rootCommand = new RootCommand();
    /// oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
    /// rootCommand.Invoke("onedrive list --path /Documents");
    /// </code>
    /// </example>
    public void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
    {
        var oneDriveCommand = new Command("onedrive", "OneDrive file management commands");            // List files command
        var listCommand = new Command("list", "TODO: List files and folders in OneDrive");
        var pathArgument = new Argument<string>("path", "Path to list (default: root)") { Arity = ArgumentArity.ZeroOrOne };
        listCommand.AddArgument(pathArgument);
        listCommand.SetHandler(async context =>
        {
            // Print usage/help if required argument is missing (path is optional, but show help if both path and config are missing)
            var path = context.ParseResult.GetValueForArgument(pathArgument);
            if (string.IsNullOrEmpty(path))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation onedrive list [path] [options]",
                    listCommand.Description ?? string.Empty,
                    string.Join("\n", listCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", listCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            await this.ExecuteOneDriveCommandAsync("list", path, null, config, debug, verbose, dryRun).ConfigureAwait(false);
        });

        // Download command
        var downloadCommand = new Command("download", "TODO: Download a file from OneDrive");
        var remotePathArgument = new Argument<string>("remote-path", "Path in OneDrive to download from");
        var localPathArgument = new Argument<string>("local-path", "Local path to save to");
        downloadCommand.AddArgument(remotePathArgument);
        downloadCommand.AddArgument(localPathArgument);
        downloadCommand.SetHandler(async context =>
        {
            string remotePath = context.ParseResult.GetValueForArgument(remotePathArgument);
            string localPath = context.ParseResult.GetValueForArgument(localPathArgument);
            if (string.IsNullOrEmpty(remotePath) || string.IsNullOrEmpty(localPath))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation onedrive download <remote-path> <local-path> [options]",
                    downloadCommand.Description ?? string.Empty,
                    string.Join("\n", downloadCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", downloadCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            await this.ExecuteOneDriveCommandAsync("download", remotePath, localPath, config, debug, verbose, dryRun).ConfigureAwait(false);
        });            // Upload command
        var uploadCommand = new Command("upload", "TODO: Upload a file to OneDrive");
        var uploadLocalPath = new Argument<string>("local-path", "Local file to upload");
        var uploadRemotePath = new Argument<string>("remote-path", "Destination path in OneDrive");
        uploadCommand.AddArgument(uploadLocalPath);
        uploadCommand.AddArgument(uploadRemotePath);
        uploadCommand.SetHandler(async context =>
        {
            string localPath = context.ParseResult.GetValueForArgument(uploadLocalPath);
            string remotePath = context.ParseResult.GetValueForArgument(uploadRemotePath);
            if (string.IsNullOrEmpty(localPath) || string.IsNullOrEmpty(remotePath))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation onedrive upload <local-path> <remote-path> [options]",
                    uploadCommand.Description ?? string.Empty,
                    string.Join("\n", uploadCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", uploadCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            await this.ExecuteOneDriveCommandAsync("upload", localPath, remotePath, config, debug, verbose, dryRun).ConfigureAwait(false);
        });            // Search command
        var searchCommand = new Command("search", "TODO: Search for files in OneDrive");
        var queryArgument = new Argument<string>("query", "Search query");
        searchCommand.AddArgument(queryArgument);
        searchCommand.SetHandler(async context =>
        {
            string query = context.ParseResult.GetValueForArgument(queryArgument);
            if (string.IsNullOrEmpty(query))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation onedrive search <query> [options]",
                    searchCommand.Description ?? string.Empty,
                    string.Join("\n", searchCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", searchCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            await this.ExecuteOneDriveCommandAsync("search", query, null, config, debug, verbose, dryRun).ConfigureAwait(false);
        });            // Sync command with options for direction, etc.
        var syncCommand = new Command("sync", "TODO: Sync files between local and OneDrive");
        var syncLocalPath = new Argument<string>("local-path", "Local folder to sync");
        var syncRemotePath = new Argument<string>("remote-path", "OneDrive folder to sync") { Arity = ArgumentArity.ZeroOrOne };
        var directionOption = new Option<string>(
            aliases: ["--direction", "-d"],
            description: "Sync direction: up (local to OneDrive), down (OneDrive to local), or both",
            getDefaultValue: () => "both");
        syncCommand.AddArgument(syncLocalPath);
        syncCommand.AddArgument(syncRemotePath);
        syncCommand.AddOption(directionOption);
        syncCommand.SetHandler(async context =>
        {
            string localPath = context.ParseResult.GetValueForArgument(syncLocalPath);
            string? remotePath = context.ParseResult.GetValueForArgument(syncRemotePath);
            string? direction = context.ParseResult.GetValueForOption(directionOption);
            if (string.IsNullOrEmpty(localPath))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation onedrive sync <local-path> [remote-path] [options]",
                    syncCommand.Description ?? string.Empty,
                    string.Join("\n", syncCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", syncCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            await this.ExecuteOneDriveCommandAsync(
                "sync",
                localPath,
                remotePath,
                config,
                debug,
                verbose,
                dryRun,
                [(Key: "direction", Value: direction ?? "both")])
            .ConfigureAwait(false);
        });

        // Add commands to onedrive group
        oneDriveCommand.AddCommand(listCommand);
        oneDriveCommand.AddCommand(downloadCommand);
        oneDriveCommand.AddCommand(uploadCommand);
        oneDriveCommand.AddCommand(searchCommand);
        oneDriveCommand.AddCommand(syncCommand);

        // Add onedrive command to root
        rootCommand.AddCommand(oneDriveCommand);
    }

    /// <summary>
    /// Executes an OneDrive command using the OneDriveService.
    /// </summary>
    /// <param name="command">The OneDrive command to execute: list, download, upload, search, or sync.</param>
    /// <param name="arg1">First argument (e.g., path, local path, etc.).</param>
    /// <param name="arg2">Second argument (optional, e.g., remote path).</param>
    /// <param name="configPath">Path to the configuration file.</param>
    /// <param name="debug">Whether to enable debug logging.</param>
    /// <param name="verbose">Whether to enable verbose output.</param>
    /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
    /// <param name="extraOptions">Additional options as key-value pairs.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExecuteOneDriveCommandAsync(
        string command,
        string? arg1,
        string? arg2,
        string? configPath,
        bool debug,
        bool verbose,
        bool dryRun,
        params (string Key, string Value)[] extraOptions)
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
            var logger = loggerFactory.CreateLogger("OneDriveCommands");
            var loggingService = serviceProvider.GetRequiredService<LoggingService>();
            var failedLogger = loggingService?.FailedLogger;
            var appConfig = serviceProvider.GetRequiredService<AppConfig>();
            var oneDriveService = serviceProvider.GetService<OneDriveService>();

            // Always show the active config file being used
            string activeConfigPath = configPath ?? AppConfig.FindConfigFile() ?? "config.json";
            AnsiConsoleHelper.WriteInfo($"Using config file: {activeConfigPath}\n");

            // Validate Microsoft Graph config before proceeding
            if (!ConfigValidation.RequireMicrosoftGraph(appConfig))
            {
                logger.LogErrorWithPath("Microsoft Graph configuration is missing or incomplete. Exiting.", "OneDriveCommands.cs");
                return;
            }

            logger.LogInformationWithPath("Executing OneDrive command: {Command}", "OneDriveCommands.cs", command);

            if (dryRun)
            {
                logger.LogInformationWithPath("[DRY RUN] No changes will be made", "OneDriveCommands.cs");
            }

            if (verbose)
            {
                logger.LogInformationWithPath("Verbose output enabled", "OneDriveCommands.cs");
            }

            // Execute the command with the relevant arguments
            switch (command.ToLowerInvariant())
            {
                case "list":
                    logger.LogInformationWithPath("Listing files at path: {FilePath}", arg1 ?? "root");

                    // TODO: Implement with oneDriveService.ListFilesAsync(arg1);
                    await Task.Delay(100).ConfigureAwait(false); // Placeholder for actual implementation
                    break;

                case "download":
                    if (arg1 == null || arg2 == null)
                    {
                        logger.LogWarningWithPath("One or more arguments are null. Skipping logging.", "OneDriveCommands.cs");
                    }
                    else
                    {
                        logger.LogInformationWithPath("Downloading from {FilePath} to {FilePath}", arg1, arg2);
                    }

                    // TODO: Implement with oneDriveService.DownloadFileAsync(arg1, arg2);
                    await Task.Delay(100).ConfigureAwait(false); // Placeholder for actual implementation
                    break;

                case "upload":
                    if (arg1 == null || arg2 == null)
                    {
                        logger.LogWarningWithPath("One or more arguments are null. Skipping logging.", "OneDriveCommands.cs");
                    }
                    else
                    {
                        logger.LogInformationWithPath("Uploading from {FilePath} to {FilePath}", arg1, arg2);
                    }

                    // TODO: Implement with oneDriveService.UploadFileAsync(arg1, arg2);
                    await Task.Delay(100).ConfigureAwait(false); // Placeholder for actual implementation
                    break;

                case "search":
                    logger.LogInformationWithPath("Searching for: {Query}", arg1 ?? string.Empty, "OneDriveCommands.cs");

                    // TODO: Implement with oneDriveService.SearchFilesAsync(arg1);
                    await Task.Delay(100).ConfigureAwait(false); // Placeholder for actual implementation
                    break;

                case "sync":
                    var direction = extraOptions.FirstOrDefault(o => o.Key == "direction").Value ?? "both";
                    logger.LogInformationWithPath("Syncing between {LocalPath} and {RemotePath} (direction: {Direction})", arg1 ?? string.Empty, arg2 ?? string.Empty, direction);

                    // TODO: Implement with oneDriveService.SyncFilesAsync(arg1, arg2, direction);
                    await Task.Delay(100).ConfigureAwait(false); // Placeholder for actual implementation
                    break;

                default:
                    logger.LogErrorWithPath("Unknown command: {Command}", "OneDriveCommands.cs", command);
                    break;
            }

            logger.LogInformationWithPath("Command completed successfully", "OneDriveCommands.cs");
        }
        catch (Exception ex)
        {
            AnsiConsoleHelper.WriteError($"Error processing OneDrive command: {ex.Message}");
            if (debug)
            {
                AnsiConsoleHelper.WriteError(ex.ToString());
            }

            this.logger.LogErrorWithPath("Error in OneDrive command", "OneDriveCommands.cs", ex);
        }
    }

    public async Task ExecuteOneDriveCommandAsync(string command, string? arg1, string? arg2, string? config, bool debug, bool verbose, bool dryRun)
    {
        if (string.IsNullOrEmpty(config))
        {
            this.logger.LogErrorWithPath("Microsoft Graph configuration is missing or incomplete. Exiting.", "OneDriveCommands.cs");
            return;
        }

        this.logger.LogInformationWithPath("Executing OneDrive command: {Command}", "OneDriveCommands.cs", command);

        if (dryRun)
        {
            this.logger.LogInformationWithPath("[DRY RUN] No changes will be made", "OneDriveCommands.cs");
        }

        if (verbose)
        {
            this.logger.LogInformationWithPath("Verbose output enabled", "OneDriveCommands.cs");
        }

        this.logger.LogDebugWithPath("Debugging OneDrive command", "OneDriveCommands.cs");

        switch (command.ToLower())
        {
            case "list":
                if (!string.IsNullOrEmpty(arg1))
                {
                    this.logger.LogInformationWithPath("Listing files at path: {FilePath}", arg1);
                }

                break;

            case "download":
                if (arg1 == null || arg2 == null)
                {
                    this.logger.LogWarningWithPath("One or more arguments are null. Skipping logging.", "OneDriveCommands.cs");
                }
                else
                {
                    this.logger.LogInformationWithPath("Downloading from {FilePath} to {FilePath}", arg1, arg2);
                }

                break;

            case "upload":
                if (arg1 == null || arg2 == null)
                {
                    this.logger.LogWarning("One or more arguments are null. Skipping logging.");
                }
                else
                {
                    this.logger.LogInformationWithPath("Uploading from {FilePath} to {FilePath}", arg1, arg2);
                }

                break;

            case "search":
                if (!string.IsNullOrEmpty(arg1))
                {
                    this.logger.LogInformation("Searching for: {Query}", arg1);
                }

                break;

            case "sync":
                if (!string.IsNullOrEmpty(arg1) && !string.IsNullOrEmpty(arg2))
                {
                    this.logger.LogInformation(
                        "Syncing between {LocalPath} and {RemotePath} (direction: {Direction})",
                        arg1 ?? "unknown", arg2 ?? "unknown", "bidirectional");
                }

                break;

            default:
                this.logger.LogError("Unknown command: {Command}", command);
                break;
        }

        await Task.CompletedTask.ConfigureAwait(false);

        this.logger.LogInformation("Command completed successfully");
    }
}
