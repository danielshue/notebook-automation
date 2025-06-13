// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Represents the result of a configuration management operation.
/// </summary>
/// <remarks>
/// This class encapsulates the outcome of configuration discovery and loading operations,
/// including success/failure status, the loaded configuration, and any error information.
/// </remarks>
public class ConfigManagerResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    /// <value>
    /// true if the operation succeeded; otherwise, false.
    /// </value>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the loaded application configuration.
    /// </summary>
    /// <value>
    /// The application configuration if loading was successful; otherwise, null.
    /// </value>
    public AppConfig? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the path to the configuration file that was loaded.
    /// </summary>
    /// <value>
    /// The full path to the configuration file that was successfully loaded, or null if no configuration was loaded.
    /// </value>
    public string? ConfigurationPath { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    /// <value>
    /// A descriptive error message if the operation failed; otherwise, null.
    /// </value>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception that caused the operation to fail.
    /// </summary>
    /// <value>
    /// The exception that caused the failure; otherwise, null.
    /// </value>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Creates a successful result with the specified configuration and path.
    /// </summary>
    /// <param name="configuration">The loaded application configuration.</param>
    /// <param name="configurationPath">The path to the configuration file.</param>
    /// <returns>A successful configuration manager result.</returns>
    public static ConfigManagerResult Success(AppConfig configuration, string configurationPath)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(configurationPath);

        return new ConfigManagerResult
        {
            IsSuccess = true,
            Configuration = configuration,
            ConfigurationPath = configurationPath
        };
    }

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed configuration manager result.</returns>
    public static ConfigManagerResult Failure(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);

        return new ConfigManagerResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates a failed result with the specified error message and exception.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed configuration manager result.</returns>
    public static ConfigManagerResult Failure(string errorMessage, Exception exception)
    {
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);
        ArgumentNullException.ThrowIfNull(exception);

        return new ConfigManagerResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}
