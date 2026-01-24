// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Logs session interactions to files for debugging, auditing, or replay.
/// </summary>
public class SessionLogger : IAsyncDisposable
{
    private readonly ILogger<SessionLogger> logger;
    private readonly CopilotLoggingConfig config;
    private readonly string logDirectory;
    private StreamWriter? currentLogWriter;
    private string? currentSessionId;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionLogger"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="config">Logging configuration.</param>
    /// <param name="logDirectory">Directory for session logs.</param>
    public SessionLogger(
        ILogger<SessionLogger> logger,
        CopilotLoggingConfig? config = null,
        string? logDirectory = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.config = config ?? new CopilotLoggingConfig();
        this.logDirectory = logDirectory ?? this.config.LogDirectory ?? GetDefaultLogDirectory();

        if (this.config.Enabled)
        {
            EnsureLogDirectoryExists();
        }
    }

    /// <summary>
    /// Gets a value indicating whether session logging is enabled.
    /// </summary>
    public bool IsEnabled => config.Enabled;

    /// <summary>
    /// Gets the path to the current log file.
    /// </summary>
    public string? CurrentLogPath { get; private set; }

    /// <summary>
    /// Start logging a new session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="metadata">Optional session metadata.</param>
    public async Task StartSessionAsync(
        string sessionId,
        CopilotSessionMetadata? metadata = null)
    {
        if (!IsEnabled)
        {
            return;
        }

        // Close any existing log
        await CloseCurrentLogAsync();

        currentSessionId = sessionId;
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
        var logFileName = $"session_{sessionId}_{timestamp}.jsonl";
        CurrentLogPath = Path.Combine(logDirectory, logFileName);

        currentLogWriter = new StreamWriter(CurrentLogPath, append: false);

        // Write session start entry
        await WriteEntryAsync(new SessionLogEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = SessionLogEventType.SessionStart,
            SessionId = sessionId,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null
        });

