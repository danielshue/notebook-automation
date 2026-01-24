// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Configuration for GitHub Copilot SDK integration.
/// </summary>
public class CopilotConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether Copilot integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default model to use for chat sessions.
    /// </summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to auto-enter chat mode when no args provided.
    /// </summary>
    public bool AutoChatMode { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to show the welcome banner in chat mode.
    /// </summary>
    public bool ShowWelcomeBanner { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable streaming responses.
    /// </summary>
    public bool EnableStreaming { get; set; } = true;

    /// <summary>
    /// Gets or sets the session retention period in days.
    /// Sessions older than this will be purged.
    /// </summary>
    public int SessionRetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to save sessions automatically.
    /// </summary>
    public bool AutoSaveSessions { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of sessions to keep.
    /// </summary>
    public int MaxSessions { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether to use high contrast colors.
    /// </summary>
    public bool HighContrast { get; set; }

    /// <summary>
    /// Gets or sets the log level for Copilot SDK operations.
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets accessibility options.
    /// </summary>
    public CopilotAccessibilityConfig Accessibility { get; set; } = new();

    /// <summary>
    /// Gets or sets logging options.
    /// </summary>
    public CopilotLoggingConfig Logging { get; set; } = new();
}

/// <summary>
/// Accessibility configuration for Copilot CLI.
/// </summary>
public record CopilotAccessibilityConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether high contrast mode is enabled.
    /// </summary>
    public bool HighContrast { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether screen reader mode is enabled.
    /// </summary>
    public bool ScreenReaderMode { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to reduce motion/animations.
    /// </summary>
    public bool ReducedMotion { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to use simple output format.
    /// </summary>
    public bool SimpleOutput { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to announce progress updates.
    /// </summary>
    public bool AnnounceProgress { get; init; }
}

/// <summary>
/// Logging configuration for Copilot sessions.
/// </summary>
public class CopilotLoggingConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether session logging is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the directory for session logs.
    /// </summary>
    public string? LogDirectory { get; set; }

    /// <summary>
    /// Gets or sets the retention period for logs in days.
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to log request content.
    /// </summary>
    public bool LogRequestContent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to log response content.
    /// </summary>
    public bool LogResponseContent { get; set; }
}
