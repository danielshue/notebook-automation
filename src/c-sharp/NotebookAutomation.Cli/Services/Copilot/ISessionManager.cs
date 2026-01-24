// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Manages Copilot session persistence and lifecycle.
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Save a session.
    /// </summary>
    /// <param name="session">Session metadata to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveSessionAsync(CopilotSessionMetadata session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load a session by ID.
    /// </summary>
    /// <param name="sessionId">Session ID to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session metadata, or null if not found.</returns>
    Task<CopilotSessionMetadata?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// List all saved sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of session metadata.</returns>
    Task<IReadOnlyList<CopilotSessionMetadata>> ListSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the most recent session.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Most recent session metadata, or null if none exist.</returns>
    Task<CopilotSessionMetadata?> GetLastSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a session.
    /// </summary>
    /// <param name="sessionId">Session ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Purge old sessions based on retention policy.
    /// </summary>
    /// <param name="olderThanDays">Delete sessions older than this many days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of sessions purged.</returns>
    Task<int> PurgeOldSessionsAsync(int olderThanDays, CancellationToken cancellationToken = default);
}
