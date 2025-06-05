using System.Runtime.CompilerServices;

using NotebookAutomation.Cli.Utilities;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.Vault;
using NotebookAutomation.Core.Utils;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace NotebookAutomation.Cli.Commands;
/// <summary>
/// Provides CLI commands for managing and processing Obsidian vault directories.
/// 
/// This class registers the 'vault' command group and its subcommands for vault management,
/// including generating index files and ensuring metadata consistency across markdown files.
/// </summary>
/// <remarks>
/// The vault commands manage the overall organization and structure of an Obsidian vault,
/// which is a collection of markdown files and related assets. These commands help maintain
/// proper structure and metadata consistency throughout the vault.
/// </remarks>
public class VaultCommands
{
    private readonly ILogger<VaultCommands> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AppConfig _appConfig;

    /// <summary>
    /// Initializes a new instance of the VaultCommands class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public VaultCommands(ILogger<VaultCommands> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _appConfig = serviceProvider.GetRequiredService<AppConfig>();
        _logger.LogInformationWithPath("Vault command initialized", "VaultCommands.cs");
    }

    /// <summary>
    /// Registers all vault-related commands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to add vault commands to.</param>
    /// <param name="configOption">The global config file option.</param>
    /// <param name="debugOption">The global debug option.</param>
    /// <param name="verboseOption">The global verbose output option.</param>
    /// <param name="dryRunOption">The global dry run option to simulate actions without making changes.</param>
    public void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
    {
        var pathArg = new Argument<string>("path", "Path to the vault directory");
        var vaultCommand = new Command("vault", "Vault management commands");

        var generateIndexCommand = new Command("generate-index", "Generate a vault index file");
        generateIndexCommand.AddArgument(pathArg);
        generateIndexCommand.SetHandler(async context =>
        {
            // If path argument is missing, print usage and return
            if (string.IsNullOrWhiteSpace(context.ParseResult.GetValueForArgument(pathArg)))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: vault generate-index <path>",
                    "Generate a vault index for the specified directory.",
                    "  <path>    Path to the vault directory (required)"
                );
                return;
            }
            string path = context.ParseResult.GetValueForArgument(pathArg);
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            await ExecuteVaultCommandAsync("generate-index", path, config, debug, verbose, dryRun);
        });

        var ensureMetadataCommand = new Command("ensure-metadata", "Update YAML frontmatter with program/course/class metadata based on directory structure");
        ensureMetadataCommand.AddArgument(pathArg);
        ensureMetadataCommand.SetHandler(async context =>
        {
            string path = context.ParseResult.GetValueForArgument(pathArg);
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            await ExecuteVaultCommandAsync("ensure-metadata", path, config, debug, verbose, dryRun);
        });

        vaultCommand.AddCommand(generateIndexCommand);
        vaultCommand.AddCommand(ensureMetadataCommand);
        rootCommand.AddCommand(vaultCommand);
    }

    /// <summary>
    /// Executes a vault command on the specified path.
    /// </summary>
    /// <param name="command">The vault command to execute: generate-index or ensure-metadata.</param>
    /// <param name="path">Path to the vault directory.</param>
    /// <param name="configPath">Path to the configuration file.</param>
    /// <param name="debug">Whether to enable debug logging.</param>
    /// <param name="verbose">Whether to enable verbose output.</param>
    /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method handles the following commands:
    /// <list type="bullet">
    /// <item><description>generate-index: Creates index files for each directory in the vault</description></item>
    /// <item><description>ensure-metadata: Updates YAML frontmatter with program/course/class metadata based on directory hierarchy (Program/Course/Class structure)</description></item>
    /// </list>        /// </remarks>
    private async Task ExecuteVaultCommandAsync(
        string command,
        string path,
        string? configPath,
        bool debug,
        bool verbose,
        bool dryRun)
    {
        // Configuration is loaded via DI, so we don't need to validate configPath
        // Just ensure we have a valid _appConfig instance
        if (_appConfig == null)
        {
            _logger.LogErrorWithPath("Configuration is missing or incomplete. Exiting.", "VaultCommands.cs");
            return;
        }

        if (string.IsNullOrEmpty(path))
        {
            _logger.LogErrorWithPath("Path is required: {FilePath}", "VaultCommands.cs", path ?? "unknown");
            return;
        }

        if (!Directory.Exists(path))
        {
            _logger.LogErrorWithPath("Vault directory does not exist: {FilePath}", "VaultCommands.cs", path);
            return;
        }

        _logger.LogInformationWithPath("Executing vault command: {Command} on path: {FilePath}", "VaultCommands.cs", command, path);
        _logger.LogDebugWithPath("Debugging vault command", "VaultCommands.cs");

        try
        {
            switch (command)
            {
                case "generate-index":
                    await ExecuteGenerateIndexAsync(path, dryRun);
                    break;
                case "ensure-metadata":
                    await ExecuteEnsureMetadataAsync(path, dryRun, verbose);
                    break;
                default:
                    _logger.LogErrorWithPath("Unknown vault command: {Command}", "VaultCommands.cs", command);
                    return;
            }

            _logger.LogInformationWithPath("Vault command completed successfully.", "VaultCommands.cs");
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithPath(ex, "An error occurred during vault command execution.", "VaultCommands.cs");
            _logger.LogErrorWithPath("Error in vault command", "VaultCommands.cs", ex);
        }
    }

    /// <summary>
    /// Executes the generate-index command (placeholder implementation).
    /// </summary>
    private async Task ExecuteGenerateIndexAsync(string path, bool dryRun)
    {
        // TODO: Implement generate-index functionality
        _logger.LogWarningWithPath("Generate-index command is not yet implemented", "VaultCommands.cs");
        await Task.CompletedTask;
    }
    /// <summary>
    /// Executes the ensure-metadata command using the MetadataEnsureBatchProcessor.
    /// </summary>        
    private async Task ExecuteEnsureMetadataAsync(string path, bool dryRun, bool verbose)
    {
        try
        {
            _logger.LogInformationWithPath("Starting metadata ensure process for vault: {VaultPath}", "VaultCommands.cs", path);

            // Get the batch processor from DI
            var batchProcessor = _serviceProvider.GetRequiredService<MetadataEnsureBatchProcessor>();

            // Use the same approach as video commands for consistent animated progress output
            var result = await AnsiConsoleHelper.WithStatusAsync<MetadataBatchResult>(
                async (updateStatus) =>
                {                        // Hook up progress events to update the status display
                    batchProcessor.ProcessingProgressChanged += (sender, e) =>
                    {
                        // Escape any markup to avoid Spectre.Console parsing issues
                        string safeStatus = e.Status.Replace("[", "[[").Replace("]", "]]");
                        updateStatus(safeStatus);
                    };

                    // Execute the batch processing
                    return await batchProcessor.EnsureMetadataAsync(
                        vaultPath: path,
                        dryRun: dryRun,
                        forceOverwrite: false, // Could be made configurable via CLI option
                        retryFailed: false     // Could be made configurable via CLI option
                    );
                },
                $"Processing metadata for vault: {Path.GetFileName(path)}"
            );

            // Log the results
            if (result.Success)
            {
                // Report completion message that's similar to video/pdf commands
                string prefix = dryRun ? "[DRY RUN] " : "";

                // Write a clear summary message
                AnsiConsoleHelper.WriteSuccess($"\n{prefix}Metadata processing completed");
                AnsiConsoleHelper.WriteInfo($"  Files processed: {result.ProcessedFiles}");
                AnsiConsoleHelper.WriteInfo($"  Files skipped: {result.SkippedFiles}");
                if (result.FailedFiles > 0)
                {
                    AnsiConsoleHelper.WriteWarning($"  Files failed: {result.FailedFiles}");
                }
                AnsiConsoleHelper.WriteInfo($"  Total files: {result.TotalFiles}");                      // For dry run, indicate where detailed changes can be found
                if (dryRun)
                {
                    AnsiConsoleHelper.WriteInfo("\nDetailed metadata changes are available in the log file:");

                    // Get the actual log file path from the logging service
                    var loggingService = _serviceProvider.GetRequiredService<ILoggingService>();
                    var logFilePath = loggingService.CurrentLogFilePath;

                    if (!string.IsNullOrEmpty(logFilePath))
                    {
                        AnsiConsoleHelper.WriteInfo($"  {logFilePath}");
                    }
                    else if (_appConfig?.Paths?.LoggingDir != null)
                    {
                        // Fallback to manual construction if service doesn't provide path
                        string logPath = _appConfig.Paths.LoggingDir;
                        string logFile = Path.Combine(logPath, $"notebookautomation.core_{DateTime.Now:yyyyMMdd}.log");
                        AnsiConsoleHelper.WriteInfo($"  {logFile}");
                    }

                    // Only show verbose tip when not already running in verbose mode
                    if (!verbose)
                    {
                        AnsiConsoleHelper.WriteInfo("\nTip: Run with --verbose to see more details about proposed changes.");
                    }
                }

                // Still log to the file for record-keeping
                _logger.LogInformationWithPath(
                    "{Prefix}Metadata processing completed: {Processed} processed, {Skipped} skipped, {Failed} failed out of {Total} total files",
                    "VaultCommands.cs",
                    prefix, result.ProcessedFiles, result.SkippedFiles, result.FailedFiles, result.TotalFiles);

                if (result.FailedFiles > 0)
                {
                    _logger.LogWarningWithPath("Some files failed to process. Check the logs and failed_metadata_files.txt for details.", "VaultCommands.cs");
                }
            }
            else
            {
                AnsiConsoleHelper.WriteError($"Metadata processing failed: {result.ErrorMessage ?? "Unknown error"}");
                _logger.LogErrorWithPath("Metadata processing failed: {ErrorMessage}", "VaultCommands.cs", result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            AnsiConsoleHelper.WriteError($"Failed to execute ensure-metadata command: {ex.Message}");
            _logger.LogErrorWithPath(ex, "Failed to execute ensure-metadata command", "VaultCommands.cs");
            throw;
        }
    }
}
