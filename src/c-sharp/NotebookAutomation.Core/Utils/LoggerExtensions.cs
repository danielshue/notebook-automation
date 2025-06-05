// Module: LoggerExtensions.cs
// Provides OneDrive integration for file/folder sync and access using Microsoft Graph API.
#nullable enable

namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Provides extension methods for <see cref="ILogger"/> and <see cref="ILogger{T}"/> to simplify and standardize
/// logging of messages with file path formatting, exception handling, and flexible message templates.
/// </summary>
/// <remarks>
/// <para>
/// These extensions enable consistent logging patterns for file-related operations, supporting both full and shortened
/// file paths depending on log level, and provide overloads for common log levels (Information, Debug, Warning, Error).
/// </para>
/// <para>
/// Use <c>LogWithFormattedPath</c> and its overloads to automatically format file paths in log messages, and use
/// <c>LogFormatted</c> for general-purpose message formatting. All methods support structured logging and exception details.
/// </para>
/// <example>
/// <code>
/// logger.LogInformationWithPath("Processed file: {FilePath}", filePath);
/// logger.LogErrorWithPath(ex, "Failed to process: {FilePath}", filePath);
/// logger.LogDebugFormatted("Processing {Count} items", count);
/// </code>
/// </example>
/// </remarks>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs a message with a formatted file path, automatically shortening or expanding the path based on log level.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging.</param>
    /// <param name="logLevel">The severity level of the log message.</param>
    /// <param name="eventId">The event ID for structured logging (optional).</param>
    /// <param name="exception">An optional exception to include in the log entry.</param>
    /// <param name="message">A message template that should include a <c>{FilePath}</c> placeholder for the formatted path.</param>
    /// <param name="filePath">The file path to be formatted and injected into the log message.</param>
    /// <param name="args">Additional arguments to be formatted into the message template.</param>
    /// <remarks>
    /// For <see cref="LogLevel.Debug"/> or <see cref="LogLevel.Trace"/>, the full file path is used. For other levels, a shortened path is used.
    /// </remarks>
    /// <example>
    /// <code>
    /// logger.LogWithFormattedPath(LogLevel.Information, 0, null, "Processed file: {FilePath}", filePath);
    /// </code>
    /// </example>
    public static void LogWithFormattedPath(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        string message,
        string filePath,
        params object[] args)
    {
        // Format the path based on the log level
        string formattedPath = PathFormatter.Format(filePath, logLevel);

        // Create a new array with the formatted path and other arguments
        object[] newArgs = new object[args.Length + 1];
        newArgs[0] = formattedPath;
        Array.Copy(args, 0, newArgs, 1, args.Length);

        // Log the message with the formatted path
        logger.Log(logLevel, eventId, exception, message, newArgs);
    }

    /// <summary>
    /// Logs a message with general string formatting support, without any special file path formatting.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging.</param>
    /// <param name="logLevel">The severity level of the log message.</param>
    /// <param name="eventId">The event ID for structured logging (optional).</param>
    /// <param name="exception">An optional exception to include in the log entry.</param>
    /// <param name="message">A message template with any number of placeholders.</param>
    /// <param name="args">Arguments to be formatted into the message template placeholders.</param>
    /// <example>
    /// <code>
    /// logger.LogFormatted(LogLevel.Warning, 0, null, "Warning: {Detail}", detail);
    /// </code>
    /// </example>
    public static void LogFormatted(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        string message,
        params object[] args)
    {
        logger.Log(logLevel, eventId, exception, message, args);
    }

    /// <summary>
    /// Logs an information-level message with a formatted file path.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging.</param>
    /// <param name="message">A message template that should include a <c>{FilePath}</c> placeholder for the formatted path.</param>
    /// <param name="filePath">The file path to be formatted and injected into the log message.</param>
    /// <param name="args">Additional arguments to be formatted into the message template.</param>
    /// <example>
    /// <code>
    /// logger.LogInformationWithPath("Imported file: {FilePath}", filePath);
    /// </code>
    /// </example>
    public static void LogInformationWithPath(
        this ILogger logger,
        string message,
        string filePath,
        params object[] args)
    {
        LogWithFormattedPath(logger, LogLevel.Information, 0, null, message, filePath, args);
    }

    /// <summary>
    /// Logs an information-level message with general string formatting support.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging.</param>
    /// <param name="message">A message template with any number of placeholders.</param>
    /// <param name="args">Arguments to be formatted into the message template placeholders.</param>
    /// <example>
    /// <code>
    /// logger.LogInformationFormatted("Processed {Count} items", count);
    /// </code>
    /// </example>
    public static void LogInformationFormatted(
        this ILogger logger,
        string message,
        params object[] args)
    {
        LogFormatted(logger, LogLevel.Information, 0, null, message, args);
    }

    /// <summary>
    /// Logs a debug-level message with general string formatting support.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging.</param>
    /// <param name="message">A message template with any number of placeholders.</param>
    /// <param name="args">Arguments to be formatted into the message template placeholders.</param>
    /// <example>
    /// <code>
    /// logger.LogDebugFormatted("Debug info: {Detail}", detail);
    /// </code>
    /// </example>
    public static void LogDebugFormatted(
        this ILogger logger,
        string message,
        params object[] args)
    {
        LogFormatted(logger, LogLevel.Debug, 0, null, message, args);
    }

    /// <summary>
    /// Logs a warning-level message with general string formatting support.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging.</param>
    /// <param name="message">A message template with any number of placeholders.</param>
    /// <param name="args">Arguments to be formatted into the message template placeholders.</param>
    /// <example>
    /// <code>
    /// logger.LogWarningFormatted("Warning: {Detail}", detail);
    /// </code>
    /// </example>
    public static void LogWarningFormatted(
        this ILogger logger,
        string message,
        params object[] args)
    {
        LogFormatted(logger, LogLevel.Warning, 0, null, message, args);
    }

    /// <summary>
    /// Logs an error-level message with general string formatting support.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging.</param>
    /// <param name="message">A message template with any number of placeholders.</param>
    /// <param name="args">Arguments to be formatted into the message template placeholders.</param>
    /// <example>
    /// <code>
    /// logger.LogErrorFormatted("Error: {Detail}", detail);
    /// </code>
    /// </example>
    public static void LogErrorFormatted(
        this ILogger logger,
        string message,
        params object[] args)
    {
        LogFormatted(logger, LogLevel.Error, 0, null, message, args);
    }

    /// <summary>
    /// Logs an error-level message with general string formatting support and an exception.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging.</param>
    /// <param name="exception">The exception to include in the log entry.</param>
    /// <param name="message">A message template with any number of placeholders.</param>
    /// <param name="args">Arguments to be formatted into the message template placeholders.</param>
    /// <example>
    /// <code>
    /// logger.LogErrorFormatted(ex, "Failed: {Detail}", detail);
    /// </code>
    /// </example>
    public static void LogErrorFormatted(
        this ILogger logger,
        Exception exception,
        string message,
        params object[] args)
    {
        LogFormatted(logger, LogLevel.Error, 0, exception, message, args);
    }

    /// <summary>
    /// Logs a warning message with a formatted file path.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="message">The message template with {FilePath} placeholder.</param>
    /// <param name="filePath">The file path to format and log.</param>
    /// <param name="args">Additional arguments for the message template.</param>
    public static void LogWarningWithPath(
        this ILogger logger,
        string message,
        string filePath,
        params object[] args)
    {
        LogWithFormattedPath(logger, LogLevel.Warning, 0, null, message, filePath, args);
    }

    /// <summary>
    /// Logs an error message with a formatted file path.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message template with {FilePath} placeholder.</param>
    /// <param name="filePath">The file path to format and log.</param>
    /// <param name="args">Additional arguments for the message template.</param>
    public static void LogErrorWithPath(
        this ILogger logger,
        Exception exception,
        string message,
        string filePath,
        params object[] args)
    {
        LogWithFormattedPath(logger, LogLevel.Error, 0, exception, message, filePath, args);
    }

    /// <summary>
    /// Logs an error message with a formatted file path.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="message">The message template with {FilePath} placeholder.</param>
    /// <param name="filePath">The file path to format and log.</param>
    /// <param name="args">Additional arguments for the message template.</param>
    public static void LogErrorWithPath(
        this ILogger logger,
        string message,
        string filePath,
        params object[] args)
    {
        LogWithFormattedPath(logger, LogLevel.Error, 0, null, message, filePath, args);
    }

    /// <summary>
    /// Logs a warning message with a formatted file path and exception.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message template with {FilePath} placeholder.</param>
    /// <param name="filePath">The file path to format and log.</param>
    /// <param name="args">Additional arguments for the message template.</param>
    public static void LogWarningWithPath(
        this ILogger logger,
        Exception exception,
        string message,
        string filePath,
        params object[] args)
    {
        LogWithFormattedPath(logger, LogLevel.Warning, 0, exception, message, filePath, args);
    }

    /// <summary>
    /// Logs a debug message with a formatted file path and exception.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message template with {FilePath} placeholder.</param>
    /// <param name="filePath">The file path to format and log.</param>
    /// <param name="args">Additional arguments for the message template.</param>
    public static void LogDebugWithPath(
        this ILogger logger,
        Exception? exception,
        string message,
        string filePath,
        params object[] args)
    {
        LogWithFormattedPath(logger, LogLevel.Debug, 0, exception, message, filePath, args);
    }

    public static void LogDebugWithPath(
        this ILogger logger,
        string message,
        string filePath,
        params object[] args)
    {
        LogWithFormattedPath(logger, LogLevel.Debug, 0, null, message, filePath, args);
    }

    public static void LogErrorWithPath<T>(
        this ILogger<T> logger,
        Exception exception,
        string message,
        string filePath,
        params object[] args)
    {
        LogWithFormattedPath(logger, LogLevel.Error, 0, exception, message, filePath, args);
    }
}
