// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Abstraction for GitHub Copilot SDK operations.
/// Provides testability and allows graceful degradation when Copilot is unavailable.
/// </summary>
public interface ICopilotService : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the Copilot client is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Check if Copilot CLI is available and authenticated.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if Copilot is available and ready to use.</returns>
    Task<CopilotAvailabilityResult> CheckAvailabilityAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Start the Copilot client and establish connection.
    /// </summary>
    /// <param name="options">Client startup options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(
        CopilotStartupOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the Copilot client gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new conversation session with optional configuration.
    /// </summary>
    /// <param name="config">Session configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new Copilot session.</returns>
    Task<ICopilotSession> CreateSessionAsync(
        CopilotSessionConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resume an existing session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID to resume.</param>
    /// <param name="config">Optional configuration overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resumed session.</returns>
    Task<ICopilotSession> ResumeSessionAsync(
        string sessionId,
        CopilotSessionConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all available sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of session metadata.</returns>
    Task<IReadOnlyList<CopilotSessionMetadata>> ListSessionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Start interactive chat mode with the full UI experience.
    /// </summary>
    /// <param name="options">Chat mode options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code (0 for success).</returns>
    Task<int> StartInteractiveChatAsync(
        ChatModeOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a one-shot question and get a response (no interactive session).
    /// </summary>
    /// <param name="prompt">The question or prompt.</param>
    /// <param name="options">Ask options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response text.</returns>
    Task<string> AskAsync(
        string prompt,
        AskOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available models from the Copilot CLI.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available model names.</returns>
    Task<IReadOnlyList<string>> GetAvailableModelsAsync(
        CancellationToken cancellationToken = default);
}
