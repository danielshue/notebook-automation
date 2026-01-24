// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

using Microsoft.Extensions.AI;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Implementation of a Copilot conversation session with live AI integration.
/// </summary>
/// <remarks>
/// Uses Microsoft.Extensions.AI IChatClient for production AI responses with streaming
/// and function calling support for all 21 registered Notebook Automation tools.
/// </remarks>
public class CopilotSession : ICopilotSession
{
    private readonly IChatClient chatClient;
    private readonly ILogger<CopilotSession> logger;
    private readonly ISessionManager sessionManager;
    private readonly INotebookTools notebookTools;
    private readonly List<Microsoft.Extensions.AI.ChatMessage> conversationHistory = new();
    private CopilotSessionMetadata metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotSession"/> class.
    /// </summary>
    /// <param name="chatClient">The AI chat client instance.</param>
    /// <param name="config">Session configuration.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="notebookTools">Notebook tools registry.</param>
    /// <param name="systemMessageBuilder">System message builder.</param>
    /// <param name="sessionManager">Session manager.</param>
    public CopilotSession(
        IChatClient chatClient,
        CopilotSessionConfig? config,
        ILogger<CopilotSession> logger,
        INotebookTools notebookTools,
        ISystemMessageBuilder systemMessageBuilder,
        ISessionManager sessionManager)
    {
        this.chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.notebookTools = notebookTools ?? throw new ArgumentNullException(nameof(notebookTools));
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
        conversationHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, systemMessage));

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
        conversationHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, message));

        // Create chat options with tools
        var options = new ChatOptions
        {
            Tools = notebookTools.GetAllTools()
                .Cast<AITool>()
                .ToList()
        };

        try
        {
            // Call AI with full conversation history and tools
            var response = await chatClient.GetResponseAsync(
                conversationHistory,
                options,
                cancellationToken);

            var responseText = response.Text ?? "No response generated.";

            // Add assistant response to history
            if (response.Messages.Any())
            {
                foreach (var msg in response.Messages.Where(m => m.Role == ChatRole.Assistant))
                {
                    conversationHistory.Add(msg);
                }
            }

            // Update metadata
            metadata = metadata with
            {
                LastAccessedAt = DateTime.UtcNow,
                MessageCount = metadata.MessageCount + 1
            };

            return responseText;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message to AI");
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

        // Add user message to history
        conversationHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, message));

        // Create chat options with tools
        var options = new ChatOptions
        {
            Tools = notebookTools.GetAllTools()
                .Cast<AITool>()
                .ToList()
        };

        var responseBuilder = new StringBuilder();

        await foreach (var update in chatClient.GetStreamingResponseAsync(
            conversationHistory,
            options,
            cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                responseBuilder.Append(update.Text);
                yield return update.Text;
            }

            // Handle tool calls if present  (automatic via UseFunctionInvocation)
            if (update.Contents.OfType<FunctionCallContent>().Any())
            {
                foreach (var toolCall in update.Contents.OfType<FunctionCallContent>())
                {
                    logger.LogInformation("Tool called: {ToolName}", toolCall.Name);
                }
            }
        }

        // Add complete assistant response to history
        conversationHistory.Add(new Microsoft.Extensions.AI.ChatMessage(
            ChatRole.Assistant,
            responseBuilder.ToString()));

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
        // Filter out system messages and convert to interface type
        var history = conversationHistory
            .Where(m => m.Role != ChatRole.System)
            .Select(m => new ChatMessage(
                m.Role.Value,
                m.Text ?? string.Empty,
                DateTime.UtcNow)) // Note: original timestamps not preserved in current impl
            .ToList();

        return Task.FromResult<IReadOnlyList<ChatMessage>>(history);
    }

    /// <inheritdoc/>
    public Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        // Keep system message, clear the rest
        var systemMessage = conversationHistory.FirstOrDefault(m => m.Role == ChatRole.System);
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
