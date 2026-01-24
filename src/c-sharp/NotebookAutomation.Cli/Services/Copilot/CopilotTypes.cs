// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Result of checking Copilot availability.
/// </summary>
public record CopilotAvailabilityResult(
    bool IsAvailable,
    bool IsCliInstalled,
    bool IsAuthenticated,
    string? CliVersion,
    string? ErrorMessage);

/// <summary>
/// Options for starting the Copilot client.
/// </summary>
public record CopilotStartupOptions
{
    /// <summary>
    /// Working directory for the CLI process.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Log level for SDK logging.
    /// </summary>
    public string LogLevel { get; init; } = "info";

    /// <summary>
    /// Whether to auto-restart on crash.
    /// </summary>
    public bool AutoRestart { get; init; } = true;
}

/// <summary>
/// Configuration for creating a Copilot session.
/// </summary>
public record CopilotSessionConfig
{
    /// <summary>
    /// Custom session ID. If null, one will be generated.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Model to use (e.g., "gpt-5", "claude-sonnet-4.5").
    /// If null, uses Copilot CLI default.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Enable streaming responses.
    /// </summary>
    public bool Streaming { get; init; } = true;

    /// <summary>
    /// Custom tools to make available to the model.
    /// </summary>
    public IReadOnlyList<object>? Tools { get; init; }

    /// <summary>
    /// System message configuration.
    /// </summary>
    public SystemMessageConfig? SystemMessage { get; init; }

    /// <summary>
    /// List of tool names to allow. If null, all tools are allowed.
    /// </summary>
    public IReadOnlyList<string>? AvailableTools { get; init; }

    /// <summary>
    /// List of tool names to exclude.
    /// </summary>
    public IReadOnlyList<string>? ExcludedTools { get; init; }
}

/// <summary>
/// System message configuration.
/// </summary>
public record SystemMessageConfig
{
    /// <summary>
    /// How to apply the system message.
    /// </summary>
    public SystemMessageMode Mode { get; init; } = SystemMessageMode.Append;

    /// <summary>
    /// The system message content.
    /// </summary>
    public required string Content { get; init; }
}

/// <summary>
/// How to apply a custom system message.
/// </summary>
public enum SystemMessageMode
{
    /// <summary>
    /// Append to the default system message.
    /// </summary>
    Append,

    /// <summary>
    /// Replace the default system message entirely.
    /// </summary>
    Replace
}

/// <summary>
/// Metadata about a Copilot session.
/// </summary>
public record CopilotSessionMetadata(
    string SessionId,
    DateTime CreatedAt,
    DateTime LastAccessedAt,
    string? Model,
    int MessageCount);

/// <summary>
/// Options for interactive chat mode.
/// </summary>
public record ChatModeOptions
{
    /// <summary>
    /// Whether to resume the last session.
    /// </summary>
    public bool Resume { get; init; }

    /// <summary>
    /// Specific session ID to resume.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Model to use for the chat session.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Whether to display the welcome banner.
    /// </summary>
    public bool ShowBanner { get; init; } = true;

    /// <summary>
    /// Whether to use high contrast colors.
    /// </summary>
    public bool HighContrast { get; init; }
}

/// <summary>
/// Options for one-shot ask queries.
/// </summary>
public record AskOptions
{
    /// <summary>
    /// Model to use for the query.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Whether to output in JSON format.
    /// </summary>
    public bool Json { get; init; }

    /// <summary>
    /// Whether to stream the response.
    /// </summary>
    public bool Stream { get; init; } = true;
}
