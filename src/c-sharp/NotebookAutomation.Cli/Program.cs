// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text.Json;

using NotebookAutomation.Core.Configuration;

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
            description: "Simulate actions without making changes"); rootCommand.AddGlobalOption(configOption);
        rootCommand.AddGlobalOption(debugOption);
        rootCommand.AddGlobalOption(verboseOption);
        rootCommand.AddGlobalOption(dryRunOption);        // Check for debug mode from environment or command line
        var isDebugMode = IsDebugModeEnabled(args);
        var isVerboseMode = IsVerboseModeEnabled(args);

        if (isDebugMode)
        {
            AnsiConsoleHelper.WriteInfo($"Debug mode enabled");
        }

        // Parse config option early to use it in dependency injection setup

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
        if (args.Contains("--version"))
        {
            ShowVersionInfo();
            return 0;
        }        // Setup dependency injection with the parsed config path
        IServiceProvider serviceProvider;
        try
        {
            serviceProvider = SetupDependencyInjection(configPath, isDebugMode);
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
                // Use similar logic to ExceptionHandler for user-friendly messages
                var friendlyMessage = GetServiceSetupFriendlyMessage(ex);
                AnsiConsoleHelper.WriteError($"Error: {friendlyMessage}");
                AnsiConsoleHelper.WriteInfo("Run with --debug flag for detailed error information.");
            }

            return 1;
        }
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Program>();

        // Initialize centralized exception handler
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
        PdfCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption); var markdownCommands = new MarkdownCommands(
            loggerFactory.CreateLogger<MarkdownCommands>(),
            serviceProvider.GetRequiredService<AppConfig>(),
            serviceProvider); markdownCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption); var configCommands = new ConfigCommands(loggerFactory.CreateLogger<ConfigCommands>(), serviceProvider);
        configCommands.Register(rootCommand, configOption, debugOption);// Print config file path for most commands, including help
        var isHelp = args.Any(a => a == "--help" || a == "-h");
        var isNoArgs = args.Length == 0;  // Add check for no arguments
        var isConfigOnly = args.Length == 1 && (args[0] == "--config" || args[0] == "-c");  // Check for --config without value
        var isVersion = args.Any(a => a == "--version");
        var isConfigView = args.Length >= 2 && args[0] == "config" && args[1] == "view";        // Show custom help for: no args, explicit help, or --config without value
        if (isNoArgs || isHelp || isConfigOnly)
        {
            await DisplayCustomHelpAsync(rootCommand, configPath, isDebugMode, args);
            return 0;
        }

        // Show config file path for other commands (exclude version and config view)
        if (!isVersion && !isConfigView)
        {
            // Use ConfigManager for consistent config discovery
            var finalConfigPath = await DiscoverConfigurationForDisplayAsync(configPath);
            if (!string.IsNullOrEmpty(finalConfigPath))
            {
                AnsiConsoleHelper.WriteInfo($"Using configuration file: {finalConfigPath}");
            }
            else
            {
                AnsiConsoleHelper.WriteInfo($"No configuration file found. Using defaults.");
            }
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
        });        // The root command no longer handles AI provider/model/endpoint options globally.
        // These are now handled under the config command group only.        // Create command line builder with exception handling using our ExceptionHandler
        var commandLineBuilder = new CommandLineBuilder(rootCommand);// Configure exception handler to use our centralized ExceptionHandler
        commandLineBuilder.UseExceptionHandler((exception, context) =>
        {
            // Use our centralized exception handler for consistent error reporting
            var exitCode = ExceptionHandler.HandleException(exception, "command execution");

            // Set the exit code and signal that the exception has been handled
            context.ExitCode = exitCode;
        });

        // Add other middleware
        commandLineBuilder
            .UseHelp()
            .UseVersionOption()
            .UseTypoCorrections()
            .UseSuggestDirective()
            .UseParseDirective()
            .RegisterWithDotnetSuggest();

        var parser = commandLineBuilder.Build();

        // Execute the command
        return await parser.InvokeAsync(args).ConfigureAwait(false);
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
                             "Production";

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
    }    /// <summary>
         /// Displays professional version information for the application.
         /// </summary>
         /// <remarks>
         /// Shows version, runtime information, author, and copyright in a well-formatted style.
         /// Used by both --version option and version command for consistency.
         /// </remarks>
    public static void ShowVersionInfo()
    {
        try
        {
            Assembly? entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                // Get version information from the executable
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                var versionInfo = !string.IsNullOrEmpty(exePath)
                    ? FileVersionInfo.GetVersionInfo(exePath)
                    : null;

                // Extract version details
                var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
                string fileVersion = versionInfo?.FileVersion ?? assemblyVersion?.ToString() ?? "1.0.0.0";
                string productName = versionInfo?.ProductName ?? "Notebook Automation";
                string companyName = versionInfo?.CompanyName ?? "Dan Shue";
                string copyrightInfo = versionInfo?.LegalCopyright ?? "Copyright 2025";

                // Display professional version information
                Console.WriteLine();
                Console.WriteLine($"{AnsiColors.BOLD}{productName}{AnsiColors.ENDC} {AnsiColors.OKGREEN}v{fileVersion}{AnsiColors.ENDC}");
                Console.WriteLine($"Runtime: {AnsiColors.OKCYAN}.NET {Environment.Version}{AnsiColors.ENDC}");
                Console.WriteLine($"Author:  {AnsiColors.GREY}{companyName}{AnsiColors.ENDC}");
                Console.WriteLine($"{AnsiColors.GREY}{copyrightInfo}{AnsiColors.ENDC}");
                Console.WriteLine();
            }
            else
            {
                AnsiConsoleHelper.WriteError("Unable to retrieve version information.");
            }
        }
        catch (Exception ex)
        {
            AnsiConsoleHelper.WriteError($"Error retrieving version information: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts service setup exceptions to user-friendly messages.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <returns>User-friendly error message.</returns>
    private static string GetServiceSetupFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            InvalidOperationException ioe when ioe.Message.Contains("OpenAI API key is missing") =>
                "OpenAI API key is missing. Please set the OPENAI_API_KEY environment variable with your API key",
            InvalidOperationException ioe when ioe.Message.Contains("Azure OpenAI API key is missing") =>
                "Azure OpenAI API key is missing. Please set the AZURE_OPENAI_KEY environment variable with your API key",
            InvalidOperationException ioe when ioe.Message.Contains("Foundry API key is missing") =>
                "Foundry API key is missing. Please set the FOUNDRY_API_KEY environment variable with your API key",
            InvalidOperationException ioe when ioe.Message.Contains("endpoint is missing") =>
                "AI service endpoint configuration is missing. Please check your configuration file",
            InvalidOperationException ioe when ioe.Message.Contains("deployment name is missing") =>
                "Azure OpenAI deployment name is missing. Please check your configuration file",
            InvalidOperationException ioe when ioe.Message.Contains("Configuration") =>
                "Configuration error. Please check your config file and ensure all required settings are present",
            FileNotFoundException fnf => $"Required file not found: {fnf.FileName ?? "Unknown file"}",
            DirectoryNotFoundException => "Required directory not found",
            _ => $"Failed to initialize services: {exception.Message}"
        };
    }

    /// <summary>
    /// Discovers the configuration file path using the same logic as ConfigManager for consistent display.
    /// </summary>
    /// <param name="explicitConfigPath">Explicitly provided config path via CLI.</param>
    /// <returns>Path to the configuration file if found, otherwise null.</returns>
    private static async Task<string?> DiscoverConfigurationForDisplayAsync(string? explicitConfigPath)
    {
        var fileSystem = new FileSystemWrapper();
        var environment = new EnvironmentWrapper();
        var logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .CreateLogger<ConfigManager>();

        var configManager = new ConfigManager(fileSystem, environment, logger);

        var options = new ConfigDiscoveryOptions
        {
            ConfigPath = explicitConfigPath,
            Debug = false,
            ExecutableDirectory = environment.GetExecutableDirectory(),
            WorkingDirectory = environment.GetCurrentDirectory()
        };

        var result = await configManager.LoadConfigurationAsync(options);
        return result.IsSuccess ? result.ConfigurationPath : null;
    }

    /// <summary>
    /// Displays custom help with current environment information.
    /// </summary>
    /// <param name="rootCommand">The root command to display help for.</param>
    /// <param name="configPath">The configuration file path.</param>
    /// <param name="isDebug">Whether debug mode is enabled.</param>
    /// <param name="args">The original command line arguments.</param>
    /// <returns>Task representing the async operation.</returns>
    private static async Task DisplayCustomHelpAsync(RootCommand rootCommand, string? configPath, bool isDebug, string[] args)
    {
        // Get the configuration path for display
        var finalConfigPath = await DiscoverConfigurationForDisplayAsync(configPath);

        // Display description
        Console.WriteLine($"{AnsiColors.BOLD}Description:{AnsiColors.ENDC}");
        Console.WriteLine($"  {rootCommand.Description}");
        Console.WriteLine();

        // Display usage
        Console.WriteLine($"{AnsiColors.BOLD}Usage:{AnsiColors.ENDC}");
        Console.WriteLine($"  na [command] [options]");
        Console.WriteLine();        // Display current environment
        Console.WriteLine($"{AnsiColors.HEADER}Current Environment:{AnsiColors.ENDC}");
        await DisplayEnvironmentSettingsAsync(finalConfigPath, isDebug, args);
        Console.WriteLine();
        // Get and display options
        Console.WriteLine($"{AnsiColors.BOLD}Options:{AnsiColors.ENDC}");
        foreach (var option in rootCommand.Options)
        {
            var aliases = string.Join(", ", option.Aliases);
            var description = option.Description ?? "";
            Console.WriteLine($"  {aliases,-25} {description}");
        }
        // Add help and version options that are built-in
        Console.WriteLine($"  {"-?, -h, --help",-25} Show help and usage information");
        Console.WriteLine($"  {"--version",-25} Show version information");
        Console.WriteLine();

        // Get and display commands
        Console.WriteLine($"{AnsiColors.BOLD}Commands:{AnsiColors.ENDC}");
        foreach (var command in rootCommand.Subcommands)
        {
            Console.WriteLine($"  {command.Name,-18} {command.Description}");
        }
    }    /// <summary>
         /// Displays current environment settings with appropriate colors and masking.
         /// </summary>
         /// <param name="configPath">The configuration file path.</param>
         /// <param name="isDebug">Whether debug mode is enabled.</param>
         /// <param name="args">Command line arguments for determining mode sources.</param>
         /// <returns>Task representing the async operation.</returns>
    private static async Task DisplayEnvironmentSettingsAsync(string? configPath, bool isDebug, string[] args)
    {
        // Configuration file status
        if (!string.IsNullOrEmpty(configPath))
        {
            Console.WriteLine($"  Configuration:    {AnsiColors.GREY}{configPath}{AnsiColors.ENDC} {AnsiColors.OKGREEN}✓{AnsiColors.ENDC}");
        }
        else
        {
            Console.WriteLine($"  Configuration:    {AnsiColors.WARNING}Using defaults (no config.json found) ⚠{AnsiColors.ENDC}");
        }        // Debug mode status with source indication
        var debugEnvVar = Environment.GetEnvironmentVariable("DEBUG");
        var debugFromEnv = !string.IsNullOrEmpty(debugEnvVar) &&
                          (debugEnvVar.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                           debugEnvVar.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                           debugEnvVar.Equals("yes", StringComparison.OrdinalIgnoreCase));

        if (isDebug)
        {
            if (debugFromEnv)
            {
                Console.WriteLine($"  Debug Mode:       {AnsiColors.WARNING}Enabled via ENV ⚡{AnsiColors.ENDC}");
            }
            else
            {
                Console.WriteLine($"  Debug Mode:       {AnsiColors.WARNING}Enabled ⚡{AnsiColors.ENDC}");
            }
        }
        else
        {
            Console.WriteLine($"  Debug Mode:       {AnsiColors.GREY}Disabled{AnsiColors.ENDC}");
        }

        // Verbose mode status with source indication
        var isVerbose = IsVerboseModeEnabled(args);
        var verboseEnvVar = Environment.GetEnvironmentVariable("VERBOSE");
        var verboseFromEnv = !string.IsNullOrEmpty(verboseEnvVar) &&
                            (verboseEnvVar.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                             verboseEnvVar.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                             verboseEnvVar.Equals("yes", StringComparison.OrdinalIgnoreCase));

        if (isVerbose)
        {
            if (verboseFromEnv)
            {
                Console.WriteLine($"  Verbose Mode:     {AnsiColors.OKCYAN}Enabled via ENV 📢{AnsiColors.ENDC}");
            }
            else
            {
                Console.WriteLine($"  Verbose Mode:     {AnsiColors.OKCYAN}Enabled 📢{AnsiColors.ENDC}");
            }
        }
        else
        {
            Console.WriteLine($"  Verbose Mode:     {AnsiColors.GREY}Disabled{AnsiColors.ENDC}");
        }

        // Working directory
        Console.WriteLine($"  Working Dir:      {AnsiColors.GREY}{Environment.CurrentDirectory}{AnsiColors.ENDC}");

        // .NET Runtime
        Console.WriteLine($"  .NET Runtime:     {AnsiColors.OKCYAN}{Environment.Version}{AnsiColors.ENDC}");

        // Load and display configuration settings if available
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            try
            {
                await DisplayConfigurationSettingsAsync(configPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Config Status:    {AnsiColors.WARNING}Error reading config: {ex.Message}{AnsiColors.ENDC}");
            }
        }
    }

    /// <summary>
    /// Displays configuration settings from the config file, masking sensitive values.
    /// </summary>
    /// <param name="configPath">Path to the configuration file.</param>
    /// <returns>Task representing the async operation.</returns>
    private static async Task DisplayConfigurationSettingsAsync(string configPath)
    {
        try
        {
            var configJson = await File.ReadAllTextAsync(configPath);
            var configDoc = JsonDocument.Parse(configJson);
            var root = configDoc.RootElement;
            // Display paths section
            if (root.TryGetProperty("paths", out var paths))
            {
                if (paths.TryGetProperty("notebook_vault_fullpath_root", out var vaultPath))
                {
                    Console.WriteLine($"  Obsidian Vault Root: {AnsiColors.GREY}{vaultPath.GetString()}{AnsiColors.ENDC}");
                }
                if (paths.TryGetProperty("onedrive_fullpath_root", out var oneDrivePath))
                {
                    Console.WriteLine($"  OneDrive Root:    {AnsiColors.GREY}{oneDrivePath.GetString()}{AnsiColors.ENDC}");
                }
                if (paths.TryGetProperty("prompts_path", out var promptsPath))
                {
                    Console.WriteLine($"  Prompts Path:     {AnsiColors.GREY}{promptsPath.GetString()}{AnsiColors.ENDC}");
                }
            }// Display AI service configuration
            if (root.TryGetProperty("aiservice", out var aiService))
            {
                if (aiService.TryGetProperty("provider", out var provider))
                {
                    Console.WriteLine($"  AI Provider:      {AnsiColors.OKCYAN}{provider.GetString()}{AnsiColors.ENDC}");
                }

                // Check for Azure configuration
                if (aiService.TryGetProperty("azure", out var azure))
                {
                    if (azure.TryGetProperty("endpoint", out var endpoint))
                    {
                        Console.WriteLine($"  AI Endpoint:      {AnsiColors.GREY}{endpoint.GetString()}{AnsiColors.ENDC}");
                    }
                    if (azure.TryGetProperty("deployment", out var deployment))
                    {
                        Console.WriteLine($"  AI Deployment:    {AnsiColors.OKCYAN}{deployment.GetString()}{AnsiColors.ENDC}");
                    }
                    if (azure.TryGetProperty("model", out var model))
                    {
                        Console.WriteLine($"  AI Model:         {AnsiColors.OKCYAN}{model.GetString()}{AnsiColors.ENDC}");
                    }

                    // Check for API key in config (unlikely to be there, but check anyway)
                    var hasApiKey = azure.TryGetProperty("api_key", out var apiKey) &&
                                   !string.IsNullOrWhiteSpace(apiKey.GetString());
                    if (hasApiKey)
                    {
                        Console.WriteLine($"  Azure API Key:    {AnsiColors.OKGREEN}[SET] ✓{AnsiColors.ENDC}");
                    }
                    else
                    {
                        // Check environment variable for Azure OpenAI key
                        var envKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
                        if (!string.IsNullOrWhiteSpace(envKey))
                        {
                            Console.WriteLine($"  Azure API Key:    {AnsiColors.OKGREEN}[SET via ENV] ✓{AnsiColors.ENDC}");
                        }
                        else
                        {
                            Console.WriteLine($"  Azure API Key:    {AnsiColors.WARNING}[NOT SET] ⚠{AnsiColors.ENDC}");
                        }
                    }
                }

                // Check for OpenAI configuration
                if (aiService.TryGetProperty("openai", out var openAi))
                {
                    if (openAi.TryGetProperty("endpoint", out var endpoint))
                    {
                        Console.WriteLine($"  OpenAI Endpoint:  {AnsiColors.GREY}{endpoint.GetString()}{AnsiColors.ENDC}");
                    }
                    if (openAi.TryGetProperty("model", out var model))
                    {
                        Console.WriteLine($"  OpenAI Model:     {AnsiColors.OKCYAN}{model.GetString()}{AnsiColors.ENDC}");
                    }

                    var hasApiKey = openAi.TryGetProperty("api_key", out var apiKey) &&
                                   !string.IsNullOrWhiteSpace(apiKey.GetString());
                    if (hasApiKey)
                    {
                        Console.WriteLine($"  OpenAI API Key:   {AnsiColors.OKGREEN}[SET] ✓{AnsiColors.ENDC}");
                    }
                    else
                    {
                        // Check environment variable for OpenAI key
                        var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                        if (!string.IsNullOrWhiteSpace(envKey))
                        {
                            Console.WriteLine($"  OpenAI API Key:   {AnsiColors.OKGREEN}[SET via ENV] ✓{AnsiColors.ENDC}");
                        }
                        else
                        {
                            Console.WriteLine($"  OpenAI API Key:   {AnsiColors.WARNING}[NOT SET] ⚠{AnsiColors.ENDC}");
                        }
                    }
                }

                // Check for Foundry configuration
                if (aiService.TryGetProperty("foundry", out var foundry))
                {
                    if (foundry.TryGetProperty("endpoint", out var endpoint))
                    {
                        Console.WriteLine($"  Foundry Endpoint: {AnsiColors.GREY}{endpoint.GetString()}{AnsiColors.ENDC}");
                    }
                    if (foundry.TryGetProperty("model", out var model))
                    {
                        Console.WriteLine($"  Foundry Model:    {AnsiColors.OKCYAN}{model.GetString()}{AnsiColors.ENDC}");
                    }

                    // Check environment variable for Foundry key
                    var envKey = Environment.GetEnvironmentVariable("FOUNDRY_API_KEY");
                    if (!string.IsNullOrWhiteSpace(envKey))
                    {
                        Console.WriteLine($"  Foundry API Key:  {AnsiColors.OKGREEN}[SET via ENV] ✓{AnsiColors.ENDC}");
                    }
                    else
                    {
                        Console.WriteLine($"  Foundry API Key:  {AnsiColors.WARNING}[NOT SET] ⚠{AnsiColors.ENDC}");
                    }
                }
            }
            // Check for Microsoft Graph configuration
            if (root.TryGetProperty("microsoft_graph", out var msGraph))
            {
                if (msGraph.TryGetProperty("client_id", out var clientId))
                {
                    Console.WriteLine($"  OneDrive Graph Client ID: {AnsiColors.GREY}{clientId.GetString()}{AnsiColors.ENDC}");
                }
            }
        }
        catch (JsonException)
        {
            Console.WriteLine($"  Config Status:    {AnsiColors.WARNING}Invalid JSON format ⚠{AnsiColors.ENDC}");
        }
    }

    /// <summary>
    /// Determines if debug mode is enabled from environment variable or command line arguments.
    /// Environment variable takes precedence over command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>True if debug mode should be enabled.</returns>
    private static bool IsDebugModeEnabled(string[] args)
    {
        // Check environment variable first
        var envDebug = Environment.GetEnvironmentVariable("DEBUG");
        if (!string.IsNullOrEmpty(envDebug))
        {
            return envDebug.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   envDebug.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   envDebug.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        // Fall back to command line arguments
        return args.Contains("--debug") || args.Contains("-d");
    }

    /// <summary>
    /// Determines if verbose mode is enabled from environment variable or command line arguments.
    /// Environment variable takes precedence over command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>True if verbose mode should be enabled.</returns>
    private static bool IsVerboseModeEnabled(string[] args)
    {
        // Check environment variable first
        var envVerbose = Environment.GetEnvironmentVariable("VERBOSE");
        if (!string.IsNullOrEmpty(envVerbose))
        {
            return envVerbose.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   envVerbose.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   envVerbose.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        // Fall back to command line arguments
        return args.Contains("--verbose") || args.Contains("-v");
    }
}
