// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Manages user preferences for Copilot.
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly ILogger<UserPreferencesService> logger;
    private readonly string preferencesFilePath;
    private readonly string firstRunMarkerPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPreferencesService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public UserPreferencesService(ILogger<UserPreferencesService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(homeDir, ".notebookautomation");
        Directory.CreateDirectory(configDir);
        
        preferencesFilePath = Path.Combine(configDir, "preferences.json");
        firstRunMarkerPath = Path.Combine(configDir, ".firstrun");
    }

    /// <inheritdoc/>
    public async Task<UserPreferences> GetPreferencesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(preferencesFilePath))
            {
                return new UserPreferences();
            }

            var json = await File.ReadAllTextAsync(preferencesFilePath, cancellationToken);
            return JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load preferences, using defaults");
            return new UserPreferences();
        }
    }

    /// <inheritdoc/>
    public async Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(preferencesFilePath, json, cancellationToken);
            logger.LogInformation("Saved user preferences");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save preferences");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> IsFirstRunAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!File.Exists(firstRunMarkerPath));
    }

    /// <inheritdoc/>
    public async Task MarkFirstRunCompleteAsync(CancellationToken cancellationToken = default)
    {
        await File.WriteAllTextAsync(firstRunMarkerPath, DateTime.UtcNow.ToString("O"), cancellationToken);
        logger.LogInformation("Marked first run complete");
    }
}
