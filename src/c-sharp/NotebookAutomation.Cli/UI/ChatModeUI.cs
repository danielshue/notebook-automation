// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

            // Create or resume session
            ICopilotSession? session = null;
            try
            {
                if (options.Resume || !string.IsNullOrEmpty(options.SessionId))
                {
                    AnsiConsole.MarkupLine("[yellow]Session resumption will be implemented in Phase 4[/]");
                }

                session = await copilotService.CreateSessionAsync(
                    new CopilotSessionConfig
                    {
                        Model = options.Model,
                        Streaming = true
                    },
                    cancellationToken);

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
            if (highContrast)
            {
                Console.Write("AI > ");
            }
            else
            {
                AnsiConsole.Markup("[green]AI[/] > ");
            }

            var responseBuilder = new StringBuilder();

            await foreach (var chunk in session.SendMessageStreamAsync(message, cancellationToken))
            {
                responseBuilder.Append(chunk);
                
                if (highContrast)
                {
                    Console.Write(chunk);
                }
                else
                {
                    AnsiConsole.Markup(chunk.EscapeMarkup());
                }
            }

            Console.WriteLine();
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
            AnsiConsole.MarkupLine("[yellow]GitHub CLI with Copilot extension is not installed.[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Install with:");
            AnsiConsole.MarkupLine("  [cyan]gh extension install github/gh-copilot[/]");
        }
        else if (!availability.IsAuthenticated)
        {
            AnsiConsole.MarkupLine("[yellow]GitHub CLI is not authenticated.[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Authenticate with:");
            AnsiConsole.MarkupLine("  [cyan]gh auth login[/]");
        }
        else if (availability.ErrorMessage != null)
        {
            AnsiConsole.MarkupLine($"[yellow]{availability.ErrorMessage.EscapeMarkup()}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Falling back to regular CLI mode...[/]");
    }
}
