// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Manages Copilot session persistence.
/// </summary>
public class SessionManager : ISessionManager
{
    private readonly ILogger<SessionManager> logger;
    private readonly string sessionsDirectory;
    private readonly string indexFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public SessionManager(ILogger<SessionManager> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        sessionsDirectory = Path.Combine(homeDir, ".notebookautomation", "sessions");
        indexFilePath = Path.Combine(sessionsDirectory, "index.json");
        
        Directory.CreateDirectory(sessionsDirectory);
    }

    /// <inheritdoc/>
    public async Task SaveSessionAsync(CopilotSessionMetadata session, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionPath = Path.Combine(sessionsDirectory, $"{session.SessionId}.json");
            var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(sessionPath, json, cancellationToken);
            
            await UpdateIndexAsync(session, cancellationToken);
            logger.LogInformation("Saved session {SessionId}", session.SessionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save session {SessionId}", session.SessionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CopilotSessionMetadata?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionPath = Path.Combine(sessionsDirectory, $"{sessionId}.json");
            if (!File.Exists(sessionPath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(sessionPath, cancellationToken);
            return JsonSerializer.Deserialize<CopilotSessionMetadata>(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load session {SessionId}", sessionId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CopilotSessionMetadata>> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(indexFilePath))
            {
                return Array.Empty<CopilotSessionMetadata>();
            }

            var json = await File.ReadAllTextAsync(indexFilePath, cancellationToken);
            var sessions = JsonSerializer.Deserialize<List<CopilotSessionMetadata>>(json);
            return (IReadOnlyList<CopilotSessionMetadata>)(sessions ?? new List<CopilotSessionMetadata>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list sessions");
            return Array.Empty<CopilotSessionMetadata>();
        }
    }

    /// <inheritdoc/>
    public async Task<CopilotSessionMetadata?> GetLastSessionAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await ListSessionsAsync(cancellationToken);
        return sessions.OrderByDescending(s => s.LastAccessedAt).FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionPath = Path.Combine(sessionsDirectory, $"{sessionId}.json");
            if (File.Exists(sessionPath))
            {
                File.Delete(sessionPath);
            }

            await RemoveFromIndexAsync(sessionId, cancellationToken);
            logger.LogInformation("Deleted session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete session {SessionId}", sessionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> PurgeOldSessionsAsync(int olderThanDays, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await ListSessionsAsync(cancellationToken);
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            var sessionsToDelete = sessions.Where(s => s.LastAccessedAt < cutoffDate).ToList();

            foreach (var session in sessionsToDelete)
            {
                await DeleteSessionAsync(session.SessionId, cancellationToken);
            }

            logger.LogInformation("Purged {Count} old sessions", sessionsToDelete.Count);
            return sessionsToDelete.Count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to purge old sessions");
            return 0;
        }
    }

    private async Task UpdateIndexAsync(CopilotSessionMetadata session, CancellationToken cancellationToken)
    {
        var sessions = (await ListSessionsAsync(cancellationToken)).ToList();
        var existing = sessions.FirstOrDefault(s => s.SessionId == session.SessionId);
        
        if (existing != null)
        {
            sessions.Remove(existing);
        }
        
        sessions.Add(session);
        
        var json = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(indexFilePath, json, cancellationToken);
    }

    private async Task RemoveFromIndexAsync(string sessionId, CancellationToken cancellationToken)
    {
        var sessions = (await ListSessionsAsync(cancellationToken)).ToList();
        sessions.RemoveAll(s => s.SessionId == sessionId);
        
        var json = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(indexFilePath, json, cancellationToken);
    }
}
