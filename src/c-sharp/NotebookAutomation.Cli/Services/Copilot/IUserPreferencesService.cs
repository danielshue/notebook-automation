// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Manages user preferences for Copilot.
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Get user preferences.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User preferences.</returns>
    Task<UserPreferences> GetPreferencesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Save user preferences.
    /// </summary>
    /// <param name="preferences">Preferences to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if this is the first run.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if first run.</returns>
    Task<bool> IsFirstRunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark first run as complete.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkFirstRunCompleteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// User preferences for Copilot.
/// </summary>
public record UserPreferences
{
    /// <summary>
    /// Session retention period in days.
    /// </summary>
    public int SessionRetentionDays { get; init; } = 30;

    /// <summary>
    /// Whether to auto-save sessions.
    /// </summary>
    public bool AutoSaveSessions { get; init; } = true;

    /// <summary>
    /// Preferred model name.
    /// </summary>
    public string? PreferredModel { get; init; }

    /// <summary>
    /// Whether high contrast mode is enabled.
    /// </summary>
    public bool HighContrastMode { get; init; }

    /// <summary>
    /// Whether to show welcome banner.
    /// </summary>
    public bool ShowWelcomeBanner { get; init; } = true;
}
