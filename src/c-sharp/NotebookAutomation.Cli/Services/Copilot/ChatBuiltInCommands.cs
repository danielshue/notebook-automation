// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Cli.Services.Copilot;
using NotebookAutomation.Cli.UI.Browser;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.Vault;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Handles built-in commands for the chat mode.
/// </summary>
public class ChatBuiltInCommands
{
    private readonly ILogger<ChatBuiltInCommands> logger;
    private readonly ICopilotService copilotService;
    private readonly IVaultBrowserService? vaultBrowserService;
    private readonly IOneDriveService? oneDriveService;
    private readonly AppConfig? appConfig;
    private readonly ILoggerFactory loggerFactory;
    private string? currentModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatBuiltInCommands"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="copilotService">Copilot service.</param>
    /// <param name="vaultBrowserService">Vault browser service (optional).</param>
    /// <param name="oneDriveService">OneDrive service (optional).</param>
    /// <param name="appConfig">Application configuration (optional).</param>
    /// <param name="loggerFactory">Logger factory for creating loggers.</param>
    public ChatBuiltInCommands(
        ILogger<ChatBuiltInCommands> logger,
        ICopilotService copilotService,
        IVaultBrowserService? vaultBrowserService,
        IOneDriveService? oneDriveService,
        AppConfig? appConfig,
        ILoggerFactory loggerFactory)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.copilotService = copilotService ?? throw new ArgumentNullException(nameof(copilotService));
        this.vaultBrowserService = vaultBrowserService;
        this.oneDriveService = oneDriveService;
        this.appConfig = appConfig;
        this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Gets or sets the current model name (set by session creation).
    /// </summary>
    public string? CurrentModel
    {
        get => currentModel;
        set => currentModel = value;
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
            "browse" => true,
            _ when command.StartsWith("help ") => true,
            _ when command.StartsWith("model ") => true,
            _ when command.StartsWith("session ") => true,
            _ when command.StartsWith("browse ") => true,
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

        // Browse command
        if (command == "browse" || command.StartsWith("browse "))
        {
            return await HandleBrowseCommandAsync(command, session, cancellationToken);
        }

        // Direct CLI execution
        if (command.StartsWith("!"))
        {
            await ExecuteDirectCliCommandAsync(command[1..].Trim(), cancellationToken);
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
            table.AddRow("[cyan]model[/]", "Show current model and available models");
            table.AddRow("[cyan]model <name>[/]", "Switch to a different model");
            table.AddRow("[cyan]session[/]", "Show current session info");
            table.AddRow("[cyan]session list[/]", "List saved sessions");
            table.AddRow("[cyan]browse[/]", "Browse vault files interactively");
            table.AddRow("[cyan]browse vault[/]", "Browse vault files");
            table.AddRow("[cyan]!<command>[/]", "Execute CLI command directly");

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]What I can help with:[/]");
            AnsiConsole.MarkupLine("  • Manage your Obsidian vault");
            AnsiConsole.MarkupLine("  • Process PDFs and videos");
            AnsiConsole.MarkupLine("  • Manage tags and metadata");
            AnsiConsole.MarkupLine("  • Answer questions about your notes");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Type[/] [cyan]help tools[/] [dim]for available tools,[/] [cyan]help models[/] [dim]for model info[/]");
        }
        else
        {
            // Topic-specific help
            var topic = parts[1].Trim().ToLowerInvariant();
            await ShowTopicHelpAsync(topic);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Show help for a specific topic.
    /// </summary>
    private async Task ShowTopicHelpAsync(string topic)
    {
        switch (topic)
        {
            case "tools":
                AnsiConsole.MarkupLine("[bold]Available Tools[/]");
                AnsiConsole.WriteLine();
                var tools = new (string Name, string Description)[]
                {
                    ("vault_list_folders", "List folders in the vault"),
                    ("vault_search_notes", "Search notes by content or tags"),
                    ("vault_read_note", "Read contents of a note"),
                    ("vault_create_note", "Create a new note"),
                    ("vault_update_note", "Update an existing note"),
                    ("tags_list", "List all tags in the vault"),
                    ("tags_add", "Add tags to a note"),
                    ("tags_remove", "Remove tags from a note"),
                    ("video_notes_process", "Process video files and create notes"),
                    ("video_notes_reprocess", "Reprocess existing video notes"),
                    ("pdf_notes_process", "Process PDF files and create notes"),
                    ("config_show", "Show current configuration"),
                    ("index_build", "Build vault index"),
                };
                var table = new Table()
                    .Border(TableBorder.Simple)
                    .AddColumn("[bold]Tool[/]")
                    .AddColumn("[bold]Description[/]");
                foreach (var (name, description) in tools)
                {
                    table.AddRow($"[cyan]{name}[/]", description);
                }
                AnsiConsole.Write(table);
                break;

            case "models":
                var models = await copilotService.GetAvailableModelsAsync();
                AnsiConsole.MarkupLine("[bold]Available Models[/]");
                AnsiConsole.WriteLine();
                foreach (var model in models)
                {
                    var isCurrent = string.Equals(model, currentModel, StringComparison.OrdinalIgnoreCase);
                    if (isCurrent)
                    {
                        AnsiConsole.MarkupLine($"  [green]●[/] [cyan]{model}[/] [dim](current)[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"  [dim]○[/] {model}");
                    }
                }
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Use[/] [cyan]model <name>[/] [dim]to switch models[/]");
                break;

            case "session":
            case "sessions":
                AnsiConsole.MarkupLine("[bold]Session Commands[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("  [cyan]session[/]           Show current session info");
                AnsiConsole.MarkupLine("  [cyan]session list[/]      List all saved sessions");
                AnsiConsole.MarkupLine("  [cyan]session new[/]       Start a new session");
                AnsiConsole.MarkupLine("  [cyan]session save[/]      Save current session");
                AnsiConsole.MarkupLine("  [cyan]session delete <id>[/]  Delete a session");
                break;

            case "shortcuts":
            case "keys":
                AnsiConsole.MarkupLine("[bold]Keyboard Shortcuts[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("  [cyan]Ctrl+C[/]    Cancel current operation / Exit");
                AnsiConsole.MarkupLine("  [cyan]Ctrl+L[/]    Clear screen (same as [cyan]clear[/])");
                AnsiConsole.MarkupLine("  [cyan]Up/Down[/]   Navigate history");
                AnsiConsole.MarkupLine("  [cyan]Tab[/]       Auto-complete commands");
                break;

            case "browse":
                AnsiConsole.MarkupLine("[bold]Browse Command[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("Interactive file browser for navigating your vault.");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Usage:[/]");
                AnsiConsole.MarkupLine("  [cyan]browse[/]              Open browser at vault root");
                AnsiConsole.MarkupLine("  [cyan]browse vault[/]        Browse vault files");
                AnsiConsole.MarkupLine("  [cyan]browse vault <path>[/] Browse at specific path");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Keyboard Controls:[/]");
                AnsiConsole.MarkupLine("  [cyan]↑/↓[/]      Navigate up/down");
                AnsiConsole.MarkupLine("  [cyan]Enter/→[/]  Open folder / Select file");
                AnsiConsole.MarkupLine("  [cyan]←/Backspace[/] Go to parent directory");
                AnsiConsole.MarkupLine("  [cyan]r[/]        Read/Preview file");
                AnsiConsole.MarkupLine("  [cyan]e[/]        Edit file");
                AnsiConsole.MarkupLine("  [cyan]d[/]        Delete file");
                AnsiConsole.MarkupLine("  [cyan]t[/]        Manage tags");
                AnsiConsole.MarkupLine("  [cyan]n[/]        Create new file");
                AnsiConsole.MarkupLine("  [cyan]s[/]        Send to Copilot");
                AnsiConsole.MarkupLine("  [cyan]q/Esc[/]    Exit browser");
                break;

            default:
                AnsiConsole.MarkupLine($"[yellow]Unknown help topic:[/] {topic}");
                AnsiConsole.MarkupLine("[dim]Available topics:[/] tools, models, session, shortcuts, browse");
                break;
        }
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
            // Show current model and available models
            var availableModels = await copilotService.GetAvailableModelsAsync(cancellationToken);

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Blue)
                .AddColumn("[bold]Model[/]")
                .AddColumn("[bold]Status[/]");

            foreach (var model in availableModels)
            {
                var isCurrent = string.Equals(model, currentModel, StringComparison.OrdinalIgnoreCase);
                var status = isCurrent ? "[green]● Active[/]" : "[dim]Available[/]";
                var modelName = isCurrent ? $"[cyan]{model}[/]" : model;
                table.AddRow(modelName, status);
            }

            AnsiConsole.Write(table);

            if (!string.IsNullOrEmpty(currentModel))
            {
                AnsiConsole.MarkupLine($"\n[dim]Current model:[/] [cyan]{currentModel}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("\n[dim]No model selected (using default gpt-4o)[/]");
            }

            AnsiConsole.MarkupLine("[dim]Use[/] [cyan]model <name>[/] [dim]to switch models[/]");
        }
        else
        {
            // Switch model
            var modelName = parts[1].Trim();
            var availableModels = await copilotService.GetAvailableModelsAsync(cancellationToken);

            if (availableModels.Any(m => string.Equals(m, modelName, StringComparison.OrdinalIgnoreCase)))
            {
                currentModel = modelName;
                AnsiConsole.MarkupLine($"[green]✓[/] Switched to model: [cyan]{modelName}[/]");
                AnsiConsole.MarkupLine("[dim]Note: Model change takes effect on next message[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗ Unknown model:[/] {modelName}");
                AnsiConsole.MarkupLine($"[dim]Available models:[/] {string.Join(", ", availableModels)}");
            }
        }
    }

    /// <summary>
    /// Handle session-related commands.
    /// </summary>
    private async Task HandleSessionCommandAsync(string command, CancellationToken cancellationToken)
    {
        var parts = command.Split(' ', 2);

        if (parts.Length == 1)
        {
            // Show session info and list sessions
            await ShowSessionInfoAsync(cancellationToken);
        }
        else
        {
            var subCommand = parts[1].Trim().ToLowerInvariant();

            switch (subCommand)
            {
                case "list":
                    await ListSessionsAsync(cancellationToken);
                    break;
                case "new":
                    AnsiConsole.MarkupLine("[dim]A new session will be created automatically.[/]");
                    AnsiConsole.MarkupLine("[dim]Type[/] [cyan]clear[/] [dim]to start a fresh conversation.[/]");
                    break;
                case "save":
                    AnsiConsole.MarkupLine("[green]✓[/] Session auto-saved");
                    break;
                default:
                    if (subCommand.StartsWith("delete "))
                    {
                        var sessionId = subCommand[7..].Trim();
                        await DeleteSessionAsync(sessionId, cancellationToken);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]Unknown session command:[/] {subCommand}");
                        AnsiConsole.MarkupLine("[dim]Available:[/] session, session list, session new, session save, session delete <id>");
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Show information about the current session.
    /// </summary>
    private async Task ShowSessionInfoAsync(CancellationToken cancellationToken)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .AddColumn("[bold]Property[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Status", "[green]● Active[/]");
        table.AddRow("Model", $"[cyan]{currentModel ?? "gpt-4o (default)"}[/]");
        table.AddRow("Started", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        AnsiConsole.Write(table);

        // Show recent sessions count
        try
        {
            var sessions = await copilotService.ListSessionsAsync(cancellationToken);
            AnsiConsole.MarkupLine($"\n[dim]Saved sessions:[/] {sessions.Count}");
            AnsiConsole.MarkupLine("[dim]Use[/] [cyan]session list[/] [dim]to see all sessions[/]");
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to get session count");
        }
    }

    /// <summary>
    /// List all saved sessions.
    /// </summary>
    private async Task ListSessionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var sessions = await copilotService.ListSessionsAsync(cancellationToken);

            if (sessions.Count == 0)
            {
                AnsiConsole.MarkupLine("[dim]No saved sessions found[/]");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Blue)
                .AddColumn("[bold]Session ID[/]")
                .AddColumn("[bold]Created[/]")
                .AddColumn("[bold]Messages[/]")
                .AddColumn("[bold]Model[/]");

            foreach (var session in sessions.OrderByDescending(s => s.LastAccessedAt).Take(10))
            {
                table.AddRow(
                    $"[cyan]{session.SessionId[..Math.Min(8, session.SessionId.Length)]}...[/]",
                    session.CreatedAt.ToString("MM-dd HH:mm"),
                    session.MessageCount.ToString(),
                    session.Model ?? "[dim]default[/]");
            }

            AnsiConsole.Write(table);

            if (sessions.Count > 10)
            {
                AnsiConsole.MarkupLine($"[dim]Showing 10 of {sessions.Count} sessions[/]");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to list sessions");
            AnsiConsole.MarkupLine("[red]Failed to list sessions[/]");
        }
    }

    /// <summary>
    /// Delete a session by ID.
    /// </summary>
    private async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            await copilotService.DeleteSessionAsync(sessionId, cancellationToken);
            AnsiConsole.MarkupLine($"[green]✓[/] Deleted session: {sessionId}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete session {SessionId}", sessionId);
            AnsiConsole.MarkupLine($"[red]Failed to delete session:[/] {ex.Message}");
        }
    }

    /// <summary>
    /// Execute a direct CLI command (commands prefixed with !).
    /// </summary>
    private async Task ExecuteDirectCliCommandAsync(string command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            AnsiConsole.MarkupLine("[dim]Usage: !<command> - Execute a shell command[/]");
            AnsiConsole.MarkupLine("[dim]Example: !dir or !ls[/]");
            return;
        }

        try
        {
            AnsiConsole.MarkupLine($"[dim]Executing: {command.EscapeMarkup()}[/]");
            AnsiConsole.WriteLine();

            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
                    Arguments = OperatingSystem.IsWindows() ? $"/c {command}" : $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (!string.IsNullOrWhiteSpace(output))
            {
                AnsiConsole.WriteLine(output.TrimEnd());
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                AnsiConsole.MarkupLine($"[red]{error.TrimEnd().EscapeMarkup()}[/]");
            }

            if (process.ExitCode != 0)
            {
                AnsiConsole.MarkupLine($"[dim]Exit code: {process.ExitCode}[/]");
            }

            AnsiConsole.WriteLine();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to execute command: {Command}", command);
            AnsiConsole.MarkupLine($"[red]Failed to execute command:[/] {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the browse command for interactive file browsing.
    /// </summary>
    /// <param name="command">The browse command with optional arguments.</param>
    /// <param name="session">The current Copilot session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the chat should exit, false otherwise.</returns>
    private async Task<bool> HandleBrowseCommandAsync(
        string command,
        ICopilotSession? session,
        CancellationToken cancellationToken)
    {
        // Parse command arguments
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var source = parts.Length > 1 ? parts[1].ToLowerInvariant() : "vault";
        var initialPath = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : null;

        // Available sources for switching
        var sources = new[] { "vault", "onedrive" };
        var currentSourceIndex = Array.IndexOf(sources, source);
        if (currentSourceIndex < 0)
        {
            currentSourceIndex = 0;
            source = "vault";
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            // Check source availability and create appropriate browser source
            IFileBrowserSource? browserSource = null;

            if (source == "onedrive")
            {
                if (oneDriveService == null)
                {
                    AnsiConsole.MarkupLine("[yellow]OneDrive is not configured.[/]");
                    AnsiConsole.MarkupLine("[dim]Press any key to switch to Vault browser, or 'q' to exit.[/]");
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        return false;
                    }

                    source = "vault";
                    currentSourceIndex = 0;
                    continue;
                }

                try
                {
                    var oneDriveSource = new OneDriveBrowserSource(
                        oneDriveService,
                        loggerFactory.CreateLogger<OneDriveBrowserSource>());

                    // Authenticate before browsing
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .Start("[dim]Authenticating with OneDrive...[/]", ctx =>
                        {
                            oneDriveSource.EnsureAuthenticatedAsync().GetAwaiter().GetResult();
                        });

                    browserSource = oneDriveSource;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to authenticate with OneDrive");
                    AnsiConsole.MarkupLine($"[red]OneDrive authentication failed:[/] {ex.Message.EscapeMarkup()}");
                    AnsiConsole.MarkupLine("[dim]Press any key to switch to Vault browser, or 'q' to exit.[/]");
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        return false;
                    }

                    source = "vault";
                    currentSourceIndex = 0;
                    continue;
                }
            }
            else if (source == "vault")
            {
                if (vaultBrowserService == null)
                {
                    AnsiConsole.MarkupLine("[yellow]Vault is not configured.[/]");
                    if (oneDriveService != null)
                    {
                        AnsiConsole.MarkupLine("[dim]Press any key to switch to OneDrive browser, or 'q' to exit.[/]");
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Q)
                        {
                            return false;
                        }

                        source = "onedrive";
                        currentSourceIndex = 1;
                        continue;
                    }

                    AnsiConsole.MarkupLine("[dim]Please configure your vault path in the settings to use the browser.[/]");
                    return false;
                }

                browserSource = new VaultBrowserSource(
                    vaultBrowserService,
                    loggerFactory.CreateLogger<VaultBrowserSource>());
            }

            if (browserSource == null)
            {
                AnsiConsole.MarkupLine("[red]No browser source available.[/]");
                return false;
            }

            try
            {
                // Determine initial path based on source and configuration
                // For both sources, use provided path or default to root
                var effectiveInitialPath = initialPath ?? string.Empty;

                var browserOptions = new FileBrowserOptions
                {
                    InitialPath = effectiveInitialPath,
                    EnableFileOperations = true
                };

                var browserUI = new FileBrowserUI(
                    browserSource,
                    loggerFactory.CreateLogger<FileBrowserUI>(),
                    browserOptions);

                // Run the browser
                var result = await browserUI.RunAsync(cancellationToken);

                // Handle the result
                switch (result.LastAction)
                {
                    case BrowseAction.SwitchSource:
                        // Toggle to the next source
                        currentSourceIndex = (currentSourceIndex + 1) % sources.Length;
                        source = sources[currentSourceIndex];
                        initialPath = null; // Reset path when switching
                        AnsiConsole.Clear();
                        continue; // Loop back to try the new source

                    case BrowseAction.SendToCopilot when session != null && result.SelectedPath != null:
                        // Read the file and send to Copilot
                        var fileResult = await browserSource.ReadFileAsync(result.SelectedPath, cancellationToken);
                        if (fileResult.IsSuccess && fileResult.Value != null)
                        {
                            AnsiConsole.Clear();
                            AnsiConsole.MarkupLine($"[dim]Sending[/] [cyan]{fileResult.Value.Info.Name}[/] [dim]to Copilot...[/]");

                            // Format content for Copilot
                            var fileContent = $"Please analyze this note:\n\n**File:** {fileResult.Value.Info.Name}\n**Path:** {fileResult.Value.Info.Path}\n\n```markdown\n{fileResult.Value.Content}\n```";

                            // Send to Copilot (this would stream the response)
                            var responseBuilder = new StringBuilder();
                            await AnsiConsole.Status()
                                .Spinner(Spinner.Known.Dots)
                                .SpinnerStyle(Style.Parse("green"))
                                .StartAsync("Analyzing...", async ctx =>
                                {
                                    await foreach (var chunk in session.SendMessageStreamAsync(fileContent, cancellationToken))
                                    {
                                        responseBuilder.Append(chunk);
                                    }
                                });

                            // Display response
                            var fullResponse = responseBuilder.ToString();
                            if (!string.IsNullOrWhiteSpace(fullResponse))
                            {
                                AnsiConsole.Markup("[green]AI[/] > ");
                                MarkdownRenderer.RenderLine(fullResponse);
                            }

                            AnsiConsole.WriteLine();
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]Error reading file:[/] {fileResult.Error}");
                        }

                        return false;

                    case BrowseAction.Selected when result.SelectedPath != null:
                        // Browser already cleared screen, just show result
                        AnsiConsole.MarkupLine($"[green]Selected:[/] {result.SelectedPath}");
                        return false;

                    case BrowseAction.Cancelled:
                        // Browser already cleared screen on exit, just return
                        return false;

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in browse command");
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
                return false;
            }
        }

        return false;
    }
}
