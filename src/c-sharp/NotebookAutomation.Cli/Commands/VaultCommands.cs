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

        var pathArg = new Argument<string?>("path", "Path to the vault directory to process (defaults to vault root from config)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var vaultRootOverrideOption = new Option<bool>("--override-vault-root", "Use the provided path as the vault root (overrides the config)");

        // Create generate-index subcommand
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
        generateIndexCommand.AddOption(forceOption);

        // Add --recursive option for processing subdirectories
        var recursiveOption = new Option<bool>("--recursive", "Process subdirectories recursively (default: process only the specified directory)");
        generateIndexCommand.AddOption(recursiveOption);

        generateIndexCommand.SetHandler(async (string? path, bool overrideVaultRoot, string[] types, bool force, bool recursive, bool dryRun, bool verbose) =>
        {
            try
            {
                var batchProcessor = _serviceProvider.GetRequiredService<VaultIndexBatchProcessor>();
                var appConfig = _serviceProvider.GetRequiredService<AppConfig>();

                // Resolve effective vault root (combined resources path if configured)
                var effectiveVaultRoot = appConfig.Paths?.GetEffectiveVaultRoot();
                var rawVaultRoot = appConfig.Paths?.NotebookVaultFullpathRoot;
                string vaultRootPath = effectiveVaultRoot
                                       ?? rawVaultRoot
                                       ?? string.Empty;

                // Determine target path
                string targetPath;
                if (string.IsNullOrEmpty(path))
                {
                    targetPath = vaultRootPath;
                }
                else if (Path.IsPathRooted(path))
                {
                    targetPath = path;
                }
                else
                {
                    if (string.IsNullOrEmpty(vaultRootPath))
                    {
                        AnsiConsoleHelper.WriteError("Relative path provided but no vault root configured. Please configure vault root in config file.");
                        return;
                    }
                    targetPath = Path.Combine(vaultRootPath, path);
                }

                if (string.IsNullOrEmpty(targetPath))
                {
                    AnsiConsoleHelper.WriteError("No path provided and no vault root configured. Please provide a path or configure vault root in config file.");
                    return;
                }

                if (verbose)
                {
                    AnsiConsoleHelper.WriteInfo($"Starting vault index generation for: {targetPath}");
                    AnsiConsoleHelper.WriteInfo($"Effective vault root: {effectiveVaultRoot ?? "(null)"}");
                    if (!string.Equals(effectiveVaultRoot, rawVaultRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        AnsiConsoleHelper.WriteInfo($"Raw vault root (pre-resources merge): {rawVaultRoot ?? "(null)"}");
                    }

                    if (string.IsNullOrEmpty(path))
                    {
                        AnsiConsoleHelper.WriteInfo("Using vault root from configuration (no path provided)");
                    }
                    else if (Path.IsPathRooted(path))
                    {
                        AnsiConsoleHelper.WriteInfo("Using provided absolute path");
                    }
                    else
                    {
                        AnsiConsoleHelper.WriteInfo($"Resolved relative path '{path}' against vault root");
                    }

                    if (types.Length > 0)
                    {
                        AnsiConsoleHelper.WriteInfo($"Filtering by types: {string.Join(", ", types)}");
                    }
                    if (overrideVaultRoot)
                    {
                        AnsiConsoleHelper.WriteInfo("Using provided path as vault root override");
                    }
                    AnsiConsoleHelper.WriteInfo(recursive ? "Recursive mode: processing subdirectories" : "Non-recursive mode: processing only the specified directory");
                    if (force)
                    {
                        AnsiConsoleHelper.WriteInfo("Force overwrite mode enabled");
                    }
                    if (dryRun)
                    {
                        AnsiConsoleHelper.WriteInfo("Dry run mode enabled - no files will be modified");
                    }
                }

                AnsiConsoleHelper.WriteInfo($"Executing vault generate-index for path: {targetPath}");
                var templateTypes = types.Length > 0 ? types.ToList() : null;

                // Decide vault root to pass for relative hierarchy calculation
                string? vaultRoot = overrideVaultRoot ? targetPath : vaultRootPath;

                var result = await AnsiConsoleHelper.WithStatusAsync(
                    async (updateStatus) => await batchProcessor.GenerateIndexesAsync(
                        vaultPath: targetPath,
                        dryRun: dryRun,
                        templateTypes: templateTypes,
                        forceOverwrite: force,
                        recursive: recursive,
                        vaultRoot: vaultRoot),
                    $"Generating vault indexes for: {targetPath}").ConfigureAwait(false);

                if (result.Success)
                {
                    AnsiConsoleHelper.WriteSuccess("Vault index generation completed successfully.");
                    AnsiConsoleHelper.WriteInfo($"Processed {result.ProcessedFolders} folders out of {result.TotalFolders} total.");
                    if (result.SkippedFolders > 0) AnsiConsoleHelper.WriteWarning($"{result.SkippedFolders} folders were skipped.");
                    if (result.FailedFolders > 0) AnsiConsoleHelper.WriteWarning($"{result.FailedFolders} folders failed to process.");
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
        }, pathArg, vaultRootOverrideOption, typeOption, forceOption, recursiveOption, dryRunOption, verboseOption);

        // Create ensure-metadata subcommand
        var ensureMetadataCommand = new Command("ensure-metadata", "Ensure metadata consistency across markdown files based on directory hierarchy");
        ensureMetadataCommand.AddArgument(pathArg);
        ensureMetadataCommand.AddOption(vaultRootOverrideOption);
        var forceMetadataOption = new Option<bool>("--force", "Force overwrite existing metadata values");
        ensureMetadataCommand.AddOption(forceMetadataOption);
        ensureMetadataCommand.SetHandler(async (string? path, bool overrideVaultRoot, bool force, bool dryRun, bool verbose) =>
        {
            try
            {
                var batchProcessor = _serviceProvider.GetRequiredService<MetadataEnsureBatchProcessor>();
                var appConfig = _serviceProvider.GetRequiredService<AppConfig>();

                var effectiveVaultRoot = appConfig.Paths?.GetEffectiveVaultRoot();
                var rawVaultRoot = appConfig.Paths?.NotebookVaultFullpathRoot;
                var defaultPath = effectiveVaultRoot ?? rawVaultRoot;

                var targetPath = path ?? defaultPath;
                if (string.IsNullOrEmpty(targetPath))
                {
                    AnsiConsoleHelper.WriteError("No path provided and no vault root configured. Please provide a path or configure vault root in config file.");
                    return;
                }

                if (verbose)
                {
                    AnsiConsoleHelper.WriteInfo($"Starting metadata ensure process for: {targetPath}");
                    AnsiConsoleHelper.WriteInfo($"Effective vault root: {effectiveVaultRoot ?? "(null)"}");
                    if (!string.Equals(effectiveVaultRoot, rawVaultRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        AnsiConsoleHelper.WriteInfo($"Raw vault root: {rawVaultRoot ?? "(null)"}");
                    }
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

                AnsiConsoleHelper.WriteInfo($"Executing vault ensure-metadata for path: {targetPath}");

                var result = await AnsiConsoleHelper.WithStatusAsync(
                    async (updateStatus) => await batchProcessor.EnsureMetadataAsync(
                        vaultPath: targetPath,
                        dryRun: dryRun,
                        forceOverwrite: force),
                    $"Processing metadata for: {targetPath}").ConfigureAwait(false);

                if (result.Success)
                {
                    AnsiConsoleHelper.WriteSuccess("Metadata ensure process completed successfully.");
                    AnsiConsoleHelper.WriteInfo($"Processed {result.ProcessedFiles} files out of {result.TotalFiles} total.");
                    if (result.SkippedFiles > 0) AnsiConsoleHelper.WriteWarning($"{result.SkippedFiles} files were skipped (no changes needed).");
                    if (result.FailedFiles > 0) AnsiConsoleHelper.WriteWarning($"{result.FailedFiles} files failed to process.");
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

        // Create clean-index subcommand (unchanged)
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
            AnsiConsoleHelper.WriteInfo($"Executing vault clean-index for path: {pathValue}");
        });

        // Create sync-dirs subcommand
        var syncDirsCommand = new Command("sync-dirs", "Synchronize directory structure between OneDrive and vault (bidirectional by default)");
        var vaultPathArg = new Argument<string?>("vault-path", "Vault path to start synchronization from (defaults to vault root from config)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var unidirectionalOption = new Option<bool>("--unidirectional", "Disable bidirectional sync (OneDrive → Vault only)");
        var syncRecursiveOption = new Option<bool>("--recursive", "Enable recursive directory scanning (default: false, immediate children only)");
        var documentTypesOption = new Option<List<string>>(
            ["--document-types", "-t"],
            "Document types to create placeholder markdown files for (comma-separated: videos, pdf, html)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        syncDirsCommand.AddArgument(vaultPathArg);
        syncDirsCommand.AddOption(unidirectionalOption);
        syncDirsCommand.AddOption(syncRecursiveOption);
        syncDirsCommand.AddOption(dryRunOption);
        syncDirsCommand.AddOption(verboseOption);
        syncDirsCommand.AddOption(documentTypesOption);

        syncDirsCommand.SetHandler(async (string? vaultPath, bool unidirectional, bool recursive, bool dryRun, bool verbose, List<string>? documentTypes) =>
        {
            try
            {
                var syncProcessor = _serviceProvider.GetRequiredService<IVaultFolderSyncProcessor>();
                var appConfig = _serviceProvider.GetRequiredService<AppConfig>();
                _logger.LogDebug($"[sync-dirs] Config paths object: {appConfig.Paths}");
                _logger.LogDebug($"[sync-dirs] Config paths vault root: {appConfig.Paths?.NotebookVaultFullpathRoot}");
                _logger.LogDebug($"[sync-dirs] OnedriveFullpathRoot: {appConfig.Paths?.OnedriveFullpathRoot}");
                _logger.LogDebug($"[sync-dirs] OnedriveResourcesBasepath: {appConfig.Paths?.OnedriveResourcesBasepath}");
                _logger.LogDebug($"[sync-dirs] NotebookVaultFullpathRoot: {appConfig.Paths?.NotebookVaultFullpathRoot}");
                _logger.LogDebug($"[sync-dirs] NotebookVaultResourcesBasepath: {appConfig.Paths?.NotebookVaultResourcesBasepath}");
                _logger.LogDebug($"[sync-dirs] Direct property access to NotebookVaultResourcesBasepath: {appConfig.Paths?.NotebookVaultResourcesBasepath}");
                var effectiveVaultRoot = appConfig.Paths?.GetEffectiveVaultRoot();
                _logger.LogDebug($"[sync-dirs] EffectiveVaultRoot (GetEffectiveVaultRoot): {effectiveVaultRoot}");
                var vaultBasePath = appConfig.Paths?.NotebookVaultResourcesBasepath;
                _logger.LogDebug($"[sync-dirs] RAW vaultBasePath from config: {vaultBasePath ?? "(null)"}");
                if (!string.IsNullOrWhiteSpace(vaultBasePath))
                {
                    vaultBasePath = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(vaultBasePath);
                    vaultBasePath = vaultBasePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    _logger.LogDebug("[sync-dirs] PROCESSED vaultBasePath: '{vaultBasePath}'", vaultBasePath);
                }
                var effectiveVaultPath = vaultPath;
                if (string.IsNullOrWhiteSpace(effectiveVaultPath))
                {
                    effectiveVaultPath = effectiveVaultRoot;
                    if (string.IsNullOrWhiteSpace(effectiveVaultPath))
                    {
                        AnsiConsoleHelper.WriteError("Vault path is required - either provide a path or configure vault root in config file");
                        return;
                    }
                }
                else if (!Path.IsPathRooted(effectiveVaultPath))
                {
                    if (string.IsNullOrWhiteSpace(effectiveVaultRoot))
                    {
                        AnsiConsoleHelper.WriteError("Relative vault path provided but no vault root configured. Please configure vault root in config file.");
                        return;
                    }
                    _logger.LogDebug("[sync-dirs] Resolving relative path against effectiveVaultRoot='{effectiveVaultRoot}'", effectiveVaultRoot);
                    effectiveVaultPath = Path.GetFullPath(Path.Combine(effectiveVaultRoot, effectiveVaultPath));
                    _logger.LogDebug("[sync-dirs] Resolved effectiveVaultPath: '{effectiveVaultPath}'", effectiveVaultPath);
                }
                if (!Directory.Exists(effectiveVaultPath))
                {
                    AnsiConsoleHelper.WriteError($"Vault path does not exist: {effectiveVaultPath}");
                    return;
                }
                var onedriveRoot = appConfig.Paths?.OnedriveFullpathRoot;
                var onedriveBase = appConfig.Paths?.OnedriveResourcesBasepath;
                if (string.IsNullOrWhiteSpace(onedriveRoot) || string.IsNullOrWhiteSpace(onedriveBase))
                {
                    AnsiConsoleHelper.WriteError("OneDrive root or resources basepath not configured. Please set paths.onedrive_fullpath_root and paths.onedrive_resources_basepath in configuration.");
                    return;
                }
                onedriveRoot = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(onedriveRoot);
                onedriveBase = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(onedriveBase);
                onedriveBase = onedriveBase.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var configuredVaultRoot = appConfig.Paths?.NotebookVaultFullpathRoot;
                if (string.IsNullOrWhiteSpace(configuredVaultRoot))
                {
                    AnsiConsoleHelper.WriteError("Vault root not configured. Please set paths.notebook_vault_fullpath_root in configuration.");
                    return;
                }
                configuredVaultRoot = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(configuredVaultRoot);
                string calculationBasePath = configuredVaultRoot;
                if (!string.IsNullOrWhiteSpace(vaultBasePath))
                {
                    calculationBasePath = Path.Combine(configuredVaultRoot, vaultBasePath);
                    calculationBasePath = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(calculationBasePath);
                }
                string providedPath = string.Empty;
                if (!effectiveVaultPath.Equals(calculationBasePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (!effectiveVaultPath.StartsWith(calculationBasePath, StringComparison.OrdinalIgnoreCase))
                    {
                        AnsiConsoleHelper.WriteError($"Vault path '{effectiveVaultPath}' is not under the calculated base path '{calculationBasePath}'");
                        return;
                    }
                    providedPath = Path.GetRelativePath(calculationBasePath, effectiveVaultPath);
                    providedPath = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(providedPath);
                }
                var oneDrivePath = Path.Combine(onedriveRoot, onedriveBase, providedPath);
                oneDrivePath = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(oneDrivePath);
                _logger.LogDebug("[sync-dirs] onedriveRoot: {onedriveRoot}", onedriveRoot);
                _logger.LogDebug("[sync-dirs] onedriveBase: {onedriveBase}", onedriveBase);
                _logger.LogDebug("[sync-dirs] vaultBasePath: {vaultBasePath}", vaultBasePath ?? "(not configured)");
                _logger.LogDebug("[sync-dirs] configuredVaultRoot: {configuredVaultRoot}", configuredVaultRoot);
                _logger.LogDebug("[sync-dirs] calculationBasePath: {calculationBasePath}", calculationBasePath);
                _logger.LogDebug("[sync-dirs] effectiveVaultPath: {effectiveVaultPath}", effectiveVaultPath);
                _logger.LogDebug("[sync-dirs] providedPath: {providedPath}", providedPath);
                _logger.LogDebug("[sync-dirs] oneDrivePath: {oneDrivePath}", oneDrivePath);
                bool bidirectional = !unidirectional;
                if (verbose)
                {
                    AnsiConsoleHelper.WriteInfo("Starting directory synchronization");
                    AnsiConsoleHelper.WriteInfo($"Vault path: {effectiveVaultPath}{(string.IsNullOrWhiteSpace(vaultPath) ? " (vault root from config)" : "")}");
                    AnsiConsoleHelper.WriteInfo($"OneDrive path: {oneDrivePath}");
                    AnsiConsoleHelper.WriteInfo($"Bidirectional: {bidirectional}");
                    AnsiConsoleHelper.WriteInfo(recursive ? "Recursive mode: processing subdirectories" : "Non-recursive mode: processing only immediate children");
                    if (dryRun) AnsiConsoleHelper.WriteInfo("Dry run mode enabled - no directories will be created");
                }
                var syncMode = bidirectional ? "bidirectional sync-dirs" : "sync-dirs";
                // Write execution message (tests assert this presence)
                var executionMessage = $"Executing vault {syncMode} for vault path: {effectiveVaultPath}";
                AnsiConsoleHelper.WriteInfo(executionMessage);
                var statusMessage = bidirectional
                    ? $"Synchronizing directories bidirectionally: {effectiveVaultPath}"
                    : $"Synchronizing directories from OneDrive: {effectiveVaultPath}";
                var result = await AnsiConsoleHelper.WithStatusAsync(
                    async (updateStatus) => await syncProcessor.SyncDirectoriesAsync(
                        oneDrivePath: oneDrivePath,
                        vaultPath: effectiveVaultPath,
                        dryRun: dryRun,
                        bidirectional: bidirectional,
                        recursive: recursive,
                        documentTypes: documentTypes?.Count > 0 ? documentTypes : null),
                    statusMessage).ConfigureAwait(false);
                // Status complete – continue with result reporting
                if (result.Success)
                {
                    AnsiConsoleHelper.WriteSuccess("Directory synchronization completed successfully.");
                    AnsiConsoleHelper.WriteInfo($"Synchronized {result.SynchronizedFolders} folders out of {result.TotalFolders} total.");
                    if (result.CreatedVaultFolders > 0) AnsiConsoleHelper.WriteInfo($"Created {result.CreatedVaultFolders} new vault directories.");
                    if (bidirectional && result.CreatedOneDriveFolders > 0) AnsiConsoleHelper.WriteInfo($"Created {result.CreatedOneDriveFolders} new OneDrive directories.");
                    if (result.CreatedPlaceholderFiles > 0) AnsiConsoleHelper.WriteInfo($"Created {result.CreatedPlaceholderFiles} placeholder markdown files.");
                    if (result.SkippedFolders > 0) AnsiConsoleHelper.WriteWarning($"{result.SkippedFolders} folders were skipped (already exist).");
                    if (result.FailedFolders > 0) AnsiConsoleHelper.WriteWarning($"{result.FailedFolders} folders failed to synchronize.");
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
        }, vaultPathArg, unidirectionalOption, syncRecursiveOption, dryRunOption, verboseOption, documentTypesOption);

        var vaultCommand = new Command("vault", "Vault management commands");
        vaultCommand.AddCommand(generateIndexCommand);
        vaultCommand.AddCommand(ensureMetadataCommand);
        vaultCommand.AddCommand(cleanIndexCommand);
        vaultCommand.AddCommand(syncDirsCommand);
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

