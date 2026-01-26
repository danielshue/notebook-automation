// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Cli.Models.Browse;
using NotebookAutomation.Core.Tools.Vault;

namespace NotebookAutomation.Cli.UI.Browse;

/// <summary>
/// Interactive file browser UI using Spectre.Console.
/// </summary>
public class FileBrowserUI(IVaultBrowserService vaultBrowserService, ILogger<FileBrowserUI> logger)
{
    private readonly IVaultBrowserService _vaultBrowserService = vaultBrowserService ?? throw new ArgumentNullException(nameof(vaultBrowserService));
    private readonly ILogger<FileBrowserUI> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private string _currentPath = string.Empty;

    /// <summary>
    /// Runs the file browser UI.
    /// </summary>
    /// <param name="initialPath">The initial path to browse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The browse session result.</returns>
    public async Task<BrowseSession> RunAsync(
        string? initialPath = null,
        CancellationToken cancellationToken = default)
    {
        _currentPath = initialPath ?? string.Empty;
        var exitRequested = false;
        string? selectedPath = null;
        var lastAction = BrowseAction.None;

        while (!exitRequested && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                // List current directory using VaultBrowserService
                var vaultResult = _vaultBrowserService.ListDirectory(_currentPath);

                if (!vaultResult.IsSuccess)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {vaultResult.Error?.EscapeMarkup()}");
                    break;
                }

                var vaultListing = vaultResult.Value!;

                // Display the current path
                DisplayHeader(vaultListing.Path);

                // Check if directory is empty
                var totalItems = vaultListing.Directories.Count + vaultListing.Files.Count;
                var hasParent = !string.IsNullOrEmpty(_currentPath) && _currentPath != "/";

                if (totalItems == 0)
                {
                    AnsiConsole.MarkupLine("[dim]This directory is empty.[/]");
                    
                    if (!hasParent)
                    {
                        AnsiConsole.MarkupLine("[yellow]Press any key to exit...[/]");
                        Console.ReadKey(true);
                        exitRequested = true;
                        lastAction = BrowseAction.Cancelled;
                        continue;
                    }

                    var goBack = AnsiConsole.Confirm("Go back to parent directory?", true);
                    if (goBack)
                    {
                        _currentPath = GetParentPath(_currentPath);
                    }
                    else
                    {
                        exitRequested = true;
                        lastAction = BrowseAction.Cancelled;
                    }
                    continue;
                }

                // Build choices list
                var choices = new List<string>();
                var itemMap = new Dictionary<string, (bool IsDirectory, string Path)>();
                
                if (hasParent)
                {
                    choices.Add("[dim].. (Parent Directory)[/]");
                }

                // Add directories
                foreach (var dir in vaultListing.Directories)
                {
                    var displayName = $"📁 [cyan]{dir.Name}[/]";
                    choices.Add(displayName);
                    itemMap[displayName] = (true, dir.RelativePath);
                }

                // Add files
                foreach (var file in vaultListing.Files)
                {
                    var displayName = $"📄 {file.Name.EscapeMarkup()} [dim]{file.SizeFormatted}[/]";
                    choices.Add(displayName);
                    itemMap[displayName] = (false, file.RelativePath);
                }

                choices.Add("[dim]────────────────────────[/]");
                choices.Add("[yellow]⬅ Go Back[/]");
                choices.Add("[red]✖ Exit Browser[/]");

                // Show selection prompt
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"[bold]Select a file or directory:[/]")
                        .PageSize(15)
                        .MoreChoicesText("[dim](Move up and down to see more items)[/]")
                        .AddChoices(choices));

                // Handle selection
                if (selection == "[red]✖ Exit Browser[/]")
                {
                    exitRequested = true;
                    lastAction = BrowseAction.Cancelled;
                }
                else if (selection == "[yellow]⬅ Go Back[/]")
                {
                    if (hasParent)
                    {
                        _currentPath = GetParentPath(_currentPath);
                    }
                    else
                    {
                        exitRequested = true;
                        lastAction = BrowseAction.Cancelled;
                    }
                }
                else if (selection == "[dim].. (Parent Directory)[/]")
                {
                    _currentPath = GetParentPath(_currentPath);
                }
                else if (itemMap.TryGetValue(selection, out var item))
                {
                    if (item.IsDirectory)
                    {
                        // Navigate into directory
                        _currentPath = item.Path;
                    }
                    else
                    {
                        // File selected - show actions menu
                        var action = await ShowFileActionsMenuAsync(item.Path, cancellationToken);
                        
                        if (action == "view")
                        {
                            await ShowFilePreviewAsync(item.Path, cancellationToken);
                        }
                        else if (action == "select")
                        {
                            selectedPath = item.Path;
                            lastAction = BrowseAction.Selected;
                            exitRequested = true;
                        }
                        else if (action == "delete")
                        {
                            var fileName = System.IO.Path.GetFileName(item.Path);
                            var confirmed = AnsiConsole.Confirm(
                                $"Are you sure you want to delete [red]{fileName.EscapeMarkup()}[/]?",
                                defaultValue: false);
                            
                            if (confirmed)
                            {
                                var deleteResult = _vaultBrowserService.DeleteNote(item.Path);
                                if (deleteResult.IsSuccess)
                                {
                                    AnsiConsole.MarkupLine("[green]✓[/] File deleted successfully");
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[red]Error:[/] {deleteResult.Error?.EscapeMarkup()}");
                                }
                                
                                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                                Console.ReadKey(true);
                            }
                        }
                        // For "back" action, just continue the loop
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in file browser");
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
                exitRequested = true;
                lastAction = BrowseAction.Cancelled;
            }
        }

        return new BrowseSession(selectedPath, lastAction);
    }

    private void DisplayHeader(string currentPath)
    {
        var panel = new Panel(
            Align.Left(
                new Markup($"[bold]📁 Vault File Browser[/]\n[dim]Path:[/] [cyan]{currentPath.EscapeMarkup()}[/]")))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Padding = new Padding(1, 0)
        };

        AnsiConsole.Clear();
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private async Task<string> ShowFileActionsMenuAsync(string filePath, CancellationToken cancellationToken)
    {
        var fileName = System.IO.Path.GetFileName(filePath);
        
        var choices = new List<string>
        {
            "📖 View/Preview",
            "✅ Select File",
            "🗑 Delete File",
            "────────────",
            "⬅ Back to List"
        };

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]Actions for:[/] {fileName.EscapeMarkup()}")
                .AddChoices(choices));

        await Task.CompletedTask;
        
        return action switch
        {
            "📖 View/Preview" => "view",
            "✅ Select File" => "select",
            "🗑 Delete File" => "delete",
            _ => "back"
        };
    }

    private async Task ShowFilePreviewAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var readResult = _vaultBrowserService.ReadNote(path);

            if (!readResult.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error reading file:[/] {readResult.Error?.EscapeMarkup()}");
                return;
            }

            var noteContent = readResult.Value!;
            
            // Create preview panel
            var previewContent = noteContent.Content;
            
            // Limit preview to first 50 lines for display
            var lines = previewContent.Split('\n');
            var displayLines = lines.Take(50).ToArray();
            var displayContent = string.Join('\n', displayLines);
            
            if (lines.Length > 50)
            {
                displayContent += "\n[dim]... (content truncated)[/]";
            }

            var panel = new Panel(displayContent.EscapeMarkup())
            {
                Header = new PanelHeader($"📄 {noteContent.Info.Name.EscapeMarkup()}"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Blue),
                Padding = new Padding(2, 1)
            };

            AnsiConsole.Clear();
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
            
            // Show file info
            var info = new Table()
                .Border(TableBorder.None)
                .AddColumn("[dim]Property[/]")
                .AddColumn("[dim]Value[/]")
                .AddRow("Size", noteContent.Info.SizeFormatted)
                .AddRow("Modified", noteContent.Info.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
            
            AnsiConsole.Write(info);
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[dim]Press any key to go back...[/]");
            Console.ReadKey(true);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing file preview for {Path}", path);
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey(true);
        }
    }

    private string GetParentPath(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
        {
            return string.Empty;
        }

        var normalized = path.TrimEnd('/').TrimEnd('\\');
        var lastSlash = Math.Max(normalized.LastIndexOf('/'), normalized.LastIndexOf('\\'));
        
        if (lastSlash <= 0)
        {
            return string.Empty;
        }

        return normalized[..lastSlash];
    }
}
