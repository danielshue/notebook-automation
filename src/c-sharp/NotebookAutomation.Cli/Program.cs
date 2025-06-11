// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli;

/// <summary>
/// Main entry point for the Notebook Automation CLI.
/// </summary>
/// <remarks>
/// This program provides a unified command-line interface for accessing
/// all the notebook automation tools, including commands for managing
/// course-related content between OneDrive and Obsidian notebooks.
/// </remarks>
internal class Program
{
    private static IServiceProvider? serviceProvider;

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the service provider is not initialized.</exception>
    public static IServiceProvider ServiceProvider
    {
        get => serviceProvider ?? throw new InvalidOperationException("Service provider not initialized. Call SetupDependencyInjection first.");
    }
    /// <summary>
    /// Entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code (0 for success, non-zero for error).</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            return await ExecuteMainAsync(args).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Fallback exception handling if ExceptionHandler isn't initialized yet
            var isDebug = args.Contains("--debug") || args.Contains("-d");

            if (isDebug)
            {
                AnsiConsoleHelper.WriteError($"Unhandled exception: {ex}");
            }
            else
            {
                AnsiConsoleHelper.WriteError($"A critical error occurred: {ex.Message}");
                AnsiConsoleHelper.WriteInfo("Run with --debug flag for detailed error information.");
            }

            return 1;
        }
    }

    /// <summary>
    /// Main execution logic with proper exception handling.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code (0 for success, non-zero for error).</returns>
    private static async Task<int> ExecuteMainAsync(string[] args)
    {
        // Create the root command with description
        var rootCommand = new RootCommand(
            description: "Comprehensive toolkit for managing course-related content between OneDrive and Obsidian notebooks.");

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
        } // Parse config option early to use it in dependency injection setup

        string? configPath = null;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--config" || args[i] == "-c")
            {
                configPath = args[i + 1];
                break;
            }
        }

        // Handle --version manually before building the CLI to avoid conflicts
        // if (args.Contains("--version") || args.Contains("-v"))
        // {
        //     AnsiConsoleHelper.WriteInfo($"Notebook Automation v1.0.0");
        //     AnsiConsoleHelper.WriteInfo($"Running on .NET {Environment.Version}");
        //     AnsiConsoleHelper.WriteInfo("(c) 2025 Dan Shue");
        //     return 0;
        // }
        // Setup dependency injection with the parsed config path
        var serviceProvider = SetupDependencyInjection(configPath, args.Contains("--debug"));
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Program>();

        // Initialize centralized exception handler
        var isDebugMode = args.Contains("--debug") || args.Contains("-d");
        ExceptionHandler.Initialize(logger, isDebugMode);

        logger.LogDebug("Application started");

        // Initialize the AppConfig instance
        var tagCommands = new TagCommands(loggerFactory.CreateLogger<TagCommands>(), serviceProvider);
        tagCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
        var vaultCommands = new VaultCommands(loggerFactory.CreateLogger<VaultCommands>(), serviceProvider);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var videoCommands = new VideoCommands(loggerFactory.CreateLogger<VideoCommands>());
        VideoCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);
        var pdfCommands = new PdfCommands(loggerFactory.CreateLogger<PdfCommands>());
        PdfCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var markdownCommands = new MarkdownCommands(
            loggerFactory.CreateLogger<MarkdownCommands>(),
            serviceProvider.GetRequiredService<AppConfig>(),
            serviceProvider);
        markdownCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var configCommands = new ConfigCommands();
        ConfigCommands.Register(rootCommand, configOption, debugOption);

        var oneDriveCommands = new OneDriveCommands(loggerFactory.CreateLogger<OneDriveCommands>());
        oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var versionCommands = new VersionCommands();
        VersionCommands.Register(rootCommand);

        // Print help if no subcommand or arguments are provided
        if (args.Length == 0)
        {
            await rootCommand.InvokeAsync("--help").ConfigureAwait(false);
            return 0;
        }

        // Print available subcommands if no valid subcommand is provided
        rootCommand.TreatUnmatchedTokensAsErrors = true;
        rootCommand.SetHandler(context =>
        {
            if (context.ParseResult.Tokens.Count == 0)
            {
                context.Console.WriteLine("Please specify a command to execute. Available commands:");

                // Display all top-level commands with descriptions
                foreach (var command in rootCommand.Subcommands)
                {
                    context.Console.WriteLine($"  {command.Name,-15} {command.Description}");
                }

                context.Console.WriteLine("\nRun 'notebookautomation.exe [command] --help' for more information on a specific command.");
            }
        });

        // The root command no longer handles AI provider/model/endpoint options globally.
        // These are now handled under the config command group only.

        // Print config file path before any command except help/version
        var isHelp = args.Any(a => a == "--help" || a == "-h");
        var isVersion = args.Any(a => a == "--version" || a == "-v");
        var isConfigView = args.Length >= 2 && args[0] == "config" && args[1] == "view";
        if (!isHelp && !isVersion && !isConfigView)
        {
            // Use the already parsed config path, else fallback
            var finalConfigPath = configPath ?? AppConfig.FindConfigFile();
            if (!string.IsNullOrEmpty(finalConfigPath))
            {
                AnsiConsoleHelper.WriteInfo($"Using configuration file: {finalConfigPath}");
            }
            else
            {
                AnsiConsoleHelper.WriteInfo($"No configuration file found. Using defaults.");
            }
        }

        // Execute the command
        return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets up the dependency injection container with configuration and services.
    /// </summary>
    /// <param name="configPath">Path to the configuration file.</param>
    /// <param name="debug">Whether debug mode is enabled.</param>
    /// <returns>An <see cref="IServiceProvider"/> instance configured with application services.</returns>
    public static IServiceProvider SetupDependencyInjection(string? configPath, bool debug)
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
        services.AddSingleton(configuration);        // Add notebook automation services using ServiceRegistration
        services.AddNotebookAutomationServices(configuration, debug, configPath);

        // Build service provider
        serviceProvider = services.BuildServiceProvider();

        return serviceProvider;
    }
}
