// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.UI;

/// <summary>
/// Displays welcome banner and introduction for Copilot chat mode.
/// </summary>
public class WelcomeBanner
{
    private readonly CopilotConfig config;

    /// <summary>
    /// Initializes a new instance of the <see cref="WelcomeBanner"/> class.
    /// </summary>
    /// <param name="config">Copilot configuration.</param>
    public WelcomeBanner(CopilotConfig config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Display the welcome banner for chat mode.
    /// </summary>
    /// <param name="showBanner">Whether to show the banner.</param>
    public void Display(bool showBanner = true)
    {
        if (!showBanner || !config.ShowWelcomeBanner)
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

        // Welcome message
        var panel = new Panel(
            new Markup(
                "[bold]Welcome to Notebook Automation AI Assistant![/]\n\n" +
                "I can help you manage your vault, process documents, and answer questions.\n" +
                "Type [cyan]help[/] to see what I can do, or just start chatting!\n\n" +
                "Commands: [dim]help, exit, clear, history, model, session[/]"))
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
        Console.WriteLine("Commands: help, exit, clear, history, model, session");
        Console.WriteLine();
    }
}
