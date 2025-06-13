// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Utilities;

/// <summary>
/// Provides centralized exception handling for CLI operations.
/// </summary>
/// <remarks>
/// Handles exceptions gracefully based on debug mode:
/// - In DEBUG mode: Shows full exception details and stack trace
/// - In normal mode: Shows user-friendly error messages only
/// </remarks>
public static class ExceptionHandler
{
    private static ILogger? _logger;
    private static bool _debugMode;

    /// <summary>
    /// Initializes the exception handler with logging and debug mode settings.
    /// </summary>
    /// <param name="logger">Logger instance for writing error information.</param>
    /// <param name="debugMode">Whether debug mode is enabled.</param>
    public static void Initialize(ILogger logger, bool debugMode)
    {
        _logger = logger;
        _debugMode = debugMode;
    }

    /// <summary>    /// Handles an exception gracefully based on the configured debug mode.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="operation">Description of the operation that failed.</param>
    /// <param name="exitCode">Exit code to return (default: 1).</param>
    /// <returns>The specified exit code.</returns>
    public static int HandleException(Exception exception, string operation, int exitCode = 1)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);        // Log the exception details based on debug mode
        if (_debugMode)
        {
            // In debug mode, log the full exception details for internal tracking
            _logger?.LogError(exception, $"Failed to execute {operation}: {exception.Message}");
        }
        else
        {
            // In normal mode, log with the exception for tracking but simpler message format
            _logger?.LogError(exception, $"Failed to execute {operation}: {exception.Message}");
        }

        if (_debugMode)
        {
            // In debug mode, show full exception details
            AnsiConsoleHelper.WriteError($"Error in {operation}:");
            AnsiConsoleHelper.WriteError($"  Message: {exception.Message}");
            AnsiConsoleHelper.WriteError($"  Type: {exception.GetType().FullName}");

            if (exception.InnerException != null)
            {
                AnsiConsoleHelper.WriteError($"  Inner Exception: {exception.InnerException.Message}");
            }

            AnsiConsoleHelper.WriteError("Stack Trace:");
            AnsiConsoleHelper.WriteError(exception.StackTrace ?? "No stack trace available");
        }
        else
        {
            // In normal mode, show user-friendly error message
            AnsiConsoleHelper.WriteError($"Error: {GetUserFriendlyMessage(exception, operation)}");
            AnsiConsoleHelper.WriteInfo("Run with --debug flag for detailed error information.");
        }

        return exitCode;
    }

    /// <summary>
    /// Executes an operation with centralized exception handling.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">Name of the operation for error reporting.</param>
    /// <returns>Exit code (0 for success, non-zero for failure).</returns>
    public static async Task<int> ExecuteWithHandling(Func<Task> operation, string operationName)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrEmpty(operationName);

        try
        {
            await operation().ConfigureAwait(false);
            return 0; // Success
        }
        catch (Exception ex)
        {
            return HandleException(ex, operationName);
        }
    }

    /// <summary>
    /// Executes an operation with centralized exception handling and return value.
    /// </summary>
    /// <typeparam name="T">Type of the return value.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">Name of the operation for error reporting.</param>
    /// <param name="defaultValue">Default value to return on error.</param>
    /// <returns>Result of the operation or default value on error.</returns>
    public static async Task<T> ExecuteWithHandling<T>(Func<Task<T>> operation, string operationName, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrEmpty(operationName);

        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            HandleException(ex, operationName);
            return defaultValue;
        }
    }

    /// <summary>
    /// Converts an exception to a user-friendly message.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <returns>User-friendly error message.</returns>
    private static string GetUserFriendlyMessage(Exception exception, string operation)
    {
        return exception switch
        {
            FileNotFoundException fnf => $"Required file not found: {fnf.FileName ?? "Unknown file"}",
            DirectoryNotFoundException => "Required directory not found",
            UnauthorizedAccessException => "Access denied. Check file permissions or run as administrator",
            // Check more specific patterns first (Azure OpenAI, Foundry) before general OpenAI
            InvalidOperationException ioe when ioe.Message.Contains("Azure OpenAI API key is missing") =>
                "Azure OpenAI API key is missing. Please set the AZURE_OPENAI_KEY environment variable with your API key",
            InvalidOperationException ioe when ioe.Message.Contains("Foundry API key is missing") =>
                "Foundry API key is missing. Please set the FOUNDRY_API_KEY environment variable with your API key",
            InvalidOperationException ioe when ioe.Message.Contains("OpenAI API key is missing") =>
                "OpenAI API key is missing. Please set the OPENAI_API_KEY environment variable with your API key",
            InvalidOperationException ioe when ioe.Message.Contains("endpoint is missing") =>
                "AI service endpoint configuration is missing. Please check your configuration file",
            InvalidOperationException ioe when ioe.Message.Contains("deployment name is missing") =>
                "Azure OpenAI deployment name is missing. Please check your configuration file",
            InvalidOperationException ioe when ioe.Message.Contains("Configuration") =>
                "Configuration error. Please check your config file and ensure all required settings are present",
            InvalidOperationException ioe when ioe.Message.Contains("service") =>
                "Internal service configuration error. Please report this issue",
            ArgumentException ae when ae.Message.Contains("apiKey") =>
                "Missing or invalid API key. Please check your configuration file and ensure the API key is properly set",
            ArgumentException ae when ae.Message.Contains("empty string") =>
                "Configuration contains empty values. Please check your configuration file for missing required settings",
            ArgumentException ae => $"Invalid argument: {ae.Message}",
            TimeoutException => "Operation timed out. Please try again",
            TaskCanceledException => "Operation was cancelled",
            NotImplementedException => "This feature is not yet implemented",
            _ => $"An error occurred while {operation.ToLowerInvariant()}: {exception.Message}"
        };
    }

    /// <summary>
    /// Checks if the exception handler is properly initialized.
    /// </summary>
    /// <returns>True if initialized, false otherwise.</returns>
    public static bool IsInitialized => _logger != null;
}
