// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Cli.Services.Copilot;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Handles built-in commands for the chat mode.
/// </summary>
public class ChatBuiltInCommands
{
    private readonly ILogger<ChatBuiltInCommands> logger;
    private readonly ICopilotService copilotService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatBuiltInCommands"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="copilotService">Copilot service.</param>
    public ChatBuiltInCommands(
        ILogger<ChatBuiltInCommands> logger,
        ICopilotService copilotService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.copilotService = copilotService ?? throw new ArgumentNullException(nameof(copilotService));
    }

    /// <summary>
    /// Check if the input is a built-in command.
    /// </summary>
    /// <param name="input">User input.</param>
    /// <returns>True if it's a built-in command.</returns>
    public bool IsBuiltInCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var command = input.Trim().ToLowerInvariant();
        return command switch
        {
            "help" => true,
            "exit" => true,
            "quit" => true,
            "clear" => true,
            "history" => true,
            "model" => true,
            "session" => true,
            _ when command.StartsWith("help ") => true,
            _ when command.StartsWith("model ") => true,
            _ when command.StartsWith("session ") => true,
            _ when command.StartsWith("!") => true,
            _ => false
        };
    }

    /// <summary>
    /// Execute a built-in command.
    /// </summary>
    /// <param name="input">User input.</param>
    /// <param name="session">Current session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the chat should exit.</returns>
    public async Task<bool> ExecuteAsync(
        string input,
        ICopilotSession? session,
        CancellationToken cancellationToken = default)
    {
        var command = input.Trim().ToLowerInvariant();

        // Exit commands
        if (command is "exit" or "quit")
        {
            AnsiConsole.MarkupLine("[dim]Goodbye![/]");
            return true; // Signal to exit
        }

        // Clear screen
        if (command == "clear")
        {
            AnsiConsole.Clear();
            return false;
        }

        // Help command
        if (command == "help" || command.StartsWith("help "))
        {
            await ShowHelpAsync(command);
            return false;
        }

        // History command
        if (command == "history")
        {
            await ShowHistoryAsync(session, cancellationToken);
            return false;
        }

        // Model command
        if (command == "model" || command.StartsWith("model "))
        {
            await HandleModelCommandAsync(command, cancellationToken);
            return false;
        }

        // Session command
        if (command == "session" || command.StartsWith("session "))
        {
            await HandleSessionCommandAsync(command, cancellationToken);
            return false;
        }

        // Direct CLI execution (not implemented yet)
        if (command.StartsWith("!"))
        {
            AnsiConsole.MarkupLine("[yellow]Direct CLI execution will be implemented in Phase 5[/]");
            return false;
        }

        return false;
    }

    /// <summary>
    /// Show help information.
    /// </summary>
    private async Task ShowHelpAsync(string command)
    {
        var parts = command.Split(' ', 2);

        if (parts.Length == 1)
        {
            // General help
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Blue)
                .AddColumn("[bold]Command[/]")
                .AddColumn("[bold]Description[/]");

            table.AddRow("[cyan]help[/]", "Show this help message");
            table.AddRow("[cyan]help <topic>[/]", "Get help on a specific topic");
            table.AddRow("[cyan]exit, quit[/]", "Exit chat mode");
            table.AddRow("[cyan]clear[/]", "Clear the screen");
            table.AddRow("[cyan]history[/]", "Show conversation history");
            table.AddRow("[cyan]model[/]", "Show current model");
            table.AddRow("[cyan]model <name>[/]", "Switch to a different model");
            table.AddRow("[cyan]session[/]", "Session management commands");
            table.AddRow("[cyan]!<command>[/]", "Execute CLI command directly (Phase 5)");

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]What I can help with:[/]");
            AnsiConsole.MarkupLine("  • Manage your Obsidian vault");
            AnsiConsole.MarkupLine("  • Process PDFs and videos");
            AnsiConsole.MarkupLine("  • Manage tags and metadata");
            AnsiConsole.MarkupLine("  • Answer questions about your notes");
            AnsiConsole.WriteLine();
        }
        else
        {
            // Topic-specific help
            var topic = parts[1].Trim();
            AnsiConsole.MarkupLine($"[yellow]Topic-specific help for '{topic}' will be implemented in Phase 3[/]");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Show conversation history.
    /// </summary>
    private async Task ShowHistoryAsync(ICopilotSession? session, CancellationToken cancellationToken)
    {
        if (session == null)
        {
            AnsiConsole.MarkupLine("[yellow]No active session[/]");
            return;
        }

        try
        {
            var history = await session.GetHistoryAsync(cancellationToken);

            if (history.Count == 0)
            {
                AnsiConsole.MarkupLine("[dim]No conversation history yet[/]");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Blue)
                .AddColumn("[bold]Time[/]")
                .AddColumn("[bold]Role[/]")
                .AddColumn("[bold]Message[/]");

            foreach (var message in history)
            {
                var roleColor = message.Role == "user" ? "cyan" : "green";
                var content = message.Content.Length > 100
                    ? message.Content[..100] + "..."
                    : message.Content;

                table.AddRow(
                    message.Timestamp.ToString("HH:mm:ss"),
                    $"[{roleColor}]{message.Role}[/]",
                    content.EscapeMarkup());
            }

            AnsiConsole.Write(table);
        }
        catch (NotImplementedException)
        {
            AnsiConsole.MarkupLine("[yellow]History viewing will be fully implemented in Phase 4[/]");
        }
    }

    /// <summary>
    /// Handle model-related commands.
    /// </summary>
    private async Task HandleModelCommandAsync(string command, CancellationToken cancellationToken)
    {
        var parts = command.Split(' ', 2);

        if (parts.Length == 1)
        {
            // Show current model
            AnsiConsole.MarkupLine("[yellow]Model information will be implemented in Phase 2[/]");
        }
        else
        {
            // Switch model
            var modelName = parts[1].Trim();
            AnsiConsole.MarkupLine($"[yellow]Model switching to '{modelName}' will be implemented in Phase 2[/]");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handle session-related commands.
    /// </summary>
    private async Task HandleSessionCommandAsync(string command, CancellationToken cancellationToken)
    {
        var parts = command.Split(' ', 2);

        if (parts.Length == 1)
        {
            // Show session info
            AnsiConsole.MarkupLine("[yellow]Session information will be implemented in Phase 4[/]");
        }
        else
        {
            var subCommand = parts[1].Trim();
            AnsiConsole.MarkupLine($"[yellow]Session command '{subCommand}' will be implemented in Phase 4[/]");
        }

        await Task.CompletedTask;
    }
}
