// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Cli.Services.Copilot;

namespace NotebookAutomation.Cli.Commands;

/// <summary>
/// Commands for interacting with GitHub Copilot.
/// </summary>
public class CopilotCommands
{
    private readonly ILogger<CopilotCommands> logger;
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotCommands"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    public CopilotCommands(
        ILogger<CopilotCommands> logger,
        IServiceProvider serviceProvider)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Register Copilot commands with the CLI.
    /// </summary>
    /// <param name="rootCommand">The root command to add subcommands to.</param>
    public void RegisterCommands(RootCommand rootCommand)
    {
        ArgumentNullException.ThrowIfNull(rootCommand);

        // Create 'copilot' parent command
        var copilotCommand = new Command("copilot", "GitHub Copilot commands and setup");

        var installGuideOption = new Option<bool>(
            "--install-guide",
            "Display platform-specific installation instructions for GitHub Copilot CLI");
        copilotCommand.AddOption(installGuideOption);

        var installOption = new Option<bool>(
            "--install",
            "Attempt to automatically install GitHub Copilot CLI (Windows only)");
        copilotCommand.AddOption(installOption);

        var statusOption = new Option<bool>(
            "--status",
            "Check GitHub Copilot CLI availability and authentication status");
        statusOption.AddAlias("-s");
        copilotCommand.AddOption(statusOption);

        copilotCommand.SetHandler(async (context) =>
        {
            var showInstallGuide = context.ParseResult.GetValueForOption(installGuideOption);
            var attemptInstall = context.ParseResult.GetValueForOption(installOption);
            var showStatus = context.ParseResult.GetValueForOption(statusOption);

            await CopilotSetupCommandHandlerAsync(
                showInstallGuide,
                attemptInstall,
                showStatus,
                context.GetCancellationToken());
        });

        rootCommand.AddCommand(copilotCommand);

        // Create 'chat' command
        var chatCommand = new Command("chat", "Enter interactive AI chat mode");

        var resumeOption = new Option<bool>(
            "--resume",
            "Resume the last chat session");
        chatCommand.AddOption(resumeOption);

        var sessionOption = new Option<string?>(
            "--session",
            "Resume a specific chat session by ID");
        chatCommand.AddOption(sessionOption);

        var modelOption = new Option<string?>(
            "--model",
            "Specify the AI model to use");
        chatCommand.AddOption(modelOption);

        var noBannerOption = new Option<bool>(
            "--no-banner",
            "Skip the welcome banner");
        chatCommand.AddOption(noBannerOption);

        var highContrastOption = new Option<bool>(
            "--high-contrast",
            "Use high contrast colors");
        chatCommand.AddOption(highContrastOption);

        chatCommand.SetHandler(async (context) =>
        {
            var resume = context.ParseResult.GetValueForOption(resumeOption);
            var sessionId = context.ParseResult.GetValueForOption(sessionOption);
            var model = context.ParseResult.GetValueForOption(modelOption);
            var noBanner = context.ParseResult.GetValueForOption(noBannerOption);
            var highContrast = context.ParseResult.GetValueForOption(highContrastOption);

            await ChatCommandHandlerAsync(
                resume,
                sessionId,
                model,
                !noBanner,
                highContrast,
                context.GetCancellationToken());
        });

        rootCommand.AddCommand(chatCommand);

        // Create 'ask' command
        var askCommand = new Command("ask", "Ask a one-shot question to the AI");

        var questionArgument = new Argument<string>(
            "question",
            "The question to ask");
        askCommand.AddArgument(questionArgument);

        var askModelOption = new Option<string?>(
            "--model",
            "Specify the AI model to use");
        askCommand.AddOption(askModelOption);

        var jsonOption = new Option<bool>(
            "--json",
            "Output response in JSON format");
        askCommand.AddOption(jsonOption);

        var noStreamOption = new Option<bool>(
            "--no-stream",
            "Disable streaming output");
        askCommand.AddOption(noStreamOption);

        askCommand.SetHandler(async (context) =>
        {
            var question = context.ParseResult.GetValueForArgument(questionArgument);
            var model = context.ParseResult.GetValueForOption(askModelOption);
            var json = context.ParseResult.GetValueForOption(jsonOption);
            var noStream = context.ParseResult.GetValueForOption(noStreamOption);

            await AskCommandHandlerAsync(
                question,
                model,
                json,
                !noStream,
                context.GetCancellationToken());
        });

        rootCommand.AddCommand(askCommand);
    }

    /// <summary>
    /// Handler for the 'chat' command.
    /// </summary>
    private async Task ChatCommandHandlerAsync(
        bool resume,
        string? sessionId,
        string? model,
        bool showBanner,
        bool highContrast,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting chat mode");

        try
        {
            var copilotService = serviceProvider.GetRequiredService<ICopilotService>();
            var config = serviceProvider.GetRequiredService<AppConfig>();
            var welcomeBanner = new WelcomeBanner(config);
            var builtInCommands = new ChatBuiltInCommands(
                serviceProvider.GetRequiredService<ILogger<ChatBuiltInCommands>>(),
                copilotService);

            var chatUI = new ChatModeUI(
                serviceProvider.GetRequiredService<ILogger<ChatModeUI>>(),
                copilotService,
                welcomeBanner,
                builtInCommands,
                highContrast);

            var options = new ChatModeOptions
            {
                Resume = resume,
                SessionId = sessionId,
                Model = model,
                ShowBanner = showBanner,
                HighContrast = highContrast
            };

            var exitCode = await chatUI.RunAsync(options, cancellationToken);
            Environment.ExitCode = exitCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in chat command");
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
            Environment.ExitCode = 1;
        }
    }

    /// <summary>
    /// Handler for the 'copilot' setup command.
    /// </summary>
    private async Task CopilotSetupCommandHandlerAsync(
        bool showInstallGuide,
        bool attemptInstall,
        bool showStatus,
        CancellationToken cancellationToken)
    {
        // Default to status check if no other option specified
        if (!showInstallGuide && !attemptInstall && !showStatus)
        {
            showStatus = true;
        }

        if (showInstallGuide)
        {
            CopilotInstallationGuide.DisplayInstructions();
            return;
        }

        if (attemptInstall)
        {
            AnsiConsole.MarkupLine("[bold]Attempting to install GitHub Copilot CLI...[/]");
            AnsiConsole.WriteLine();

            var success = await CopilotInstallationGuide.TryInstallAsync(logger, cancellationToken);

            if (success)
            {
                AnsiConsole.MarkupLine("[green]Installation complete![/]");
                AnsiConsole.MarkupLine("[dim]Please restart your terminal to refresh PATH.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Automatic installation failed or is not supported on this platform.[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Run [cyan]na copilot --install-guide[/] for manual installation instructions.[/]");
                Environment.ExitCode = 1;
            }

            return;
        }

        if (showStatus)
        {
            var copilotService = serviceProvider.GetRequiredService<ICopilotService>();
            var availability = await copilotService.CheckAvailabilityAsync(cancellationToken);

            AnsiConsole.MarkupLine("[bold]GitHub Copilot Status[/]");
            AnsiConsole.WriteLine();

            var table = new Table();
            table.AddColumn("Check");
            table.AddColumn("Status");

            table.AddRow(
                "CLI Installed",
                availability.IsCliInstalled
                    ? "[green]✓ Yes[/]"
                    : "[red]✗ No[/]");

            table.AddRow(
                "Authenticated",
                availability.IsAuthenticated
                    ? "[green]✓ Yes[/]"
                    : availability.IsCliInstalled ? "[red]✗ No[/]" : "[dim]-[/]");

            table.AddRow(
                "Available",
                availability.IsAvailable
                    ? "[green]✓ Ready[/]"
                    : "[yellow]✗ Not Ready[/]");

            if (!string.IsNullOrEmpty(availability.CliVersion))
            {
                table.AddRow("CLI Version", availability.CliVersion);
            }

            AnsiConsole.Write(table);

            if (!availability.IsAvailable)
            {
                AnsiConsole.WriteLine();
                if (!availability.IsCliInstalled)
                {
                    AnsiConsole.MarkupLine("[dim]Run [cyan]na copilot --install-guide[/] for installation instructions.[/]");
                }
                else if (!availability.IsAuthenticated)
                {
                    AnsiConsole.MarkupLine("[dim]Run [cyan]gh auth login --scopes copilot[/] to authenticate.[/]");
                }

                if (availability.ErrorMessage != null)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[dim]{availability.ErrorMessage.EscapeMarkup()}[/]");
                }

                Environment.ExitCode = 1;
            }
        }
    }

    /// <summary>
    /// Handler for the 'ask' command.
    /// </summary>
    private async Task AskCommandHandlerAsync(
        string question,
        string? model,
        bool json,
        bool stream,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Executing ask command");

        try
        {
            var copilotService = serviceProvider.GetRequiredService<ICopilotService>();

            // Check availability
            var availability = await copilotService.CheckAvailabilityAsync(cancellationToken);
            if (!availability.IsAvailable)
            {
                AnsiConsole.MarkupLine("[red]GitHub Copilot is not available[/]");

                if (availability.ErrorMessage != null)
                {
                    AnsiConsole.MarkupLine($"[yellow]{availability.ErrorMessage.EscapeMarkup()}[/]");
                }

                Environment.ExitCode = 1;
                return;
            }

            // Start the service
            await copilotService.StartAsync(cancellationToken: cancellationToken);

            try
            {
                var options = new AskOptions
                {
                    Model = model,
                    Json = json,
                    Stream = stream
                };

                var response = await copilotService.AskAsync(question, options, cancellationToken);

                if (json)
                {
                    Console.WriteLine(response);
                }
                else
                {
                    AnsiConsole.MarkupLine(response.EscapeMarkup());
                }

                Environment.ExitCode = 0;
            }
            finally
            {
                await copilotService.StopAsync(cancellationToken);
            }
        }
        catch (NotImplementedException)
        {
            AnsiConsole.MarkupLine("[yellow]The 'ask' command is using stub AI responses[/]");
            AnsiConsole.MarkupLine($"[dim]You asked: {question.EscapeMarkup()}[/]");
            Environment.ExitCode = 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ask command");
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
            Environment.ExitCode = 1;
        }
    }
}
