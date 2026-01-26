// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.UI;

/// <summary>
/// Displays welcome banner and introduction for Copilot chat mode.
/// </summary>
public class WelcomeBanner
{
    private readonly CopilotConfig copilotConfig;
    private readonly AppConfig appConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="WelcomeBanner"/> class.
    /// </summary>
    /// <param name="appConfig">Application configuration.</param>
    public WelcomeBanner(AppConfig appConfig)
    {
        this.appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        this.copilotConfig = appConfig.Copilot;
    }

    /// <summary>
    /// Display the welcome banner for chat mode.
    /// </summary>
    /// <param name="showBanner">Whether to show the banner.</param>
    public void Display(bool showBanner = true)
    {
        if (!showBanner || !copilotConfig.ShowWelcomeBanner)
        {
            return;
        }

        AnsiConsole.Clear();

        // ASCII Art Banner
        var banner = new FigletText("Notebook AI")
            .Centered()
            .Color(Color.Blue);

        AnsiConsole.Write(banner);
        AnsiConsole.WriteLine();

        // Get configured paths
        var vaultPath = appConfig.Paths?.GetEffectiveVaultRoot() ?? "[dim]Not configured[/]";
        var oneDrivePath = appConfig.Paths?.GetEffectiveOneDriveRoot() ?? "[dim]Not configured[/]";
        var vaultDisplay = string.IsNullOrEmpty(vaultPath) ? "[dim]Not configured[/]" : $"[green]{Markup.Escape(vaultPath)}[/]";
        var oneDriveDisplay = string.IsNullOrEmpty(oneDrivePath) ? "[dim]Not configured[/]" : $"[green]{Markup.Escape(oneDrivePath)}[/]";

        // Welcome message
        var panel = new Panel(
            new Markup(
                "[bold]Welcome to Notebook Automation AI Assistant![/]\n\n" +
                "I can help you manage your vault, process documents, and answer questions.\n" +
                "Type [cyan]help[/] to see what I can do, or just start chatting!\n\n" +
                $"[bold]Vault:[/]     {vaultDisplay}\n" +
                $"[bold]OneDrive:[/]  {oneDriveDisplay}\n\n" +
                "[dim]Try asking:[/]\n" +
                "  [italic]• What commands are available?[/]\n" +
                "  [italic]• Process videos in my Financial Management folder[/]\n" +
                "  [italic]• Show me notes tagged with #economics[/]\n" +
                "  [italic]• Build the index for my vault[/]\n\n" +
                "Commands: [dim]help, browse, model, history,  clear, session, exit, !cmd[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display a simplified banner for high contrast mode.
    /// </summary>
    public void DisplayHighContrast()
    {
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("NOTEBOOK AUTOMATION AI ASSISTANT");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine();
        Console.WriteLine("Welcome! Type 'help' for commands or start chatting.");
        Console.WriteLine();

        var vaultPath = appConfig.Paths?.GetEffectiveVaultRoot();
        var oneDrivePath = appConfig.Paths?.GetEffectiveOneDriveRoot();
        Console.WriteLine($"Vault:    {(string.IsNullOrEmpty(vaultPath) ? "Not configured" : vaultPath)}");
        Console.WriteLine($"OneDrive: {(string.IsNullOrEmpty(oneDrivePath) ? "Not configured" : oneDrivePath)}");
        Console.WriteLine();

        Console.WriteLine("Try asking:");
        Console.WriteLine("  - What commands are available?");
        Console.WriteLine("  - Process videos in my Financial Management folder");
        Console.WriteLine("  - Show me notes tagged with #economics");
        Console.WriteLine("  - Build the index for my vault");
        Console.WriteLine();
        Console.WriteLine("Commands: help, exit, clear, history, model, session, !cmd");
        Console.WriteLine();
    }
}
