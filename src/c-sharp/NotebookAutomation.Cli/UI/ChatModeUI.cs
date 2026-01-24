// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using NotebookAutomation.Cli.Services.Copilot;

namespace NotebookAutomation.Cli.UI;

/// <summary>
/// Handles the interactive chat mode UI.
/// </summary>
public class ChatModeUI
{
    private readonly ILogger<ChatModeUI> logger;
    private readonly ICopilotService copilotService;
    private readonly WelcomeBanner welcomeBanner;
    private readonly ChatBuiltInCommands builtInCommands;
    private readonly bool highContrast;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatModeUI"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="copilotService">Copilot service.</param>
    /// <param name="welcomeBanner">Welcome banner.</param>
    /// <param name="builtInCommands">Built-in commands handler.</param>
    /// <param name="highContrast">Whether to use high contrast mode.</param>
    public ChatModeUI(
        ILogger<ChatModeUI> logger,
        ICopilotService copilotService,
        WelcomeBanner welcomeBanner,
        ChatBuiltInCommands builtInCommands,
        bool highContrast = false)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.copilotService = copilotService ?? throw new ArgumentNullException(nameof(copilotService));
        this.welcomeBanner = welcomeBanner ?? throw new ArgumentNullException(nameof(welcomeBanner));
        this.builtInCommands = builtInCommands ?? throw new ArgumentNullException(nameof(builtInCommands));
        this.highContrast = highContrast;
    }

    /// <summary>
    /// Start the interactive chat loop.
    /// </summary>
    /// <param name="options">Chat mode options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code.</returns>
    public async Task<int> RunAsync(
        ChatModeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ChatModeOptions();

        try
        {
            // Display welcome banner
            if (options.ShowBanner)
            {
                if (highContrast)
                {
                    welcomeBanner.DisplayHighContrast();
                }
                else
                {
                    welcomeBanner.Display();
                }
            }

            // Check availability
            var availability = await copilotService.CheckAvailabilityAsync(cancellationToken);
            if (!availability.IsAvailable)
            {
                DisplayAvailabilityError(availability);
                return 1;
            }

            // Start Copilot service
            await copilotService.StartAsync(cancellationToken: cancellationToken);

            // Check if service actually started (SDK may fail due to protocol mismatch)
            if (!copilotService.IsRunning)
            {
                // Fall back to launching copilot CLI directly
                return await LaunchCopilotCliFallbackAsync(cancellationToken);
            }

            // Create or resume session
            ICopilotSession? session = null;
            try
            {
                var sessionModel = options.Model ?? "gpt-4o";

                if (!string.IsNullOrEmpty(options.SessionId))
                {
                    // Resume specific session
                    try
                    {
                        session = await copilotService.ResumeSessionAsync(
                            options.SessionId,
                            new CopilotSessionConfig { Model = sessionModel, Streaming = true },
                            cancellationToken);
                        AnsiConsole.MarkupLine($"[dim]Resumed session:[/] [cyan]{options.SessionId[..Math.Min(8, options.SessionId.Length)]}...[/]");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to resume session {SessionId}", options.SessionId);
                        AnsiConsole.MarkupLine($"[yellow]Could not resume session. Starting new session.[/]");
                        session = await copilotService.CreateSessionAsync(
                            new CopilotSessionConfig { Model = sessionModel, Streaming = true },
                            cancellationToken);
                    }
                }
                else if (options.Resume)
                {
                    // Try to resume most recent session
                    var sessions = await copilotService.ListSessionsAsync(cancellationToken);
                    var recentSession = sessions.OrderByDescending(s => s.LastAccessedAt).FirstOrDefault();
                    if (recentSession != null)
                    {
                        try
                        {
                            session = await copilotService.ResumeSessionAsync(
                                recentSession.SessionId,
                                new CopilotSessionConfig { Model = sessionModel, Streaming = true },
                                cancellationToken);
                            AnsiConsole.MarkupLine($"[dim]Resumed most recent session[/]");
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to resume recent session");
                            session = await copilotService.CreateSessionAsync(
                                new CopilotSessionConfig { Model = sessionModel, Streaming = true },
                                cancellationToken);
                        }
                    }
                    else
                    {
                        session = await copilotService.CreateSessionAsync(
                            new CopilotSessionConfig { Model = sessionModel, Streaming = true },
                            cancellationToken);
                    }
                }
                else
                {
                    session = await copilotService.CreateSessionAsync(
                        new CopilotSessionConfig { Model = sessionModel, Streaming = true },
                        cancellationToken);
                }

                // Set current model in built-in commands
                builtInCommands.CurrentModel = session.Model ?? sessionModel;

                // Run chat loop
                await RunChatLoopAsync(session, cancellationToken);
            }
            finally
            {
                if (session != null)
                {
                    await session.DisposeAsync();
                }
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Chat mode cancelled by user");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in chat mode");
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
            return 1;
        }
        finally
        {
            await copilotService.StopAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Run the main chat loop.
    /// </summary>
    private async Task RunChatLoopAsync(
        ICopilotSession session,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Get user input
                var input = ReadUserInput();

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                // Handle built-in commands
                if (builtInCommands.IsBuiltInCommand(input))
                {
                    var shouldExit = await builtInCommands.ExecuteAsync(
                        input,
                        session,
                        cancellationToken);

                    if (shouldExit)
                    {
                        break;
                    }

                    continue;
                }

                // Send to Copilot and display streaming response
                await SendAndDisplayResponseAsync(session, input, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message");
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
            }
        }
    }

    /// <summary>
    /// Read user input from the console.
    /// </summary>
    private string ReadUserInput()
    {
        if (highContrast)
        {
            Console.Write("You > ");
            return Console.ReadLine() ?? string.Empty;
        }
        else
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan]You[/] > ")
                    .AllowEmpty());
        }
    }

    /// <summary>
    /// Send message and display streaming response.
    /// </summary>
    private async Task SendAndDisplayResponseAsync(
        ICopilotSession session,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            var responseBuilder = new StringBuilder();

            // Show spinner while getting response
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("green"))
                .StartAsync("Thinking...", async ctx =>
                {
                    await foreach (var chunk in session.SendMessageStreamAsync(message, cancellationToken))
                    {
                        responseBuilder.Append(chunk);
                    }
                });

            // Render the complete response with markdown formatting
            var fullResponse = responseBuilder.ToString();
            if (!string.IsNullOrWhiteSpace(fullResponse))
            {
                if (highContrast)
                {
                    Console.Write("AI > ");
                    Console.WriteLine(fullResponse);
                }
                else
                {
                    AnsiConsole.Markup("[green]AI[/] > ");
                    // Use markdown renderer for formatted output
                    MarkdownRenderer.RenderLine(fullResponse);
                }
            }

            Console.WriteLine();
        }
        catch (NotImplementedException)
        {
            // Fallback for Phase 1 stub
            AnsiConsole.MarkupLine("[yellow]Streaming responses will be fully implemented in Phase 2[/]");
            AnsiConsole.MarkupLine($"[dim]You said: {message.EscapeMarkup()}[/]");
        }
    }

    /// <summary>
    /// Display error when Copilot is not available.
    /// </summary>
    private void DisplayAvailabilityError(CopilotAvailabilityResult availability)
    {
        AnsiConsole.MarkupLine("[red]GitHub Copilot is not available[/]");
        AnsiConsole.WriteLine();

        if (!availability.IsCliInstalled)
        {
            AnsiConsole.MarkupLine("[yellow]GitHub Copilot CLI is not installed.[/]");
            AnsiConsole.WriteLine();

            // Show platform-specific quick install hint
            var instructions = CopilotInstallationGuide.GetInstallationInstructions();
            AnsiConsole.MarkupLine($"[dim]Platform: {instructions.Platform}[/]");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]Quick Install:[/]");
            switch (instructions.Platform)
            {
                case "Windows":
                    AnsiConsole.MarkupLine("  [cyan]winget install GitHub.Copilot[/]");
                    break;
                case "macOS":
                    AnsiConsole.MarkupLine("  [cyan]brew install gh[/]");
                    AnsiConsole.MarkupLine("  [cyan]gh extension install github/gh-copilot[/]");
                    break;
                case "Linux":
                    AnsiConsole.MarkupLine("  [cyan]gh extension install github/gh-copilot[/]");
                    break;
                default:
                    AnsiConsole.MarkupLine("  [cyan]npm install -g @githubnext/github-copilot-cli[/]");
                    break;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]For detailed installation instructions, run: [cyan]na copilot --install-guide[/][/]");
            AnsiConsole.MarkupLine($"[dim]Documentation: [link={instructions.DocumentationUrl}]{instructions.DocumentationUrl}[/][/]");
        }
        else if (!availability.IsAuthenticated)
        {
            AnsiConsole.MarkupLine("[yellow]GitHub is not authenticated.[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Authenticate with:");
            AnsiConsole.MarkupLine("  [cyan]gh auth login --scopes copilot[/]");
        }
        else if (availability.ErrorMessage != null)
        {
            AnsiConsole.MarkupLine($"[yellow]{availability.ErrorMessage.EscapeMarkup()}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Chat mode requires GitHub Copilot. Use [cyan]na --help[/] for available commands.[/]");
    }

    /// <summary>
    /// Display error when SDK fails to connect to Copilot CLI.
    /// </summary>
    private void DisplaySdkConnectionError()
    {
        AnsiConsole.MarkupLine("[red]Failed to connect to GitHub Copilot[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]The GitHub Copilot SDK could not connect to the Copilot CLI.[/]");
        AnsiConsole.MarkupLine("[dim]This may be due to a version mismatch between the SDK and CLI.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Try updating your Copilot CLI:");
        AnsiConsole.MarkupLine("  [cyan]npm update -g @anthropic-ai/copilot[/]");
        AnsiConsole.MarkupLine("  or");
        AnsiConsole.MarkupLine("  [cyan]gh extension upgrade gh-copilot[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Chat mode requires a compatible GitHub Copilot CLI. Use [cyan]na --help[/] for available commands.[/]");
    }

    /// <summary>
    /// Launch the Copilot CLI directly as a fallback when SDK connection fails.
    /// </summary>
    private async Task<int> LaunchCopilotCliFallbackAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[yellow]SDK connection unavailable. Launching Copilot CLI directly...[/]");
        AnsiConsole.WriteLine();

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "copilot",
                    UseShellExecute = false,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = false
                }
            };

            process.Start();

            // Wait for the process to exit
            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to launch Copilot CLI");
            DisplaySdkConnectionError();
            return 1;
        }
    }
}
