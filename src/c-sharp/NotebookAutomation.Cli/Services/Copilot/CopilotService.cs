// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using GitHub.Copilot.SDK;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Implementation of ICopilotService using the official GitHub Copilot SDK.
/// </summary>
/// <remarks>
/// This implementation uses the GitHub.Copilot.SDK to connect to the Copilot CLI
/// server via JSON-RPC, leveraging the user's existing GitHub Copilot subscription.
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
    private CopilotClient? copilotClient;

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
    public async Task StartAsync(
        CopilotStartupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Copilot service with GitHub Copilot SDK");

        try
        {
            // Configure the Copilot client
            var clientOptions = new CopilotClientOptions
            {
                AutoStart = true,
                AutoRestart = true,
                UseStdio = true,
                LogLevel = "debug",
                Logger = loggerFactory.CreateLogger<CopilotClient>()
            };

            logger.LogInformation("Using auto-detected copilot CLI from PATH");

            // Create and start the Copilot client
            logger.LogInformation("Creating CopilotClient...");
            copilotClient = new CopilotClient(clientOptions);

            logger.LogInformation("Starting CopilotClient...");
            await copilotClient.StartAsync();

            // Verify connection with a ping
            logger.LogInformation("Sending ping to verify connection...");
            var pingResponse = await copilotClient.PingAsync("startup-check");
            logger.LogDebug("Copilot CLI ping response: {Response}", pingResponse);

            isRunning = true;
            logger.LogInformation("Copilot service started successfully via GitHub Copilot SDK");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start Copilot service");
            isRunning = false;
            logger.LogWarning("Copilot service will remain unavailable. Ensure GitHub Copilot CLI is installed and authenticated.");
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Stopping Copilot service");

        if (copilotClient != null)
        {
            try
            {
                await copilotClient.StopAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error during graceful Copilot client stop, forcing stop");
                try
                {
                    await copilotClient.ForceStopAsync();
                }
                catch (Exception forceEx)
                {
                    logger.LogDebug(forceEx, "Error during force stop (may be expected if client never started)");
                }
            }
            finally
            {
                try
                {
                    await copilotClient.DisposeAsync();
                }
                catch (Exception disposeEx)
                {
                    logger.LogDebug(disposeEx, "Error disposing Copilot client");
                }

                copilotClient = null;
            }
        }

        isRunning = false;
    }

    /// <inheritdoc/>
    public async Task<ICopilotSession> CreateSessionAsync(
        CopilotSessionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        if (!isRunning || copilotClient == null)
        {
            throw new InvalidOperationException("Copilot service is not running. Call StartAsync first.");
        }

        logger.LogInformation("Creating new Copilot session");

        // Build session configuration for the SDK
        var sessionConfig = BuildSessionConfig(config);

        // Create session via SDK
        var sdkSession = await copilotClient.CreateSessionAsync(sessionConfig);

        // Wrap in our session adapter
        var session = new CopilotSessionAdapter(
            sdkSession,
            config,
            loggerFactory.CreateLogger<CopilotSessionAdapter>(),
            sessionManager);

        return session;
    }

    /// <inheritdoc/>
    public async Task<ICopilotSession> ResumeSessionAsync(
        string sessionId,
        CopilotSessionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        if (!isRunning || copilotClient == null)
        {
            throw new InvalidOperationException("Copilot service is not running. Call StartAsync first.");
        }

        logger.LogInformation("Resuming session {SessionId}", sessionId);

        // Resume session via SDK
        var sdkSession = await copilotClient.ResumeSessionAsync(sessionId);

        // Wrap in our session adapter
        var session = new CopilotSessionAdapter(
            sdkSession,
            config,
            loggerFactory.CreateLogger<CopilotSessionAdapter>(),
            sessionManager);

        logger.LogInformation("Resumed session {SessionId}", sessionId);
        return session;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CopilotSessionMetadata>> ListSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        if (copilotClient != null && isRunning)
        {
            try
            {
                // Get sessions from SDK
                var sdkSessions = await copilotClient.ListSessionsAsync();

                return sdkSessions.Select(s => new CopilotSessionMetadata(
                    SessionId: s.SessionId,
                    CreatedAt: DateTime.UtcNow, // SDK doesn't expose creation time
                    LastAccessedAt: DateTime.UtcNow,
                    Model: null, // SDK doesn't expose model in metadata
                    MessageCount: 0
                )).ToList();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to list sessions from SDK, falling back to local storage");
            }
        }

        // Fallback to local session manager
        return await sessionManager.ListSessionsAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (copilotClient != null && isRunning)
        {
            try
            {
                await copilotClient.DeleteSessionAsync(sessionId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete session from SDK");
            }
        }

        // Also delete from local storage
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
    public async Task<string> AskAsync(
        string prompt,
        AskOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!isRunning || copilotClient == null)
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
            // Create a temporary session for the one-shot question
            var sessionConfig = BuildSessionConfig(null);
            await using var session = await copilotClient.CreateSessionAsync(sessionConfig);

            var responseBuilder = new StringBuilder();
            var completionSource = new TaskCompletionSource<string>();

            // Subscribe to events
            session.On(evt =>
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
                        completionSource.TrySetException(new Exception(err.Data.Message));
                        break;
                }
            });

            // Send the message
            await session.SendAsync(new MessageOptions { Prompt = prompt });

            // Wait for response with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(2));

            try
            {
                return await completionSource.Task.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                await session.AbortAsync();
                throw new TimeoutException("Request timed out waiting for Copilot response");
            }
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
        // Return models supported by GitHub Copilot SDK
        var models = new List<string>
        {
            "gpt-4o",
            "gpt-4",
            "claude-sonnet-4.5",
            "o1-preview",
            "o1-mini"
        };

        logger.LogDebug("Returning {Count} available models", models.Count);
        return Task.FromResult<IReadOnlyList<string>>(models);
    }

    /// <summary>
    /// Build SDK session configuration from our config model.
    /// </summary>
    private SessionConfig BuildSessionConfig(CopilotSessionConfig? config)
    {
        var sessionConfig = new SessionConfig
        {
            Model = config?.Model ?? appConfig.Copilot?.DefaultModel ?? "gpt-4o",
            Streaming = appConfig.Copilot?.EnableStreaming ?? true,
            Tools = notebookTools.GetAllTools().ToList()
        };

        // Configure system message
        if (config?.SystemMessage != null)
        {
            sessionConfig.SystemMessage = new GitHub.Copilot.SDK.SystemMessageConfig
            {
                Mode = config.SystemMessage.Mode == SystemMessageMode.Replace
                    ? GitHub.Copilot.SDK.SystemMessageMode.Replace
                    : GitHub.Copilot.SDK.SystemMessageMode.Append,
                Content = config.SystemMessage.Content
            };
        }
        else
        {
            // Use default system message
            var toolNames = notebookTools.GetAllTools()
                .Select(t => t.Name)
                .ToList();

            sessionConfig.SystemMessage = new GitHub.Copilot.SDK.SystemMessageConfig
            {
                Mode = GitHub.Copilot.SDK.SystemMessageMode.Append,
                Content = systemMessageBuilder.BuildSystemMessageWithTools(toolNames)
            };
        }

        // Configure infinite sessions
        if (appConfig.Copilot?.SessionRetentionDays > 0)
        {
            sessionConfig.InfiniteSessions = new InfiniteSessionConfig
            {
                Enabled = true
            };
        }

        return sessionConfig;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (isRunning)
        {
            await StopAsync();
        }

        GC.SuppressFinalize(this);
    }
}
