// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Represents errors that occur during configuration operations.
/// </summary>
/// <remarks>
/// This exception is thrown when configuration discovery, loading, or validation
/// operations encounter errors that prevent successful configuration management.
/// </remarks>
public class ConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    public ConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}