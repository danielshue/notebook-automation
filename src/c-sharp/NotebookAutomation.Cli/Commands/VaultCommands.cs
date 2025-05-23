using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Cli.Commands
{
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
    internal static class VaultCommands
    {
        /// <summary>
        /// Registers all vault-related commands with the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add vault commands to.</param>
        /// <param name="configOption">The global config file option.</param>
        /// <param name="debugOption">The global debug option.</param>
        /// <param name="verboseOption">The global verbose output option.</param>
        /// <param name="dryRunOption">The global dry run option to simulate actions without making changes.</param>
        public static void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
        {
            var pathArg = new Argument<string>("path", "Path to the vault directory");
            var vaultCommand = new Command("vault", "Vault management commands");

            var generateIndexCommand = new Command("generate-index", "Generate a vault index");
            generateIndexCommand.AddArgument(pathArg);
            generateIndexCommand.SetHandler(async (InvocationContext context) =>
            {
                string path = context.ParseResult.GetValueForArgument(pathArg);
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                
                await ExecuteVaultCommandAsync("generate-index", path, config, debug, verbose, dryRun);
            });

            var ensureMetadataCommand = new Command("ensure-metadata", "Ensure consistent metadata across vault files");
            ensureMetadataCommand.AddArgument(pathArg);
            ensureMetadataCommand.SetHandler(async (InvocationContext context) =>
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
        /// <item><description>ensure-metadata: Ensures consistent metadata across markdown files in the vault</description></item>
        /// </list>
        /// Note: This is currently a placeholder implementation that logs actions but does not perform actual operations.
        /// </remarks>
        private static async Task ExecuteVaultCommandAsync(
            string command,
            string path,
            string? configPath,
            bool debug,
            bool verbose,
            bool dryRun)
        {
            try
            {
                // Initialize dependency injection if needed
                if (configPath != null)
                {
                    Program.SetupDependencyInjection(configPath, debug);
                }

                var serviceProvider = Program.ServiceProvider;
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("VaultCommands");
                var loggingService = serviceProvider.GetRequiredService<LoggingService>();
                var failedLogger = loggingService.FailedLogger;
                var appConfig = serviceProvider.GetRequiredService<AppConfig>();

                // Always show the active config file being used
                string activeConfigPath = configPath ?? AppConfig.FindConfigFile() ?? "config.json";
                Console.WriteLine($"Using config file: {activeConfigPath}\n");

                logger.LogInformation("Executing vault command: {Command} on {Path}", command, path);

                if (dryRun)
                {
                    logger.LogInformation("[DRY RUN] No changes will be made");
                }

                if (verbose)
                {
                    logger.LogInformation("Verbose output enabled");
                }

                // Placeholder for actual vault command logic
                switch (command.ToLowerInvariant())
                {
                    case "generate-index":
                        logger.LogInformation("Simulating generation of vault index");
                        await Task.Delay(500); // Simulate work
                        break;
                        
                    case "ensure-metadata":
                        logger.LogInformation("Simulating metadata consistency check");
                        await Task.Delay(500); // Simulate work
                        break;
                        
                    default:
                        logger.LogError("Unknown command: {Command}", command);
                        break;
                }

                logger.LogInformation("Command completed successfully");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing vault command: {ex.Message}");
                if (debug)
                {
                    Console.Error.WriteLine(ex.ToString());
                }
            }
        }
    }
}