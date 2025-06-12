// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Provides a concrete implementation of environment variable operations.
/// </summary>
/// <remarks>
/// This class wraps standard .NET environment operations, providing a testable
/// abstraction layer for components that need to access environment variables.
/// </remarks>
public class EnvironmentWrapper : IEnvironmentWrapper
{
    /// <summary>
    /// Gets the value of the specified environment variable.
    /// </summary>
    /// <param name="variableName">The name of the environment variable.</param>
    /// <returns>The value of the environment variable, or null if it doesn't exist.</returns>
    public string? GetEnvironmentVariable(string variableName) => Environment.GetEnvironmentVariable(variableName);

    /// <summary>
    /// Gets the value of the specified environment variable from the specified target.
    /// </summary>
    /// <param name="variableName">The name of the environment variable.</param>
    /// <param name="target">The target scope for the environment variable.</param>
    /// <returns>The value of the environment variable, or null if it doesn't exist.</returns>
    public string? GetEnvironmentVariable(string variableName, EnvironmentVariableTarget target) => Environment.GetEnvironmentVariable(variableName, target);

    /// <summary>
    /// Gets the current working directory.
    /// </summary>
    /// <returns>The current working directory.</returns>
    public string GetCurrentDirectory() => Environment.CurrentDirectory;    /// <summary>
                                                                            /// Gets the directory containing the current executable.
                                                                            /// </summary>
                                                                            /// <returns>The directory containing the current executable.</returns>
    public string GetExecutableDirectory()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var location = assembly.Location;
        return Path.GetDirectoryName(location) ?? Environment.CurrentDirectory;
    }

    /// <summary>
    /// Determines whether the current environment is a development environment.
    /// </summary>
    /// <returns>true if the environment is development; otherwise, false.</returns>
    public bool IsDevelopment()
    {
        var environment = GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
    }
}