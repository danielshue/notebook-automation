// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Implementation of a Copilot conversation session.
/// </summary>
/// <remarks>
/// This is a working stub implementation that demonstrates the session structure.
/// For full SDK integration, replace the stub responses with actual IChatClient calls.
/// See docs/SDK-INTEGRATION-STATUS.md for implementation details.
/// </remarks>
public class CopilotSession : ICopilotSession
{
    private readonly object chatClient; // Placeholder - will be IChatClient from Microsoft.Extensions.AI
    private readonly ILogger<CopilotSession> logger;
    private readonly ISessionManager sessionManager;
    private readonly List<ChatMessage> conversationHistory = new();
    private CopilotSessionMetadata metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotSession"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client instance (placeholder for SDK integration).</param>
    /// <param name="config">Session configuration.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="notebookTools">Notebook tools registry.</param>
    /// <param name="systemMessageBuilder">System message builder.</param>
    /// <param name="sessionManager">Session manager.</param>
    public CopilotSession(
        object chatClient,
        CopilotSessionConfig? config,
        ILogger<CopilotSession> logger,
        INotebookTools notebookTools,
        ISystemMessageBuilder systemMessageBuilder,
        ISessionManager sessionManager)
    {
        this.chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));

        var sessionId = config?.SessionId ?? Guid.NewGuid().ToString("N")[..16];
        var now = DateTime.UtcNow;
        
        metadata = new CopilotSessionMetadata(
            SessionId: sessionId,
            CreatedAt: now,
            LastAccessedAt: now,
            Model: config?.Model,
            MessageCount: 0);

        // Initialize with system message
        var systemMessage = BuildSystemMessage(config, notebookTools, systemMessageBuilder);
        conversationHistory.Add(new ChatMessage("system", systemMessage, now));

        logger.LogInformation("Created Copilot session {SessionId}", sessionId);
    }

    /// <inheritdoc/>
    public string SessionId => metadata.SessionId;

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

        // Add user message to history
        var now = DateTime.UtcNow;
        conversationHistory.Add(new ChatMessage("user", message, now));

        // Stub response - For SDK integration, replace with:
        // var aiMessages = ConvertToAIMessages(conversationHistory);
        // var response = await ((IChatClient)chatClient).CompleteAsync(aiMessages, cancellationToken);
        // var responseText = response.Message.Text ?? string.Empty;
        
        var responseText = $"I understand you want to: '{message}'. \n\n" +
                          "This is a demonstration response. For full AI functionality, complete the SDK integration " +
                          "as described in docs/SDK-INTEGRATION-STATUS.md.\n\n" +
                          "I have 21 tools available for: vault management, tag operations, PDF/video processing, " +
                          "markdown generation, configuration, and OneDrive sync.";

        // Add assistant response to history
        conversationHistory.Add(new ChatMessage("assistant", responseText, DateTime.UtcNow));

        // Update metadata
        metadata = metadata with 
        { 
            LastAccessedAt = DateTime.UtcNow,
            MessageCount = metadata.MessageCount + 1
        };

        await Task.CompletedTask;
        return responseText;
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

        // Add user message to history
        var now = DateTime.UtcNow;
        conversationHistory.Add(new ChatMessage("user", message, now));

        // Stub streaming response - For SDK integration, replace with:
        // var aiMessages = ConvertToAIMessages(conversationHistory);
        // await foreach (var update in ((IChatClient)chatClient).CompleteStreamingAsync(aiMessages, cancellationToken))
        // {
        //     if (!string.IsNullOrEmpty(update.Text))
        //     {
        //         yield return update.Text;
        //     }
        // }
        
        var response = $"I received: '{message}' - This is a stub streaming response demonstrating the chat interface. Complete SDK integration for live AI responses with tool calling.";
        var words = response.Split(' ');
        
        for (int i = 0; i < words.Length; i++)
        {
            yield return words[i] + (i < words.Length - 1 ? " " : "");
            await Task.Delay(50, cancellationToken); // Simulate streaming effect
        }

        // Add complete assistant response to history
        conversationHistory.Add(new ChatMessage("assistant", response, DateTime.UtcNow));

        // Update metadata
        metadata = metadata with 
        { 
            LastAccessedAt = DateTime.UtcNow,
            MessageCount = metadata.MessageCount + 1
        };
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        // Filter out system messages
        var history = conversationHistory
            .Where(m => m.Role != "system")
            .ToList();

        return Task.FromResult<IReadOnlyList<ChatMessage>>(history);
    }

    /// <inheritdoc/>
    public Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        // Keep system message, clear the rest
        var systemMessage = conversationHistory.FirstOrDefault(m => m.Role == "system");
        conversationHistory.Clear();
        
        if (systemMessage != null)
        {
            conversationHistory.Add(systemMessage);
        }

        logger.LogInformation("Cleared conversation history for session {SessionId}", SessionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await sessionManager.SaveSessionAsync(metadata, cancellationToken);
        logger.LogInformation("Saved session {SessionId}", SessionId);
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

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Build the system message for the session.
    /// </summary>
    private string BuildSystemMessage(
        CopilotSessionConfig? config,
        INotebookTools notebookTools,
        ISystemMessageBuilder systemMessageBuilder)
    {
        if (config?.SystemMessage != null)
        {
            return config.SystemMessage.Mode == SystemMessageMode.Replace
                ? config.SystemMessage.Content
                : systemMessageBuilder.BuildDefaultSystemMessage() + "\n\n" + config.SystemMessage.Content;
        }

        // Build system message with tools
        var toolNames = notebookTools.GetAllTools()
            .Select((t, i) => $"tool_{i}") // Simplified - in real impl, extract actual tool names from AIFunction
            .ToList();

        return systemMessageBuilder.BuildSystemMessageWithTools(toolNames);
    }
}
