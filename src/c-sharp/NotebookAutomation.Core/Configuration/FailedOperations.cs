// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Contains methods and constants for managing and recording failed operations.
/// </summary>
/// <remarks>
/// <para>
/// The FailedOperations class provides a centralized way to handle operations that fail
/// during execution. It works in conjunction with specialized loggers created by the
/// <see cref="LoggingService"/> to record detailed information about failures in a consistent format.
/// </para>
/// <para>
/// This class is primarily used as a category name for specialized loggers and as a container
/// for constants and static methods related to failed operations.
/// </para>
/// <para>
/// When operations fail, they should be logged using a failed logger (created with
/// <see cref="LoggingService.CreateFailedLogger"/>) to ensure consistent tracking and reporting
/// of failures throughout the application.
/// </para>
/// </remarks>
/// <example>
/// Example of using a failed logger to record a failed operation:
/// <code>
/// try
/// {
///     // Perform operation
///     await ProcessFile(filePath);
/// }
/// catch (Exception ex)
/// {
///     failedLogger.LogError("Failed to process file: {Path}. Error: {Error}", filePath, ex.Message);
/// }
/// </code>
/// </example>
public static class FailedOperations
{
    /// <summary>
    /// Records a failed file operation using the provided logger.
    /// </summary>
    /// <param name="failedLogger">The logger to record the failure with.</param>
    /// <param name="filePath">The path to the file that failed to process.</param>
    /// <param name="operationName">The name of the operation that failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <remarks>
    /// <para>
    /// This method provides a standardized way to log failed file operations, ensuring
    /// consistent formatting and detail level across the application.
    /// </para>
    /// <para>
    /// The method logs the failure at the Error level, including the file path, operation name,
    /// and exception details (message and stack trace).
    /// </para>
    /// </remarks>
    public static void RecordFailedFileOperation(
        ILogger failedLogger,
        string filePath,
        string operationName,
        Exception exception)
    {
        failedLogger.LogError(
            exception,
            "Failed operation: {Operation} on file {FilePath}. Error: {ErrorMessage}",
            operationName,
            filePath,
            exception.Message);
    }

    /// <summary>
    /// Records a failed file operation with a custom error message.
    /// </summary>
    /// <param name="failedLogger">The logger to record the failure with.</param>
    /// <param name="filePath">The path to the file that failed to process.</param>
    /// <param name="operationName">The name of the operation that failed.</param>
    /// <param name="errorMessage">A custom error message describing the failure.</param>
    /// <remarks>
    /// <para>
    /// This overload is useful when you want to provide a custom error message rather than
    /// recording an exception's details. This is common when operations fail for logical
    /// reasons rather than due to exceptions.
    /// </para>
    /// <para>
    /// The method logs the failure at the Error level, including the file path, operation name,
    /// and the provided error message.
    /// </para>
    /// </remarks>
    public static void RecordFailedFileOperation(
        ILogger failedLogger,
        string filePath,
        string operationName,
        string errorMessage)
    {
        failedLogger.LogError(
            "Failed operation: {Operation} on file {FilePath}. Error: {ErrorMessage}",
            operationName,
            filePath,
            errorMessage);
    }
}
