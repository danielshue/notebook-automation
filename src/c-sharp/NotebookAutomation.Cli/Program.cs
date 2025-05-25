using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Cli.Commands;
using NotebookAutomation.Cli.Utilities;

namespace NotebookAutomation.Cli
{    /// <summary>
     /// Main entry point for the Notebook Automation CLI.
     /// 
     /// This program provides a unified command-line interface for accessing
     /// all the notebook automation tools.
     /// </summary>
    public class Program
    {
        private static IServiceProvider? _serviceProvider;

        /// <summary>
        /// Gets the service provider for dependency injection.
        /// </summary>
        public static IServiceProvider ServiceProvider
        {
            get => _serviceProvider ?? throw new InvalidOperationException("Service provider not initialized. Call SetupDependencyInjection first.");
        }

        /// <summary>
        /// Entry point for the application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Exit code (0 for success, non-zero for error).</returns>
        public static async Task<int> Main(string[] args)
        {
            // Create the root command with description
            var rootCommand = new RootCommand(
                description: "notebookautomation.exe - Tools for managing Obsidian notebooks");

            // Global options
            var configOption = new Option<string>(
                aliases: ["--config", "-c"],
                description: "Path to the configuration file");
            var debugOption = new Option<bool>(
                aliases: ["--debug", "-d"],
                description: "Enable debug output");
            var verboseOption = new Option<bool>(
                aliases: ["--verbose", "-v"],
                description: "Enable verbose output");
            var dryRunOption = new Option<bool>(
                aliases: ["--dry-run"],
                description: "Simulate actions without making changes");

            rootCommand.AddGlobalOption(configOption);
            rootCommand.AddGlobalOption(debugOption);
            rootCommand.AddGlobalOption(verboseOption);
            rootCommand.AddGlobalOption(dryRunOption);

            if (args.Contains("--debug") || args.Contains("-d"))
            {
                AnsiConsoleHelper.WriteInfo($"Debug mode enabled");
            }

            // Handle --version manually before building the CLI to avoid conflicts
            if (args.Contains("--version") || args.Contains("-v"))
            {
                AnsiConsoleHelper.WriteInfo($"Notebook Automation v1.0.0");
                AnsiConsoleHelper.WriteInfo($"Running on .NET {Environment.Version}");
                AnsiConsoleHelper.WriteInfo("(c) 2025 Dan Shue");
                return 0;
            }

            var tagCommands = new TagCommands();
            tagCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
            var vaultCommands = new VaultCommands();
            vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
            var videoCommands = new VideoCommands();
            videoCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
            var pdfCommands = new PdfCommands();
            pdfCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
            var markdownCommands = new MarkdownCommands();
            markdownCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
            var configCommands = new ConfigCommands();
            configCommands.Register(rootCommand, configOption, debugOption);
            var oneDriveCommands = new OneDriveCommands();
            oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
            var versionCommands = new VersionCommands();
            versionCommands.Register(rootCommand);

            // Print help if no subcommand or arguments are provided
            if (args.Length == 0)
            {
                await rootCommand.InvokeAsync("--help");
                return 0;
            }            // Print available subcommands if no valid subcommand is provided
            rootCommand.TreatUnmatchedTokensAsErrors = true;
            rootCommand.SetHandler((InvocationContext context) =>
            {
                if (context.ParseResult.Tokens.Count == 0)
                {
                    context.Console.WriteLine("Please specify a command to execute. Available commands:");

                    // Display all top-level commands with descriptions
                    foreach (var command in rootCommand.Subcommands)
                    {
                        context.Console.WriteLine($"  {command.Name,-15} {command.Description}");
                    }

                    context.Console.WriteLine("\nRun 'notebookautomation [command] --help' for more information on a specific command.");
                }
            });            // Set a root handler to initialize configuration and logging

            rootCommand.SetHandler((string configPath, bool debug) =>
            {
                // 1. Config file existence check (if provided)
                string? configFile = null;
                if (!string.IsNullOrEmpty(configPath))
                {
                    configFile = configPath;
                    if (!File.Exists(configFile))
                    {
                        AnsiConsoleHelper.WriteError($"Configuration file not found: {configFile}");
                        AnsiConsoleHelper.WriteInfo("You can create a new config with: notebookautomation config update-key <key> <value>");
                        Environment.Exit(1);
                    }
                }
                else
                {
                    configFile = AppConfig.FindConfigFile();
                    if (string.IsNullOrEmpty(configFile) || !File.Exists(configFile))
                    {
                        AnsiConsoleHelper.WriteWarning("No configuration file found. You can create one using the config commands.");
                        AnsiConsoleHelper.WriteInfo("Run: notebookautomation config update-key <key> <value> to create a config file.");
                        Environment.Exit(1);
                    }
                }

                // 2. Config file format/parse check
                AppConfig? appConfig = null;
                try
                {
                    appConfig = AppConfig.LoadFromJsonFile(configFile);
                }
                catch (FileNotFoundException)
                {
                    AnsiConsoleHelper.WriteError($"Configuration file not found: {configFile}");
                    Environment.Exit(1);
                }
                catch (Exception ex)
                {
                    AnsiConsoleHelper.WriteError($"Failed to read configuration: {ex.Message}");
                    AnsiConsoleHelper.WriteInfo("Please check your config file format or recreate it using the config commands.");
                    Environment.Exit(1);
                }

                // 3. Required values check (paths)
                if (appConfig != null && !ConfigValidation.RequireAllPaths(appConfig, out var missingKeys))
                {
                    AnsiConsoleHelper.WriteError("The following required configuration values are missing:");
                    foreach (var key in missingKeys)
                    {
                        Console.WriteLine($"  - {key}");
                    }
                    AnsiConsoleHelper.WriteInfo("You can set missing values with: notebookautomation config update-key <key> <value>");
                    Environment.Exit(1);
                }

                // Setup dependency injection
                // Set an environment variable so AppConfig can log the config path if needed
                if (!string.IsNullOrEmpty(configFile))
                {
                    Environment.SetEnvironmentVariable("NOTEBOOKAUTOMATION_CONFIG_PATH", configFile);
                }
                SetupDependencyInjection(configFile, debug);

                var logger = ServiceProvider.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Notebook Automation initialized");

                if (debug && appConfig != null)
                {
                    logger.LogDebug("Debug logging enabled");
                    logger.LogDebug($"Config file: {configFile}");
                    logger.LogDebug($"Paths: ResourcesRoot={appConfig.Paths.ResourcesRoot}, NotebookVaultRoot={appConfig.Paths.NotebookVaultRoot}, MetadataFile={appConfig.Paths.MetadataFile}, ObsidianVaultRoot={appConfig.Paths.ObsidianVaultRoot}, OneDriveResourcesBasepath={appConfig.Paths.OnedriveResourcesBasepath}, LoggingDir={appConfig.Paths.LoggingDir}");
                    logger.LogDebug($"AI Model: {appConfig.AiService?.Model}");
                    logger.LogDebug($"AI Endpoint: {appConfig.AiService?.Endpoint}");
                    logger.LogDebug($"Video Extensions: {string.Join(", ", appConfig.VideoExtensions ?? new List<string>())}");
                }
            }, configOption, debugOption);

            // Make sure DI container is initialized with default config
            if (_serviceProvider == null)
            {
                SetupDependencyInjection(null, false);
            }

            // Execute the command
            return await rootCommand.InvokeAsync(args);
        }

        /// <summary>
        /// Sets up dependency injection container with configuration and services.
        /// </summary>
        /// <param name="configPath">Path to the configuration file.</param>
        /// <param name="debug">Whether debug mode is enabled.</param>
        public static void SetupDependencyInjection(string? configPath, bool debug)
        {
            // Determine environment
            string environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                                "Development";

            // Build configuration using ConfigurationSetup helper
            var configuration = ConfigurationSetup.BuildConfiguration<Program>(environment, configPath);

            // Setup service collection
            var services = new ServiceCollection();

            // Register configuration
            services.AddSingleton<IConfiguration>(configuration);

            // Add notebook automation services using ServiceRegistration
            services.AddNotebookAutomationServices(configuration, debug, configPath);

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();
        }

    }
}
