using NotebookAutomation.Cli.Utilities;
using System.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NotebookAutomation.Core.Utils;
using NotebookAutomation.Core.Tools.Vault;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace NotebookAutomation.Cli.Commands
{    /// <summary>
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

        public VaultCommands(ILogger<VaultCommands> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
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
            var vaultCommand = new Command("vault", "Vault management commands"); var generateIndexCommand = new Command("generate-index", "TODO: Generate a vault index");
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
                await this.ExecuteVaultCommandAsync("generate-index", path, config, debug, verbose, dryRun);
            }); var ensureMetadataCommand = new Command("ensure-metadata", "TODO: Update YAML frontmatter with program/course/class metadata based on directory structure");
            ensureMetadataCommand.AddArgument(pathArg);
            ensureMetadataCommand.SetHandler(async context =>
            {
                string path = context.ParseResult.GetValueForArgument(pathArg);
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

                await this.ExecuteVaultCommandAsync("ensure-metadata", path, config, debug, verbose, dryRun);
            });

            vaultCommand.AddCommand(generateIndexCommand);
            vaultCommand.AddCommand(ensureMetadataCommand);
            rootCommand.AddCommand(vaultCommand);
        }        /// <summary>
                 /// Executes a vault command on the specified path.
                 /// </summary>
                 /// <param name="command">The vault command to execute: generate-index or ensure-metadata.</param>
                 /// <param name="path">Path to the vault directory.</param>
                 /// <param name="configPath">Path to the configuration file.</param>
                 /// <param name="debug">Whether to enable debug logging.</param>
                 /// <param name="verbose">Whether to enable verbose output.</param>
                 /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
                 /// <returns>A task representing the asynchronous operation.</returns>        /// <remarks>
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
            if (string.IsNullOrEmpty(configPath))
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

            _logger.LogInformationWithPath("Executing vault command: {Command} on path: {FilePath}", command, path);
            _logger.LogDebugWithPath("Debugging vault command", "VaultCommands.cs");

            try
            {
                switch (command)
                {
                    case "generate-index":
                        await ExecuteGenerateIndexAsync(path, dryRun);
                        break;
                    case "ensure-metadata":
                        await ExecuteEnsureMetadataAsync(path, dryRun);
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
        }        /// <summary>
                 /// Executes the ensure-metadata command using the MetadataEnsureBatchProcessor.
                 /// </summary>
        private async Task ExecuteEnsureMetadataAsync(string path, bool dryRun)
        {
            try
            {
                _logger.LogInformationWithPath("Starting metadata ensure process for vault: {VaultPath}", "VaultCommands.cs", path);

                // Get the batch processor from DI
                var batchProcessor = _serviceProvider.GetRequiredService<MetadataEnsureBatchProcessor>();

                // Create a Spectre.Console batch progress reporter for real-time updates
                using var progressReporter = AnsiConsoleHelper.CreateBatchProgressReporter(
                    $"Processing metadata for vault: {Path.GetFileName(path)}");

                // Hook up progress events to the Spectre.Console progress reporter
                batchProcessor.ProcessingProgressChanged += (sender, e) =>
                {
                    progressReporter.UpdateProgress(e);
                };

                batchProcessor.QueueChanged += (sender, e) =>
                {
                    progressReporter.UpdateQueue(e);
                };

                // Start the progress reporter and execute batch processing concurrently
                var progressTask = progressReporter.StartAsync();

                try
                {
                    // Execute the batch processing
                    var result = await batchProcessor.EnsureMetadataAsync(
                        vaultPath: path,
                        dryRun: dryRun,
                        forceOverwrite: false, // Could be made configurable via CLI option
                        retryFailed: false     // Could be made configurable via CLI option
                    );

                    // Stop the progress reporter
                    await progressReporter.StopAsync();

                    // Wait for the progress task to complete
                    await progressTask;

                    // Log the results
                    if (result.Success)
                    {
                        string prefix = dryRun ? "[DRY RUN] " : "";
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
                        _logger.LogErrorWithPath("Metadata processing failed: {ErrorMessage}", "VaultCommands.cs", result.ErrorMessage ?? "Unknown error");
                    }
                }
                catch
                {
                    // Make sure to stop progress reporter on error
                    await progressReporter.StopAsync();
                    await progressTask;
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogErrorWithPath(ex, "Failed to execute ensure-metadata command", "VaultCommands.cs");
                throw;
            }
        }
    }
}