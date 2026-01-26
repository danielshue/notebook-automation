// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Cli.Models.Browse;
using NotebookAutomation.Cli.Services.Browse;

namespace NotebookAutomation.Cli.UI.Browse;

/// <summary>
/// Interactive file browser UI using Spectre.Console.
/// </summary>
public class FileBrowserUI(IFileBrowserSource source, ILogger<FileBrowserUI> logger)
{
    private readonly IFileBrowserSource _source = source ?? throw new ArgumentNullException(nameof(source));
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
                // List current directory
                var listResult = await _source.ListDirectoryAsync(_currentPath, cancellationToken);

                if (!listResult.IsSuccess)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {listResult.ErrorMessage?.EscapeMarkup()}");
                    break;
                }

                var listing = listResult.Data!;

                // Display the current path and source
                DisplayHeader(listing.CurrentPath);

                // If empty directory
                if (listing.Items.Count == 0)
                {
                    AnsiConsole.MarkupLine("[dim]This directory is empty.[/]");
                    
                    if (!listing.HasParent)
                    {
                        AnsiConsole.MarkupLine("[yellow]Press any key to exit...[/]");
                        Console.ReadKey(true);
                        exitRequested = true;
                        lastAction = BrowseAction.Cancelled;
                        continue;
                    }

                    var goBack = AnsiConsole.Confirm("Go back to parent directory?", true);
                    if (goBack && listing.HasParent)
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
                
                if (listing.HasParent)
                {
                    choices.Add("[dim].. (Parent Directory)[/]");
                }

                foreach (var item in listing.SortedItems)
                {
                    var displayName = item.IsDirectory
                        ? $"📁 [cyan]{item.Name}[/]"
                        : $"📄 {item.Name.EscapeMarkup()} [dim]{item.SizeFormatted}[/]";
                    choices.Add(displayName);
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
                    if (listing.HasParent)
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
                else
                {
                    // Find the selected item
                    var selectedIndex = choices.IndexOf(selection) - (listing.HasParent ? 1 : 0);
                    var selectedItem = listing.SortedItems[selectedIndex];

                    if (selectedItem.IsDirectory)
                    {
                        // Navigate into directory
                        _currentPath = selectedItem.Path;
                    }
                    else
                    {
                        // File selected - show actions menu
                        var action = await ShowFileActionsMenuAsync(selectedItem, cancellationToken);
                        
                        if (action == "view")
                        {
                            await ShowFilePreviewAsync(selectedItem.Path, cancellationToken);
                        }
                        else if (action == "select")
                        {
                            selectedPath = selectedItem.Path;
                            lastAction = BrowseAction.Selected;
                            exitRequested = true;
                        }
                        else if (action == "delete")
                        {
                            var confirmed = AnsiConsole.Confirm(
                                $"Are you sure you want to delete [red]{selectedItem.Name.EscapeMarkup()}[/]?",
                                defaultValue: false);
                            
                            if (confirmed)
                            {
                                var deleteResult = await _source.DeleteFileAsync(selectedItem.Path, cancellationToken);
                                if (deleteResult.IsSuccess)
                                {
                                    AnsiConsole.MarkupLine("[green]✓[/] File deleted successfully");
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[red]Error:[/] {deleteResult.ErrorMessage?.EscapeMarkup()}");
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
                new Markup($"[bold]📁 {_source.SourceName} File Browser[/]\n[dim]Path:[/] [cyan]{currentPath.EscapeMarkup()}[/]")))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Padding = new Padding(1, 0)
        };

        AnsiConsole.Clear();
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private async Task<string> ShowFileActionsMenuAsync(BrowseItem item, CancellationToken cancellationToken)
    {
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
                .Title($"[bold]Actions for:[/] {item.Name.EscapeMarkup()}")
                .AddChoices(choices));

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
            var readResult = await _source.ReadFileAsync(path, cancellationToken);

            if (!readResult.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error reading file:[/] {readResult.ErrorMessage?.EscapeMarkup()}");
                return;
            }

            var fileContent = readResult.Data!;
            
            // Create preview panel
            var previewContent = fileContent.Content;
            
            // Limit preview to first 50 lines for display
            var lines = previewContent.Split('\n').Take(50).ToArray();
            var displayContent = string.Join('\n', lines);
            
            if (fileContent.Content.Split('\n').Length > 50)
            {
                displayContent += "\n[dim]... (content truncated)[/]";
            }

            var panel = new Panel(displayContent.EscapeMarkup())
            {
                Header = new PanelHeader($"📄 {fileContent.Info.Name.EscapeMarkup()}"),
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
                .AddRow("Size", fileContent.Info.SizeFormatted)
                .AddRow("Modified", fileContent.Info.LastModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A");
            
            AnsiConsole.Write(info);
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[dim]Press any key to go back...[/]");
            Console.ReadKey(true);
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
