// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Implementation of ICopilotService with live AI integration.
/// </summary>
/// <remarks>
/// This implementation uses Microsoft.Extensions.AI with Azure OpenAI or OpenAI
/// for production AI responses with function calling support.
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
    private IChatClient? chatClient;

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
        logger.LogInformation("Starting Copilot service with AI integration");

        try
        {
            // Get API configuration
            var provider = appConfig.AiService?.Provider?.ToLowerInvariant() ?? "openai";
            var apiKey = appConfig.AiService?.GetApiKey();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("No AI API key configured. Service cannot start.");
                throw new InvalidOperationException(
                    "No AI API key configured. Set AZURE_OPENAI_KEY, OPENAI_API_KEY, or FOUNDRY_API_KEY environment variable.");
            }

            // Create chat client based on provider
            chatClient = provider switch
            {
                "azure" => CreateAzureOpenAIChatClient(apiKey, options),
                "openai" => CreateOpenAIChatClient(apiKey, options),
                _ => throw new InvalidOperationException($"Unsupported AI provider: {provider}")
            };

            isRunning = true;
            logger.LogInformation("Copilot service started successfully with {Provider} provider", provider);
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
            // Create message for the AI
            var messages = new List<Microsoft.Extensions.AI.ChatMessage>
            {
                new(ChatRole.User, prompt)
            };

            // Send to AI and get response
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            return response.Text ?? "No response generated.";
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

    /// <summary>
    /// Create an Azure OpenAI chat client.
    /// </summary>
    private IChatClient CreateAzureOpenAIChatClient(
        string apiKey,
        CopilotStartupOptions? options)
    {
        var endpoint = appConfig.AiService?.Azure?.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("Azure OpenAI endpoint not configured");
        }

        var deploymentName = appConfig.AiService?.Azure?.Deployment ?? "gpt4";

        logger.LogInformation("Creating Azure OpenAI chat client: {Endpoint}, {Deployment}",
            endpoint, deploymentName);

        // Use Semantic Kernel's Azure OpenAI connector
        var kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                deploymentName: deploymentName,
                endpoint: endpoint,
                apiKey: apiKey)
            .Build();

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        return chatService.AsChatClient();
    }

    /// <summary>
    /// Create an OpenAI chat client.
    /// </summary>
    private IChatClient CreateOpenAIChatClient(
        string apiKey,
        CopilotStartupOptions? options)
    {
        var model = appConfig.AiService?.OpenAI?.Model ?? "gpt-4";

        logger.LogInformation("Creating OpenAI chat client: {Model}", model);

        // Use Semantic Kernel's OpenAI connector
        var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: model,
                apiKey: apiKey)
            .Build();

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        return chatService.AsChatClient();
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
