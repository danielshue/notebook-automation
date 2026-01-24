// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

using GitHub.Copilot.SDK;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Adapter that wraps the GitHub Copilot SDK CopilotSession to implement ICopilotSession.
/// </summary>
/// <remarks>
/// This class adapts the event-based GitHub Copilot SDK session model to the
/// simpler request-response model expected by our ICopilotSession interface.
/// </remarks>
public class CopilotSessionAdapter : ICopilotSession
{
    private readonly GitHub.Copilot.SDK.CopilotSession sdkSession;
    private readonly ILogger<CopilotSessionAdapter> logger;
    private readonly ISessionManager sessionManager;
    private readonly List<ChatMessage> conversationHistory = [];
    private CopilotSessionMetadata metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotSessionAdapter"/> class.
    /// </summary>
    /// <param name="sdkSession">The GitHub Copilot SDK session instance.</param>
    /// <param name="config">Session configuration.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sessionManager">Session manager for persistence.</param>
    public CopilotSessionAdapter(
        GitHub.Copilot.SDK.CopilotSession sdkSession,
        CopilotSessionConfig? config,
        ILogger<CopilotSessionAdapter> logger,
        ISessionManager sessionManager)
    {
        this.sdkSession = sdkSession ?? throw new ArgumentNullException(nameof(sdkSession));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));

        var now = DateTime.UtcNow;

        metadata = new CopilotSessionMetadata(
            SessionId: sdkSession.SessionId,
            CreatedAt: now,
            LastAccessedAt: now,
            Model: config?.Model,
            MessageCount: 0);

        logger.LogInformation("Created Copilot session adapter for {SessionId}", sdkSession.SessionId);
    }

    /// <inheritdoc/>
    public string SessionId => sdkSession.SessionId;

    /// <inheritdoc/>
    public string? Model => metadata.Model;

    /// <inheritdoc/>
    public CopilotSessionMetadata Metadata => metadata;

    /// <inheritdoc/>
    public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(message));
        }

        logger.LogDebug("Sending message to Copilot session {SessionId}", SessionId);

        // Track user message
        conversationHistory.Add(new ChatMessage("user", message, DateTime.UtcNow));

        var responseBuilder = new StringBuilder();
        var completionSource = new TaskCompletionSource<string>();

        // Subscribe to events
        using var subscription = sdkSession.On(evt =>
        {
            switch (evt)
            {
                case AssistantMessageEvent msg:
                    responseBuilder.Append(msg.Data.Content);
                    break;
                case SessionIdleEvent:
                    completionSource.TrySetResult(responseBuilder.ToString());
                    break;
                case SessionErrorEvent err:
                    completionSource.TrySetException(new CopilotException(err.Data.Message));
                    break;
                case ToolExecutionStartEvent toolStart:
                    logger.LogInformation("Tool execution started: {ToolName}", toolStart.Data.ToolName);
                    break;
                case ToolExecutionCompleteEvent toolComplete:
                    logger.LogInformation("Tool execution completed: {ToolCallId}", toolComplete.Data.ToolCallId);
                    break;
            }
        });

        try
        {
            // Send the message
            await sdkSession.SendAsync(new MessageOptions { Prompt = message });

            // Wait for completion with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(5));

            var response = await completionSource.Task.WaitAsync(cts.Token);

            // Track assistant response
            conversationHistory.Add(new ChatMessage("assistant", response, DateTime.UtcNow));

            // Update metadata
            metadata = metadata with
            {
                LastAccessedAt = DateTime.UtcNow,
                MessageCount = metadata.MessageCount + 1
            };

            return response;
        }
        catch (OperationCanceledException)
        {
            await sdkSession.AbortAsync();
            throw new TimeoutException("Request timed out waiting for Copilot response");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message to Copilot");
            throw;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> SendMessageStreamAsync(
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(message));
        }

        logger.LogDebug("Sending streaming message to Copilot session {SessionId}", SessionId);

        // Track user message
        conversationHistory.Add(new ChatMessage("user", message, DateTime.UtcNow));

        var responseBuilder = new StringBuilder();
        var channel = System.Threading.Channels.Channel.CreateUnbounded<string>();
        var completionSource = new TaskCompletionSource();

        // Subscribe to events
        using var subscription = sdkSession.On(evt =>
        {
            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    // Streaming chunk
                    if (!string.IsNullOrEmpty(delta.Data.DeltaContent))
                    {
                        responseBuilder.Append(delta.Data.DeltaContent);
                        channel.Writer.TryWrite(delta.Data.DeltaContent);
                    }
                    break;
                case AssistantMessageEvent msg:
                    // Final complete message (also sent even with streaming)
                    break;
                case SessionIdleEvent:
                    channel.Writer.Complete();
                    completionSource.TrySetResult();
                    break;
                case SessionErrorEvent err:
                    channel.Writer.Complete(new CopilotException(err.Data.Message));
                    completionSource.TrySetException(new CopilotException(err.Data.Message));
                    break;
                case ToolExecutionStartEvent toolStart:
                    logger.LogInformation("Tool execution started: {ToolName}", toolStart.Data.ToolName);
                    break;
                case ToolExecutionCompleteEvent toolComplete:
                    logger.LogInformation("Tool execution completed: {ToolCallId}", toolComplete.Data.ToolCallId);
                    break;
            }
        });

        // Send the message (don't await completion)
        _ = sdkSession.SendAsync(new MessageOptions { Prompt = message });

        // Yield streaming chunks
        await foreach (var chunk in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return chunk;
        }

        // Wait for completion
        await completionSource.Task;

        // Track assistant response
        conversationHistory.Add(new ChatMessage("assistant", responseBuilder.ToString(), DateTime.UtcNow));

        // Update metadata
        metadata = metadata with
        {
            LastAccessedAt = DateTime.UtcNow,
            MessageCount = metadata.MessageCount + 1
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from SDK first
        try
        {
            var events = await sdkSession.GetMessagesAsync();
            var history = new List<ChatMessage>();

            foreach (var evt in events)
            {
                switch (evt)
                {
                    case UserMessageEvent userMsg:
                        history.Add(new ChatMessage("user", userMsg.Data.Content, DateTime.UtcNow));
                        break;
                    case AssistantMessageEvent assistantMsg:
                        history.Add(new ChatMessage("assistant", assistantMsg.Data.Content, DateTime.UtcNow));
                        break;
                }
            }

            return history;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get history from SDK, returning local history");
            return conversationHistory.ToList();
        }
    }

    /// <inheritdoc/>
    public Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        conversationHistory.Clear();
        logger.LogInformation("Cleared local conversation history for session {SessionId}", SessionId);

        // Note: SDK sessions don't support clearing history directly
        // A new session would need to be created for a fresh start
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await sessionManager.SaveSessionAsync(metadata, cancellationToken);
        logger.LogInformation("Saved session metadata for {SessionId}", SessionId);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await SaveAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving session {SessionId} during disposal", SessionId);
        }

        try
        {
            await sdkSession.DisposeAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error disposing SDK session {SessionId}", SessionId);
        }

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Exception thrown when a Copilot operation fails.
/// </summary>
public class CopilotException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotException"/> class.
    /// </summary>
    public CopilotException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotException"/> class.
    /// </summary>
    public CopilotException(string message, Exception innerException) : base(message, innerException) { }
}
