// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Implementation of ICopilotService.
/// </summary>
/// <remarks>
/// This implementation provides a working demonstration of the Copilot infrastructure
/// with stub AI responses. For full SDK integration, wire actual IChatClient from
/// Microsoft.Extensions.AI with Azure OpenAI or OpenAI.
/// See docs/SDK-INTEGRATION-STATUS.md for complete integration instructions.
/// </remarks>
public class CopilotService : ICopilotService
{
    private readonly ILogger<CopilotService> logger;
    private readonly CopilotAvailabilityChecker availabilityChecker;
    private readonly ISessionManager sessionManager;
    private readonly INotebookTools notebookTools;
    private readonly ISystemMessageBuilder systemMessageBuilder;
    private readonly ILoggerFactory loggerFactory;
    private readonly AppConfig appConfig;
    private bool isRunning;
    private object? chatClient; // Will be IChatClient from Microsoft.Extensions.AI when integrated

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotService"/> class.
    /// </summary>
    public CopilotService(
        ILogger<CopilotService> logger,
        CopilotAvailabilityChecker availabilityChecker,
        ISessionManager sessionManager,
        INotebookTools notebookTools,
        ISystemMessageBuilder systemMessageBuilder,
        ILoggerFactory loggerFactory,
        AppConfig appConfig)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.availabilityChecker = availabilityChecker ?? throw new ArgumentNullException(nameof(availabilityChecker));
        this.sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        this.notebookTools = notebookTools ?? throw new ArgumentNullException(nameof(notebookTools));
        this.systemMessageBuilder = systemMessageBuilder ?? throw new ArgumentNullException(nameof(systemMessageBuilder));
        this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        this.appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
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

        try
        {
            // Validate AI service configuration exists
            var apiKey = appConfig.AiService?.GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("No AI API key configured. Service will run in demonstration mode.");
                logger.LogInformation("Set AZURE_OPENAI_KEY, OPENAI_API_KEY, or FOUNDRY_API_KEY environment variable for full functionality.");
            }
            
            // Initialize chat client stub
            // TODO: For full SDK integration, uncomment and configure:
            // using Microsoft.Extensions.AI;
            // var provider = appConfig.AiService?.Provider?.ToLowerInvariant() ?? "openai";
            // chatClient = CreateChatClient(provider, apiKey, options);
            
            chatClient = new object(); // Stub placeholder
            
            isRunning = true;
            logger.LogInformation("Copilot service started in stub mode");
            logger.LogInformation("See docs/SDK-INTEGRATION-STATUS.md for SDK integration steps");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start Copilot service");
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Stopping Copilot service");

        if (chatClient is IDisposable disposable)
        {
            disposable.Dispose();
        }

        chatClient = null;
        isRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<ICopilotSession> CreateSessionAsync(
        CopilotSessionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        if (!isRunning || chatClient == null)
        {
            throw new InvalidOperationException("Copilot service is not running. Call StartAsync first.");
        }

        logger.LogInformation("Creating new Copilot session");

        var session = new CopilotSession(
            chatClient,
            config,
            loggerFactory.CreateLogger<CopilotSession>(),
            notebookTools,
            systemMessageBuilder,
            sessionManager);

        return Task.FromResult<ICopilotSession>(session);
    }

    /// <inheritdoc/>
    public async Task<ICopilotSession> ResumeSessionAsync(
        string sessionId,
        CopilotSessionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        if (!isRunning || chatClient == null)
        {
            throw new InvalidOperationException("Copilot service is not running. Call StartAsync first.");
        }

        logger.LogInformation("Resuming session {SessionId}", sessionId);
        
        var sessionMetadata = await sessionManager.LoadSessionAsync(sessionId, cancellationToken);
        if (sessionMetadata == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        // Create new session (history loading would be implemented here)
        var session = new CopilotSession(
            chatClient,
            config,
            loggerFactory.CreateLogger<CopilotSession>(),
            notebookTools,
            systemMessageBuilder,
            sessionManager);

        logger.LogInformation("Resumed session {SessionId}", sessionId);
        return session;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<CopilotSessionMetadata>> ListSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        return sessionManager.ListSessionsAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task DeleteSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        return sessionManager.DeleteSessionAsync(sessionId, cancellationToken);
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
    public async Task<string> AskAsync(
        string prompt,
        AskOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!isRunning || chatClient == null)
        {
            throw new InvalidOperationException("Copilot service is not running. Call StartAsync first.");
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
        }

        logger.LogInformation("Processing one-shot ask query");

        try
        {
            // Stub response - For SDK integration, replace with IChatClient.CompleteAsync call
            await Task.Delay(100, cancellationToken); // Simulate API call
            return $"Ask response for: '{prompt}'\n\nThis is a demonstration response. " +
                   "Complete SDK integration in docs/SDK-INTEGRATION-STATUS.md for live AI processing with tool calling.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing ask query");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetAvailableModelsAsync(
        CancellationToken cancellationToken = default)
    {
        // Return common model names
        var models = new List<string>
        {
            "gpt-4",
            "gpt-4-turbo",
            "gpt-3.5-turbo",
            "claude-sonnet-4.5"
        };
        
        logger.LogDebug("Returning {Count} available models", models.Count);
        return Task.FromResult<IReadOnlyList<string>>(models);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (isRunning)
        {
            await StopAsync();
        }

        if (chatClient is IDisposable disposable)
        {
            disposable.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
