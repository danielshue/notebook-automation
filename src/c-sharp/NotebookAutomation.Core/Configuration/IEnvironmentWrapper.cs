// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Defines an abstraction for environment variable operations to enable testability.
/// </summary>
/// <remarks>
/// This interface wraps environment variable access, allowing for easy mocking
/// and testing of components that depend on environment variables.
/// </remarks>
public interface IEnvironmentWrapper
{
    /// <summary>
    /// Gets the value of the specified environment variable.
    /// </summary>
    /// <param name="variableName">The name of the environment variable.</param>
    /// <returns>The value of the environment variable, or null if it doesn't exist.</returns>
    string? GetEnvironmentVariable(string variableName);

    /// <summary>
    /// Gets the value of the specified environment variable from the specified target.
    /// </summary>
    /// <param name="variableName">The name of the environment variable.</param>
    /// <param name="target">The target scope for the environment variable.</param>
    /// <returns>The value of the environment variable, or null if it doesn't exist.</returns>
    string? GetEnvironmentVariable(string variableName, EnvironmentVariableTarget target);

    /// <summary>
    /// Gets the current working directory.
    /// </summary>
    /// <returns>The current working directory.</returns>
    string GetCurrentDirectory();    /// <summary>
                                     /// Gets the directory containing the current executable.
                                     /// </summary>
                                     /// <returns>The directory containing the current executable.</returns>
    string GetExecutableDirectory();

    /// <summary>
    /// Determines whether the current environment is a development environment.
    /// </summary>
    /// <returns>true if the environment is development; otherwise, false.</returns>
    bool IsDevelopment();
}
