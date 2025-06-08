// <copyright file="VaultCommands.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Cli/Commands/VaultCommands.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
using System.Runtime.CompilerServices;

using NotebookAutomation.Cli.Utilities;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
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
internal class VaultCommands
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

        var pathArg = new Argument<string>("path", "Path to the directory to start indexing");
        var vaultCommand = new Command("vault", "Vault management commands");
        var generateIndexCommand = new Command("generate-index", "Generate a vault index file");
        generateIndexCommand.AddArgument(pathArg);        // Add force option to regenerate indexes even if they already exist
        var forceOption = new Option<bool>("--force", "Force regeneration of index files even if they already exist");
        generateIndexCommand.AddOption(forceOption);

        // Add override-vault-root option to use the provided path as vault root (overrides config)
        var vaultRootOverrideOption = new Option<bool>("--override-vault-root", "Use the provided path as the vault root (overrides the config)");        // Add template-types option to specify which types of indexes to generate
        var templateTypesOption = new Option<string[]>("--template-types", "Specify which template types to generate (main, program, course, class, module, lesson). Default: all types")
        {
            AllowMultipleArgumentsPerToken = true,
        };
        generateIndexCommand.AddOption(vaultRootOverrideOption);
        generateIndexCommand.AddOption(templateTypesOption);

        generateIndexCommand.SetHandler(async context =>
        {
            if (string.IsNullOrWhiteSpace(context.ParseResult.GetValueForArgument(pathArg)))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: vault generate-index <path> [--force] [--override-vault-root] [--template-types <type1> <type2>...]",
                    "Generate a vault index for the specified directory.",
                    "  <path>                Path to the directory to start indexing (required)\n" +
                    "  --force               Force regeneration of index files\n" +
                    "  --override-vault-root Use the provided path as the vault root (overrides config)\n" +
                    "  --template-types <types>  Specify index types: main, program, course, class, module, lesson (default: all)");
                return;
            }

            string path = context.ParseResult.GetValueForArgument(pathArg);
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            bool force = context.ParseResult.GetValueForOption(forceOption);
            bool overrideVaultRoot = context.ParseResult.GetValueForOption(vaultRootOverrideOption);
            string[]? templateTypes = context.ParseResult.GetValueForOption(templateTypesOption);

            // If override-vault-root is specified, use the provided path as vault root
            string? vaultRoot = overrideVaultRoot ? path : null;

            await ExecuteVaultCommandAsync("generate-index", path, config, debug, verbose, dryRun, force, vaultRoot, templateTypes).ConfigureAwait(false);
        });

        var ensureMetadataCommand = new Command("ensure-metadata", "Update YAML frontmatter with program/course/class metadata based on directory structure");
        ensureMetadataCommand.AddArgument(pathArg);
        ensureMetadataCommand.AddOption(vaultRootOverrideOption);
        ensureMetadataCommand.SetHandler(async context =>
        {
            string path = context.ParseResult.GetValueForArgument(pathArg);
            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool verbose = context.ParseResult.GetValueForOption(verboseOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            bool overrideVaultRoot = context.ParseResult.GetValueForOption(vaultRootOverrideOption);

            // If override-vault-root is specified, use the provided path as vault root
            string? vaultRoot = overrideVaultRoot ? path : null;

            await ExecuteVaultCommandAsync("ensure-metadata", path, config, debug, verbose, dryRun, false, vaultRoot, null).ConfigureAwait(false);
        });

        var cleanIndexCommand = new Command(
            "clean-index",
            "Delete all index markdown files in the vault. " +
            "This includes any file with type: index or with a known index template-type (main, program, course, class, module, lesson, case-studies, readings, resources, case-study) in the YAML frontmatter.");
        cleanIndexCommand.AddArgument(pathArg);
        cleanIndexCommand.AddOption(vaultRootOverrideOption);
        cleanIndexCommand.AddOption(dryRunOption); // Add dry-run option
        cleanIndexCommand.SetHandler(async context =>
        {
            string path = context.ParseResult.GetValueForArgument(pathArg);
            bool overrideVaultRoot = context.ParseResult.GetValueForOption(vaultRootOverrideOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            // If override-vault-root is specified, use the provided path as vault root
            string? vaultRootOverride = overrideVaultRoot ? path : null;

            await ExecuteVaultCommandAsync("clean-index", path, null, false, false, dryRun, false, vaultRootOverride, null).ConfigureAwait(false);
        });

        vaultCommand.AddCommand(generateIndexCommand);
        vaultCommand.AddCommand(ensureMetadataCommand);
        vaultCommand.AddCommand(cleanIndexCommand);
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
    /// <item><description>generate-index: Creates index files for each directory in the vault</description></item>    /// <item><description>ensure-metadata: Updates YAML frontmatter with program/course/class metadata based on directory hierarchy (Program/Course/Class structure)</description></item>
    /// </list>
    /// </remarks>
    private async Task ExecuteVaultCommandAsync(
        string command,
        string path,
        string? configPath,
        bool debug,
        bool verbose,
        bool dryRun,
        bool force = false,
        string? vaultRoot = null,
        string[]? templateTypes = null)
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

        // Validate that the path is within the configured vault root or that vault root override is provided
        if (string.IsNullOrEmpty(vaultRoot))
        {
            string? configuredVaultRoot = _appConfig.Paths?.NotebookVaultFullpathRoot;
            if (!string.IsNullOrEmpty(configuredVaultRoot))
            {
                string normalizedPath = Path.GetFullPath(path).Replace('\\', '/');
                string normalizedConfigRoot = Path.GetFullPath(configuredVaultRoot).Replace('\\', '/');

                if (!normalizedPath.StartsWith(normalizedConfigRoot, StringComparison.OrdinalIgnoreCase))
                {
                    string errorMessage = $"Error: The specified path '{path}' is not within the configured vault root '{configuredVaultRoot}'.\n" +
                                         $"To process files outside the configured vault root, use the --override-vault-root flag:\n" +
                                         $"  {command} \"{path}\" --override-vault-root \"{path}\"";

                    AnsiConsoleHelper.WriteError(errorMessage);
                    _logger.LogErrorWithPath(
                        "Path validation failed: {FilePath} is not within configured vault root {VaultRoot}",
                        "VaultCommands.cs", path, configuredVaultRoot);
                    return;
                }
            }
        }

        _logger.LogInformationWithPath("Executing vault command: {Command} on path: {FilePath}", "VaultCommands.cs", command, path);
        _logger.LogDebugWithPath("Debugging vault command", "VaultCommands.cs");

        try
        {
            switch (command)
            {
                case "generate-index":
                    await ExecuteGenerateIndexAsync(path, dryRun, force, vaultRoot, templateTypes).ConfigureAwait(false);
                    break;
                case "ensure-metadata":
                    await ExecuteEnsureMetadataAsync(path, dryRun, verbose, vaultRoot).ConfigureAwait(false);
                    break;
                case "clean-index":
                    await ExecuteCleanIndexAsync(path, dryRun, vaultRoot).ConfigureAwait(false);
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
    /// Executes the generate-index command using the VaultIndexBatchProcessor.
    /// </summary>
    /// <param name="path">Path to the vault directory.</param>
    /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExecuteGenerateIndexAsync(string path, bool dryRun, bool force = false, string? vaultRoot = null, string[]? templateTypes = null)
    {
        try
        {
            _logger.LogInformationWithPath("Starting vault index generation process for vault: {VaultPath}", "VaultCommands.cs", path);

            // Create a new scope to set vault root override
            using var scope = _serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;            // Set up vault root override in scoped context
            var vaultRootContext = scopedServices.GetRequiredService<VaultRootContextService>();

            // Use explicit vault root if provided, otherwise use the provided path as vault root
            string effectiveVaultRoot = !string.IsNullOrEmpty(vaultRoot) ? vaultRoot : Path.GetFullPath(path);
            vaultRootContext.VaultRootOverride = effectiveVaultRoot;
            _logger.LogInformationWithPath("Using vault root override: {VaultRoot}", "VaultCommands.cs", effectiveVaultRoot);

            var batchProcessor = scopedServices.GetRequiredService<VaultIndexBatchProcessor>();

            var result = await AnsiConsoleHelper.WithStatusAsync(
                async (updateStatus) =>
                {
                    batchProcessor.ProcessingProgressChanged += (sender, e) =>
                    {
                        string safeStatus = e.Status.Replace("[", "[[").Replace("]", "]]");
                        updateStatus(safeStatus);
                    };
                    return await batchProcessor.GenerateIndexesAsync(
                        vaultPath: path,
                        dryRun: dryRun,
                        templateTypes: templateTypes?.ToList(), // Convert array to list
                        forceOverwrite: force,      // Use the force parameter
                        vaultRoot: vaultRoot) // Use the explicit vault root if provided
                    .ConfigureAwait(false);
                },
                $"Generating indexes for vault: {Path.GetFileName(path)}")
            .ConfigureAwait(false);

            if (result.Success)
            {
                string prefix = dryRun ? "[DRY RUN] " : string.Empty;
                AnsiConsoleHelper.WriteSuccess($"\n{prefix}Vault index generation completed");
                AnsiConsoleHelper.WriteInfo($"  Folders processed: {result.ProcessedFolders}");
                AnsiConsoleHelper.WriteInfo($"  Folders skipped: {result.SkippedFolders}");
                if (result.FailedFolders > 0)
                {
                    AnsiConsoleHelper.WriteWarning($"  Folders failed: {result.FailedFolders}");
                }

                AnsiConsoleHelper.WriteInfo($"  Total folders: {result.TotalFolders}");

                if (dryRun)
                {
                    AnsiConsoleHelper.WriteInfo("\nDetailed index generation changes are available in the log file:");

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

                    AnsiConsoleHelper.WriteInfo("\nTip: Use --verbose for more details about the index generation process.");
                }

                _logger.LogInformationWithPath(
                    "{Prefix}Vault index generation completed: {Processed} processed, {Skipped} skipped, {Failed} failed out of {Total} total folders",
                    "VaultCommands.cs",
                    prefix, result.ProcessedFolders, result.SkippedFolders, result.FailedFolders, result.TotalFolders);

                if (result.FailedFolders > 0)
                {
                    _logger.LogWarningWithPath("Some folders failed to process. Check the logs for details.", "VaultCommands.cs");
                }
            }
            else
            {
                AnsiConsoleHelper.WriteError($"Vault index generation failed: {result.ErrorMessage ?? "Unknown error"}");
                _logger.LogErrorWithPath("Vault index generation failed: {ErrorMessage}", "VaultCommands.cs", result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            AnsiConsoleHelper.WriteError($"Failed to execute generate-index command: {ex.Message}");
            _logger.LogErrorWithPath(ex, "Failed to execute generate-index command", "VaultCommands.cs");
            throw;
        }
    }

    /// <summary>
    /// Executes the ensure-metadata command using the MetadataEnsureBatchProcessor.
    /// </summary>
    /// <param name="path">Path to the vault directory.</param>
    /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
    /// <param name="verbose">Whether to enable verbose output.</param>
    /// <param name="vaultRoot">Optional vault root override path.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExecuteEnsureMetadataAsync(string path, bool dryRun, bool verbose, string? vaultRoot = null)
    {
        try
        {
            _logger.LogInformationWithPath("Starting metadata ensure process for vault: {VaultPath}", "VaultCommands.cs", path);

            // Create a new scope to set vault root override
            using var scope = _serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            // Set up vault root override in scoped context
            var vaultRootContext = scopedServices.GetRequiredService<VaultRootContextService>();

            // Use explicit vault root if provided, otherwise use the provided path as vault root
            string effectiveVaultRoot = !string.IsNullOrEmpty(vaultRoot) ? vaultRoot : Path.GetFullPath(path);
            vaultRootContext.VaultRootOverride = effectiveVaultRoot;
            _logger.LogInformationWithPath("Using vault root override: {VaultRoot}", "VaultCommands.cs", effectiveVaultRoot);

            var batchProcessor = scopedServices.GetRequiredService<MetadataEnsureBatchProcessor>();

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
                        retryFailed: false)
                    .ConfigureAwait(false);
                },
                $"Processing metadata for vault: {Path.GetFileName(path)}")
            .ConfigureAwait(false);

            if (result.Success)
            {
                string prefix = dryRun ? "[DRY RUN] " : string.Empty;

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

    /// <summary>
    /// Executes the clean-index command to delete all index markdown files in the vault.
    /// </summary>
    /// <param name="path">Path to the vault directory.</param>
    /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
    /// <param name="vaultRoot">Explicit vault root path override.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExecuteCleanIndexAsync(string path, bool dryRun, string? vaultRoot = null)
    {
        var mdFiles = Directory.GetFiles(path, "*.md", SearchOption.AllDirectories);        // Known index template-types (should match those in metadata.yaml)
        string[] indexTemplateTypes = [
            "main", "program", "course", "class", "module", "lesson",
            "case-studies", "case-studies-index", "readings", "resources", "case-study"
        ];
        int deleted = 0;
        foreach (var file in mdFiles)
        {
            string content = await File.ReadAllTextAsync(file).ConfigureAwait(false);
            var yaml = new YamlHelper(_logger).ExtractFrontmatter(content);
            if (yaml != null)
            {
                var dict = new YamlHelper(_logger).ParseYamlToDictionary(yaml);
                var typeVal = dict.TryGetValue("type", out var t) ? t?.ToString() : null;
                var templateTypeVal = dict.TryGetValue("template-type", out var tt) ? tt?.ToString() : null;
                bool isIndexType = typeVal == "index";
                bool isIndexTemplate = templateTypeVal != null && (indexTemplateTypes.Contains(templateTypeVal) || templateTypeVal.EndsWith("-index"));
                if (isIndexType || isIndexTemplate)
                {
                    if (!dryRun)
                    {
                        File.Delete(file);
                    }

                    deleted++;
                    _logger.LogInformationWithPath($"Deleted index file: {file}", "VaultCommands.cs");
                }
            }
        }

        _logger.LogInformationWithPath($"Deleted {deleted} index files in {path}", "VaultCommands.cs");
    }
}
