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
        // Initialize services
        var bootstrapper = new ApplicationBootstrapper();
        var configDiscoveryService = new ConfigurationDiscoveryService();

        // Create command line builder and setup
        var commandLineBuilder = new Cli.CommandLineBuilder();
        var rootCommand = commandLineBuilder.CreateRootCommand();
        var options = commandLineBuilder.CreateGlobalOptions();

        // Check for debug mode from environment or command line
        var isDebugMode = CommandLineModeDetector.IsDebugModeEnabled(args);
        var isVerboseMode = CommandLineModeDetector.IsVerboseModeEnabled(args);

        if (isDebugMode)
        {
            AnsiConsoleHelper.WriteInfo($"Debug mode enabled");
        }

        // Parse config option early to use it in dependency injection setup
        var configPath = configDiscoveryService.ParseConfigPathFromArgs(args);

        // Handle --version manually before building the CLI to avoid conflicts
        if (args.Contains("--version"))
        {
            var helpDisplayService = new HelpDisplayService(new EnvironmentDisplayService(configDiscoveryService));
            helpDisplayService.ShowVersionInfo();
            return 0;
        }
        // Setup dependency injection with the parsed config path
        IServiceProvider serviceProvider;
        try
        {
            serviceProvider = bootstrapper.SetupDependencyInjection(configPath, isDebugMode);
            Program.serviceProvider = serviceProvider; // Set the static field
        }
        catch (Exception ex)
        {
            // Handle DI setup failures before ExceptionHandler is available
            var isDebug = args.Contains("--debug") || args.Contains("-d");

            if (isDebug)
            {
                AnsiConsoleHelper.WriteError($"Failed to initialize services: {ex.Message}");
                AnsiConsoleHelper.WriteError($"Exception Type: {ex.GetType().FullName}");
                if (ex.StackTrace != null)
                {
                    AnsiConsoleHelper.WriteError("Stack Trace:");
                    AnsiConsoleHelper.WriteError(ex.StackTrace);
                }
            }
            else
            {
                var friendlyMessage = bootstrapper.GetServiceSetupFriendlyMessage(ex);
                AnsiConsoleHelper.WriteError($"Error: {friendlyMessage}");
                AnsiConsoleHelper.WriteInfo("Run with --debug flag for detailed error information.");
            }

            return 1;
        }
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Program>();

        // Initialize centralized exception handler
        ExceptionHandler.Initialize(logger, isDebugMode); logger.LogDebug("Application started");

        // Register commands with the command line builder
        commandLineBuilder.RegisterCommands(rootCommand, options, serviceProvider);

        // Check for help display scenarios
        var isHelp = args.Any(a => a == "--help" || a == "-h");
        var isNoArgs = args.Length == 0;
        var isConfigOnly = args.Length == 1 && (args[0] == "--config" || args[0] == "-c");
        var isVersion = args.Any(a => a == "--version");
        var isConfigView = args.Contains("config") && args.Contains("view") &&
                          Array.IndexOf(args, "view") == Array.IndexOf(args, "config") + 1;

        // Show custom help for: no args, explicit help, or --config without value
        if (isNoArgs || isHelp || isConfigOnly)
        {
            var helpDisplayService = serviceProvider.GetRequiredService<HelpDisplayService>();
            await helpDisplayService.DisplayCustomHelpAsync(rootCommand, configPath, isDebugMode, args);
            return 0;
        }

        // Show config file path for other commands (exclude version and config view)
        if (!isVersion && !isConfigView)
        {
            var finalConfigPath = await configDiscoveryService.DiscoverConfigurationForDisplayAsync(configPath);
            if (!string.IsNullOrEmpty(finalConfigPath))
            {
                AnsiConsoleHelper.WriteInfo($"Using configuration file: {finalConfigPath}");
            }
            else
            {
                AnsiConsoleHelper.WriteInfo($"No configuration file found. Using defaults.");
            }
        }

        // Build and execute parser
        var parser = commandLineBuilder.BuildParser(rootCommand, isDebugMode);
        return await parser.InvokeAsync(args).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets up the dependency injection container with configuration and services.
    /// </summary>
    /// <param name="configPath">Path to the configuration file.</param>
    /// <param name="debug">Whether debug mode is enabled.</param>
    /// <returns>An <see cref="IServiceProvider"/> instance configured with application services.</returns>
    /// <remarks>
    /// This method provides compatibility for existing command classes.
    /// Consider using ApplicationBootstrapper directly for new code.
    /// </remarks>
    public static IServiceProvider SetupDependencyInjection(string? configPath, bool debug)
    {
        var bootstrapper = new ApplicationBootstrapper();
        var serviceProvider = bootstrapper.SetupDependencyInjection(configPath, debug);
        Program.serviceProvider = serviceProvider; // Set the static field
        return serviceProvider;
    }
}