        logger.LogInformation("Started session logging to {LogPath}", CurrentLogPath);
    }

    /// <summary>
    /// Log a user message.
    /// </summary>
    /// <param name="message">The user's message.</param>
    /// <param name="attachments">Any file attachments.</param>
    public async Task LogUserMessageAsync(
        string message,
        IEnumerable<FileAttachment>? attachments = null)
    {
        if (!IsEnabled || currentLogWriter == null)
        {
            return;
        }

        var attachmentPaths = attachments?.Select(a => a.Path).ToArray();

        await WriteEntryAsync(new SessionLogEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = SessionLogEventType.UserMessage,
            SessionId = currentSessionId!,
            Content = message,
            Attachments = attachmentPaths?.Length > 0 ? attachmentPaths : null
        });
    }

    /// <summary>
    /// Log an assistant response.
    /// </summary>
    /// <param name="response">The assistant's response.</param>
    /// <param name="model">The model used.</param>
    /// <param name="tokenCount">Optional token count.</param>
    public async Task LogAssistantResponseAsync(
        string response,
        string? model = null,
        int? tokenCount = null)
    {
        if (!IsEnabled || currentLogWriter == null)
        {
            return;
        }

        await WriteEntryAsync(new SessionLogEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = SessionLogEventType.AssistantMessage,
            SessionId = currentSessionId!,
            Content = response,
            Model = model,
            TokenCount = tokenCount
        });
    }

    /// <summary>
    /// Log a tool execution.
    /// </summary>
    /// <param name="toolName">The tool name.</param>
    /// <param name="arguments">The tool arguments.</param>
    /// <param name="result">The execution result.</param>
    /// <param name="success">Whether the execution succeeded.</param>
    /// <param name="durationMs">Execution duration in milliseconds.</param>
    public async Task LogToolExecutionAsync(
        string toolName,
        string? arguments,
        string? result,
        bool success,
        long? durationMs = null)
    {
        if (!IsEnabled || currentLogWriter == null)
        {
            return;
        }

        await WriteEntryAsync(new SessionLogEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = SessionLogEventType.ToolExecution,
            SessionId = currentSessionId!,
            ToolName = toolName,
            ToolArguments = arguments,
            ToolResult = result,
            Success = success,
            DurationMs = durationMs
        });
    }

    /// <summary>
    /// Log an error.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorCode">Optional error code.</param>
    /// <param name="exception">Optional exception.</param>
    public async Task LogErrorAsync(
        string errorMessage,
        string? errorCode = null,
        Exception? exception = null)
    {
        if (!IsEnabled || currentLogWriter == null)
        {
            return;
        }

        await WriteEntryAsync(new SessionLogEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = SessionLogEventType.Error,
            SessionId = currentSessionId!,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            StackTrace = exception?.StackTrace
        });
    }

    /// <summary>
    /// Log session end.
    /// </summary>
    /// <param name="messageCount">Total message count.</param>
    /// <param name="totalDurationMs">Total session duration.</param>
    public async Task EndSessionAsync(
        int? messageCount = null,
        long? totalDurationMs = null)
    {
        if (!IsEnabled || currentLogWriter == null)
        {
            return;
        }

        await WriteEntryAsync(new SessionLogEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = SessionLogEventType.SessionEnd,
            SessionId = currentSessionId!,
            MessageCount = messageCount,
            DurationMs = totalDurationMs
        });

        await CloseCurrentLogAsync();

        logger.LogInformation("Session logging ended for {SessionId}", currentSessionId);
    }

    /// <summary>
    /// Write a log entry.
    /// </summary>
    private async Task WriteEntryAsync(SessionLogEntry entry)
    {
        if (currentLogWriter == null)
        {
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            await currentLogWriter.WriteLineAsync(json);
            await currentLogWriter.FlushAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write session log entry");
        }
    }

    /// <summary>
    /// Close the current log file.
    /// </summary>
    private async Task CloseCurrentLogAsync()
    {
        if (currentLogWriter != null)
        {
            await currentLogWriter.FlushAsync();
            await currentLogWriter.DisposeAsync();
            currentLogWriter = null;
        }

        currentSessionId = null;
        CurrentLogPath = null;
    }

    /// <summary>
    /// Get the default log directory.
    /// </summary>
    private static string GetDefaultLogDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".notebookautomation",
            "logs",
            "sessions");
    }

    /// <summary>
    /// Ensure the log directory exists.
    /// </summary>
    private void EnsureLogDirectoryExists()
    {
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
            logger.LogDebug("Created session log directory: {LogDirectory}", logDirectory);
        }
    }

    /// <summary>
    /// Get recent session log files.
    /// </summary>
    /// <param name="count">Maximum number of files to return.</param>
    /// <returns>List of log file paths, most recent first.</returns>
    public IReadOnlyList<string> GetRecentLogFiles(int count = 10)
    {
        if (!Directory.Exists(logDirectory))
        {
            return [];
        }

        return Directory.GetFiles(logDirectory, "session_*.jsonl")
            .OrderByDescending(f => new FileInfo(f).CreationTimeUtc)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Read entries from a session log file.
    /// </summary>
    /// <param name="logPath">Path to the log file.</param>
    /// <returns>Sequence of log entries.</returns>
    public async IAsyncEnumerable<SessionLogEntry> ReadLogEntriesAsync(string logPath)
    {
        if (!File.Exists(logPath))
        {
            yield break;
        }

        await foreach (var line in File.ReadLinesAsync(logPath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            SessionLogEntry? entry = null;
            try
            {
                entry = JsonSerializer.Deserialize<SessionLogEntry>(line);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse log entry");
            }

            if (entry != null)
            {
                yield return entry;
            }
        }
    }

    /// <summary>
    /// Clean up old log files.
    /// </summary>
    /// <param name="retentionDays">Keep logs newer than this many days.</param>
    /// <returns>Number of files deleted.</returns>
    public int CleanupOldLogs(int retentionDays = 30)
    {
        if (!Directory.Exists(logDirectory))
        {
            return 0;
        }

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var deleted = 0;

        foreach (var file in Directory.GetFiles(logDirectory, "session_*.jsonl"))
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.CreationTimeUtc < cutoff)
            {
                try
                {
                    File.Delete(file);
                    deleted++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete old log file: {File}", file);
                }
            }
        }

        if (deleted > 0)
        {
            logger.LogInformation("Cleaned up {Count} old session log files", deleted);
        }

        return deleted;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!disposed)
        {
            await CloseCurrentLogAsync();
            disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// A single entry in a session log.
/// </summary>
public class SessionLogEntry
{
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public SessionLogEventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the metadata JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets file attachment paths.
    /// </summary>
    public string[]? Attachments { get; set; }

    /// <summary>
    /// Gets or sets the model used.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the token count.
    /// </summary>
    public int? TokenCount { get; set; }

    /// <summary>
    /// Gets or sets the tool name.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Gets or sets the tool arguments.
    /// </summary>
    public string? ToolArguments { get; set; }

    /// <summary>
    /// Gets or sets the tool result.
    /// </summary>
    public string? ToolResult { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool? Success { get; set; }

    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the message count.
    /// </summary>
    public int? MessageCount { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the stack trace.
    /// </summary>
    public string? StackTrace { get; set; }
}

/// <summary>
/// Types of events in session logs.
/// </summary>
public enum SessionLogEventType
{
    /// <summary>Session started.</summary>
    SessionStart,

    /// <summary>Session ended.</summary>
    SessionEnd,

    /// <summary>User sent a message.</summary>
    UserMessage,

    /// <summary>Assistant responded.</summary>
    AssistantMessage,

    /// <summary>Tool was executed.</summary>
    ToolExecution,

    /// <summary>An error occurred.</summary>
    Error
}
