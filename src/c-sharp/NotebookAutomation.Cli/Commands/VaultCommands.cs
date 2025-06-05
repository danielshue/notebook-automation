using System.Runtime.CompilerServices;

using NotebookAutomation.Cli.Utilities;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.Vault;
using NotebookAutomation.Core.Utils;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace NotebookAutomation.Cli.Commands;

/// <summary>
/// Provides CLI commands for managing and processing Obsidian vault directories.
/// </summary>
/// <remarks>
/// This class registers the 'vault' command group and its subcommands for vault management,
/// including generating index files and ensuring metadata consistency across markdown files.
/// </remarks>
public class VaultCommands
{
    private readonly ILogger<VaultCommands> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AppConfig _appConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultCommands"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information and errors.</param>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    public VaultCommands(ILogger<VaultCommands> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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

        var pathArg = new Argument<string>("path", "Path to the vault directory");
        var vaultCommand = new Command("vault", "Vault management commands");

        var generateIndexCommand = new Command("generate-index", "Generate a vault index file");
        generateIndexCommand.AddArgument(pathArg);
        generateIndexCommand.SetHandler(async context =>
        {
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
    /// </list>
    /// </remarks>
    private async Task ExecuteVaultCommandAsync(
        string command,
        string path,
        string? configPath,
        bool debug,
        bool verbose,
        bool dryRun)
    {
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
    /// <param name="path">Path to the vault directory.</param>
    /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExecuteGenerateIndexAsync(string path, bool dryRun)
    {
        _logger.LogWarningWithPath("Generate-index command is not yet implemented", "VaultCommands.cs");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes the ensure-metadata command using the MetadataEnsureBatchProcessor.
    /// </summary>
    /// <param name="path">Path to the vault directory.</param>
    /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
    /// <param name="verbose">Whether to enable verbose output.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExecuteEnsureMetadataAsync(string path, bool dryRun, bool verbose)
    {
        try
        {
            _logger.LogInformationWithPath("Starting metadata ensure process for vault: {VaultPath}", "VaultCommands.cs", path);

            var batchProcessor = _serviceProvider.GetRequiredService<MetadataEnsureBatchProcessor>();

            var result = await AnsiConsoleHelper.WithStatusAsync(
                async (updateStatus) =>
                {
                    batchProcessor.ProcessingProgressChanged += (sender, e) =>
                    {
                        string safeStatus = e.Status.Replace("[", "[[").Replace("]", "]]");
                        updateStatus(safeStatus);
                    };

                    return await batchProcessor.EnsureMetadataAsync(
                        vaultPath: path,
                        dryRun: dryRun,
                        forceOverwrite: false,
                        retryFailed: false
                    );
                },
                $"Processing metadata for vault: {Path.GetFileName(path)}"
            );

            if (result.Success)
            {
                string prefix = dryRun ? "[DRY RUN] " : "";

                AnsiConsoleHelper.WriteSuccess($"\n{prefix}Metadata processing completed");
                AnsiConsoleHelper.WriteInfo($"  Files processed: {result.ProcessedFiles}");
                AnsiConsoleHelper.WriteInfo($"  Files skipped: {result.SkippedFiles}");
                if (result.FailedFiles > 0)
                {
                    AnsiConsoleHelper.WriteWarning($"  Files failed: {result.FailedFiles}");
                }
                AnsiConsoleHelper.WriteInfo($"  Total files: {result.TotalFiles}");

                if (dryRun)
                {
                    AnsiConsoleHelper.WriteInfo("\nDetailed metadata changes are available in the log file:");

                    var loggingService = _serviceProvider.GetRequiredService<ILoggingService>();
                    var logFilePath = loggingService.CurrentLogFilePath;

                    if (!string.IsNullOrEmpty(logFilePath))
                    {
                        AnsiConsoleHelper.WriteInfo($"  {logFilePath}");
                    }
                    else if (_appConfig?.Paths?.LoggingDir != null)
                    {
                        string logPath = _appConfig.Paths.LoggingDir;
                        string logFile = Path.Combine(logPath, $"notebookautomation.core_{DateTime.Now:yyyyMMdd}.log");
                        AnsiConsoleHelper.WriteInfo($"  {logFile}");
                    }

                    if (!verbose)
                    {
                        AnsiConsoleHelper.WriteInfo("\nTip: Run with --verbose to see more details about proposed changes.");
                    }
                }

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
