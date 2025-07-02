// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.CommandLine;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NotebookAutomation.Cli.Utilities;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.Vault;

namespace NotebookAutomation.Cli.Commands;

/// <summary>
/// Provides CLI commands for managing and processing Obsidian vault directories.
/// </summary>
/// <remarks>
/// This class registers the 'vault' command group and its subcommands for vault management,
/// including generating index files and ensuring metadata consistency across markdown files.
/// </remarks>
internal class VaultCommands
{
    private readonly ILogger<VaultCommands> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultCommands"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information and errors.</param>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    public VaultCommands(ILogger<VaultCommands> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }    /// <summary>
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
        ArgumentNullException.ThrowIfNull(dryRunOption); var pathArg = new Argument<string?>("path", "Path to the vault directory to process (defaults to vault root from config)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var vaultRootOverrideOption = new Option<bool>("--override-vault-root", "Use the provided path as the vault root (overrides the config)");// Create generate-index subcommand
        var generateIndexCommand = new Command("generate-index", "Generate index files for each directory in the vault");
        generateIndexCommand.AddArgument(pathArg);
        generateIndexCommand.AddOption(vaultRootOverrideOption);

        // Add --type option for filtering by template types
        var typeOption = new Option<string[]>("--type", "Filter by template types (main, program, course, class, module, lesson)")
        {
            AllowMultipleArgumentsPerToken = true
        };
        generateIndexCommand.AddOption(typeOption);

        // Add --force option for overwriting existing files
        var forceOption = new Option<bool>("--force", "Force overwrite existing index files");
        generateIndexCommand.AddOption(forceOption); generateIndexCommand.SetHandler(async (string? path, bool overrideVaultRoot, string[] types, bool force, bool dryRun, bool verbose) =>
        {
            try
            {
                // Get services from service provider
                var batchProcessor = _serviceProvider.GetRequiredService<VaultIndexBatchProcessor>();
                var appConfig = _serviceProvider.GetRequiredService<AppConfig>();
                // Use vault root from config if no path provided
                var targetPath = path ?? appConfig.Paths.NotebookVaultFullpathRoot;

                if (string.IsNullOrEmpty(targetPath))
                {
                    AnsiConsoleHelper.WriteError("No path provided and no vault root configured. Please provide a path or configure vault root in config file.");
                    return;
                }
                if (verbose)
                {
                    AnsiConsoleHelper.WriteInfo($"Starting vault index generation for: {targetPath}");
                    if (string.IsNullOrEmpty(path))
                    {
                        AnsiConsoleHelper.WriteInfo("Using vault root from configuration (no path provided)");
                    }
                    if (types.Length > 0)
                    {
                        AnsiConsoleHelper.WriteInfo($"Filtering by types: {string.Join(", ", types)}");
                    }
                    if (overrideVaultRoot)
                    {
                        AnsiConsoleHelper.WriteInfo("Using provided path as vault root override");
                    }
                    if (force)
                    {
                        AnsiConsoleHelper.WriteInfo("Force overwrite mode enabled");
                    }
                    if (dryRun)
                    {
                        AnsiConsoleHelper.WriteInfo("Dry run mode enabled - no files will be modified");
                    }
                }

                // Output basic execution message for test compatibility
                AnsiConsoleHelper.WriteInfo($"Executing vault generate-index for path: {targetPath}");

                // Convert template types to list if provided
                var templateTypes = types.Length > 0 ? types.ToList() : null;                // Set vault root override if requested
                var vaultRoot = overrideVaultRoot ? targetPath : null;                // Execute the batch index generation with animated status
                var result = await AnsiConsoleHelper.WithStatusAsync(
                    async (updateStatus) =>
                    {
                        // Execute the batch index generation
                        return await batchProcessor.GenerateIndexesAsync(
                            vaultPath: targetPath,
                            dryRun: dryRun,
                            templateTypes: templateTypes,
                            forceOverwrite: force,
                            vaultRoot: vaultRoot);
                    },
                    $"Generating vault indexes for: {targetPath}").ConfigureAwait(false); if (result.Success)
                {
                    AnsiConsoleHelper.WriteSuccess($"Vault index generation completed successfully.");
                    AnsiConsoleHelper.WriteInfo($"Processed {result.ProcessedFolders} folders out of {result.TotalFolders} total.");

                    if (result.SkippedFolders > 0)
                    {
                        AnsiConsoleHelper.WriteWarning($"{result.SkippedFolders} folders were skipped.");
                    }

                    if (result.FailedFolders > 0)
                    {
                        AnsiConsoleHelper.WriteWarning($"{result.FailedFolders} folders failed to process.");
                    }
                }
                else
                {
                    AnsiConsoleHelper.WriteError($"Vault index generation failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing vault generate-index command");
                AnsiConsoleHelper.WriteError($"An error occurred: {ex.Message}");
            }
        }, pathArg, vaultRootOverrideOption, typeOption, forceOption, dryRunOption, verboseOption);        // Create ensure-metadata subcommand
        var ensureMetadataCommand = new Command("ensure-metadata", "Ensure metadata consistency across markdown files based on directory hierarchy");
        ensureMetadataCommand.AddArgument(pathArg);
        ensureMetadataCommand.AddOption(vaultRootOverrideOption);
        var forceMetadataOption = new Option<bool>("--force", "Force overwrite existing metadata values");
        ensureMetadataCommand.AddOption(forceMetadataOption);
        ensureMetadataCommand.SetHandler(async (string? path, bool overrideVaultRoot, bool force, bool dryRun, bool verbose) =>
        {
            try
            {
                // Get services from service provider
                var batchProcessor = _serviceProvider.GetRequiredService<MetadataEnsureBatchProcessor>();
                var appConfig = _serviceProvider.GetRequiredService<AppConfig>();

                // Use vault root from config if no path provided
                var targetPath = path ?? appConfig.Paths.NotebookVaultFullpathRoot;

                if (string.IsNullOrEmpty(targetPath))
                {
                    AnsiConsoleHelper.WriteError("No path provided and no vault root configured. Please provide a path or configure vault root in config file.");
                    return;
                }

                if (verbose)
                {
                    AnsiConsoleHelper.WriteInfo($"Starting metadata ensure process for: {targetPath}");
                    if (string.IsNullOrEmpty(path))
                    {
                        AnsiConsoleHelper.WriteInfo("Using vault root from configuration (no path provided)");
                    }
                    if (overrideVaultRoot)
                    {
                        AnsiConsoleHelper.WriteInfo("Using provided path as vault root override");
                    }
                    if (force)
                    {
                        AnsiConsoleHelper.WriteInfo("Force overwrite mode enabled");
                    }
                    if (dryRun)
                    {
                        AnsiConsoleHelper.WriteInfo("Dry run mode enabled - no files will be modified");
                    }
                }

                // Output basic execution message for test compatibility
                AnsiConsoleHelper.WriteInfo($"Executing vault ensure-metadata for path: {targetPath}");

                // Execute the batch metadata processing with animated status
                var result = await AnsiConsoleHelper.WithStatusAsync(
                    async (updateStatus) =>
                    {
                        // Execute the batch metadata processing
                        return await batchProcessor.EnsureMetadataAsync(
                            vaultPath: targetPath,
                            dryRun: dryRun,
                            forceOverwrite: force);
                    },
                    $"Processing metadata for: {targetPath}").ConfigureAwait(false);

                if (result.Success)
                {
                    AnsiConsoleHelper.WriteSuccess($"Metadata ensure process completed successfully.");
                    AnsiConsoleHelper.WriteInfo($"Processed {result.ProcessedFiles} files out of {result.TotalFiles} total.");

                    if (result.SkippedFiles > 0)
                    {
                        AnsiConsoleHelper.WriteWarning($"{result.SkippedFiles} files were skipped (no changes needed).");
                    }

                    if (result.FailedFiles > 0)
                    {
                        AnsiConsoleHelper.WriteWarning($"{result.FailedFiles} files failed to process.");
                    }
                }
                else
                {
                    AnsiConsoleHelper.WriteError($"Metadata ensure process failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing vault ensure-metadata command");
                AnsiConsoleHelper.WriteError($"An error occurred: {ex.Message}");
            }
        }, pathArg, vaultRootOverrideOption, forceMetadataOption, dryRunOption, verboseOption);

        // Create clean-index subcommand
        var cleanIndexCommand = new Command("clean-index", "Delete all index markdown files in the vault");
        cleanIndexCommand.AddArgument(pathArg);
        cleanIndexCommand.AddOption(vaultRootOverrideOption);
        cleanIndexCommand.SetHandler(context =>
        {
            var pathValue = context.ParseResult.GetValueForArgument(pathArg);
            if (string.IsNullOrEmpty(pathValue))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: na vault clean-index <path> [options]",
                    cleanIndexCommand.Description ?? string.Empty,
                    string.Join("\n", cleanIndexCommand.Arguments.Select(arg => $"  <{arg.Name}>\t{arg.Description}")) +
                    "\n" + string.Join("\n", cleanIndexCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            // TODO: Implement vault clean-index logic
            AnsiConsoleHelper.WriteInfo($"Executing vault clean-index for path: {pathValue}");
        });

        // Create sync-dirs subcommand
        var syncDirsCommand = new Command("sync-dirs", "Synchronize directory structure between OneDrive and vault (bidirectional by default)");
        var oneDrivePathArg = new Argument<string?>("onedrive-path", "Relative path within OneDrive to synchronize (defaults to onedrive_resources_basepath from config)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var vaultPathArg = new Argument<string?>("vault-path", "Target vault path (defaults to vault root from config)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var unidirectionalOption = new Option<bool>("--unidirectional", "Disable bidirectional sync (OneDrive → Vault only)");

        syncDirsCommand.AddArgument(oneDrivePathArg);
        syncDirsCommand.AddArgument(vaultPathArg);
        syncDirsCommand.AddOption(unidirectionalOption);
        syncDirsCommand.AddOption(dryRunOption);
        syncDirsCommand.AddOption(verboseOption);

        syncDirsCommand.SetHandler(async (string? oneDrivePath, string? vaultPath, bool unidirectional, bool dryRun, bool verbose) =>
        {
            try
            {
                // Get services from service provider
                var syncProcessor = _serviceProvider.GetRequiredService<IVaultFolderSyncProcessor>();
                var appConfig = _serviceProvider.GetRequiredService<AppConfig>();

                // Default paths from config if not provided
                var effectiveOneDrivePath = oneDrivePath;
                var effectiveVaultPath = vaultPath;

                if (string.IsNullOrWhiteSpace(effectiveOneDrivePath))
                {
                    // Construct default OneDrive path from config: onedrive_fullpath_root + onedrive_resources_basepath
                    var oneDriveRoot = appConfig.Paths?.OnedriveFullpathRoot;
                    var oneDriveBase = appConfig.Paths?.OnedriveResourcesBasepath;

                    if (string.IsNullOrWhiteSpace(oneDriveRoot))
                    {
                        AnsiConsoleHelper.WriteError("OneDrive path is required");
                        return;
                    }

                    effectiveOneDrivePath = string.IsNullOrWhiteSpace(oneDriveBase)
                        ? oneDriveRoot
                        : Path.Combine(oneDriveRoot, oneDriveBase.TrimStart('/', '\\'));
                }

                if (string.IsNullOrWhiteSpace(effectiveVaultPath))
                {
                    // Use vault root from config
                    effectiveVaultPath = appConfig.Paths?.NotebookVaultFullpathRoot;

                    if (string.IsNullOrWhiteSpace(effectiveVaultPath))
                    {
                        AnsiConsoleHelper.WriteError("Vault path is required");
                        return;
                    }
                }

                // Bidirectional is default, unidirectional flag turns it off
                bool bidirectional = !unidirectional;

                if (verbose)
                {
                    AnsiConsoleHelper.WriteInfo($"Starting directory synchronization");
                    AnsiConsoleHelper.WriteInfo($"OneDrive path: {effectiveOneDrivePath}{(string.IsNullOrWhiteSpace(oneDrivePath) ? " (from config)" : "")}");
                    AnsiConsoleHelper.WriteInfo($"Vault path: {effectiveVaultPath}{(string.IsNullOrWhiteSpace(vaultPath) ? " (from config)" : "")}");
                    AnsiConsoleHelper.WriteInfo($"Bidirectional: {bidirectional}");
                    if (dryRun)
                    {
                        AnsiConsoleHelper.WriteInfo("Dry run mode enabled - no directories will be created");
                    }
                }

                // Output basic execution message for test compatibility
                var syncMode = bidirectional ? "bidirectional sync-dirs" : "sync-dirs";
                AnsiConsoleHelper.WriteInfo($"Executing vault {syncMode} for OneDrive path: {effectiveOneDrivePath}");

                // Execute the directory synchronization with animated status
                var statusMessage = bidirectional
                    ? $"Synchronizing directories bidirectionally: {effectiveOneDrivePath}"
                    : $"Synchronizing directories from OneDrive: {effectiveOneDrivePath}";

                var result = await AnsiConsoleHelper.WithStatusAsync(
                    async (updateStatus) =>
                    {
                        // Execute the directory synchronization
                        return await syncProcessor.SyncDirectoriesAsync(
                            oneDrivePath: effectiveOneDrivePath,
                            vaultPath: effectiveVaultPath,
                            dryRun: dryRun,
                            bidirectional: bidirectional);
                    },
                    statusMessage).ConfigureAwait(false);

                if (result.Success)
                {
                    AnsiConsoleHelper.WriteSuccess($"Directory synchronization completed successfully.");
                    AnsiConsoleHelper.WriteInfo($"Synchronized {result.SynchronizedFolders} folders out of {result.TotalFolders} total.");

                    if (result.CreatedVaultFolders > 0)
                    {
                        AnsiConsoleHelper.WriteInfo($"Created {result.CreatedVaultFolders} new vault directories.");
                    }

                    if (bidirectional && result.CreatedOneDriveFolders > 0)
                    {
                        AnsiConsoleHelper.WriteInfo($"Created {result.CreatedOneDriveFolders} new OneDrive directories.");
                    }

                    if (result.SkippedFolders > 0)
                    {
                        AnsiConsoleHelper.WriteWarning($"{result.SkippedFolders} folders were skipped (already exist).");
                    }

                    if (result.FailedFolders > 0)
                    {
                        AnsiConsoleHelper.WriteWarning($"{result.FailedFolders} folders failed to synchronize.");
                    }
                }
                else
                {
                    AnsiConsoleHelper.WriteError($"Directory synchronization failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing vault sync-dirs command");
                AnsiConsoleHelper.WriteError($"An error occurred: {ex.Message}");
            }
        }, oneDrivePathArg, vaultPathArg, unidirectionalOption, dryRunOption, verboseOption);

        // Create parent vault command
        var vaultCommand = new Command("vault", "Vault management commands");
        vaultCommand.AddCommand(generateIndexCommand);
        vaultCommand.AddCommand(ensureMetadataCommand);
        vaultCommand.AddCommand(cleanIndexCommand);
        vaultCommand.AddCommand(syncDirsCommand);

        // Add help handler for when no subcommand is specified
        vaultCommand.TreatUnmatchedTokensAsErrors = true;
        vaultCommand.SetHandler(context =>
        {
            AnsiConsoleHelper.WriteUsage(
                "Usage: na vault <subcommand> [options]",
                "Please specify a vault subcommand to execute. Available vault commands:",
                string.Join("\n", vaultCommand.Subcommands.Select(cmd => $"  {cmd.Name,-15} {cmd.Description}")) +
                "\n\nRun 'na vault [command] --help' for more information on a specific command.");
        });

        rootCommand.AddCommand(vaultCommand);
    }
}

