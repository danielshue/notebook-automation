// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Represents a Copilot conversation session.
/// </summary>
public interface ICopilotSession : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique session identifier.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Gets the model being used for this session.
    /// </summary>
    string? Model { get; }

    /// <summary>
    /// Gets the session metadata.
    /// </summary>
    CopilotSessionMetadata Metadata { get; }

    /// <summary>
    /// Send a message to the Copilot session and get a response.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from Copilot.</returns>
    Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message to the Copilot session and stream the response.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of response chunks.</returns>
    IAsyncEnumerable<string> SendMessageStreamAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the conversation history for this session.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of messages in the conversation.</returns>
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear the conversation history.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Save the current session.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a message in a chat conversation.
/// </summary>
public record ChatMessage(
    string Role,
    string Content,
    DateTime Timestamp);
