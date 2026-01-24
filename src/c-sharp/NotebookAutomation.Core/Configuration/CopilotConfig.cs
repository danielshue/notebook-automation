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
    /// Gets or sets the path to the Copilot CLI executable.
    /// If null, will use "gh" from PATH.
    /// </summary>
    public string? CliPath { get; set; }

    /// <summary>
    /// Gets or sets the default model to use for chat sessions.
    /// </summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to auto-enter chat mode when no args provided.
    /// </summary>
    public bool AutoChatMode { get; set; } = true;

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
}
