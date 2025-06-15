// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Cli;

/// <summary>
/// Service responsible for building and configuring the command line interface.
/// </summary>
/// <remarks>
/// This service handles the creation of the root command, global options, command registration,
/// and parser configuration with middleware for exception handling and help display.
/// </remarks>
internal class CommandLineBuilder
{
    /// <summary>
    /// Creates the root command with description and global options.
    /// </summary>
    /// <returns>A configured RootCommand instance.</returns>
    public RootCommand CreateRootCommand()
    {
        return new RootCommand(
            description: "Comprehensive toolkit for managing course-related content between OneDrive and Obsidian notebooks.");
    }


    /// <summary>
    /// Creates and configures global command line options.
    /// </summary>
    /// <returns>CommandLineOptions containing all global options.</returns>
    public CommandLineOptions CreateGlobalOptions()
    {
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

        return new CommandLineOptions(configOption, debugOption, verboseOption, dryRunOption);
    }


    /// <summary>
    /// Registers all command groups with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>    /// <param name="options">Global command line options.</param>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    public void RegisterCommands(RootCommand rootCommand, CommandLineOptions options, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(rootCommand);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        // Add global options to root command
        rootCommand.AddGlobalOption(options.ConfigOption);
        rootCommand.AddGlobalOption(options.DebugOption);
        rootCommand.AddGlobalOption(options.VerboseOption);
        rootCommand.AddGlobalOption(options.DryRunOption);

        // Get logger factory for command creation
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Register tag commands
        var tagCommands = new TagCommands(loggerFactory.CreateLogger<TagCommands>(), serviceProvider);
        tagCommands.Register(rootCommand, options.ConfigOption, options.DebugOption, options.VerboseOption, options.DryRunOption);        // Register vault commands
        VaultCommands.Register(rootCommand, options.ConfigOption, options.DebugOption, options.VerboseOption, options.DryRunOption);

        // Register video commands
        var videoCommands = new VideoCommands(loggerFactory.CreateLogger<VideoCommands>());
        VideoCommands.Register(rootCommand, options.ConfigOption, options.DebugOption, options.VerboseOption, options.DryRunOption);

        // Register PDF commands
        var pdfCommands = new PdfCommands(loggerFactory.CreateLogger<PdfCommands>());
        PdfCommands.Register(rootCommand, options.ConfigOption, options.DebugOption, options.VerboseOption, options.DryRunOption);

        // Register markdown commands
        var markdownCommands = new MarkdownCommands(
            loggerFactory.CreateLogger<MarkdownCommands>(),
            serviceProvider.GetRequiredService<AppConfig>(),
            serviceProvider);
        markdownCommands.Register(rootCommand, options.ConfigOption, options.DebugOption, options.VerboseOption, options.DryRunOption);

        // Register config commands (note: doesn't support verbose/dry-run)
        var configCommands = new ConfigCommands(loggerFactory.CreateLogger<ConfigCommands>(), serviceProvider);
        configCommands.Register(rootCommand, options.ConfigOption, options.DebugOption);
    }


    /// <summary>
    /// Builds the command line parser with middleware configuration.
    /// </summary>    /// <param name="rootCommand">The root command to build the parser for.</param>
    /// <param name="isDebugMode">Whether debug mode is enabled for exception handling.</param>
    /// <returns>A configured Parser instance.</returns>
    public Parser BuildParser(RootCommand rootCommand, bool isDebugMode)
    {
        ArgumentNullException.ThrowIfNull(rootCommand);

        // Configure default handler for root command
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

        // Create command line builder with exception handling
        var commandLineBuilder = new System.CommandLine.Builder.CommandLineBuilder(rootCommand);

        // Configure exception handler to use centralized ExceptionHandler
        commandLineBuilder.UseExceptionHandler((exception, context) =>
        {
            // Use centralized exception handler for consistent error reporting
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

        return commandLineBuilder.Build();
    }
}
