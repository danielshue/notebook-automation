// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Defines the contract for a centralized logging service for the notebook automation system.
/// </summary>
/// <remarks>
/// <para>
/// The ILoggingService interface provides methods for creating appropriately configured
/// ILogger instances for different parts of the application.
/// </para>
/// </remarks>
public interface ILoggingService
{
    /// <summary>
    /// Gets the main logger instance used for general application logging (Microsoft.Extensions.Logging).
    /// </summary>
    Microsoft.Extensions.Logging.ILogger Logger { get; }

    /// <summary>
    /// Gets the specialized logger instance used for recording failed operations (Microsoft.Extensions.Logging).
    /// </summary>
    Microsoft.Extensions.Logging.ILogger FailedLogger { get; }

    /// <summary>
    /// Gets a typed ILogger instance for the specified type T from this LoggingService instance.
    /// </summary>
    /// <typeparam name="T">The type to create the logger for.</typeparam>
    /// <returns>An ILogger{T} configured for the specified type.</returns>
    Microsoft.Extensions.Logging.ILogger<T> GetLogger<T>();

    /// <summary>
    /// Configures the logging builder with the appropriate providers.
    /// </summary>
    /// <param name="builder">The logging builder to configure.</param>
    void ConfigureLogging(ILoggingBuilder builder);

    /// <summary>
    /// Gets the full path to the current log file.
    /// </summary>
    /// <returns>The absolute path to the current log file, or null if logging is not configured to a file.</returns>
    string? CurrentLogFilePath { get; }
}
