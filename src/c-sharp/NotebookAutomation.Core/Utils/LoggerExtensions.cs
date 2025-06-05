#nullable enable

using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Utils
{    /// <summary>
    /// Extension methods for ILogger to support both file path formatting and general string formatting.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Logs a message with a formatted file path depending on the log level.
        /// For Debug or Trace level, uses the full path. For other levels, shortens the path.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="logLevel">The log level.</param>
        /// <param name="eventId">The event ID.</param>
        /// <param name="exception">Optional exception to log.</param>
        /// <param name="message">The message template with {FilePath} placeholder.</param>
        /// <param name="filePath">The file path to format and log.</param>
        /// <param name="args">Additional arguments for the message template.</param>
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
        /// Logs a message with general string formatting support.
        /// This method supports any number of placeholders and arguments without special file path formatting.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="logLevel">The log level.</param>
        /// <param name="eventId">The event ID.</param>
        /// <param name="exception">Optional exception to log.</param>
        /// <param name="message">The message template with placeholders.</param>
        /// <param name="args">Arguments for the message template placeholders.</param>
        public static void LogFormatted(
            this ILogger logger,
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            string message,
            params object[] args)
        {
            logger.Log(logLevel, eventId, exception, message, args);
        }        /// <summary>
        /// Logs an information message with a formatted file path.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message template with {FilePath} placeholder.</param>
        /// <param name="filePath">The file path to format and log.</param>
        /// <param name="args">Additional arguments for the message template.</param>
        public static void LogInformationWithPath(
            this ILogger logger,
            string message,
            string filePath,
            params object[] args)
        {
            LogWithFormattedPath(logger, LogLevel.Information, 0, null, message, filePath, args);
        }        /// <summary>
        /// Logs an information message with general string formatting support.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message template with placeholders.</param>
        /// <param name="args">Arguments for the message template placeholders.</param>
        public static void LogInformationFormatted(
            this ILogger logger,
            string message,
            params object[] args)
        {
            LogFormatted(logger, LogLevel.Information, 0, null, message, args);
        }

        /// <summary>
        /// Logs a debug message with general string formatting support.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message template with placeholders.</param>
        /// <param name="args">Arguments for the message template placeholders.</param>
        public static void LogDebugFormatted(
            this ILogger logger,
            string message,
            params object[] args)
        {
            LogFormatted(logger, LogLevel.Debug, 0, null, message, args);
        }

        /// <summary>
        /// Logs a warning message with general string formatting support.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message template with placeholders.</param>
        /// <param name="args">Arguments for the message template placeholders.</param>
        public static void LogWarningFormatted(
            this ILogger logger,
            string message,
            params object[] args)
        {
            LogFormatted(logger, LogLevel.Warning, 0, null, message, args);
        }

        /// <summary>
        /// Logs an error message with general string formatting support.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message template with placeholders.</param>
        /// <param name="args">Arguments for the message template placeholders.</param>
        public static void LogErrorFormatted(
            this ILogger logger,
            string message,
            params object[] args)
        {
            LogFormatted(logger, LogLevel.Error, 0, null, message, args);
        }

        /// <summary>
        /// Logs an error message with general string formatting support and exception.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message template with placeholders.</param>
        /// <param name="args">Arguments for the message template placeholders.</param>
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
}
