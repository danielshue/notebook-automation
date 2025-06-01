using NotebookAutomation.Cli.Utilities;
using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace NotebookAutomation.Cli.Commands
{
    /// <summary>
    /// Provides CLI commands for interacting with OneDrive through Microsoft Graph API.
    /// 
    /// This class registers the 'onedrive' command group and its subcommands for OneDrive operations,
    /// including listing files, downloading files from OneDrive, uploading files to OneDrive,
    /// searching for files, and synchronizing files between local and OneDrive locations.
    /// </summary>
    /// <remarks>
    /// The OneDrive commands utilize the <see cref="OneDriveService"/> from the Core library
    /// to perform the actual OneDrive operations. These commands require proper authentication
    /// with Microsoft Graph API, which is handled by the OneDriveService.
    /// </remarks>
    public class OneDriveCommands
    {
        private readonly ILogger<OneDriveCommands> _logger;

        public OneDriveCommands(ILogger<OneDriveCommands> logger)
        {
            _logger = logger;
            _logger.LogInformationWithPath("OneDrive command initialized", "OneDriveCommands.cs");
        }

        /// <summary>
        /// Registers all OneDrive-related commands with the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add OneDrive commands to.</param>
        /// <param name="configOption">The global config file option.</param>
        /// <param name="debugOption">The global debug option.</param>
        /// <param name="verboseOption">The global verbose output option.</param>
        /// <param name="dryRunOption">The global dry run option to simulate actions without making changes.</param>
        public void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
        {
            var oneDriveCommand = new Command("onedrive", "OneDrive file management commands");

            // List files command
            var listCommand = new Command("list", "List files and folders in OneDrive");
            var pathArgument = new Argument<string>("path", "Path to list (default: root)") { Arity = ArgumentArity.ZeroOrOne };
            listCommand.AddArgument(pathArgument);
            listCommand.SetHandler(async (InvocationContext context) =>
            {
                // Print usage/help if required argument is missing (path is optional, but show help if both path and config are missing)
                var path = context.ParseResult.GetValueForArgument(pathArgument);
                if (string.IsNullOrEmpty(path))
                {
                    AnsiConsoleHelper.WriteUsage(
                        "Usage: notebookautomation onedrive list [path] [options]",
                        listCommand.Description ?? string.Empty,
                        string.Join("\n", listCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                        "\n" + string.Join("\n", listCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}"))
                    );
                    return;
                }
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

                await this.ExecuteOneDriveCommandAsync("list", path, null, config, debug, verbose, dryRun);
            });

            // Download command
            var downloadCommand = new Command("download", "Download a file from OneDrive");
            var remotePathArgument = new Argument<string>("remote-path", "Path in OneDrive to download from");
            var localPathArgument = new Argument<string>("local-path", "Local path to save to");
            downloadCommand.AddArgument(remotePathArgument);
            downloadCommand.AddArgument(localPathArgument);
            downloadCommand.SetHandler(async (InvocationContext context) =>
            {
                string remotePath = context.ParseResult.GetValueForArgument(remotePathArgument);
                string localPath = context.ParseResult.GetValueForArgument(localPathArgument);
                if (string.IsNullOrEmpty(remotePath) || string.IsNullOrEmpty(localPath))
                {
                    AnsiConsoleHelper.WriteUsage(
                        "Usage: notebookautomation onedrive download <remote-path> <local-path> [options]",
                        downloadCommand.Description ?? string.Empty,
                        string.Join("\n", downloadCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                        "\n" + string.Join("\n", downloadCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}"))
                    );
                    return;
                }
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

                await this.ExecuteOneDriveCommandAsync("download", remotePath, localPath, config, debug, verbose, dryRun);
            });

            // Upload command
            var uploadCommand = new Command("upload", "Upload a file to OneDrive");
            var uploadLocalPath = new Argument<string>("local-path", "Local file to upload");
            var uploadRemotePath = new Argument<string>("remote-path", "Destination path in OneDrive");
            uploadCommand.AddArgument(uploadLocalPath);
            uploadCommand.AddArgument(uploadRemotePath);
            uploadCommand.SetHandler(async (InvocationContext context) =>
            {
                string localPath = context.ParseResult.GetValueForArgument(uploadLocalPath);
                string remotePath = context.ParseResult.GetValueForArgument(uploadRemotePath);
                if (string.IsNullOrEmpty(localPath) || string.IsNullOrEmpty(remotePath))
                {
                    AnsiConsoleHelper.WriteUsage(
                        "Usage: notebookautomation onedrive upload <local-path> <remote-path> [options]",
                        uploadCommand.Description ?? string.Empty,
                        string.Join("\n", uploadCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                        "\n" + string.Join("\n", uploadCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}"))
                    );
                    return;
                }
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

                await this.ExecuteOneDriveCommandAsync("upload", localPath, remotePath, config, debug, verbose, dryRun);
            });

            // Search command
            var searchCommand = new Command("search", "Search for files in OneDrive");
            var queryArgument = new Argument<string>("query", "Search query");
            searchCommand.AddArgument(queryArgument);
            searchCommand.SetHandler(async (InvocationContext context) =>
            {
                string query = context.ParseResult.GetValueForArgument(queryArgument);
                if (string.IsNullOrEmpty(query))
                {
                    AnsiConsoleHelper.WriteUsage(
                        "Usage: notebookautomation onedrive search <query> [options]",
                        searchCommand.Description ?? string.Empty,
                        string.Join("\n", searchCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                        "\n" + string.Join("\n", searchCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}"))
                    );
                    return;
                }
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

                await this.ExecuteOneDriveCommandAsync("search", query, null, config, debug, verbose, dryRun);
            });

            // Sync command with options for direction, etc.
            var syncCommand = new Command("sync", "Sync files between local and OneDrive");
            var syncLocalPath = new Argument<string>("local-path", "Local folder to sync");
            var syncRemotePath = new Argument<string>("remote-path", "OneDrive folder to sync") { Arity = ArgumentArity.ZeroOrOne };
            var directionOption = new Option<string>(
                aliases: new[] { "--direction", "-d" },
                description: "Sync direction: up (local to OneDrive), down (OneDrive to local), or both",
                getDefaultValue: () => "both");
            syncCommand.AddArgument(syncLocalPath);
            syncCommand.AddArgument(syncRemotePath);
            syncCommand.AddOption(directionOption);
            syncCommand.SetHandler(async (InvocationContext context) =>
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
                        "\n" + string.Join("\n", syncCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}"))
                    );
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
                    new[] { (Key: "direction", Value: direction ?? "both") }
                );
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
                    logger.LogError("Microsoft Graph configuration is missing or incomplete. Exiting.");
                    return;
                }

                logger.LogInformation("Executing OneDrive command: {Command}", command);

                if (dryRun)
                {
                    logger.LogInformation("[DRY RUN] No changes will be made");
                }

                if (verbose)
                {
                    logger.LogInformation("Verbose output enabled");
                }

                // Execute the command with the relevant arguments
                switch (command.ToLowerInvariant())
                {
                    case "list":
                        logger.LogInformationWithPath("Listing files at path: {FilePath}", arg1 ?? "root");
                        // TODO: Implement with oneDriveService.ListFilesAsync(arg1);
                        await Task.Delay(100); // Placeholder for actual implementation
                        break;

                    case "download":
                        if (arg1 == null || arg2 == null)
                        {
                            logger.LogWarning("One or more arguments are null. Skipping logging.");
                        }
                        else
                        {
                            logger.LogInformationWithPath("Downloading from {FilePath} to {FilePath}", arg1, arg2);
                        }
                        // TODO: Implement with oneDriveService.DownloadFileAsync(arg1, arg2);
                        await Task.Delay(100); // Placeholder for actual implementation
                        break;

                    case "upload":
                        if (arg1 == null || arg2 == null)
                        {
                            logger.LogWarning("One or more arguments are null. Skipping logging.");
                        }
                        else
                        {
                            logger.LogInformationWithPath("Uploading from {FilePath} to {FilePath}", arg1, arg2);
                        }
                        // TODO: Implement with oneDriveService.UploadFileAsync(arg1, arg2);
                        await Task.Delay(100); // Placeholder for actual implementation
                        break;

                    case "search":
                        logger.LogInformation("Searching for: {Query}", arg1);
                        // TODO: Implement with oneDriveService.SearchFilesAsync(arg1);
                        await Task.Delay(100); // Placeholder for actual implementation
                        break;

                    case "sync":
                        var direction = extraOptions.FirstOrDefault(o => o.Key == "direction").Value ?? "both";
                        logger.LogInformation("Syncing between {LocalPath} and {RemotePath} (direction: {Direction})",
                            arg1, arg2, direction);
                        // TODO: Implement with oneDriveService.SyncFilesAsync(arg1, arg2, direction);
                        await Task.Delay(100); // Placeholder for actual implementation
                        break;

                    default:
                        logger.LogError("Unknown command: {Command}", command);
                        break;
                }

                logger.LogInformation("Command completed successfully");
            }
            catch (Exception ex)
            {
                AnsiConsoleHelper.WriteError($"Error processing OneDrive command: {ex.Message}");
                if (debug)
                {
                    AnsiConsoleHelper.WriteError(ex.ToString());
                }
                _logger.LogErrorWithPath("Error in OneDrive command", "OneDriveCommands.cs", ex);
            }
        }

        public async Task ExecuteOneDriveCommandAsync(string command, string? arg1, string? arg2, string? config, bool debug, bool verbose, bool dryRun)
        {
            if (string.IsNullOrEmpty(config))
            {
                _logger.LogError("Microsoft Graph configuration is missing or incomplete. Exiting.");
                return;
            }

            _logger.LogInformation("Executing OneDrive command: {Command}", command);

            if (dryRun)
            {
                _logger.LogInformation("[DRY RUN] No changes will be made");
            }

            if (verbose)
            {
                _logger.LogInformation("Verbose output enabled");
            }

            _logger.LogDebugWithPath("Debugging OneDrive command", "OneDriveCommands.cs");

            switch (command.ToLower())
            {
                case "list":
                    if (!string.IsNullOrEmpty(arg1))
                    {
                        _logger.LogInformationWithPath("Listing files at path: {FilePath}", arg1);
                    }
                    break;

                case "download":
                    if (arg1 == null || arg2 == null)
                    {
                        _logger.LogWarning("One or more arguments are null. Skipping logging.");
                    }
                    else
                    {
                        _logger.LogInformationWithPath("Downloading from {FilePath} to {FilePath}", arg1, arg2);
                    }
                    break;

                case "upload":
                    if (arg1 == null || arg2 == null)
                    {
                        _logger.LogWarning("One or more arguments are null. Skipping logging.");
                    }
                    else
                    {
                        _logger.LogInformationWithPath("Uploading from {FilePath} to {FilePath}", arg1, arg2);
                    }
                    break;

                case "search":
                    if (!string.IsNullOrEmpty(arg1))
                    {
                        _logger.LogInformation("Searching for: {Query}", arg1);
                    }
                    break;

                case "sync":
                    if (!string.IsNullOrEmpty(arg1) && !string.IsNullOrEmpty(arg2))
                    {
                        _logger.LogInformation("Syncing between {LocalPath} and {RemotePath} (direction: {Direction})",
                            arg1 ?? "unknown", arg2 ?? "unknown", "bidirectional");
                    }
                    break;

                default:
                    _logger.LogError("Unknown command: {Command}", command);
                    break;
            }

            await Task.CompletedTask;

            _logger.LogInformation("Command completed successfully");
        }
    }
}
