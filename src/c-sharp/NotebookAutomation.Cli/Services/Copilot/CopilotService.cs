// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Implementation of ICopilotService wrapping the GitHub Copilot SDK.
/// </summary>
/// <remarks>
/// This implementation provides the foundation for GitHub Copilot SDK integration.
/// The current version uses stub implementations that should be replaced with actual
/// SDK calls once the integration path is chosen (GitHub.Copilot.SDK or Microsoft.Extensions.AI).
/// See docs/copilot-sdk-integration-guide.md for detailed implementation instructions.
/// </remarks>
public class CopilotService : ICopilotService
{
    private readonly ILogger<CopilotService> logger;
    private readonly CopilotAvailabilityChecker availabilityChecker;
    private readonly ISessionManager sessionManager;
    private readonly INotebookTools notebookTools;
    private readonly ISystemMessageBuilder systemMessageBuilder;
    private bool isRunning;

    // TODO: Add SDK client field when implementing
    // private IChatClient? chatClient; // For Microsoft.Extensions.AI approach
    // private CopilotClient? copilotClient; // For GitHub.Copilot.SDK approach

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="availabilityChecker">Availability checker.</param>
    /// <param name="sessionManager">Session manager.</param>
    /// <param name="notebookTools">Notebook tools registry.</param>
    /// <param name="systemMessageBuilder">System message builder.</param>
    public CopilotService(
        ILogger<CopilotService> logger,
        CopilotAvailabilityChecker availabilityChecker,
        ISessionManager sessionManager,
        INotebookTools notebookTools,
        ISystemMessageBuilder systemMessageBuilder)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.availabilityChecker = availabilityChecker ?? throw new ArgumentNullException(nameof(availabilityChecker));
        this.sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        this.notebookTools = notebookTools ?? throw new ArgumentNullException(nameof(notebookTools));
        this.systemMessageBuilder = systemMessageBuilder ?? throw new ArgumentNullException(nameof(systemMessageBuilder));
    }

    /// <inheritdoc/>
    public bool IsRunning => isRunning;

    /// <inheritdoc/>
    public async Task<CopilotAvailabilityResult> CheckAvailabilityAsync(
        CancellationToken cancellationToken = default)
    {
        return await availabilityChecker.CheckAvailabilityAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task StartAsync(
        CopilotStartupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Copilot service");
        
        // TODO: Initialize SDK client here
        // Example for Microsoft.Extensions.AI:
        // var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        // var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
        // chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key))
        //     .AsChatClient("gpt-4")
        //     .AsBuilder()
        //     .UseFunctionInvocation()
        //     .Build();
        
        isRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Stopping Copilot service");
        
        // TODO: Dispose SDK client here
        // chatClient?.Dispose();
        
        isRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<ICopilotSession> CreateSessionAsync(
        CopilotSessionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        if (!isRunning)
        {
            throw new InvalidOperationException("Copilot service is not running. Call StartAsync first.");
        }

        logger.LogInformation("Creating new Copilot session");
        
        // TODO: Create actual session with SDK client
        // var session = new CopilotSession(chatClient, config, logger, notebookTools, systemMessageBuilder, sessionManager);
        // return Task.FromResult<ICopilotSession>(session);
        
        throw new NotImplementedException(
            "Session creation requires SDK integration. " +
            "See docs/copilot-sdk-integration-guide.md for implementation details.");
    }

    /// <inheritdoc/>
    public async Task<ICopilotSession> ResumeSessionAsync(
        string sessionId,
        CopilotSessionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        if (!isRunning)
        {
            throw new InvalidOperationException("Copilot service is not running. Call StartAsync first.");
        }

        logger.LogInformation("Resuming session {SessionId}", sessionId);
        
        var sessionMetadata = await sessionManager.LoadSessionAsync(sessionId, cancellationToken);
        if (sessionMetadata == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        // TODO: Load session history and create new session with context
        // var session = new CopilotSession(chatClient, config, logger, notebookTools, systemMessageBuilder, sessionManager);
        // await session.LoadHistoryAsync(sessionId, cancellationToken);
        // return session;
        
        throw new NotImplementedException(
            "Session resumption requires SDK integration. " +
            "See docs/copilot-sdk-integration-guide.md for implementation details.");
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CopilotSessionMetadata>> ListSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await sessionManager.ListSessionsAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        await sessionManager.DeleteSessionAsync(sessionId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<int> StartInteractiveChatAsync(
        ChatModeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // This is handled by ChatModeUI, not directly by the service
        throw new InvalidOperationException(
            "Use ChatModeUI.RunAsync() for interactive chat. " +
            "This method is not directly callable.");
    }

    /// <inheritdoc/>
    public Task<string> AskAsync(
        string prompt,
        AskOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!isRunning)
        {
            throw new InvalidOperationException("Copilot service is not running. Call StartAsync first.");
        }

        logger.LogInformation("Processing one-shot ask query");
        
        // TODO: Implement one-shot query without persistent session
        // var response = await chatClient.CompleteAsync(prompt, cancellationToken);
        // return response.Message.Text;
        
        throw new NotImplementedException(
            "Ask command requires SDK integration. " +
            "See docs/copilot-sdk-integration-guide.md for implementation details.");
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetAvailableModelsAsync(
        CancellationToken cancellationToken = default)
    {
        // TODO: Query available models from SDK/API
        // For now, return common models
        var models = new List<string>
        {
            "gpt-4",
            "gpt-4-turbo",
            "gpt-3.5-turbo"
        };
        
        return Task.FromResult<IReadOnlyList<string>>(models);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (isRunning)
        {
            await StopAsync();
        }

        // TODO: Dispose SDK client
        // chatClient?.Dispose();

        GC.SuppressFinalize(this);
    }
}
