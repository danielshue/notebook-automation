// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Implementation of ICopilotService wrapping the GitHub Copilot SDK.
/// </summary>
public class CopilotService : ICopilotService
{
    private readonly ILogger<CopilotService> logger;
    private readonly CopilotAvailabilityChecker availabilityChecker;
    private bool isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="availabilityChecker">Availability checker.</param>
    public CopilotService(
        ILogger<CopilotService> logger,
        CopilotAvailabilityChecker availabilityChecker)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.availabilityChecker = availabilityChecker ?? throw new ArgumentNullException(nameof(availabilityChecker));
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
        isRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Stopping Copilot service");
        isRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<ICopilotSession> CreateSessionAsync(
        CopilotSessionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Session creation will be implemented in Phase 2");
    }

    /// <inheritdoc/>
    public Task<ICopilotSession> ResumeSessionAsync(
        string sessionId,
        CopilotSessionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Session resumption will be implemented in Phase 4");
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<CopilotSessionMetadata>> ListSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Session listing will be implemented in Phase 4");
    }

    /// <inheritdoc/>
    public Task DeleteSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Session deletion will be implemented in Phase 4");
    }

    /// <inheritdoc/>
    public Task<int> StartInteractiveChatAsync(
        ChatModeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Interactive chat will be implemented in Phase 2");
    }

    /// <inheritdoc/>
    public Task<string> AskAsync(
        string prompt,
        AskOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Ask command will be implemented in Phase 2");
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetAvailableModelsAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Model listing will be implemented in Phase 2");
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
