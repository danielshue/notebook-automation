// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Cli.Services.Copilot;

using Spectre.Console;

namespace NotebookAutomation.Cli.UI;

/// <summary>
/// Handles the first-run experience for new users.
/// </summary>
public class FirstRunExperience(
    ILogger<FirstRunExperience> logger,
    IUserPreferencesService userPreferencesService,
    IGitService gitService)
{
    private readonly ILogger<FirstRunExperience> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IUserPreferencesService userPreferencesService = userPreferencesService ?? throw new ArgumentNullException(nameof(userPreferencesService));
    private readonly IGitService gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));

    /// <summary>
    /// Check if first-run experience should be shown.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if first-run should be shown.</returns>
    public async Task<bool> ShouldShowFirstRunAsync(CancellationToken cancellationToken = default)
    {
        return await userPreferencesService.IsFirstRunAsync(cancellationToken);
    }

    /// <summary>
    /// Run the first-run experience.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the operation.</returns>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting first-run experience");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[blue]Welcome to Notebook Automation AI Chat[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[dim]This appears to be your first time using the AI chat feature.[/]");
        AnsiConsole.MarkupLine("[dim]Let's set up a few preferences to personalize your experience.[/]");
        AnsiConsole.WriteLine();

        // Check for Git repository
        await CheckGitRepositoryAsync(cancellationToken);

        // Prompt for session retention
        var retentionDays = await PromptSessionRetentionAsync(cancellationToken);

        // Prompt for high contrast preference
        var highContrast = await PromptHighContrastAsync(cancellationToken);

        // Prompt for welcome banner preference
        var showWelcomeBanner = await PromptWelcomeBannerAsync(cancellationToken);

        // Save preferences
        var preferences = new UserPreferences
        {
            SessionRetentionDays = retentionDays,
            HighContrastMode = highContrast,
            ShowWelcomeBanner = showWelcomeBanner,
            AutoSaveSessions = true
        };

        await userPreferencesService.SavePreferencesAsync(preferences, cancellationToken);
        await userPreferencesService.MarkFirstRunCompleteAsync(cancellationToken);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✓ Preferences saved![/]");
        AnsiConsole.MarkupLine("[dim]You can change these settings anytime using /settings command.[/]");
        AnsiConsole.WriteLine();

        logger.LogInformation("First-run experience completed");
    }

    /// <summary>
    /// Check if the current directory is a Git repository.
    /// </summary>
    private async Task CheckGitRepositoryAsync(CancellationToken cancellationToken)
    {
        var currentDir = Directory.GetCurrentDirectory();
        var isGitRepo = await gitService.IsGitRepositoryAsync(currentDir);

        if (isGitRepo)
        {
            var gitRoot = await gitService.GetGitRootAsync(currentDir);
            var currentBranch = await gitService.GetCurrentBranchAsync(currentDir);

            AnsiConsole.MarkupLine($"[green]✓[/] Git repository detected: [blue]{gitRoot}[/]");

            if (!string.IsNullOrEmpty(currentBranch))
            {
                AnsiConsole.MarkupLine($"  [dim]Branch: {currentBranch}[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]![/] No Git repository detected in current directory.");
            AnsiConsole.MarkupLine("[dim]  AI responses will have limited context about your project structure.[/]");
            AnsiConsole.MarkupLine("[dim]  Consider running 'git init' to enable better project context.[/]");
        }

        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Prompt user for session retention preference.
    /// </summary>
    private Task<int> PromptSessionRetentionAsync(CancellationToken cancellationToken)
    {
        var retention = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How long should chat sessions be retained?")
                .AddChoices(
                    "7 days",
                    "14 days",
                    "30 days (recommended)",
                    "90 days",
                    "Never delete"));

        var days = retention switch
        {
            "7 days" => 7,
            "14 days" => 14,
            "30 days (recommended)" => 30,
            "90 days" => 90,
            "Never delete" => -1,
            _ => 30
        };

        logger.LogDebug("User selected session retention: {Days} days", days);
        return Task.FromResult(days);
    }

    /// <summary>
    /// Prompt user for high contrast preference.
    /// </summary>
    private Task<bool> PromptHighContrastAsync(CancellationToken cancellationToken)
    {
        var highContrast = AnsiConsole.Confirm(
            "Enable high contrast mode for better accessibility?",
            defaultValue: false);

        logger.LogDebug("User selected high contrast: {HighContrast}", highContrast);
        return Task.FromResult(highContrast);
    }

    /// <summary>
    /// Prompt user for welcome banner preference.
    /// </summary>
    private Task<bool> PromptWelcomeBannerAsync(CancellationToken cancellationToken)
    {
        var showBanner = AnsiConsole.Confirm(
            "Show welcome banner when starting chat?",
            defaultValue: true);

        logger.LogDebug("User selected show welcome banner: {ShowBanner}", showBanner);
        return Task.FromResult(showBanner);
    }
}
